using System;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
                        filteredCollection.CollectionChanged += (collectionSender, collectionE) => {
                            // 延迟一小段时间，确保UI更新完成后再加载封面
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                // 只加载可视区域的封面
                                var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                                var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                                LoadVisibleAlbumCovers(viewModel, scrollViewer, viewport, scrollOffset);
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        };
                    }
                    
                    // 初始加载时也加载一次封面
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                        var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                        LoadVisibleAlbumCovers(viewModel, scrollViewer, viewport, scrollOffset);
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
                scrollViewer.ScrollChanged -= OnScrollChanged;
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
                // 尝试从附加属性获取ViewModel
                var viewModel = GetViewModel(listBox);
                
                // 如果附加属性未设置，尝试从ListBox的DataContext获取
                if (viewModel == null && listBox.DataContext is ISingerViewModel dataContextViewModel)
                {
                    viewModel = dataContextViewModel;
                   }
                
                if (viewModel != null)
                {
                    // 为ListBox添加滚动事件处理
                    var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollChanged += OnScrollChanged;
                        System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: ListBox专辑封面懒加载行为已启用");
                        
                        // 监听DataContext变化，以防ViewModel在初始化后被设置
                        var dataContextBinding = System.Windows.Data.BindingOperations.GetBinding(listBox, System.Windows.Controls.ListBox.DataContextProperty);
                        if (dataContextBinding == null)
                        {
                           
                        }
                        
                        // 监听Loaded事件，确保在ListBox完全加载后再尝试
                        listBox.Loaded += (sender, e) => {
                            System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: ListBox已加载，尝试获取ViewModel");
                            // 重新获取ViewModel，因为此时DataContext应该已经设置
                            var currentViewModel = GetViewModel(listBox);
                            if (currentViewModel == null && listBox.DataContext is ISingerViewModel contextViewModel)
                            {
                                currentViewModel = contextViewModel;
                                System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: 从Loaded事件获取ViewModel");
                            }
                            
                            if (currentViewModel != null)
                            {
                                // 监听FilteredSingers集合的变化（索引切换时会更新）
                                if (currentViewModel.FilteredSingers is INotifyCollectionChanged filteredCollection)
                                {
                                    filteredCollection.CollectionChanged += (collectionSender, collectionE) => {
                                         // 延迟一小段时间，确保UI更新完成后再加载封面
                                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                            // 只加载可视区域的封面
                                            var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                                            var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                                            LoadVisibleAlbumCovers(currentViewModel, scrollViewer, viewport, scrollOffset);
                                        }), System.Windows.Threading.DispatcherPriority.Background);
                                    };
                                }
                                
                                // 延迟一小段时间，确保ListBox完全初始化后再加载封面
                                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                    // 只加载可视区域的封面
                                    var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                                    var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                                    LoadVisibleAlbumCovers(currentViewModel, scrollViewer, viewport, scrollOffset);
                                }), System.Windows.Threading.DispatcherPriority.Background);
                            }
                        };
                        
                        // 如果ListBox已经加载，立即尝试
                    // 监听FilteredSingers集合的变化（索引切换时会更新）
                    if (viewModel.FilteredSingers is INotifyCollectionChanged filteredCollection)
                    {
                        filteredCollection.CollectionChanged += (collectionSender, collectionE) => {
                             // 延迟一小段时间，确保UI更新完成后再加载封面
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                                // 只加载可视区域的封面
                                var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                                var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                                LoadVisibleAlbumCovers(viewModel, scrollViewer, viewport, scrollOffset);
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        };
                    }
                    
                    if (listBox.IsLoaded)
                    {
                          // 只加载可视区域的封面
                        var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                        var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                        LoadVisibleAlbumCovers(viewModel, scrollViewer, viewport, scrollOffset);
                    }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: 无法找到ListBox的ScrollViewer");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SingerAlbumArtBehavior: ViewModel 未设置");
                }
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
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollChanged -= OnScrollChanged;
                 }
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
       

        // 节流计时器，用于限制滚动事件处理频率
        private static System.Windows.Threading.DispatcherTimer? _scrollThrottleTimer;
        private static ScrollViewer? _pendingScrollViewer;
        private static ISingerViewModel? _pendingViewModel;
        private static Rect _pendingViewport;
        private static System.Windows.Point _pendingScrollOffset;
        
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
            double preloadFactor = 1.5;
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
            
           
            // 使用并行加载，提高预加载速度，限制并行度为5
            var parallelOptions = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 5 };
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
        /// 同步加载可视区域内及预加载区域的歌手封面（用于初始化）
        /// </summary>
        private static void LoadVisibleAlbumCovers(ISingerViewModel viewModel, ScrollViewer scrollViewer, Rect viewport, Point scrollOffset)
        {
             
            // 创建一个列表来保存需要加载封面的歌手
            var singersToLoad = new List<SingerInfo>();
            
            // 扩大可视区域检测范围，实现预加载
            // 在当前可视区域上下各增加1.5倍视口高度的预加载区域
            double preloadFactor = 1.5;
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
            
            
            // 同步加载封面（用于初始化）
            foreach (var singer in singersToLoad)
            {
                try
                {
                    LoadAlbumCover(singer);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 同步加载歌手 {singer.Name} 封面失败: {ex.Message}");
                }
            }
        }

        private static async Task CleanupInvisibleAlbumCovers(ISingerViewModel viewModel, ScrollViewer scrollViewer, Rect viewport, Point scrollOffset)
        {
            
            // 使用与预加载相同的扩展视口，只清理超出这个区域的封面
            double preloadFactor = 1.5;
            var extendedViewport = new Rect(
                viewport.X,
                viewport.Y - viewport.Height * preloadFactor,
                viewport.Width,
                viewport.Height * (1 + 2 * preloadFactor)
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

            // 优先从缓存加载封面
            var bitmapFromCache = TryLoadAlbumArtFromCache(singer.FirstSongFilePath);
            if (bitmapFromCache != null)
            {
                // 确保在UI线程上更新封面
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    // 先设置为null，再设置为新图片，强制UI更新
                    singer.CoverImage = null;
                    singer.CoverImage = bitmapFromCache;
                });
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 从缓存加载歌手 {singer.Name} 的封面完成");
                return;
            }

            // 从文件中提取专辑封面
            System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 开始异步加载歌手 {singer.Name} 的封面，文件路径: {singer.FirstSongFilePath}");
            
            try
            {
                byte[]? albumArtData = null;
                
                // 在后台线程提取封面数据
                await Task.Run(() => {
                    try
                    {
                        var tagFile = TagLib.File.Create(singer.FirstSongFilePath);
                        if (tagFile?.Tag?.Pictures?.Length > 0)
                        {
                            var picture = tagFile.Tag.Pictures[0];
                            albumArtData = picture.Data.Data;
                            System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 成功从文件提取封面数据，大小: {albumArtData.Length} 字节");
                            
                            // 保存到缓存
                            SaveAlbumArtToCache(singer.FirstSongFilePath, albumArtData);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 文件中没有找到封面信息");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 从文件提取封面数据失败: {ex.Message}");
                    }
                });

                // 如果成功提取到封面数据，加载为图像
                if (albumArtData != null && albumArtData.Length > 0)
                {
                    var bitmap = LoadBitmapFromBytes(albumArtData);
                    if (bitmap != null)
                    {
                        // 确保在UI线程上更新封面
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            // 先设置为null，再设置为新图片，强制UI更新
                            singer.CoverImage = null;
                            singer.CoverImage = bitmap;
                        });
                        System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 歌手 {singer.Name} 的封面加载完成");
                        return;
                    }
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
        /// 同步加载歌手封面（用于初始化）
        /// </summary>
        private static void LoadAlbumCover(SingerInfo singer)
        {
            if (singer.HasCoverImage) return; // 已有封面则跳过

            // 获取歌曲文件路径
            if (string.IsNullOrEmpty(singer.FirstSongFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 歌手 {singer.Name} 没有文件路径");
                // 设置默认封面
                SetDefaultCover(singer);
                return;
            }

            // 检查文件是否存在
            if (!System.IO.File.Exists(singer.FirstSongFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 歌手 {singer.Name} 的文件不存在: {singer.FirstSongFilePath}");
                // 设置默认封面
                SetDefaultCover(singer);
                return;
            }

            // 优先从缓存加载封面
            var bitmapFromCache = TryLoadAlbumArtFromCache(singer.FirstSongFilePath);
            if (bitmapFromCache != null)
            {
                // 确保在UI线程上更新封面
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    // 先设置为null，再设置为新图片，强制UI更新
                    singer.CoverImage = null;
                    singer.CoverImage = bitmapFromCache;
                });
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 从缓存加载歌手 {singer.Name} 的封面完成");
                return;
            }

            // 从文件中提取专辑封面
            System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 开始同步加载歌手 {singer.Name} 的封面，文件路径: {singer.FirstSongFilePath}");
            
            // 异步加载封面，避免阻塞UI
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // 使用TagLib#库提取专辑封面数据
                    byte[]? albumArtData = null;
                    try
                    {
                        var tagFile = TagLib.File.Create(singer.FirstSongFilePath);
                        if (tagFile?.Tag?.Pictures?.Length > 0)
                        {
                            var picture = tagFile.Tag.Pictures[0];
                            albumArtData = picture.Data.Data;
                            System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 成功从文件提取封面数据，大小: {albumArtData.Length} 字节");
                            
                            // 保存到缓存
                            SaveAlbumArtToCache(singer.FirstSongFilePath, albumArtData);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 文件中没有找到封面信息");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 从文件提取封面数据失败: {ex.Message}");
                    }

                    // 如果成功提取到封面数据，加载为图像
                    if (albumArtData != null && albumArtData.Length > 0)
                    {
                        var bitmap = LoadBitmapFromBytes(albumArtData);
                        if (bitmap != null)
                        {
                            // 确保在UI线程上更新封面
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                // 先设置为null，再设置为新图片，强制UI更新
                                singer.CoverImage = null;
                                singer.CoverImage = bitmap;
                            });
                            System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 歌手 {singer.Name} 的封面加载完成");
                            return;
                        }
                    }

                    // 如果加载失败，设置默认封面
                    SetDefaultCover(singer);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 加载歌手 {singer.Name} 的封面失败: {ex.Message}");
                    // 设置默认封面
                    //SetDefaultCover(singer);
                }
            }));
        }

        /// <summary>
        /// 异步设置默认封面
        /// </summary>
        private static async Task SetDefaultCoverAsync(SingerInfo singer)
        {
            try
            {
                // 在后台线程创建BitmapImage
                System.Windows.Media.Imaging.BitmapImage bitmap = await Task.Run(() => {
                    var defaultBitmap = new System.Windows.Media.Imaging.BitmapImage();
                    defaultBitmap.BeginInit();//C:\Users\fly\code\githubCode\MusicPlayer\MusicPlayer\resources\MusicPlayer.png
                    defaultBitmap.UriSource =  new System.Uri(Path.Combine(Paths.ExecutableDirectory, "resources", "MusicPlayer.png"), System.UriKind.Relative);
                    defaultBitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    defaultBitmap.EndInit();
                    defaultBitmap.Freeze(); // 冻结图像以确保线程安全
                    return defaultBitmap;
                });
                
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
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new System.Uri("/MusicPlayer;component/Image/默认列表.png", System.UriKind.Relative);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // 冻结图像以确保线程安全
                
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

        /// <summary>
        /// 从字节数组加载BitmapImage，并缩放到最大120*120尺寸，参考Song模型的实现
        /// </summary>
        private static System.Windows.Media.Imaging.BitmapImage? LoadBitmapFromBytes(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return null;
            }

            // 验证图像数据完整性
            if (imageBytes.Length < 100) // 太小的数据可能不是有效的图像
            {
                return null;
            }

            // 检查图像文件头以验证格式
            if (!IsValidImageData(imageBytes))
            {
                return null;
            }

            // 尝试多种图像加载方法
            System.Windows.Media.Imaging.BitmapImage? loadedImage = null;

            // 方法1: 标准加载
            loadedImage = TryLoadImage(imageBytes, System.Windows.Media.Imaging.BitmapCreateOptions.None);

            // 如果加载成功，缩放图片到最大120*120尺寸
            if (loadedImage != null)
            {
                return ResizeImage(loadedImage, 120, 120);
            }

            return loadedImage;
        }
        
        /// <summary>
        /// 将图片缩放到指定的最大宽度和高度，保持原始宽高比
        /// </summary>
        /// <param name="sourceImage">源图片</param>
        /// <param name="maxWidth">最大宽度</param>
        /// <param name="maxHeight">最大高度</param>
        /// <returns>缩放后的图片</returns>
        private static System.Windows.Media.Imaging.BitmapImage ResizeImage(System.Windows.Media.Imaging.BitmapImage sourceImage, int maxWidth, int maxHeight)
        {
            try
            {
                // 计算缩放比例
                double widthRatio = (double)maxWidth / sourceImage.Width;
                double heightRatio = (double)maxHeight / sourceImage.Height;
                double scaleRatio = Math.Min(widthRatio, heightRatio);
                
                // 如果图片已经小于等于目标尺寸，直接返回原图片
                if (scaleRatio >= 1)
                {
                    return sourceImage;
                }
                
                // 计算新尺寸
                int newWidth = (int)(sourceImage.Width * scaleRatio);
                int newHeight = (int)(sourceImage.Height * scaleRatio);
                
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 缩放图片从 {sourceImage.Width}x{sourceImage.Height} 到 {newWidth}x{newHeight}");
                
                // 创建缩放变换
                var scaleTransform = new System.Windows.Media.ScaleTransform(scaleRatio, scaleRatio);
                
                // 创建DrawingVisual并绘制缩放后的图像
                var drawingVisual = new System.Windows.Media.DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(
                        sourceImage, 
                        new System.Windows.Rect(0, 0, newWidth, newHeight)
                    );
                }
                
                // 渲染到RenderTargetBitmap
                var renderTargetBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    newWidth, 
                    newHeight, 
                    96, // DPI
                    96, // DPI
                    System.Windows.Media.PixelFormats.Default
                );
                renderTargetBitmap.Render(drawingVisual);
                
                // 编码为BitmapImage
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderTargetBitmap));
                
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    encoder.Save(memoryStream);
                    memoryStream.Position = 0;
                    
                    var resizedImage = new System.Windows.Media.Imaging.BitmapImage();
                    resizedImage.BeginInit();
                    resizedImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    resizedImage.StreamSource = memoryStream;
                    resizedImage.EndInit();
                    resizedImage.Freeze(); // 冻结图像以确保线程安全
                    
                    return resizedImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 缩放图片失败: {ex.Message}");
                // 缩放失败时返回原图片
                return sourceImage;
            }
        }

        /// <summary>
        /// 验证图像数据是否有效
        /// </summary>
        private static bool IsValidImageData(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length < 4)
                return false;

            // 检查常见图像格式的文件头
            // JPEG: FF D8 FF
            if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47
            if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                return true;

            // BMP: 42 4D
            if (imageBytes[0] == 0x42 && imageBytes[1] == 0x4D)
                return true;

            // GIF: 47 49 46 38
            if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x38)
                return true;

            return false;
        }

        /// <summary>
        /// 尝试加载图像数据
        /// </summary>
        private static System.Windows.Media.Imaging.BitmapImage? TryLoadImage(byte[] imageBytes, System.Windows.Media.Imaging.BitmapCreateOptions createOptions)
        {
            try
            {
                using (var ms = new System.IO.MemoryStream(imageBytes))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();

                    try
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        // 添加IgnoreColorProfile选项避免颜色上下文错误
                        bitmap.CreateOptions = createOptions | System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreColorProfile;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze(); // 冻结图像以确保线程安全
                    }
                    catch (Exception)
                    {
                    }

                    // 验证结果
                    if (bitmap.Width > 0 && bitmap.Height > 0)
                    {
                        return bitmap;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 生成文件路径的哈希值，用于缓存文件名
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件路径的SHA1哈希值</returns>
        private static string GenerateFileHash(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            using (var sha1 = SHA1.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(filePath);
                var hash = sha1.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// 获取封面缓存文件路径
        /// </summary>
        /// <param name="filePath">音频文件路径</param>
        /// <returns>封面缓存文件的完整路径</returns>
        private static string GetAlbumArtCachePath(string filePath)
        {
            var hash = GenerateFileHash(filePath);
            return Path.Combine(Paths.AlbumArtCacheDirectory, $"{hash}.png");
        }

        /// <summary>
        /// 从缓存加载封面
        /// </summary>
        /// <param name="filePath">音频文件路径</param>
        /// <returns>加载的BitmapImage，如果缓存不存在则返回null</returns>
        private static System.Windows.Media.Imaging.BitmapImage? TryLoadAlbumArtFromCache(string filePath)
        {
            var cachePath = GetAlbumArtCachePath(filePath);
            if (System.IO.File.Exists(cachePath))
            {
                try
                {
                    var imageBytes = System.IO.File.ReadAllBytes(cachePath);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        return LoadBitmapFromBytes(imageBytes);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 从缓存加载封面失败: {ex.Message}");
                }
            }
            return null;
        }

        /// <summary>
        /// 将封面保存到缓存
        /// </summary>
        /// <param name="filePath">音频文件路径</param>
        /// <param name="imageBytes">封面图像字节数据</param>
        private static void SaveAlbumArtToCache(string filePath, byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return;

            try
            {
                // 确保缓存目录存在
                Paths.EnsureDirectoryExists(Paths.AlbumArtCacheDirectory);
                
                var cachePath = GetAlbumArtCachePath(filePath);
                System.IO.File.WriteAllBytes(cachePath, imageBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SingerAlbumArtBehavior: 保存封面到缓存失败: {ex.Message}");
            }
        }

    }
}