using MusicPlayer.Controls;
using MusicPlayer.Core.Interface;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace MusicPlayer.Services
{
    /// <summary>
    /// WPF对话框服务实现 - 使用WPF-UI的ContentDialogService
    /// </summary>
    public class WpfDialogService : IDialogService
    {
        private readonly IContentDialogService _contentDialogService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="contentDialogService">ContentDialog服务</param>
        public WpfDialogService(IContentDialogService contentDialogService)
        {
            _contentDialogService = contentDialogService;
        }

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        public async Task<bool> ShowInformationAsync(string message, string title = "信息")
        {
            var dialog = CreateMessageBox(title, message, "确认", "取消");
            var dialogResult = await dialog.ShowDialogAsync();
            return dialogResult == Wpf.Ui.Controls.MessageBoxResult.Primary;

            //var dialog = CreateContentDialog(title, message, "确认", null);
            //var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            //return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        public async Task<bool> ShowWarningAsync(string message, string title = "警告")
        {
            var dialog = CreateMessageBox(title, message, "确认", "取消");
            var dialogResult = await dialog.ShowDialogAsync();
            return dialogResult == Wpf.Ui.Controls.MessageBoxResult.Primary;

            //var dialog = CreateContentDialog(title, message, "确认", null);
            //var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            //return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        public async Task<bool> ShowErrorAsync(string message, string title = "错误")
        {
            var dialog = CreateMessageBox(title, message, "确认", "取消");
            var dialogResult = await dialog.ShowDialogAsync();
            return dialogResult == Wpf.Ui.Controls.MessageBoxResult.Primary;

            //var dialog = CreateContentDialog(title, message, "确认", null);
            //var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            //return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            var dialog = CreateMessageBox(title, message, "确认", "取消");
            var dialogResult = await dialog.ShowDialogAsync();
            return dialogResult == Wpf.Ui.Controls.MessageBoxResult.Primary;

            //var dialog = CreateContentDialog(title, message, "是", "否");
            //var result = await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            //return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// 同步显示信息对话框
        /// </summary>
        public void ShowInformation(string message, string title = "信息")
        {
            var dialog = CreateContentDialog(title, message, "确认", "取消");
            _contentDialogService.ShowAsync(dialog, CancellationToken.None).Wait();
        }




        private Wpf.Ui.Controls.MessageBox   CreateMessageBox(string title, string content, string primaryButtonText, string secondaryButtonText)
        {
            var text = new Wpf.Ui.Controls.TextBlock()
            {
                Text = content,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };
            var result = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = text,
                PrimaryButtonText = primaryButtonText,
                IsSecondaryButtonEnabled = false,
                CloseButtonText = secondaryButtonText
            };

             return result;
        }
        /// <summary>
        /// 创建ContentDialog实例
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="content">内容</param>
        /// <param name="primaryButtonText">主按钮文本</param>
        /// <param name="secondaryButtonText">次要按钮文本</param>
        /// <returns>ContentDialog实例</returns>
        private ContentDialog CreateContentDialog(string title, string content, string primaryButtonText, string secondaryButtonText)
        {
            //// 1. 创建内容区域（保持原有逻辑）
            //var grid = new Grid() { Width = 300, Height = 50  };
            //var text = new Wpf.Ui.Controls.TextBlock()
            //{
            //    Text = content,
            //    TextAlignment = System.Windows.TextAlignment.Left,
            //    TextWrapping = System.Windows.TextWrapping.Wrap,
            //    FontSize = 16,
            //    FontWeight = FontWeights.SemiBold
            //};
            //grid.Children.Add(text);
            // 3. 创建ContentDialog并应用全局样式
            var contentDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText, 
                CloseButtonText = string.Empty
            };

            return contentDialog;

        }

 
    }
}