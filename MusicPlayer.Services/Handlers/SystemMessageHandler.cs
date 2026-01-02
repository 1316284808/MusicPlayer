using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Services.Messages;
using System.Windows;

namespace MusicPlayer.Services.Handlers
{
    /// <summary>
    /// 系统消息处理器 - 处理窗口、通知、配置等系统级消息
    /// 集中处理系统相关的操作，减少组件间的直接依赖
    /// </summary>
    public class SystemMessageHandler : IDisposable
    {
        private readonly IMessagingService _messagingService;
        private readonly WindowManagerService _windowManager;
        private readonly IConfigurationService _configurationService;
        private readonly INotificationService _notificationService;
        private readonly IDispatcherService _dispatcherService;
        private bool _disposed = false;

        public SystemMessageHandler(
            IMessagingService messagingService,
            WindowManagerService windowManager,
            IConfigurationService configurationService,
            INotificationService notificationService,
            IDispatcherService dispatcherService)
        {
            _messagingService = messagingService;
            _windowManager = windowManager;
            _configurationService = configurationService;
            _notificationService = notificationService;
            _dispatcherService = dispatcherService;
            
            System.Diagnostics.Debug.WriteLine($"SystemMessageHandler constructed, _windowManager is null: {_windowManager == null}");

            RegisterMessageHandlers();
        }

        /// <summary>
        /// 注册所有系统相关的消息处理器
        /// </summary>
        private void RegisterMessageHandlers()
        {
            // 窗口控制消息
            _messagingService.Register<WindowStateMessage>(this, OnWindowStateRequested);
            _messagingService.Register<CloseWindowMessage>(this, OnCloseWindowRequested);
            _messagingService.Register<MinimizeWindowMessage>(this, OnMinimizeWindowRequested);
            _messagingService.Register<ToggleMaximizeWindowMessage>(this, OnToggleMaximizeWindowRequested);
            _messagingService.Register<CloseApplicationMessage>(this, OnCloseApplicationRequested);

            // 窗口状态变化消息
            _messagingService.Register<WindowPositionChangedMessage>(this, OnWindowPositionChanged);
            _messagingService.Register<WindowSizeChangedMessage>(this, OnWindowSizeChanged);

            // UI状态消息
            _messagingService.Register<ToggleWallpaperMessage>(this, OnToggleWallpaperRequested);
            _messagingService.Register<ThemeChangedMessage>(this, OnThemeChanged);
            _messagingService.Register<LanguageChangedMessage>(this, OnLanguageChanged);

            // 配置和系统通知消息
            _messagingService.Register<ConfigurationUpdatedMessage>(this, OnConfigurationUpdated);
            _messagingService.Register<ConfigurationQueryMessage>(this, OnConfigurationQuery);
            _messagingService.Register<ConfigurationChangedMessage>(this, OnConfigurationChanged);
            _messagingService.Register<SystemNotificationMessage>(this, OnSystemNotificationRequested);

            // 应用生命周期消息
            _messagingService.Register<ApplicationStartedMessage>(this, OnApplicationStarted);
            _messagingService.Register<ApplicationClosingMessage>(this, OnApplicationClosing);
            _messagingService.Register<ApplicationSuspendedMessage>(this, OnApplicationSuspended);
            _messagingService.Register<ApplicationResumedMessage>(this, OnApplicationResumed);

            // 错误和警告消息
            _messagingService.Register<ErrorMessage>(this, OnErrorMessage);
            _messagingService.Register<WarningMessage>(this, OnWarningMessage);
            _messagingService.Register<InfoMessage>(this, OnInfoMessage);

            // 内存管理消息
            _messagingService.Register<MemoryCleanupRequestedMessage>(this, OnMemoryCleanupRequested);
        }

        #region 窗口控制消息处理

        private void OnWindowStateRequested(object recipient, WindowStateMessage message)
        {
            try
            {
                // 确保在UI线程上执行
                _dispatcherService.Invoke(() =>
                {
                    _windowManager.SetWindowState(message.Value);
                    
                    // 发送状态更新消息，通知TitleBarViewModel更新图标
                    _messagingService.Send(new WindowStateChangedMessage(message.Value));
                });
                
                message.Reply(message.Value);
            }
            catch (Exception ex)
            {
                HandleError("OnWindowStateRequested", ex);
                message.Reply(WindowState.Normal);
            }
        }

        private void OnCloseWindowRequested(object recipient, CloseWindowMessage message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"SystemMessageHandler.OnCloseWindowRequested called, _windowManager is null: {_windowManager == null}");
                
