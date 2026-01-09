using MusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 播放列表初始化行为
    /// </summary>
    public static class PlaylistInitializationBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PlaylistInitializationBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty PlaylistViewModelProperty =
            DependencyProperty.RegisterAttached(
                "PlaylistViewModel",
                typeof(IPlaylistViewModel),
                typeof(PlaylistInitializationBehavior),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static IPlaylistViewModel GetPlaylistViewModel(DependencyObject obj)
        {
            return (IPlaylistViewModel)obj.GetValue(PlaylistViewModelProperty);
        }

        public static void SetPlaylistViewModel(DependencyObject obj, IPlaylistViewModel value)
        {
            obj.SetValue(PlaylistViewModelProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UserControl userControl)
            {
                if ((bool)e.NewValue)
                {
                    Enable(userControl);
                }
            }
        }

        private static void Enable(UserControl userControl)
        {
            try
            {
                userControl.Loaded += OnUserControlLoaded;
                userControl.Unloaded += OnUserControlUnloaded;
                System.Diagnostics.Debug.WriteLine("PlaylistInitializationBehavior: 初始化行为已启用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistInitializationBehavior: 启用初始化行为失败: {ex.Message}");
            }
        }

        private static void Disable(UserControl userControl)
        {
            try
            {
                userControl.Loaded -= OnUserControlLoaded;
                userControl.Unloaded -= OnUserControlUnloaded;
                System.Diagnostics.Debug.WriteLine("PlaylistInitializationBehavior: 初始化行为已禁用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistInitializationBehavior: 禁用初始化行为失败: {ex.Message}");
            }
        }

        private static void OnUserControlLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is UserControl userControl)
                {
                    var viewModel = GetPlaylistViewModel(userControl);
                    if (viewModel != null)
                    {
                        // 调用ViewModel的Initialize方法
                        viewModel.Initialize();

                        // 延迟配置滚动行为
                        userControl.Dispatcher.BeginInvoke(() =>
                        {
                            ConfigureScrollBehavior(userControl);
                        }, System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistInitializationBehavior: 控件加载处理失败: {ex.Message}");
            }
        }

        private static void OnUserControlUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is UserControl userControl)
                {
                    var viewModel = GetPlaylistViewModel(userControl);
                    if (viewModel != null)
                    {
                        // 清理ViewModel资源
                        viewModel.Cleanup();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistInitializationBehavior: 控件卸载处理失败: {ex.Message}");
            }
        }

        private static void ConfigureScrollBehavior(UserControl userControl)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PlaylistInitializationBehavior: 开始配置滚动行为");

                var viewModel = GetPlaylistViewModel(userControl);
                if (viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistInitializationBehavior: ViewModel为null");
                    return;
                }

                // 查找ListBox
                var listBox = FindVisualChild<ListBox>(userControl);
                if (listBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistInitializationBehavior: 未找到ListBox");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("PlaylistInitializationBehavior: 找到ListBox");

                // 查找ListBox内部的ScrollViewer
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer == null)
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistInitializationBehavior: 未找到ScrollViewer");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("PlaylistInitializationBehavior: 找到ScrollViewer，通过Behavior设置到UI行为服务");

                // 获取PlaylistUIBehaviorService并设置到Behavior中
                var serviceProvider = ((App)Application.Current).ServiceProvider;
                if (serviceProvider != null)
                {
                    var uiBehaviorService = serviceProvider.GetService(typeof(object));
                    if (uiBehaviorService != null)
                    {
                        // 使用Behavior替代直接调用SetScrollViewer，符合MVVM架构
                        PlaylistScrollBehavior.SetPlaylistUIBehaviorService(scrollViewer, uiBehaviorService);
                        PlaylistScrollBehavior.SetIsEnabled(scrollViewer, true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistInitializationBehavior: 配置滚动行为失败: {ex.Message}");
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                {
                    return found;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
