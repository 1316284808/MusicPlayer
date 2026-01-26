using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 歌曲模型类 - 表示音频文件的基本信息和元数据
    /// 使用MVVM工具包实现属性变更通知，支持数据绑定
    /// 已优化：实现专辑封面懒加载机制
    /// </summary>
   
    public partial class Song : ObservableObject
    {
        /// <summary>歌曲ID（数据库主键）</summary>
        [ObservableProperty]
        private int _id = -1;

        /// <summary>音频文件完整路径</summary>
        [ObservableProperty]
        private string _filePath = string.Empty;

        /// <summary>歌曲标题</summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>艺术家/演唱者</summary>
        [ObservableProperty]
        private string _artist = string.Empty;

        /// <summary>专辑名称</summary>
        [ObservableProperty]
        private string _album = string.Empty;

        /// <summary>歌曲时长</summary>
        [ObservableProperty]
        private TimeSpan _duration; 
        
        /// <summary>
        /// Duration属性变化时的处理
        /// </summary>
        partial void OnDurationChanged(TimeSpan value)
        {
            OnPropertyChanged(nameof(TotalTimeText));
        }


        /// <summary>文件大小（字节）</summary>
        [ObservableProperty]
        private long _fileSize;

        /// <summary>添加时间戳</summary>
        [ObservableProperty]
        private DateTime _addedTime = DateTime.Now;

        /// <summary>是否已删除（逻辑删除，不实际删除文件）</summary>
        [ObservableProperty]
        private bool _isDeleted = false;

        /// <summary>
        /// 专辑封面图像（可选）
        /// </summary>
        [property: LiteDB.BsonIgnore]
        [ObservableProperty]
        private BitmapImage? _albumArt;
        
        /// <summary>
        /// 原图专辑封面（用于旋转封面等需要高清显示的场合）
        /// </summary>
        [property: LiteDB.BsonIgnore]
        [ObservableProperty]
        private BitmapImage? _originalAlbumArt;
        /// <summary>
        /// 音频格式（文件扩展名）
        /// </summary>
        public string AudioFormat
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath))
                    return string.Empty;
                
                return Path.GetExtension(FilePath).ToLowerInvariant();
            }
        }
        /// <summary>
        /// 格式化的歌曲时长文本
        /// </summary>
        public string TotalTimeText
        {
            get
            {
                if (Duration == TimeSpan.Zero)
                    return "--:--";

                if (Duration.TotalHours >= 1)
                    return $"{(int)Duration.TotalHours}:{Duration:mm\\:ss}";
                else
                    return Duration.ToString(@"mm\:ss");
            }
        }
    }
}