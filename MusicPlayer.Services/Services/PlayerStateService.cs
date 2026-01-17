using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services.Messages;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 播放状态管理服务 - 作为播放状态的唯一可信源
    /// 集中管理歌曲、播放进度、音量等状态，避免数据重复和状态不一致
    /// </summary>
    public class PlayerStateService : IPlayerStateService, IStateUpdater, IDisposable
    {
        private readonly IConfigurationService _configurationService;
        private readonly IMessagingService _messagingService;
        private readonly IPlaybackContextService _playbackContextService;
        private readonly object _lockObject = new();
        private bool _disposed = false;
        private bool _isRestoringFromPersistence = false; // 标记是否正在从持久化恢复状态
        
        // 批量更新相关字段
        private bool _isBatchUpdate = false;
        private readonly Dictionary<string, object> _pendingUpdates = new();
        private readonly HashSet<string> _changedProperties = new();
        
        // 播放状态
        private double _currentPosition;
        private double _maxPosition;
        private float _volume = 0.5f;
        private bool _isPlaying;
        private bool _isMuted;
        private PlayMode _currentPlayMode = PlayMode.RepeatAll;
        private AudioEngine _currentAudioEngine = AudioEngine.Auto;
        private float[] _spectrumData = new float[32];
        private Song? _currentSong;
        private DateTime _lastSpectrumMessageTime = DateTime.MinValue;
        private PlaybackContext _currentPlaybackContext;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<string, object>? StateChanged;

        // 播放状态属性
        public Song? CurrentSong
        {
            get => _currentSong;
            private set
            {
                if (_currentSong != value)
                {
                    _currentSong = value;
                   OnPropertyChanged();
                    // 注意：不再发送CurrentSongChangedMessage，避免循环
                    // 消息应该由PlaylistDataService发送，这里是消费者
                    // _messagingService.Send(new CurrentSongChangedMessage(value));
                 }
            }
        }
        
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged();
                    _messagingService.Send(new PlaybackStateChangedMessage(value));
                }
            }
        }
        
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    OnPropertyChanged();
                    _messagingService.Send(new MuteStateChangedMessage(value));
                }
            }
        }
        
        public float Volume
        {
            get => _volume;
            set
            {
                if (Math.Abs(_volume - value) > 0.001f)
                {
                    _volume = Math.Clamp(value, 0.0f, 1.0f);
                   OnPropertyChanged();
                    
                    // 非恢复状态时才发送消息和保存配置
                    if (!_isRestoringFromPersistence)
                    {
                        _messagingService.Send(new VolumeChangedMessage(_volume));
                        // 注释掉直接保存配置，改为在应用退出时同步保存
                        // _configurationService?.UpdateVolume(_volume);
                    }
                }
            }
        }
        
        private bool _isUserDragging = false;
        
        public double CurrentPosition
        {
            get => _currentPosition;
            set
            {
                // 在用户拖动时，我们需要确保新值被接受，即使差异很小
                var oldValue = _currentPosition;
                _currentPosition = Math.Clamp(value, 0.0, _maxPosition);
                
                // 只有当值确实发生了变化时才通知
                if (Math.Abs(oldValue - _currentPosition) > 0.001)
                {
                    OnPropertyChanged();
                    _messagingService.Send(new PlaybackProgressChangedMessage(_currentPosition));
                }
            }
        }
        
        /// <summary>
        /// 设置用户拖动状态
        /// </summary>
        public void SetUserDragging(bool isDragging)
        {
            _isUserDragging = isDragging;
         }
        
        /// <summary>
        /// 用户拖动时设置位置
        /// </summary>
        public void SetPositionByUser(double position)
        {
            var oldValue = _currentPosition;
            _currentPosition = Math.Clamp(position, 0.0, _maxPosition);
           // 立即通知UI更新
            OnPropertyChanged();
            _messagingService.Send(new PlaybackProgressChangedMessage(_currentPosition));
        }
        
        public double MaxPosition
        {
            get => _maxPosition;
            set
            {
                if (Math.Abs(_maxPosition - value) > 0.1)
                {
                    _maxPosition = Math.Max(0, value);
                    OnPropertyChanged();
                    _messagingService.Send(new MaxPositionChangedMessage(_maxPosition));
                }
            }
        }
        
        public PlayMode CurrentPlayMode
        {
            get => _currentPlayMode;
            set
            {
                if (_currentPlayMode != value)
                {
                    _currentPlayMode = value;
                    OnPropertyChanged();
                    
                    // 非恢复状态时才发送消息
                    if (!_isRestoringFromPersistence)
                    {
                        _messagingService.Send(new PlayModeChangedMessage(_currentPlayMode));
                    }
                }
            }
        }
        
        public AudioEngine CurrentAudioEngine
        {
            get => _currentAudioEngine;
            set
            {
                if (_currentAudioEngine != value)
                {
                    var oldEngine = _currentAudioEngine;
                    _currentAudioEngine = value;
                    OnPropertyChanged();
                    StateChanged?.Invoke(nameof(CurrentAudioEngine), _currentAudioEngine);
                    
                    // 非恢复状态时才执行特殊逻辑
                    if (!_isRestoringFromPersistence)
                    {
                        System.Diagnostics.Debug.WriteLine($"PlayerStateService: 音频引擎从 {oldEngine} 切换到 {_currentAudioEngine}");
                        // 可以在这里添加音频引擎变更的消息处理
                    }
                }
            }
        }
        
        public PlaybackContext CurrentPlaybackContext
        {
            get => _currentPlaybackContext;
            set
            {
                if (_currentPlaybackContext != value)
                {
                    _currentPlaybackContext = value ?? PlaybackContext.CreateDefault();
                    OnPropertyChanged();
                    StateChanged?.Invoke(nameof(CurrentPlaybackContext), _currentPlaybackContext);
                    System.Diagnostics.Debug.WriteLine($"PlayerStateService: 播放上下文更新为 {_currentPlaybackContext}");
                }
            }
        }
        
        public float[] SpectrumData
        {
            get => _spectrumData;
            private set
            {//如果频谱禁用，就发送空数组1
                if (_configurationService.CurrentConfiguration.IsSpectrumEnabled)
                {

                    if (value != null)
                    {
                        if (value.Length != _spectrumData.Length)
                        {
                            // 调整数组大小
                            _spectrumData = new float[value.Length];
                        }

                        // 复制数据到内部数组
                        int copyLength = Math.Min(value.Length, _spectrumData.Length);
                        Array.Copy(value, _spectrumData, copyLength);

                        OnPropertyChanged();
                        var now = DateTime.Now;
                        if ((now - _lastSpectrumMessageTime).TotalMilliseconds > 100) // 每100ms最多发送一次
                        {
                            _lastSpectrumMessageTime = now;
                            // 发送内部数组的副本，而不是引用
                            var spectrumCopy = new float[copyLength];
                            Array.Copy(_spectrumData, spectrumCopy, copyLength); 
                            _messagingService.Send(new SpectrumDataUpdatedMessage(spectrumCopy));
                        }

                    }

                }
                else
                {
                    var now = DateTime.Now;
                    if ((now - _lastSpectrumMessageTime).TotalMilliseconds > 100) // 每100ms最多发送一次
                    {
                        _lastSpectrumMessageTime = now; 
                        _messagingService.Send(new SpectrumDataUpdatedMessage(new float[32]));//发送值为空的数组。
                    }

                }

            }
        }

        public PlayerStateService(
            IPlaylistDataService playlistDataService, 
            IConfigurationService configurationService, 
            IMessagingService messagingService,
            IPlaybackContextService playbackContextService)
        {
            // 添加实例ID日志，用于调试单例问题
            System.Diagnostics.Debug.WriteLine($"PlayerStateService: 创建新实例，ID: {GetHashCode()}, 线程ID: {Thread.CurrentThread.ManagedThreadId}");
            
            _configurationService = configurationService;
            _messagingService = messagingService;
            _playbackContextService = playbackContextService;
            
            // 加载配置
            LoadConfiguration();
            
            // 订阅播放列表数据服务的当前歌曲变化事件
            _messagingService.Register<CurrentSongChangedMessage>(this, OnPlaylistCurrentSongChanged);
            
            // 订阅配置变更事件
            _configurationService.ConfigurationChanged += OnConfigurationChanged;
            
            // 初始化播放上下文
            _currentPlaybackContext = _playbackContextService.CurrentPlaybackContext;
            
            // 订阅播放状态变化消息，同步状态
            _messagingService.Register<PlaybackStateChangedMessage>(this, (r, m) =>
            {
                // 避免循环引用 - 直接设置内部字段，不触发事件
                if (_isPlaying != m.Value)
                {
                    _isPlaying = m.Value;
                    OnPropertyChanged(nameof(IsPlaying));
                    // 不再发送消息，避免循环
                }
            });
            
            // 订阅播放进度变化消息，同步状态
            _messagingService.Register<PlaybackProgressChangedMessage>(this, (r, m) =>
            {
                // 如果用户正在拖动，则不自动更新播放进度
                if (_isUserDragging)
                {
                    return;
                }
                
                // 直接设置内部字段，不触发事件，避免循环
                if (Math.Abs(_currentPosition - m.Value) > 0.01)
                { 
                    _currentPosition = Math.Clamp(m.Value, 0.0, _maxPosition);
                    OnPropertyChanged(nameof(CurrentPosition));
                      
                }
            });
            
            // 订阅最大位置变化消息，同步状态
            _messagingService.Register<MaxPositionChangedMessage>(this, (r, m) =>
            {
                // 直接设置内部字段，不触发事件，避免循环
                _maxPosition = Math.Max(0, m.Value);
                OnPropertyChanged(nameof(MaxPosition));
               
            });
        }

        private void LoadConfiguration()
        {
            var config = _configurationService.LoadConfiguration();
            if (config != null)
            {
                // 使用新的 RestoreFromConfiguration 方法
                RestoreFromConfiguration(config);
            }
            else
            {
                // 如果配置加载失败，使用默认值
                RestoreFromConfiguration(new PlayerConfiguration());
            }
        }

        // 添加一个标志位，用于标识是否正在切歌
        private bool _isSongChanging = false;
        
        private void OnConfigurationChanged(PlayerConfiguration configuration)
        {
            // 只有在不是切歌过程中才应用配置文件中的音量值
            if (!_isSongChanging)
            {
                Volume = configuration.Volume;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"PlayerStateService: 切歌中，跳过配置文件中的音量值 {configuration.Volume}，保持当前音量 {_volume}");
            }
            CurrentPlayMode = configuration.PlayMode;
        }

        private void OnPlaylistCurrentSongChanged(object recipient, CurrentSongChangedMessage message)
        {
            // 避免循环 - 直接设置内部字段，不通过属性
            if (_currentSong != message.Value)
            {
                _currentSong = message.Value;
                OnPropertyChanged(nameof(CurrentSong));
                
                // 每次歌曲变化都重置播放进度到0，包括同一首歌的情况
                if (message.Value != null)
                {
                    System.Diagnostics.Debug.WriteLine($"PlayerStateService: 歌曲切换到 {message.Value.Title}，重置播放进度到0");
                    _currentPosition = 0.0;
                    OnPropertyChanged(nameof(CurrentPosition));
                    _messagingService.Send(new PlaybackProgressChangedMessage(0.0));
                }
            }
            else
            {
                // 即使是同一首歌，也重置播放进度到0
                if (message.Value != null)
                {
                    System.Diagnostics.Debug.WriteLine($"PlayerStateService: 重新加载歌曲 {message.Value.Title}，重置播放进度到0");
                    _currentPosition = 0.0;
                    OnPropertyChanged(nameof(CurrentPosition));
                    _messagingService.Send(new PlaybackProgressChangedMessage(0.0));
                }
            }
            
            // 切歌完成后，清除切歌标志位
            if (_isSongChanging)
            {
                System.Diagnostics.Debug.WriteLine($"PlayerStateService: 切歌完成，清除切歌标志位");
                _isSongChanging = false;
            }
        }

        public void UpdateCurrentSong(Song? song)
        {
            ThrowIfDisposed();
            CurrentSong = song;
        }
        
        public void UpdateSpectrumData(float[] spectrumData)
        {
            ThrowIfDisposed();
            SpectrumData = spectrumData;
        }
        


        /// <summary>
        /// 切换播放/暂停状态
        /// </summary>
        public void TogglePlayPause()
        {
            ThrowIfDisposed();
            IsPlaying = !IsPlaying;
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            ThrowIfDisposed();
            IsPlaying = false;
            CurrentPosition = 0.0;
        }

        /// <summary>
        /// 设置切歌状态
        /// </summary>
        public void SetSongChanging(bool isChanging)
        {
            _isSongChanging = isChanging;
            System.Diagnostics.Debug.WriteLine($"PlayerStateService: 设置切歌状态为 {_isSongChanging}");
        }

        /// <summary>
        /// 切换播放模式
        /// </summary>
        public void TogglePlayMode()
        {
            ThrowIfDisposed();
            var nextMode = CurrentPlayMode switch
            {
                PlayMode.RepeatOne => PlayMode.RepeatAll,
                PlayMode.RepeatAll => PlayMode.Shuffle,
                PlayMode.Shuffle => PlayMode.RepeatOne,
                _ => PlayMode.RepeatAll
            };
            CurrentPlayMode = nextMode;
        }

        /// <summary>
        /// 从配置恢复状态（应用启动时调用）
        /// </summary>
        public void RestoreFromConfiguration(PlayerConfiguration configuration)
        {
            ThrowIfDisposed();
            
            if (configuration == null)
            {
                System.Diagnostics.Debug.WriteLine("PlayerStateService: RestoreFromConfiguration - 配置为null，使用默认值");
                configuration = new PlayerConfiguration();
            }
            
            _isRestoringFromPersistence = true;
            try
            {
                // 直接设置内部字段，不触发事件
                _volume = Math.Clamp(configuration.Volume, 0f, 1f);
                _currentPlayMode = configuration.PlayMode;
                _currentAudioEngine = configuration.AudioEngine;
                
                // 确保播放模式在有效范围内
                if (!Enum.IsDefined(typeof(PlayMode), _currentPlayMode))
                {
                    System.Diagnostics.Debug.WriteLine($"PlayerStateService: 无效的播放模式 {_currentPlayMode}，使用默认模式 RepeatAll");
                    _currentPlayMode = PlayMode.RepeatAll;
                }
                
                // 确保音频引擎在有效范围内
                if (!Enum.IsDefined(typeof(AudioEngine), _currentAudioEngine))
                {
                    System.Diagnostics.Debug.WriteLine($"PlayerStateService: 无效的音频引擎 {_currentAudioEngine}，使用默认引擎 Auto");
                    _currentAudioEngine = AudioEngine.Auto;
                }
                
                _currentPosition = configuration.CurrentPosition;
                _isMuted = false; // 默认不静音，从持久化恢复时不保存静音状态
                
                // 通知UI更新，但不触发消息或持久化
                OnPropertyChanged(nameof(Volume));
                OnPropertyChanged(nameof(CurrentPlayMode));
                OnPropertyChanged(nameof(CurrentAudioEngine));
                OnPropertyChanged(nameof(CurrentPosition));
                OnPropertyChanged(nameof(IsMuted));
                
                System.Diagnostics.Debug.WriteLine($"PlayerStateService: 从配置恢复状态完成，播放模式: {_currentPlayMode}, 音量: {_volume}");
            }
            finally
            {
                _isRestoringFromPersistence = false;
            }
        }

        /// <summary>
        /// 同步状态到配置（内存同步，不持久化）
        /// </summary>
        public void SyncToConfiguration(PlayerConfiguration configuration)
        {
            ThrowIfDisposed();
            
            if (configuration == null)
            {
                System.Diagnostics.Debug.WriteLine("PlayerStateService: SyncToConfiguration - 配置为null，无法同步");
                return;
            }
            
            // 将当前状态同步到内存中的 PlayerConfiguration
            configuration.Volume = _volume;
            configuration.PlayMode = _currentPlayMode;
            configuration.AudioEngine = _currentAudioEngine;
            configuration.CurrentPosition = _currentPosition;
            configuration.CurrentSongPath = _currentSong?.FilePath;
            
            // 更新最后修改时间
            configuration.LastSaved = DateTime.Now;
            
            System.Diagnostics.Debug.WriteLine($"PlayerStateService: 状态已同步到内存配置，播放模式: {_currentPlayMode}, 音量: {_volume}");
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // 在非批量更新模式下触发StateChanged事件
            if (!_isBatchUpdate && !string.IsNullOrEmpty(propertyName))
            {
                var propertyValue = GetPropertyValue(propertyName);
                StateChanged?.Invoke(propertyName, propertyValue);
            }
            else if (_isBatchUpdate)
            {
                // 在批量更新模式下，记录已更改的属性
                _changedProperties.Add(propertyName);
            }
        }
        
        /// <summary>
        /// 获取属性值
        /// </summary>
        private object? GetPropertyValue(string propertyName)
        {
            return propertyName switch
            {
                nameof(IsPlaying) => IsPlaying,
                nameof(IsMuted) => IsMuted,
                nameof(Volume) => Volume,
                nameof(CurrentPosition) => CurrentPosition,
                nameof(MaxPosition) => MaxPosition,
                nameof(CurrentPlayMode) => CurrentPlayMode,
                nameof(CurrentAudioEngine) => CurrentAudioEngine,
                nameof(CurrentSong) => CurrentSong,
                _ => null
            };
        }

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
                // 取消订阅配置变更事件
                if (_configurationService != null)
                {
                    _configurationService.ConfigurationChanged -= OnConfigurationChanged;
                }

                // 注销所有消息处理器
                _messagingService?.Unregister(this);

                // 清理事件处理器
                PropertyChanged = null;

                _disposed = true;
            }
        }

        /// <summary>
        /// 检查是否已释放
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PlayerStateService));
            }
        }
        
        #region IStateUpdater 实现
        
        /// <summary>
        /// 批量更新状态（避免多次触发事件）
        /// </summary>
        public void UpdateStates(Action<IStateUpdater> updateAction)
        {
            ThrowIfDisposed();
            
            if (updateAction == null)
                throw new ArgumentNullException(nameof(updateAction));
                
            _isBatchUpdate = true;
            _pendingUpdates.Clear();
            _changedProperties.Clear();
            
            try
            {
                // 执行批量更新操作
                updateAction(this);
                
                // 应用所有更新
                ApplyBatchUpdates();
            }
            finally
            {
                _isBatchUpdate = false;
            }
        }
        
        /// <summary>
        /// 更新播放状态
        /// </summary>
        IStateUpdater IStateUpdater.SetPlaying(bool isPlaying)
        {
            _pendingUpdates[nameof(IsPlaying)] = isPlaying;
            return this;
        }
        
        /// <summary>
        /// 更新音量
        /// </summary>
        IStateUpdater IStateUpdater.SetVolume(float volume)
        {
            _pendingUpdates[nameof(Volume)] = Math.Clamp(volume, 0.0f, 1.0f);
            return this;
        }
        
        /// <summary>
        /// 更新静音状态
        /// </summary>
        IStateUpdater IStateUpdater.SetMuted(bool isMuted)
        {
            _pendingUpdates[nameof(IsMuted)] = isMuted;
            return this;
        }
        
        /// <summary>
        /// 更新播放位置
        /// </summary>
        IStateUpdater IStateUpdater.SetPosition(double position)
        {
            _pendingUpdates[nameof(CurrentPosition)] = Math.Clamp(position, 0.0, _maxPosition);
            return this;
        }
        
        /// <summary>
        /// 更新播放模式
        /// </summary>
        IStateUpdater IStateUpdater.SetPlayMode(PlayMode playMode)
        {
            _pendingUpdates[nameof(CurrentPlayMode)] = playMode;
            return this;
        }
        
        /// <summary>
        /// 更新最大位置
        /// </summary>
        IStateUpdater IStateUpdater.SetMaxPosition(double maxPosition)
        {
            _pendingUpdates[nameof(MaxPosition)] = Math.Max(0, maxPosition);
            return this;
        }
        
        /// <summary>
        /// 应用所有批量更新
        /// </summary>
        void IStateUpdater.Apply()
        {
            // ApplyBatchUpdates将在UpdateStates方法中调用
            // 这里不做任何操作，因为实际的应用由UpdateStates方法控制
        }
        
        /// <summary>
        /// 应用批量更新
        /// </summary>
        private void ApplyBatchUpdates()
        {
            if (_pendingUpdates.Count == 0)
                return;
                
            // 应用所有挂起的更新
            foreach (var kvp in _pendingUpdates)
            {
                switch (kvp.Key)
                {
                    case nameof(IsPlaying):
                        _isPlaying = (bool)kvp.Value;
                        _changedProperties.Add(nameof(IsPlaying));
                        if (!_isRestoringFromPersistence)
                            _messagingService.Send(new PlaybackStateChangedMessage(_isPlaying));
                        break;
                        
                    case nameof(Volume):
                        _volume = (float)kvp.Value;
                        _changedProperties.Add(nameof(Volume));
                        if (!_isRestoringFromPersistence)
                            _messagingService.Send(new VolumeChangedMessage(_volume));
                        break;
                        
                    case nameof(IsMuted):
                        _isMuted = (bool)kvp.Value;
                        _changedProperties.Add(nameof(IsMuted));
                        if (!_isRestoringFromPersistence)
                            _messagingService.Send(new MuteStateChangedMessage(_isMuted));
                        break;
                        
                    case nameof(CurrentPosition):
                        _currentPosition = (double)kvp.Value;
                        _changedProperties.Add(nameof(CurrentPosition));
                        if (!_isRestoringFromPersistence)
                            _messagingService.Send(new PlaybackProgressChangedMessage(_currentPosition));
                        break;
                        
                    case nameof(MaxPosition):
                        _maxPosition = (double)kvp.Value;
                        _changedProperties.Add(nameof(MaxPosition));
                        if (!_isRestoringFromPersistence)
                            _messagingService.Send(new MaxPositionChangedMessage(_maxPosition));
                        break;
                        
                    case nameof(CurrentPlayMode):
                        _currentPlayMode = (PlayMode)kvp.Value;
                        _changedProperties.Add(nameof(CurrentPlayMode));
                        if (!_isRestoringFromPersistence)
                            _messagingService.Send(new PlayModeChangedMessage(_currentPlayMode));
                        break;
                }
            }
            
            // 批量触发PropertyChanged事件
            if (_changedProperties.Count > 0)
            {
                foreach (var property in _changedProperties)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
                    
                    // 触发StateChanged事件
                    var propertyValue = GetPropertyValue(property);
                    if (propertyValue != null)
                    {
                        StateChanged?.Invoke(property, propertyValue);
                    }
                }
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 检查是否可以播放
        /// </summary>
        public bool CanPlay()
        {
            ThrowIfDisposed();
            return CurrentSong != null && !IsPlaying;
        }
        
        /// <summary>
        /// 检查是否可以暂停
        /// </summary>
        public bool CanPause()
        {
            ThrowIfDisposed();
            return CurrentSong != null && IsPlaying;
        }
        
        #endregion
    }
}