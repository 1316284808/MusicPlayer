using System.Windows.Controls;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Page
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : System.Windows.Controls.Page
    {

        public HomePage() { InitializeComponent(); }
        public HomePage(IMainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel;
            this.PlaylistControl.DataContext = mainViewModel.PlaylistViewModel;
        }


    }
}
