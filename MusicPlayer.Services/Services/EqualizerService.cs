using System;
using NAudio.Wave;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Audio;
using System.Linq;
using System.Collections.Generic;

namespace MusicPlayer.Services.Services
{
    /// <summary>
    /// 均衡器服务实现
    /// </summary>
    public class EqualizerService : IEqualizerService
    {
        private readonly EqualizerSettings _settings;
        private object? _currentEqualizerStream;
        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private readonly IEqualizerPresetRepository? _presetRepository;

        /// <summary>
        /// 均衡器设置
        /// </summary>
        public EqualizerSettings Settings => _settings;

        /// <summary>
        /// 是否启用均衡器
        /// </summary>
        public bool IsEnabled
        {
            get => _settings.IsEnabled;
            set
            {
                if (_settings.IsEnabled != value)
                {
                    _settings.IsEnabled = value;
                    
                    // 如果有当前音频流，更新其状态
                    if (_currentEqualizerStream is EqualizerStream equalizerStream)
                    {
                        if (!value)
                        {
                            // 禁用均衡器，重置所有增益
                            equalizerStream.Reset();
                            System.Diagnostics.Debug.WriteLine($"EqualizerService: 均衡器已禁用，已重置所有频段增益");
                        }
                        else
                        {
                            // 启用均衡器，应用当前设置
                            equalizerStream.SetAllBandGains(_settings.BandGains);
                            System.Diagnostics.Debug.WriteLine($"EqualizerService: 均衡器已启用，已应用所有频段增益");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"EqualizerService: 没有当前音频流，均衡器状态已保存到设置中");
                    }
                }
            }
        }

        /// <summary>
        /// 均衡器预设名称
        /// </summary>
        public string PresetName
        {
            get => _settings.PresetName;
            set
            {
                if (_settings.PresetName != value)
                {
                    _settings.PresetName = value;
                    
                    // 如果有当前音频流，应用新预设
                    if (_currentEqualizerStream is EqualizerStream equalizerStream)
                    {
                        equalizerStream.SetAllBandGains(_settings.BandGains);
                    }
                }
            }
        }

        /// <summary>
        /// 初始化均衡器服务
        /// </summary>
        public EqualizerService()
        {
            _settings = new EqualizerSettings();
        }

        /// <summary>
        /// 初始化均衡器服务（带预设仓库）
        /// </summary>
        /// <param name="presetRepository">预设仓库</param>
        public EqualizerService(IEqualizerPresetRepository presetRepository)
        {
            _settings = new EqualizerSettings();
            _presetRepository = presetRepository;
        }

        /// <summary>
        /// 应用均衡器设置到音频流
        /// </summary>
        /// <param name="audioStream">原始音频流</param>
        /// <returns>处理后的音频流</returns>
        public object? ApplyEqualizer(object? audioStream)
        {
            if (_disposed)
                return audioStream;

            if (!_settings.IsEnabled || audioStream == null)
                return audioStream;

            lock (_lockObject)
            {
                try
                {
                    // 如果已经有均衡器流，先释放
                    if (_currentEqualizerStream is IDisposable disposableStream)
                    {
                        disposableStream.Dispose();
                        _currentEqualizerStream = null;
                    }

                    // 检查是否为WaveStream
                    if (audioStream is WaveStream sourceWaveStream)
                    {
                        // 创建新的均衡器流
                        var equalizerStream = new EqualizerStream(sourceWaveStream, EqualizerSettings.FrequencyBands);
                        
                        // 应用当前设置
                        equalizerStream.SetAllBandGains(_settings.BandGains);
                        
                        _currentEqualizerStream = equalizerStream;
                        return equalizerStream;
                    }
                    // 检查是否为ISampleProvider
                    else if (audioStream is ISampleProvider sampleProvider)
                    {
                        // 创建一个适配器，将ISampleProvider转换为WaveStream
                        var adapterWaveStream = new WaveProvider32ToWaveStream(sampleProvider);
                        
                        // 创建新的均衡器流
                        var equalizerStream = new EqualizerStream(adapterWaveStream, EqualizerSettings.FrequencyBands);
                        
                        // 应用当前设置
                        equalizerStream.SetAllBandGains(_settings.BandGains);
                        
                        _currentEqualizerStream = equalizerStream;
                        return equalizerStream;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EqualizerService: 应用均衡器时发生错误: {ex.Message}");
                }
            }

            return audioStream;
        }

