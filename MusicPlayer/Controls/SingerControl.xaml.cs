using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Helper;
using MusicPlayer.Services.Messages;
using MusicPlayer.ViewModels;
using System.Windows.Controls;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// SingerControl.xaml 的交互逻辑
    /// </summary>
    public partial class SingerControl : UserControl, IDisposable
    { 
        private bool _disposed = false;

        public SingerControl()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.Register<SearchBoxFocusRequestMessage>(this, (recipient, message) =>
            {
                // 处理搜索框焦点请求
                SearchTextBox.Focus();
            });
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border)
            {
                // 获取歌手信息
                var singerInfo = border.DataContext as MusicPlayer.Core.Models.SingerInfo;
                if (singerInfo != null)
                {
                    // 获取父级ViewModel
                    var viewModel = this.DataContext as MusicPlayer.ViewModels.SingerViewModel;
                    if (viewModel != null)
                    {
                        // 调用导航命令
                        var navigateCommand = viewModel.GetType().GetProperty("NavigateToSingerDetailCommand")?.GetValue(viewModel) as System.Windows.Input.ICommand;
                        if (navigateCommand != null && navigateCommand.CanExecute(singerInfo.Name))
                        {
                            navigateCommand.Execute(singerInfo.Name);
                        }
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 取消消息订阅
                    WeakReferenceMessenger.Default.UnregisterAll(this); 
                    
                    // 清理SingerAlbumArtBehavior资源
                    if (SingerListBox != null)
                    {
                        // 禁用SingerAlbumArtBehavior
                        Helper.SingerAlbumArtBehavior.SetIsEnabled(SingerListBox, false);
                    }
                    
                    this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
                    this.Content = null;     // 清空页面内容，释放UI资源
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
