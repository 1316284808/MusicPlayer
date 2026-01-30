using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Data;
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
    public class CenterContentViewModel : ObservableObject, ICenterContentViewModel, IDisposable
    {
        private readonly IMessagingService _messagingService;
        private readonly IConfigurationService _configurationService;
        private readonly IPlayerStateService _playerStateService;
        private readonly IPlaylistService _playlistService;

        // 状态属性通过消息同步
        private Core.Models.Song? _currentSong;
        private bool _isPlaying;
        private bool _isWindowMaximized = false;
        
        // 自动隐藏相关属性
        private double _lyricSettingsOpacity = 1.0; // 控制歌词设置面板的透明度
        private System.Timers.Timer _hideSettingsTimer; // 隐藏设置面板的计时器

        private ObservableCollection<Core.Models.LyricLine> _lyrics = new();
        private Core.Models.LyricLine? _currentLyricLine;

        // 歌词对齐方式
        private System.Windows.HorizontalAlignment _lyricTextAlignment = System.Windows.HorizontalAlignment.Right;
        // 是否启用歌词翻译
        private bool _isLyricTranslationEnabled = true;

        /// <summary>
        /// 歌词对齐方式
        /// </summary>
        public System.Windows.HorizontalAlignment LyricTextAlignment
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
                    case System.Windows.HorizontalAlignment.Left:
                        return "Left";
                    case System.Windows.HorizontalAlignment.Center:
                        return "Center";
                    case System.Windows.HorizontalAlignment.Right:
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

        public string LyricTranslationText => IsLyricTranslationEnabled ? "开启" : "禁用";
        /// <summary>
        /// 是否启用歌词翻译
        /// </summary>

        public bool IsLyricTranslationEnabled
        {
            get => _isLyricTranslationEnabled;
            set
            {
                if (_isLyricTranslationEnabled != value)
                {
                    _isLyricTranslationEnabled = value;
                    OnPropertyChanged(nameof(LyricTranslationText));
                    OnPropertyChanged(nameof(IsLyricTranslationEnabled));
                    // 更新配置
                    _configurationService.UpdateLyricTranslationEnabled(value);
                }
            }
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
                // 使用AlbumArtLoader直接加载封面
                _currentSong.AlbumArt = AlbumArtLoader.LoadAlbumArt(_currentSong.FilePath);
                _currentSong.OriginalAlbumArt = AlbumArtLoader.LoadAlbumArt(_currentSong.FilePath);

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
        
        /// <summary>
        /// 歌词设置面板的透明度
        /// </summary>
        public double LyricSettingsOpacity
        {
            get => _lyricSettingsOpacity;
            set
            {
                if (Math.Abs(_lyricSettingsOpacity - value) > 0.01)
                {
                    _lyricSettingsOpacity = value;
                    OnPropertyChanged(nameof(LyricSettingsOpacity));
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
                UpdatePlayedAndUnplayedLyrics();
            }
        }

        public Core.Models.LyricLine? CurrentLyricLine
        {
            get => _currentLyricLine;
            set
            {
                _currentLyricLine = value;
                OnPropertyChanged(nameof(CurrentLyricLine));
                UpdatePlayedAndUnplayedLyrics();
            }
        }

        /// <summary>
        /// 已播放歌词行集合
        /// </summary>
        public ObservableCollection<Core.Models.LyricLine> PlayedLyrics { get; } = new ObservableCollection<Core.Models.LyricLine>();

        /// <summary>
        /// 未播放歌词行集合
        /// </summary>
        public ObservableCollection<Core.Models.LyricLine> UnplayedLyrics { get; } = new ObservableCollection<Core.Models.LyricLine>();

        /// <summary>
        /// 更新已播放和未播放歌词集合
        /// </summary>
        private void UpdatePlayedAndUnplayedLyrics()
        {
            PlayedLyrics.Clear();
            UnplayedLyrics.Clear();

            if (Lyrics == null || Lyrics.Count == 0 || CurrentLyricLine == null)
            {
                return;
            }

            // 找到当前歌词行的索引
            int currentIndex = Lyrics.IndexOf(CurrentLyricLine);
            if (currentIndex == -1)
            {
                return;
            }

            // 添加已播放歌词（当前歌词之前的所有歌词）
            for (int i = 0; i < currentIndex; i++)
            {
                PlayedLyrics.Add(Lyrics[i]);
            }

            // 添加未播放歌词（当前歌词之后的所有歌词）
            for (int i = currentIndex + 1; i < Lyrics.Count; i++)
            {
                UnplayedLyrics.Add(Lyrics[i]);
            }
        }



        // 命令定义
        public ICommand PlayPauseCommand { get; }
        public ICommand ToggleLyricAlignmentCommand { get; }
        public ICommand IncreaseLyricFontSizeCommand { get; }
        public ICommand DecreaseLyricFontSizeCommand { get; }
        public ICommand MouseEnterCommand { get; }
        public ICommand MouseLeaveCommand { get; }



        public CenterContentViewModel(
            IMessagingService messagingService, 
            IConfigurationService configurationService,
            IPlayerStateService playerStateService,
            IPlaylistService playlistService)
        {
            _messagingService = messagingService;
            _configurationService = configurationService;
            _playerStateService = playerStateService;
            _playlistService = playlistService;

            // 从配置中初始化歌词样式
            _lyricFontSize = _configurationService.CurrentConfiguration.LyricFontSize;
            _lyricTextAlignment = _configurationService.CurrentConfiguration.LyricTextAlignment;
            _isLyricTranslationEnabled = _configurationService.CurrentConfiguration.IsLyricTranslationEnabled;

            // 初始化默认歌曲信息
            InitializeDefaultSongInfo();

            // 初始化命令
            PlayPauseCommand = new RelayCommand(ExecutePlayPause);
            ToggleLyricAlignmentCommand = new RelayCommand(ToggleLyricAlignment);
            IncreaseLyricFontSizeCommand = new RelayCommand(IncreaseLyricFontSize);
            DecreaseLyricFontSizeCommand = new RelayCommand(DecreaseLyricFontSize);
            MouseEnterCommand = new RelayCommand(ExecuteMouseEnter);
            MouseLeaveCommand = new RelayCommand(ExecuteMouseLeave);

            // 初始化计时器
            _hideSettingsTimer = new System.Timers.Timer(3000); // 3秒
            _hideSettingsTimer.Elapsed += HideSettingsTimer_Elapsed;
            _hideSettingsTimer.AutoReset = false;
            _hideSettingsTimer.Start(); // 初始启动计时器

            // 注册消息处理器 - 通过消息系统接收状态更新
            RegisterMessageHandlers();

            // 从PlayerStateService获取当前播放状态并初始化
            InitializeFromPlayerState();
        }

        /// <summary>
        /// 从PlayerStateService获取当前状态并初始化
        /// </summary>
        private void InitializeFromPlayerState()
        {
            try
            {
                // 获取当前歌曲并触发属性更新
                var currentSong = _playerStateService.CurrentSong;
                if (currentSong != null)
                {
                    CurrentSong = currentSong;
                    
                    // 主动加载并设置当前歌曲的歌词（防止错过LyricsUpdatedMessage）
                    try
                    {
                        var lyrics = _playlistService.LoadLyrics(currentSong.FilePath);
                        SetLyrics(new ObservableCollection<Core.Models.LyricLine>(lyrics));
                        System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 主动加载歌词成功，共 {lyrics.Count} 行");
                    }
                    catch (Exception lyricEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 加载歌词失败: {lyricEx.Message}");
                        SetLyrics(new ObservableCollection<Core.Models.LyricLine>());
                    }
                }

                // 获取播放状态
                IsPlaying = _playerStateService.IsPlaying;

                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 从PlayerStateService初始化完成，歌曲: {currentSong?.Title ?? "无"}, 播放状态: {IsPlaying}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 从PlayerStateService初始化失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Cleanup();
        }

        /// <summary>
        /// 切换歌词对齐方式
        /// </summary>
        private void ToggleLyricAlignment()
        {
            switch (LyricTextAlignment)
            {
                case System.Windows.HorizontalAlignment.Left:
                    LyricTextAlignment = System.Windows.HorizontalAlignment.Center;
                    break;
                case System.Windows.HorizontalAlignment.Center:
                    LyricTextAlignment = System.Windows.HorizontalAlignment.Right;
                    break;
                case System.Windows.HorizontalAlignment.Right:
                    LyricTextAlignment = System.Windows.HorizontalAlignment.Left;
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
                // 将秒转换为TimeSpan
                var timeSpan = TimeSpan.FromSeconds(currentTime);
                
                // 找到当前时间对应的歌词行
                var currentLyric = Lyrics.LastOrDefault(l => l.Time <= timeSpan);

                if (currentLyric != null)
                {
                    // 更新当前歌词行
                    if (currentLyric != CurrentLyricLine)
                    {
                        CurrentLyricLine = currentLyric;
                    }
                    
                    // 计算逐字进度
                    int currentIndex = Lyrics.IndexOf(currentLyric);
                    TimeSpan lineDuration;
                    
                    // 如果是最后一句歌词，进度固定为1
                    if (currentIndex == Lyrics.Count - 1)
                    {
                        lineDuration = TimeSpan.FromSeconds(5); // 最后一句持续5秒
                    }
                    else
                    {
                        // 计算当前歌词行的播放时长（下一句时间 - 当前句时间）
                        var nextLine = Lyrics[currentIndex + 1];
                        lineDuration = nextLine.Time - currentLyric.Time;
                    }

                    // 计算当前进度在当前歌词行内的比例
                    TimeSpan elapsedInLine = timeSpan - currentLyric.Time;
                    double progress = elapsedInLine.TotalSeconds / lineDuration.TotalSeconds;
                    
                    // 确保进度在0-1之间
                    progress = Math.Clamp(progress, 0.0, 1.0);
                    
                    // 更新当前歌词行的进度
                    if (Math.Abs(currentLyric.Progress - progress) > 0.01) // 避免频繁更新
                    {
                        currentLyric.Progress = progress;
                    }
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

            // 通知歌词已更新，触发滚动重置
            OnPropertyChanged(nameof(Lyrics));
            
            // 如果有歌词，设置第一句为当前歌词
            if (Lyrics.Count > 0)
            {
                CurrentLyricLine = Lyrics[0];
            }
            else
            {
                CurrentLyricLine = null;
            }
        }

        /// <summary>
        /// 清空歌词
        /// </summary>
        public void ClearLyrics()
        {
            Lyrics.Clear();
            CurrentLyricLine = null;
        }
        
        /// <summary>
        /// 计时器事件处理 - 隐藏歌词设置面板
        /// </summary>
        private void HideSettingsTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 确保在UI线程上执行
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LyricSettingsOpacity = 0.0;
            });
        }
        
        /// <summary>
        /// 执行鼠标进入操作
        /// </summary>
        private void ExecuteMouseEnter()
        {
            // 当鼠标进入时，停止计时器并显示设置面板
            _hideSettingsTimer.Stop();
            LyricSettingsOpacity = 1.0;
        }
        
        /// <summary>
        /// 执行鼠标离开操作
        /// </summary>
        private void ExecuteMouseLeave()
        {
            // 当鼠标离开时，启动计时器，3秒后隐藏设置面板
            _hideSettingsTimer.Stop();
            _hideSettingsTimer.Start();
        }

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            // 取消注册所有消息处理器
            _messagingService.Unregister(this);
            
            // 停止并释放计时器
            if (_hideSettingsTimer != null)
            {
                _hideSettingsTimer.Stop();
                _hideSettingsTimer.Elapsed -= HideSettingsTimer_Elapsed;
                _hideSettingsTimer.Dispose();
                _hideSettingsTimer = null;
            }
            
            // 清理歌词数据
            ClearLyrics();
            
            // 清理播放列表集合
            PlayedLyrics.Clear();
            UnplayedLyrics.Clear();
        }
    }
}