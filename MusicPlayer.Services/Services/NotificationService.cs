using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using MusicPlayer.Core.Interface;
using Hardcodet.Wpf.TaskbarNotification;

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
        private readonly IDialogService _dialogService;
        private readonly IDispatcherService _dispatcherService;
        private readonly ILogger<NotificationService> _logger;
        private TaskbarIcon? _taskbarIcon;

        /// <summary>
        /// 初始化通知服务
        /// </summary>
        /// <param name="playerServiceFactory">播放服务工厂</param>
        /// <param name="playlistDataServiceFactory">播放列表数据服务工厂</param>
        /// <param name="messagingServiceFactory">消息服务工厂</param>
        /// <param name="dialogService">对话框服务</param>
        /// <param name="dispatcherService">调度器服务</param>
        /// <param name="logger">日志记录器</param>
        public NotificationService(
            Func<IPlayerService> playerServiceFactory,
            Func<IPlaylistDataService> playlistDataServiceFactory,
            Func<IMessagingService> messagingServiceFactory,
            IDialogService dialogService,
            IDispatcherService dispatcherService,
            ILogger<NotificationService> logger)
        {
            _playerServiceFactory = playerServiceFactory ?? throw new ArgumentNullException(nameof(playerServiceFactory));
            _playlistDataServiceFactory = playlistDataServiceFactory ?? throw new ArgumentNullException(nameof(playlistDataServiceFactory));
            _messagingServiceFactory = messagingServiceFactory ?? throw new ArgumentNullException(nameof(messagingServiceFactory));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
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
                
                // 获取系统托盘图标
                if (Application.Current != null)
                {
                    _taskbarIcon = Application.Current.FindResource("NotifyIcon") as TaskbarIcon;
                    if (_taskbarIcon != null)
                    {
                        _logger.LogInformation("系统托盘图标获取成功");
                    }
                    else
                    {
                        _logger.LogWarning("无法获取系统托盘图标资源");
                    }
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
                        if (_taskbarIcon != null)
                        {
                            _taskbarIcon.ShowBalloonTip("信息", message, BalloonIcon.Info);
                        }
                        else
                        {
                            _dialogService.ShowInformation(message, "信息");
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
                    //_dialogService.ShowInformation(message, "成功");
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
                    _dialogService.ShowWarningAsync(message, "警告");
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
                    _dialogService.ShowErrorAsync(message, "错误");
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
                            _dialogService.ShowInformation(message, title);
                            break;
                        case NotificationType.Warning:
                            _dialogService.ShowWarningAsync(message, title);
                            break;
                        case NotificationType.Error:
                            _dialogService.ShowErrorAsync(message, title);
                            break;
                        case NotificationType.Info:
                        default:
                            _dialogService.ShowInformation(message, title);
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
        public void ShowBalloonTip(string title, string message, BalloonIcon icon = BalloonIcon.Info)
        {
            try
            {
                _dispatcherService.Invoke(() =>
                {
                    try
                    {
                        if (_taskbarIcon != null)
                        {
                            _taskbarIcon.ShowBalloonTip(title, message, icon);
                            _logger.LogInformation("显示系统托盘通知: {Title} - {Message}", title, message);
                        }
                        else
                        {
                            _logger.LogWarning("系统托盘图标为空，无法显示通知");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "显示系统托盘通知失败: {Title} - {Message}", title, message);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调度器调用失败: {Title} - {Message}", title, message);
            }
        }

        /// <summary>
        /// 隐藏系统托盘通知
        /// </summary>
        public void HideBalloonTip()
        {
            try
            {
                _dispatcherService.Invoke(() =>
                {
                    try
                    {
                        if (_taskbarIcon != null)
                        {
                            _taskbarIcon.HideBalloonTip();
                            _logger.LogInformation("隐藏系统托盘通知");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "隐藏系统托盘通知失败");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调度器调用失败，无法隐藏系统托盘通知");
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
                
                // 清理托盘图标
                _taskbarIcon?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理通知服务资源失败");
            }
        }
    }
}