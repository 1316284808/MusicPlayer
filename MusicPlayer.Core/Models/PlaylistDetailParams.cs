using System;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 歌单详情页导航参数类
    /// </summary>
    public class PlaylistDetailParams
    {
        /// <summary>
        /// 歌手名称
        /// </summary>
        public string? ArtistName { get; set; }
        
        /// <summary>
        /// 专辑名称
        /// </summary>
        public string? AlbumName { get; set; }
        
        /// <summary>
        /// 歌单ID
        /// </summary>
        public int? PlaylistId { get; set; }
    }
}