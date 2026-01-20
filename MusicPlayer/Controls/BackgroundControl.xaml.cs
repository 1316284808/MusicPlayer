using System.Windows.Controls;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Controls
{
    public partial class BackgroundControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public BackgroundControl()
        {
            InitializeComponent();
        }

        public BackgroundControl(IBackgroundViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                    DataContext = null;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}