using System;
using System.Diagnostics;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 播放列表状态服务实现
    /// 职责：管理播放列表相关的状态（当前歌曲、排序规则等）
    /// </summary>
    public class PlaylistStateService : IPlaylistStateService
    {
        private readonly IMessagingService _messagingService;
        private readonly IDispatcherService _dispatcherService;
        private readonly object _lock = new object();

        private Song? _currentSong;
        private SortRule _currentSortRule = SortRule.ByAddedTime;

        public PlaylistStateService(
            IMessagingService messagingService,
            IDispatcherService dispatcherService)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
        }

        /// <summary>
        /// 当前播放歌曲
        /// </summary>
        public Song? CurrentSong
        {
            get
            {
                lock (_lock)
                {
                    return _currentSong;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (_currentSong != value)
                    {
                        var oldSong = _currentSong;
                        _currentSong = value;

                        Debug.WriteLine($"PlaylistStateService: CurrentSong 更新 - 旧歌曲: {oldSong?.Title}, 新歌曲: {value?.Title}");

                        // 在UI线程上发送消息
                        _dispatcherService.Invoke(() =>
                        {
                            _messagingService.Send(new CurrentSongChangedMessage(value));
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 当前排序规则
        /// </summary>
        public SortRule CurrentSortRule
        {
            get
            {
                lock (_lock)
                {
                    return _currentSortRule;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (_currentSortRule != value)
                    {
                        _currentSortRule = value;
                        Debug.WriteLine($"PlaylistStateService: CurrentSortRule 更新为 {value}");
                    }
                }
            }
        }

        /// <summary>
        /// 设置当前歌曲（不发送消息，用于初始化恢复）
        /// </summary>
        public void SetCurrentSongWithoutNotification(Song? song)
        {
            lock (_lock)
            {
                _currentSong = song;
                Debug.WriteLine($"PlaylistStateService: 静默设置当前歌曲: {song?.Title}");
            }
        }
    }
}