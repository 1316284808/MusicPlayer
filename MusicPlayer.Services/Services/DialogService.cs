using System;
using System.Threading.Tasks; 
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 对话框服务实现 - 封装WPF的MessageBox操作
    /// 通过IDispatcherService确保在UI线程上执行
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly IDispatcherService _dispatcherService;

        public DialogService(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));
        }

        public async Task<bool> ShowInformationAsync(string message, string title = "信息")
        {
            return await _dispatcherService.InvokeAsync(() =>
            {
                var result = MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                return result == MessageBoxResult.OK;
            });
        }

        public async Task<bool> ShowWarningAsync(string message, string title = "警告")
        {
            return await _dispatcherService.InvokeAsync(() =>
            {
                var result = MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return result == MessageBoxResult.OK;
            });
        }

        public async Task<bool> ShowErrorAsync(string message, string title = "错误")
        {
            return await _dispatcherService.InvokeAsync(() =>
            {
                var result = MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                return result == MessageBoxResult.OK;
            });
        }

        public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            return await _dispatcherService.InvokeAsync(() =>
            {
                var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            });
        }

        public void ShowInformation(string message, string title = "信息")
        {
            _dispatcherService.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}