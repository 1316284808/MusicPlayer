using System;

namespace MusicPlayer.Core.Enums
{
    /// <summary>
    /// 播放上下文类型枚举
    /// 定义了不同的播放上下文类型，用于确定下一曲/上一曲的依据
    /// </summary>
    public enum PlaybackContextType
    {
        /// <summary>
        /// 默认播放列表
        /// </summary>
        DefaultPlaylist,
        
        /// <summary>
        /// 收藏列表
        /// </summary>
        Favorites,
        
        /// <summary>
        /// 特定歌手
        /// </summary>
        Artist,
        
        /// <summary>
        /// 特定专辑
        /// </summary>
        Album,
        
        /// <summary>
        /// 特定流派（未来扩展）
        /// </summary>
        Genre,
        
        /// <summary>
        /// 自定义播放列表（未来扩展）
        /// </summary>
        CustomPlaylist
    }
}