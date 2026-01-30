using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Interfaces;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.Services.Handlers
{
    /// <summary>
    /// 播放列表消息处理器 - 专门处理播放列表相关的消息
    /// 职责：接收并处理所有播放列表相关的消息，调用相应的服务执行操作
    /// </summary>
    public class PlaylistMessageHandler : IDisposable
    {
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaylistService _playlistService;
        private readonly IMessagingService _messagingService;
        private readonly IDispatcherService _dispatcherService;

        public PlaylistMessageHandler(
            IPlaylistDataService playlistDataService,
            IPlaylistService playlistService,
            IMessagingService messagingService,
            IDispatcherService dispatcherService)
        {
            _playlistDataService = playlistDataService ?? throw new ArgumentNullException(nameof(playlistDataService));
            _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

            Debug.WriteLine("PlaylistMessageHandler: 注册消息处理器");
            RegisterMessageHandlers();
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 注册添加文件消息
            _messagingService.Register<AddFilesMessage>(this, async (receiver, message) =>
            {
                await HandleAddFilesMessage(message);
            });

            // 注册排序播放列表消息
            _messagingService.Register<SortPlaylistMessage>(this, async (receiver, message) =>
            {
                await HandleSortPlaylistMessage(message);
            });

            // 注册清空播放列表消息
            _messagingService.Register<ClearPlaylistMessage>(this, async (receiver, message) =>
            {
                await HandleClearPlaylistMessage();
            });

            // 注册更新歌曲删除状态消息
            _messagingService.Register<UpdateSongDeletionStatusMessage>(this, async (receiver, message) =>
            {
                await HandleUpdateSongDeletionStatusMessage(message);
            });
        }

        /// <summary>
        /// 处理添加文件消息
        /// </summary>
        private async Task HandleAddFilesMessage(AddFilesMessage message)
        {
            try
            {
                var filePaths = message.FilePaths.ToList();
                Debug.WriteLine($"PlaylistMessageHandler: 处理添加文件消息，文件数量: {filePaths.Count}");

                var songs = new List<Core.Models.Song>();
                foreach (var filePath in filePaths)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        // 使用PlaylistService提取歌曲元数据
                        var song = _playlistService.ExtractSongInfo(filePath);
                        if (song != null)
                        {
                            songs.Add(song);
                        }
                    }
                }

                if (songs.Count > 0)
                {
                    await _playlistDataService.AddSongsAndReloadAsync(songs);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlaylistMessageHandler: 处理添加文件消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理排序播放列表消息
        /// </summary>
        private async Task HandleSortPlaylistMessage(SortPlaylistMessage message)
        {
            try
            {
                Debug.WriteLine($"PlaylistMessageHandler: 处理排序消息，排序规则: {message.SortRule}");
                await _playlistDataService.SaveAndReloadAsync(message.SortRule);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlaylistMessageHandler: 处理排序消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理清空播放列表消息
        /// </summary>
        private async Task HandleClearPlaylistMessage()
        {
            try
            {
                Debug.WriteLine("PlaylistMessageHandler: 处理清空播放列表消息");
                await _playlistDataService.ClearAndReloadAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlaylistMessageHandler: 处理清空播放列表消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理更新歌曲删除状态消息
        /// </summary>
        private async Task HandleUpdateSongDeletionStatusMessage(UpdateSongDeletionStatusMessage message)
        {
            try
            {
                Debug.WriteLine($"PlaylistMessageHandler: 处理更新歌曲删除状态消息，歌曲: {message.Song.Title}, 删除状态: {message.IsDeleted}");
                
                // 调用PlaylistDataService的更新方法
                _playlistDataService.UpdateSongDeletionStatus(message.Song, message.IsDeleted);
                
                Debug.WriteLine($"PlaylistMessageHandler: 更新歌曲删除状态成功");
                
                // 回复成功
                message.Reply(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlaylistMessageHandler: 处理更新歌曲删除状态消息失败: {ex.Message}");
                message.Reply(false);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 注销所有消息注册
            _messagingService.Unregister<AddFilesMessage>(this);
            _messagingService.Unregister<SortPlaylistMessage>(this);
            _messagingService.Unregister<ClearPlaylistMessage>(this);
            _messagingService.Unregister<UpdateSongDeletionStatusMessage>(this);
        }
    }
}