using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 歌手信息模型
    /// </summary>
    public class SingerInfo : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _songCount;
        private System.Windows.Media.Imaging.BitmapImage? _coverImage;
        private bool _coverImageLoaded = false;
        private string? _firstSongFilePath; // 用于懒加载时获取歌曲文件

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public int SongCount
        {
            get => _songCount;
            set
            {
                if (_songCount != value)
                {
                    _songCount = value;
                    OnPropertyChanged(nameof(SongCount));
                }
            }
        }

        public System.Windows.Media.Imaging.BitmapImage? CoverImage
        {
            get => _coverImage;
            set
            {
                if (_coverImage != value)
                {
                    _coverImage = value;
                    _coverImageLoaded = value != null;
                    OnPropertyChanged(nameof(CoverImage));
                    OnPropertyChanged(nameof(HasCoverImage));
                }
            }
        }

        public bool HasCoverImage => _coverImageLoaded;

        public string? FirstSongFilePath
        {
            get => _firstSongFilePath;
            set
            {
                if (_firstSongFilePath != value)
                {
                    _firstSongFilePath = value;
                    OnPropertyChanged(nameof(FirstSongFilePath));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
