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

   

    
}