using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Models;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 专辑页面专辑封面加载行为
    /// 基于容器虚拟化+可视范围界定来实现封面懒加载
    /// 即用即取，即走即清的策略
    /// </summary>
    public static class AlbumAlbumArtBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(AlbumAlbumArtBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.RegisterAttached(
                "ViewModel",
                typeof(IAlbumViewModel),
                typeof(AlbumAlbumArtBehavior),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static IAlbumViewModel GetViewModel(DependencyObject obj)
        {
            return (IAlbumViewModel)obj.GetValue(ViewModelProperty);
        }

        public static void SetViewModel(DependencyObject obj, IAlbumViewModel value)
        {
            obj.SetValue(ViewModelProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 同时支持ScrollViewer和ListBox
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
            else if (d is System.Windows.Controls.ListBox listBox)
            {
                if ((bool)e.NewValue)
                {
                    EnableListBox(listBox);
                }
                else
                {
                    DisableListBox(listBox);
                }
            }
        }

        private static void Enable(ScrollViewer scrollViewer)
        {
            try
            {
                var viewModel = GetViewModel(scrollViewer);
                
                // 如果附加属性未设置，尝试从ScrollViewer的父级ListBox的DataContext获取
                if (viewModel == null)
                {
                    var listBox = FindVisualParent<System.Windows.Controls.ListBox>(scrollViewer);
                    if (listBox?.DataContext is IAlbumViewModel dataContextViewModel)
                    {
                        viewModel = dataContextViewModel;
                        System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: 从父级ListBox的DataContext获取ViewModel");
                    }
                }
                
                if (viewModel != null)
                {
                    // 滚动事件处理懒加载
                    scrollViewer.ScrollChanged += OnScrollChanged;
                    
                    // 监听FilteredAlbums集合的变化（索引切换时会更新）
                    if (viewModel.FilteredAlbums is INotifyCollectionChanged filteredCollection)
                    {
                        filteredCollection.CollectionChanged += (collectionSender, collectionE) => {
                            // 延迟一小段时间，确保UI更新完成后再加载封面
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                                // 只加载可视区域的封面
                                var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                                var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                                await LoadVisibleAlbumCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        };
                    }
                    
                    // 初始加载时也加载一次封面
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                        var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                        var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                        await LoadVisibleAlbumCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: ViewModel 未设置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 启用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void Disable(ScrollViewer scrollViewer)
        {
            try
            {
                scrollViewer.ScrollChanged -= OnScrollChanged;
             }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 禁用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void EnableListBox(System.Windows.Controls.ListBox listBox)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: 开始启用ListBox专辑封面懒加载行为");
                
                // 为ListBox添加DataContextChanged事件监听，确保当ViewModel被父级设置后能重新初始化
                listBox.DataContextChanged += OnListBoxDataContextChanged;
                
                // 为ListBox添加Loaded事件监听，确保在ListBox完全加载后再尝试获取ViewModel
                listBox.Loaded += OnListBoxLoaded;
                
                // 初始化ScrollViewer和相关事件
                InitializeScrollViewer(listBox);
                
                System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: ListBox专辑封面懒加载行为已启用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 启用ListBox专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void DisableListBox(System.Windows.Controls.ListBox listBox)
        {
            try
            {
                // 移除ScrollViewer事件
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollChanged -= OnScrollChanged;
                }
                
                // 移除ListBox事件
                listBox.DataContextChanged -= OnListBoxDataContextChanged;
                listBox.Loaded -= OnListBoxLoaded;
                
                System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: ListBox专辑封面懒加载行为已禁用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 禁用ListBox专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void OnListBoxDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox)
            {
                System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: ListBox DataContext已变化，重新初始化封面加载");
                
                // 检查新的DataContext是否为IAlbumViewModel
                if (e.NewValue is IAlbumViewModel viewModel)
                {
                    InitializeCoverLoading(listBox, viewModel);
                }
            }
        }

        private static void OnListBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox)
            {
                System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: ListBox已加载，初始化封面加载");
                
                // 尝试从DataContext获取ViewModel
                if (listBox.DataContext is IAlbumViewModel viewModel)
                {
                    InitializeCoverLoading(listBox, viewModel);
                }
            }
        }

        private static void InitializeScrollViewer(System.Windows.Controls.ListBox listBox)
        {
            // 查找ListBox内部的ScrollViewer
            var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
            if (scrollViewer != null)
            {
                System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: 找到ListBox内部的ScrollViewer");
                
                // 尝试从ListBox的DataContext获取ViewModel
                if (listBox.DataContext is IAlbumViewModel viewModel)
                {
                    InitializeCoverLoading(listBox, viewModel);
                }
            }
        }

        private static void InitializeCoverLoading(System.Windows.Controls.ListBox listBox, IAlbumViewModel viewModel)
        {
            try
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer == null)
                {
                    System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: 无法找到ListBox的ScrollViewer");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("AlbumAlbumArtBehavior: 开始初始化封面加载逻辑");
                
                // 确保ScrollViewer的ScrollChanged事件被正确绑定
                // 先移除旧的事件绑定，避免重复绑定
                scrollViewer.ScrollChanged -= OnScrollChanged;
                // 重新添加事件绑定
                scrollViewer.ScrollChanged += OnScrollChanged;
                
                // 监听FilteredAlbums集合的变化（索引切换时会更新）
                if (viewModel.FilteredAlbums is INotifyCollectionChanged filteredCollection)
                {
                    filteredCollection.CollectionChanged += (collectionSender, collectionE) => {
                        // 延迟一小段时间，确保UI更新完成后再加载封面
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                                var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                                var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                                await LoadVisibleAlbumCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                            }), System.Windows.Threading.DispatcherPriority.Background);
                    };
                }
                
                // 初始加载时也加载一次封面
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                    var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                    var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                    await LoadVisibleAlbumCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 初始化封面加载逻辑失败: {ex.Message}");
            }
        }

        // 节流计时器，用于限制滚动事件处理频率
        private static System.Windows.Threading.DispatcherTimer? _scrollThrottleTimer;
        private static ScrollViewer? _pendingScrollViewer;
        private static IAlbumViewModel? _pendingViewModel;
        private static Rect _pendingViewport;
        private static System.Windows.Point _pendingScrollOffset;
        
        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (sender is ScrollViewer scrollViewer)
                {
                    // 尝试从附加属性获取ViewModel
                    var viewModel = GetViewModel(scrollViewer);
                    
                    // 如果附加属性未设置，尝试从ScrollViewer的父级ListBox的DataContext获取
                    if (viewModel == null)
                    {
                        var listBox = FindVisualParent<System.Windows.Controls.ListBox>(scrollViewer);
                        if (listBox?.DataContext is IAlbumViewModel dataContextViewModel)
                        {
                            viewModel = dataContextViewModel;
                        }
                    }
                    
                    if (viewModel == null) return;

                    // 获取可视区域信息
                    var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                    var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                    
                    // 使用节流机制，限制处理频率
                    if (_scrollThrottleTimer == null)
                    {
                        _scrollThrottleTimer = new System.Windows.Threading.DispatcherTimer();
                        _scrollThrottleTimer.Interval = System.TimeSpan.FromMilliseconds(50); // 50ms节流
                        _scrollThrottleTimer.Tick += OnScrollThrottleTimerTick;
                    }
                    
                    // 取消现有计时器
                    _scrollThrottleTimer.Stop();
                    
                    // 保存当前状态
                    _pendingScrollViewer = scrollViewer;
                    _pendingViewModel = viewModel;
                    _pendingViewport = viewport;
                    _pendingScrollOffset = scrollOffset;
                    
                    // 重新启动计时器
                    _scrollThrottleTimer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 滚动事件处理失败: {ex.Message}");
            }
        }
        
        private static void OnScrollThrottleTimerTick(object? sender, EventArgs e)
        {
            try
            {
                // 停止计时器
                if (_scrollThrottleTimer != null)
                {
                    _scrollThrottleTimer.Stop();
                }
                
                // 检查是否有挂起的滚动事件
                if (_pendingScrollViewer != null && _pendingViewModel != null)
                {
                    // 异步处理可视区域封面加载
                    Task.Run(async () => {
                        await LoadVisibleAlbumCoversAsync(_pendingViewModel, _pendingScrollViewer, _pendingViewport, _pendingScrollOffset);
                    });

                    // 异步处理清理
                    Task.Run(async () => {
                        await CleanupInvisibleAlbumCovers(_pendingViewModel, _pendingScrollViewer, _pendingViewport, _pendingScrollOffset);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 处理节流滚动事件失败: {ex.Message}");
            }
        }

        private static async Task LoadVisibleAlbumCoversAsync(IAlbumViewModel viewModel, ScrollViewer scrollViewer, Rect viewport, Point scrollOffset)
        {
            try
            {
                var albumsToLoad = new List<AlbumInfo>();
                
                // 扩大可视区域检测范围，实现预加载
                // 在当前可视区域上下各增加1.5倍视口高度的预加载区域
                double preloadFactor = 1.5;
                var extendedViewport = new Rect(
                    viewport.X,
                    viewport.Y - viewport.Height * preloadFactor,
                    viewport.Width,
                    viewport.Height * (1 + 2 * preloadFactor)
                );
                
                // 遍历过滤后的专辑项，找出需要加载封面的专辑
                for (int i = 0; i < viewModel.FilteredAlbums.Count; i++)
                {
                    var album = viewModel.FilteredAlbums[i];
                    
                    // 跳过已有封面的项
                    if (album.HasCoverImage) continue;
                    
                    // 获取项的位置
                    if (GetItemBounds(scrollViewer, i, out Rect itemBounds))
                    {
                        // 检查项是否在扩展的预加载区域内
                        var itemViewport = itemBounds;
                        itemViewport.Offset(-scrollOffset.X, -scrollOffset.Y);
                        
                        if (extendedViewport.IntersectsWith(itemViewport))
                        {
                            // 在预加载区域内，添加到加载列表
                            albumsToLoad.Add(album);
                        }
                    }
                }
                
                // 使用并行加载，提高预加载速度，限制并行度为5
                var parallelOptions = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 5 };
                await System.Threading.Tasks.Parallel.ForEachAsync(albumsToLoad, parallelOptions, async (album, cancellationToken) =>
                {
                    try
                    {
                        await LoadAlbumCoverAsync(album);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 异步加载专辑 {album.Name} 封面失败: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 加载可视区域封面失败: {ex.Message}");
            }
        }

        private static async Task CleanupInvisibleAlbumCovers(IAlbumViewModel viewModel, ScrollViewer scrollViewer, Rect viewport, Point scrollOffset)
        {
            try
            {
                // 扩大可视区域检测范围，避免滚动时频繁加载/清理
                // 只清理距离可视区域较远的项
                double cleanupFactor = 2.0;
                var extendedViewport = new Rect(
                    viewport.X - viewport.Width * cleanupFactor,
                    viewport.Y - viewport.Height * cleanupFactor,
                    viewport.Width * (1 + 2 * cleanupFactor),
                    viewport.Height * (1 + 2 * cleanupFactor)
                );
                
                // 遍历过滤后的专辑项
                for (int i = 0; i < viewModel.FilteredAlbums.Count; i++)
                {
                    var album = viewModel.FilteredAlbums[i];
                    
                    // 跳过没有封面的项
                    if (!album.HasCoverImage) continue;
                    
                    // 获取项的位置
                    if (GetItemBounds(scrollViewer, i, out Rect itemBounds))
                    {
                        // 检查项是否在扩展视口外
                        var itemViewport = itemBounds;
                        itemViewport.Offset(-scrollOffset.X, -scrollOffset.Y);
                        
                        if (!extendedViewport.IntersectsWith(itemViewport))
                        {
                            // 在扩展视口外，清理封面
                            CleanupAlbumCover(album);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 清理不可见区域封面失败: {ex.Message}");
            }
        }

        private static async Task LoadAlbumCoverAsync(AlbumInfo album)
        {
            try
            {
                if (string.IsNullOrEmpty(album.FirstSongFilePath))
                    return;
                
                // 使用AlbumArtLoader异步加载封面
                var bitmap = await AlbumArtLoader.LoadAlbumArtAsync(album.FirstSongFilePath);
                
                // 将封面设置给专辑
                if (bitmap != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        // 先设置为null，再设置为新图片，强制UI更新
                        album.CoverImage = null;
                        album.CoverImage = bitmap;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 加载专辑封面失败: {ex.Message}");
            }
        }

        private static void CleanupAlbumCover(AlbumInfo album)
        {
            try
            {
                // 清理封面，释放内存
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    album.CoverImage = null;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AlbumAlbumArtBehavior: 清理专辑封面失败: {ex.Message}");
            }
        }

        private static bool GetItemBounds(ScrollViewer scrollViewer, int itemIndex, out Rect bounds)
        {
            // 假设每个项的大小为 280x120 (在XAML中定义的Width="280" Height="120")
            const double itemWidth = 280 + 20; // 包含边距
            const double itemHeight = 120 + 20; // 包含边距
            
            // 计算每行的项目数（基于容器宽度）
            var itemsPerRow = Math.Max(1, (int)(scrollViewer.ViewportWidth / itemWidth));  
            // 计算项目位置
            var row = itemIndex / itemsPerRow;
            var column = itemIndex % itemsPerRow;
            
            var x = column * itemWidth;
            var y = row * itemHeight;
            
            bounds = new Rect(
                x,
                y,
                itemWidth - 20, // 减去边距
                itemHeight - 20  // 减去边距
            );
            
            return true;
        }

        // 查找可视化父元素
        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null)
                return null;

            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T parentT)
                    return parentT;
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        // 查找可视化子元素
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T childT)
                    return childT;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}