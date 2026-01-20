using System;
using System.Windows;
using System.Windows.Controls;
using MusicPlayer.ViewModels;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Page
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : System.Windows.Controls.Page, IDisposable
    {
        private bool _disposed;
        
        public SettingsPage() { }
        public SettingsPage(ISettingsPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.WindowSettingsControl.DataContext = viewModel.WindowSettingsViewModel;
            this.PlaylistSettingControl.DataContext = viewModel.PlaylistSettingViewModel;

            // 订阅导航消息
            Loaded += SettingsPage_Loaded;
            Unloaded += SettingsPage_Unloaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 直接在View中处理消息注册，符合MVVM架构
                if (Application.Current is App app && app.ServiceProvider != null)
                {
                    var messagingService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<IMessagingService>(app.ServiceProvider);
                    if (messagingService != null)
                    {
                        messagingService.Register<NavigateToPageMessage>(this, (r, message) => OnNavigateToPageMessage(message));
                    }
                }

                Loaded -= SettingsPage_Loaded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"注册导航消息失败: {ex.Message}");
            }
        }

        private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Dispose();
                Unloaded -= SettingsPage_Unloaded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"取消注册导航消息失败: {ex.Message}");
            }
        }

        private void OnNavigateToPageMessage(NavigateToPageMessage message)
        {
            try
            {
                // 这里不需要处理导航，因为导航应该由MainWindow中的导航服务统一处理
                // 这样可以确保状态一致性，避免创建新的实例
                System.Diagnostics.Debug.WriteLine($"收到导航消息: {message.PageUri}，导航将由导航服务统一处理");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理导航消息失败: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            try
            {
                // 直接在View中处理消息注销，符合MVVM架构
                if (Application.Current is App app && app.ServiceProvider != null)
                {
                    var messagingService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<IMessagingService>(app.ServiceProvider);
                    if (messagingService != null)
                    {
                        messagingService.Unregister(this);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"取消注册导航消息失败: {ex.Message}");
            }
            WindowSettingsControl.Dispose();
            SoundSettingsControl.Dispose();
            PlaylistSettingControl.Dispose();
            this.DataContext = null; // 核心：清空DataContext，解除Page对ViewModel的强引用
            this.Content = null;     // 清空页面内容，释放UI资源
            _disposed = true;
        }
    }
}