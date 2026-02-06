namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 音乐库统计信息模型
    /// 用于展示音乐库的核心数据概览
    /// </summary>
    public class LibraryStatistics
    {
        /// <summary>
        /// 总歌曲数
        /// </summary>
        public int TotalSongs { get; set; }

        /// <summary>
        /// 总歌单数
        /// </summary>
        public int TotalPlaylists { get; set; }

        /// <summary>
        /// 总歌手数（不重复）
        /// </summary>
        public int TotalArtists { get; set; }

        /// <summary>
        /// 总专辑数（不重复）
        /// </summary>
        public int TotalAlbums { get; set; }

        /// <summary>
        /// 创建一个空的统计对象
        /// </summary>
        public static LibraryStatistics Empty => new LibraryStatistics();
    }
}
