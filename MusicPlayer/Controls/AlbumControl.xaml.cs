using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Helper;
using MusicPlayer.Services.Messages;
using MusicPlayer.ViewModels;
using System.Windows.Controls;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// AlbumControl.xaml 的交互逻辑
    /// </summary>
    public partial class AlbumControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public AlbumControl()
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
                // 获取专辑信息
                var albumInfo = border.DataContext as MusicPlayer.Core.Models.AlbumInfo;
                if (albumInfo != null)
                {
                    // 获取父级ViewModel
                    var viewModel = this.DataContext as MusicPlayer.ViewModels.AlbumViewModel;
                    if (viewModel != null)
                    {
                        // 调用导航命令
                        var navigateCommand = viewModel.GetType().GetProperty("NavigateToAlbumDetailCommand")?.GetValue(viewModel) as System.Windows.Input.ICommand;
                        if (navigateCommand != null && navigateCommand.CanExecute(albumInfo.Name))
                        {
                            navigateCommand.Execute(albumInfo.Name);
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
                    
                    // 清理AlbumAlbumArtBehavior资源
                    if (AlbumListBox != null)
                    {
                        // 禁用AlbumAlbumArtBehavior
                        Helper.AlbumAlbumArtBehavior.SetIsEnabled(AlbumListBox, false);
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