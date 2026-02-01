using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.Services.Coordinators
{
    /// <summary>
    /// 播放协调器实现 - 封装播放相关的业务逻辑
    /// </summary>
    public class PlaybackCoordinator : IPlaybackCoordinator
    {
        private readonly IPlayerService _playerService;
        private readonly IPlayerStateService _playerStateService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly INotificationService _notificationService;
        private readonly IMessagingService _messagingService;
        private readonly ILogger<PlaybackCoordinator>? _logger;

        public bool IsPlaying => _playerStateService.IsPlaying;
        public Song? CurrentSong => _playerStateService.CurrentSong;
        public double CurrentPosition => _playerStateService.CurrentPosition;
        public float Volume => _playerStateService.Volume;

        public PlaybackCoordinator(
            IPlayerService playerService,
            IPlayerStateService playerStateService,
            IPlaylistDataService playlistDataService,
            INotificationService notificationService,
            IMessagingService messagingService,
            ILogger<PlaybackCoordinator> logger = null)
        {
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _playerStateService = playerStateService ?? throw new ArgumentNullException(nameof(playerStateService));
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _logger = logger;

            _logger?.LogDebug($"PlaybackCoordinator创建完成，实例ID: {GetHashCode()}");
        }

        public async Task PlayAsync(Song song)
        {
            using var operation = new OperationScope($"播放: {song?.Title ?? "未知"}", _logger);

            try
            {
                if (song == null)
                {
                    _logger?.LogWarning("尝试播放空歌曲");
                    return;
                }

                _logger?.LogDebug($"开始播放歌曲: {song.Title} (ID: {song.Id})");

                // 1. 停止当前播放
                if (_playerStateService.IsPlaying)
                {
                    _logger?.LogDebug("当前正在播放，先停止");
                    await PauseAsync();
                }

                // 2. 设置当前歌曲
                _playlistDataService.SetCurrentSong(song);
                _logger?.LogDebug("已设置当前歌曲到播放列表服务") ;

                // 3. 加载并播放
                _playerService.LoadSong(song);
                _logger?.LogDebug("歌曲已加载到音频引擎");

                await Task.Delay(100); // 给予音频引擎初始化时间

                StartPlaybackInternal();
                _logger?.LogDebug("播放已开始");

                // 4. 发送通知和消息
                _notificationService.ShowNotification("开始播放", song.Title, NotificationType.Info);
                _messagingService.Send(new PlaybackStateChangedMessage(true));
                _messagingService.Send(new CurrentSongChangedMessage(song));

                _logger?.LogInformation($"播放成功: {song.Title}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"播放失败: {song?.Title}");
                _notificationService.ShowError($"播放失败: {ex.Message}");
                throw;
            }
        }

        public async Task PauseAsync()
        {
            using var operation = new OperationScope("暂停播放", _logger);

            try
            {
                if (!_playerStateService.IsPlaying)
                {
                    _logger?.LogDebug("当前未在播放，无需暂停");
                    return;
                }

                _logger?.LogDebug("执行暂停操作");

                _playerService.PausePlayback();
                _playerStateService.IsPlaying = false;

                _messagingService.Send(new PlaybackStateChangedMessage(false));

                _logger?.LogInformation("暂停成功");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "暂停失败");
                throw;
            }
        }

        public async Task ResumeAsync()
        {
            using var operation = new OperationScope("恢复播放", _logger);

            try
            {
                if (_playerStateService.CurrentSong == null)
                {
                    _logger?.LogWarning("没有当前歌曲，无法恢复播放");
                    return;
                }

                var currentSong = _playerStateService.CurrentSong;
                _logger?.LogDebug($"恢复播放: {currentSong.Title}");

                // 检查是否需要先加载歌曲到音频引擎
                // 只有在音频引擎未初始化时才需要加载歌曲
                if (!_playerService.IsAudioEngineInitialized)
                {
                    _logger?.LogDebug("音频引擎未初始化，需要加载歌曲");
                    
                    // 通过设置当前歌曲到 PlaylistDataService 来确保音频引擎已初始化
                    _playlistDataService.SetCurrentSong(currentSong);
                    
                    // 加载歌曲到音频引擎
                    _playerService.LoadSong(currentSong);
                    _logger?.LogDebug("已加载歌曲到音频引擎");

                    await Task.Delay(100); // 给予音频引擎初始化时间
                }
                else
                {
                    _logger?.LogDebug("音频引擎已初始化，直接恢复播放");
                }

                StartPlaybackInternal();
                _playerStateService.IsPlaying = true;

                _messagingService.Send(new PlaybackStateChangedMessage(true));

                _logger?.LogInformation("恢复播放成功");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "恢复播放失败");
                throw;
            }
        }

        public async Task StopAsync()
        {
            using var operation = new OperationScope("停止播放", _logger);

            try
            {
                _logger?.LogDebug("执行停止操作");

                _playerService.StopPlayback();
                _playerStateService.IsPlaying = false;
                _playerStateService.CurrentPosition = 0;

                _messagingService.Send(new PlaybackStateChangedMessage(false));
                _messagingService.Send(new PlaybackProgressChangedMessage(0));

                _logger?.LogInformation("停止成功");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "停止失败");
                throw;
            }
        }

        public async Task SeekAsync(double position)
        {
            using var operation = new OperationScope($"跳转到: {position:F2}秒", _logger);

            try
            {
                _logger?.LogDebug($"执行跳转操作: {position:F2}秒");

                _playerService.SeekToPosition(position);
                _playerStateService.CurrentPosition = position;

                _messagingService.Send(new SeekedMessage(position));

                _logger?.LogInformation($"跳转成功: {position:F2}秒");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"跳转失败: {position:F2}秒");
                throw;
            }
        }

        public async Task SetVolumeAsync(float volume)
        {
            using var operation = new OperationScope($"设置音量: {volume:P0}", _logger);

            try
            {
                var clampedVolume = Math.Clamp(volume, 0.0f, 1.0f);

                _logger?.LogDebug($"执行音量设置: {clampedVolume:P0}");

                _playerStateService.Volume = clampedVolume;
                _playerService.Volume = clampedVolume;

                _messagingService.Send(new VolumeChangedMessage(clampedVolume));

                _logger?.LogInformation($"音量设置成功: {clampedVolume:P0}");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"音量设置失败: {volume:P0}");
                throw;
            }
        }

        private void StartPlaybackInternal()
        {
            try
            {
                _playerService.StartPlayback();
                _playerStateService.IsPlaying = true;
                _logger?.LogDebug("内部播放方法执行成功");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "内部播放方法失败");
                throw;
            }
        }
    }

    /// <summary>
    /// 播放列表协调器实现 - 封装播放列表相关的业务逻辑
    /// </summary>
    public class PlaylistCoordinator : IPlaylistCoordinator
    {
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IMessagingService _messagingService;
        private readonly ILogger<PlaylistCoordinator>? _logger;

        public PlaylistCoordinator(
            IPlaylistDataService playlistDataService,
            IMessagingService messagingService,
            ILogger<PlaylistCoordinator> logger = null)
        {
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _logger = logger;

            _logger?.LogDebug($"PlaylistCoordinator创建完成，实例ID: {GetHashCode()}");
        }

        public async Task LoadPlaylistAsync()
        {
            using var operation = new OperationScope("加载播放列表", _logger);

            try
            {
                _logger?.LogDebug("开始加载播放列表数据");

                await _playlistDataService.LoadFromDataAsync();

                var count = _playlistDataService.DataSource?.Count ?? 0;
                _logger?.LogInformation($"播放列表加载成功，共 {count} 首歌曲");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载播放列表失败");
                throw;
            }
        }

        public async Task AddSongsAsync(System.Collections.Generic.IEnumerable<string> filePaths)
        {
            using var operation = new OperationScope("添加歌曲到播放列表", _logger);

            try
            {
                if (filePaths == null)
                {
                    _logger?.LogWarning("文件路径集合为空");
                    return;
                }

                var paths = new System.Collections.Generic.List<string>(filePaths);
                _logger?.LogDebug($"准备添加 {paths.Count} 个文件到播放列表");

                // 调用AddSongs方法（同步方法）
                _playlistDataService.AddSongs(paths.Select(p => new Song { FilePath = p }));

                _logger?.LogInformation($"成功添加 {paths.Count} 个文件到播放列表");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "添加歌曲到播放列表失败");
                throw;
            }
        }

        public async Task RemoveSongsAsync(System.Collections.Generic.IEnumerable<Song> songs)
        {
            using var operation = new OperationScope("从播放列表移除歌曲", _logger);

            try
            {
                if (songs == null)
                {
                    _logger?.LogWarning("歌曲集合为空");
                    return;
                }

                var songList = new System.Collections.Generic.List<Song>(songs);
                _logger?.LogDebug($"准备从播放列表移除 {songList.Count} 首歌曲");

                foreach (var song in songList)
                {
                    _playlistDataService.RemoveSong(song);
                    _logger?.LogDebug($"已移除歌曲: {song.Title}");
                }

                _logger?.LogInformation($"成功从播放列表移除 {songList.Count} 首歌曲");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "从播放列表移除歌曲失败");
                throw;
            }
        }

        public async Task<MusicPlayer.Core.Enums.SortRule> ChangeSortRuleAsync(MusicPlayer.Core.Enums.SortRule newRule)
        {
            using var operation = new OperationScope($"切换排序规则到: {newRule}", _logger);

            try
            {
                _logger?.LogDebug($"开始切换排序规则到: {newRule}");

                _playlistDataService.SetSortRule(newRule);

                _logger?.LogInformation($"排序规则切换成功: {newRule}");

                return newRule;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"切换排序规则失败: {newRule}");
                throw;
            }
        }
    }

    /// <summary>
    /// 操作范围辅助类 - 用于自动记录操作开始和结束
    /// </summary>
    internal sealed class OperationScope : IDisposable
    {
        private readonly string _operationName;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;

        public OperationScope(string operationName, ILogger logger)
        {
            _operationName = operationName;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
            _logger?.LogDebug($"操作开始: {_operationName}");
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger?.LogDebug($"操作完成: {_operationName}, 耗时: {_stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