                // 确保在UI线程上执行
                _dispatcherService.Invoke(() => _windowManager.CloseWindow());
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnCloseWindowRequested", ex);
                message.Reply(false);
            }
        }

        private void OnMinimizeWindowRequested(object recipient, MinimizeWindowMessage message)
        {
            try
            {
                // 确保在UI线程上执行
                _dispatcherService.Invoke(() => _windowManager.MinimizeWindow());
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnMinimizeWindowRequested", ex);
                message.Reply(false);
            }
        }

        private void OnToggleMaximizeWindowRequested(object recipient, ToggleMaximizeWindowMessage message)
        {
            try
            {
                // 确保在UI线程上执行
                _dispatcherService.Invoke(() => _windowManager.ToggleMaximize());
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnToggleMaximizeWindowRequested", ex);
                message.Reply(false);
            }
        }

        private void OnCloseApplicationRequested(object recipient, CloseApplicationMessage message)
        {
            try
            {
                // 发送应用即将关闭消息，给其他组件清理资源的机会
                _messagingService.Send(new ApplicationClosingMessage { CanCancel = false, Reason = "User requested" });
                
                // 执行关闭
                _windowManager.CloseApplication();
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnCloseApplicationRequested", ex);
                message.Reply(false);
            }
        }

        #endregion

        #region 窗口状态变化处理

        private void OnWindowPositionChanged(object recipient, WindowPositionChangedMessage message)
        {
            // 窗口位置配置功能已移除
        }

        private void OnWindowSizeChanged(object recipient, WindowSizeChangedMessage message)
        {
            // 窗口尺寸配置功能已移除
        }

        #endregion

        #region UI状态消息处理

        private void OnToggleWallpaperRequested(object recipient, ToggleWallpaperMessage message)
        {
            try
            {
                // 切换壁纸显示
                // _wallpaperService.Toggle();
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnToggleWallpaperRequested", ex);
                message.Reply(false);
            }
        }

        private void OnThemeChanged(object recipient, ThemeChangedMessage message)
        {
            try
            {
                // 更新主题
                // _themeService.SetTheme(message.Theme);
                
                // 保存到配置
                _configurationService.UpdateTheme(message.Theme);
            }
            catch (Exception ex)
            {
                HandleError("OnThemeChanged", ex);
            }
        }

        private void OnLanguageChanged(object recipient, LanguageChangedMessage message)
        {
            try
            {
                // 更新语言
                // _languageService.SetLanguage(message.Value);
                
                // 保存到配置
                _configurationService.UpdateLanguage(message.Value);
            }
            catch (Exception ex)
            {
                HandleError("OnLanguageChanged", ex);
            }
        }

        #endregion

        #region 配置和系统通知消息处理

        private void OnConfigurationUpdated(object recipient, ConfigurationUpdatedMessage message)
        {
            try
            {
                // 处理配置更新
                var configInfo = message.Value;
                
                // 根据配置类型执行相应操作
                switch (configInfo.Section.ToLower())
                {
                    case "player":
                        HandlePlayerConfiguration(configInfo);
                        break;
                    case "ui":
                        HandleUIConfiguration(configInfo);
                        break;
                    case "system":
                        HandleSystemConfiguration(configInfo);
                        break;
                }
            }
            catch (Exception ex)
            {
                HandleError("OnConfigurationUpdated", ex);
            }
        }

        /// <summary>
        /// 处理配置查询消息
        /// </summary>
        private void OnConfigurationQuery(object recipient, ConfigurationQueryMessage message)
        {
            try
            {
                // 执行查询操作
                var result = message.QueryAction(_configurationService.CurrentConfiguration);
                
                // 返回查询结果
                message.Reply(result);
            }
            catch (Exception ex)
            {
                HandleError("OnConfigurationQuery", ex);
                message.Reply(false);
            }
        }

        /// <summary>
        /// 处理配置变化消息
        /// </summary>
        private void OnConfigurationChanged(object recipient, ConfigurationChangedMessage message)
        {
            try
            {
                // 配置已变化，可以在这里执行需要的操作
                System.Diagnostics.Debug.WriteLine($"Configuration changed: IsSpectrumEnabled = {message.Value.IsSpectrumEnabled}");
            }
            catch (Exception ex)
            {
                HandleError("OnConfigurationChanged", ex);
            }
        }

        private void HandlePlayerConfiguration(ConfigurationInfo configInfo)
        {
            // 处理播放器相关配置
            switch (configInfo.Key.ToLower())
            {
                case "volume":
                    // 音量配置已在PlayerStateService处理
                    break;
                case "playmode":
                    // 播放模式配置已在PlayerStateService处理
                    break;
            }
        }

        private void HandleUIConfiguration(ConfigurationInfo configInfo)
        {
            // 处理UI相关配置
            switch (configInfo.Key.ToLower())
            {
                case "theme":
                    // 主题配置已在OnThemeChanged处理
                    break;
                case "language":
                    // 语言配置已在OnLanguageChanged处理
                    break;
            }
        }

        private void HandleSystemConfiguration(ConfigurationInfo configInfo)
        {
            // 处理系统相关配置
        }

        private void OnSystemNotificationRequested(object recipient, SystemNotificationMessage message)
        {
            try
            {
                var notificationInfo = message.Value;
                
                // 通过通知服务显示通知
                if (Enum.TryParse<NotificationType>(notificationInfo.Type, out var notificationType))
                {
                    _notificationService.ShowNotification(
                        notificationInfo.Title,
                        notificationInfo.Message,
                        notificationType);
                }
                else
                {
                    _notificationService.ShowNotification(
                        notificationInfo.Title,
                        notificationInfo.Message,
                        NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                HandleError("OnSystemNotificationRequested", ex);
            }
        }

        #endregion

        #region 应用生命周期消息处理

        private void OnApplicationStarted(object recipient, ApplicationStartedMessage message)
        {
            try
            {
                // 应用启动完成后的初始化工作
                System.Diagnostics.Debug.WriteLine("Application started");
                
                // 发送播放器初始化消息
                _messagingService.Send(new PlayerInitializedMessage());
                
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnApplicationStarted", ex);
                message.Reply(false);
            }
        }

        private void OnApplicationClosing(object recipient, ApplicationClosingMessage message)
        {
            try
            {
                
                //_configurationService.SaveAll();
                
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnApplicationClosing", ex);
                message.Reply(false);
            }
        }

        private void OnApplicationSuspended(object recipient, ApplicationSuspendedMessage message)
        {
            try
            {
                // 应用暂停时的处理
                // 例如：暂停播放
                _messagingService.Send(new PlayPauseMessage());
                
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnApplicationSuspended", ex);
                message.Reply(false);
            }
        }

        private void OnApplicationResumed(object recipient, ApplicationResumedMessage message)
        {
            try
            {
                // 应用恢复时的处理
                System.Diagnostics.Debug.WriteLine("Application resumed");
                
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnApplicationResumed", ex);
                message.Reply(false);
            }
        }

        #endregion

        #region 错误、警告和信息消息处理

        private void OnErrorMessage(object recipient, ErrorMessage message)
        {
            try
            {
                var errorInfo = message.Value;
                
                // 记录错误日志
                System.Diagnostics.Debug.WriteLine($"[ERROR] [{errorInfo.Source}] {errorInfo.Message}");
                if (errorInfo.Exception != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception: {errorInfo.Exception}");
                }
                
                // 显示用户友好的错误通知
                _notificationService.ShowError(errorInfo.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling error message: {ex.Message}");
            }
        }

        private void OnWarningMessage(object recipient, WarningMessage message)
        {
            try
            {
                var warningInfo = message.Value;
                
                // 记录警告日志
                System.Diagnostics.Debug.WriteLine($"[WARNING] [{warningInfo.Source}] {warningInfo.Message}");
                
                // 显示警告通知
                _notificationService.ShowWarning(warningInfo.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling warning message: {ex.Message}");
            }
        }

        private void OnInfoMessage(object recipient, InfoMessage message)
        {
            try
            {
                var infoData = message.Value;
                
                // 记录信息日志
                System.Diagnostics.Debug.WriteLine($"[INFO] [{infoData.Source}] {infoData.Message}");
                
                // 显示信息通知（可选）
                // _notificationService.ShowInfo(infoData.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling info message: {ex.Message}");
            }
        }

        #endregion

        #region 内存管理消息处理

        private void OnMemoryCleanupRequested(object recipient, MemoryCleanupRequestedMessage message)
        {
            try
            {
                // 执行内存清理
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                System.Diagnostics.Debug.WriteLine("Memory cleanup completed");
                
                message.Reply(true);
            }
            catch (Exception ex)
            {
                HandleError("OnMemoryCleanupRequested", ex);
                message.Reply(false);
            }
        }

        #endregion

        #region 错误处理

        private void HandleError(string operation, Exception ex)
        {
            var errorInfo = new ErrorInfo
            {
                Code = "SYSTEM_HANDLER_ERROR",
                Message = $"系统操作失败: {operation}",
                Details = ex.Message,
                Source = "SystemMessageHandler",
                Exception = ex
            };

            // 发送错误消息
            _messagingService.Send(new ErrorMessage(errorInfo));
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