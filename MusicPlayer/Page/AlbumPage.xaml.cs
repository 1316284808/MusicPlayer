using System.Windows.Controls;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Page
{
    /// <summary>
    /// AlbumPage.xaml 的交互逻辑
    /// </summary>
    public partial class AlbumPage : System.Windows.Controls.Page
    {
        public AlbumPage()
        {
            InitializeComponent();
        }
        
        public AlbumPage(IAlbumViewModel albumViewModel)
        {
            InitializeComponent();
            DataContext = albumViewModel;
            this.AlbumControl.DataContext = albumViewModel;
        }
    }
}
