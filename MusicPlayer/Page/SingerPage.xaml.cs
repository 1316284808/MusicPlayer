using System.Windows.Controls;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Page
{
    /// <summary>
    /// SingerPage.xaml 的交互逻辑
    /// </summary>
    public partial class SingerPage : System.Windows.Controls.Page
    {
        public SingerPage()
        {
            InitializeComponent();
        }
        
        public SingerPage(ISingerViewModel singerViewModel)
        {
            InitializeComponent();
            DataContext = singerViewModel;
            this.SingerControl.DataContext = singerViewModel;
            //singerViewModel.Initialize();
        }
    }
}
