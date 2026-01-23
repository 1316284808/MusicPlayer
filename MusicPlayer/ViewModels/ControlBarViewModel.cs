using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Navigation;
using MusicPlayer.Page;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MusicPlayer.ViewModels
{


    /// <summary>
    /// 控制栏视图模型 - 负责播放控制相关逻辑
    /// 使用PlayerStateService作为播放状态的唯一可信源，避免数据重复
    /// </summary>
    public class ControlBarViewModel : ObservableObject, IControlBarViewModel
    {
        private readonly IMessagingService _messagingService;
        private readonly IPlayerStateService _playerStateService;
        private readonly IConfigurationService _configurationService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaylistViewModel _playlistViewModel;
        private readonly IPlaybackContextService _playbackContextService;
        private bool _isUserDragging = false; // 标记用户是否正在拖拽进度条
        private double _dragPosition = 0.0; // 拖动过程中的临时位置
        private bool _isVolumeFlyoutOpen = false; // 音量弹出层打开状态
        private string _togglePlayerPageToolTip = "进入播放页"; // 控制按钮的提示文本
        private bool _isPlayerPage = false; // 当前是否在PlayerPage

        // 播放列表视图模型
        public IPlaylistViewModel PlaylistViewModel => _playlistViewModel;

        // 基于当前播放上下文的播放队列
        private ObservableCollection<Core.Models.Song> _currentContextPlaylist = new();
        public ObservableCollection<Core.Models.Song> CurrentContextPlaylist
        {
            get => _currentContextPlaylist;
            set
            {
                if (_currentContextPlaylist != value)
                {
                    _currentContextPlaylist = value;
                    OnPropertyChanged();
                    // 更新播放队列标题
                    OnPropertyChanged(nameof(QueueTitle));
                    OnPropertyChanged(nameof(SongCount));
                }
            }
        }
        /// <summary>
        /// 播放队列计数
        /// </summary>
        public int SongCount
        {
            get => CurrentContextPlaylist.Count;
        }
        
        /// <summary>
        /// 播放队列标题，根据当前播放上下文动态生成
        /// </summary>
        public string QueueTitle
        {
            get
            {
                try
                {
                    // 获取当前播放上下文
                    var context = _playbackContextService.CurrentPlaybackContext;
                    
                    switch (context.Type)
                    {
                        case Core.Enums.PlaybackContextType.DefaultPlaylist:
                            return "正在播放[默认列表]";
                        case Core.Enums.PlaybackContextType.Favorites:
                            return "正在播放[收藏列表]";
                        case Core.Enums.PlaybackContextType.Artist:
                            return $"正在播放[歌手:{context.Identifier}]";
                        case Core.Enums.PlaybackContextType.Album:
                            return $"正在播放[专辑:{context.Identifier}]";
                        default:
                            return "正在播放[默认列表]";
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 生成播放队列标题失败: {ex.Message}");
                    return "正在播放[默认列表]";
                }
            }
        }

        public bool IsUserDragging
        {
            get => _isUserDragging;
            set
            {
                if (_isUserDragging != value)
                {
                    _isUserDragging = value;
                    System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 拖拽状态变更为 {_isUserDragging}");

                    // 通知PlayerStateService用户拖动状态变化
                    if (_playerStateService is PlayerStateService playerStateService)
                    {
                        playerStateService.SetUserDragging(_isUserDragging);
                    }

                    // 如果结束拖动，将临时位置应用到实际播放位置
                    if (!_isUserDragging)
                    {
                        System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 拖动结束，应用位置 {_dragPosition}");
                        _messagingService.Send(new SeekMessage(_dragPosition));
                    }

                    OnPropertyChanged();
                }
            }
        }

        // 播放状态属性 - 统一使用 PlayerStateService 作为唯一可信源
        public bool IsPlaying => _playerStateService.IsPlaying;
        public bool IsMuted => _playerStateService.IsMuted;
        public float Volume
        {
            get => _playerStateService.Volume;
            set
            {
                _playerStateService.Volume = value;
            }
        }

        // 音量图标类型属性 - 已移除，改用Converter处理UI逻辑
        /// <summary>
        /// 控制按钮的提示文本
        /// </summary>
        public string TogglePlayerPageToolTip
        {
            get => _togglePlayerPageToolTip;
            set
            {
                if (_togglePlayerPageToolTip != value)
                {
                    _togglePlayerPageToolTip = value;
                    OnPropertyChanged(nameof(TogglePlayerPageToolTip));
                }
            }
        }

        /// <summary>
        /// 当前是否在PlayerPage
        /// </summary>
        public bool IsPlayerPage
        {
            get => _isPlayerPage;
            set
            {
                if (_isPlayerPage != value)
                {
                    _isPlayerPage = value;
                    OnPropertyChanged(nameof(IsPlayerPage));
                }
            }
        }

        public double CurrentPosition
        {
            get
            {
                // 如果用户正在拖动，返回临时位置
                if (_isUserDragging)
                {
                    return _dragPosition;
                }

                // 否则返回实际播放位置
                return _playerStateService.CurrentPosition;
            }
            set
            {
                // 如果用户正在拖动，更新临时位置而不是实际播放位置
                if (_isUserDragging)
                {
                    _dragPosition = Math.Max(0, Math.Min(_playerStateService.MaxPosition, value)); 
                    // 只通知UI更新，不发送SeekMessage
                    OnPropertyChanged();
                }
                else
                {
                    // 如果不在拖拽状态，当UI更新时我们也需要更新PlayerStateService
                    // 但要避免循环调用
                    if (Math.Abs(_playerStateService.CurrentPosition - value) > 0.01)
                    {
                        System.Diagnostics.Debug.WriteLine($"UI更新进度位置到: {value}");
                        if (_playerStateService is PlayerStateService playerStateService)
                        {
                            playerStateService.SetPositionByUser(value);
                        }
                        else
                        {
                            _playerStateService.CurrentPosition = value;
                        }
                    }
                }
            }
        }
        public double MaxPosition => _playerStateService.MaxPosition;

        // 当前歌曲属性 - 需要有自己的属性来支持UI更新
        private Core.Models.Song? _currentSong;
        public Core.Models.Song? CurrentSong
        {
            get => _currentSong;
            set
            {
                if (_currentSong != value)
                {
                    _currentSong = value;
                    OnPropertyChanged(nameof(CurrentSong));
                }
            }
        }

        private PlayMode _currentPlayMode;

        public PlayMode CurrentPlayMode
        {
            get => _currentPlayMode;
            private set
            {
                if (_currentPlayMode != value)
                {
                    _currentPlayMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _currentTimeText = "00:00";
        private string _totalTimeText = "00:00";





        public string CurrentTimeText
        {
            get => _currentTimeText;
            set
            {
                _currentTimeText = value;
                OnPropertyChanged(nameof(CurrentTimeText));
            }
        }

        public string TotalTimeText
        {
            get => _totalTimeText;
            set
            {
                _totalTimeText = value;
                OnPropertyChanged(nameof(TotalTimeText));
            }
        }

        /// <summary>
        /// 播放模式文本描述
        /// </summary>
        public string PlayModeText => CurrentPlayMode switch
        {
            PlayMode.RepeatOne => "单曲循环",
            PlayMode.RepeatAll => "列表循环",
            PlayMode.Shuffle => "随机播放",
            _ => "顺序播放"
        };

        // 为了解决绑定刷新问题，添加缓存的单独属性
        private string _currentSongTitle = string.Empty;
        private string _currentSongArtist = string.Empty;
        private BitmapImage? _currentSongAlbumArt;

        public string CurrentSongTitle
        {
            get => _currentSongTitle;
            private set
            {
                _currentSongTitle = value;
                OnPropertyChanged(nameof(CurrentSongTitle));
            }
        }

        public string CurrentSongArtist
        {
            get => _currentSongArtist;
            private set
            {
                _currentSongArtist = value;
                OnPropertyChanged(nameof(CurrentSongArtist));
            }
        }

        public BitmapImage? CurrentSongAlbumArt
        {
            get => _currentSongAlbumArt;
            private set
            {
                _currentSongAlbumArt = value;
                OnPropertyChanged(nameof(CurrentSongAlbumArt));
            }
        }

        // 音量弹出层打开状态
        public bool IsVolumeFlyoutOpen
        {
            get => _isVolumeFlyoutOpen;
            set
            {
                if (_isVolumeFlyoutOpen != value)
                {
                    _isVolumeFlyoutOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        // 播放队列弹出层打开状态
        private bool _isQueueFlyoutOpen = false;
        public bool IsQueueFlyoutOpen
        {
            get => _isQueueFlyoutOpen;
            set
            {
                if (_isQueueFlyoutOpen != value)
                {
                    _isQueueFlyoutOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        // 当前歌曲收藏状态
        public bool IsCurrentSongFavorited
        {
            get => _currentSong?.Heart ?? false;
        }

        // 当前播放列表项
        private Core.Models.Song? _currentPlaylistItem;
        public Core.Models.Song? CurrentPlaylistItem
        {
            get => _currentPlaylistItem;
            set
            {
                if (_currentPlaylistItem != value)
                {
                    _currentPlaylistItem = value;
                    OnPropertyChanged();
                    // 当选中歌曲变化时，播放该歌曲
                    if (_currentPlaylistItem != null)
                    {
                        ExecutePlaySelectedSong(_currentPlaylistItem);
                    }
                }
            }
        }

        // 命令定义
        public ICommand PlayPauseCommand { get; }
        public ICommand ShowLyricsCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand MuteCommand { get; }
        public ICommand ShowVolumePopupCommand { get; }
        public ICommand ToggleVolumePopupCommand { get; }
        public ICommand TogglePlayModeCommand { get; }
        public ICommand TogglePlayerPageCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand ToggleQueueFlyoutCommand { get; }
        public ICommand PlaySelectedSongCommand { get; }


        private readonly NavigationService _navigationService;

        public ControlBarViewModel(IMessagingService messagingService, IPlayerStateService playerStateService, NavigationService navigationService, IConfigurationService configurationService, IPlaylistDataService playlistDataService, IPlaylistViewModel playlistViewModel, IPlaybackContextService playbackContextService)
        {
            // 添加实例ID日志，用于调试单例问题
            System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 创建新实例，ID: {GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: PlayerStateService实例ID: {playerStateService.GetHashCode()}");
            
            _messagingService = messagingService;
            _playerStateService = playerStateService;
            _navigationService = navigationService;
            _configurationService = configurationService;
            _playlistDataService = playlistDataService;
            _playlistViewModel = playlistViewModel;
            _playbackContextService = playbackContextService;
            
            // 初始化当前上下文播放队列
            UpdateCurrentContextPlaylist();

            // 初始化命令
            PlayPauseCommand = new RelayCommand(ExecutePlayPause);
            ShowLyricsCommand = new RelayCommand(ExecuteShowLyrics);
            NextCommand = new RelayCommand(ExecuteNext);
            PreviousCommand = new RelayCommand(ExecutePrevious);
            MuteCommand = new RelayCommand(ExecuteMute);
            ShowVolumePopupCommand = new RelayCommand(ExecuteShowVolumePopup);
            ToggleVolumePopupCommand = new RelayCommand(ExecuteToggleVolumePopup);
            TogglePlayModeCommand = new RelayCommand(ExecuteTogglePlayMode);
            TogglePlayerPageCommand = new RelayCommand(ExecuteTogglePlayerPage);
            ToggleFavoriteCommand = new RelayCommand(ExecuteToggleFavorite);
            ToggleQueueFlyoutCommand = new RelayCommand(ExecuteToggleQueueFlyout);
            PlaySelectedSongCommand = new RelayCommand<Core.Models.Song>(ExecutePlaySelectedSong);

            // 注册消息处理器 - 通过消息系统接收时间更新
            RegisterMessageHandlers();

            // 初始化时间显示
            CurrentTimeText = FormatTime(TimeSpan.FromSeconds(CurrentPosition));
            TotalTimeText = FormatTime(TimeSpan.FromSeconds(MaxPosition));

            // 初始化拖动临时位置
            _dragPosition = _playerStateService.CurrentPosition;

            // 初始化播放模式
            CurrentPlayMode = _playerStateService.CurrentPlayMode;
            OnPropertyChanged(nameof(PlayModeText));
            
            // 初始化页面状态 - 检查当前是否在PlayerPage
            IsPlayerPage = _navigationService.CurrentPageType == typeof(PlayerPage);
            System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 初始化页面状态，当前页面: {_navigationService.CurrentPageType?.Name}, IsPlayerPage: {IsPlayerPage}");
            
            // 初始化按钮提示文本，根据当前页面状态
            if (_navigationService.CurrentPageType == typeof(PlayerPage))
            {
                TogglePlayerPageToolTip = "返回上一页";
            }
            else
            {
                TogglePlayerPageToolTip = "进入播放页";
            }
            
            // 检查是否需要恢复最后播放的歌曲信息
            RestoreLastPlayedSongInfo();
        }
        
        /// <summary>
        /// 恢复最后播放的歌曲信息到ControlBar
        /// </summary>
        private void RestoreLastPlayedSongInfo()
        {
            try
            {
                // 获取最后播放的歌曲ID
                var lastPlayedSongId = _configurationService.CurrentConfiguration.LastPlayedSongId;
                
                // 如果ID无效（-1表示无记录），则不进行恢复
                if (lastPlayedSongId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 最后播放歌曲ID无效({lastPlayedSongId})，不需要恢复");
                    return;
                }
                
                // 如果当前已经有一首歌，不需要恢复
                if (_playerStateService.CurrentSong != null)
                {
                    System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 当前已有歌曲({_playerStateService.CurrentSong.Title})，不需要恢复");
                    return;
                }
                
                // 从播放列表数据服务中查找指定ID的歌曲
                var playlist = _playlistDataService.DataSource;
                var song = playlist.FirstOrDefault(s => s.Id == lastPlayedSongId);
                
                // 清除临时数据缓存
                _playlistDataService.ClearDataSource();
                
                if (song != null)
                {
                    // 更新ControlBar的歌曲信息，但不触发播放
                    CurrentSong = song;
                    UpdateControlBarSongProperties();
                    
                    // 同步到PlayerStateService和PlaylistDataService，但不发送消息（避免触发自动播放）
                    _playerStateService.UpdateCurrentSong(song);
                    //_playlistDataService.SetCurrentSongWithoutNotification(song);
                    
                    System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 已恢复最后播放歌曲信息到ControlBar - {song.Title}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 未找到ID为{lastPlayedSongId}的歌曲，无法恢复");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 恢复最后播放歌曲信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册消息处理器 - 只处理UI相关的消息
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 当前歌曲变化消息
            _messagingService.Register<CurrentSongChangedMessage>(this, (r, m) =>
            {
                // 直接设置属性，WPF绑定系统会自动处理UI更新
                CurrentSong = m.Value;
                UpdateControlBarSongProperties();
                OnPropertyChanged(nameof(CurrentTimeText));
                OnPropertyChanged(nameof(TotalTimeText));
                OnPropertyChanged(nameof(CurrentPosition)); // 确保滑块位置更新

                // 重置拖动临时位置
                _dragPosition = _playerStateService.CurrentPosition;
            });

            // 播放状态变化消息
            _messagingService.Register<PlaybackStateChangedMessage>(this, (r, m) =>
            {
                OnPropertyChanged(nameof(IsPlaying));
            });

            // 静音状态变化消息（不再需要，因为静音按钮直接设置音量）
            // 保留消息处理，但只记录日志，不执行实际逻辑
            _messagingService.Register<MuteStateChangedMessage>(this, (r, m) =>
            {
                System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 收到静音状态变化消息，静音状态: {m.Value} (已忽略，使用直接音量设置)");
            });

            // 音量变化消息
            _messagingService.Register<VolumeChangedMessage>(this, (r, m) =>
            {
                OnPropertyChanged(nameof(Volume));
            });

            // 播放进度变化 - 用于更新时间显示和进度条位置
            _messagingService.Register<PlaybackProgressChangedMessage>(this, (r, m) =>
            {
                CurrentTimeText = FormatTime(TimeSpan.FromSeconds(m.Value));

                // 通知UI更新CurrentPosition
                OnPropertyChanged(nameof(CurrentPosition));
            });

            // 最大播放位置变化
            _messagingService.Register<MaxPositionChangedMessage>(this, (r, m) =>
            {
                TotalTimeText = FormatTime(TimeSpan.FromSeconds(m.Value));
                OnPropertyChanged(nameof(MaxPosition));
            });

            // 播放模式变化
            _messagingService.Register<PlayModeChangedMessage>(this, (r, m) =>
            {
                System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 收到播放模式变化消息: {m.Value}");
                CurrentPlayMode = m.Value;  // 直接使用消息中的值更新本地属性
                OnPropertyChanged(nameof(PlayModeText));
            });
            
            // 导航完成消息 - 用于更新按钮提示文本和页面状态
            _messagingService.Register<NavigationCompletedMessage>(this, (r, m) =>
            {
                // 根据当前页面更新按钮提示文本和页面状态
                if (m.Value == typeof(PlayerPage))
                {
                    // 在PlayerPage时，点击按钮会返回上一页
                    TogglePlayerPageToolTip = "返回上一页";
                    IsPlayerPage = true;
                }
                else
                {
                    // 不在PlayerPage时，点击按钮会进入播放页
                    TogglePlayerPageToolTip = "进入播放页";
                    IsPlayerPage = false;
                }
            });

            // 歌曲收藏状态变化消息 - 更新UI
            _messagingService.Register<PlaylistDataChangedMessage>(this, (r, m) =>
            {
                // 如果是歌曲更新，且是当前歌曲，更新收藏状态
                if (m.Type == DataChangeType.SongUpdated && _currentSong != null)
                {
                    var updatedSong = m.Data.FirstOrDefault(s => s.FilePath == _currentSong.FilePath);
                    if (updatedSong != null)
                    {
                        // 只在状态确实不同时更新
                        if (updatedSong.Heart != _currentSong.Heart)
                        {
                            _currentSong.Heart = updatedSong.Heart;
                            OnPropertyChanged(nameof(IsCurrentSongFavorited));
                            System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 收藏状态更新 - {_currentSong.Title}, 状态: {_currentSong.Heart}");
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 更新控制栏的歌曲属性
        /// </summary>
        private void UpdateControlBarSongProperties()
        {
            if (CurrentSong != null)
            {
                CurrentSongTitle = CurrentSong.Title ?? string.Empty;
                CurrentSongArtist = CurrentSong.Artist ?? string.Empty;

                // 确保封面已加载
                CurrentSong.AlbumArt = AlbumArtLoader.LoadAlbumArt(CurrentSong.FilePath);

                // 延迟更新封面，确保加载完成
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CurrentSongAlbumArt = CurrentSong.AlbumArt;
                }));

                // 更新收藏状态相关属性
                OnPropertyChanged(nameof(IsCurrentSongFavorited));
            }
            else
            {
                CurrentSongTitle = string.Empty;
                CurrentSongArtist = string.Empty;
                CurrentSongAlbumArt = null;
                
                // 重置收藏状态相关属性
                OnPropertyChanged(nameof(IsCurrentSongFavorited));
            }
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
        /// 执行显示桌面歌词命令
        /// </summary>
        private void ExecuteShowLyrics()
        {
            // 发送显示桌面歌词消息
            _messagingService.Send<ShowLyricsMessage, bool>(new ShowLyricsMessage());
        }

        /// <summary>
        /// 执行下一首命令
        /// </summary>
        private void ExecuteNext()
        {
            // 发送下一首消息
            _messagingService.Send(new NextSongMessage());
        }

        /// <summary>
        /// 执行上一首命令
        /// </summary>
        private void ExecutePrevious()
        {
            // 发送上一首消息
            _messagingService.Send(new PreviousSongMessage());
        }

        /// <summary>
        /// 执行静音/取消静音命令 - 直接设置音量为0
        /// </summary>
        private void ExecuteMute()
        {
            // 直接设置音量为0，音量滑轨会自动更新图标
            Volume = 0f;
            System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 静音按钮点击，设置音量到0");
        }

        /// <summary>
        /// 显示音量弹出层
        /// </summary>
        private void ExecuteShowVolumePopup()
        {
            // 此方法现在由 Flyout 处理，不需要额外的逻辑
            // Flyout 会自动处理显示/隐藏
            System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 显示音量弹出层");
        }

        /// <summary>
        /// 切换音量弹出层的显示状态
        /// </summary>
        private void ExecuteToggleVolumePopup()
        {
            // 切换弹出层的显示状态
            IsVolumeFlyoutOpen = !IsVolumeFlyoutOpen;
            System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 切换音量弹出层状态为: {IsVolumeFlyoutOpen}");
        }

        /// <summary>
        /// 执行切换播放模式命令
        /// </summary>
        private void ExecuteTogglePlayMode()
        {
            // 发送切换播放模式消息
            _messagingService.Send(new TogglePlayModeMessage());
        }

        /// <summary>
        /// 执行切换播放器页面命令
        /// </summary>
        private void ExecuteTogglePlayerPage()
        {
            try
            {
                // 根据当前页面类型决定导航目标
                if (_navigationService.CurrentPageType == typeof(PlayerPage))
                {
                    // 如果当前是PlayerPage，返回上一页
                    if (_navigationService.CanGoBack())
                    {
                        _navigationService.GoBack();
                    }
                    else
                    {
                        // 如果没有上一页可返回，则导航到PlaylistPage
                        _navigationService.NavigateToPlaylist();
                    }
                }
                else
                {
                    // 否则导航到PlayerPage
                    _navigationService.NavigateToPlayer();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"切换播放器页面失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行切换收藏状态命令
        /// </summary>
        private void ExecuteToggleFavorite()
        {
            try
            {
                // 确保有当前歌曲
                if (_currentSong != null)
                {
                    // 切换收藏状态
                    var newFavoriteStatus = !_currentSong.Heart;
                    
                    // 立即更新本地状态以提供即时UI反馈
                    _currentSong.Heart = newFavoriteStatus;
                    OnPropertyChanged(nameof(IsCurrentSongFavorited));
                    
                    // 发送更新歌曲收藏状态消息
                    _messagingService.Send<UpdateSongFavoriteStatusMessage, bool>(
                        new UpdateSongFavoriteStatusMessage(_currentSong, newFavoriteStatus));
                    
                    System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 切换歌曲收藏状态 - {_currentSong.Title}, 新状态: {newFavoriteStatus}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ControlBarViewModel: 没有当前歌曲，无法切换收藏状态");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 切换收藏状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换播放队列弹出层的显示状态
        /// </summary>
        private void ExecuteToggleQueueFlyout()
        {
            // 切换播放队列弹出层的显示状态
            IsQueueFlyoutOpen = !IsQueueFlyoutOpen;
            
            // 每次打开时更新当前上下文播放队列
            if (IsQueueFlyoutOpen)
            {
                UpdateCurrentContextPlaylist();
            }
            
            System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 切换播放队列弹出层状态为: {IsQueueFlyoutOpen}");
        }
        
        /// <summary>
        /// 更新当前播放上下文的播放队列
        /// </summary>
        private void UpdateCurrentContextPlaylist()
        {
            try
            {
                // 获取当前播放上下文
                var context = _playbackContextService.CurrentPlaybackContext;
                System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 正在更新当前上下文播放队列，上下文类型: {context.Type}, 标识符: {context.Identifier}");
                
                // 获取当前播放列表数据
                var allSongs = _playlistDataService.DataSource;
                
                // 根据上下文类型过滤歌曲
                var filteredSongs = new List<Core.Models.Song>();
                
                switch (context.Type)
                {
                    case Core.Enums.PlaybackContextType.DefaultPlaylist:
                        // 默认列表：所有未删除的歌曲
                        filteredSongs = allSongs.Where(s => !s.IsDeleted).ToList();
                        break;
                    case Core.Enums.PlaybackContextType.Artist:
                        // 歌手列表：所有属于当前歌手且未删除的歌曲
                        filteredSongs = allSongs.Where(s => s.Artist == context.Identifier && !s.IsDeleted).ToList();
                        break;
                    case Core.Enums.PlaybackContextType.Album:
                        // 专辑列表：所有属于当前专辑且未删除的歌曲
                        filteredSongs = allSongs.Where(s => s.Album == context.Identifier && !s.IsDeleted).ToList();
                        break;
                    default:
                        // 默认情况：所有未删除的歌曲
                        filteredSongs = allSongs.Where(s => !s.IsDeleted).ToList();
                        break;
                }
                
                // 更新播放队列
                CurrentContextPlaylist = new ObservableCollection<Core.Models.Song>(filteredSongs);
                System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 当前上下文播放队列已更新，共包含 {filteredSongs.Count} 首歌曲");
                
                // 清除临时数据缓存
                _playlistDataService.ClearDataSource();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 更新当前上下文播放队列失败: {ex.Message}");
            }
        }
         

        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public override void Initialize()
        {
            // ControlBarViewModel的初始化逻辑
        }

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            // 取消注册所有消息处理器
            //_messagingService.Unregister(this);

 
        }



        /// <summary>
        /// 执行播放选中歌曲命令
        /// </summary>
        /// <param name="song">选中的歌曲对象</param>
        private void ExecutePlaySelectedSong(Core.Models.Song? song)
        {
            if (song != null)
            {
                // 发送播放选中歌曲消息
                _messagingService.Send(new Services.Messages.PlaySelectedSongMessage(song));
                System.Diagnostics.Debug.WriteLine($"ControlBarViewModel: 播放选中歌曲 - {song.Title}");
            }
        }

        private static string FormatTime(TimeSpan time)
        {
            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }
    }
}