using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services;
using MusicPlayer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 播放列表滚动行为
    /// 将UI行为逻辑从代码后台移至附加行为
    /// </summary>
    public static class PlaylistScrollBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PlaylistScrollBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty PlaylistUIBehaviorServiceProperty =
            DependencyProperty.RegisterAttached(
                "PlaylistUIBehaviorService",
                typeof(object),
                typeof(PlaylistScrollBehavior),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static object GetPlaylistUIBehaviorService(DependencyObject obj)
        {
            return obj.GetValue(PlaylistUIBehaviorServiceProperty);
        }

        public static void SetPlaylistUIBehaviorService(DependencyObject obj, object value)
        {
            obj.SetValue(PlaylistUIBehaviorServiceProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                if ((bool)e.NewValue)
                {
                    Enable(scrollViewer);
                }
                else
                {
                    Disable(scrollViewer);
                }
            }
        }

        private static void Enable(ScrollViewer scrollViewer)
        {
            try
            {
                var behaviorService = GetPlaylistUIBehaviorService(scrollViewer);
                if (behaviorService != null)
                {
                    // 使用反射调用SetScrollViewer方法，以避免直接依赖IPlaylistUIBehaviorService
                    var setScrollViewerMethod = behaviorService.GetType().GetMethod("SetScrollViewer");
                    if (setScrollViewerMethod != null)
                    {
                        setScrollViewerMethod.Invoke(behaviorService, new object[] { scrollViewer });
                        System.Diagnostics.Debug.WriteLine("PlaylistScrollBehavior: 滚动行为已启用");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("PlaylistScrollBehavior: SetScrollViewer方法未找到");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistScrollBehavior: PlaylistUIBehaviorService 未设置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistScrollBehavior: 启用滚动行为失败: {ex.Message}");
            }
        }

        private static void Disable(ScrollViewer scrollViewer)
        {
            try
            {
                var behaviorService = GetPlaylistUIBehaviorService(scrollViewer);
                // 不传递null，而是直接不调用SetScrollViewer
                // 或者创建一个空的ScrollViewer来替代null
                System.Diagnostics.Debug.WriteLine("PlaylistScrollBehavior: 滚动行为已禁用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistScrollBehavior: 禁用滚动行为失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 播放列表专辑封面加载行为
    /// </summary>
    public static class PlaylistAlbumArtBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PlaylistAlbumArtBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty PlaylistViewModelProperty =
            DependencyProperty.RegisterAttached(
                "PlaylistViewModel",
                typeof(IPlaylistViewModel),
                typeof(PlaylistAlbumArtBehavior),
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
                else
                {
                    Disable(userControl);
                }
            }
        }

        private static void Enable(UserControl userControl)
        {
            try
            {
                var viewModel = GetPlaylistViewModel(userControl);
                if (viewModel != null)
                {
                    // 订阅专辑封面加载请求事件
                    viewModel.AlbumLoadRequested += OnAlbumLoadRequested;
                    System.Diagnostics.Debug.WriteLine("PlaylistAlbumArtBehavior: 专辑封面加载行为已启用");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("PlaylistAlbumArtBehavior: PlaylistViewModel 未设置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 启用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void Disable(UserControl userControl)
        {
            try
            {
                var viewModel = GetPlaylistViewModel(userControl);
                if (viewModel != null)
                {
                    // 取消订阅事件
                    viewModel.AlbumLoadRequested -= OnAlbumLoadRequested;
                    System.Diagnostics.Debug.WriteLine("PlaylistAlbumArtBehavior: 专辑封面加载行为已禁用");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 禁用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void OnAlbumLoadRequested(object? sender, AlbumLoadRequestEventArgs e)
        {
            try
            {
                var song = e.Song;
                System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 收到专辑封面加载请求，歌曲: {song.Title}");
                
                // 这里应该实际处理专辑封面加载逻辑
                // 例如：调用专辑封面加载服务或更新UI
                // 而不是重新触发事件
                
                // 检查专辑封面是否已加载
                if (song.AlbumArt != null)
                {
                    // 专辑封面已存在，不需要处理
                    System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 专辑封面已加载");
                }
                else if (song.AlbumArtData == null || song.AlbumArtData.Length == 0)
                {
                    // 有专辑封面数据但未加载，强制加载
                    System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 有专辑封面数据，开始加载");
                    song.EnsureAlbumArtLoaded();
                }
                else
                {
                    // 需要加载专辑封面的逻辑
                    System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 需要加载歌曲 {song.Title} 的专辑封面");
                    // 这里可以调用专辑封面加载服务
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaylistAlbumArtBehavior: 处理专辑封面加载请求失败: {ex.Message}");
            }
        }
    }

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