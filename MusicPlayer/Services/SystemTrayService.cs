using Hardcodet.Wpf.TaskbarNotification;
using MusicPlayer.Core.Interface;
using System.Windows;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 系统托盘服务实现 - 使用Hardcodet.Wpf.TaskbarNotification
    /// </summary>
    public class SystemTrayService : ISystemTrayService
    {
        private TaskbarIcon? _taskbarIcon;
        private readonly IDispatcherService _dispatcherService;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dispatcherService">调度器服务</param>
        public SystemTrayService(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService;
            Initialize();
        }
        
        /// <summary>
        /// 初始化系统托盘服务
        /// </summary>
        private void Initialize()
        {
            _dispatcherService.Invoke(() =>
            {
                if (Application.Current != null)
                {
                    _taskbarIcon = Application.Current.FindResource("NotifyIcon") as TaskbarIcon;
                }
            });
        }
        
        /// <summary>
        /// 系统托盘图标是否可用
        /// </summary>
        public bool IsTrayIconAvailable => _taskbarIcon != null;
        
        /// <summary>
        /// 显示系统托盘通知
        /// </summary>
        public void ShowBalloonTip(string title, string message, MusicPlayer.Core.Interface.BalloonIcon icon = MusicPlayer.Core.Interface.BalloonIcon.Info)
        {
            _dispatcherService.Invoke(() =>
            {
                if (_taskbarIcon != null)
                {
                    var taskbarIcon = _taskbarIcon;
                    var taskbarIconType = icon switch
                    {
                        MusicPlayer.Core.Interface.BalloonIcon.Info => Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info,
                        MusicPlayer.Core.Interface.BalloonIcon.Warning => Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning,
                        MusicPlayer.Core.Interface.BalloonIcon.Error => Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error,
                        _ => Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info
                    };
                    
                    taskbarIcon.ShowBalloonTip(title, message, taskbarIconType);
                }
            });
        }
        
        /// <summary>
        /// 隐藏系统托盘通知
        /// </summary>
        public void HideBalloonTip()
        {
            _dispatcherService.Invoke(() =>
            {
                _taskbarIcon?.HideBalloonTip();
            });
        }
        
        /// <summary>
        /// 清理系统托盘资源
        /// </summary>
        public void Cleanup()
        {
            _taskbarIcon?.Dispose();
            _taskbarIcon = null;
        }
    }
}