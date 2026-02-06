using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;


namespace MusicPlayer.Services.Providers
{
    /// <summary>
    /// 专辑列表提供者
    /// 负责处理特定专辑的上下文逻辑
    /// </summary>
    public class AlbumProvider : BasePlaybackContextProvider
    {
        private readonly IPlaylistCacheService _cacheService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cacheService">播放列表缓存服务</param>
        public AlbumProvider(IPlaylistCacheService cacheService)
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
            if (context.Type != PlaybackContextType.Album)
                throw new ArgumentException($"Invalid context type. Expected {PlaybackContextType.Album}, but got {context.Type}");

            if (string.IsNullOrWhiteSpace(context.Identifier))
                throw new ArgumentException("Album identifier cannot be null or empty");

            // 获取指定专辑的所有未删除歌曲
            return _cacheService.GetPlaylist()
                .Where(song => !song.IsDeleted && 
                    !string.IsNullOrWhiteSpace(song.Album) &&
                    string.Equals(song.Album, context.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}