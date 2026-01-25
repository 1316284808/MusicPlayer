using System;
using Microsoft.Extensions.Logging;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 系统通知服务实现
    /// 提供统一的跨平台通知功能，支持不同类型通知的显示
    /// 已重构为使用依赖注入，符合MVVM架构原则
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly Func<IPlayerService> _playerServiceFactory;
        private readonly Func<IPlaylistDataService> _playlistDataServiceFactory;
        private readonly Func<IMessagingService> _messagingServiceFactory;
        private readonly IUINotificationService _uiNotificationService;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger<NotificationService> _logger;
        private readonly ISystemTrayService _systemTrayService;

        /// <summary>
        /// 初始化通知服务
        /// </summary>
        /// <param name="playerServiceFactory">播放服务工厂</param>
        /// <param name="playlistDataServiceFactory">播放列表数据服务工厂</param>
        /// <param name="messagingServiceFactory">消息服务工厂</param>
        /// <param name="uiNotificationService">UI通知服务</param>
        /// <param name="dispatcherService">调度器服务</param>
        /// <param name="systemTrayService">系统托盘服务</param>
        /// <param name="logger">日志记录器</param>
        public NotificationService(
            Func<IPlayerService> playerServiceFactory,
            Func<IPlaylistDataService> playlistDataServiceFactory,
            Func<IMessagingService> messagingServiceFactory,
            IUINotificationService uiNotificationService,
            IDispatcherService dispatcherService,
            ISystemTrayService systemTrayService,
            ILogger<NotificationService> logger)
        {
            _playerServiceFactory = playerServiceFactory ?? throw new ArgumentNullException(nameof(playerServiceFactory));
            _playlistDataServiceFactory = playlistDataServiceFactory ?? throw new ArgumentNullException(nameof(playlistDataServiceFactory));
            _messagingServiceFactory = messagingServiceFactory ?? throw new ArgumentNullException(nameof(messagingServiceFactory));
            _uiNotificationService = uiNotificationService ?? throw new ArgumentNullException(nameof(uiNotificationService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
            _systemTrayService = systemTrayService ?? throw new ArgumentNullException(nameof(systemTrayService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 初始化通知系统
        /// </summary>
        public void Initialize()
        {
            try
            {
                _logger.LogInformation("初始化通知服务");
                
                // 检查系统托盘服务是否可用
                if (_systemTrayService.IsTrayIconAvailable)
                {
                    _logger.LogInformation("系统托盘服务可用");
                }
                else
                {
                    _logger.LogWarning("系统托盘服务不可用");
                }
                
                // 订阅播放状态变更消息
                var messagingService = _messagingServiceFactory();
                if (messagingService != null)
                {
                    // 可以在这里订阅相关消息来显示通知
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化通知服务失败");
            }
        }

        /// <summary>
        /// 显示信息类型通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        public void ShowInfo(string message)
        {
            try
            {
                _dispatcherService.Invoke(() =>
                {
                    try
                    {
                        // 优先使用系统托盘显示通知
                        if (_systemTrayService.IsTrayIconAvailable)
                        {
                            _systemTrayService.ShowBalloonTip("信息", message, MusicPlayer.Core.Interface.BalloonIcon.Info);
                        }
                        else
                        {
                            _uiNotificationService.ShowInfo("信息", message);
                        }
                        _logger.LogInformation("显示信息通知: {Message}", message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "显示信息通知失败: {Message}", message);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调度器调用失败: {Message}", message);
            }
        }

        /// <summary>
        /// 显示成功类型通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        public void ShowSuccess(string message)
        {
            _dispatcherService.Invoke(() =>
            {
                try
                {
                    _uiNotificationService.ShowSuccess("成功", message);
                    _logger.LogInformation("显示成功通知: {Message}", message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示成功通知失败: {Message}", message);
                }
            });
        }

        /// <summary>
        /// 显示警告类型通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        public void ShowWarning(string message)
        {
            _dispatcherService.Invoke(() =>
            {
                try
                {
                    _uiNotificationService.ShowWarning("警告", message);
                    _logger.LogWarning("显示警告通知: {Message}", message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示警告通知失败: {Message}", message);
                }
            });
        }

        /// <summary>
        /// 显示错误类型通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        public void ShowError(string message)
        {
            _dispatcherService.Invoke(() =>
            {
                try
                {
                    _uiNotificationService.ShowError("错误", message);
                    _logger.LogError("显示错误通知: {Message}", message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示错误通知失败: {Message}", message);
                }
            });
        }

        /// <summary>
        /// 显示自定义通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知消息内容</param>
        /// <param name="notificationType">通知类型</param>
        public void ShowNotification(string title, string message, NotificationType notificationType = NotificationType.Info)
        {
            _dispatcherService.Invoke(() =>
            {
                try
                {
                    switch (notificationType)
                    {
                        case NotificationType.Success:
                            _uiNotificationService.ShowSuccess(title, message);
                            break;
                        case NotificationType.Warning:
                            _uiNotificationService.ShowWarning(title, message);
                            break;
                        case NotificationType.Error:
                            _uiNotificationService.ShowError(title, message);
                            break;
                        case NotificationType.Info:
                        default:
                            _uiNotificationService.ShowInfo(title, message);
                            break;
                    }
                    
                    _logger.LogInformation("显示自定义通知: {Title} - {Message} ({Type})", title, message, notificationType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "显示自定义通知失败: {Title} - {Message} ({Type})", title, message, notificationType);
                }
            });
        }

        /// <summary>
        /// 显示系统托盘通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知消息内容</param>
        /// <param name="icon">通知图标类型</param>
        public void ShowBalloonTip(string title, string message, MusicPlayer.Core.Interface.BalloonIcon icon = MusicPlayer.Core.Interface.BalloonIcon.Info)
        {
            try
            {
                _systemTrayService.ShowBalloonTip(title, message, icon);
                _logger.LogInformation("显示系统托盘通知: {Title} - {Message}", title, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示系统托盘通知失败: {Title} - {Message}", title, message);
            }
        }

        /// <summary>
        /// 隐藏系统托盘通知
        /// </summary>
        public void HideBalloonTip()
        {
            try
            {
                _systemTrayService.HideBalloonTip();
                _logger.LogInformation("隐藏系统托盘通知");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "隐藏系统托盘通知失败");
            }
        }

        /// <summary>
        /// 清理通知资源
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _logger.LogInformation("清理通知服务资源");
                
                // 清理系统托盘资源
                _systemTrayService.Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理通知服务资源失败");
            }
        }
    }
}