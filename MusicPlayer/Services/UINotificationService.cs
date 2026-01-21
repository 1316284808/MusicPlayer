using MusicPlayer.Core.Interface;
using System;
using Wpf.Ui;
using Wpf.Ui.Extensions;

namespace MusicPlayer.Services
{
    /// <summary>
    /// UI通知服务实现 - 使用WPF-UI的SnackbarService
    /// </summary>
    public class UINotificationService : IUINotificationService
    {
        private readonly ISnackbarService _snackbarService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="snackbarService">Snackbar服务</param>
        public UINotificationService(ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
        }

        /// <summary>
        /// 显示成功通知
        /// </summary>
        public void ShowSuccess(string title, string message, TimeSpan? duration = null)
        {
            _snackbarService.Show(title, message, Wpf.Ui.Controls.ControlAppearance.Success, duration ?? TimeSpan.FromSeconds(3));
        }

        /// <summary>
        /// 显示错误通知
        /// </summary>
        public void ShowError(string title, string message, TimeSpan? duration = null)
        {
            _snackbarService.Show(title, message, Wpf.Ui.Controls.ControlAppearance.Danger, duration ?? TimeSpan.FromSeconds(3));
        }

        /// <summary>
        /// 显示警告通知
        /// </summary>
        public void ShowWarning(string title, string message, TimeSpan? duration = null)
        {
            _snackbarService.Show(title, message, Wpf.Ui.Controls.ControlAppearance.Caution, duration ?? TimeSpan.FromSeconds(3));
        }

        /// <summary>
        /// 显示信息通知
        /// </summary>
        public void ShowInfo(string title, string message, TimeSpan? duration = null)
        {
            _snackbarService.Show(title, message, Wpf.Ui.Controls.ControlAppearance.Success, duration ?? TimeSpan.FromSeconds(3));
        }
    }
}