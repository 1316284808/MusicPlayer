using System;
using System.Diagnostics;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 播放列表导航服务实现
    /// 职责：处理播放列表中的导航逻辑（上一首、下一首）
    /// 支持插队播放队列，优先返回插队歌曲
    /// </summary>
    public class PlaylistNavigationService : IPlaylistNavigationService
    {
        private readonly IPlaybackContextService _playbackContextService;
        private readonly IPlaylistCacheService _cacheService;
        private readonly IPlayQueueService _playQueueService;

        public PlaylistNavigationService(
            IPlaybackContextService playbackContextService,
            IPlaylistCacheService cacheService,
            IPlayQueueService playQueueService)
        {
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _playQueueService = playQueueService ?? throw new ArgumentNullException(nameof(playQueueService));
        }

        /// <summary>
        /// 获取下一首歌曲
        /// 优先返回插队队列中的歌曲
        /// </summary>
        public Song? GetNextSong(PlayMode playMode = PlayMode.RepeatAll, Song? currentSong = null)
        {
            // 首先检查插队队列是否有歌曲
            if (_playQueueService.HasSongs)
            {
                var queuedSong = _playQueueService.DequeueSong();
                Debug.WriteLine($"PlaylistNavigationService: GetNextSong - 从插队队列获取歌曲: {queuedSong?.Title}");
                return queuedSong;
            }
            
            // 获取当前播放上下文
            var context = _playbackContextService.CurrentPlaybackContext;
            var actualCurrentSong = currentSong;
            
            Debug.WriteLine($"PlaylistNavigationService: GetNextSong - 播放上下文: {context}, 播放模式: {playMode}, 当前歌曲: {actualCurrentSong?.Title}");
            
            // 如果当前歌曲为空，返回null
            if (actualCurrentSong == null) 
            {
                Debug.WriteLine($"PlaylistNavigationService: GetNextSong - 当前歌曲为空，返回null");
                return null;
            }

            // 获取对应的提供者
            try
            {
                var provider = _playbackContextService.GetProvider(context.Type);
                
                // 使用提供者获取下一首歌曲
                var nextSong = provider.GetNextSong(context, actualCurrentSong, playMode);
                
                Debug.WriteLine($"PlaylistNavigationService: GetNextSong - 播放上下文: {context}, 播放模式: {playMode}, 当前歌曲: {actualCurrentSong?.Title}, 下一首: {nextSong?.Title}");
                
                return nextSong;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlaylistNavigationService: GetNextSong - 异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取上一首歌曲
        /// </summary>
        public Song? GetPreviousSong(PlayMode playMode = PlayMode.RepeatAll, Song? currentSong = null)
        {
            // 获取当前播放上下文
            var context = _playbackContextService.CurrentPlaybackContext;
            var actualCurrentSong = currentSong;
            
            Debug.WriteLine($"PlaylistNavigationService: GetPreviousSong - 播放上下文: {context}, 播放模式: {playMode}, 当前歌曲: {actualCurrentSong?.Title}");
            
            // 如果当前歌曲为空，返回null
            if (actualCurrentSong == null) 
            {
                Debug.WriteLine($"PlaylistNavigationService: GetPreviousSong - 当前歌曲为空，返回null");
                return null;
            }

            // 获取对应的提供者
            try
            {
                var provider = _playbackContextService.GetProvider(context.Type);
                
                // 使用提供者获取上一首歌曲
                var previousSong = provider.GetPreviousSong(context, actualCurrentSong, playMode);
                
                Debug.WriteLine($"PlaylistNavigationService: GetPreviousSong - 播放上下文: {context}, 播放模式: {playMode}, 当前歌曲: {actualCurrentSong?.Title}, 上一首: {previousSong?.Title}");
                
                return previousSong;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlaylistNavigationService: GetPreviousSong - 异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取歌曲在列表中的索引
        /// </summary>
        public int GetSongIndex(Song song)
        {
            var playlist = _cacheService.GetPlaylist();
            return playlist.IndexOf(song);
        }
    }
}