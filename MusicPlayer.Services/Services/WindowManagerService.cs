using System;
using System.Windows;
using System.Windows.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services.Messages;
using Hardcodet.Wpf.TaskbarNotification;
using System.IO;


namespace MusicPlayer.Services
{
    /// <summary>
    /// 窗口管理服务，负责处理窗口相关操作
    /// </summary>
    public class WindowManagerService
    {
        private readonly IMessagingService _messagingService;
        private readonly IConfigurationService _configurationService;
        private readonly INotificationService _notificationService;
        private Window? _window;
        private TaskbarIcon? _taskbarIcon;
        
        // 添加静态引用，确保所有实例都使用同一个窗口对象
        private static Window? _staticWindow;

        public WindowManagerService(
            IMessagingService messagingService,
            IConfigurationService configurationService,
            INotificationService notificationService)
        {
            _messagingService = messagingService;
            _configurationService = configurationService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// 设置要管理的窗口
        /// </summary>
        /// <param name="window">要管理的窗口</param>
        public void SetWindow(Window window)
        {
            System.Diagnostics.Debug.WriteLine($"WindowManagerService.SetWindow called with window: {window?.Title}");
            _window = window;
            
            // 同时设置静态窗口引用，确保所有实例都能访问同一个窗口
            _staticWindow = window;
            
            if (_window == null)
            {
                System.Diagnostics.Debug.WriteLine("WindowManagerService.SetWindow: window is null!");
                return;
            }
            
            // 获取系统托盘图标
            if (Application.Current != null)
            {
                _taskbarIcon = Application.Current.FindResource("NotifyIcon") as TaskbarIcon;
                if (_taskbarIcon != null)
                {
                    System.Diagnostics.Debug.WriteLine("WindowManagerService: 系统托盘图标获取成功");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("WindowManagerService: 无法获取系统托盘图标资源");
                }
            }
        }



        /// <summary>
        /// 处理窗口状态变化
        /// </summary>
        public void OnWindowStateChanged()
        {
            try
            {
                if (_window != null)
                {
                    // 确保窗口状态在UI线程上访问
                    var windowState = _window.Dispatcher.Invoke(() => _window.WindowState);
                    
                    // 当窗口状态发生变化时，发送消息通知TitleBarViewModel
                    if (_messagingService != null)
                    {
                        _messagingService.Send(new WindowStateChangedMessage(windowState));
                    }
                    
                    // 窗口配置保存功能已移除
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"窗口状态变化处理失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常详情: {ex.StackTrace}");
                // 不重新抛出异常，防止崩溃
            }
        }

       

       

        /// <summary>
        /// 最小化到系统托盘
        /// </summary>
        public void MinimizeToTray()
        {
            if (_window == null) return;
            
            try
            {
                // 获取关闭行为设置
                bool closeBehavior = _configurationService.CurrentConfiguration.CloseBehavior;

                // 检查是否是通过系统托盘菜单退出或按住Shift键
                var shouldExit = false;

                // 如果用户按住Shift键点击关闭按钮，则直接退出程序
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    shouldExit = true;
                }

                // 如果配置设置为完全退出或者用户强制退出
                // closeBehavior=false 程序托盘未启用，直接退出
                if (!closeBehavior || shouldExit)
                {
                    // 关闭歌词窗口
                    CloseLyricsWindow();
                    
                    // 保存配置并允许退出
                    _configurationService.SaveCurrentConfiguration();

                    // 正常关闭程序
                    _window.Close();
                }
                else
                {
                    // 隐藏窗口
                    _window.Hide();

                    // 显示系统托盘通知 - 添加异常处理
                    try
                    {
                        _notificationService.ShowInfo("音乐播放器已最小化到系统托盘");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"显示托盘通知失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"最小化到托盘操作失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 关闭歌词窗口
        /// </summary>
        private void CloseLyricsWindow()
        {
            try
            {
                // 查找并关闭歌词窗口
                var lyricsWindow = System.Windows.Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.GetType().Name == "LyricsWindow");
                
                if (lyricsWindow != null)
                {
                    System.Diagnostics.Debug.WriteLine("WindowManagerService: 关闭歌词窗口");
                    lyricsWindow.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowManagerService: 关闭歌词窗口失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 从系统托盘恢复窗口
        /// </summary>
        public void RestoreFromTray()
        {
            if (_window == null) return;
            
            try
            {
                // 检查窗口是否已被关闭
                if (!_window.IsLoaded)
                {
                    System.Diagnostics.Debug.WriteLine("WindowManagerService.RestoreFromTray: 窗口已被关闭，无法恢复");
                    return;
                }
                
                // 显示窗口
                _window.Show();
                
                // 将窗口状态设置为正常
                if (_window.WindowState == WindowState.Minimized)
                {
                    _window.WindowState = WindowState.Normal;
                }
                
                // 激活窗口
                _window.Activate();
                _window.Focus();
                
                // 显示系统托盘通知
                try
                {
                    if (_taskbarIcon != null)
                    {
                        _taskbarIcon.ShowBalloonTip("音乐播放器", "主窗口已恢复", BalloonIcon.Info);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"显示恢复窗口通知失败: {ex.Message}");
                }
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowManagerService.RestoreFromTray: 窗口恢复失败 - {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowManagerService.RestoreFromTray: 恢复窗口时发生未知错误 - {ex.Message}");
            }
        }

        /// <summary>
        /// 设置窗口状态
        /// </summary>
        public void SetWindowState(WindowState state)
        {
            if (_window != null)
            {
                // 确保在UI线程上执行
                if (_window.Dispatcher.CheckAccess())
                {
                    _window.WindowState = state;
                }
                else
                {
                    _window.Dispatcher.Invoke(() => _window.WindowState = state);
                }
            }
        }

        /// <summary>
        /// 获取当前窗口状态
        /// </summary>
        public WindowState GetWindowState()
        {
            if (_window != null)
            {
                // 确保在UI线程上执行
                if (_window.Dispatcher.CheckAccess())
                {
                    return _window.WindowState;
                }
                else
                {
                    return _window.Dispatcher.Invoke(() => _window.WindowState);
                }
            }
            return WindowState.Normal;
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void CloseWindow()
        {
            System.Diagnostics.Debug.WriteLine($"WindowManagerService.CloseWindow called, _window is null: {_window == null}, _staticWindow is null: {_staticWindow == null}");
            
            // 优先使用实例窗口引用，如果为空则使用静态窗口引用
            var windowToClose = _window ?? _staticWindow;
            
            if (windowToClose != null)
            {
                System.Diagnostics.Debug.WriteLine($"WindowManagerService.CloseWindow: Closing window {windowToClose.Title}");
                
                // 确保在UI线程上执行
                if (windowToClose.Dispatcher.CheckAccess())
                {
                    windowToClose.Close();
                }
                else
                {
                    windowToClose.Dispatcher.Invoke(() => windowToClose.Close());
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WindowManagerService.CloseWindow: Both _window and _staticWindow are null, cannot close window!");
            }
        }

        /// <summary>
        /// 最小化窗口
        /// </summary>
        public void MinimizeWindow()
        {
            System.Diagnostics.Debug.WriteLine($"WindowManagerService.MinimizeWindow called, _window is null: {_window == null}, _staticWindow is null: {_staticWindow == null}");
            
            // 优先使用实例窗口引用，如果为空则使用静态窗口引用
            var windowToMinimize = _window ?? _staticWindow;
            
            if (windowToMinimize != null)
            {
                // 确保在UI线程上执行
                if (windowToMinimize.Dispatcher.CheckAccess())
                {
                    windowToMinimize.WindowState = WindowState.Minimized;
                }
                else
                {
                    windowToMinimize.Dispatcher.Invoke(() => windowToMinimize.WindowState = WindowState.Minimized);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WindowManagerService.MinimizeWindow: Both _window and _staticWindow are null, cannot minimize window!");
            }
        }

        /// <summary>
        /// 切换最大化状态
        /// </summary>
        public void ToggleMaximize()
        {
            System.Diagnostics.Debug.WriteLine($"WindowManagerService.ToggleMaximize called, _window is null: {_window == null}, _staticWindow is null: {_staticWindow == null}");
            
            // 优先使用实例窗口引用，如果为空则使用静态窗口引用
            var windowToToggle = _window ?? _staticWindow;
            
            if (windowToToggle != null)
            {
                // 确保在UI线程上执行
                if (windowToToggle.Dispatcher.CheckAccess())
                {
                    windowToToggle.WindowState = windowToToggle.WindowState == WindowState.Maximized ? 
                        WindowState.Normal : WindowState.Maximized;
                }
                else
                {
                    windowToToggle.Dispatcher.Invoke(() => 
                    {
                        windowToToggle.WindowState = windowToToggle.WindowState == WindowState.Maximized ? 
                            WindowState.Normal : WindowState.Maximized;
                    });
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WindowManagerService.ToggleMaximize: Both _window and _staticWindow are null, cannot toggle maximize window!");
            }
        }

        /// <summary>
        /// 关闭应用程序
        /// </summary>
        public void CloseApplication()
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 保存窗口位置（已弃用）
        /// </summary>
        public void SaveWindowPosition(double x, double y)
        {
            // 窗口位置配置功能已移除
        }

        /// <summary>
        /// 保存窗口大小（已弃用）
        /// </summary>
        public void SaveWindowSize(double width, double height)
        {
            // 窗口大小配置功能已移除
        }

        /// <summary>
        /// 保存窗口配置（已弃用）
        /// </summary>
        public void SaveWindowConfiguration()
        {
            // 窗口配置功能已移除
        }

        /// <summary>
        /// 注销消息处理器
        /// </summary>
        public void Unregister()
        {
            _messagingService.Unregister(this);
        }
    }
}