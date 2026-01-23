using System;
using LiteDB;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 播放列表歌曲关联模型类 - 表示播放列表和歌曲之间的多对多关系
    /// 用于维护歌曲在播放列表中的顺序和添加时间
    /// </summary>
    public class PlaylistSong
    {
        /// <summary>关联ID（数据库主键）</summary>
        [BsonId]
        public int Id { get; set; } = 0;

        /// <summary>播放列表ID</summary>
        public int PlaylistId { get; set; }

        /// <summary>歌曲ID</summary>
        public int SongId { get; set; }

        /// <summary>歌曲在播放列表中的顺序</summary>
        public int Order { get; set; }

        /// <summary>添加时间</summary>
        public DateTime AddedTime { get; set; } = DateTime.Now;
    }
}