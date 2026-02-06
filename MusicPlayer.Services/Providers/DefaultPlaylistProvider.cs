using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;


namespace MusicPlayer.Services.Providers
{
    /// <summary>
    /// 默认播放列表提供者
    /// 负责处理默认播放列表的上下文逻辑
    /// </summary>
    public class DefaultPlaylistProvider : BasePlaybackContextProvider
    {
        private readonly IPlaylistCacheService _cacheService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cacheService">播放列表缓存服务</param>
        public DefaultPlaylistProvider(IPlaylistCacheService cacheService)
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
            if (context.Type != PlaybackContextType.DefaultPlaylist)
                throw new ArgumentException($"Invalid context type. Expected {PlaybackContextType.DefaultPlaylist}, but got {context.Type}");

            // 获取所有未删除的歌曲
            return _cacheService.GetPlaylist().Where(song => !song.IsDeleted).ToList();
        }
    }
}