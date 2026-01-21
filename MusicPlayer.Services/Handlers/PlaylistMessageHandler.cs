using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;
using MusicPlayer.Services.Messages;
using System.Collections.ObjectModel;

namespace MusicPlayer.Services.Handlers
{
    /// <summary>
    /// 播放列表消息处理器 - 处理所有播放列表相关的消息
    /// 集中处理播放列表操作，减少组件间的直接依赖
    /// </summary>
    public class PlaylistMessageHandler : IDisposable
    {
        private readonly IMessagingService _messagingService;
        private readonly IPlaylistDataService _playlistDataService;
        private readonly IPlaylistService _playlistService;
        private readonly INotificationService _notificationService;
        private readonly IPlaylistCommandHandler _playlistCommandHandler;
        private bool _disposed = false;

        public PlaylistMessageHandler(
            IMessagingService messagingService,
            IPlaylistDataService playlistDataService,
            IPlaylistService playlistService,
            INotificationService notificationService,
            IPlaylistCommandHandler playlistCommandHandler)
        {
            _messagingService = messagingService;
            _playlistDataService = playlistDataService;
            _playlistService = playlistService;
            _notificationService = notificationService;
            _playlistCommandHandler = playlistCommandHandler;

            RegisterMessageHandlers();
        }

        /// <summary>
        /// 注册所有播放列表相关的消息处理器
        /// 只保留与UI操作相关的消息处理器，数据相关的消息由PlaylistDataService处理
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 只保留UI操作相关的消息处理器
            _messagingService.Register<AddMusicFilesMessage>(this, OnAddMusicFilesRequested);
            _messagingService.Register<TogglePlaylistMessage>(this, OnTogglePlaylistRequested);
            // 导航消息
            _messagingService.Register<NavigateToSettingsMessage>(this, OnNavigateToSettingsRequested);
            _messagingService.Register<NavigateToHomeMessage>(this, OnNavigateToHomeRequested);
            _messagingService.Register<NavigateToSingerPageMessage>(this, OnNavigateToSingerPageRequested);
            _messagingService.Register<NavigateToAlbumPageMessage>(this, OnNavigateToAlbumPageRequested);
            
            // 收藏相关消息
            _messagingService.Register<UpdateSongFavoriteStatusMessage>(this, OnUpdateSongFavoriteStatusRequested);
            
            // 删除相关消息
            _messagingService.Register<UpdateSongDeletionStatusMessage>(this, OnUpdateSongDeletionStatusRequested);
        }

          

        #region 播放列表控制消息处理

        private async void OnAddMusicFilesRequested(object recipient, AddMusicFilesMessage message)
        {
            try
            {
                // 通过PlaylistCommandHandler处理添加音乐文件
                var result = await _playlistCommandHandler.HandleAddMusicCommand();
                message.Reply(result);
            }
            catch (Exception ex)
            {
                HandleError("OnAddMusicFilesRequested", ex);
                message.Reply(false);
            }
        }

        private void OnTogglePlaylistRequested(object recipient, TogglePlaylistMessage message)
        {
            try
            {
                // 通过PlaylistDataService切换播放列表折叠状态
                // 这里需要发送一个消息来更新UI状态
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnTogglePlaylistRequested", ex);
                message.Reply(false);
            }
        }

