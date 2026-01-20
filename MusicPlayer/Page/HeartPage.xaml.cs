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

namespace MusicPlayer.Page
{
    /// <summary>
    /// HeartPage.xaml 的交互逻辑
    /// </summary>
    public partial class HeartPage : System.Windows.Controls.Page, IDisposable
    {
        private bool _disposed = false;
        public HeartPage(IHeartViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
        public HeartPage()
        {
            InitializeComponent();
        }
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
            this.Content = null;     // 清空页面内容，释放UI资源
            _disposed = true;
        }
    }
}
