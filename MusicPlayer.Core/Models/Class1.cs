using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MusicPlayer.Core.Models
{
   

 
        /// <summary>
        /// 歌曲数据库DTO - 仅包含可序列化的简单类型
        /// </summary>
        public class Song : INotifyPropertyChanged
    {
            /// <summary>数据库主键</summary>
            public int Id { get; set; } = -1;

            /// <summary>文件路径</summary>
            public string FilePath { get; set; } = string.Empty;

            /// <summary>歌曲标题</summary>
            public string Title { get; set; } = string.Empty;

            /// <summary>艺术家</summary>
            public string Artist { get; set; } = string.Empty;

            /// <summary>专辑名</summary>
            public string Album { get; set; } = string.Empty;

            /// <summary>时长</summary>
            public TimeSpan Duration { get; set; }

            /// <summary>文件大小</summary>
            public long FileSize { get; set; }

            /// <summary>添加时间</summary>
            public DateTime AddedTime { get; set; } = DateTime.Now;

            /// <summary>是否收藏</summary>
            public bool Heart { get; set; }

            /// <summary>逻辑删除</summary>
            public bool IsDeleted { get; set; }
        }
    }