        /// <summary>
        /// 设置指定频段的增益
        /// </summary>
        /// <param name="bandIndex">频段索引 (0-9)</param>
        /// <param name="gain">增益值 (dB)</param>
        public void SetBandGain(int bandIndex, float gain)
        {
            if (_disposed || bandIndex < 0 || bandIndex >= EqualizerSettings.FrequencyBands.Length)
                return;

            // 限制增益范围
            gain = Math.Clamp(gain, -12f, 12f);
            
            // 检查值是否真的有变化
            if (Math.Abs(_settings[bandIndex] - gain) < 0.01f)
                return;

            // 更新设置
            _settings[bandIndex] = gain;
            
            // 添加调试信息
            System.Diagnostics.Debug.WriteLine($"EqualizerService: 设置频段{bandIndex}增益为{gain}dB");

            // 如果有当前音频流，更新其增益
            lock (_lockObject)
            {
                if (_currentEqualizerStream is EqualizerStream equalizerStream)
                {
                    equalizerStream.SetBandGain(bandIndex, gain);
                    System.Diagnostics.Debug.WriteLine($"EqualizerService: 已将增益应用到当前音频流");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"EqualizerService: 没有当前音频流，增益值已保存到设置中");
                }
            }
        }

        /// <summary>
        /// 获取指定频段的增益
        /// </summary>
        /// <param name="bandIndex">频段索引 (0-9)</param>
        /// <returns>增益值 (dB)</returns>
        public float GetBandGain(int bandIndex)
        {
            if (_disposed || bandIndex < 0 || bandIndex >= EqualizerSettings.FrequencyBands.Length)
            {
                System.Diagnostics.Debug.WriteLine($"EqualizerService: GetBandGain - 无效的频段索引: {bandIndex}");
                return 0f;
            }

            float gain = _settings[bandIndex];
            System.Diagnostics.Debug.WriteLine($"EqualizerService: GetBandGain - 频段{bandIndex}增益: {gain:F1}dB");
            return gain;
        }

        /// <summary>
        /// 重置均衡器设置
        /// </summary>
        public void Reset()
        {
            // 重置设置
            _settings.Reset();

            // 如果有当前音频流，重置其均衡器
            lock (_lockObject)
            {
                if (_currentEqualizerStream is EqualizerStream equalizerStream)
                {
                    equalizerStream.Reset();
                }
            }
        }

