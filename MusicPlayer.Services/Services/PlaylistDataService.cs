using System.Collections.Generic;
using System.Threading.Tasks;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;
using MusicPlayer.Core.Interfaces;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Services
{
/// <summary>
/// 播放列表数据服务实现 - 数据访问协调器
/// 职责：协调播放列表数据的加载、保存和持久化操作
/// 
/// 重构后：
/// - 消息处理 → PlaylistMessageHandler
/// - 导航逻辑 → PlaylistNavigationService  
/// - 业务逻辑 → PlaylistBusinessService
/// - 状态管理 → PlaylistStateService
/// - 数据访问协调 → PlaylistDataService (本类)
/// </summary>
    public class PlaylistDataService : IPlaylistDataService, IDisposable
    {
        private readonly IPlaylistCacheService _cacheService;
        private readonly IPlaylistService _playlistService;
        private readonly IMessagingService _messagingService;
        private readonly IDispatcherService _dispatcherService;
        private readonly IConfigurationService _configurationService;
        private readonly IPlaylistNavigationService _navigationService;
        private readonly IPlaylistBusinessService _businessService;
        private readonly IPlaylistStateService _stateService;

        private readonly object _lock = new object();
        
        // 临时数据缓存，使用后立即清空
        private List<Song>? _tempDataCache = null;

        public List<Song> DataSource 
        { 
            get 
            {
                lock (_lock)
                {
                    // 获取数据并存储到临时缓存
                    _tempDataCache = _cacheService.GetPlaylist();
                    return _tempDataCache;
                }
            }
        }

        /// <summary>
        /// 清除临时数据缓存
        /// </summary>
        public void ClearDataSource()
        {
            lock (_lock)
            {
                _tempDataCache = null;
                System.Diagnostics.Debug.WriteLine("PlaylistDataService: 临时数据缓存已清空");
            }
        }

        public SortRule CurrentSortRule 
        { 
            get => _stateService.CurrentSortRule;
            set => _stateService.CurrentSortRule = value;
        }

        public Song? CurrentSong
        {
            get => _stateService.CurrentSong;
            set => _stateService.CurrentSong = value;
        }

        public PlaylistDataService(
            IPlaylistCacheService cacheService,
            IPlaylistService playlistService,
            IMessagingService messagingService,
            IDispatcherService dispatcherService,
            IConfigurationService configurationService,
            IPlaybackContextService playbackContextService,
            IPlaylistNavigationService navigationService,
            IPlaylistBusinessService businessService,
            IPlaylistStateService stateService)
        {
            System.Diagnostics.Debug.WriteLine("PlaylistDataService: 构造函数开始执行");
            
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));

            // 初始化状态
            _stateService.CurrentSortRule = _configurationService.CurrentConfiguration.SortRule;
            
            System.Diagnostics.Debug.WriteLine("PlaylistDataService: 构造函数执行完成");
            System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 播放列表数据库路径: {Paths.PlaylistDatabasePath}");
        }

        /// <summary>
        /// 检查缓存是否已初始化
        /// </summary>
        /// <returns>缓存是否已初始化</returns>
        public bool IsInitialized()
        {
            return _cacheService.IsCacheReady;
        }

        /// <summary>
        /// 从JSON文件加载数据
        /// 重构后此方法主要用于初始化通知，实际数据加载由缓存服务处理
        /// </summary>
        public async Task LoadFromDataAsync()
        {
            try
            {
                // 确保缓存已初始化
                if (!_cacheService.IsCacheReady)
                {
                    await _cacheService.InitializeCacheAsync();
                }
                // 应用排序规则
                var sortedPlaylist = _cacheService.GetSortedPlaylist(_stateService.CurrentSortRule);
                
                // 更新缓存中的排序后的数据
                _cacheService.UpdatePlaylist(sortedPlaylist);

                // 在UI线程上发送通知
                await _dispatcherService.InvokeAsync(() =>
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistDataService: 发送PlaylistDataChangedMessage");
                    _messagingService.Send(new PlaylistDataChangedMessage(
                        DataChangeType.InitialLoad, 
                        CurrentSortRule, 
                        new List<Song>(sortedPlaylist),
                        CurrentSong));
                });

                System.Diagnostics.Debug.WriteLine("PlaylistDataService: 数据加载完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 加载数据失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 异常堆栈: {ex.StackTrace}");

            }
        }

        /// <summary>
        /// 保存到JSON并重新加载
        /// 重构后使用缓存服务确保单一数据源
        /// </summary>
        public async Task SaveAndReloadAsync(SortRule? sortRule = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 开始保存并重载，排序规则: {sortRule ?? CurrentSortRule}");

                SortRule finalSortRule;
                List<Song> dataToSave;

                lock (_lock)
                {
                    if (sortRule.HasValue)
                    {
                        CurrentSortRule = sortRule.Value;
                        // 保存排序规则到配置
                        _configurationService.CurrentConfiguration.SortRule = CurrentSortRule;
                        //_configurationService.SaveCurrentConfiguration();
                    }
                    finalSortRule = CurrentSortRule;
                    
                    // 从缓存获取当前播放列表并排序
                    var currentPlaylist = _cacheService.GetPlaylist();
                    dataToSave = _cacheService.GetSortedPlaylist(finalSortRule);
                }

                // 更新缓存中的排序后的数据
                _cacheService.UpdatePlaylist(dataToSave);

              

                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 保存完成，歌曲数量: {dataToSave.Count}");

                // 重新定位当前播放歌曲
                RelocateCurrentSong();

                // 在UI线程上发送通知
                await _dispatcherService.InvokeAsync(() =>
                {
                    _messagingService.Send(new PlaylistDataChangedMessage(
                        DataChangeType.Sort, 
                        finalSortRule, 
                        new List<Song>(dataToSave),
                        CurrentSong));
                });

                System.Diagnostics.Debug.WriteLine("PlaylistDataService: 保存并重载完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 保存并重载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加歌曲并重新加载
        /// 重构后使用缓存服务确保单一数据源
        /// </summary>
        public async Task AddSongsAndReloadAsync(IEnumerable<Song> songs)
        {
            try
            {
                var songsToAdd = songs.ToList();
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 开始添加 {songsToAdd.Count} 首歌曲");

                // 使用缓存服务添加歌曲，确保避免重复
                int addedCount = _cacheService.AddSongs(songsToAdd);
                
                if (addedCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistDataService: 没有新歌曲需要添加");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 实际添加了 {addedCount} 首新歌曲");
                
                // 从缓存获取更新后的播放列表
                var updatedPlaylist = _cacheService.GetPlaylist();
                
                // 延迟加载专辑封面
                foreach (var song in updatedPlaylist.Where(s => songsToAdd.Any(a => a.FilePath == s.FilePath)))
                {

                }
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 添加完成，实际添加 {addedCount} 首歌曲，总计 {updatedPlaylist.Count} 首");

                // 在UI线程上发送通知
                await _dispatcherService.InvokeAsync(() =>
                {
                    _messagingService.Send(new PlaylistDataChangedMessage(
                        DataChangeType.AddSongs, 
                        CurrentSortRule, 
                        new List<Song>(updatedPlaylist),
                        CurrentSong));
                });

                System.Diagnostics.Debug.WriteLine("PlaylistDataService: 添加歌曲并重载完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 添加歌曲并重载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空播放列表并重新加载
        /// </summary>
        public async Task ClearAndReloadAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PlaylistDataService: 开始清空播放列表");

                // 清空缓存
                _cacheService.ClearCache();

                // 重新初始化缓存
                await _cacheService.InitializeCacheAsync();

                // 清除当前歌曲
                _stateService.SetCurrentSongWithoutNotification(null);

                System.Diagnostics.Debug.WriteLine("PlaylistDataService: 清空并保存完成");

                // 在UI线程上发送通知
                _dispatcherService.Invoke(() =>
                {
                    _messagingService.Send(new PlaylistDataChangedMessage(
                        DataChangeType.Clear, 
                        CurrentSortRule, 
                        new List<Song>(),
                        null));
                });

                System.Diagnostics.Debug.WriteLine("PlaylistDataService: 清空播放列表并重载完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 清空播放列表并重载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置当前歌曲
        /// </summary>
        public void SetCurrentSong(Song? song)
        {
            _stateService.CurrentSong = song;
        }
        
        /// <summary>
        /// 设置当前歌曲（不发送消息，用于初始化恢复）
        /// </summary>
        public void SetCurrentSongWithoutNotification(Song? song)
        {
            _stateService.SetCurrentSongWithoutNotification(song);
        }

        

        /// <summary>
        /// 获取歌曲在列表中的索引
        /// </summary>
        public int GetSongIndex(Song song)
        {
            return _navigationService.GetSongIndex(song);
        }

        /// <summary>
        /// 获取下一首歌曲
        /// 重构后委托给PlaylistNavigationService
        /// </summary>
        public Song? GetNextSong(PlayMode playMode = PlayMode.RepeatAll)
        {
            return _navigationService.GetNextSong(playMode, CurrentSong);
        }

        /// <summary>
        /// 获取上一首歌曲
        /// 重构后委托给PlaylistNavigationService
        /// </summary>
        public Song? GetPreviousSong(PlayMode playMode = PlayMode.RepeatAll)
        {
            return _navigationService.GetPreviousSong(playMode, CurrentSong);
        }

        #region 业务逻辑委托

        /// <summary>
        /// 设置排序规则（委托给PlaylistBusinessService）
        /// </summary>
        public void SetSortRule(SortRule sortRule) => _ = _businessService.SetSortRuleAsync(sortRule);

        /// <summary>
        /// 应用过滤器（委托给PlaylistBusinessService）
        /// </summary>
        public void ApplyFilter(string filterText) => _businessService.ApplyFilter(filterText);

        /// <summary>
        /// 移除歌曲（委托给PlaylistBusinessService）
        /// </summary>
        public void RemoveSong(Song song) => _businessService.RemoveSong(song);

        /// <summary>
        /// 清空播放列表（委托给PlaylistBusinessService）
        /// </summary>
        public void ClearPlaylist() => _ = _businessService.ClearPlaylistAsync();

        /// <summary>
        /// 添加歌曲（委托给PlaylistBusinessService）
        /// </summary>
        public void AddSongs(IEnumerable<Song> songs) => _ = _businessService.AddSongsAsync(songs);

        /// <summary>
        /// 更新歌曲收藏状态（委托给PlaylistBusinessService）
        /// </summary>
        public async void UpdateSongFavoriteStatus(Song song, bool isFavorite) => _ = _businessService.UpdateSongFavoriteStatusAsync(song, isFavorite);

        /// <summary>
        /// 更新歌曲删除状态（委托给PlaylistBusinessService）
        /// </summary>
        public void UpdateSongDeletionStatus(Song song, bool isDeleted) => _ = _businessService.UpdateSongDeletionStatusAsync(song, isDeleted);

        #endregion

        #region 私有方法

        /// <summary>
        /// 重新定位当前播放歌曲
        /// </summary>
        private void RelocateCurrentSong()
        {
            bool shouldSendUpdate = false;
            Song? songToUpdate = null;
            
            lock (_lock)
            {
                var currentSong = _stateService.CurrentSong;
                if (currentSong != null)
                {
                    var playlist = _cacheService.GetPlaylist();
                    var currentIndex = playlist.IndexOf(currentSong);
                    if (currentIndex == -1)
                    {
                        // 当前歌曲不在列表中，清空当前歌曲
                        _stateService.SetCurrentSongWithoutNotification(null);
                        shouldSendUpdate = true;
                        songToUpdate = null;
                    }
                }
            }
            
            // 如果需要更新，在UI线程上发送消息
            if (shouldSendUpdate)
            {
                _dispatcherService.Invoke(() =>
                {
                    _messagingService.Send(new CurrentSongChangedMessage(songToUpdate));
                });
            }
        }

        #endregion

        #region 消息处理器

        /// <summary>
        /// 处理添加文件消息
        /// </summary>
        private async Task HandleAddFilesMessage(AddFilesMessage message)
        {
            try
            {
                var filePaths = message.FilePaths.ToList();
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 收到添加文件消息，文件数量: {filePaths.Count}");

                var songs = new List<Song>();
                foreach (var filePath in filePaths)
                {
                        if (System.IO.File.Exists(filePath))
                        {
                            // 使用PlaylistService来提取歌曲元数据信息
                            var song = _playlistService.ExtractSongInfo(filePath);
                            if (song != null)
                            {
                                songs.Add(song);
                            }
                        }
                }

                if (songs.Count > 0)
                {
                    await AddSongsAndReloadAsync(songs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 处理添加文件消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理排序播放列表消息
        /// </summary>
        private async Task HandleSortPlaylistMessage(SortPlaylistMessage message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 收到排序消息，排序规则: {message.SortRule}");
                await SaveAndReloadAsync(message.SortRule);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 处理排序消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理清空播放列表消息
        /// </summary>
        private async Task HandleClearPlaylistMessage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PlaylistDataService: 收到清空播放列表消息");
                await ClearAndReloadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 处理清空播放列表消息失败: {ex.Message}");
            }
        }

        #endregion
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _playlistService?.Dispose();
        }

        #region 消息处理器（已迁移到PlaylistMessageHandler）
        // 消息处理逻辑已迁移到 PlaylistMessageHandler
        #endregion
    }
}