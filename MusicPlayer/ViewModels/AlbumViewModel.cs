using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;
using MusicPlayer.Services.Messages;
using System.Windows.Input;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 专辑页面视图模型
    /// </summary>
    public class AlbumViewModel : ObservableObject, IAlbumViewModel, IDisposable
    {
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaybackContextService _playbackContextService;
        private readonly IMessagingService _messagingService;

        private ObservableCollection<AlbumInfo> _albums = new();
        private ObservableCollection<AlbumInfo> _filteredAlbums = new();
        private string _currentIndex = "ALL";//ALL
        private string _searchText = string.Empty;
        private bool _isSearchExpanded = false;
        private readonly List<string> _indexList = new() { "ALL","#", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"  };

        private readonly Navigation.NavigationService _navigationService;

        /// <summary>
        /// 专辑列表
        /// </summary>
        public ObservableCollection<AlbumInfo> Albums => _albums;

        /// <summary>
        /// 过滤后的专辑列表
        /// </summary>
        public ObservableCollection<AlbumInfo> FilteredAlbums => _filteredAlbums;

        /// <summary>
        /// 专辑总数
        /// </summary>
        public int AlbumCount => _albums.Count;

        /// <summary>
        /// 索引列表
        /// </summary>
        public List<string> IndexList => _indexList;

        /// <summary>
        /// 当前选中的索引
        /// </summary>
        public string CurrentIndex
        {
            get => _currentIndex;
            set
            {
                if (_currentIndex != value)
                {
                    _currentIndex = value;
                    OnPropertyChanged(nameof(CurrentIndex));
                    UpdateFilteredAlbums();
                }
            }
        }

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
                    UpdateFilteredAlbums();
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
        /// 播放选中专辑的歌曲命令
        /// </summary>
        public ICommand PlayAlbumCommand { get; }

        /// <summary>
        /// 搜索按钮点击命令
        /// </summary>
        public ICommand SearchButtonClickCommand { get; }

        /// <summary>
        /// 导航到专辑详情页命令
        /// </summary>
        public ICommand NavigateToAlbumDetailCommand { get; }

        // 当前播放状态
        private bool _isPlaying = false;
        
        public AlbumViewModel(
            IPlaylistDataService playlistDataService,
            IPlaybackContextService playbackContextService,
            IMessagingService messagingService,
            Navigation.NavigationService navigationService)
        {
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            PlayAlbumCommand = new RelayCommand<string>(ExecutePlayAlbum);
            SearchButtonClickCommand = new RelayCommand(ExecuteSearchButtonClick);
            NavigateToAlbumDetailCommand = new RelayCommand<string>(ExecuteNavigateToAlbumDetail);
            
            // 注册消息处理器
            RegisterMessageHandlers();

            LoadAlbums();
        }

        /// <summary>
        /// 加载专辑列表
        /// </summary>
        public void LoadAlbums()
        {
            // 从播放列表中获取所有专辑及其歌曲数量和第一首歌
            var playlist = _playlistDataService.DataSource;
            var albumGroups = playlist
                .Where(song => !string.IsNullOrEmpty(song.Album))
                .GroupBy(song => song.Album!)
                .Select(group => new AlbumInfo
                {
                    Name = group.Key,
                    SongCount = group.Count(),
                    FirstSongFilePath = group.First().FilePath
                })
                .OrderBy(album => album.Name)
                .ToList();

            // 更新UI - 重新创建集合实例，确保VirtualizingWrapPanel能正确处理
            _albums = new ObservableCollection<AlbumInfo>(albumGroups);
            
            // 重新创建过滤后的专辑列表，确保VirtualizingWrapPanel能正确处理
            _filteredAlbums = new ObservableCollection<AlbumInfo>();
            
            // 初始过滤
            UpdateFilteredAlbums();
        }

        /// <summary>
        /// 刷新专辑数据
        /// </summary>
        public void RefreshAlbums()
        {
            LoadAlbums();
        }

        /// <summary>
        /// 更新过滤后的专辑列表
        /// </summary>
        private void UpdateFilteredAlbums()
        {
            var filtered = _albums.AsEnumerable();

            // 按索引过滤
            if (CurrentIndex != "ALL")
            {
                filtered = filtered.Where(album => 
                {
                    if (string.IsNullOrEmpty(album.Name))
                        return CurrentIndex == "#";
                    
                    var firstChar = album.Name[0].ToString().ToUpper();
                    if (CurrentIndex == "#")
                        return !char.IsLetter(firstChar[0]);
                    
                    return firstChar == CurrentIndex;
                });
            }

            // 按搜索文本过滤
            if (!string.IsNullOrEmpty(SearchText))
            {
                filtered = filtered.Where(album => 
                    album.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // 更新过滤后的列表
            _filteredAlbums.Clear();
            foreach (var album in filtered)
            {
                _filteredAlbums.Add(album);
            }
        }

        /// <summary>
        /// 执行播放选中专辑的歌曲
        /// </summary>
        /// <param name="album">专辑名称</param>
        private void ExecutePlayAlbum(string? album)
        {
            if (string.IsNullOrEmpty(album))
                return;

            // 获取该专辑的第一首歌
            var playlist = _playlistDataService.DataSource;
            var firstSong = playlist
                .Where(song => !string.IsNullOrEmpty(song.Album) && 
                              string.Equals(song.Album, album, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (firstSong != null)
            {
                // 设置播放上下文为专辑列表
                var context = PlaybackContext.CreateAlbum(album);
                _playbackContextService.SetPlaybackContext(context.Type, context.Identifier, context.DisplayName);
                
                System.Diagnostics.Debug.WriteLine($"AlbumViewModel: 设置播放上下文为专辑: {album}");

                // 发送播放消息
                _messagingService.Send(new PlaySelectedSongMessage(firstSong));
            }
        }

        /// <summary>
        /// 执行导航到专辑详情页
        /// </summary>
        /// <param name="albumName">专辑名称</param>
        private void ExecuteNavigateToAlbumDetail(string? albumName)
        {
            if (string.IsNullOrEmpty(albumName))
                return;

            System.Diagnostics.Debug.WriteLine($"AlbumViewModel: 准备导航到专辑 {albumName} 的详情页");
            
           
            var navParams = new PlaylistDetailParams
            {
                AlbumName = albumName
            };
            
            // 导航到歌单详情页
            _navigationService.NavigateToPlaylistDetail(navParams);
        }
        
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 监听当前歌曲变化消息
            _messagingService.Register<CurrentSongChangedMessage>(this, OnCurrentSongChanged);
            
            // 监听播放状态变化消息
            _messagingService.Register<PlaybackStateChangedMessage>(this, OnPlaybackStateChanged);
        }
        
        /// <summary>
        /// 处理当前歌曲变化消息
        /// </summary>
        private void OnCurrentSongChanged(object recipient, CurrentSongChangedMessage message)
        {
            UpdateAlbumPlayingState(message.Value, _isPlaying);
        }
        
        /// <summary>
        /// 处理播放状态变化消息
        /// </summary>
        private void OnPlaybackStateChanged(object recipient, PlaybackStateChangedMessage message)
        {
            _isPlaying = message.Value;
            UpdateAlbumPlayingState(_playlistDataService.CurrentSong, _isPlaying);
        }
        
        /// <summary>
        /// 更新专辑播放状态
        /// </summary>
        private void UpdateAlbumPlayingState(Core.Models.Song? currentSong, bool isPlaying)
        {
            // 先重置所有专辑的播放状态
            foreach (var album in _albums)
            {
                album.IsPlaying = false;
            }
            
            // 如果有当前播放歌曲，且专辑不为空，则更新对应专辑的播放状态
            if (currentSong != null && !string.IsNullOrEmpty(currentSong.Album))
            {
                var album = _albums.FirstOrDefault(a => 
                    string.Equals(a.Name, currentSong.Album, StringComparison.OrdinalIgnoreCase));
                
                if (album != null)
                {
                    // 从PlayerStateService获取当前播放上下文
                    var context = _playbackContextService.CurrentPlaybackContext;
                    album.IsPlaying = isPlaying && 
                                      context.Type == Core.Enums.PlaybackContextType.Album && 
                                      string.Equals(context.Identifier, album.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>
        /// 执行搜索按钮点击
        /// </summary>
        private void ExecuteSearchButtonClick()
        {
            if (!IsSearchExpanded)
            {
                IsSearchExpanded = true;
                // 搜索框展开时，将索引定位到ALL，确保能显示所有符合条件的专辑
                CurrentIndex = "ALL";
                // 发送消息请求搜索框获取焦点
                _messagingService.Send(new SearchBoxFocusRequestMessage());
            }
            else
            {
                SearchText = string.Empty;
                IsSearchExpanded = false;
            }
        }

        /// <summary>
        /// 初始化视图模型
        /// </summary>
        public override void Initialize()
        {
            System.Diagnostics.Debug.WriteLine("AlbumViewModel: Initialize 方法被调用");
            LoadAlbums(); // 重新分组数据
        }

        /// <summary>
        /// 清理视图模型
        /// </summary>
        public override void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("AlbumViewModel: Cleanup 方法被调用");
            _messagingService.Unregister(this);

            // 主动清理所有已加载的封面图像
            foreach (var album in _albums)
            {
                album.CoverImage = null;
            }

            _albums.Clear();
            _filteredAlbums.Clear();
        }
        
        public void Dispose()
        {
            Cleanup();
        }
    }
}