        /// <summary>
        /// 从配置加载均衡器设置
        /// </summary>
        /// <param name="config">播放器配置</param>
        public void LoadFromConfiguration(PlayerConfiguration config)
        {
            if (config == null || _disposed)
                return;

            try
            {
                // 加载是否启用
                _settings.IsEnabled = config.IsEqualizerEnabled;
                
                // 加载预设名称
                _settings.PresetName = config.EqualizerPresetName;
                
                // 加载增益值
                float[] gains = config.EqualizerGains;
                if (gains != null && gains.Length == 10)
                {
                    System.Diagnostics.Debug.WriteLine("EqualizerService: 从配置加载均衡器增益值");
                    for (int i = 0; i < 10; i++)
                    {
                        _settings[i] = gains[i];
                        System.Diagnostics.Debug.WriteLine($"  频段{i}: {gains[i]:F1}dB");
                    }
                }
                
                // 尝试从数据库获取预设的增益值进行比较
                if (_presetRepository != null && !string.IsNullOrEmpty(config.EqualizerPresetName))
                {
                    var preset = _presetRepository.GetByName(config.EqualizerPresetName);
                    if (preset != null)
                    {
                        // 如果增益值与预设不匹配，标记为自定义
                        bool isCustom = false;
                        for (int i = 0; i < 10; i++)
                        {
                            if (Math.Abs(gains[i] - preset.GetBandGain(i)) > 0.5f)
                            {
                                isCustom = true;
                                break;
                            }
                        }
                        
                        if (isCustom)
                        {
                            _settings.PresetName = "自定义";
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 从配置加载均衡器设置，启用={_settings.IsEnabled}，预设={_settings.PresetName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 从配置加载均衡器设置时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存均衡器设置到配置
        /// </summary>
        /// <param name="config">播放器配置</param>
        public void SaveToConfiguration(PlayerConfiguration config)
        {
            if (config == null || _disposed)
                return;

            try
            {
                // 保存是否启用
                config.IsEqualizerEnabled = _settings.IsEnabled;
                
                //// 保存预设
                //config.EqualizerPreset = _settings.Preset;
                
                // 保存增益值
                config.EqualizerGains = _settings.BandGains;
                
                //System.Diagnostics.Debug.WriteLine($"EqualizerService: 保存均衡器设置到配置，启用={_settings.IsEnabled}，预设={_settings.Preset}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 保存均衡器设置到配置时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存均衡器设置到配置服务
        /// </summary>
        /// <param name="configService">配置服务</param>
        public void SaveToConfigurationService(IConfigurationService configService)
        {
            if (configService == null || _disposed)
                return;

            try
            {
                // 保存是否启用
                configService.UpdateEqualizerEnabled(_settings.IsEnabled);
                
                // 保存预设名称
                configService.UpdateEqualizerPresetName(_settings.PresetName);
                
                // 保存增益值
                configService.UpdateEqualizerGains(_settings.BandGains);
                
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 保存均衡器设置到配置服务，启用={_settings.IsEnabled}，预设={_settings.PresetName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 保存均衡器设置到配置服务时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用指定预设的增益值
        /// </summary>
        /// <param name="preset">均衡器预设</param>
        public void ApplyPreset(EqualizerPreset preset)
        {
            if (preset == null || _disposed)
                return;

            try
            {
                // 设置预设名称
                _settings.PresetName = preset.PresetName;
                
                // 设置增益值
                for (int i = 0; i < 10; i++)
                {
                    _settings[i] = preset.GetBandGain(i);
                }
                
                // 如果有当前音频流，应用新预设
                lock (_lockObject)
                {
                    if (_currentEqualizerStream is EqualizerStream equalizerStream)
                    {
                        equalizerStream.SetAllBandGains(_settings.BandGains);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 已应用预设 '{preset.PresetName}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 应用预设失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有可用的均衡器预设
        /// </summary>
        /// <returns>均衡器预设列表</returns>
        public List<EqualizerPreset> GetAvailablePresets()
        {
            if (_disposed || _presetRepository == null)
                return new List<EqualizerPreset>();

            try
            {
                var presets = _presetRepository.GetAll();
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 获取到 {presets.Count} 个可用预设");
                return presets;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EqualizerService: 获取可用预设失败: {ex.Message}");
                return new List<EqualizerPreset>();
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        /// <param name="disposing">是否正在释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lockObject)
                {
                    if (_currentEqualizerStream is IDisposable disposableStream)
                    {
                        disposableStream.Dispose();
                        _currentEqualizerStream = null;
                    }
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 将ISampleProvider转换为WaveStream的适配器类
    /// </summary>
    public class WaveProvider32ToWaveStream : WaveStream
    {
        private readonly ISampleProvider _sourceProvider;
        private readonly object _lockObject = new object();
        private long _position;
        private float[]? _sampleBuffer; // 预分配的样本缓冲区，避免频繁GC
        private const int _maxBufferSize = 32768; // 32KB的最大缓冲区大小

        /// <summary>
        /// 初始化适配器
        /// </summary>
        /// <param name="sourceProvider">源样本提供者</param>
        public WaveProvider32ToWaveStream(ISampleProvider sourceProvider)
        {
            _sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
        }

        /// <summary>
        /// 音频格式
        /// </summary>
        public override WaveFormat WaveFormat => _sourceProvider.WaveFormat;

        /// <summary>
        /// 流长度
        /// </summary>
        public override long Length => long.MaxValue; // 无法确定样本提供者的长度

        /// <summary>
        /// 当前位置
        /// </summary>
        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        /// <summary>
        /// 读取音频数据
        /// </summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">字节数</param>
        /// <returns>实际读取的字节数</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_lockObject)
            {
                // 计算需要的样本数
                int bytesPerSample = 4; // 32位音频，每样本4字节
                int samplesRequired = count / bytesPerSample;
                
                // 确保样本缓冲区足够大
                if (_sampleBuffer == null || _sampleBuffer.Length < samplesRequired)
                {
                    // 分配足够大的缓冲区，最大不超过_maxBufferSize
                    int bufferSize = Math.Min(samplesRequired, _maxBufferSize);
                    _sampleBuffer = new float[bufferSize];
                }
                
                // 从样本提供者读取数据
                int samplesRead = _sourceProvider.Read(_sampleBuffer, 0, samplesRequired);

                // 将浮点样本转换为字节数组
                int bytesRead = 0;
                for (int i = 0; i < samplesRead; i++)
                {
                    int bytePos = offset + i * bytesPerSample;
                    if (bytePos + bytesPerSample <= buffer.Length)
                    {
                        BitConverter.GetBytes(_sampleBuffer[i]).CopyTo(buffer, bytePos);
                        bytesRead += bytesPerSample;
                    }
                }

                _position += bytesRead;
                return bytesRead;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 释放样本提供者资源
                if (_sourceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
                
                // 清理缓冲区
                _sampleBuffer = null;
            }
            base.Dispose(disposing);
        }
    }
}