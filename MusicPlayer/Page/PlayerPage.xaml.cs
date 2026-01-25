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
    public partial class PlayerPage : System.Windows.Controls.Page, IDisposable
    {
        private bool _disposed;

        public PlayerPage()
        {
            InitializeComponent();
        }
        public PlayerPage(IMainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel;
            this.CenterContentControl.DataContext = mainViewModel.CenterContentViewModel;
            // 使用命名方法代替匿名方法，以便能够取消订阅
            Unloaded += PlayerPage_Unloaded;
        }
        
        // 命名的Unloaded事件处理方法
        private void PlayerPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }
        
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            // 取消Unloaded事件订阅
            Unloaded -= PlayerPage_Unloaded;
            
            // 释放CenterContentControl资源
            if (CenterContentControl != null)
            {
                CenterContentControl.Dispose();
            }
            
            // 清空DataContext，解除Page对ViewModel的强引用
            this.DataContext = null;
            
            // 清空页面内容，释放UI资源
            this.Content = null;
            
            _disposed = true;
        }


    }
}
