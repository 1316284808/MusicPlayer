using System.Windows;
using System.Windows.Controls;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Page
{
    /// <summary>
    /// PlaylistPage.xaml 的交互逻辑
    /// </summary>
    public partial class PlaylistPage : System.Windows.Controls.Page, IDisposable
    {
        private bool _disposed;

        public PlaylistPage() { InitializeComponent(); }
        public PlaylistPage(IMainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel;
            var playlistViewModel = mainViewModel.PlaylistViewModel;
            this.PlaylistControl.DataContext = playlistViewModel;
            playlistViewModel.Initialize();
            // 使用命名方法代替匿名方法，以便取消订阅
            Unloaded += PlaylistPage_Unloaded;
        }

        // 命名的Unloaded事件处理方法
        private void PlaylistPage_Unloaded(object sender, RoutedEventArgs e)
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
            Unloaded -= PlaylistPage_Unloaded;
            
            // 调用PlaylistControl的Dispose方法
            PlaylistControl.Dispose();
            
            // 清空DataContext，解除Page对ViewModel的强引用
            this.DataContext = null;
            
            // 清空页面内容，释放UI资源
            this.Content = null;
            
            _disposed = true;
        }
    }
}
