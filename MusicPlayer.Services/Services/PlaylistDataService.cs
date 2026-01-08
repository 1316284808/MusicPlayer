using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Services.Messages;
 
using System.IO;
using MusicPlayer.Core.Interfaces;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Services
{
/// <summary>
/// 播放列表数据服务实现 - 数据访问层
/// 负责整个应用程序的播放列表数据管理和持久化
/// 
/// 数据流设计：
/// 1. 初始化时：从PlaylistCacheService获取已加载的数据
/// 2. 运行时：通过DataSource临时获取数据，使用后立即清空
/// 3. 操作：所有数据操作通过PlaylistCacheService进行
/// 
/// 注意：不应维护本地缓存，应始终使用PlaylistCacheService作为唯一数据源
/// </summary>
    public class PlaylistDataService : IPlaylistDataService, IDisposable
    {
        private readonly IPlaylistCacheService _cacheService;
        private readonly IPlaylistService _playlistService;
        private readonly IMessagingService _messagingService;
        private readonly IDispatcherService _dispatcherService;
        private readonly IConfigurationService _configurationService;
        private readonly IPlaybackContextService _playbackContextService;

        private Song? _currentSong;
        private SortRule _currentSortRule = SortRule.ByAddedTime;
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
            get 
            {
                lock (_lock)
                {
                    return _currentSortRule;
                }
            }
            set 
            {
                lock (_lock)
                {
                    if (_currentSortRule != value)
                    {
                        _currentSortRule = value;
                    }
                }
            }
        }

        public Song? CurrentSong
        {
            get
            {
                lock (_lock)
                {
                    return _currentSong;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (_currentSong != value)
                    {
                        var oldSong = _currentSong;
                        _currentSong = value;
                        
                        System.Diagnostics.Debug.WriteLine($"PlaylistDataService: CurrentSong 更新 - 旧歌曲: {oldSong?.Title}, 新歌曲: {value?.Title}");
                        
                        // 在UI线程上发送消息
                        _dispatcherService.Invoke(() =>
                        {
                            _messagingService.Send(new CurrentSongChangedMessage(value));
                        });
                    }
                }
            }
        }

        public PlaylistDataService(
            IPlaylistCacheService cacheService,
            IPlaylistService playlistService,
            IMessagingService messagingService,
            IDispatcherService dispatcherService,
            IConfigurationService configurationService,
            IPlaybackContextService playbackContextService)
        {
            System.Diagnostics.Debug.WriteLine("PlaylistDataService: 构造函数开始执行");
            
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _playbackContextService = playbackContextService ?? throw new ArgumentNullException(nameof(playbackContextService));

            // 获取当前排序规则
            _currentSortRule = _configurationService.CurrentConfiguration.SortRule;
            
            System.Diagnostics.Debug.WriteLine("PlaylistDataService: 构造函数执行完成");
            System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 播放列表数据库路径: {Paths.PlaylistDatabasePath}");

            // 注册消息处理器
            System.Diagnostics.Debug.WriteLine("PlaylistDataService: 准备注册消息处理器");
            RegisterMessageHandlers();
            System.Diagnostics.Debug.WriteLine("PlaylistDataService: 消息处理器注册完成");
            
            System.Diagnostics.Debug.WriteLine("PlaylistDataService: 构造函数完全执行完成");
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            _messagingService.Register<AddFilesMessage>(this, async (receiver, message) =>
            {
                await HandleAddFilesMessage(message);
            });

            _messagingService.Register<SortPlaylistMessage>(this, async (receiver, message) =>
            {
                await HandleSortPlaylistMessage(message);
            });

            _messagingService.Register<ClearPlaylistMessage>(this, async (receiver, message) =>
            {
                await HandleClearPlaylistMessage();
            });
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
                var sortedPlaylist = _cacheService.GetSortedPlaylist(_currentSortRule);
                
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
                    song.DelayAlbumArtLoading = true;
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
                lock (_lock)
                {
                    _currentSong = null;
                }

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
            CurrentSong = song;
        }
        
        /// <summary>
        /// 设置当前歌曲（不发送消息，用于初始化恢复）
        /// </summary>
        public void SetCurrentSongWithoutNotification(Song? song)
        {
            lock (_lock)
            {
                _currentSong = song;
            }
        }

        

        /// <summary>
        /// 获取歌曲在列表中的索引
        /// </summary>
        public int GetSongIndex(Song song)
        {
            lock (_lock)
            {
                var playlist = _cacheService.GetPlaylist();
                return playlist.IndexOf(song);
            }
        }

        /// <summary>
        /// 获取下一首歌曲
        /// 重构后从缓存服务获取播放列表
        /// </summary>
        public Song? GetNextSong(PlayMode playMode = PlayMode.RepeatAll)
        {
            lock (_lock)
            {
                // 获取当前播放上下文
                var context = _playbackContextService.CurrentPlaybackContext;
                var currentSong = CurrentSong;
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: GetNextSong 开始 - 播放上下文: {context}, 播放模式: {playMode}, 当前歌曲: {currentSong?.Title}");
                
                // 如果当前歌曲为空，返回null
                if (currentSong == null) 
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistDataService: GetNextSong - 当前歌曲为空，返回null");
                    return null;
                }

                // 获取对应的提供者
                try
                {
                    var provider = _playbackContextService.GetProvider(context.Type);
                    
                    // 使用提供者获取下一首歌曲
                    var nextSong = provider.GetNextSong(context, currentSong, playMode);
                    
                    System.Diagnostics.Debug.WriteLine($"PlaylistDataService: GetNextSong - 播放上下文: {context}, 播放模式: {playMode}, 当前歌曲: {currentSong?.Title}, 下一首: {nextSong?.Title}");
                    
                    return nextSong;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistDataService: GetNextSong - 异常: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// 获取上一首歌曲
        /// 重构后从缓存服务获取播放列表
        /// </summary>
        public Song? GetPreviousSong(PlayMode playMode = PlayMode.RepeatAll)
        {
            lock (_lock)
            {
                // 获取当前播放上下文
                var context = _playbackContextService.CurrentPlaybackContext;
                var currentSong = CurrentSong;
                
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: GetPreviousSong 开始 - 播放上下文: {context}, 播放模式: {playMode}, 当前歌曲: {currentSong?.Title}");
                
                // 如果当前歌曲为空，返回null
                if (currentSong == null) 
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistDataService: GetPreviousSong - 当前歌曲为空，返回null");
                    return null;
                }

                // 获取对应的提供者
                try
                {
                    var provider = _playbackContextService.GetProvider(context.Type);
                    
                    // 使用提供者获取上一首歌曲
                    var previousSong = provider.GetPreviousSong(context, currentSong, playMode);
                    
                    System.Diagnostics.Debug.WriteLine($"PlaylistDataService: GetPreviousSong - 播放上下文: {context}, 播放模式: {playMode}, 当前歌曲: {currentSong?.Title}, 上一首: {previousSong?.Title}");
                    
                    return previousSong;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistDataService: GetPreviousSong - 异常: {ex.Message}");
                    return null;
                }
            }
        }

        #region 新增方法实现

        /// <summary>
        /// 设置排序规则
        /// </summary>
        public void SetSortRule(SortRule sortRule)
        {
            lock (_lock)
            {
                if (_currentSortRule != sortRule)
                {
                    _currentSortRule = sortRule;
                    
                    // 保存配置
                    _configurationService.CurrentConfiguration.SortRule = sortRule;
                    _configurationService.SaveCurrentConfiguration();
                    
                    // 异步保存并重新加载
                    _ = Task.Run(async () => await SaveAndReloadAsync(sortRule));
                }
            }
        }

        /// <summary>
        /// 应用过滤器
        /// </summary>
        public void ApplyFilter(string filterText)
        {
            // 过滤功能在UI层面实现，这里只需更新配置
            _configurationService.CurrentConfiguration.FilterText = filterText;
            _configurationService.SaveCurrentConfiguration();
            
            // 获取当前播放列表
            var currentPlaylist = _cacheService.GetPlaylist();
            
            // 发送过滤更新消息
            _dispatcherService.Invoke(() =>
            {
                _messagingService.Send(new PlaylistDataChangedMessage(
                    DataChangeType.Filter, 
                    CurrentSortRule,
                     new List<Song>(currentPlaylist), 
                    CurrentSong));
            });
        }

        /// <summary>
        /// 移除歌曲
        /// </summary>
        public void RemoveSong(Song song)
        {
            lock (_lock)
            {
                var playlist = _cacheService.GetPlaylist();
                if (playlist.Contains(song))
                {
                    // 使用缓存服务移除歌曲
                    _cacheService.RemoveSongs(new[] { song.FilePath });
                    
                    // 如果移除的是当前播放歌曲，清空当前歌曲
                    if (_currentSong == song)
                    {
                        _currentSong = null;
                    }
                    
                    // 异步保存并重新加载
                    _ = Task.Run(async () => await SaveAndReloadAsync());
                }
            }
        }

        /// <summary>
        /// 清空播放列表
        /// </summary>
        public void ClearPlaylist()
        {
            // 直接调用清空并重新加载的方法
            _ = Task.Run(async () => await ClearAndReloadAsync());
        }

        /// <summary>
        /// 添加歌曲（直接添加，不重新加载）
        /// </summary>
        public void AddSongs(IEnumerable<Song> songs)
        {
            // 直接调用添加并重新加载的方法
            _ = Task.Run(async () => await AddSongsAndReloadAsync(songs));
        }

        /// <summary>
        /// 更新歌曲收藏状态
        /// </summary>
        /// <param name="song">要更新的歌曲</param>
        /// <param name="isFavorite">收藏状态</param>
        public void UpdateSongFavoriteStatus(Song song, bool isFavorite)
        {
            try
            {
                lock (_lock)
                {
                    // 从缓存中获取歌曲
                    var playlist = _cacheService.GetPlaylist();
                    var songToUpdate = playlist.FirstOrDefault(s => s.FilePath == song.FilePath);
                    
                    if (songToUpdate != null)
                    {
                        // 更新歌曲的收藏状态
                        songToUpdate.Heart = isFavorite;
                        
                        // 更新缓存中的播放列表
                        _cacheService.UpdatePlaylist(playlist);
                        
                        // 异步保存歌曲状态到数据库
                        _ = Task.Run(async () => await _cacheService.UpdateSongStatusInDatabaseAsync(songToUpdate));
                        
                        System.Diagnostics.Debug.WriteLine($"更新歌曲收藏状态: {song.Title}, 收藏状态: {isFavorite} (已更新缓存并异步保存到数据库)");
                        
                        // 发送播放列表数据变化消息，通知UI更新
                        _messagingService.Send(new PlaylistDataChangedMessage(
                            DataChangeType.SongUpdated, 
                            _currentSortRule, 
                            new List<Song>(playlist), 
                            _currentSong));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新歌曲收藏状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新歌曲删除状态
        /// </summary>
        /// <param name="song">要更新的歌曲</param>
        /// <param name="isDeleted">删除状态</param>
        public void UpdateSongDeletionStatus(Song song, bool isDeleted)
        {
            try
            {
                lock (_lock)
                {
                    // 从缓存中获取歌曲
                    var playlist = _cacheService.GetPlaylist();
                    var songToUpdate = playlist.FirstOrDefault(s => s.FilePath == song.FilePath);
                    
                    if (songToUpdate != null)
                    {
                        // 更新歌曲的删除状态
                        songToUpdate.IsDeleted = isDeleted;
                        
                        // 更新缓存中的播放列表
                        _cacheService.UpdatePlaylist(playlist);
                        
                        // 异步保存歌曲状态到数据库
                        _ = Task.Run(async () => await _cacheService.UpdateSongStatusInDatabaseAsync(songToUpdate));
                        
                        System.Diagnostics.Debug.WriteLine($"更新歌曲删除状态: {song.Title}, 删除状态: {isDeleted} (已更新缓存并异步保存到数据库)");
                        
                        // 发送播放列表数据变化消息，通知UI更新
                        _messagingService.Send(new PlaylistDataChangedMessage(
                            DataChangeType.SongUpdated, 
                            _currentSortRule, 
                            new List<Song>(playlist), 
                            _currentSong));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新歌曲删除状态失败: {ex.Message}");
            }
        }

        #endregion

        #region 私有方法

        // 注意：LoadFromJsonAsyncInternal 方法已被移除
        // 现在数据初始化在程序启动时由 ServiceCoordinator.InitializePlaylistAsyncSynchronously() 处理
        // 这样可以确保数据在用户操作前就已经加载完成，避免竞态条件

        /// <summary>
        /// 排序歌曲列表
        /// </summary>
        private List<Song> SortSongs(List<Song> songs, SortRule sortRule)
        {
            if (songs.Count <= 1) return songs;

            try
            {
                return sortRule switch
                {
                    SortRule.ByAddedTime => songs.OrderByDescending(s => s.AddedTime).ToList(),
                    SortRule.ByTitle => songs.OrderBy(s => s.Title ?? "", StringComparer.OrdinalIgnoreCase).ToList(),
                    SortRule.ByArtist => songs.OrderBy(s => s.Artist ?? "", StringComparer.OrdinalIgnoreCase).ToList(),
                    SortRule.ByAlbum => songs.OrderBy(s => s.Album ?? "", StringComparer.OrdinalIgnoreCase).ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase).ToList(),
                    SortRule.ByDuration => songs.OrderBy(s => s.Duration).ToList(),
                    SortRule.ByFileSize => songs.OrderBy(s => s.FileSize).ToList(),
                    _ => songs.OrderByDescending(s => s.AddedTime).ToList()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistDataService: 排序失败: {ex.Message}");
                return songs;
            }
        }

        /// <summary>
        /// 重新定位当前播放歌曲
        /// </summary>
        private void RelocateCurrentSong()
        {
            bool shouldSendUpdate = false;
            Song? songToUpdate = null;
            
            lock (_lock)
            {
                if (_currentSong != null)
                {
                    var playlist = _cacheService.GetPlaylist();
                    var currentIndex = playlist.IndexOf(_currentSong);
                    if (currentIndex == -1)
                    {
                        // 当前歌曲不在列表中，清空当前歌曲
                        _currentSong = null;
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
    }
}