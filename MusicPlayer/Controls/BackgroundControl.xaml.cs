using System.Windows.Controls;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Controls
{
    public partial class BackgroundControl : UserControl
    {
        public BackgroundControl()
        {
            InitializeComponent();
        }

        public BackgroundControl(IBackgroundViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}