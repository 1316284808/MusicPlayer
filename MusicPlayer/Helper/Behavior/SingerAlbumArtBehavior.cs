using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Models;
using MusicPlayer.ViewModels;


namespace MusicPlayer.Helper
{
    /// <summary>
    /// 歌手页面专辑封面加载行为
    /// 基于容器虚拟化+可视范围界定来实现封面懒加载
    /// 即用即取，即走即清的策略
    /// </summary>
    public static class SingerAlbumArtBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SingerAlbumArtBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.RegisterAttached(
                "ViewModel",
                typeof(ISingerViewModel),
                typeof(SingerAlbumArtBehavior),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static ISingerViewModel GetViewModel(DependencyObject obj)
        {
            return (ISingerViewModel)obj.GetValue(ViewModelProperty);
        }

        public static void SetViewModel(DependencyObject obj, ISingerViewModel value)
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
                    if (listBox?.DataContext is ISingerViewModel dataContextViewModel)
                    {
                        viewModel = dataContextViewModel;
                        System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: 从父级ListBox的DataContext获取ViewModel");
                    }
                }
                
                if (viewModel != null)
                {
                    // 滚动事件处理懒加载
                    scrollViewer.ScrollChanged += OnScrollChanged;
                    
                    // 监听FilteredSingers集合的变化（索引切换时会更新）
                    if (viewModel.FilteredSingers is INotifyCollectionChanged filteredCollection)
                    {
                        // 创建唯一的事件处理器
                        NotifyCollectionChangedEventHandler handler = (collectionSender, collectionE) => {
                            // 延迟一小段时间，确保UI更新完成后再加载封面
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                                // 异步加载可视区域及预加载区域的封面
                                var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                                var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                                await LoadVisibleAlbumCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        };
                        
                        // 移除旧的事件处理器（如果存在）
                        if (_collectionChangedHandlers.TryGetValue(scrollViewer, out var oldHandler))
                        {
                            filteredCollection.CollectionChanged -= oldHandler;
                            _collectionChangedHandlers.Remove(scrollViewer);
                        }
                        
                        // 添加新的事件处理器
                        filteredCollection.CollectionChanged += handler;
                        _collectionChangedHandlers[scrollViewer] = handler;
                    }
                    
                    // 初始加载时使用异步加载+预加载（1.5倍视口）
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                        var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                        var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                        await LoadVisibleAlbumCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: ViewModel 未设置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 启用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void Disable(ScrollViewer scrollViewer)
        {
            try
            {
                // 移除滚动事件处理器
                scrollViewer.ScrollChanged -= OnScrollChanged;
                
                // 移除CollectionChanged事件处理器
                if (_collectionChangedHandlers.TryGetValue(scrollViewer, out var collectionHandler))
                {
                    // 尝试从ViewModel中获取FilteredSingers集合
                    var viewModel = GetViewModel(scrollViewer);
                    if (viewModel == null)
                    {
                        var listBox = FindVisualParent<System.Windows.Controls.ListBox>(scrollViewer);
                        if (listBox?.DataContext is ISingerViewModel dataContextViewModel)
                        {
                            viewModel = dataContextViewModel;
                        }
                    }
                    
                    if (viewModel?.FilteredSingers is INotifyCollectionChanged filteredCollection)
                    {
                        filteredCollection.CollectionChanged -= collectionHandler;
                    }
                    
                    _collectionChangedHandlers.Remove(scrollViewer);
                }
                
                // 清理静态字段引用
                if (_pendingScrollViewer == scrollViewer)
                {
                    _pendingScrollViewer = null;
                    _pendingViewModel = null;
                }
             }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 禁用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void EnableListBox(System.Windows.Controls.ListBox listBox)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: 开始启用ListBox专辑封面懒加载行为");
                
                // 为ListBox添加DataContextChanged事件监听，确保当ViewModel被父级设置后能重新初始化
                listBox.DataContextChanged += OnListBoxDataContextChanged;
                
                // 为ListBox添加Loaded事件监听，确保在ListBox完全加载后再尝试获取ViewModel
                listBox.Loaded += OnListBoxLoaded;
                
                // 初始化ScrollViewer和相关事件
                InitializeScrollViewer(listBox);
                
                System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: ListBox专辑封面懒加载行为已启用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 启用ListBox专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void DisableListBox(System.Windows.Controls.ListBox listBox)
        {
            try
            {
                // 移除ScrollViewer相关事件
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer != null)
                {
                    // 移除滚动事件处理器
                    scrollViewer.ScrollChanged -= OnScrollChanged;
                    
                    // 移除CollectionChanged事件处理器
                    if (_collectionChangedHandlers.TryGetValue(scrollViewer, out var collectionHandler))
                    {
                        // 尝试从ViewModel中获取FilteredSingers集合
                        var viewModel = GetViewModel(listBox);
                        if (viewModel == null && listBox.DataContext is ISingerViewModel dataContextViewModel)
                        {
                            viewModel = dataContextViewModel;
                        }
                        
                        if (viewModel?.FilteredSingers is INotifyCollectionChanged filteredCollection)
                        {
                            filteredCollection.CollectionChanged -= collectionHandler;
                        }
                        
                        _collectionChangedHandlers.Remove(scrollViewer);
                    }
                    
                    // 清理静态字段引用
                    if (_pendingScrollViewer == scrollViewer)
                    {
                        _pendingScrollViewer = null;
                        _pendingViewModel = null;
                    }
                }
                
                // 移除ListBox事件
                listBox.DataContextChanged -= OnListBoxDataContextChanged;
                listBox.Loaded -= OnListBoxLoaded;
                
                System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: ListBox专辑封面懒加载行为已禁用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 禁用ListBox专辑封面加载行为失败: {ex.Message}");
            }
        }

        // 查找可视化子元素
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }
        
