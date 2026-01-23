using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Interfaces;
using MusicPlayer.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 歌单页面视图模型
    /// </summary>
    public class HeartViewModel : ObservableObject, IHeartViewModel
    {
        private readonly IMessagingService _messagingService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaybackContextService _playbackContextService;
        private readonly IDialogService _dialogService;
        private readonly IPlaylistCacheService _playlistCacheService;

        /// <summary>
        /// 音乐分组列表 - 绑定到UI的歌单列表
        /// </summary>
        private ObservableCollection<Playlist> _musicGroups = new ObservableCollection<Playlist>();

        /// <summary>
        /// 音乐分组列表 - 绑定到UI的歌单列表
        /// </summary>
        public ObservableCollection<Playlist> MusicGroups
        {
            get { return _musicGroups; }
            set
            {
                if (_musicGroups != value)
                {
                    _musicGroups = value;
                    OnPropertyChanged(nameof(MusicGroups));
                }
            }
        }

        /// <summary>
        /// 当前选中的播放列表
        /// </summary>
        private Core.Models.Playlist? _selectedPlaylist;

        /// <summary>
        /// 当前选中的播放列表
        /// </summary>
        public Core.Models.Playlist? SelectedPlaylist
        {
            get { return _selectedPlaylist; }
            set
            {
                if (_selectedPlaylist != value)
                {
                    _selectedPlaylist = value;
                    OnPropertyChanged(nameof(SelectedPlaylist));
                    // 当播放列表被选中时执行命令
                    if (value != null)
                    {
                        ExecuteSelectPlaylist(value);
                    }
                }
            }
        }

        /// <summary>
        /// 新建歌单命令
        /// </summary>
        public ICommand CreatePlaylistCommand { get; }

        /// <summary>
        /// 选择歌单命令
        /// </summary>
        public ICommand SelectPlaylistCommand { get; }

        public HeartViewModel(
            IMessagingService messagingService,
            IPlaylistDataService playlistDataService,
            IPlaybackContextService playbackContextService,
            IDialogService dialogService,
            IPlaylistCacheService playlistCacheService)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _playlistCacheService = playlistCacheService ?? throw new ArgumentNullException(nameof(playlistCacheService));

            // 初始化命令
            CreatePlaylistCommand = new AsyncRelayCommand(CreatePlaylistAsync);
            SelectPlaylistCommand = new RelayCommand<Playlist>(ExecuteSelectPlaylist);
        }

        /// <summary>
        /// 初始化视图模型
        /// </summary>
        public void Initialize()
        {
            System.Diagnostics.Debug.WriteLine("HeartViewModel: Initialize 方法被调用");
            // 加载歌单数据
            LoadPlaylistsAsync();
        }

        /// <summary>
        /// 清理视图模型资源
        /// </summary>
        public void Cleanup()
        {
            System.Diagnostics.Debug.WriteLine("HeartViewModel: Cleanup 方法被调用");
            // 这里可以添加清理逻辑，比如取消消息注册、释放资源等
            _messagingService.Unregister(this);
        }

        /// <summary>
        /// 加载播放列表数据
        /// </summary>
        private async Task LoadPlaylistsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("HeartViewModel: 开始加载播放列表数据");
                
                // 从缓存服务获取所有播放列表
                var playlists = await _playlistCacheService.GetAllPlaylistsAsync();
                
                // 清空现有列表并添加新数据
                _musicGroups.Clear();
                foreach (var playlist in playlists)
                {
                    // 获取每个播放列表的歌曲数量
                    var songs = await _playlistCacheService.GetSongsByPlaylistIdAsync(playlist.Id);
                    playlist.SongCount = songs.Count;

                    _musicGroups.Add(playlist);
                }
                
                System.Diagnostics.Debug.WriteLine($"HeartViewModel: 加载了 {MusicGroups.Count} 个播放列表");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HeartViewModel: 加载播放列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 选择歌单逻辑
        /// </summary>
        private void ExecuteSelectPlaylist(Playlist? playlist)
        {
            if (playlist != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"HeartViewModel: 选择了歌单: {playlist.Name}");
                    
                    // 设置播放上下文
                    _playbackContextService.SetPlaybackContext(
                        Core.Enums.PlaybackContextType.CustomPlaylist,
                        playlist.Id.ToString(),
                        playlist.Name);
                    System.Diagnostics.Debug.WriteLine($"HeartViewModel: 设置播放上下文为 {playlist.Name}");
                    
                    // 发送导航到歌单详情页面的消息
                    _messagingService.Send<MusicPlayer.Services.Messages.NavigateToPlaylistDetailMessage, bool>(new MusicPlayer.Services.Messages.NavigateToPlaylistDetailMessage());
                    System.Diagnostics.Debug.WriteLine("HeartViewModel: 发送导航到歌单详情页面的消息");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HeartViewModel: 选择歌单失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 新建歌单逻辑
        /// </summary>
        private async Task CreatePlaylistAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("HeartViewModel: 开始创建新歌单");
                
                // 调用对话框服务显示新建歌单对话框
                var result = await _dialogService.ShowCreatePlaylistDialogAsync("创建新歌单");
                
                if (result != null && result.Length == 2)
                {
                    string name = result[0];
                    string description = result[1];
                    if (name.Length <= 0) return;

                    // 创建新的播放列表
                    var playlist = new Playlist
                    {
                        Name = name,
                        Description = description,
                        CreatedTime = DateTime.Now,
                        UpdatedTime = DateTime.Now,
                        IsDefault = false
                    };
                    
                    // 保存到数据库和缓存
                    await _playlistCacheService.InsertPlaylistAsync(playlist);
                    
                    // 更新UI
                    playlist.SongCount = 0;
                    MusicGroups.Add(playlist);
                    
                    System.Diagnostics.Debug.WriteLine($"HeartViewModel: 成功创建新歌单: {name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HeartViewModel: 创建歌单失败: {ex.Message}");
            }
        }
    }
}