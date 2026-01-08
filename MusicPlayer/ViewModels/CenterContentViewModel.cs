using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 中心内容视图模型 - 负责显示专辑封面、歌曲信息和歌词
    /// 使用PlayerStateService作为播放状态的唯一可信源，保持架构一致性
    /// 已优化：支持专辑封面懒加载
    /// </summary>
    public class CenterContentViewModel : ObservableObject, ICenterContentViewModel
    {
        private readonly IMessagingService _messagingService;
        private readonly IConfigurationService _configurationService;
        
        // 状态属性通过消息同步
        private Core.Models.Song? _currentSong;
        private bool _isPlaying;
        private bool _isWindowMaximized = false;
        
        private ObservableCollection<Core.Models.LyricLine> _lyrics = new();
        private Core.Models.LyricLine? _currentLyricLine;
        
        // 歌词对齐方式
        private System.Windows.TextAlignment _lyricTextAlignment = System.Windows.TextAlignment.Right;
        
        /// <summary>
        /// 歌词对齐方式
        /// </summary>
        public System.Windows.TextAlignment LyricTextAlignment
        {
            get => _lyricTextAlignment;
            set
            {
                if (_lyricTextAlignment != value)
                {
                    _lyricTextAlignment = value;
                    OnPropertyChanged(nameof(LyricTextAlignment));
                    OnPropertyChanged(nameof(LyricAlignmentIconKind));
                    // 更新配置
                    _configurationService.UpdateLyricTextAlignment(value);
                }
            }
        }
        
        /// <summary>
        /// 歌词对齐图标类型
        /// </summary>
        public string LyricAlignmentIconKind
        {
            get
            {
                switch (LyricTextAlignment)
                {
                    case System.Windows.TextAlignment.Left:
                        return "Left";
                    case System.Windows.TextAlignment.Center:
                        return "Center";
                    case System.Windows.TextAlignment.Right:
                        return "Right";
                    default:
                        return "Right";
                }
            }
        }
        
        // 歌词字体大小
        private double _lyricFontSize = 20;
        
        /// <summary>
        /// 歌词字体大小
        /// </summary>
        public double LyricFontSize
        {
            get => _lyricFontSize;
            set
            {
                if (_lyricFontSize != value)
                {
                    _lyricFontSize = value;
                    OnPropertyChanged(nameof(LyricFontSize));
                    OnPropertyChanged(nameof(SelectedLyricFontSize));
                    // 更新配置
                    _configurationService.UpdateLyricFontSize(value);
                }
            }
        }
        
        /// <summary>
        /// 选中歌词字体大小（始终比普通大8）
        /// </summary>
        public double SelectedLyricFontSize
        {
            get => LyricFontSize + 8;
        }
        

        /// <summary>
        /// 当前歌曲 - 通过消息系统同步
        /// </summary>
        public Core.Models.Song? CurrentSong
        {
            get => _currentSong;
            set 
            { 
                if (_currentSong != value)
                {
                    _currentSong = value; 
                    OnPropertyChanged(nameof(CurrentSong));
                    
                    // 更新缓存的歌曲信息属性
                    UpdateSongProperties();
                }
            }
        }

        /// <summary>
        /// 更新所有歌曲相关属性的缓存值
        /// </summary>
        private void UpdateSongProperties()
        {
            if (_currentSong != null)
            {
                // 直接设置属性值，WPF绑定系统会自动处理UI更新
                CurrentSongTitle = _currentSong.Title ?? string.Empty;
                CurrentSongArtist = _currentSong.Artist ?? string.Empty;
                CurrentSongAlbum = _currentSong.Album ?? string.Empty;
               
                // 确保专辑封面已加载
                _currentSong.EnsureAlbumArtLoaded();
                _currentSong.EnsureOriginalAlbumArtLoaded();
                
                // 更新封面
                CurrentSongAlbumArt = _currentSong.AlbumArt;
                CurrentSongOriginalAlbumArt = _currentSong.OriginalAlbumArt;
            }
            else
            {
                // 当没有歌曲时，显示默认文本
                CurrentSongTitle = "请选择歌曲";
                CurrentSongArtist = "未知歌手";
                CurrentSongAlbum = "未知专辑";
                CurrentSongAlbumArt = null;
                CurrentSongOriginalAlbumArt = null;
            }
        }

        // 为了解决绑定刷新问题，添加缓存的单独属性
        private string _currentSongTitle = string.Empty;
        private string _currentSongArtist = string.Empty;
        private string _currentSongAlbum = string.Empty;
        private BitmapImage? _currentSongAlbumArt;
        private BitmapImage? _currentSongOriginalAlbumArt;

        public string CurrentSongTitle
        {
            get => _currentSongTitle;
            private set
            {
                if (_currentSongTitle != value)
                {
                    _currentSongTitle = value;
                    OnPropertyChanged(nameof(CurrentSongTitle));
                }
            }
        }

        public string CurrentSongArtist
        {
            get => _currentSongArtist;
            private set
            {
                if (_currentSongArtist != value)
                {
                    _currentSongArtist = value;
                    OnPropertyChanged(nameof(CurrentSongArtist));
                }
            }
        }

        public string CurrentSongAlbum
        {
            get => _currentSongAlbum;
            private set
            {
                if (_currentSongAlbum != value)
                {
                    _currentSongAlbum = value;
                    OnPropertyChanged(nameof(CurrentSongAlbum));
                }
            }
        }

        public BitmapImage? CurrentSongAlbumArt
        {
            get => _currentSongAlbumArt;
            private set
            {
                if (_currentSongAlbumArt != value)
                {
                    _currentSongAlbumArt = value;
                    OnPropertyChanged(nameof(CurrentSongAlbumArt));
                }
            }
        }

        public BitmapImage? CurrentSongOriginalAlbumArt
        {
            get => _currentSongOriginalAlbumArt;
            private set
            {
                if (_currentSongOriginalAlbumArt != value)
                {
                    _currentSongOriginalAlbumArt = value;
                    OnPropertyChanged(nameof(CurrentSongOriginalAlbumArt));
                }
            }
        }

        /// <summary>
        /// 播放状态 - 通过消息系统同步
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    var oldValue = _isPlaying;
                    _isPlaying = value;
                    OnPropertyChanged(nameof(IsPlaying)); 
                }
            }
        }

        /// <summary>
        /// 窗口是否最大化 - 通过消息系统同步
        /// </summary>
        public bool IsWindowMaximized
        {
            get => _isWindowMaximized;
            set
            {
                if (_isWindowMaximized != value)
                {
                    _isWindowMaximized = value;
                    OnPropertyChanged(nameof(IsWindowMaximized));
                }
            }
        }

        public ObservableCollection<Core.Models.LyricLine> Lyrics
        {
            get => _lyrics;
            set
            {
                _lyrics = value;
                OnPropertyChanged(nameof(Lyrics));
            }
        }

        public Core.Models.LyricLine? CurrentLyricLine
        {
            get => _currentLyricLine;
            set
            {
                _currentLyricLine = value;
                OnPropertyChanged(nameof(CurrentLyricLine));
            }
        }



        // 命令定义
        public ICommand PlayPauseCommand { get; }
        public ICommand ToggleLyricAlignmentCommand { get; }
        public ICommand IncreaseLyricFontSizeCommand { get; }
        public ICommand DecreaseLyricFontSizeCommand { get; }

       

        public CenterContentViewModel(IMessagingService messagingService, IConfigurationService configurationService)
        {
            _messagingService = messagingService;
            _configurationService = configurationService;
            
            // 从配置中初始化歌词样式
            _lyricFontSize = _configurationService.CurrentConfiguration.LyricFontSize;
            _lyricTextAlignment = _configurationService.CurrentConfiguration.LyricTextAlignment;
            
            // 初始化默认歌曲信息
            InitializeDefaultSongInfo();
            
            // 初始化命令
            PlayPauseCommand = new RelayCommand(ExecutePlayPause);
            ToggleLyricAlignmentCommand = new RelayCommand(ToggleLyricAlignment);
            IncreaseLyricFontSizeCommand = new RelayCommand(IncreaseLyricFontSize);
            DecreaseLyricFontSizeCommand = new RelayCommand(DecreaseLyricFontSize);

            // 注册消息处理器 - 通过消息系统接收状态更新
            RegisterMessageHandlers();
          
        }
        
        /// <summary>
        /// 切换歌词对齐方式
        /// </summary>
        private void ToggleLyricAlignment()
        {
            switch (LyricTextAlignment)
            {
                case System.Windows.TextAlignment.Left:
                    LyricTextAlignment = System.Windows.TextAlignment.Center;
                    break;
                case System.Windows.TextAlignment.Center:
                    LyricTextAlignment = System.Windows.TextAlignment.Right;
                    break;
                case System.Windows.TextAlignment.Right:
                    LyricTextAlignment = System.Windows.TextAlignment.Left;
                    break;
            }
        }
        
        /// <summary>
        /// 增加歌词字体大小
        /// </summary>
        private void IncreaseLyricFontSize()
        {
            if (LyricFontSize >= 40) return;
            LyricFontSize += 1;
        }
        
        /// <summary>
        /// 减少歌词字体大小
        /// </summary>
        private void DecreaseLyricFontSize()
        {
            if (LyricFontSize <= 10) return;
            LyricFontSize -= 1;
        }

        /// <summary>
        /// 初始化默认歌曲信息
        /// </summary>
        private void InitializeDefaultSongInfo()
        {
           
            // 确保初始状态下有默认值显示
            CurrentSongTitle = "请选择歌曲";
            CurrentSongArtist = "未知歌手";
            CurrentSongAlbum = "未知专辑";
            
               }

        /// <summary>
        /// 处理PlayerStateService的属性变化事件
        /// </summary>
        private void OnPlayerStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 通知UI相关属性变化，确保UI更新
            switch (e.PropertyName)
            {
                case nameof(PlayerStateService.CurrentSong):
                    OnPropertyChanged(nameof(CurrentSong));
                    break;
                case nameof(IsPlaying):
                    OnPropertyChanged(nameof(IsPlaying));
                    break;

            }
        }



        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
           
            // 当前歌曲变化消息
            _messagingService.Register<CurrentSongChangedMessage>(this, (r, m) =>
            {
                // 直接设置CurrentSong，WPF绑定系统会自动处理UI更新
                CurrentSong = m.Value;
            });

            // 播放状态变化消息
            _messagingService.Register<PlaybackStateChangedMessage>(this, (r, m) =>
            {
                IsPlaying = m.Value;
            });

            // 播放进度变化消息
            _messagingService.Register<PlaybackProgressMessage>(this, (r, m) =>
            {
                UpdateCurrentLyricLine(m.Value);
            });

            // 歌词更新消息
            _messagingService.Register<LyricsUpdatedMessage>(this, (r, m) =>
            {
                SetLyrics(m.Value);
            });

            // 当前歌词行变化消息
            _messagingService.Register<CurrentLyricLineMessage>(this, (r, m) =>
            {
                CurrentLyricLine = m.Value;
            });

            // 窗口状态变化消息
            _messagingService.Register<WindowStateChangedMessage>(this, (r, m) =>
            {
                IsWindowMaximized = m.Value == WindowState.Maximized;
            });
        }

        /// <summary>
        /// 执行播放/暂停命令
        /// </summary>
        private void ExecutePlayPause()
        {
            // 发送播放/暂停消息
            _messagingService.Send(new PlayPauseMessage());
        }

        /// <summary>
        /// 更新当前歌词行
        /// </summary>
        private void UpdateCurrentLyricLine(double currentTime)
        {
            if (Lyrics.Any() && CurrentSong != null)
            {
                // 找到当前时间对应的歌词行
                var currentLyric = Lyrics.LastOrDefault(l => l.Time <= TimeSpan.FromSeconds(currentTime));
                
                if (currentLyric != null && currentLyric != CurrentLyricLine)
                {
                    CurrentLyricLine = currentLyric;
                }
            }
        }

        /// <summary>
        /// 设置歌词数据
        /// </summary>
        public void SetLyrics(ObservableCollection<Core.Models.LyricLine> lyrics)
        {
            Lyrics.Clear();
            foreach (var lyric in lyrics)
            {
                Lyrics.Add(lyric);
            }
            CurrentLyricLine = null;
            
            // 通知歌词已更新，触发滚动重置
            OnPropertyChanged(nameof(Lyrics));
        }

        /// <summary>
        /// 清空歌词
        /// </summary>
        public void ClearLyrics()
        {
            Lyrics.Clear();
            CurrentLyricLine = null;
        }





        
    }
}