using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.Controls
{
    public partial class ControlBarControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public ControlBarControl()
        {
            InitializeComponent();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //全局单例，不需要释放
                    //WeakReferenceMessenger.Default.UnregisterAll(this);
                    //this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
                    //this.Content = null;     // 清空页面内容，释放UI资源
                    //_disposed = true;
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