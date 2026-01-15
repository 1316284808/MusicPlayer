using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;
using MusicPlayer.Services;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 音频设置视图模型
    /// </summary>
    public partial class SoundSettingsViewModel : ObservableObject, ISoundSettingsViewModel, IDisposable
    {
        private AudioEngine _currentAudioEngine = AudioEngine.Auto;
        private readonly IConfigurationService _configurationService;
        private readonly IMessagingService _messagingService;
        private readonly IEqualizerService _equalizerService;
        private readonly IEqualizerPresetRepository _presetRepository;
        private bool _disposed = false;
        private bool _isEqualizerEnabled = false;
        private string _currentEqualizerPresetName = "平衡";
        private float[] _bandGains = new float[10];
        private List<string> _equalizerPresetNames = new();
        private bool _isSavePresetExpanded = false;
        private string _newPresetName = string.Empty;

        /// <summary>
        /// 当前音频引擎
        /// </summary>
        public AudioEngine CurrentAudioEngine
        {
            get => _currentAudioEngine;
            set
            {
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.CurrentAudioEngine: Setter被调用，当前值={_currentAudioEngine}, 新值={value}");
                if (_currentAudioEngine != value)
                {
                    _currentAudioEngine = value;
                    OnPropertyChanged();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.CurrentAudioEngine: 值已更新为{_currentAudioEngine}");

                    // 保存设置到配置
                    SaveAudioEngineSetting(value);

                    // 发送消息通知其他组件音频引擎变更
                    SendAudioEngineChangedMessage(value);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.CurrentAudioEngine: 值未变化，不更新");
                }
            }
        }

        /// <summary>
        /// 可用的音频引擎选项
        /// </summary>
        public AudioEngine[] AudioEngineOptions => Enum.GetValues(typeof(AudioEngine)).Cast<AudioEngine>().ToArray();


        public string IsEqualizerEnabledText => IsEqualizerEnabled ? "开启" : "禁用";

        /// <summary>
        /// 是否启用均衡器
        /// </summary>
        public bool IsEqualizerEnabled
        {
            get => _isEqualizerEnabled;
            set
            {
                if (_isEqualizerEnabled != value)
                {
                    _isEqualizerEnabled = value;
                    OnPropertyChanged(nameof(IsEqualizerEnabled));
                    OnPropertyChanged(nameof(IsEqualizerEnabledText));

                    _equalizerService.IsEnabled = value;

                    // 保存设置
                    SaveEqualizerSettings();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.IsEqualizerEnabled: 值未变化，不更新");
                }
            }
        }

        /// <summary>
        /// 当前均衡器预设名称
        /// </summary>
        public string CurrentEqualizerPresetName
        {
            get => _currentEqualizerPresetName;
            set
            {
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.CurrentEqualizerPresetName: Setter被调用，当前值={_currentEqualizerPresetName}, 新值={value}");
                if (_currentEqualizerPresetName != value)
                {
                    _currentEqualizerPresetName = value ?? "平衡";
                    OnPropertyChanged();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.CurrentEqualizerPresetName: 值已更新为{_currentEqualizerPresetName}");

                    // 查找并应用预设
                    try
                    {
                        var presets = _equalizerService.GetAvailablePresets();
                        var selectedPreset = presets.FirstOrDefault(p => p.PresetName == _currentEqualizerPresetName);

                        if (selectedPreset != null)
                        {
                            // 应用预设到均衡器服务
                            _equalizerService.ApplyPreset(selectedPreset);
                            System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 已应用预设 '{_currentEqualizerPresetName}'");

                            // 更新本地频段增益值
                            for (int i = 0; i < 10; i++)
                            {
                                _bandGains[i] = selectedPreset.GetBandGain(i);
                            }

                            // 通知UI更新
                            OnPropertyChanged(nameof(BandGains));
                            for (int i = 0; i < 10; i++)
                            {
                                OnPropertyChanged($"BandGain{i}");
                            }

                            // 输出当前所有频段的增益值，用于调试
                            System.Diagnostics.Debug.WriteLine("SoundSettingsViewModel: 应用预设后的均衡器频段增益值:");
                            for (int i = 0; i < 10; i++)
                            {
                                System.Diagnostics.Debug.WriteLine($"  频段{i}: {_bandGains[i]:F1}dB");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 未找到预设 '{_currentEqualizerPresetName}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 应用预设失败: {ex.Message}");
                    }

                    // 保存设置
                    SaveEqualizerSettings();

                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 均衡器预设设置为 {_currentEqualizerPresetName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.CurrentEqualizerPresetName: 值未变化，不更新");
                }
            }
        }

        /// <summary>
        /// 可用的均衡器预设选项
        /// </summary>
        public List<string> EqualizerPresetNames
        {
            get
            {
                if (_equalizerPresetNames.Count == 0)
                {
                    LoadEqualizerPresets();
                }
                return _equalizerPresetNames;
            }
        }

        /// <summary>
        /// 可用的均衡器预设对象列表
        /// </summary>
        public EqualizerPreset[] EqualizerPresets
        {
            get
            {
                try
                {
                    var presets = _equalizerService.GetAvailablePresets();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 获取到 {presets.Count} 个均衡器预设");
                    return presets.ToArray();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 获取均衡器预设失败: {ex.Message}");
                    return Array.Empty<EqualizerPreset>();
                }
            }
        }

        /// <summary>
        /// 均衡器频段增益值
        /// </summary>
        public float[] BandGains
        {
            get => _bandGains;
            set
            {
                if (value != null && value.Length == 10)
                {
                    bool changed = false;
                    for (int i = 0; i < 10; i++)
                    {
                        if (Math.Abs(_bandGains[i] - value[i]) > 0.01f) // 降低阈值，提高响应性
                        {
                            _bandGains[i] = value[i];
                            OnPropertyChanged($"BandGains[{i}]");
                            changed = true;

                            // 更新均衡器服务
                            _equalizerService.SetBandGain(i, value[i]);

                            // 添加调试信息
                            System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段{i}增益为{value[i]}dB");
                        }
                    }

                    if (changed)
                    {
                        OnPropertyChanged(nameof(BandGains));

                        // 如果不是自定义预设，则切换到自定义
                        if (CurrentEqualizerPresetName != "自定义")
                        {
                            CurrentEqualizerPresetName = "自定义";
                        }

                        // 保存设置
                        SaveEqualizerSettings();
                    }
                }
            }
        }

        // 为每个频段创建独立的属性，以便于UI绑定
        public float BandGain0
        {
            get => _bandGains[0];
            set
            {
                if (Math.Abs(_bandGains[0] - value) > 0.01f)
                {
                    _bandGains[0] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(0, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段0增益为{value}dB");
                }
            }
        }

        public float BandGain1
        {
            get => _bandGains[1];
            set
            {
                if (Math.Abs(_bandGains[1] - value) > 0.01f)
                {
                    _bandGains[1] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(1, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段1增益为{value}dB");
                }
            }
        }

        public float BandGain2
        {
            get => _bandGains[2];
            set
            {
                if (Math.Abs(_bandGains[2] - value) > 0.01f)
                {
                    _bandGains[2] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(2, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段2增益为{value}dB");
                }
            }
        }

        public float BandGain3
        {
            get => _bandGains[3];
            set
            {
                if (Math.Abs(_bandGains[3] - value) > 0.01f)
                {
                    _bandGains[3] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(3, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段3增益为{value}dB");
                }
            }
        }

        public float BandGain4
        {
            get => _bandGains[4];
            set
            {
                if (Math.Abs(_bandGains[4] - value) > 0.01f)
                {
                    _bandGains[4] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(4, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段4增益为{value}dB");
                }
            }
        }

        public float BandGain5
        {
            get => _bandGains[5];
            set
            {
                if (Math.Abs(_bandGains[5] - value) > 0.01f)
                {
                    _bandGains[5] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(5, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段5增益为{value}dB");
                }
            }
        }

        public float BandGain6
        {
            get => _bandGains[6];
            set
            {
                if (Math.Abs(_bandGains[6] - value) > 0.01f)
                {
                    _bandGains[6] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(6, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段6增益为{value}dB");
                }
            }
        }

        public float BandGain7
        {
            get => _bandGains[7];
            set
            {
                if (Math.Abs(_bandGains[7] - value) > 0.01f)
                {
                    _bandGains[7] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(7, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段7增益为{value}dB");
                }
            }
        }

        public float BandGain8
        {
            get => _bandGains[8];
            set
            {
                if (Math.Abs(_bandGains[8] - value) > 0.01f)
                {
                    _bandGains[8] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(8, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段8增益为{value}dB");
                }
            }
        }

        public float BandGain9
        {
            get => _bandGains[9];
            set
            {
                if (Math.Abs(_bandGains[9] - value) > 0.01f)
                {
                    _bandGains[9] = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BandGains));
                    _equalizerService.SetBandGain(9, value);

                    // 如果不是自定义预设，则切换到自定义
                    if (CurrentEqualizerPresetName != "自定义")
                    {
                        CurrentEqualizerPresetName = "自定义";
                    }

                    SaveEqualizerSettings();
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 更新频段9增益为{value}dB");
                }
            }
        }

        /// <summary>
        /// 重置均衡器命令
        /// </summary>
        [RelayCommand]
        private void ResetEqualizer()
        {
            _currentEqualizerPresetName = "平衡";
            OnPropertyChanged(nameof(CurrentEqualizerPresetName));

            // 重置所有频段增益
            for (int i = 0; i < 10; i++)
            {
                _bandGains[i] = 0f;
                _equalizerService.SetBandGain(i, 0f);
            }

            // 通知UI更新
            OnPropertyChanged(nameof(BandGains));
            for (int i = 0; i < 10; i++)
            {
                OnPropertyChanged($"BandGain{i}");
            }

            // 保存设置
            SaveEqualizerSettings();

            System.Diagnostics.Debug.WriteLine("SoundSettingsViewModel: 重置均衡器设置");
        }

        /// <summary>
        /// 是否展开保存预设输入框
        /// </summary>
        public bool IsSavePresetExpanded
        {
            get => _isSavePresetExpanded;
            set
            {
                if (_isSavePresetExpanded != value)
                {
                    _isSavePresetExpanded = value;
                    OnPropertyChanged();

                    // 如果展开，发送消息请求UI获取焦点
                    if (value)
                    {
                        _messagingService.Send(new SavePresetFocusRequestMessage());
                    }
                }
            }
        }

        /// <summary>
        /// 新预设名称
        /// </summary>
        public string NewPresetName
        {
            get => _newPresetName;
            set
            {
                if (_newPresetName != value)
                {
                    _newPresetName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 保存预设按钮点击命令
        /// </summary>
        [RelayCommand]
        private void SavePreset()
        {
            try
            {
                if (!IsSavePresetExpanded)
                {
                    // 第一次点击：展开文本框并获取焦点
                    // IsSavePresetExpanded属性的setter中已经发送了焦点请求消息
                    IsSavePresetExpanded = true;
                }
                else if (string.IsNullOrWhiteSpace(_newPresetName))
                {
                    // 第二次点击且文本框为空：折叠文本框
                    IsSavePresetExpanded = false;
                }
                else
                {
                    // 文本框有内容：保存预设
                    var newPreset = new EqualizerPreset
                    {
                        PresetName = _newPresetName.Trim(),
                        BandGain0 = _bandGains[0],
                        BandGain1 = _bandGains[1],
                        BandGain2 = _bandGains[2],
                        BandGain3 = _bandGains[3],
                        BandGain4 = _bandGains[4],
                        BandGain5 = _bandGains[5],
                        BandGain6 = _bandGains[6],
                        BandGain7 = _bandGains[7],
                        BandGain8 = _bandGains[8],
                        BandGain9 = _bandGains[9]
                    };

                    // 保存到数据库
                    _presetRepository.SavePreset(newPreset);

                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 保存预设 '{_newPresetName}' 成功");

                    // 清空文本框并折叠
                    NewPresetName = string.Empty;
                    IsSavePresetExpanded = false;

                    // 重新加载预设列表
                    LoadEqualizerPresets();
                    OnPropertyChanged(nameof(EqualizerPresets));

                    // 切换到新保存的预设
                    CurrentEqualizerPresetName = newPreset.PresetName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 保存预设失败: {ex.Message}");
            }
        }

        public SoundSettingsViewModel(IConfigurationService configurationService, IMessagingService messagingService, IEqualizerService equalizerService, IEqualizerPresetRepository presetRepository)
        {
            _configurationService = configurationService;
            _messagingService = messagingService;
            _equalizerService = equalizerService;
            _presetRepository = presetRepository;

            // 命令由[RelayCommand]特性自动生成

            // 加载设置
            LoadSettings();

            // 通知属性更改，确保UI正确初始化
            OnPropertyChanged(nameof(CurrentAudioEngine));
            OnPropertyChanged(nameof(AudioEngineOptions));
            OnPropertyChanged(nameof(IsEqualizerEnabled));
            OnPropertyChanged(nameof(CurrentEqualizerPresetName));
            OnPropertyChanged(nameof(EqualizerPresetNames));
            OnPropertyChanged(nameof(EqualizerPresets));
            OnPropertyChanged(nameof(BandGains));
        }

        /// <summary>
        /// 加载均衡器预设列表
        /// </summary>
        private void LoadEqualizerPresets()
        {
            try
            {
                var presets = _equalizerService.GetAvailablePresets();
                _equalizerPresetNames = presets.Select(p => p.PresetName).ToList();
                OnPropertyChanged(nameof(EqualizerPresetNames));
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 加载了 {_equalizerPresetNames.Count} 个均衡器预设");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 加载均衡器预设失败: {ex.Message}");
                _equalizerPresetNames = new List<string> { "平衡" }; // 至少提供默认选项
            }
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // 加载音频引擎设置
                _currentAudioEngine = _configurationService.CurrentConfiguration.AudioEngine;
                OnPropertyChanged(nameof(CurrentAudioEngine));

                // 加载均衡器设置
                _isEqualizerEnabled = _configurationService.CurrentConfiguration.IsEqualizerEnabled;
                OnPropertyChanged(nameof(IsEqualizerEnabled));

                _currentEqualizerPresetName = _configurationService.CurrentConfiguration.EqualizerPresetName;
                OnPropertyChanged(nameof(CurrentEqualizerPresetName));

                // 首先尝试加载预设
                try
                {
                    var presets = _equalizerService.GetAvailablePresets();
                    var currentPreset = presets.FirstOrDefault(p => p.PresetName == _currentEqualizerPresetName);

                    if (currentPreset != null)
                    {
                        // 应用预设到均衡器服务
                        _equalizerService.ApplyPreset(currentPreset);
                        System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 已应用预设 '{_currentEqualizerPresetName}'");

                        // 更新本地频段增益值
                        for (int i = 0; i < 10; i++)
                        {
                            _bandGains[i] = currentPreset.GetBandGain(i);
                        }
                    }
                    else
                    {
                        // 如果找不到预设，则从配置中加载频段增益值
                        if (_configurationService.CurrentConfiguration.EqualizerGains != null &&
                            _configurationService.CurrentConfiguration.EqualizerGains.Length == 10)
                        {
                            Array.Copy(_configurationService.CurrentConfiguration.EqualizerGains, _bandGains, 10);
                        }
                        else
                        {
                            // 如果都没有，则使用默认值（所有频段增益为0）
                            for (int i = 0; i < 10; i++)
                            {
                                _bandGains[i] = 0f;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 加载预设失败，使用配置中的增益值: {ex.Message}");

                    // 如果加载预设失败，则从配置中加载频段增益值
                    if (_configurationService.CurrentConfiguration.EqualizerGains != null &&
                        _configurationService.CurrentConfiguration.EqualizerGains.Length == 10)
                    {
                        Array.Copy(_configurationService.CurrentConfiguration.EqualizerGains, _bandGains, 10);
                    }
                }

                // 通知UI更新
                OnPropertyChanged(nameof(BandGains));
                for (int i = 0; i < 10; i++)
                {
                    OnPropertyChanged($"BandGain{i}");
                }

                // 同步到均衡器服务
                _equalizerService.IsEnabled = _isEqualizerEnabled;
                _equalizerService.PresetName = _currentEqualizerPresetName;

                // 如果预设加载失败，手动设置频段增益
                try
                {
                    var currentPreset = _equalizerService.GetAvailablePresets().FirstOrDefault(p => p.PresetName == _currentEqualizerPresetName);
                    if (currentPreset == null)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            _equalizerService.SetBandGain(i, _bandGains[i]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 同步频段增益到均衡器服务失败: {ex.Message}");
                }

                // 输出当前所有频段的增益值，用于调试
                System.Diagnostics.Debug.WriteLine("SoundSettingsViewModel: 加载设置后的均衡器频段增益值:");
                for (int i = 0; i < 10; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"  频段{i}: {_bandGains[i]:F1}dB");
                }

                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 加载均衡器设置，启用={_isEqualizerEnabled}，预设={_currentEqualizerPresetName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 加载设置失败: {ex.Message}");
            }
        }



        /// <summary>
        /// 保存音频引擎设置
        /// </summary>
        private void SaveAudioEngineSetting(AudioEngine audioEngine)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.SaveAudioEngineSetting: 正在保存AudioEngine={audioEngine}");
                _configurationService.UpdateAudioEngine(audioEngine);
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.SaveAudioEngineSetting: AudioEngine={audioEngine} 保存完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel.SaveAudioEngineSetting: 保存失败，异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送音频引擎变更消息
        /// </summary>
        private void SendAudioEngineChangedMessage(AudioEngine audioEngine)
        {
            try
            {
                // 发送音频引擎变更消息
                _messagingService.Send(new AudioEngineChangedMessage(audioEngine));

                // 同时发送配置变更消息
                _messagingService.Send(new ConfigurationChangedMessage(_configurationService.CurrentConfiguration));
            }
            catch (Exception)
            {
                // 静默处理异常
            }
        }

       
        /// <summary>
        /// 保存均衡器设置
        /// </summary>
        private void SaveEqualizerSettings()
        {
            try
            {
                _equalizerService.SaveToConfigurationService(_configurationService);
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 保存均衡器设置，启用={_isEqualizerEnabled}，预设={_currentEqualizerPresetName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoundSettingsViewModel: 保存均衡器设置时发生错误: {ex.Message}");
            }
        }





        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 清理资源
                _disposed = true;
            }
        }
    }
}