using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;


namespace MusicPlayer.Services.Providers
{
    /// <summary>
    /// 收藏列表提供者
    /// 负责处理收藏列表的上下文逻辑
    /// </summary>
    public class FavoritesProvider : BasePlaybackContextProvider
    {
        private readonly IPlaylistCacheService _cacheService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cacheService">播放列表缓存服务</param>
        public FavoritesProvider(IPlaylistCacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        /// <summary>
        /// 根据播放上下文获取歌曲列表
        /// </summary>
        /// <param name="context">播放上下文</param>
        /// <returns>歌曲列表</returns>
        public override List<Song> GetSongsForContext(PlaybackContext context)
        {
            if (context.Type != PlaybackContextType.Favorites)
                throw new ArgumentException($"Invalid context type. Expected {PlaybackContextType.Favorites}, but got {context.Type}");

            // 从收藏列表歌单获取歌曲
            return _cacheService.GetSongsByPlaylistIdAsync(1).Result
                .Where(song => !song.IsDeleted)
                .ToList();
        }
    }
}