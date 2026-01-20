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
            Unloaded += (s, e) =>
            {
                Dispose();
            };
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            PlaylistControl.Dispose();
            this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
            this.Content = null;     // 清空页面内容，释放UI资源
            _disposed = true;
        }
    }
}
