using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Interfaces;
using MusicPlayer.Core.Models;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages; // 引入新的消息类型
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

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
        private readonly ICustomPlaylistService _customPlaylistService;
        private readonly IPlaybackContextService _playbackContextService;
        private readonly INotificationService _notificationService;
        private readonly IPlaylistCacheService _playlistCacheService;
        
        // 所有歌单列表
        private readonly ObservableCollection<Playlist> _allPlaylists = new();
        public ObservableCollection<Playlist> AllPlaylists => _allPlaylists;
        
        // 本地状态通过消息同步
        // 注意：_playlist已移除，使用FilteredPlaylist作为主要的数据集合
        private readonly ObservableCollection<Song> _filteredPlaylist = new();
        private string _searchText = string.Empty;
        private Song? _currentPlaylistItem;
        private Song? _currentSong;
        private bool _isSearchExpanded = false;

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
                        // 使用AlbumArtLoader直接加载封面
                        _currentSong.AlbumArt = AlbumArtLoader.LoadAlbumArt(_currentSong.FilePath);
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
        /// 搜索按钮点击命令
        /// </summary>
        public ICommand SearchButtonClickCommand { get; }

        /// <summary>
        /// 滚动到当前播放歌曲命令
        /// </summary>
        public ICommand ScrollToCurrentSongCommand { get; }
        
        /// <summary>
        /// 添加歌曲到歌单命令
        /// </summary>
        public ICommand AddSongToPlaylistCommand { get; }

        /// <summary>
        /// 播放全部命令
        /// </summary>
        public ICommand PlayAllCommand { get; }

        /// <summary>
        /// 随机播放命令
        /// </summary>
        public ICommand ShufflePlayCommand { get; }
        

        /// <summary>
        /// 排序命令
        /// </summary>
        public ICommand SortCommand { get; }
        
        /// <summary>
        /// 选择排序选项命令
        /// </summary>
        public ICommand SelectSortOptionCommand { get; } 

        



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

        public PlaylistViewModel(IMessagingService messagingService, IConfigurationService? configurationService = null, IPlaylistDataService? playlistDataService = null, ICustomPlaylistService? customPlaylistService = null, IPlaybackContextService? playbackContextService = null, INotificationService? notificationService = null, IPlaylistCacheService? playlistCacheService = null)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _configurationService = configurationService;
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _customPlaylistService = customPlaylistService ?? throw new ArgumentNullException(nameof(customPlaylistService));
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _playlistCacheService = playlistCacheService ?? throw new ArgumentNullException(nameof(playlistCacheService));
            
            // 监听配置变化
            if (_configurationService != null)
            {
                _configurationService.ConfigurationChanged += OnConfigurationChanged;
            }
            
            // 初始化命令
            PlaySelectedSongCommand = new RelayCommand<Song>(ExecutePlaySelectedSong);
            DeleteSelectedSongCommand = new RelayCommand<Song>(ExecuteDeleteSelectedSong);
            ClearPlaylistCommand = new RelayCommand(ExecuteClearPlaylist);
            SortCommand = new RelayCommand<string>(ExecuteSort);
            SelectSortOptionCommand = new RelayCommand<SortOption>(ExecuteSelectSortOption);
            ToggleSongHeartCommand = new RelayCommand<Song>(ExecuteToggleSongHeart);
            SearchButtonClickCommand = new RelayCommand(ExecuteSearchButtonClick);
            ScrollToCurrentSongCommand = new RelayCommand(ExecuteScrollToCurrentSong);
            AddSongToPlaylistCommand = new RelayCommand<object>(ExecuteAddSongToPlaylist);
            PlayAllCommand = new RelayCommand(ExecutePlayAll);
            ShufflePlayCommand = new RelayCommand(ExecuteShufflePlay);
          
         
            
            // 注册消息处理器
            RegisterMessageHandlers();
            
            // 注册UI相关的消息处理器
            //RegisterUIMessageHandlers();
            
            // 主动获取初始数据，确保UI有数据
            InitializePlaylistData();
            
            // 加载所有歌单
           
            
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
            
            // 加载当歌曲
            LoadSongsForCurrentContext();
            _ = LoadAllPlaylistsAsync();
            System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 初始化完成，过滤列表包含 {_filteredPlaylist.Count} 首歌曲");
            
            // 通知SongCount属性已更新
            OnPropertyChanged(nameof(SongCount));
        }

        /// <summary>
        /// 加载所有歌曲并应用过滤
        /// </summary>
        private void LoadSongsForCurrentContext()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 开始加载所有歌曲");
                
                // 加载所有歌曲
                List<Core.Models.Song> songs = _playlistDataService.DataSource;
                
                // 更新过滤后的播放列表
                _filteredPlaylist.Clear();
                
                // 应用搜索过滤
                var filteredSongs = string.IsNullOrWhiteSpace(SearchText)
                    ? songs
                    : songs.Where(song => 
                        (song.Title != null && song.Title.ToLower().Contains(SearchText.ToLower(), StringComparison.OrdinalIgnoreCase)) ||
                        (song.Artist != null && song.Artist.ToLower().Contains(SearchText.ToLower(), StringComparison.OrdinalIgnoreCase)) ||
                        (song.Album != null && song.Album.ToLower().Contains(SearchText.ToLower(), StringComparison.OrdinalIgnoreCase))).ToList();
                
                // 只添加未删除的歌曲
                foreach (var song in filteredSongs)
                {
                    if (!song.IsDeleted)
                    {
                        _filteredPlaylist.Add(song);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 加载完成，共加载 {songs.Count} 首歌曲，过滤后显示 {_filteredPlaylist.Count} 首歌曲");
                
                // 通知相关属性已更新
                OnPropertyChanged(nameof(SongCount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 加载歌曲失败: {ex.Message}");
            }
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
                if (m.Value != null)
                {
                    // 在FilteredPlaylist中查找具有相同ID的歌曲对象
                    var matchingSong = _filteredPlaylist.FirstOrDefault(song => song.Id == m.Value.Id);
                    
                    if (matchingSong != null)
                    {
                        CurrentSong = matchingSong;
                        CurrentPlaylistItem = matchingSong;
                        System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 找到匹配的歌曲，更新选中项: {matchingSong.Title}");
                    }
                    else
                    {
                        CurrentSong = m.Value;
                        CurrentPlaylistItem = null;
                        System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 未找到匹配的歌曲，清空选中项: {m.Value.Title}");
                    }
                }
                else
                {
                    CurrentSong = null;
                    CurrentPlaylistItem = null;
                    System.Diagnostics.Debug.WriteLine("PlaylistViewModel: 没有当前播放歌曲，清空选中项");
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
                // 设置播放上下文为默认列表
                var context = PlaybackContext.CreateDefault();
                _playbackContextService.SetPlaybackContext(context.Type, context.Identifier, context.DisplayName);
                
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 设置播放上下文为默认列表");
                
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
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 开始删除歌曲 {song.Title}");
                
                // 发送更新删除状态的消息，而不是真正删除
                var result = _messagingService.Send<UpdateSongDeletionStatusMessage, bool>(
                    new UpdateSongDeletionStatusMessage(song, true));
                
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 删除歌曲结果 {result}");
                
                if (result)
                {
                    _notificationService.ShowSuccess($"成功删除歌曲[{song.Title}]");
                }
                else
                {
                    _notificationService.ShowError( "无法更新歌曲删除状态");
                }
            }
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
            
            System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: UpdateFilteredPlaylist - 搜索文本: '{SearchText}', 当前排序规则: {_playlistDataService.CurrentSortRule}");
            
            // 加载当前播放上下文对应的歌曲
            LoadSongsForCurrentContext();
            
            System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: UpdateFilteredPlaylist 完成，过滤后列表包含 {_filteredPlaylist.Count} 首歌曲");
            
            // 尝试恢复之前选中的歌曲
            if (selectedSong != null && _filteredPlaylist.Contains(selectedSong))
            {
                CurrentPlaylistItem = selectedSong;
            }
            else
            {
                CurrentPlaylistItem = null;
            }

            // 通知相关属性已更新
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
        private async void ExecuteToggleSongHeart(Song? song)
        {
            if (song != null)
            {
                // 检查歌曲是否已在收藏列表中
                var favoritesPlaylistSongs = await _playlistCacheService.GetSongsByPlaylistIdAsync(1);
                var isCurrentlyFavorited = favoritesPlaylistSongs.Any(s => s.Id == song.Id);
                
                // 切换收藏状态
                if (!isCurrentlyFavorited)
                {
                    // 添加到收藏列表
                    await _playlistCacheService.AddSongToPlaylistAsync(1, song.Id);
                }
                else
                {
                    // 从收藏列表移除
                    await _playlistCacheService.RemoveSongFromPlaylistAsync(1, song.Id);
                }
            }
        }

        /// <summary>
        /// 请求专辑封面加载
        /// </summary>
        /// <param name="song">需要加载封面的歌曲</param>
        public void RequestAlbumLoad(Song song)
        {
            // 确保取消延迟加载设置

            
            // 如果还没有加载封面，触发事件
            if (song.AlbumArt == null)
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
            System.Diagnostics.Debug.WriteLine("PlaylistViewModel: Initialize 方法被调用");
            InitializePlaylistData();
        }

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("PlaylistViewModel: Cleanup 方法被调用");
            
            // 注销消息处理器
            _messagingService.Unregister(this);
            
            // 取消配置变化监听
            if (_configurationService != null)
            {
                _configurationService.ConfigurationChanged -= OnConfigurationChanged;
            }
            
            // 清理集合资源
            try
            {
                // 清空过滤后的播放列表
                if (_filteredPlaylist != null)
                {
                    _filteredPlaylist.Clear();
                }
                
                // 清空所有播放列表
                if (_allPlaylists != null)
                {
                    _allPlaylists.Clear();
                }
                
                // 清空排序选项
                if (SortOptions != null)
                {
                    SortOptions.Clear();
                }
                
                System.Diagnostics.Debug.WriteLine("PlaylistViewModel: 已清理所有集合资源");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 清理集合资源时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 滚动到当前播放歌曲命令执行方法
        /// </summary>
        private void ExecuteScrollToCurrentSong()
        {
            // 直接从PlaylistDataService获取最新的当前歌曲，确保使用最新数据
            var currentSong = _playlistDataService.CurrentSong;
            
            if (currentSong != null && _filteredPlaylist.Contains(currentSong))
            {
                // 发送滚动到当前播放歌曲的消息
                _messagingService.Send(new ScrollToCurrentSongMessage(currentSong));
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 发送滚动到当前歌曲的消息: {currentSong.Title}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlaylistViewModel: 没有当前播放歌曲或当前歌曲不在播放列表中");
            }
        }

        /// <summary>
        /// 播放全部歌曲
        /// </summary>
        private void ExecutePlayAll()
        {
            try
            {
                if (_filteredPlaylist.Count > 0)
                {
                    // 设置播放上下文为默认列表
                    _playbackContextService.SetPlaybackContext(
                        Core.Enums.PlaybackContextType.DefaultPlaylist, 
                        "default", 
                        "全部歌曲");
                    
                    System.Diagnostics.Debug.WriteLine("PlaylistViewModel: 设置播放上下文为默认列表: 全部歌曲");
                    
                    // 发送播放消息
                    var firstSong = _filteredPlaylist.First();
                    _messagingService.Send(new SongSelectionMessage(firstSong, 0));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 播放全部歌曲失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 随机播放歌曲
        /// </summary>
        private void ExecuteShufflePlay()
        {
            try
            {
                if (_filteredPlaylist.Count > 0)
                {
                    // 设置播放上下文为默认列表
                    _playbackContextService.SetPlaybackContext(
                        Core.Enums.PlaybackContextType.DefaultPlaylist, 
                        "default", 
                        "全部歌曲");
                    
                    System.Diagnostics.Debug.WriteLine("PlaylistViewModel: 设置播放上下文为默认列表: 全部歌曲");
                    
                    // 随机排序播放列表
                    var shuffledSongs = _filteredPlaylist.OrderBy(song => Guid.NewGuid()).ToList();
                    
                    // 发送播放消息，播放随机排序后的第一首歌曲
                    var firstSong = shuffledSongs.First();
                    _messagingService.Send(new SongSelectionMessage(firstSong, 0));
                    
                    System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 随机播放开始，第一首歌曲: {firstSong.Title}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistViewModel: 随机播放歌曲失败: {ex.Message}");
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

        /// <summary>
        /// 加载所有歌单
        /// </summary>
        private async Task LoadAllPlaylistsAsync()
        {
            try
            {
                var playlists = await _customPlaylistService.GetAllPlaylistsAsync();
                _allPlaylists.Clear();
                foreach (var playlist in playlists)
                {
                    _allPlaylists.Add(playlist);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载歌单失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行添加歌曲到歌单操作
        /// </summary>
        private async void ExecuteAddSongToPlaylist(object? param)
        {
            if (param is ValueTuple<Song, int> tuple)
            {
                var song = tuple.Item1;
                var playlistId = tuple.Item2;
                
                if (song == null || playlistId <= -1) //只要不是默认列表就行
                    return;
                
                try
                {
                    bool result = await _customPlaylistService.AddSongToPlaylistAsync(playlistId, song.Id);
                    if (result)
                    {
                        System.Diagnostics.Debug.WriteLine($"成功将歌曲 {song.Title} 添加到歌单 {playlistId}");
                        // 获取歌单名称
                        var playlist = await _customPlaylistService.GetPlaylistByIdAsync(playlistId);
                        _notificationService.ShowSuccess($"成功将歌曲 '{song.Title}' 添加到歌单 '{playlist?.Name}'");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"歌曲 {song.Title} 已存在于歌单 {playlistId} 中");
                        // 获取歌单名称
                        var playlist = await _customPlaylistService.GetPlaylistByIdAsync(playlistId);
                        _notificationService.ShowInfo($"歌曲 '{song.Title}' 已存在于歌单 '{playlist?.Name}' 中");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"添加歌曲到歌单失败: {ex.Message}");
                    _notificationService.ShowError($"添加歌曲到歌单失败: {ex.Message}");
                }
            }
        }
        
        public void Dispose()
        {
            Cleanup();
        }

    }
}