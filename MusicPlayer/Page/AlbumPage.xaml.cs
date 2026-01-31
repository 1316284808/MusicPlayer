using System.Windows;
using System.Windows.Controls;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Page
{
    /// <summary>
    /// AlbumPage.xaml 的交互逻辑
    /// </summary>
    public partial class AlbumPage : System.Windows.Controls.Page, IDisposable
    {
        private readonly IAlbumViewModel _albumViewModel;
        private bool _disposed = false;
        private bool _isUnloadedRegistered = false;
        
        public AlbumPage()
        {
            InitializeComponent();
        }
        
        public AlbumPage(IAlbumViewModel albumViewModel)
        {
            InitializeComponent();
            _albumViewModel = albumViewModel;
            DataContext = albumViewModel;
            this.AlbumControl.DataContext = albumViewModel;
            Unloaded += AlbumPage_Unloaded;
            _isUnloadedRegistered = true;
        }
        
        // 命名的Unloaded事件处理方法
        private void AlbumPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }
        
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            // 取消Unloaded事件订阅
            if (_isUnloadedRegistered)
            {
                Unloaded -= AlbumPage_Unloaded;
                _isUnloadedRegistered = false;
            }
            
            // 释放ViewModel
            if (_albumViewModel is IDisposable disposableVm)
            {
                disposableVm.Dispose();
            }
          
            // 先调用控件的Dispose方法，再清理数据上下文
            AlbumControl.Dispose();
            this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
            this.Content = null;     // 清空页面内容，释放UI资源
            _disposed = true;
        }
    }
}
