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
        private readonly ILyricsService _lyricsService;

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
        private System.Windows.TextAlignment _lyricTextAlignment = System.Windows.TextAlignment.Right;
        // 是否启用歌词翻译
        private bool _isLyricTranslationEnabled = true;

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
                    
                    // 同步到所有歌词行
                    foreach (var lyric in Lyrics)
                    {
                        lyric.LyricTextAlignment = value;
                    }
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
                    
                    // 同步到所有歌词行
                    foreach (var lyric in Lyrics)
                    {
                        lyric.LyricFontSize = value;
                        lyric.SelectedLyricFontSize = SelectedLyricFontSize;
                    }
                }
            }
        }

        /// <summary>
        /// 选中歌词字体大小（始终比普通大8）
        /// </summary>
        public double SelectedLyricFontSize
        {
            get => LyricFontSize + 4;
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
                    
                    // 同步到所有歌词行
                    foreach (var lyric in Lyrics)
                    {
                        lyric.IsLyricTranslationEnabled = value;
                    }
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
                    // 清理旧歌曲的资源
                    CleanupOldSongResources(_currentSong);
                    
                    // 先将CurrentSong设置为null，触发UI更新，清空封面
                    _currentSong = null;
                    OnPropertyChanged(nameof(CurrentSong));
                    
                    // 清空ViewModel中的封面资源
                    CurrentSongAlbumArt = null;
                    CurrentSongOriginalAlbumArt = null;
                    
                    // 强制UI更新
                    OnPropertyChanged(nameof(CurrentSongAlbumArt));
                    OnPropertyChanged(nameof(CurrentSongOriginalAlbumArt));
                    
                    // 延迟设置新歌曲，确保UI有足够时间更新
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => 
                    {
                        try
                        {
                            _currentSong = value;
                            OnPropertyChanged(nameof(CurrentSong));

                            // 更新缓存的歌曲信息属性
                            UpdateSongProperties();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 设置新歌曲失败: {ex.Message}");
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }
        
        /// <summary>
        /// 清理旧歌曲的BitmapImage资源
        /// </summary>
        /// <param name="oldSong">旧歌曲对象</param>
        private void CleanupOldSongResources(Core.Models.Song? oldSong)
        {
            try
            {
                if (oldSong != null)
                {
                    System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 清理旧歌曲资源 - {oldSong.Title}");
                    
                    // 清理旧歌曲的BitmapImage资源
                    oldSong.Cleanup();
                    
                    // 清理ViewModel中的BitmapImage资源
                    CurrentSongAlbumArt = null;
                    CurrentSongOriginalAlbumArt = null;
                    
                    System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 旧歌曲资源清理完成");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 清理旧歌曲资源失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新所有歌曲相关属性的缓存值（异步加载专辑封面）
        /// </summary>
        private void UpdateSongProperties()
        {
            if (_currentSong != null)
            {
                // 直接设置属性值，WPF绑定系统会自动处理UI更新
                CurrentSongTitle = _currentSong.Title ?? string.Empty;
                CurrentSongArtist = _currentSong.Artist ?? string.Empty;
                CurrentSongAlbum = _currentSong.Album ?? string.Empty;

                // 异步加载专辑封面（不阻塞UI）
                LoadAlbumArtAsync(_currentSong);
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

        /// <summary>
        /// 异步加载专辑封面
        /// </summary>
        private async void LoadAlbumArtAsync(Core.Models.Song song)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 开始加载专辑封面 - {song.Title}");
                
                await Task.Run(async () => 
                {
                    BitmapImage? albumArt = null;
                    BitmapImage? originalAlbumArt = null;
                    
                    try
                    {
                        // 关键修复：只加载一次封面，然后复用引用
                        albumArt = await AlbumArtLoader.LoadAlbumArtAsync(song.FilePath);
                        
                        // 如果加载失败，使用默认封面
                        if (albumArt == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 封面加载失败，使用默认封面 - {song.Title}");
                            albumArt = await AlbumArtLoader.GetDefaultAlbumArtAsync();
                        }
                        
                        // 复用同一个BitmapImage引用，避免重复加载占用双倍内存
                        originalAlbumArt = albumArt;
                    }
                    catch (Exception loadEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 加载封面失败: {loadEx.Message}");
                        // 使用默认封面
                        albumArt = await AlbumArtLoader.GetDefaultAlbumArtAsync();
                        originalAlbumArt = albumArt;
                    }
                    
                    // 在UI线程更新封面
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                    {
                        try
                        {
                            if (song != null && CurrentSong == song)
                            {
                                // 更新歌曲对象的封面
                                song.AlbumArt = albumArt;
                                song.OriginalAlbumArt = originalAlbumArt;
                                
                                // 更新ViewModel的封面属性
                                CurrentSongAlbumArt = albumArt;
                                CurrentSongOriginalAlbumArt = originalAlbumArt;
                                
                                // 触发属性变更通知
                                OnPropertyChanged(nameof(CurrentSongAlbumArt));
                                OnPropertyChanged(nameof(CurrentSongOriginalAlbumArt));
                                
                                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 异步更新专辑封面完成 - {song.Title}");
                            }
                            else
                            {
                                // 如果歌曲已不再是当前歌曲，释放资源
                                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 歌曲已变更，放弃封面更新");
                                // BitmapImage将在GC时被回收
                            }
                        }
                        catch (Exception uiEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 更新封面UI失败: {uiEx.Message}");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 异步更新专辑封面失败: {ex.Message}");
                
                // 加载失败时使用默认封面
                try
                {
                    var defaultAlbumArt = await AlbumArtLoader.GetDefaultAlbumArtAsync();
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    {
                        if (CurrentSong == song)
                        {
                            CurrentSongAlbumArt = defaultAlbumArt;
                            CurrentSongOriginalAlbumArt = defaultAlbumArt;
                            OnPropertyChanged(nameof(CurrentSongAlbumArt));
                            OnPropertyChanged(nameof(CurrentSongOriginalAlbumArt));
                        }
                    });
                }
                catch (Exception defaultEx)
                {
                    System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 加载默认封面失败: {defaultEx.Message}");
                }
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
            IPlaylistService playlistService,
            ILyricsService lyricsService)
        {
            _messagingService = messagingService;
            _configurationService = configurationService;
            _playerStateService = playerStateService;
            _playlistService = playlistService;
            _lyricsService = lyricsService;

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

            // 从PlayerStateService获取当前播放状态并初始化（异步）
            InitializeFromPlayerStateAsync();
        }

        /// <summary>
        /// 从PlayerStateService获取当前状态并初始化（异步版本）
        /// </summary>
        private async void InitializeFromPlayerStateAsync()
        {
            try
            {
                // 获取当前歌曲并触发属性更新
                var currentSong = _playerStateService.CurrentSong;
                if (currentSong != null)
                {
                    CurrentSong = currentSong;
                    
                    // 异步加载专辑封面（不阻塞UI）
                    await Task.Run(async () => 
                    {
                        try
                        {
                            var albumArt = await AlbumArtLoader.LoadAlbumArtAsync(currentSong.FilePath);
                            var originalAlbumArt = await AlbumArtLoader.LoadAlbumArtAsync(currentSong.FilePath);
                            
                            // 在UI线程更新封面
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                            {
                                if (currentSong != null)
                                {
                                    currentSong.AlbumArt = albumArt;
                                    currentSong.OriginalAlbumArt = originalAlbumArt;
                                    
                                    // 更新ViewModel的封面属性
                                    CurrentSongAlbumArt = albumArt;
                                    CurrentSongOriginalAlbumArt = originalAlbumArt;
                                    System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 异步加载专辑封面完成");
                                }
                            });
                        }
                        catch (Exception artEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 异步加载专辑封面失败: {artEx.Message}");
                        }
                    });

                    // 异步加载歌词（不阻塞UI）
                    await Task.Run(async () => 
                    {
                        try
                        {
                            var lyrics = await _lyricsService.LoadLyricsAsync(currentSong.FilePath);
                            
                            // 在UI线程更新歌词
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                            {
                                SetLyrics(new ObservableCollection<Core.Models.LyricLine>(lyrics));
                                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 异步加载歌词成功，共 {lyrics.Count} 行");
                            });
                        }
                        catch (Exception lyricEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 异步加载歌词失败: {lyricEx.Message}");
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                            {
                                SetLyrics(new ObservableCollection<Core.Models.LyricLine>());
                            });
                        }
                    });
                }

                // 获取播放状态
                IsPlaying = _playerStateService.IsPlaying;

                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 从PlayerStateService异步初始化完成，歌曲: {currentSong?.Title ?? "无"}, 播放状态: {IsPlaying}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 从PlayerStateService异步初始化失败: {ex.Message}");
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
                // 将秒转换为TimeSpan
                var timeSpan = TimeSpan.FromSeconds(currentTime);
                
                // 找到当前时间对应的非空歌词行
                var currentLyric = Lyrics.Where(l => !string.IsNullOrEmpty(l.OriginalText))
                    .LastOrDefault(l => l.Time <= timeSpan);

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
                    
                    // 找到下一句非空歌词行
                    var nextNonEmptyLyric = Lyrics.Skip(currentIndex + 1)
                        .Where(l => !string.IsNullOrEmpty(l.OriginalText))
                        .FirstOrDefault();
                    
                    // 如果是最后一句非空歌词，进度固定为1
                    if (nextNonEmptyLyric == null)
                    {
                        lineDuration = TimeSpan.FromSeconds(5); // 最后一句持续5秒
                    }
                    else
                    {
                        // 计算当前歌词行的播放时长（下一句时间 - 当前句时间）
                        lineDuration = nextNonEmptyLyric.Time - currentLyric.Time;
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
            // 关键修复：先深度清理旧歌词数据，断开所有引用关系
            DeepCleanupLyrics();
            
            // 使用新的歌词集合，避免复用旧的ObservableCollection
            var newLyrics = new ObservableCollection<Core.Models.LyricLine>();
            
            // 添加开头空行（10行），确保第一句歌词能滚动到中心
            for (int i = 0; i < 10; i++)
            {
                var emptyLyric = new Core.Models.LyricLine
                {
                    Time = TimeSpan.MinValue, // 设置最小时间，确保在最前面
                    OriginalText = string.Empty,
                    TranslatedText = string.Empty,
                    LyricFontSize = LyricFontSize,
                    SelectedLyricFontSize = SelectedLyricFontSize,
                    LyricTextAlignment = LyricTextAlignment,
                    IsLyricTranslationEnabled = IsLyricTranslationEnabled
                };
                newLyrics.Add(emptyLyric);
            }
            
            // 添加原始歌词
            foreach (var lyric in lyrics)
            {
                // 同步歌词显示设置（值类型复制，不建立引用关系）
                lyric.LyricFontSize = LyricFontSize;
                lyric.SelectedLyricFontSize = SelectedLyricFontSize;
                lyric.LyricTextAlignment = LyricTextAlignment;
                lyric.IsLyricTranslationEnabled = IsLyricTranslationEnabled;
                
                newLyrics.Add(lyric);
            }
            
            // 添加结尾空行（10行），确保最后一句歌词能滚动到中心
            for (int i = 0; i < 10; i++)
            {
                var emptyLyric = new Core.Models.LyricLine
                {
                    Time = TimeSpan.MaxValue, // 设置最大时间，确保在最后面
                    OriginalText = string.Empty,
                    TranslatedText = string.Empty,
                    LyricFontSize = LyricFontSize,
                    SelectedLyricFontSize = SelectedLyricFontSize,
                    LyricTextAlignment = LyricTextAlignment,
                    IsLyricTranslationEnabled = IsLyricTranslationEnabled
                };
                newLyrics.Add(emptyLyric);
            }
            
            // 替换整个集合，确保旧集合可以被GC回收
            _lyrics = newLyrics;

            // 通知歌词已更新，触发滚动重置
            OnPropertyChanged(nameof(Lyrics));
            
            // 如果有歌词，设置第一句非空歌词为当前歌词
            if (Lyrics.Count > 0)
            {
                // 找到第一句非空歌词
                var firstNonEmptyLyric = Lyrics.FirstOrDefault(l => !string.IsNullOrEmpty(l.OriginalText));
                if (firstNonEmptyLyric != null)
                {
                    CurrentLyricLine = firstNonEmptyLyric;
                }
                else
                {
                    CurrentLyricLine = null;
                }
            }
            else
            {
                CurrentLyricLine = null;
            }
        }
        
        /// <summary>
        /// 深度清理歌词集合，断开所有引用关系
        /// </summary>
        private void DeepCleanupLyrics()
        {
            if (_lyrics == null || _lyrics.Count == 0)
                return;
                
            System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 开始深度清理 {Lyrics.Count} 行歌词");
            
            // 重置每行歌词的状态，断开与UI的绑定关系
            foreach (var lyric in _lyrics)
            {
                try
                {
                    // 重置所有可绑定的属性为默认值
                    lyric.Progress = 0;
                    // 注意：不要清空OriginalText和TranslatedText，因为它们是歌词内容
                    // 这些属性会在新歌曲加载时被覆盖
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 清理歌词行时出错: {ex.Message}");
                }
            }
            
            // 清空集合
            _lyrics.Clear();
            
            System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 歌词集合深度清理完成");
        }

        /// <summary>
        /// 清空歌词
        /// </summary>
        public void ClearLyrics()
        {
            System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 开始清空歌词");
            
            // 关键修复：先深度清理，断开所有绑定引用
            DeepCleanupLyrics();
            
            // 创建新的集合替换旧集合，确保旧集合可以被GC回收
            _lyrics = new ObservableCollection<Core.Models.LyricLine>();
            OnPropertyChanged(nameof(Lyrics));
            
            CurrentLyricLine = null;
            
            System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 歌词已清空");
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
            System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 开始执行Cleanup方法");
            
            // 关键修复：先深度清理歌词，断开所有绑定引用
            DeepCleanupLyrics();
            
            // 取消注册所有消息处理器
            _messagingService.Unregister(this);
            System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 已取消所有消息注册");
            
            // 停止并释放计时器
            if (_hideSettingsTimer != null)
            {
                _hideSettingsTimer.Stop();
                _hideSettingsTimer.Elapsed -= HideSettingsTimer_Elapsed;
                _hideSettingsTimer.Dispose();
                _hideSettingsTimer = null;
                System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 已释放计时器");
            }
            
            // 清理歌词数据
            ClearLyrics();
            
            // 清理播放列表集合
            PlayedLyrics.Clear();
            UnplayedLyrics.Clear();
            
            // 清理当前歌曲引用
            _currentSong = null;
            CurrentLyricLine = null;
            
            // 清理BitmapImage资源
            CleanupBitmapResources();
            
            System.Diagnostics.Debug.WriteLine("CenterContentViewModel: Cleanup方法执行完成");
        }
        
        /// <summary>
        /// 清理BitmapImage资源
        /// </summary>
        private void CleanupBitmapResources()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 开始清理BitmapImage资源");
                
                // 清空ViewModel中的封面资源
                if (CurrentSongAlbumArt != null)
                {
                    CurrentSongAlbumArt = null;
                    System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 已清空CurrentSongAlbumArt");
                }
                
                if (CurrentSongOriginalAlbumArt != null)
                {
                    CurrentSongOriginalAlbumArt = null;
                    System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 已清空CurrentSongOriginalAlbumArt");
                }
                
                // 触发属性变更通知
                OnPropertyChanged(nameof(CurrentSongAlbumArt));
                OnPropertyChanged(nameof(CurrentSongOriginalAlbumArt));
                
                // 清理CurrentSong中的BitmapImage资源
                if (_currentSong != null)
                {
                    System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 清理CurrentSong资源 - {_currentSong.Title}");
                    _currentSong.Cleanup();
                }
                
                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                System.Diagnostics.Debug.WriteLine("CenterContentViewModel: 已完成BitmapImage资源清理和垃圾回收");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CenterContentViewModel: 清理BitmapImage资源失败: {ex.Message}");
            }
        }
    }
}