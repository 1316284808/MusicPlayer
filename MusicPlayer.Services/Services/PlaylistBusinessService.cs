using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Interfaces;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 播放列表业务服务实现
    /// 职责：处理播放列表的业务逻辑（排序、过滤、添加/删除歌曲、更新状态等）
    /// </summary>
    public class PlaylistBusinessService : IPlaylistBusinessService
    {
        private readonly IPlaylistCacheService _cacheService;
        private readonly IConfigurationService _configurationService;
        private readonly IMessagingService _messagingService;
        private readonly IDispatcherService _dispatcherService;
        private readonly IPlaylistService _playlistService;

        public PlaylistBusinessService(
            IPlaylistCacheService cacheService,
            IConfigurationService configurationService,
            IMessagingService messagingService,
            IDispatcherService dispatcherService,
            IPlaylistService playlistService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
        }

        /// <summary>
        /// 设置排序规则并重新加载
        /// </summary>
        public async Task SetSortRuleAsync(SortRule sortRule)
        {
            Debug.WriteLine($"PlaylistBusinessService: 设置排序规则 {sortRule}");

            // 保存配置
            _configurationService.CurrentConfiguration.SortRule = sortRule;
            _configurationService.SaveCurrentConfiguration();

            // 从缓存获取当前播放列表并排序
            var sortedPlaylist = _cacheService.GetSortedPlaylist(sortRule);

            // 更新缓存中的排序后的数据
            _cacheService.UpdatePlaylist(sortedPlaylist);

            Debug.WriteLine($"PlaylistBusinessService: 排序完成，歌曲数量: {sortedPlaylist.Count}");

            // 在UI线程上发送通知
            await _dispatcherService.InvokeAsync(() =>
            {
                _messagingService.Send(new PlaylistDataChangedMessage(
                    DataChangeType.Sort,
                    sortRule,
                    new List<Song>(sortedPlaylist),
                    null)); // CurrentSong将由调用方提供
            });
        }

        /// <summary>
        /// 应用过滤器
        /// </summary>
        public void ApplyFilter(string filterText)
        {
            Debug.WriteLine($"PlaylistBusinessService: 应用过滤器 '{filterText}'");

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
                    _configurationService.CurrentConfiguration.SortRule,
                    new List<Song>(currentPlaylist),
                    null)); // CurrentSong将由调用方提供
            });
        }

        /// <summary>
        /// 移除歌曲
        /// </summary>
        public void RemoveSong(Song song)
        {
            Debug.WriteLine($"PlaylistBusinessService: 移除歌曲 '{song.Title}'");

            var playlist = _cacheService.GetPlaylist();
            if (playlist.Contains(song))
            {
                // 使用缓存服务移除歌曲
                _cacheService.RemoveSongs(new[] { song.FilePath });

                Debug.WriteLine($"PlaylistBusinessService: 歌曲已移除 '{song.Title}'");

                // 发送播放列表更新消息
                var updatedPlaylist = _cacheService.GetPlaylist();
                _dispatcherService.Invoke(() =>
                {
                    _messagingService.Send(new PlaylistDataChangedMessage(
                        DataChangeType.RemoveSongs,
                        _configurationService.CurrentConfiguration.SortRule,
                        new List<Song>(updatedPlaylist),
                        null)); // CurrentSong将由调用方处理
                });
            }
        }

        /// <summary>
        /// 清空播放列表
        /// </summary>
        public async Task ClearPlaylistAsync()
        {
            Debug.WriteLine("PlaylistBusinessService: 清空播放列表");

            // 清空缓存
            _cacheService.ClearCache();

            // 重新初始化缓存
            await _cacheService.InitializeCacheAsync();

            Debug.WriteLine("PlaylistBusinessService: 播放列表已清空");

            // 发送播放列表更新消息
            _dispatcherService.Invoke(() =>
            {
                _messagingService.Send(new PlaylistDataChangedMessage(
                    DataChangeType.Clear,
                    _configurationService.CurrentConfiguration.SortRule,
                    new List<Song>(),
                    null));
            });
        }

        /// <summary>
        /// 添加歌曲
        /// </summary>
        public async Task AddSongsAsync(IEnumerable<Song> songs)
        {
            var songsToAdd = songs.ToList();
            Debug.WriteLine($"PlaylistBusinessService: 添加 {songsToAdd.Count} 首歌曲");

            // 使用缓存服务添加歌曲，确保避免重复
            int addedCount = _cacheService.AddSongs(songsToAdd);

            if (addedCount == 0)
            {
                Debug.WriteLine("PlaylistBusinessService: 没有新歌曲需要添加");
                return;
            }

            Debug.WriteLine($"PlaylistBusinessService: 实际添加了 {addedCount} 首新歌曲");

            // 从缓存获取更新后的播放列表
            var updatedPlaylist = _cacheService.GetPlaylist();

            // 发送播放列表更新消息
            await _dispatcherService.InvokeAsync(() =>
            {
                _messagingService.Send(new PlaylistDataChangedMessage(
                    DataChangeType.AddSongs,
                    _configurationService.CurrentConfiguration.SortRule,
                    new List<Song>(updatedPlaylist),
                    null)); // CurrentSong将由调用方提供
            });
        }

        /// <summary>
        /// 更新歌曲收藏状态
        /// </summary>
        public async Task UpdateSongFavoriteStatusAsync(Song song, bool isFavorite)
        {
            try
            {
                Debug.WriteLine($"PlaylistBusinessService: 更新歌曲收藏状态 '{song.Title}' -> {isFavorite}");

                // 从缓存中获取歌曲
                var playlist = _cacheService.GetPlaylist();
                var songToUpdate = playlist.FirstOrDefault(s => s.FilePath == song.FilePath);

                if (songToUpdate != null)
                {
                    // 异步处理歌单歌曲关联
                    if (isFavorite)
                    {
                        // 添加到收藏列表
                        await _cacheService.AddSongToPlaylistAsync(1, songToUpdate.Id);
                        Debug.WriteLine($"更新歌曲收藏状态: {song.Title}, 添加到收藏列表");
                    }
                    else
                    {
                        // 从收藏列表移除
                        await _cacheService.RemoveSongFromPlaylistAsync(1, songToUpdate.Id);
                        Debug.WriteLine($"更新歌曲收藏状态: {song.Title}, 从收藏列表移除");
                    }

                    // 发送播放列表数据变化消息，通知UI更新
                    _messagingService.Send(new PlaylistDataChangedMessage(
                        DataChangeType.SongUpdated,
                        _configurationService.CurrentConfiguration.SortRule,
                        new List<Song>(playlist),
                        null)); // CurrentSong将由调用方提供
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新歌曲收藏状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新歌曲删除状态
        /// </summary>
        public async Task UpdateSongDeletionStatusAsync(Song song, bool isDeleted)
        {
            try
            {
                Debug.WriteLine($"PlaylistBusinessService: 更新歌曲删除状态 '{song.Title}' -> {isDeleted}");

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
                    await _cacheService.UpdateSongStatusInDatabaseAsync(songToUpdate);

                    Debug.WriteLine($"更新歌曲删除状态: {song.Title}, 删除状态: {isDeleted} (已更新缓存并异步保存到数据库)");

                    // 发送播放列表数据变化消息，通知UI更新
                    _messagingService.Send(new PlaylistDataChangedMessage(
                        DataChangeType.SongUpdated,
                        _configurationService.CurrentConfiguration.SortRule,
                        new List<Song>(playlist),
                        null)); // CurrentSong将由调用方提供
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新歌曲删除状态失败: {ex.Message}");
            }
        }
    }
}