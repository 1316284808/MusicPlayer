using System;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 播放上下文类
    /// 表示当前播放的上下文环境，用于确定下一曲/上一曲的依据
    /// </summary>
    public class PlaybackContext
    {
        /// <summary>
        /// 播放上下文类型
        /// </summary>
        public PlaybackContextType Type { get; set; }

        /// <summary>
        /// 标识符，用于区分同一类型下的不同实例
        /// 例如：歌手名称、专辑名称、播放列表名称等
        /// </summary>
        public string Identifier { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称，用于UI显示
        /// 例如："陈奕迅"、"红磡演唱会"、"收藏列表"等
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 创建默认播放列表上下文
        /// </summary>
        /// <returns>默认播放列表上下文实例</returns>
        public static PlaybackContext CreateDefault()
        {
            return new PlaybackContext
            {
                Type = PlaybackContextType.DefaultPlaylist,
                Identifier = "Default",
                DisplayName = "默认列表"
            };
        }

        /// <summary>
        /// 创建歌手上下文
        /// </summary>
        /// <param name="artistName">歌手名称</param>
        /// <returns>歌手上下文实例</returns>
        public static PlaybackContext CreateArtist(string artistName)
        {
            return new PlaybackContext
            {
                Type = PlaybackContextType.Artist,
                Identifier = artistName,
                DisplayName = artistName
            };
        }

        /// <summary>
        /// 创建专辑上下文
        /// </summary>
        /// <param name="albumName">专辑名称</param>
        /// <returns>专辑上下文实例</returns>
        public static PlaybackContext CreateAlbum(string albumName)
        {
            return new PlaybackContext
            {
                Type = PlaybackContextType.Album,
                Identifier = albumName,
                DisplayName = albumName
            };
        }

        /// <summary>
        /// 重写Equals方法，用于比较两个播放上下文是否相等
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object? obj)
        {
            if (obj is PlaybackContext other)
            {
                return Type == other.Type && 
                       string.Equals(Identifier, other.Identifier, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// 重写GetHashCode方法
        /// </summary>
        /// <returns>哈希值</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Identifier, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"{Type}: {DisplayName}";
        }
    }
}