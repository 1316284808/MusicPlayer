using CommunityToolkit.Mvvm.Messaging;
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

namespace MusicPlayer.Controls
{
    /// <summary>
    /// PlaylistSettingControl.xaml 的交互逻辑
    /// </summary>
    public partial class PlaylistSettingControl : UserControl, IDisposable
    {
        private bool _disposed = false;

        public PlaylistSettingControl()
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
