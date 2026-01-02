using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MusicPlayer.ViewModels;
using MusicPlayer.Controls;
using MusicPlayer.Services.Messages;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Helper;
namespace MusicPlayer.Page
{
    /// <summary>
    /// PlayerPage.xaml 的交互逻辑
    /// </summary>
    public partial class PlayerPage : System.Windows.Controls.Page
    {
        public PlayerPage()
        {
            InitializeComponent();
        }
        public PlayerPage(IMainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel;
            this.CenterContentControl.DataContext = mainViewModel.CenterContentViewModel;
            
            
        }

        
    }
}
