using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Core.Enums
{
    public enum SortRule
    {
        /// <summary>
        /// 按添加时间排序（默认）
        /// </summary>
        ByAddedTime,

        /// <summary>
        /// 按歌曲标题排序
        /// </summary>
        ByTitle,

        /// <summary>
        /// 按艺术家排序
        /// </summary>
        ByArtist,

        /// <summary>
        /// 按专辑排序
        /// </summary>
        ByAlbum,

        /// <summary>
        /// 按时长排序
        /// </summary>
        ByDuration,

        /// <summary>
        /// 按文件大小排序
        /// </summary>
        ByFileSize
    }
}
