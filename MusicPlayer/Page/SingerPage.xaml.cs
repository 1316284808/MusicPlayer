using System.Windows;
using System.Windows.Controls;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Page
{
    /// <summary>
    /// SingerPage.xaml 的交互逻辑
    /// </summary>
    public partial class SingerPage : System.Windows.Controls.Page, IDisposable
    {
        private readonly ISingerViewModel _singerViewModel;
        private bool _disposed;
        private bool _isUnloadedRegistered = false;

        public SingerPage()
        {
            InitializeComponent();
        }
        
        public SingerPage(ISingerViewModel singerViewModel)
        {
            InitializeComponent();
            _singerViewModel = singerViewModel;
            DataContext = singerViewModel;
            this.SingerControl.DataContext = singerViewModel;
            Unloaded += SingerPage_Unloaded;
            _isUnloadedRegistered = true;
        }
        
        // 命名的Unloaded事件处理方法
        private void SingerPage_Unloaded(object sender, RoutedEventArgs e)
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
                Unloaded -= SingerPage_Unloaded;
                _isUnloadedRegistered = false;
            }
            
            // 释放ViewModel
            if (_singerViewModel is IDisposable disposableVm)
            {
                disposableVm.Dispose();
            }
            
            SingerControl.Dispose();
            this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
            this.Content = null;     // 清空页面内容，释放UI资源
            _disposed = true;
        }
    }
}
