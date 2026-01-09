using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Enums;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages; // 引入新的消息类型

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 专辑加载请求事件参数
    /// </summary>
    public class AlbumLoadRequestEventArgs : EventArgs
    {
        public Song Song { get; }

        public AlbumLoadRequestEventArgs(Song song)
        {
            Song = song;
        }
    }

/// <summary>
/// 播放列表视图模型 - UI层
/// 
/// 数据流设计：
/// 1. 初始化时：清空本地集合，确保不维护重复缓存
/// 2. 数据访问：通过PlaylistDataService.DataSource获取临时数据，使用后立即清空
/// 3. UI显示：仅维护FilteredPlaylist用于UI绑定
/// 
/// 注意：不应维护本地播放列表缓存，始终通过数据服务获取最新数据
/// </summary>
    public class PlaylistViewModel : ObservableObject, IPlaylistViewModel
    {
        private readonly IMessagingService _messagingService;
        private readonly IConfigurationService? _configurationService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaybackContextService _playbackContextService;
        
        // 本地状态通过消息同步
        // 注意：_playlist已移除，使用FilteredPlaylist作为主要的数据集合
        private readonly ObservableCollection<Song> _filteredPlaylist = new();
        private string _searchText = string.Empty;
        private Song? _currentPlaylistItem;
        private Song? _currentSong;
        private bool _isSearchExpanded = false;
        
        // 过滤模式
        private enum FilterMode
        {
            All,
            Favorites
        }
        
        private FilterMode _currentFilterMode = FilterMode.All;

        

        /// <summary>
        /// 专辑加载请求事件
        /// </summary>
        public event EventHandler<AlbumLoadRequestEventArgs>? AlbumLoadRequested;

        public Song? CurrentSong
        {
            get => _currentSong;
            set
            {
                if (_currentSong != value)
                {
                    _currentSong = value;
                    OnPropertyChanged(nameof(CurrentSong));
                    
                    // 确保在设置歌曲时，专辑封面已加载（懒加载）
                    if (_currentSong != null)
                    {
                        _currentSong.EnsureAlbumArtLoaded();
                    }
                }
            }
        }

        /// <summary>
        /// 过滤后的播放列表
        /// </summary>
        public ObservableCollection<Song> FilteredPlaylist => _filteredPlaylist;

        /// <summary>
        /// 播放列表歌曲总数
        /// </summary>
        public int SongCount => _filteredPlaylist.Count;

        /// <summary>
        /// 播放列表标题（根据当前过滤模式显示）
        /// </summary>
        public string PlaylistTitle => _currentFilterMode == FilterMode.Favorites ? "收藏列表" : "默认列表";

        /// <summary>
        /// 当前播放列表选中项
        /// </summary>
        public Song? CurrentPlaylistItem
        {
            get => _currentPlaylistItem;
            set
            {
                if (_currentPlaylistItem != value)
                {
                    _currentPlaylistItem = value;
                    OnPropertyChanged(nameof(CurrentPlaylistItem));
                    
                    // 注意：现在左键单击不触发播放，只更新选中项
                    // 播放由双击事件处理
                }
            }
        }

        /// <summary>
        /// 排序选项列表
        /// </summary>
        public ObservableCollection<SortOption> SortOptions { get; } = new ObservableCollection<SortOption>
        {
            new SortOption { Display = "添加时间", Value = SortRule.ByAddedTime },
            new SortOption { Display = "标题", Value = SortRule.ByTitle },
            new SortOption { Display = "艺术家", Value = SortRule.ByArtist },
            new SortOption { Display = "专辑", Value = SortRule.ByAlbum },
            new SortOption { Display = "时长", Value = SortRule.ByDuration },
            new SortOption { Display = "文件大小", Value = SortRule.ByFileSize }
        };

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    
                    // 更新过滤后的播放列表
                    UpdateFilteredPlaylist();
                }
            }
        }

        /// <summary>
        /// 搜索框是否展开
        /// </summary>
        public bool IsSearchExpanded
        {
            get => _isSearchExpanded;
            set
            {
                if (_isSearchExpanded != value)
                {
                    _isSearchExpanded = value;
                    OnPropertyChanged(nameof(IsSearchExpanded));
                }
            }
        }

        /// <summary>
        /// 播放选中歌曲命令
        /// </summary>
        public ICommand PlaySelectedSongCommand { get; }

        /// <summary>
        /// 删除选中歌曲命令（逻辑删除）
        /// </summary>
        public ICommand DeleteSelectedSongCommand { get; }

        /// <summary>
        /// 切换歌曲收藏状态命令
        /// </summary>
        public ICommand ToggleSongHeartCommand { get; }

        /// <summary>
        /// 清空播放列表命令
        /// </summary>
        public ICommand ClearPlaylistCommand { get; }

        /// <summary>
        /// 添加播放列表
        /// </summary>
        public ICommand AddMusicCommand { get; }

        /// <summary>
        /// 搜索按钮点击命令
        /// </summary>
        public ICommand SearchButtonClickCommand { get; }

        /// <summary>
        /// 滚动到当前播放歌曲命令
        /// </summary>
        public ICommand ScrollToCurrentSongCommand { get; }

        

        /// <summary>
        /// 排序命令
        /// </summary>
        public ICommand SortCommand { get; }
        
        /// <summary>
        /// 选择排序选项命令
        /// </summary>
        public ICommand SelectSortOptionCommand { get; } 

        

        private SortRule _currentSortRule = SortRule.ByAddedTime;

        /// <summary>
        /// 当前排序规则
        /// </summary>
        public SortRule CurrentSortRule
        {
            get => _playlistDataService.CurrentSortRule;
            set
            {
                if (_playlistDataService.CurrentSortRule != value)
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 用户选择排序规则: {value}");
                    
                    //确保数据源正确更新
                    _messagingService.Send(new SortPlaylistMessage(value));
                    
                    // 更新UI属性
                    OnPropertyChanged(nameof(CurrentSortRule));
                    OnPropertyChanged(nameof(SortRuleText));
                }
            }
        }

        /// <summary>
        /// 排序规则文本描述
        /// </summary>
        public string SortRuleText => CurrentSortRule switch
        {
            SortRule.ByAddedTime => "添加时间",
            SortRule.ByTitle => "标题",
            SortRule.ByArtist => "艺术家",
            SortRule.ByAlbum => "专辑",
            SortRule.ByDuration => "时长",
            SortRule.ByFileSize => "文件大小",
            _ => "添加时间"
        };

        private SortOption? _selectedSortOption;

        /// <summary>
        /// 选中的排序选项
        /// </summary>
        public SortOption? SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (_selectedSortOption != value)
                {
                    _selectedSortOption = value;
                    OnPropertyChanged(nameof(SelectedSortOption));
                    OnPropertyChanged(nameof(SortRuleText));
                    // 更新排序规则
                    if (value != null)
                    {
                        CurrentSortRule = value.Value;
                    }
                }
            }
        }

        public PlaylistViewModel(IMessagingService messagingService, IConfigurationService? configurationService = null, IPlaylistDataService? playlistDataService = null, IPlaybackContextService? playbackContextService = null)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _configurationService = configurationService;
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
            
            // 监听配置变化
            if (_configurationService != null)
            {
                _configurationService.ConfigurationChanged += OnConfigurationChanged;
            }
            
            // 初始化命令
            PlaySelectedSongCommand = new RelayCommand<Song>(ExecutePlaySelectedSong);
            DeleteSelectedSongCommand = new RelayCommand<Song>(ExecuteDeleteSelectedSong);
            ClearPlaylistCommand = new RelayCommand(ExecuteClearPlaylist);
            AddMusicCommand = new RelayCommand(async () => await ExecuteAddMusic()); 
            SortCommand = new RelayCommand<string>(ExecuteSort);
            SelectSortOptionCommand = new RelayCommand<SortOption>(ExecuteSelectSortOption);
            ToggleSongHeartCommand = new RelayCommand<Song>(ExecuteToggleSongHeart);
            SearchButtonClickCommand = new RelayCommand(ExecuteSearchButtonClick);
            ScrollToCurrentSongCommand = new RelayCommand(ExecuteScrollToCurrentSong);
          
         
            
            // 注册消息处理器
            RegisterMessageHandlers();
            
            // 注册UI相关的消息处理器
            RegisterUIMessageHandlers();
            
            // 主动获取初始数据，确保UI有数据
            InitializePlaylistData();
            
            // 初始化过滤后的播放列表
            UpdateFilteredPlaylist();
        }

        /// <summary>
        /// 初始化播放列表数据
        /// </summary>
        private void InitializePlaylistData()
        {
            System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 开始初始化播放列表数据");
            
            // 初始化时清空过滤列表
            _filteredPlaylist.Clear();
            
            System.Diagnostics.Debug.WriteLine("PlaylistViewModel: 初始化完成，过滤列表已清空，将使用数据服务的缓存作为唯一数据源");
            
            // 初始化排序选项
            var currentSortRule = _playlistDataService.CurrentSortRule;
            var matchingOption = SortOptions.FirstOrDefault(o => o.Value == currentSortRule);
            if (matchingOption != null)
            {
                _selectedSortOption = matchingOption;
                OnPropertyChanged(nameof(SelectedSortOption));
                OnPropertyChanged(nameof(SortRuleText));
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 初始化排序选项为: {matchingOption.Display}");
            }
            
            System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 初始化完成，过滤列表包含 {_filteredPlaylist.Count} 首歌曲");
            
            // 通知SongCount属性已更新
            OnPropertyChanged(nameof(SongCount));
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 播放列表数据变化消息
            _messagingService.Register<PlaylistDataChangedMessage>(this, (r, m) =>
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 收到 PlaylistDataChangedMessage，类型: {m.Type}");
                
                // 不再更新本地播放列表，直接从数据服务获取最新数据
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 不再使用消息中的数据，将直接从数据服务获取最新数据");
                
                // 更新当前歌曲
                if (m.CurrentSong != null)
                {
                    CurrentSong = m.CurrentSong;
                }
                
                // 更新排序选项，确保UI与数据源同步
                if (m.CurrentSortRule != CurrentSortRule)
                {
                    // 同步排序规则
                    OnPropertyChanged(nameof(CurrentSortRule));
                    OnPropertyChanged(nameof(SortRuleText));
                    
                    // 更新选中的排序选项
                    var matchingOption = SortOptions.FirstOrDefault(o => o.Value == m.CurrentSortRule);
                    if (matchingOption != null && _selectedSortOption != matchingOption)
                    {
                        _selectedSortOption = matchingOption;
                        OnPropertyChanged(nameof(SelectedSortOption));
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 开始 UpdateFilteredPlaylist");
                UpdateFilteredPlaylist();
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: PlaylistDataChangedMessage 处理完成");
                
                // 通知SongCount属性已更新
                OnPropertyChanged(nameof(SongCount));
            });

            // 当前歌曲变化消息
            _messagingService.Register<CurrentSongChangedMessage>(this, (r, m) =>
            {
                CurrentSong = m.Value;
                
                // 更新选中项
                if (m.Value != null)
                {
                    CurrentPlaylistItem = m.Value;
                }
                else
                {
                    CurrentPlaylistItem = null;
                }
            });


        }

       

        /// <summary>
        /// 执行播放选中歌曲操作
        /// </summary>
        private void ExecutePlaySelectedSong(Song? song)
        {
            if (song != null)
            {
                // 根据当前过滤模式设置播放上下文
                var contextType = _currentFilterMode == FilterMode.Favorites 
                    ? PlaybackContextType.Favorites 
                    : PlaybackContextType.DefaultPlaylist;
                
                var context = contextType == PlaybackContextType.Favorites 
                    ? PlaybackContext.CreateFavorites() 
                    : PlaybackContext.CreateDefault();
                
                // 设置播放上下文
                _playbackContextService.SetPlaybackContext(context.Type, context.Identifier, context.DisplayName);
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 设置播放上下文为 {context.DisplayName}");
                
                // 发送播放消息
                _messagingService.Send(new PlaySelectedSongMessage(song));
            }
        }

        /// <summary>
        /// 执行删除选中歌曲操作（逻辑删除）
        /// </summary>
        private void ExecuteDeleteSelectedSong(Song? song)
        {
            if (song != null)
            {
                // 发送更新删除状态的消息，而不是真正删除
                _messagingService.Send(new UpdateSongDeletionStatusMessage(song, true));
            }
        }

        /// <summary>
        /// 执行添加音乐命令
        /// </summary>
        private async Task ExecuteAddMusic()
        {
            _messagingService.Send(new AddMusicFilesMessage());
        }

        /// <summary>
        /// 执行清空播放列表操作
        /// </summary>
        private void ExecuteClearPlaylist()
        {
            _messagingService.Send(new PlaylistClearedMessage());
            UpdateFilteredPlaylist();
        }

        /// <summary>
        /// 更新过滤后的播放列表
        /// </summary>
        private void UpdateFilteredPlaylist()
        {
            // 保存当前选中的歌曲
            var selectedSong = CurrentPlaylistItem;
            
            System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: UpdateFilteredPlaylist - 搜索文本: '{SearchText}', 当前排序规则: {_playlistDataService.CurrentSortRule}, 过滤模式: {_currentFilterMode}");
            
            // 直接从数据服务获取最新的播放列表数据，不使用本地缓存
            var dataSource = _playlistDataService.DataSource;
            
            // 清空过滤列表
            _filteredPlaylist.Clear();
            
            foreach (var song in dataSource)
            {
                // 首先检查删除状态，这是最高优先级
                if (song.IsDeleted)
                {
                    // 已删除的歌曲不在任何列表中显示
                    continue;
                }
                
                bool shouldInclude = false;
                
                // 然后检查过滤模式
                if (_currentFilterMode == FilterMode.Favorites)
                {
                    // 收藏模式，只包含收藏的歌曲
                    if (song.Heart)
                    {
                        // 如果有搜索文本，还要检查搜索条件
                        if (string.IsNullOrWhiteSpace(SearchText))
                        {
                            shouldInclude = true;
                        }
                        else
                        {
                            var lowerSearchText = SearchText.ToLower();
                            if ((song.Title != null && song.Title.ToLower().Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)) ||
                                (song.Artist != null && song.Artist.ToLower().Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)) ||
                                (song.Album != null && song.Album.ToLower().Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)))
                            {
                                shouldInclude = true;
                            }
                        }
                    }
                }
                else
                {
                    // 全部模式
                    if (string.IsNullOrWhiteSpace(SearchText))
                    {
                        // 如果没有搜索文本，则显示所有歌曲
                        shouldInclude = true;
                    }
                    else
                    {
                        // 如果有搜索文本，则根据搜索条件过滤
                        var lowerSearchText = SearchText.ToLower();
                        if ((song.Title != null && song.Title.ToLower().Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)) ||
                            (song.Artist != null && song.Artist.ToLower().Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)) ||
                            (song.Album != null && song.Album.ToLower().Contains(lowerSearchText, StringComparison.OrdinalIgnoreCase)))
                        {
                            shouldInclude = true;
                        }
                    }
                }
                
                if (shouldInclude)
                {
                    _filteredPlaylist.Add(song);
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: UpdateFilteredPlaylist 完成，过滤后列表包含 {_filteredPlaylist.Count} 首歌曲，过滤模式: {_currentFilterMode}");
            
            // 清除临时数据缓存
            _playlistDataService.ClearDataSource();
            
            // 尝试恢复之前选中的歌曲
            if (selectedSong != null && _filteredPlaylist.Contains(selectedSong))
            {
                CurrentPlaylistItem = selectedSong;
            }
            else
            {
                CurrentPlaylistItem = null;
            }

            // 通知SongCount属性已更新
            OnPropertyChanged(nameof(SongCount));
        }

        /// <summary>
        /// 执行排序操作
        /// </summary>
        private void ExecuteSort(string? sortRuleName)
        {
            if (string.IsNullOrEmpty(sortRuleName))
                return;

            SortRule sortRule = sortRuleName switch
            {
                "ByAddedTime" => SortRule.ByAddedTime,
                "ByTitle" => SortRule.ByTitle,
                "ByArtist" => SortRule.ByArtist,
                "ByAlbum" => SortRule.ByAlbum,
                "ByDuration" => SortRule.ByDuration,
                "ByFileSize" => SortRule.ByFileSize,
                _ => SortRule.ByAddedTime
            };

            // 如果排序规则没有变化，也强制刷新列表
            if (CurrentSortRule == sortRule)
            {
                UpdateFilteredPlaylist();
            }
            else
            {
                CurrentSortRule = sortRule;
            }
        }
 
 
        
        /// <summary>
        /// 注册UI相关的消息处理器
        /// </summary>
        private void RegisterUIMessageHandlers()
        {
            // 注册过滤收藏歌曲消息处理器
            _messagingService.Register<FilterFavoriteSongsMessage>(this, OnFilterFavoriteSongsRequested);
            _messagingService.Register<ShowAllSongsMessage>(this, OnShowAllSongsRequested);
        }
        
        /// <summary>
        /// 处理过滤收藏歌曲请求
        /// </summary>
        private void OnFilterFavoriteSongsRequested(object recipient, FilterFavoriteSongsMessage message)
        {
            try
            {
                // 设置过滤模式为收藏
                _currentFilterMode = FilterMode.Favorites;
                
                // 通知UI更新标题
                OnPropertyChanged(nameof(PlaylistTitle));
                
                // 获取当前播放列表数据
                var currentPlaylist = _playlistDataService.DataSource;
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 切换到收藏模式，当前播放列表中有 {currentPlaylist.Count} 首歌曲");
                
                // 统计收藏歌曲数量
                var favoriteCount = currentPlaylist.Count(s => s.Heart);
                
                // 清除临时数据缓存
                _playlistDataService.ClearDataSource();
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 其中收藏的歌曲有 {favoriteCount} 首");
                
                // 更新过滤后的播放列表
                UpdateFilteredPlaylist();
                
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 过滤后的列表包含 {_filteredPlaylist.Count} 首收藏歌曲");
                
                // 回复消息
                message.Reply(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"过滤收藏歌曲失败: {ex.Message}");
                message.Reply(false);
            }
        }
        
        /// <summary>
        /// 处理显示所有歌曲请求
        /// </summary>
        private void OnShowAllSongsRequested(object recipient, ShowAllSongsMessage message)
        {
            try
            {
                // 设置过滤模式为全部
                _currentFilterMode = FilterMode.All;
                
                // 通知UI更新标题
                OnPropertyChanged(nameof(PlaylistTitle));
                
                // 更新过滤后的播放列表
                UpdateFilteredPlaylist();
                
                // 回复消息
                message.Reply(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示所有歌曲失败: {ex.Message}");
                message.Reply(false);
            }
        }
        
        

        /// <summary>
        /// 选择排序选项
        /// </summary>
        private void ExecuteSelectSortOption(SortOption? sortOption)
        {
            if (sortOption != null)
            {
                SelectedSortOption = sortOption;
                System.Diagnostics.Debug.WriteLine($"选择排序选项: {sortOption.Display}, 规则: {sortOption.Value}");
            }
        }

        /// <summary>
        /// 执行搜索按钮点击操作
        /// </summary>
        private void ExecuteSearchButtonClick()
        {
            if (!IsSearchExpanded)
            {
                // 第一次点击：展开搜索框并获取焦点
                IsSearchExpanded = true;
                // 发送消息通知UI获取焦点
                _messagingService.Send(new SearchBoxFocusRequestMessage());
            }
            else
            {
                // 第二次点击：清空搜索文本并折叠搜索框
                SearchText = string.Empty;
                IsSearchExpanded = false;
            }
        }

        /// <summary>
        /// 执行切换歌曲收藏状态操作
        /// </summary>
        private void ExecuteToggleSongHeart(Song? song)
        {
            if (song != null)
            {
                song.Heart = !song.Heart;
                // 发送消息更新持久化存储
                _messagingService.Send(new UpdateSongFavoriteStatusMessage(song, song.Heart));
            }
        }

        /// <summary>
        /// 请求专辑封面加载
        /// </summary>
        /// <param name="song">需要加载封面的歌曲</param>
        public void RequestAlbumLoad(Song song)
        {
            // 确保取消延迟加载设置
            song.DelayAlbumArtLoading = false;
            
            // 如果还没有加载封面，触发事件
            if (song.AlbumArt == null && (song.AlbumArtData == null || song.AlbumArtData.Length == 0))
            {
                System.Diagnostics.Debug.WriteLine($"RequestAlbumLoad: 触发加载歌曲 {song.Title} 的封面");
                AlbumLoadRequested?.Invoke(this, new AlbumLoadRequestEventArgs(song));
            }
        }

        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public override void Initialize()
        {
          
        }

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            // 注销消息处理器
            _messagingService.Unregister(this);
        }

        /// <summary>
        /// 滚动到当前播放歌曲命令执行方法
        /// </summary>
        private void ExecuteScrollToCurrentSong()
        {
            if (CurrentSong != null && _filteredPlaylist.Contains(CurrentSong))
            {
                // 发送滚动到当前播放歌曲的消息
                _messagingService.Send(new ScrollToCurrentSongMessage(CurrentSong));
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 发送滚动到当前歌曲的消息: {CurrentSong.Title}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlaylistViewModel: 没有当前播放歌曲或当前歌曲不在播放列表中");
            }
        }

        /// <summary>
        /// 配置变化处理器
        /// </summary>
        private void OnConfigurationChanged(PlayerConfiguration? config)
        {
            if (config != null)
            {
                // 当配置变化时，触发属性更新
                OnPropertyChanged(nameof(CurrentSortRule));
                OnPropertyChanged(nameof(SortRuleText));
                
                // 更新过滤后的播放列表
                UpdateFilteredPlaylist();
                
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 配置更新，当前排序规则: {config.SortRule}");
            }
        }

        public void Dispose()
        {
            Cleanup();
        }

    }
}