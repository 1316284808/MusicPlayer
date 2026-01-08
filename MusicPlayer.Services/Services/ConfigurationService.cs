using System;
using System.IO;
using System.Text.Json;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 配置服务实现 - 统一的配置管理中心
    /// 负责所有应用程序配置的集中管理
    /// 使用SQLite进行配置持久化
    /// </summary>
    public class ConfigurationService : IConfigurationService, IDisposable
    {
        private PlayerConfiguration _currentConfiguration = new PlayerConfiguration();
        private bool _isModified = false;
        private bool _disposed = false;
        private readonly object _lockObject = new();
        private readonly ConfigurationDAL _configurationDal;
        private IPlayerStateService? _playerStateService;
        
        // 自动保存相关字段
        private bool _autoSaveEnabled = false;
        private int _autoSaveIntervalMs = 5000; // 默认5秒自动保存
        private System.Threading.Timer? _autoSaveTimer;
        
        // 事件
        public event Action<PlayerConfiguration>? ConfigurationChanged;
        public event Action<PlayerConfiguration>? ConfigurationLoaded;
        public event Action<PlayerConfiguration>? ConfigurationSaved;

        public PlayerConfiguration CurrentConfiguration 
        { 
            get => _currentConfiguration; 
            private set
            {
                _currentConfiguration = value;
                ConfigurationChanged?.Invoke(_currentConfiguration);
            }
        }

        public ConfigurationService(IPlayerStateService? playerStateService = null)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigurationService: 创建新实例，ID: {GetHashCode()}, 线程ID: {Thread.CurrentThread.ManagedThreadId}");

            // 设置PlayerStateService引用（可能为null，稍后通过SetPlayerStateService方法设置）
            _playerStateService = playerStateService;

            // 使用已定义的数据库路径
            _configurationDal = new ConfigurationDAL(Paths.AppSettingPath);
            
            // 保存PlayerStateService引用，用于同步状态
            _playerStateService = playerStateService;

            // 加载配置
            CurrentConfiguration = LoadConfiguration();
            
            // 如果提供了PlayerStateService，将配置恢复到PlayerState
            if (_playerStateService != null)
            {
                RestorePlayerState(_playerStateService);
            }
        }

        public PlayerConfiguration LoadConfiguration()
        {
            PlayerConfiguration config;
            try
            {
                // 从SQLite数据库加载配置
                config = _configurationDal.LoadConfiguration();
                
                if (config == null || !config.IsValid())
                {
                    // 配置无效，使用默认值
                    config = new PlayerConfiguration();
                    config.ApplyDefaults();
                    SaveConfiguration(config);
                    System.Diagnostics.Debug.WriteLine("ConfigurationService: 使用默认配置");
                }
            }
            catch (Exception ex)
            {
                // 加载失败，使用默认配置
                System.Diagnostics.Debug.WriteLine($"从SQLite加载配置失败: {ex.Message}");
                config = new PlayerConfiguration();
                config.ApplyDefaults();
                SaveConfiguration(config);
            }
            
            // 触发配置加载事件
            ConfigurationLoaded?.Invoke(config);
            
            return config;
        }

        public void SaveConfiguration(PlayerConfiguration configuration)
        {
            try
            {
                // 使用SQLite保存配置
                _configurationDal.SaveConfiguration(configuration);
                
                CurrentConfiguration = configuration;
                _isModified = false;
                
                // 触发配置保存事件
                //ConfigurationSaved?.Invoke(configuration);
                
                System.Diagnostics.Debug.WriteLine("配置已保存到SQLite数据库");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置到SQLite失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 自动持久化配置
        /// </summary>
        public bool AutoSaveEnabled 
        { 
            get => _autoSaveEnabled;
            set
            {
                if (_autoSaveEnabled != value)
                {
                    _autoSaveEnabled = value;
                    SetupAutoSaveTimer();
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 自动保存已{(_autoSaveEnabled ? "启用" : "禁用")}");
                }
            }
        }
        
        /// <summary>
        /// 自动保存间隔（毫秒）
        /// </summary>
        public int AutoSaveIntervalMs 
        { 
            get => _autoSaveIntervalMs;
            set
            {
                if (_autoSaveIntervalMs != value && value > 0)
                {
                    _autoSaveIntervalMs = value;
                    SetupAutoSaveTimer();
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 自动保存间隔已设置为 {_autoSaveIntervalMs}ms");
                }
            }
        }
        
        /// <summary>
        /// 设置自动保存定时器
        /// </summary>
        private void SetupAutoSaveTimer()
        {
            lock (_lockObject)
            {
                // 停止现有定时器
                _autoSaveTimer?.Dispose();
                _autoSaveTimer = null;
                
                // 如果启用了自动保存，启动新的定时器
                if (_autoSaveEnabled)
                {
                    _autoSaveTimer = new System.Threading.Timer(AutoSaveCallback, null, _autoSaveIntervalMs, _autoSaveIntervalMs);
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 自动保存定时器已启动，间隔: {_autoSaveIntervalMs}ms");
                }
            }
        }
        
        /// <summary>
        /// 自动保存回调
        /// </summary>
        private void AutoSaveCallback(object? state)
        {
            try
            {
                if (_isModified)
                {
                    SaveCurrentConfiguration();
                    System.Diagnostics.Debug.WriteLine("ConfigurationService: 自动保存完成");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService: 自动保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存当前配置 - 先同步PlayerState状态，然后持久化
        /// </summary>
        public void SaveCurrentConfiguration()
        {
            try
            {
                // 先将PlayerStateService的最新状态同步到CurrentConfiguration
                if (_playerStateService != null)
                {
                    SyncStateToConfiguration(_playerStateService);
                    System.Diagnostics.Debug.WriteLine("ConfigurationService: 已同步 PlayerState 状态到 CurrentConfiguration");
                }
                
                // 然后保存CurrentConfiguration
                SaveConfiguration(CurrentConfiguration);
                
                System.Diagnostics.Debug.WriteLine("ConfigurationService: CurrentConfiguration 已保存到数据库");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService: 保存 CurrentConfiguration 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新音量配置
        /// </summary>
        public void UpdateVolume(float volume)
        {
            if (Math.Abs(CurrentConfiguration.Volume - volume) > 0.01f)
            {
                CurrentConfiguration.Volume = Math.Clamp(volume, 0f, 1f);
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                
                // 如果启用自动保存，启动延迟保存
                if (!_autoSaveEnabled)
                {
                    // 传统的延迟保存方式
                    StartDelayedSave();
                }
                // 自动保存模式下的保存由定时器处理
            }
        }

        /// <summary>
        /// 更新播放模式配置
        /// </summary>
        public void UpdatePlayMode(PlayMode playMode)
        {
            if (CurrentConfiguration.PlayMode != playMode)
            {
                CurrentConfiguration.PlayMode = playMode;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                // 移除立即保存，改为只在应用关闭时保存
                // SaveCurrentConfiguration(); // 注释掉自动保存
            }
        }

        /// <summary>
        /// 更新播放进度配置
        /// </summary>
        public void UpdatePosition(double position, string? currentSongPath)
        {
            if (Math.Abs(CurrentConfiguration.CurrentPosition - position) > 1.0 || 
                CurrentConfiguration.CurrentSongPath != currentSongPath)
            {
                CurrentConfiguration.CurrentPosition = Math.Max(0, position);
                CurrentConfiguration.CurrentSongPath = currentSongPath;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                // 播放进度频繁变化，延迟保存
                StartDelayedSave();
            }
        }

        /// <summary>
        /// 更新排序规则配置
        /// </summary>
        public void UpdateSortRule(SortRule sortRule)
        {
            if (CurrentConfiguration.SortRule != sortRule)
            {
                CurrentConfiguration.SortRule = sortRule;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                // 移除立即保存，改为只在应用关闭时保存
                // SaveCurrentConfiguration(); // 注释掉自动保存
            }
        }

        /// <summary>
        /// 更新播放列表折叠状态
        /// </summary>
        public void UpdatePlaylistCollapsed(bool isCollapsed)
        {
            if (CurrentConfiguration.IsPlaylistCollapsed != isCollapsed)
            {
                CurrentConfiguration.IsPlaylistCollapsed = isCollapsed;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                // 移除立即保存，改为只在应用关闭时保存
                // SaveCurrentConfiguration(); // 注释掉自动保存
            }
        }

        /// <summary>
        /// 更新频谱显示状态
        /// </summary>
        public void UpdateSpectrumEnabled(bool isEnabled)
        {
            if (CurrentConfiguration.IsSpectrumEnabled != isEnabled)
            {
                CurrentConfiguration.IsSpectrumEnabled = isEnabled;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                // 移除立即保存，改为只在应用关闭时保存
                // SaveCurrentConfiguration(); // 注释掉自动保存
            }
        }

        /// <summary>
        /// 更新关闭主窗口行为
        /// </summary>
        public void UpdateCloseBehavior(bool closeBehavior)
        {
            if (CurrentConfiguration.CloseBehavior != closeBehavior)
            {
                CurrentConfiguration.CloseBehavior = closeBehavior;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                // 移除立即保存，改为只在应用关闭时保存
                // SaveCurrentConfiguration(); // 注释掉自动保存
            }
        }









        /// <summary>
        /// 更新主题
        /// </summary>
        public void UpdateTheme(Theme theme)
        {
            if (CurrentConfiguration.Theme != theme)
            {
                CurrentConfiguration.Theme = theme;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                // 移除立即保存，改为只在应用关闭时保存
                // SaveCurrentConfiguration(); // 注释掉自动保存
            }
        }

        /// <summary>
        /// 更新语言
        /// </summary>
        public void UpdateLanguage(string language)
        {
            if (CurrentConfiguration.Language != language)
            {
                CurrentConfiguration.Language = language;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                // 移除立即保存，改为只在应用关闭时保存
                // SaveCurrentConfiguration(); // 注释掉自动保存
            }
        }

        /// <summary>
        /// 更新音频引擎
        /// </summary>
        public void UpdateAudioEngine(AudioEngine audioEngine)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateAudioEngine: 当前值={CurrentConfiguration.AudioEngine}, 新值={audioEngine}");
            
            if (CurrentConfiguration.AudioEngine != audioEngine)
            {
                CurrentConfiguration.AudioEngine = audioEngine;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateAudioEngine: 已更新CurrentConfiguration.AudioEngine为{audioEngine}");
                
                // 同时更新PlayerStateService，确保两者保持同步
                if (_playerStateService != null && _playerStateService.CurrentAudioEngine != audioEngine)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateAudioEngine: 同时更新PlayerStateService.CurrentAudioEngine从 {_playerStateService.CurrentAudioEngine} 到 {audioEngine}");
                    _playerStateService.CurrentAudioEngine = audioEngine;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateAudioEngine: AudioEngine值未变化，不更新");
            }
        }

        /// <summary>
        /// 更新均衡器启用状态
        /// </summary>
        public void UpdateEqualizerEnabled(bool isEnabled)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerEnabled: 当前值={CurrentConfiguration.IsEqualizerEnabled}, 新值={isEnabled}");
            
            if (CurrentConfiguration.IsEqualizerEnabled != isEnabled)
            {
                CurrentConfiguration.IsEqualizerEnabled = isEnabled;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerEnabled: 已更新CurrentConfiguration.IsEqualizerEnabled为{isEnabled}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerEnabled: IsEqualizerEnabled值未变化，不更新");
            }
        }

        /// <summary>
        /// 更新均衡器预设名称
        /// </summary>
        public void UpdateEqualizerPresetName(string presetName)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerPresetName: 当前值={CurrentConfiguration.EqualizerPresetName}, 新值={presetName}");
            
            if (CurrentConfiguration.EqualizerPresetName != presetName)
            {
                CurrentConfiguration.EqualizerPresetName = presetName ?? "平衡";
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerPresetName: 已更新CurrentConfiguration.EqualizerPresetName为{CurrentConfiguration.EqualizerPresetName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerPresetName: EqualizerPresetName值未变化，不更新");
            }
        }

        /// <summary>
        /// 更新均衡器增益值
        /// </summary>
        public void UpdateEqualizerGains(float[] gains)
        {
            if (gains == null || gains.Length != 10)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerGains: 增益数组无效，长度={gains?.Length ?? 0}");
                return;
            }

            bool changed = false;
            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(CurrentConfiguration.EqualizerGains[i] - gains[i]) > 0.01f)
                {
                    CurrentConfiguration.EqualizerGains[i] = gains[i];
                    changed = true;
                }
            }

            if (changed)
            {
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerGains: 已更新均衡器增益值");
                for (int i = 0; i < 10; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"  频段{i}: {gains[i]:F1}dB");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerGains: 均衡器增益值未变化，不更新");
            }
        }

        /// <summary>
        /// 更新单个均衡器频段增益值
        /// </summary>
        public void UpdateEqualizerBandGain(int bandIndex, float gain)
        {
            if (bandIndex < 0 || bandIndex >= 10)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerBandGain: 频段索引无效，索引={bandIndex}");
                return;
            }

            // 限制增益范围
            gain = Math.Clamp(gain, -12f, 12f);

            if (Math.Abs(CurrentConfiguration.EqualizerGains[bandIndex] - gain) > 0.1f)
            {
                CurrentConfiguration.EqualizerGains[bandIndex] = gain;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerBandGain: 已更新频段{bandIndex}增益值为{gain}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateEqualizerBandGain: 频段{bandIndex}增益值未变化，不更新");
            }
        }

        /// <summary>
        /// 更新最后播放的歌曲ID
        /// </summary>
        public void UpdateLastPlayedSongId(int songId)
        {
            if (CurrentConfiguration.LastPlayedSongId != songId)
            {
                CurrentConfiguration.LastPlayedSongId = songId;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateLastPlayedSongId: 已更新最后播放歌曲ID为{songId}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateLastPlayedSongId: 最后播放歌曲ID未变化，不更新");
            }
        }

        /// <summary>
        /// 更新歌词字体大小配置
        /// </summary>
        public void UpdateLyricFontSize(double fontSize)
        {
            if (Math.Abs(CurrentConfiguration.LyricFontSize - fontSize) > 0.1)
            {
                CurrentConfiguration.LyricFontSize = fontSize;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateLyricFontSize: 已更新歌词字体大小为{fontSize}");
            }
        }

        /// <summary>
        /// 更新歌词文本对齐方式配置
        /// </summary>
        public void UpdateLyricTextAlignment(System.Windows.TextAlignment textAlignment)
        {
            if (CurrentConfiguration.LyricTextAlignment != textAlignment)
            {
                CurrentConfiguration.LyricTextAlignment = textAlignment;
                CurrentConfiguration.LastSaved = DateTime.Now;
                _isModified = true;
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.UpdateLyricTextAlignment: 已更新歌词文本对齐方式为{textAlignment}");
            }
        }

        /// <summary>
        /// 保存所有配置
        /// </summary>
        public void SaveAll()
        {
            SaveCurrentConfiguration();
        }

        private System.Threading.Timer? _saveTimer;

        private void StartDelayedSave()
        {
            lock (_lockObject)
            {
                if (_disposed) return;

                _saveTimer?.Dispose();
                _saveTimer = new System.Threading.Timer(_ =>
                {
                    try
                    {
                        SaveCurrentConfiguration();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"延迟保存配置失败: {ex.Message}");
                    }
                }, null, 2000, System.Threading.Timeout.Infinite); // 2秒后保存
            }
        }
        
        /// <summary>
        /// 同步PlayerState状态到配置（内存同步，不持久化）
        /// 使用批量更新方式，避免多次触发属性变更事件
        /// </summary>
        public void SyncStateToConfiguration(IPlayerStateService playerState)
        {
            if (playerState == null)
            {
                System.Diagnostics.Debug.WriteLine("ConfigurationService: SyncStateToConfiguration - playerState为null，无法同步");
                return;
            }
            
            try
            {
                // 使用批量更新方式同步状态，减少事件触发
                var oldVolume = CurrentConfiguration.Volume;
                var oldPlayMode = CurrentConfiguration.PlayMode;
                var oldCurrentPosition = CurrentConfiguration.CurrentPosition;
                var oldCurrentSongPath = CurrentConfiguration.CurrentSongPath;
                var oldAudioEngine = CurrentConfiguration.AudioEngine;
                var oldLastPlayedSongId = CurrentConfiguration.LastPlayedSongId;
                
                // 将PlayerState的状态同步到内存中的CurrentConfiguration
                CurrentConfiguration.Volume = playerState.Volume;
                CurrentConfiguration.PlayMode = playerState.CurrentPlayMode;
                CurrentConfiguration.CurrentPosition = playerState.CurrentPosition;
                CurrentConfiguration.CurrentSongPath = playerState.CurrentSong?.FilePath;
                
                // 记录AudioEngine同步前的值
           
                CurrentConfiguration.AudioEngine = playerState.CurrentAudioEngine;
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.SyncStateToConfiguration: AudioEngine从 {oldAudioEngine} 同步到 {playerState.CurrentAudioEngine} (来自PlayerStateService)");
                
                // 同步最后播放的歌曲ID
                // 只有当PlayerState中的歌曲ID确实不同时才更新
                var currentSongId = playerState.CurrentSong?.Id ?? -1;
                if (CurrentConfiguration.LastPlayedSongId != currentSongId)
                {
                    CurrentConfiguration.LastPlayedSongId = currentSongId;
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 最后播放歌曲ID已从 {oldLastPlayedSongId} 同步到 {CurrentConfiguration.LastPlayedSongId}");
                }
                
                // 更新最后修改时间
                CurrentConfiguration.LastSaved = DateTime.Now;
                
                // 记录状态变化
                if (Math.Abs(oldVolume - CurrentConfiguration.Volume) > 0.001f)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 音量已从 {oldVolume} 同步到 {CurrentConfiguration.Volume}");
                }
                
                if (oldPlayMode != CurrentConfiguration.PlayMode)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 播放模式已从 {oldPlayMode} 同步到 {CurrentConfiguration.PlayMode}");
                }
                
                if (Math.Abs(oldCurrentPosition - CurrentConfiguration.CurrentPosition) > 1.0)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 播放位置已从 {oldCurrentPosition} 同步到 {CurrentConfiguration.CurrentPosition}");
                }
                
                if (oldCurrentSongPath != CurrentConfiguration.CurrentSongPath)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 当前歌曲已从 '{oldCurrentSongPath}' 同步到 '{CurrentConfiguration.CurrentSongPath}'");
                }
                
                if (oldAudioEngine != CurrentConfiguration.AudioEngine)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 音频引擎已从 {oldAudioEngine} 同步到 {CurrentConfiguration.AudioEngine}");
                }
                
                if (oldLastPlayedSongId != CurrentConfiguration.LastPlayedSongId)
                {
                    System.Diagnostics.Debug.WriteLine($"ConfigurationService: 最后播放歌曲ID已从 {oldLastPlayedSongId} 同步到 {CurrentConfiguration.LastPlayedSongId}");
                }
                
                System.Diagnostics.Debug.WriteLine($"ConfigurationService: 状态已同步到内存配置，播放模式: {CurrentConfiguration.PlayMode}, 音量: {CurrentConfiguration.Volume}, 音频引擎: {CurrentConfiguration.AudioEngine}, 最后播放歌曲ID: {CurrentConfiguration.LastPlayedSongId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService: 同步状态到配置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从配置恢复到PlayerState（应用启动时调用）
        /// </summary>
        /// <summary>
        /// 设置PlayerStateService引用（用于解决循环依赖）
        /// </summary>
        /// <param name="playerStateService">PlayerStateService实例</param>
        public void SetPlayerStateService(IPlayerStateService playerStateService)
        {
            _playerStateService = playerStateService;
            System.Diagnostics.Debug.WriteLine($"ConfigurationService: 设置PlayerStateService引用，ID: {_playerStateService?.GetHashCode()}");
        }

        public void RestorePlayerState(IPlayerStateService playerState)
        {
            if (playerState == null)
            {
                System.Diagnostics.Debug.WriteLine("ConfigurationService: RestorePlayerState - playerState为null，无法恢复");
                return;
            }
            
            try
            {
                var config = CurrentConfiguration;
                if (config != null)
                {
                    // 将配置恢复到PlayerState
                    playerState.RestoreFromConfiguration(config);
                    System.Diagnostics.Debug.WriteLine("ConfigurationService: 配置已恢复到PlayerState");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService: 恢复PlayerState失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放配置服务资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 释放所有Timer资源
                _saveTimer?.Dispose();
                _saveTimer = null;

                _autoSaveTimer?.Dispose();
                _autoSaveTimer = null;

                // 释放ConfigurationDAL（包含LiteDB数据库连接）
                _configurationDal?.Dispose();

                // 清理事件处理器
                ConfigurationChanged = null;
                ConfigurationLoaded = null;
                ConfigurationSaved = null;

                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~ConfigurationService()
        {
            Dispose(false);
        }
    }
}