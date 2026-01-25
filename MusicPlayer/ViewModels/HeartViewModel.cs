using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Interfaces;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
        private readonly IPlaylistService _playlistService;
        private readonly INotificationService _notificationService;

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

        /// <summary>
        /// 添加音乐命令
        /// </summary>
        public ICommand AddMusicCommand { get; }

        /// <summary>
        /// 选择目录命令
        /// </summary>
        public ICommand SelectDirectoryCommand { get; }

        /// <summary>
        /// 播放歌单命令
        /// </summary>
        public ICommand PlayAlbumCommand { get; }

        /// <summary>
        /// 修改歌单命令
        /// </summary>
        public ICommand UpdatePlaylistCommand { get; }

        /// <summary>
        /// 删除歌单命令
        /// </summary>
        public ICommand DeletePlaylistCommand { get; }

        public HeartViewModel(
            IMessagingService messagingService,
            IPlaylistDataService playlistDataService,
            IPlaybackContextService playbackContextService,
            IDialogService dialogService,
            IPlaylistCacheService playlistCacheService,
            IPlaylistService playlistService,
            INotificationService notificationService)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _playlistCacheService = playlistCacheService ?? throw new ArgumentNullException(nameof(playlistCacheService));
            _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            // 初始化命令
            CreatePlaylistCommand = new AsyncRelayCommand(CreatePlaylistAsync);
            SelectPlaylistCommand = new RelayCommand<Playlist>(ExecuteSelectPlaylist);
            AddMusicCommand = new RelayCommand(async () => await ExecuteAddMusic());
            SelectDirectoryCommand = new RelayCommand(async () => await ExecuteSelectDirectory());
            PlayAlbumCommand = new RelayCommand<string>(ExecutePlayAlbum);
            UpdatePlaylistCommand = new AsyncRelayCommand<Playlist>(UpdatePlaylistAsync);
            DeletePlaylistCommand = new AsyncRelayCommand<Playlist>(DeletePlaylistAsync);

            // 注册消息处理器
            RegisterMessageHandlers();
        }

        /// <summary>
        /// 执行添加音乐命令
        /// </summary>
        private async Task ExecuteAddMusic()
        {
            _messagingService.Send(new AddMusicFilesMessage());
        }

        /// <summary>
        /// 执行选择目录命令
        /// </summary>
        private async Task ExecuteSelectDirectory()
        {
            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderDialog.Description = "选择音乐文件夹";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;
                    ImportSongsFromFolder(folderPath);
                }
            }
        }

        /// <summary>
        /// 从文件夹导入歌曲
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        public void ImportSongsFromFolder(string folderPath)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (!Directory.Exists(folderPath))
                    {
                        _notificationService.ShowWarning("文件夹不存在");
                        return;
                    }

                    // 扫描文件夹中的歌曲
                    var songs = _playlistService.LoadSongsFromFolder(folderPath);
                    if (songs.Count == 0)
                    {
                        _notificationService.ShowInfo("未发现音乐文件");
                        return;
                    }

                    // 添加歌曲到数据库
                    await _playlistDataService.AddSongsAndReloadAsync(songs);
                    
                    // 更新歌单列表
                    await LoadPlaylistsAsync();

                    _notificationService.ShowSuccess($"成功添加 {songs.Count} 首歌曲");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HeartViewModel: 导入歌曲失败: {ex.Message}");
                    _notificationService.ShowError("导入歌曲失败");
                }
            });
        }

        /// <summary>
        /// 初始化视图模型
        /// </summary>
        public override void Initialize()
        {
            System.Diagnostics.Debug.WriteLine("HeartViewModel: Initialize 方法被调用");
            // 重置选中的播放列表
            SelectedPlaylist = null;
            // 加载歌单数据
            _ = LoadPlaylistsAsync();
        }

        /// <summary>
        /// 清理视图模型资源
        /// </summary>
        public override void Cleanup()
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
        /// 当前歌曲变化时的处理
        /// </summary>
        private void OnCurrentSongChanged(object? sender, CurrentSongChangedMessage message)
        {
            UpdatePlaylistPlayingState();
        }

        /// <summary>
        /// 播放状态变化时的处理
        /// </summary>
        private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedMessage message)
        {
            UpdatePlaylistPlayingState();
        }

        /// <summary>
        /// 更新歌单播放状态
        /// </summary>
        private void UpdatePlaylistPlayingState()
        {
            // 重置所有歌单的播放状态
            foreach (var playlist in MusicGroups)
            {
                playlist.IsPlaying = false;
            }

            // 获取当前播放上下文
            var context = _playbackContextService.CurrentPlaybackContext;
            if (context.Type == Core.Enums.PlaybackContextType.CustomPlaylist)
            {
                // 查找当前播放的歌单
                var playlist = MusicGroups.FirstOrDefault(p => p.Name.Equals(context.DisplayName, StringComparison.OrdinalIgnoreCase));
                if (playlist != null)
                {
                    // 设置为当前播放状态
                    playlist.IsPlaying = true;
                }
            }
        }

        /// <summary>
        /// 播放歌单命令执行方法
        /// </summary>
        private async void ExecutePlayAlbum(string? playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            // 获取该歌单
            var playlist = MusicGroups.FirstOrDefault(p => p.Name.Equals(playlistName, StringComparison.OrdinalIgnoreCase));
            if (playlist != null)
            {
                // 设置播放上下文为该歌单
                _playbackContextService.SetPlaybackContext(
                    Core.Enums.PlaybackContextType.CustomPlaylist,
                    playlist.Id.ToString(),
                    playlist.Name);

                System.Diagnostics.Debug.WriteLine($"HeartViewModel: 设置播放上下文为歌单: {playlist.Name}");

                // 获取该歌单的第一首歌
                var songs = await _playlistCacheService.GetSongsByPlaylistIdAsync(playlist.Id);
                if (songs.Count > 0)
                {
                    // 发送播放消息
                    _messagingService.Send(new PlaySelectedSongMessage(songs[0]));
                }
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

                    // 检查是否为"收藏列表"，不允许用户创建同名歌单
                    if (name == "收藏列表")
                    {
                        _notificationService.ShowWarning("无法创建名为'收藏列表'的歌单，该名称为系统保留");
                        return;
                    }

                    // 检查是否已存在同名歌单
                    var existingPlaylists = await _playlistCacheService.GetAllPlaylistsAsync();
                    if (existingPlaylists.Any(p => p.Name == name))
                    {
                        _notificationService.ShowWarning($"已存在名为'{name}'的歌单，请使用其他名称");
                        return;
                    }

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
                    _notificationService.ShowSuccess($"成功创建新歌单[{name}]");
                    LoadPlaylistsAsync();
                    System.Diagnostics.Debug.WriteLine($"HeartViewModel: 成功创建新歌单: {name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HeartViewModel: 创建歌单失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 修改歌单逻辑
        /// </summary>
        private async Task UpdatePlaylistAsync(Playlist playlist)
        {
            try
            {
                // 防护逻辑：系统默认列表（如收藏列表）不能被修改
                // 使用ID和名称双重判断，确保系统列表的安全性
                if (playlist.Id == 0 || playlist.Name == "收藏列表")
                {
                    System.Diagnostics.Debug.WriteLine($"HeartViewModel: 系统默认列表不能被修改");
                    _notificationService.ShowWarning("系统默认列表不能被修改");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"HeartViewModel: 开始修改歌单: {playlist.Name}");

                // 调用对话框服务显示修改歌单对话框
                var result = await _dialogService.ShowEditPlaylistDialogAsync("修改歌单信息", playlist.Name, playlist.Description);

                if (result != null && result.Length == 2)
                {
                    string name = result[0];
                    string description = result[1];
                    if (name.Length <= 0) return;

                    // 更新歌单信息
                    playlist.Name = name;
                    playlist.Description = description;
                    playlist.UpdatedTime = DateTime.Now;

                    // 保存到数据库和缓存
                    await _playlistCacheService.UpdatePlaylistAsync(playlist);

                    // 重新加载歌单列表，确保UI同步
                    await LoadPlaylistsAsync();

                    System.Diagnostics.Debug.WriteLine($"HeartViewModel: 成功修改歌单: [{name}]");
                    
                    // 显示修改成功通知
                    _notificationService.ShowSuccess($"歌单 [{name}] 已成功修改");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HeartViewModel: 修改歌单失败: {ex.Message}");
                _notificationService.ShowError($"修改歌单失败");
            }
        }

        /// <summary>
        /// 删除歌单逻辑
        /// </summary>
        private async Task DeletePlaylistAsync(Playlist playlist)
        {
            try
            {
                // 防护逻辑：系统默认列表（如收藏列表）不能被删除
                // 使用ID和名称双重判断，确保系统列表的安全性
                if (playlist.Id == 0 || playlist.Name == "收藏列表")
                {
                    System.Diagnostics.Debug.WriteLine($"HeartViewModel: 系统默认列表不能被删除");
                    _notificationService.ShowWarning("系统默认列表不能被删除");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"HeartViewModel: 开始删除歌单: {playlist.Name}");

                // 显示确认对话框
                bool isConfirmed = await _dialogService.ShowConfirmationAsync($"确定要删除歌单 [{playlist.Name}] 吗？", "确认删除");
                if (isConfirmed)
                {
                    // 删除数据库中的歌单
                    await _playlistCacheService.DeletePlaylistAsync(playlist.Id);

                    // 重新加载歌单列表，确保UI同步
                    await LoadPlaylistsAsync(); 
                    // 显示删除成功通知
                    _notificationService.ShowSuccess($"歌单 [{playlist.Name}] 已成功删除");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HeartViewModel: 删除歌单失败: {ex.Message}");
                _notificationService.ShowError($"删除歌单 [{playlist.Name}] 失败");
            }
        }
    }
}