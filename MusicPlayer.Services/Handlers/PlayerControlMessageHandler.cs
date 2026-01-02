using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.Services.Handlers
{
    /// <summary>
    /// 播放控制消息处理器 - 处理所有播放相关的消息
    /// 作为UI层与服务层之间的唯一桥梁，集中处理播放控制逻辑
    /// 防止多个音频引擎共存，确保音频设备单例
    /// </summary>
    public class PlayerControlMessageHandler : IDisposable
    {
        private readonly IMessagingService _messagingService;
        private readonly IPlayerStateService _playerStateService;
        private readonly IPlayerService _playerService;
        private readonly IPlaylistDataService _playlistDataService;
        private bool _disposed = false;

        public PlayerControlMessageHandler(
            IMessagingService messagingService,
            IPlayerStateService playerStateService,
            IPlayerService playerService,
            IPlaylistDataService playlistDataService)
        {
            // 添加实例ID日志，用于调试单例问题
            System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: 创建新实例，ID: {GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: PlayerStateService实例ID: {playerStateService.GetHashCode()}");
            
            _messagingService = messagingService;
            _playerStateService = playerStateService;
            _playerService = playerService;
            _playlistDataService = playlistDataService;

            RegisterMessageHandlers();
        }

        /// <summary>
        /// 注册所有播放控制相关的消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 播放控制命令消息
            _messagingService.Register<PlayPauseMessage>(this, OnPlayPauseRequested);
            _messagingService.Register<StopPlaybackMessage>(this, OnStopRequested);
            _messagingService.Register<NextSongMessage>(this, OnNextSongRequested);
            _messagingService.Register<PreviousSongMessage>(this, OnPreviousSongRequested);

            // 歌曲选择消息
            _messagingService.Register<SongSelectionMessage>(this, OnSongSelectionRequested);
            _messagingService.Register<PlaySelectedSongMessage>(this, OnPlaySelectedSongRequested);

            // 音量控制消息
            _messagingService.Register<VolumeSetMessage>(this, OnVolumeSetRequested);
            _messagingService.Register<VolumeAdjustMessage>(this, OnVolumeAdjustRequested);
            _messagingService.Register<MuteToggleMessage>(this, OnMuteToggleRequested);

            // 播放模式控制消息
            _messagingService.Register<TogglePlayModeMessage>(this, OnTogglePlayModeRequested);

            // 进度控制消息
            _messagingService.Register<SeekMessage>(this, OnSeekRequested);

            // 播放器状态查询
            _messagingService.Register<PlayerStatusQueryMessage>(this, OnPlayerStatusQueryRequested);

            // 内部切换消息（避免循环依赖）
            _messagingService.Register<SwitchToNextSongMessage>(this, OnSwitchToNextSong);
            _messagingService.Register<SwitchToPreviousSongMessage>(this, OnSwitchToPreviousSong);

            // 错误处理由 SystemMessageHandler 统一处理，避免重复注册
            // _messagingService.Register<ErrorMessage>(this, OnErrorMessage);
        }

        #region 播放控制消息处理

        private void OnPlayPauseRequested(object recipient, PlayPauseMessage message)
        {
            try
            {
                // 首先通过PlayerStateService获取当前播放状态
                bool isCurrentlyPlaying = _playerStateService.IsPlaying;
                
                // 处理播放/暂停逻辑
                if (_playerStateService.CurrentSong == null)
                {
                    // 如果没有当前歌曲，尝试从播放列表获取第一首
                    var dataSource = _playlistDataService.DataSource;
                    if (dataSource.Count > 0)
                    {
                        // 加载第一首歌曲
                        var firstSong = dataSource[0];
                        _playerService.LoadSong(firstSong);
                        _playerService.StartPlayback();
                    }
                    _playlistDataService.ClearDataSource();
                }
                else if (isCurrentlyPlaying)
                {
                    // 当前正在播放，则执行暂停
                    _playerService.PausePlayback();
                }
                else
                {
                    // 当前已暂停，检查歌曲是否已加载
                    // 如果当前歌曲与PlayerService中的歌曲不同，需要先加载
                    var currentSong = _playerStateService.CurrentSong;
                    if (_playerService.CurrentSong != currentSong && currentSong != null)
                    {
                        // 加载歌曲
                        _playerService.LoadSong(currentSong);
                    }
                    
                    // 执行播放
                    _playerService.StartPlayback();
                }
                
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnPlayPauseRequested", ex);
                message.Reply(false);
            }
        }

        private void OnStopRequested(object recipient, StopPlaybackMessage message)
        {
            try
            {
                // 通过PlayerService执行实际的停止操作
                _playerService.StopPlayback();
                
                // 同时更新PlayerStateService的状态
                _playerStateService.Stop();
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnStopRequested", ex);
                message.Reply(false);
            }
        }

        private void OnNextSongRequested(object recipient, NextSongMessage message)
        {
            try
            {
                var currentPlayMode = _playerStateService.CurrentPlayMode;
                var currentVolume = _playerStateService.Volume;
                System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: 下一首请求，当前播放模式: {currentPlayMode}, 当前音量: {currentVolume}");
                
                // 设置切歌标志位，防止配置文件中的音量覆盖用户设置
                if (_playerStateService is PlayerStateService playerStateService)
                {
                    playerStateService.SetSongChanging(true);
                }
                
                // 发送音量保持消息，确保切歌时保持当前音量
                _messagingService.Send(new VolumePreserveMessage(currentVolume));
                
                // 发送内部切换消息，避免循环依赖
                _messagingService.Send(new SwitchToNextSongMessage(currentPlayMode));
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnNextSongRequested", ex);
                message.Reply(false);
            }
        }

        private void OnPreviousSongRequested(object recipient, PreviousSongMessage message)
        {
            try
            {
                var currentPlayMode = _playerStateService.CurrentPlayMode;
                var currentVolume = _playerStateService.Volume;
                System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: 上一首请求，当前播放模式: {currentPlayMode}, 当前音量: {currentVolume}");
                
                // 设置切歌标志位，防止配置文件中的音量覆盖用户设置
                if (_playerStateService is PlayerStateService playerStateService)
                {
                    playerStateService.SetSongChanging(true);
                }
                
                // 发送音量保持消息，确保切歌时保持当前音量
                _messagingService.Send(new VolumePreserveMessage(currentVolume));
                
                // 发送内部切换消息，避免循环依赖
                _messagingService.Send(new SwitchToPreviousSongMessage(currentPlayMode));
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnPreviousSongRequested", ex);
                message.Reply(false);
            }
        }

        private void OnSwitchToNextSong(object recipient, SwitchToNextSongMessage message)
        {
            try
            {
                // 添加调试信息
                System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: OnSwitchToNextSong - 播放模式: {message.PlayMode}, 当前歌曲: {_playlistDataService.CurrentSong?.Title}");
                
                // 通过PlaylistDataService获取下一首歌曲
                var nextSong = _playlistDataService.GetNextSong(message.PlayMode);
                
                System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: OnSwitchToNextSong - 下一首歌曲: {nextSong?.Title}");
                
                if (nextSong != null)
                {
                    // 设置当前歌曲会自动触发CurrentSongChangedMessage，由PlayerService处理LoadSong
                    _playlistDataService.CurrentSong = nextSong;
                    
                  
                    if (_playerStateService is PlayerStateService playerStateService)
                    {
                        playerStateService.SetPositionByUser(0);
                    }
                    
                   Task.Delay(500).ContinueWith(_ => {
                        try { _playerService.StartPlayback(); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"延迟开始播放失败: {ex.Message}"); }
                    }, TaskScheduler.Default);
                }
                else
                {
                    // 如果没有下一首歌曲，停止播放
                    System.Diagnostics.Debug.WriteLine("PlayerControlMessageHandler: OnSwitchToNextSong - 没有找到下一首歌曲，停止播放");
                    _playerService.StopPlayback();
                    _playerStateService.Stop();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: OnSwitchToNextSong - 异常: {ex.Message}");
                HandleError("OnSwitchToNextSong", ex);
            }
        }

        private void OnSwitchToPreviousSong(object recipient, SwitchToPreviousSongMessage message)
        {
            try
            {
                // 通过PlaylistDataService获取上一首歌曲
                var previousSong = _playlistDataService.GetPreviousSong(message.PlayMode);
                if (previousSong != null)
                {
                    // 设置当前歌曲会自动触发CurrentSongChangedMessage，由PlayerService处理LoadSong
                    _playlistDataService.CurrentSong = previousSong;
                 
                    
                    // 重置播放进度到0
                    if (_playerStateService is PlayerStateService playerStateService)
                    {
                        playerStateService.SetPositionByUser(0);
                    }
                    
                   Task.Delay(500).ContinueWith(_ => {
                        try { _playerService.StartPlayback(); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"延迟开始播放失败: {ex.Message}"); }
                    }, TaskScheduler.Default);
                }
                else
                {
                    // 如果没有上一首歌曲，停止播放
                    _playerService.StopPlayback();
                    _playerStateService.Stop();
                }
            }
            catch (Exception ex)
            {
                HandleError("OnSwitchToPreviousSong", ex);
            }
        }

        #endregion

        #region 歌曲选择消息处理

        private void OnSongSelectionRequested(object recipient, SongSelectionMessage message)
        {
            HandleSongSelection(message.Song, "OnSongSelectionRequested", reply => message.Reply(reply));
        }

        private void OnPlaySelectedSongRequested(object recipient, PlaySelectedSongMessage message)
        {
            HandleSongSelection(message.Value, "OnPlaySelectedSongRequested");
        }

        #endregion

        #region 歌曲选择通用处理

        /// <summary>
        /// 处理歌曲选择的通用方法
        /// 统一处理歌曲选择的逻辑，避免代码重复
        /// </summary>
        /// <param name="song">要选择的歌曲</param>
        /// <param name="methodName">调用方法名，用于日志记录</param>
        /// <param name="replyCallback">回复回调函数</param>
        private void HandleSongSelection(Song song, string methodName, Action<bool> replyCallback = null)
        {
            try
            {
                LogSongSelection(song, methodName);
                SetCurrentSong(song, methodName);
                ScheduleDelayedPlayback(methodName);
                replyCallback?.Invoke(true);
            }
            catch (Exception ex)
            {
                HandleError(methodName, ex);
                replyCallback?.Invoke(false);
            }
        }

        /// <summary>
        /// 记录歌曲选择日志
        /// </summary>
        private void LogSongSelection(Song song, string methodName)
        {
            System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: {methodName} - 歌曲: {song?.Title}");
        }

        /// <summary>
        /// 设置当前歌曲
        /// </summary>
        private void SetCurrentSong(Song song, string methodName)
        {
            _playlistDataService.CurrentSong = song;
            System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: {methodName} - 已设置CurrentSong到PlaylistDataService");
            System.Diagnostics.Debug.WriteLine($"PlayerControlMessageHandler: {methodName} - 已加载歌曲，重置播放进度到0");
        }

        /// <summary>
        /// 调度延迟播放
        /// </summary>
        private void ScheduleDelayedPlayback(string methodName)
        {
            Task.Delay(500).ContinueWith(_ =>
            {
                try
                {
                    _playerService.StartPlayback();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"{methodName} 延迟开始播放失败: {ex.Message}");
                }
            }, TaskScheduler.Default);
        }

        #endregion

        #region 音量控制消息处理

        private void OnVolumeSetRequested(object recipient, VolumeSetMessage message)
        {
            try
            {
                if (message.IsRelative)
                {
                    // 相对音量调整
                    var newVolume = _playerStateService.Volume + message.Volume;
                    _playerStateService.Volume = Math.Clamp(newVolume, 0.0f, 1.0f);
                }
                else
                {
                    // 绝对音量设置
                    _playerStateService.Volume = Math.Clamp(message.Volume, 0.0f, 1.0f);
                }
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnVolumeSetRequested", ex);
                message.Reply(false);
            }
        }

        private void OnVolumeAdjustRequested(object recipient, VolumeAdjustMessage message)
        {
            try
            {
                var newVolume = _playerStateService.Volume + message.Delta;
                newVolume = Math.Clamp(newVolume, 0.0f, 1.0f);
                _playerStateService.Volume = newVolume;
                message.Reply(newVolume);
            }
            catch (Exception ex)
            {
                HandleError("OnVolumeAdjustRequested", ex);
                message.Reply(_playerStateService.Volume);
            }
        }

        private void OnMuteToggleRequested(object recipient, MuteToggleMessage message)
        {
            try
            {
                if (message.TargetMuteState.HasValue)
                {
                    _playerStateService.IsMuted = message.TargetMuteState.Value;
                }
                else
                {
                    _playerStateService.IsMuted = !_playerStateService.IsMuted;
                }
                message.Reply(_playerStateService.IsMuted);
            }
            catch (Exception ex)
            {
                HandleError("OnMuteToggleRequested", ex);
                message.Reply(_playerStateService.IsMuted);
            }
        }

        #endregion

        #region 播放模式消息处理

        private void OnTogglePlayModeRequested(object recipient, TogglePlayModeMessage message)
        {
            try
            {
                _playerStateService.TogglePlayMode();
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnTogglePlayModeRequested", ex);
                message.Reply(false);
            }
        }

        #endregion

        #region 进度控制消息处理

        private void OnSeekRequested(object recipient, SeekMessage message)
        {
            try
            {
                // 通过PlayerService执行实际的Seek操作
                // PlayerService.SeekToPosition 已经负责更新 PlayerStateService
                _playerService.SeekToPosition(message.Position);
                
                message.Reply(message.Position);
            }
            catch (Exception ex)
            {
                HandleError("OnSeekRequested", ex);
                message.Reply(_playerStateService.CurrentPosition);
            }
        }

        #endregion

        #region 状态查询消息处理

        private void OnPlayerStatusQueryRequested(object recipient, PlayerStatusQueryMessage message)
        {
            try
            {
                var statusResponse = new PlayerStatusResponse
                {
                    IsPlaying = _playerStateService.IsPlaying,
                    IsMuted = _playerStateService.IsMuted,
                    Volume = (float)_playerStateService.Volume,
                    CurrentPosition = TimeSpan.FromSeconds(_playerStateService.CurrentPosition),
                    MaxPosition = TimeSpan.FromSeconds(_playerStateService.MaxPosition),
                    CurrentSong = _playerStateService.CurrentSong,
                    PlayMode = _playerStateService.CurrentPlayMode
                };
                message.Reply(statusResponse);
            }
            catch (Exception ex)
            {
                HandleError("OnPlayerStatusQueryRequested", ex);
                message.Reply(new PlayerStatusResponse());
            }
        }

        #endregion

        #region 错误处理

        private void OnErrorMessage(object recipient, ErrorMessage message)
        {
            // 处理错误消息，可以记录日志、显示通知等
            var errorInfo = message.Value;
            System.Diagnostics.Debug.WriteLine($"[{errorInfo.Source}] Error: {errorInfo.Message}");
            
            // 可以在这里添加日志记录、通知显示等逻辑
            // 例如：_notificationService.ShowError(errorInfo.Message);
        }

        private void HandleError(string operation, Exception ex)
        {
            var errorInfo = new ErrorInfo
            {
                Code = "PLAYER_CONTROL_ERROR",
                Message = $"播放控制操作失败: {operation}",
                Details = ex.Message,
                Source = "PlayerControlMessageHandler",
                Exception = ex
            };

            // 发送错误消息
            _messagingService.Send(new ErrorMessage(errorInfo));
        }

        #endregion

        #region 资源清理

        public void Dispose()
        {
            if (!_disposed)
            {
                _messagingService.Unregister(this);
                _disposed = true;
            }
        }

        #endregion
    }
}