using CommunityToolkit.Mvvm.Messaging;
using MusicPlayer.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MusicPlayer.Controls
{
    /// <summary>
    /// WindowSettingsControl.xaml 的交互逻辑
    /// </summary>
    public partial class WindowSettingsControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public WindowSettingsControl()
        {
            InitializeComponent();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
                    this.Content = null;     // 清空页面内容，释放UI资源
                    _disposed = true;
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