        private void OnPlaylistSortRequested(object recipient, PlaylistSortMessage message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistMessageHandler: 收到排序请求 - SortBy: {message.SortBy}, Ascending: {message.Ascending}");
                
                // 将字符串转换为SortRule枚举
                if (Enum.TryParse<SortRule>(message.SortBy, out var sortRule))
                {
                    // 更新排序规则（通过PlaylistDataService）
                    _playlistDataService.SetSortRule(sortRule);
                    
                    System.Diagnostics.Debug.WriteLine($"PlaylistMessageHandler: 排序规则已更新为 {sortRule}");
                    
                    // 排序规则更新会自动触发 UI 刷新
                    System.Diagnostics.Debug.WriteLine($"PlaylistMessageHandler: 排序规则更新完成，UI将自动刷新");
                    
                    message.Reply(true);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"PlaylistMessageHandler: 无法解析排序规则 {message.SortBy}");
                    message.Reply(false);
                }
            }
            catch (Exception ex)
            {
                HandleError("OnPlaylistSortRequested", ex);
                message.Reply(false);
            }
        }

        private void OnPlaylistFilterRequested(object recipient, PlaylistFilterMessage message)
        {
            try
            {
                // 通过PlaylistDataService执行过滤
                _playlistDataService.ApplyFilter(message.FilterText);
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnPlaylistFilterRequested", ex);
                message.Reply(false);
            }
        }

        #endregion

       

        #region 播放列表状态变化处理


        #endregion

        #region 导航消息处理

        private void OnNavigateToSettingsRequested(object recipient, NavigateToSettingsMessage message)
        {
            try
            {
                // 只需要回复消息，导航由MainWindow处理
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnNavigateToSettingsRequested", ex);
                message.Reply(false);
            }
        }
        
        private void OnNavigateToHomeRequested(object recipient, NavigateToHomeMessage message)
        {
            try
            {
                // 只需要回复消息，导航由MainWindow处理
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnNavigateToHomeRequested", ex);
                message.Reply(false);
            }
        }
        
        private void OnNavigateToSingerPageRequested(object recipient, NavigateToSingerPageMessage message)
        {
            try
            {
                // 只需要回复消息，导航由MainWindow处理
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnNavigateToSingerPageRequested", ex);
                message.Reply(false);
            }
        }
        
        private void OnNavigateToAlbumPageRequested(object recipient, NavigateToAlbumPageMessage message)
        {
            try
            {
                // 只需要回复消息，导航由MainWindow处理
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnNavigateToAlbumPageRequested", ex);
                message.Reply(false);
            }
        }

        #endregion

        #region 错误处理

        private void OnErrorMessage(object recipient, ErrorMessage message)
        {
            // 处理错误消息，可以记录日志、显示通知等
            var errorInfo = message.Value;
            System.Diagnostics.Debug.WriteLine($"[{errorInfo.Source}] Error: {errorInfo.Message}");
            
            // 可以在这里添加日志记录、通知显示等逻辑
            // 例如：_notificationService.ShowError(errorInfo.Message);
        }

        private void HandleError(string operation, Exception ex)
        {
            var errorInfo = new ErrorInfo
            {
                Code = "PLAYLIST_ERROR",
                Message = $"播放列表操作失败: {operation}",
                Details = ex.Message,
                Source = "PlaylistMessageHandler",
                Exception = ex
            };

            // 发送错误消息
            _messagingService.Send(new ErrorMessage(errorInfo));

            // 也可以通过通知服务显示用户友好的错误信息
            var notificationInfo = new SystemNotificationInfo
            {
                Title = "播放列表操作失败",
                Message = errorInfo.Message,
                Type = "Error"
            };
            _messagingService.Send(new SystemNotificationMessage(notificationInfo));
        }

        #endregion

        #region 收藏相关消息处理

        private void OnUpdateSongFavoriteStatusRequested(object recipient, UpdateSongFavoriteStatusMessage message)
        {
            try
            {
                // 通过PlaylistDataService更新歌曲收藏状态
                _playlistDataService.UpdateSongFavoriteStatus(message.Song, message.IsFavorite);
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnUpdateSongFavoriteStatusRequested", ex);
                message.Reply(false);
            }
        }

        #endregion

        #region 删除相关消息处理

        private void OnUpdateSongDeletionStatusRequested(object recipient, UpdateSongDeletionStatusMessage message)
        {
            try
            {
                // 通过PlaylistDataService更新歌曲删除状态
                _playlistDataService.UpdateSongDeletionStatus(message.Song, message.IsDeleted);
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnUpdateSongDeletionStatusRequested", ex);
                message.Reply(false);
            }
        }

        #endregion

        #region 资源清理

        public void Dispose()
        {
            if (!_disposed)
            {
                _messagingService.Unregister(this);
                _disposed = true;
            }
        }

        #endregion
    }
}