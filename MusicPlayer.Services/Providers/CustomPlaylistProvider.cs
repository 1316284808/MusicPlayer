using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;


namespace MusicPlayer.Services.Providers
{
    /// <summary>
    /// 自定义播放列表提供者 - 处理自定义播放列表的播放上下文
    /// </summary>
    public class CustomPlaylistProvider : BasePlaybackContextProvider
    {
        private readonly IPlaylistCacheService _playlistCacheService;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="playlistCacheService">播放列表缓存服务</param>
        public CustomPlaylistProvider(IPlaylistCacheService playlistCacheService)
        {
            _playlistCacheService = playlistCacheService;
        }
        
        /// <summary>
        /// 根据播放上下文获取歌曲列表
        /// 对于自定义播放列表，上下文的Identifier是播放列表ID
        /// </summary>
        /// <param name="context">播放上下文</param>
        /// <returns>歌曲列表</returns>
        public override List<Song> GetSongsForContext(PlaybackContext context)
        {
            try
            {
                // 解析播放列表ID
                if (int.TryParse(context.Identifier, out int playlistId))
                {
                    // 从缓存服务获取播放列表中的歌曲
                    var songs = _playlistCacheService.GetSongsByPlaylistIdAsync(playlistId).Result;
                    return songs;
                }
                
                System.Diagnostics.Debug.WriteLine($"CustomPlaylistProvider: 无法解析播放列表ID: {context.Identifier}");
                return new List<Song>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CustomPlaylistProvider: 获取歌曲列表失败: {ex.Message}");
                return new List<Song>();
            }
        }
    }
}