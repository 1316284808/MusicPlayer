using MusicPlayer.Controls;
using MusicPlayer.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.Page
{
    /// <summary>
    /// PlaylistDetailPage.xaml 的交互逻辑
    /// </summary>
    public partial class PlaylistDetailPage : System.Windows.Controls.Page, IDisposable
    {
        private bool _disposed;

        public PlaylistDetailPage()
        {
            InitializeComponent();
        }
        public PlaylistDetailPage(IPlaylistDetailViewModel playlistDetailViewModel)
        { 
            InitializeComponent();
            DataContext = playlistDetailViewModel;
            this.PlaylistDetailControl.DataContext = playlistDetailViewModel; 
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

            this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
            PlaylistDetailControl.Dispose();
            _disposed = true;
        }
    }
}