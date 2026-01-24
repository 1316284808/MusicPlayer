using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Enums;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using MusicPlayer.Core.Interfaces;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌单详情页面视图模型
    /// </summary>
    public class PlaylistDetailViewModel : ObservableObject, IPlaylistDetailViewModel
    {
        private readonly IMessagingService _messagingService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaybackContextService _playbackContextService;
        private readonly ICustomPlaylistService _customPlaylistService;
        private readonly IPlaylistCacheService _playlistCacheService;

        // 所有歌单列表
        private readonly ObservableCollection<Playlist> _allPlaylists = new();
        public ObservableCollection<Playlist> AllPlaylists => _allPlaylists;

        // 过滤后的播放列表
        private readonly ObservableCollection<Song> _filteredPlaylist = new();
        // 原始歌曲列表，用于保存未过滤的所有歌曲
        private List<Song> _allSongs = new();
        private string _searchText = string.Empty;
        private Song? _currentPlaylistItem;
        private Playlist _currentPlaylist = new Playlist();
        
        // 添加字段保存当前的播放上下文类型和标识符
        private PlaybackContextType _currentContextType;
        private string _currentContextIdentifier = string.Empty;
        private string _currentContextDisplayName = string.Empty;

        /// <summary>
        /// 当前歌单
        /// </summary>
        public Playlist CurrentPlaylist
        {
            get => _currentPlaylist;
            set
            {
                if (_currentPlaylist != value)
                {
                    _currentPlaylist = value;
                    OnPropertyChanged(nameof(CurrentPlaylist));
                    // 加载歌单歌曲
                    LoadPlaylistSongsAsync();
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
        private bool _isSearchExpanded = false;
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
        public bool IsPlaying { get; set; } = false;

        /// <summary>
        /// 播放全部命令
        /// </summary>
        public ICommand PlayAllCommand { get; }

        /// <summary>
        /// 搜索按钮点击命令
        /// </summary>
        public ICommand SearchButtonClickCommand { get; }

        /// <summary>
        /// 播放选中歌曲命令
        /// </summary>
        public ICommand PlaySelectedSongCommand { get; }

        /// <summary>
        /// 删除选中歌曲命令
        /// </summary>
        public ICommand DeleteSelectedSongCommand { get; }

        /// <summary>
        /// 切换歌曲收藏状态命令
        /// </summary>
        public ICommand ToggleSongHeartCommand { get; }

        /// <summary>
        /// 添加歌曲到歌单命令
        /// </summary>
        public ICommand AddSongToPlaylistCommand { get; }

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
                    if (value != null) {
                        ExecutePlaySelectedSong(value);
                    }
                        OnPropertyChanged(nameof(CurrentPlaylistItem));
                   
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="messagingService"></param>
        /// <param name="playlistDataService"></param>
        /// <param name="playbackContextService"></param>
        /// <param name="customPlaylistService"></param>
        /// <param name="playlistCacheService"></param>
        public PlaylistDetailViewModel(
            IMessagingService messagingService,
            IPlaylistDataService playlistDataService,
            IPlaybackContextService playbackContextService,
            ICustomPlaylistService customPlaylistService,
            IPlaylistCacheService playlistCacheService)
        {
            _messagingService = messagingService;
            _playlistDataService = playlistDataService;
            _playbackContextService = playbackContextService;
            _customPlaylistService = customPlaylistService;
            _playlistCacheService = playlistCacheService;

            // 初始化命令
            PlayAllCommand = new RelayCommand(ExecutePlayAll);
            SearchButtonClickCommand = new RelayCommand(ExecuteSearchButtonClick);
            PlaySelectedSongCommand = new RelayCommand<Song>(ExecutePlaySelectedSong);
            DeleteSelectedSongCommand = new RelayCommand<Song>(ExecuteDeleteSelectedSong);
            AddSongToPlaylistCommand = new RelayCommand<object>(ExecuteAddSongToPlaylist);

            // 注册消息处理器
            RegisterMessageHandlers();

            // 初始化过滤后的播放列表
            UpdateFilteredPlaylist();
        }

        /// <summary>
        /// 初始化视图模型
        /// </summary>
        public void Initialize()
        {
            Initialize(null);
        }
        
        /// <summary>
        /// 初始化视图模型，支持导航参数
        /// </summary>
        /// <param name="params">导航参数</param>
        public void Initialize( PlaylistDetailParams? @params)
        {
            // 优先使用导航参数
            if (@params != null)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 使用导航参数初始化");
                
                // 根据导航参数类型加载对应内容
                if (@params.PlaylistId.HasValue)
                {
                    _currentContextType = PlaybackContextType.CustomPlaylist;
                    _currentContextIdentifier = @params.PlaylistId.Value.ToString();
                    _currentContextDisplayName = @params.PlaylistId.Value.ToString();
                    LoadCurrentPlaylistAsync(@params.PlaylistId.Value);
                }
                else if (!string.IsNullOrEmpty(@params.ArtistName))
                {
                    _currentContextType = PlaybackContextType.Artist;
                    _currentContextIdentifier = @params.ArtistName;
                    _currentContextDisplayName = @params.ArtistName;
                    
                    // 立即设置当前歌单为虚拟歌单（显示歌手名称）
                    CurrentPlaylist = new Playlist
                    {
                        Id = -1,
                        Name = @params.ArtistName,
                        Description = $"{@params.ArtistName}的所有歌曲"
                    };
                    
                    // 同步加载歌手的歌曲（不使用异步，确保UI能看到歌曲）
                    LoadSongsByArtist(@params.ArtistName);
                }
                else if (!string.IsNullOrEmpty(@params.AlbumName))
                {
                    _currentContextType = PlaybackContextType.Album;
                    _currentContextIdentifier = @params.AlbumName;
                    _currentContextDisplayName = @params.AlbumName;
                    
                    // 立即设置当前歌单为虚拟歌单（显示专辑名称）
                    CurrentPlaylist = new Playlist
                    {
                        Id = -1,
                        Name = @params.AlbumName,
                        Description = $"{@params.AlbumName}的所有歌曲"
                    };
                    
                    // 同步加载专辑的歌曲（不使用异步，确保UI能看到歌曲）
                    LoadSongsByAlbum(@params.AlbumName);
                }
            }
            else
            {
                // 如果没有导航参数，使用播放上下文
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 使用播放上下文初始化");
                
                var context = _playbackContextService.CurrentPlaybackContext;
                _currentContextType = context.Type;
                _currentContextIdentifier = context.Identifier;
                _currentContextDisplayName = context.DisplayName;
                
                if (context.Type == PlaybackContextType.CustomPlaylist)
                {
                    if (int.TryParse(context.Identifier, out int playlistId))
                    {
                        LoadCurrentPlaylistAsync(playlistId);
                    }
                }
                else if (context.Type == PlaybackContextType.Artist)
                {
                    // 立即设置当前歌单为虚拟歌单（显示歌手名称）
                    CurrentPlaylist = new Playlist
                    {
                        Id = -1,
                        Name = context.DisplayName,
                        Description = $"{context.DisplayName}的所有歌曲"
                    };
                    
                    // 同步加载歌手的歌曲（不使用异步，确保UI能看到歌曲）
                    LoadSongsByArtist(context.Identifier);
                }
                else if (context.Type == PlaybackContextType.Album)
                {
                    // 立即设置当前歌单为虚拟歌单（显示专辑名称）
                    CurrentPlaylist = new Playlist
                    {
                        Id = -1,
                        Name = context.DisplayName,
                        Description = $"{context.DisplayName}的所有歌曲"
                    };
                    
                    // 同步加载专辑的歌曲（不使用异步，确保UI能看到歌曲）
                    LoadSongsByAlbum(context.Identifier);
                }
            }

            // 加载所有歌单
            _ = LoadAllPlaylistsAsync();
        }
        
        /// <summary>
        /// 同步版本：根据歌手名称加载歌曲
        /// </summary>
        /// <param name="artistName">歌手名称</param>
        private void LoadSongsByArtist(string artistName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 开始同步加载歌手 {artistName} 的歌曲");
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 数据源中共有 {_playlistDataService.DataSource.Count} 首歌曲");
                
                // 从数据源中获取该歌手的所有歌曲
                var songs = _playlistDataService.DataSource
                    .Where(song => !string.IsNullOrEmpty(song.Artist) && 
                                  string.Equals(song.Artist, artistName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 找到 {songs.Count} 首 {artistName} 的歌曲");
                
                // 更新原始歌曲列表
                _allSongs = songs.ToList();
                
                // 更新过滤后的播放列表
                _filteredPlaylist.Clear();
                foreach (var song in songs)
                {
                    _filteredPlaylist.Add(song);
                    System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 添加歌曲 {song.Title} ({song.Artist})");
                }
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 过滤后的播放列表共有 {_filteredPlaylist.Count} 首歌曲");
                
                OnPropertyChanged(nameof(FilteredPlaylist));
                OnPropertyChanged(nameof(SongCount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 同步加载歌手歌曲失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 异常堆栈: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 同步版本：根据专辑名称加载歌曲
        /// </summary>
        /// <param name="albumName">专辑名称</param>
        private void LoadSongsByAlbum(string albumName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 开始同步加载专辑 {albumName} 的歌曲");
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 数据源中共有 {_playlistDataService.DataSource.Count} 首歌曲");
                
                // 从数据源中获取该专辑的所有歌曲
                var songs = _playlistDataService.DataSource
                    .Where(song => !string.IsNullOrEmpty(song.Album) && 
                                  string.Equals(song.Album, albumName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 找到 {songs.Count} 首 {albumName} 的歌曲");
                
                // 更新原始歌曲列表
                _allSongs = songs.ToList();
                
                // 更新过滤后的播放列表
                _filteredPlaylist.Clear();
                foreach (var song in songs)
                {
                    _filteredPlaylist.Add(song);
                    System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 添加歌曲 {song.Title} ({song.Album})");
                }
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 过滤后的播放列表共有 {_filteredPlaylist.Count} 首歌曲");
                
                OnPropertyChanged(nameof(FilteredPlaylist));
                OnPropertyChanged(nameof(SongCount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 同步加载专辑歌曲失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 清理视图模型资源
        /// </summary>
        public void Cleanup()
        {
            // 取消消息注册
            _messagingService.Unregister(this);
        }

        /// <summary>
        /// 加载当前歌单
        /// </summary>
        /// <param name="playlistId"></param>
        private async Task LoadCurrentPlaylistAsync(int playlistId)
        {
            try
            {
                var playlist = await _playlistCacheService.GetPlaylistByIdAsync(playlistId);
                if (playlist != null)
                {
                    CurrentPlaylist = playlist;
                    // 更新显示名称为歌单名称
                    _currentContextDisplayName = playlist.Name;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 加载当前歌单失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据歌手名称加载歌曲
        /// </summary>
        /// <param name="artistName">歌手名称</param>
        private async Task LoadSongsByArtistAsync(string artistName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 开始加载歌手 {artistName} 的歌曲");
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 数据源中共有 {_playlistDataService.DataSource.Count} 首歌曲");
                
                // 从数据源中获取该歌手的所有歌曲
                var songs = _playlistDataService.DataSource
                    .Where(song => !string.IsNullOrEmpty(song.Artist) && 
                                  string.Equals(song.Artist, artistName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 找到 {songs.Count} 首 {artistName} 的歌曲");
                
                // 更新原始歌曲列表
                _allSongs = songs.ToList();
                
                // 更新过滤后的播放列表
                _filteredPlaylist.Clear();
                foreach (var song in songs)
                {
                    _filteredPlaylist.Add(song);
                    System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 添加歌曲 {song.Title} ({song.Artist})");
                }
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 过滤后的播放列表共有 {_filteredPlaylist.Count} 首歌曲");
                
                OnPropertyChanged(nameof(FilteredPlaylist));
                OnPropertyChanged(nameof(SongCount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 加载歌手歌曲失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 加载歌单歌曲
        /// </summary>
        private async Task LoadPlaylistSongsAsync()
        {
            try
            {
                var songs = await _playlistCacheService.GetSongsByPlaylistIdAsync(_currentPlaylist.Id);
                
                // 更新原始歌曲列表
                _allSongs = songs.ToList();
                
                // 更新过滤后的播放列表
                _filteredPlaylist.Clear();
                foreach (var song in songs)
                {
                    _filteredPlaylist.Add(song);
                }
                
                OnPropertyChanged(nameof(SongCount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 加载歌单歌曲失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载所有歌单
        /// </summary>
        private async Task LoadAllPlaylistsAsync()
        {
            try
            {
                var playlists = await _playlistCacheService.GetAllPlaylistsAsync();
                _allPlaylists.Clear();
                foreach (var playlist in playlists)
                {
                    _allPlaylists.Add(playlist);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 加载所有歌单失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新过滤后的播放列表
        /// </summary>
        private void UpdateFilteredPlaylist()
        {
            // 这里不需要重新加载数据，因为我们已经有了歌单的所有歌曲
            // 只需要根据搜索文本过滤即可
            var filteredSongs = string.IsNullOrWhiteSpace(_searchText)
                ? _allSongs.ToList()
                : _allSongs.Where(song => 
                    (song.Title != null && song.Title.ToLower().Contains(_searchText.ToLower(), System.StringComparison.OrdinalIgnoreCase)) ||
                    (song.Artist != null && song.Artist.ToLower().Contains(_searchText.ToLower(), System.StringComparison.OrdinalIgnoreCase)) ||
                    (song.Album != null && song.Album.ToLower().Contains(_searchText.ToLower(), System.StringComparison.OrdinalIgnoreCase))).ToList();

            // 重新构建过滤后的列表
            _filteredPlaylist.Clear();
            foreach (var song in filteredSongs)
            {
                _filteredPlaylist.Add(song);
            }

            OnPropertyChanged(nameof(SongCount));
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
                    IsPlaying= true;
                    OnPropertyChanged(nameof(IsPlaying));
                    
                    // 设置播放上下文
                    _playbackContextService.SetPlaybackContext(
                        _currentContextType,
                        _currentContextIdentifier,
                        _currentContextDisplayName);
                    
                    System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 设置播放上下文为 {_currentContextType}: {_currentContextDisplayName}");
                    
                    // 发送播放消息
                    var firstSong = _filteredPlaylist.First();
                    _messagingService.Send(new SongSelectionMessage(firstSong, 0));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 播放全部歌曲失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索按钮点击事件
        /// </summary>
        private void ExecuteSearchButtonClick()
        {
            if (!IsSearchExpanded)
            {
                // 第一次点击：展开搜索框并发送焦点请求消息
                IsSearchExpanded = true;
                // 发送消息通知UI获取焦点
                _messagingService.Send(new SearchBoxFocusRequestMessage());
            }
            else
            {
                // 第二次点击：清空搜索文本并折叠搜索框
                SearchText = string.Empty;
                IsSearchExpanded = false;
                // 刷新列表
                UpdateFilteredPlaylist();
            }
        }

        /// <summary>
        /// 播放选中歌曲
        /// </summary>
        /// <param name="song"></param>
        private void ExecutePlaySelectedSong(Song? song)
        {
            if (song != null)
            {
                try
                {
                    // 设置播放上下文
                    _playbackContextService.SetPlaybackContext(
                        _currentContextType,
                        _currentContextIdentifier,
                        _currentContextDisplayName);
                    
                    System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 设置播放上下文为 {_currentContextType}: {_currentContextDisplayName}");
                    
                    // 发送播放消息
                    int index = _filteredPlaylist.IndexOf(song);
                    _messagingService.Send(new SongSelectionMessage(song, index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 播放选中歌曲失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除选中歌曲
        /// </summary>
        /// <param name="song"></param>
        private void ExecuteDeleteSelectedSong(Song? song)
        {
            if (song != null)
            {
                try
                {
                    // 从歌单中删除歌曲
                    _customPlaylistService.RemoveSongFromPlaylistAsync(CurrentPlaylist.Id, song.Id).Wait();
                    // 更新本地列表
                    _filteredPlaylist.Remove(song);
                    OnPropertyChanged(nameof(SongCount));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 删除选中歌曲失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 可以添加消息处理器，例如歌单数据变化时更新UI
        }

       
        /// <summary>
        /// 添加歌曲到歌单
        /// </summary>
        /// <param name="parameter"></param>
        private void ExecuteAddSongToPlaylist(object? parameter)
        {
            if (parameter is object[] parameters && parameters.Length == 2)
            {
                var song = parameters[0] as Song;
                var playlistId = (int)parameters[1];
                
                if (song != null)
                {
                    try
                    {
                        // 添加歌曲到指定歌单
                        _customPlaylistService.AddSongToPlaylistAsync(playlistId, song.Id).Wait();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PlaylistDetailViewModel: 添加歌曲到歌单失败: {ex.Message}");
                    }
                }
            }
        }
    }
}