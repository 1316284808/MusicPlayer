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
        private readonly ICenterContentViewModel _centerContentViewModel;
        private bool _disposed;

        public PlayerPage()
        {
            InitializeComponent();
        }
        
        public PlayerPage(ICenterContentViewModel centerContentViewModel)
        {
            InitializeComponent();
            _centerContentViewModel = centerContentViewModel;
            this.CenterContentControl.DataContext = centerContentViewModel;
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
            
            System.Diagnostics.Debug.WriteLine("PlayerPage: 开始执行Dispose方法");
            
            // 取消Unloaded事件订阅
            Unloaded -= PlayerPage_Unloaded;
            
            // 清空CenterContentControl的DataContext
            if (CenterContentControl != null)
            {
                System.Diagnostics.Debug.WriteLine("PlayerPage: 清空CenterContentControl的DataContext");
                CenterContentControl.DataContext = null;
            }
            
            // 释放ViewModel
            if (_centerContentViewModel is IDisposable disposableVm)
            {
                System.Diagnostics.Debug.WriteLine("PlayerPage: 释放ViewModel资源");
                disposableVm.Dispose();
                // 注意：_centerContentViewModel是只读字段，不能设置为null
                // 但Dispose方法会释放其内部资源
            }
            
            // 释放CenterContentControl资源
            if (CenterContentControl != null)
            {
                System.Diagnostics.Debug.WriteLine("PlayerPage: 释放CenterContentControl资源");
                CenterContentControl.Dispose();
            }
            
            // 清空DataContext，解除Page对ViewModel的强引用
            System.Diagnostics.Debug.WriteLine("PlayerPage: 清空Page的DataContext");
            this.DataContext = null;
            
            // 清空页面内容，释放UI资源
            System.Diagnostics.Debug.WriteLine("PlayerPage: 清空页面内容");
            this.Content = null;
            
            // 强制垃圾回收，尝试回收释放的资源
            System.Diagnostics.Debug.WriteLine("PlayerPage: 执行垃圾回收");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            System.Diagnostics.Debug.WriteLine("PlayerPage: 垃圾回收完成");
            
            _disposed = true;
            System.Diagnostics.Debug.WriteLine("PlayerPage: Dispose方法执行完成");
        }
    }
}
