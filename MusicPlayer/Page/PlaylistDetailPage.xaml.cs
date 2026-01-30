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
        private readonly IPlaylistDetailViewModel _playlistDetailViewModel;
        private bool _disposed;

        public PlaylistDetailPage()
        {
            InitializeComponent();
        }
        
        public PlaylistDetailPage(IPlaylistDetailViewModel playlistDetailViewModel)
        { 
            InitializeComponent();
            _playlistDetailViewModel = playlistDetailViewModel;
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

            // 释放ViewModel
            if (_playlistDetailViewModel is IDisposable disposableVm)
            {
                disposableVm.Dispose();
            }

            this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
            PlaylistDetailControl.Dispose();
            _disposed = true;
        }
    }
}