        // 查找可视化父元素
        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;

            var parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
        }
        
        /// <summary>
        /// 初始化ScrollViewer和相关事件
        /// </summary>
        private static void InitializeScrollViewer(System.Windows.Controls.ListBox listBox)
        {
            try
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer != null)
                {
                    // 添加滚动事件处理
                    scrollViewer.ScrollChanged += OnScrollChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 初始化ScrollViewer失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ListBox DataContext变化事件处理
        /// </summary>
        private static void OnListBoxDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox)
            {
                System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: ListBox DataContext已变化，重新初始化封面加载");
                
                // 检查新的DataContext是否为ISingerViewModel
                if (e.NewValue is ISingerViewModel viewModel)
                {
                    InitializeCoverLoading(listBox, viewModel);
                }
            }
        }
        
        /// <summary>
        /// ListBox Loaded事件处理
        /// </summary>
        private static void OnListBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox)
            {
                System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: ListBox已加载，尝试获取ViewModel");
                
                // 尝试获取ViewModel
                var viewModel = GetViewModel(listBox);
                if (viewModel == null && listBox.DataContext is ISingerViewModel dataContextViewModel)
                {
                    viewModel = dataContextViewModel;
                    System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: 从Loaded事件获取ViewModel");
                }
                
                if (viewModel != null)
                {
                    InitializeCoverLoading(listBox, viewModel);
                }
            }
        }
        
        /// <summary>
        /// 初始化封面加载逻辑
        /// </summary>
        private static void InitializeCoverLoading(System.Windows.Controls.ListBox listBox, ISingerViewModel viewModel)
        {
            try
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer == null)
                {
                    System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: 无法找到ListBox的ScrollViewer");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: 开始初始化封面加载逻辑");
                
                // 确保ScrollViewer的ScrollChanged事件被正确绑定
                // 先移除旧的事件绑定，避免重复绑定
                scrollViewer.ScrollChanged -= OnScrollChanged;
                // 重新添加事件绑定
                scrollViewer.ScrollChanged += OnScrollChanged;
                
                // 监听FilteredSingers集合的变化（索引切换时会更新）
                if (viewModel.FilteredSingers is INotifyCollectionChanged filteredCollection)
                {
                    // 创建唯一的事件处理器
                    NotifyCollectionChangedEventHandler handler = (collectionSender, collectionE) => {
                        // 延迟一小段时间，确保UI更新完成后再加载封面
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                            // 异步加载可视区域及预加载区域的封面（1.5倍视口）
                            var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                            var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                            await LoadVisibleAlbumCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    };
                    
                    // 移除旧的事件处理器（如果存在）
                    if (_collectionChangedHandlers.TryGetValue(scrollViewer, out var oldHandler))
                    {
                        filteredCollection.CollectionChanged -= oldHandler;
                        _collectionChangedHandlers.Remove(scrollViewer);
                    }
                    
                    // 添加新的事件处理器
                    filteredCollection.CollectionChanged += handler;
                    _collectionChangedHandlers[scrollViewer] = handler;
                }
                
                // 延迟一小段时间，确保ListBox完全初始化后再加载封面
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                    // 异步加载可视区域及预加载区域的封面（1.5倍视口）
                    var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                    var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                    await LoadVisibleAlbumCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                }), System.Windows.Threading.DispatcherPriority.Background);
                
                System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: 封面加载逻辑初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 初始化封面加载逻辑失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 
        ///
        /// 
        /// </summary>

        // 节流计时器，用于限制滚动事件处理频率
        private static System.Windows.Threading.DispatcherTimer? _scrollThrottleTimer;
        private static ScrollViewer? _pendingScrollViewer;
        private static ISingerViewModel? _pendingViewModel;
        private static Rect _pendingViewport;
        private static System.Windows.Point _pendingScrollOffset;
        
        // 用于跟踪每个UI元素的事件处理器，以便正确移除
        private static readonly Dictionary<object, NotifyCollectionChangedEventHandler> _collectionChangedHandlers = new();
        
        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e){
            try{
                if (sender is ScrollViewer scrollViewer){
                    // 尝试从附加属性获取ViewModel
                    var viewModel = GetViewModel(scrollViewer);
                    
                    // 如果附加属性未设置，尝试从ScrollViewer的父级ListBox的DataContext获取
                    if (viewModel == null){
                        var listBox = FindVisualParent<System.Windows.Controls.ListBox>(scrollViewer);
                        if (listBox?.DataContext is ISingerViewModel dataContextViewModel){
                            viewModel = dataContextViewModel;
                        }
                    }
                    
                    if (viewModel == null) return;

                    // 获取可视区域信息
                    var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                    var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                    
                    // 使用节流机制，限制处理频率
                    if (_scrollThrottleTimer == null){
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
            catch (Exception ex){
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 处理滚动事件失败: {ex.Message}");
            }
        }
        
        private static void OnScrollThrottleTimerTick(object? sender, EventArgs e){
            try{
                // 停止计时器
                if (_scrollThrottleTimer != null){
                    _scrollThrottleTimer.Stop();
                }
                
                // 检查是否有挂起的滚动事件
                if (_pendingScrollViewer != null && _pendingViewModel != null){
                    // 异步处理可视区域封面加载
                    Task.Run(async () => {
                        await LoadVisibleAlbumCoversAsync(_pendingViewModel, _pendingScrollViewer, _pendingViewport, _pendingScrollOffset);
                    });

                    // 同步处理清理，因为清理操作比较轻量
                    Task.Run(async () => {
                        await   CleanupInvisibleAlbumCovers(_pendingViewModel, _pendingScrollViewer, _pendingViewport, _pendingScrollOffset);
                    });
                   
                }
            }
            catch (Exception ex){
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 处理节流滚动事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步加载可视区域内及预加载区域的歌手封面
        /// </summary>
        private static async Task LoadVisibleAlbumCoversAsync(ISingerViewModel viewModel, ScrollViewer scrollViewer, Rect viewport, Point scrollOffset)
        {
              // 创建一个列表来保存需要加载封面的歌手
            var singersToLoad = new List<SingerInfo>();
            
            // 扩大可视区域检测范围，实现预加载
            // 在当前可视区域上下各增加1.5倍视口高度的预加载区域
            double preloadFactor = 1;
            var extendedViewport = new Rect(
                viewport.X,
                viewport.Y - viewport.Height * preloadFactor,
                viewport.Width,
                viewport.Height * (1 + 2 * preloadFactor)
            );
            
              
            // 遍历过滤后的歌手项，找出需要加载封面的歌手
            for (int i = 0; i < viewModel.FilteredSingers.Count; i++)
            {
                var singer = viewModel.FilteredSingers[i];
                
                // 跳过已有封面的项
                if (singer.HasCoverImage) continue;
                
                // 获取项的位置
                if (GetItemBounds(scrollViewer, i, out Rect itemBounds))
                {
                    // 检查项是否在扩展的预加载区域内
                    var itemViewport = itemBounds;
                    itemViewport.Offset(-scrollOffset.X, -scrollOffset.Y);
                    
                    if (extendedViewport.IntersectsWith(itemViewport))
                    {
                        // 在预加载区域内，添加到加载列表
                        singersToLoad.Add(singer);
                    }
                }
            }
            
           
            // 使用并行加载，提高预加载速度，限制并行度为1
            var parallelOptions = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 1};
            await System.Threading.Tasks.Parallel.ForEachAsync(singersToLoad, parallelOptions, async (singer, cancellationToken) =>
            {
                try
                {
                    await LoadAlbumCoverAsync(singer);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 异步加载歌手 {singer.Name} 封面失败: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// 同步加载可视区域内及预加载区域的歌手封面（已废弃，使用异步版本）
        /// </summary>
        private static async Task LoadVisibleAlbumCovers(ISingerViewModel viewModel, ScrollViewer scrollViewer, Rect viewport, Point scrollOffset)
        {
            // 直接调用异步版本，统一使用异步加载+预加载
            await LoadVisibleAlbumCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
        }

        private static async Task CleanupInvisibleAlbumCovers(ISingerViewModel viewModel, ScrollViewer scrollViewer, Rect viewport, Point scrollOffset)
        { 
            var extendedViewport = new Rect(
                viewport.X,
                viewport.Y  ,
                viewport.Width,
                viewport.Height  
            );
            
            // 遍历过滤后的歌手项
            for (int i = 0; i < viewModel.FilteredSingers.Count; i++)
            {
                var singer = viewModel.FilteredSingers[i];
                
                // 跳过没有封面的项
                if (!singer.HasCoverImage) continue;
                
                // 获取项的位置
                if (GetItemBounds(scrollViewer, i, out Rect itemBounds))
                {
                    // 检查项是否在扩展视口外
                    var itemViewport = itemBounds;
                    itemViewport.Offset(-scrollOffset.X, -scrollOffset.Y);
                    
                    if (!extendedViewport.IntersectsWith(itemViewport))
                    {
                        // 在扩展视口外，清理封面
                            CleanupAlbumCover(singer);
                    }
                }
            }
        }
        
        /// <summary>
        /// 清理歌手封面资源
        /// </summary>
        private static void CleanupAlbumCover(SingerInfo singer)
        {
            try
            {
                // 确保在UI线程上更新封面
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    // 释放BitmapImage资源
                    if (singer.CoverImage != null)
                    {
                        // 先设置为null，触发UI更新
                        singer.CoverImage = null;
                      }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 清理歌手 {singer.Name} 的封面失败: {ex.Message}");
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

        /// <summary>
        /// 异步加载歌手封面
        /// </summary>
        private static async Task LoadAlbumCoverAsync(SingerInfo singer)
        {
            if (singer.HasCoverImage) return; // 已有封面则跳过

            // 获取歌曲文件路径
            if (string.IsNullOrEmpty(singer.FirstSongFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 歌手 {singer.Name} 没有文件路径");
                // 设置默认封面
                await SetDefaultCoverAsync(singer);
                return;
            }

            // 检查文件是否存在
            if (!System.IO.File.Exists(singer.FirstSongFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 歌手 {singer.Name} 的文件不存在: {singer.FirstSongFilePath}");
                // 设置默认封面
                await SetDefaultCoverAsync(singer);
                return;
            }

            
              
            try
            {
                // 使用AlbumArtLoader异步加载封面，自动处理缓存和文件提取
                var bitmap = await AlbumArtLoader.LoadAlbumArtAsync(singer.FirstSongFilePath);
                if (bitmap != null)
                {
                    // 确保在UI线程上更新封面
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        // 先设置为null，再设置为新图片，强制UI更新
                        singer.CoverImage = null;
                        singer.CoverImage = bitmap;
                    });
                     return;
                }

                // 如果加载失败，设置默认封面
                await SetDefaultCoverAsync(singer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 异步加载歌手 {singer.Name} 的封面失败: {ex.Message}");
                // 设置默认封面
                //await SetDefaultCoverAsync(singer);
            }
        }
        

        /// <summary>
        /// 异步设置默认封面
        /// </summary>
        private static async Task SetDefaultCoverAsync(SingerInfo singer)
        {
            try
            {
                // 调用AlbumArtLoader获取默认封面
                var bitmap = await MusicPlayer.Core.Data.AlbumArtLoader.GetDefaultAlbumArtAsync();
                
                // 确保在UI线程上更新封面
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    // 先设置为null，再设置为新图片，强制UI更新
                    singer.CoverImage = null;
                    singer.CoverImage = bitmap;
                });
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 异步为歌手 {singer.Name} 设置默认封面");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 异步设置默认封面失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 同步设置默认封面
        /// </summary>
        private static void SetDefaultCover(SingerInfo singer)
        {
            try
            {
                // 调用AlbumArtLoader获取默认封面
                var bitmap = MusicPlayer.Core.Data.AlbumArtLoader.GetDefaultAlbumArt();
                
                // 确保在UI线程上更新封面
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    // 先设置为null，再设置为新图片，强制UI更新
                    singer.CoverImage = null;
                    singer.CoverImage = bitmap;
                });
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 为歌手 {singer.Name} 设置默认封面");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 设置默认封面失败: {ex.Message}");
            }
        }
    }
}