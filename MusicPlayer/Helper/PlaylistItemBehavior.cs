using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using MusicPlayer.Controls;
using MusicPlayer.Core.Models;
using MusicPlayer.ViewModels;
using MusicPlayer.Services;
using MusicPlayer.Core.Interface;
using MusicPlayer;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 播放列表项行为附加属性类，用于处理播放列表项的加载和卸载事件
    /// 集成可视范围检测服务，实现真正的延迟加载
    /// </summary>
    public static class PlaylistItemBehavior
    {
        // 可视范围检测服务
        private static AlbumArtViewportService? _viewportService;
        
        // 跟踪已注册的列表控件
        private static readonly System.Collections.Generic.Dictionary<ItemsControl, bool> _registeredControls = new();
        
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PlaylistItemBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));
                
        public static readonly DependencyProperty ItemsControlProperty =
            DependencyProperty.RegisterAttached(
                "ItemsControl",
                typeof(ItemsControl),
                typeof(PlaylistItemBehavior),
                new PropertyMetadata(null, OnItemsControlChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }
        
        public static ItemsControl GetItemsControl(DependencyObject obj)
        {
            return (ItemsControl)obj.GetValue(ItemsControlProperty);
        }

        public static void SetItemsControl(DependencyObject obj, ItemsControl value)
        {
            obj.SetValue(ItemsControlProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBoxItem listBoxItem)
            {
                if ((bool)e.NewValue)
                {
                    listBoxItem.Loaded += ListBoxItem_Loaded;
                    listBoxItem.Unloaded += ListBoxItem_Unloaded;
                    
                    // 初始化可视范围检测服务
                    InitializeViewportService();
                }
                else
                {
                    listBoxItem.Loaded -= ListBoxItem_Loaded;
                    listBoxItem.Unloaded -= ListBoxItem_Unloaded;
                }
            }
        }
        
        private static void OnItemsControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ItemsControl itemsControl)
            {
                RegisterItemsControl(itemsControl);
            }
            else if (e.OldValue is ItemsControl oldItemsControl)
            {
                UnregisterItemsControl(oldItemsControl);
            }
        }
        
        /// <summary>
        /// 初始化可视范围检测服务
        /// </summary>
        private static void InitializeViewportService()
        {
            if (_viewportService == null)
            {
                _viewportService = new AlbumArtViewportService();
                _viewportService.ViewportChanged += OnViewportChanged;
            }
        }
        
        /// <summary>
        /// 注册列表控件
        /// </summary>
        /// <param name="itemsControl">列表控件</param>
        private static void RegisterItemsControl(ItemsControl itemsControl)
        {
            if (itemsControl == null) return;
            
            if (_registeredControls.ContainsKey(itemsControl))
                return;
                
            InitializeViewportService();
            _registeredControls[itemsControl] = true;
            
            if (_viewportService != null)
            {
                _viewportService.RegisterScrollViewer(itemsControl);
            }
        }
        
        /// <summary>
        /// 注销列表控件
        /// </summary>
        /// <param name="itemsControl">列表控件</param>
        private static void UnregisterItemsControl(ItemsControl itemsControl)
        {
            if (itemsControl == null) return;
            
            if (_registeredControls.ContainsKey(itemsControl))
            {
                _registeredControls.Remove(itemsControl);
                
                if (_viewportService != null)
                {
                    _viewportService.UnregisterScrollViewer(itemsControl);
                }
            }
        }
        
        /// <summary>
        /// 视口变化事件处理
        /// </summary>
        private static async void OnViewportChanged(object? sender, EventArgs e)
        {
            if (sender is ItemsControl itemsControl)
            {
                // 在后台线程处理，避免阻塞UI
                await Task.Run(() => HandleViewportChanged(itemsControl));
            }
        }
        
        /// <summary>
        /// 处理视口变化 - 即取即用即走即清策略
        /// </summary>
        private static void HandleViewportChanged(ItemsControl itemsControl)
        {
            if (_viewportService == null) return;
            
            try
            {
                // 获取可视范围内的项目
                var visibleItems = _viewportService.GetVisibleItems(itemsControl);
                
                // 获取离开可视范围的项目
                var outOfViewportItems = _viewportService.GetOutOfViewportItems(itemsControl, visibleItems);
                
                // 在UI线程上处理封面加载和释放
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 加载进入可视范围的封面
                    foreach (var (container, song) in visibleItems)
                    {
                        if (_viewportService.ShouldLoadAlbumArt(song, container, itemsControl))
                        {
                            song.DelayAlbumArtLoading = false;
                            song.EnsureAlbumArtLoaded();
                        }
                    }
                    
                    // 释放离开可视范围的封面 - 即走即清策略
                    foreach (var (container, song) in outOfViewportItems)
                    {
                        // 检查是否是当前播放歌曲（不释放当前播放歌曲的封面）
                        var isCurrentSong = false;
                        
                        // 尝试从容器或从全局状态获取当前播放歌曲信息
                        if (container != null)
                        {
                            // 如果容器存在，从容器向上查找
                            if (container.FindLogicalAncestor<UserControl>() is PlaylistControl playlistControl &&
                                playlistControl.DataContext is PlaylistViewModel viewModel)
                            {
                                isCurrentSong = (song == viewModel.CurrentSong);
                            }
                        }
                        else
                        {
                           // 如果容器已被虚拟化回收，尝试从服务中获取当前播放歌曲
                           var playerStateService = (App.Current as App)?.ServiceProvider?.GetService(typeof(IPlayerStateService)) as IPlayerStateService;
                            if (playerStateService != null)
                            {
                                isCurrentSong = (song.FilePath == playerStateService.CurrentSong?.FilePath);
                            }
                        }
                        
                        // 如果不是当前播放歌曲，立即释放封面资源
                        if (!isCurrentSong)
                        {
                            song.ReleaseAllAlbumArt(); // 使用新的方法，释放所有封面资源包括原始数据
                            song.DelayAlbumArtLoading = true; // 重置延迟加载状态
                            System.Diagnostics.Debug.WriteLine($"离开视口: {song.Title} - 即走即清专辑封面和原始数据");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理视口变化异常: {ex.Message}");
            }
        }
        


        /// <summary>
        /// ListBoxItem加载事件 - 容器生命周期管理
        /// 集成可视范围检测服务，实现真正的延迟加载
        /// </summary>
        private static void ListBoxItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem && listBoxItem.DataContext is Song song)
            {
                // 获取关联的ItemsControl
                var itemsControl = GetItemsControl(listBoxItem) ?? 
                                   listBoxItem.FindLogicalAncestor<ItemsControl>();
                
                if (itemsControl != null)
                {
                    // 注册ItemsControl（如果尚未注册）
                    RegisterItemsControl(itemsControl);
                    
                    // 检查是否应该加载封面
                    if (_viewportService != null && _viewportService.ShouldLoadAlbumArt(song, listBoxItem, itemsControl))
                    {
                       
                        
                        // 取消延迟加载设置
                        song.DelayAlbumArtLoading = false; 
                        // 加载专辑封面
                        song.EnsureAlbumArtLoaded(); 
                    }
                    else
                    {
                        // 不在可视范围内，保持延迟加载状态
                        song.DelayAlbumArtLoading = true;
                        System.Diagnostics.Debug.WriteLine($"容器加载: {song.Title} - 不在可视范围内，延迟加载专辑封面");
                    }
                }
            }
        }

        /// <summary>
        /// ListBoxItem卸载事件 - 容器生命周期管理
        /// 优化内存使用，及时释放不可见项目的封面
        /// </summary>
        private static void ListBoxItem_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem && listBoxItem.DataContext is Song song)
            {
                // 获取关联的ItemsControl
                var itemsControl = GetItemsControl(listBoxItem) ?? 
                                   listBoxItem.FindLogicalAncestor<ItemsControl>();
                
                // 检查是否是当前播放歌曲（不释放当前播放歌曲的封面）
                var isCurrentSong = false;
                if (listBoxItem.FindLogicalAncestor<UserControl>() is PlaylistControl playlistControl &&
                    playlistControl.DataContext is PlaylistViewModel viewModel)
                {
                    isCurrentSong = (song == viewModel.CurrentSong);
                }
                
                // 如果不是当前播放歌曲，释放封面资源
                if (!isCurrentSong)
                {
                    song.ReleaseAllAlbumArt(); // 使用新的方法，释放所有封面资源包括原始数据
                    song.DelayAlbumArtLoading = true; // 重置延迟加载状态
                    System.Diagnostics.Debug.WriteLine($"容器卸载: {song.Title} - 释放专辑封面和原始数据");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"容器卸载: {song.Title} - 当前播放歌曲，不释放封面");
                }
            }
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Dispose()
        {
            if (_viewportService != null)
            {
                _viewportService.ViewportChanged -= OnViewportChanged;
                _viewportService.Dispose();
                _viewportService = null;
            }
            
            _registeredControls.Clear();
        }
    }

    /// <summary>
    /// 扩展方法，用于查找逻辑树中的祖先元素
    /// </summary>
    public static class VisualTreeHelperExtensions
    {
        /// <summary>
        /// 查找逻辑树中的祖先元素
        /// </summary>
        public static T? FindLogicalAncestor<T>(this DependencyObject? dependencyObject) where T : class
        {
            if (dependencyObject == null)
                return null;

            var parent = System.Windows.Media.VisualTreeHelper.GetParent(dependencyObject);
            while (parent != null && !(parent is T))
            {
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }
    }
}