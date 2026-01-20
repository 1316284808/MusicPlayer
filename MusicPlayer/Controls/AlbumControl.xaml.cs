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
                    _disposed = true;
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