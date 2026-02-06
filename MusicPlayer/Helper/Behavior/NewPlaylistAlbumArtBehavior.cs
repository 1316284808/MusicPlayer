using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.CompilerServices;
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
    /// 播放列表专辑封面加载行为
    /// 基于容器虚拟化+可视范围界定来实现封面懒加载
    /// 即用即取，即走即清的策略
    /// </summary>
    public static class NewPlaylistAlbumArtBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(NewPlaylistAlbumArtBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty ViewModelProperty = 
            DependencyProperty.RegisterAttached(
                "ViewModel",
                typeof(object),
                typeof(NewPlaylistAlbumArtBehavior),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static object GetViewModel(DependencyObject obj)
        {
            return obj.GetValue(ViewModelProperty);
        }

        public static void SetViewModel(DependencyObject obj, object value)
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
                    if (listBox?.DataContext != null)
                    {
                        viewModel = listBox.DataContext;
                        System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: 从父级ListBox的DataContext获取ViewModel");
                    }
                }
                
                if (viewModel != null)
                {
                    // 滚动事件处理懒加载
                    scrollViewer.ScrollChanged += OnScrollChanged;
                    
                    // 获取FilteredPlaylist集合
                    var filteredPlaylist = GetFilteredPlaylist(viewModel);
                    if (filteredPlaylist is INotifyCollectionChanged filteredCollection)
                    {
                        // 获取或创建该UI元素的状态
                        var state = GetOrCreateElementState(scrollViewer);
                        
                        // 创建唯一的事件处理器
                        NotifyCollectionChangedEventHandler handler = (collectionSender, collectionE) => {
                            // 延迟一小段时间，确保UI更新完成后再加载封面
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                                // 只加载可视区域的封面
                                var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                                var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                                await LoadVisibleSongCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        };
                        
                        // 移除旧的事件处理器（如果存在）
                        if (state.CollectionChangedHandler != null)
                        {
                            filteredCollection.CollectionChanged -= state.CollectionChangedHandler;
                        }
                        
                        // 添加新的事件处理器
                        filteredCollection.CollectionChanged += handler;
                        state.CollectionChangedHandler = handler;
                    }
                    
                    // 初始加载时也加载一次封面
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                        var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                        var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                        await LoadVisibleSongCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: ViewModel 未设置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 启用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void Disable(ScrollViewer scrollViewer)
        {
            try
            {
                // 移除滚动事件处理器
                scrollViewer.ScrollChanged -= OnScrollChanged;
                
                // 获取并清理该UI元素的状态
                if (_elementStates.TryGetValue(scrollViewer, out var state))
                {
                    // 移除CollectionChanged事件处理器
                    var viewModel = GetViewModel(scrollViewer);
                    if (viewModel == null)
                    {
                        var listBox = FindVisualParent<System.Windows.Controls.ListBox>(scrollViewer);
                        if (listBox?.DataContext != null)
                        {
                            viewModel = listBox.DataContext;
                        }
                    }
                    
                    if (viewModel != null)
                    {
                        var filteredPlaylist = GetFilteredPlaylist(viewModel);
                        if (filteredPlaylist is INotifyCollectionChanged filteredCollection && state.CollectionChangedHandler != null)
                        {
                            filteredCollection.CollectionChanged -= state.CollectionChangedHandler;
                        }
                    }
                    
                    // 停止并销毁计时器
                    if (state.ScrollThrottleTimer != null)
                    {
                        state.ScrollThrottleTimer.Stop();
                        state.ScrollThrottleTimer = null;
                    }
                    
                    // 清理所有引用，以便垃圾回收
                    state.PendingScrollViewer = null;
                    state.PendingViewModel = null;
                    state.CollectionChangedHandler = null;
                    
                    // 从状态字典中移除该元素
                    _elementStates.Remove(scrollViewer);
                }
             }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 禁用专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void EnableListBox(System.Windows.Controls.ListBox listBox)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: 开始启用ListBox专辑封面懒加载行为");
                
                // 为ListBox添加DataContextChanged事件监听，确保当ViewModel被父级设置后能重新初始化
                listBox.DataContextChanged += OnListBoxDataContextChanged;
                
                // 为ListBox添加Loaded事件监听，确保在ListBox完全加载后再尝试获取ViewModel
                listBox.Loaded += OnListBoxLoaded;
                
                // 初始化ScrollViewer和相关事件
                InitializeScrollViewer(listBox);
                
                System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: ListBox专辑封面懒加载行为已启用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 启用ListBox专辑封面加载行为失败: {ex.Message}");
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
                    // 移除滚动事件处理器
                    scrollViewer.ScrollChanged -= OnScrollChanged;
                    
                    // 获取并清理该UI元素的状态
                    if (_elementStates.TryGetValue(scrollViewer, out var state))
                    {
                        // 移除CollectionChanged事件处理器
                        var viewModel = GetViewModel(listBox);
                        if (viewModel == null && listBox.DataContext != null)
                        {
                            viewModel = listBox.DataContext;
                        }
                        
                        if (viewModel != null)
                        {
                            var filteredPlaylist = GetFilteredPlaylist(viewModel);
                            if (filteredPlaylist is INotifyCollectionChanged filteredCollection && state.CollectionChangedHandler != null)
                            {
                                filteredCollection.CollectionChanged -= state.CollectionChangedHandler;
                            }
                        }
                        
                        // 停止并销毁计时器
                        if (state.ScrollThrottleTimer != null)
                        {
                            state.ScrollThrottleTimer.Stop();
                            state.ScrollThrottleTimer = null;
                        }
                        
                        // 清理所有引用，以便垃圾回收
                        state.PendingScrollViewer = null;
                        state.PendingViewModel = null;
                        state.CollectionChangedHandler = null;
                        
                        // 从状态字典中移除该元素
                        _elementStates.Remove(scrollViewer);
                    }
                }
                
                // 移除ListBox事件
                listBox.DataContextChanged -= OnListBoxDataContextChanged;
                listBox.Loaded -= OnListBoxLoaded;
                
                System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: ListBox专辑封面懒加载行为已禁用");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 禁用ListBox专辑封面加载行为失败: {ex.Message}");
            }
        }

        private static void OnListBoxDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox)
            {
                System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: ListBox DataContext已变化，重新初始化封面加载");
                
                // 检查新的DataContext是否为IPlaylistViewModel或IPlaylistDetailViewModel
                if (e.NewValue is IPlaylistViewModel || e.NewValue is IPlaylistDetailViewModel)
                {
                    InitializeCoverLoading(listBox, e.NewValue);
                }
            }
        }

        private static void OnListBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ListBox listBox)
            {
                System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: ListBox已加载，初始化封面加载");
                
                // 尝试从DataContext获取ViewModel
                if (listBox.DataContext is IPlaylistViewModel || listBox.DataContext is IPlaylistDetailViewModel)
                {
                    InitializeCoverLoading(listBox, listBox.DataContext);
                }
            }
        }

        private static void InitializeScrollViewer(System.Windows.Controls.ListBox listBox)
        {
            // 查找ListBox内部的ScrollViewer
            var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
            if (scrollViewer != null)
            {
                System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: 找到ListBox内部的ScrollViewer");
                
                // 尝试从ListBox的DataContext获取ViewModel
                if (listBox.DataContext is IPlaylistViewModel || listBox.DataContext is IPlaylistDetailViewModel)
                {
                    InitializeCoverLoading(listBox, listBox.DataContext);
                }
            }
        }

        private static void InitializeCoverLoading(System.Windows.Controls.ListBox listBox, object viewModel)
        {
            try
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                if (scrollViewer == null)
                {
                    System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: 无法找到ListBox的ScrollViewer");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("NewPlaylistAlbumArtBehavior: 开始初始化封面加载逻辑");
                
                // 确保ScrollViewer的ScrollChanged事件被正确绑定
                // 先移除旧的事件绑定，避免重复绑定
                scrollViewer.ScrollChanged -= OnScrollChanged;
                // 重新添加事件绑定
                scrollViewer.ScrollChanged += OnScrollChanged;
                
                // 获取过滤后的播放列表
                var filteredPlaylist = GetFilteredPlaylist(viewModel);
                
                // 监听FilteredPlaylist集合的变化
                if (filteredPlaylist is INotifyCollectionChanged filteredCollection)
                {
                    // 获取或创建该UI元素的状态
                    var state = GetOrCreateElementState(scrollViewer);
                    
                    // 创建唯一的事件处理器
                    NotifyCollectionChangedEventHandler handler = (collectionSender, collectionE) => {
                        // 延迟一小段时间，确保UI更新完成后再加载封面
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                                var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                                var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                                await LoadVisibleSongCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                            }), System.Windows.Threading.DispatcherPriority.Background);
                    };
                    
                    // 移除旧的事件处理器（如果存在）
                    if (state.CollectionChangedHandler != null)
                    {
                        filteredCollection.CollectionChanged -= state.CollectionChangedHandler;
                    }
                    
                    // 添加新的事件处理器
                    filteredCollection.CollectionChanged += handler;
                    state.CollectionChangedHandler = handler;
                }
                
                // 初始加载时也加载一次封面
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () => {
                    var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                    var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                    await LoadVisibleSongCoversAsync(viewModel, scrollViewer, viewport, scrollOffset);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 初始化封面加载逻辑失败: {ex.Message}");
            }
        }

        // 使用ConditionalWeakTable替代Dictionary，防止内存泄漏
        private static readonly ConditionalWeakTable<object, BehaviorState> _elementStates = new();
        
        // 状态类，存储每个UI元素的独立状态
        private class BehaviorState
        {
            public System.Windows.Threading.DispatcherTimer? ScrollThrottleTimer { get; set; }
            public ScrollViewer? PendingScrollViewer { get; set; }
            public object? PendingViewModel { get; set; }
            public Rect PendingViewport { get; set; }
            public System.Windows.Point PendingScrollOffset { get; set; }
            public NotifyCollectionChangedEventHandler? CollectionChangedHandler { get; set; }
        }
        
        /// <summary>
        /// 获取或创建UI元素的状态
        /// </summary>
        private static BehaviorState GetOrCreateElementState(object element)
        {
            return _elementStates.GetValue(element, _ => new BehaviorState());
        }
        
        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                if (sender is ScrollViewer scrollViewer)
                {
                    // 获取或创建该UI元素的状态
                    var state = GetOrCreateElementState(scrollViewer);
                    
                    // 尝试从附加属性获取ViewModel
                    var viewModel = GetViewModel(scrollViewer);
                    
                    // 如果附加属性未设置，尝试从ScrollViewer的父级ListBox的DataContext获取
                    if (viewModel == null)
                    {
                        var listBox = FindVisualParent<System.Windows.Controls.ListBox>(scrollViewer);
                        if (listBox?.DataContext != null)
                        {
                            viewModel = listBox.DataContext;
                        }
                    }
                    
                    if (viewModel == null) return;

                    // 获取可视区域信息
                    var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
                    var scrollOffset = new System.Windows.Point(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
                    
                    // 使用节流机制，限制处理频率
                    if (state.ScrollThrottleTimer == null)
                    {
                        state.ScrollThrottleTimer = new System.Windows.Threading.DispatcherTimer();
                        state.ScrollThrottleTimer.Interval = System.TimeSpan.FromMilliseconds(50); // 50ms节流
                        state.ScrollThrottleTimer.Tick += (timerSender, timerE) => OnScrollThrottleTimerTick(scrollViewer, timerSender, timerE);
                    }
                    
                    // 取消现有计时器
                    state.ScrollThrottleTimer.Stop();
                    
                    // 保存当前状态到元素的独立状态中
                    state.PendingScrollViewer = scrollViewer;
                    state.PendingViewModel = viewModel;
                    state.PendingViewport = viewport;
                    state.PendingScrollOffset = scrollOffset;
                    
                    // 重新启动计时器
                    state.ScrollThrottleTimer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 滚动事件处理失败: {ex.Message}");
            }
        }
        
        private static void OnScrollThrottleTimerTick(object element, object? sender, EventArgs e)
        {
            try
            {
                // 获取该UI元素的状态
                if (!_elementStates.TryGetValue(element, out var state)) return;
                
                // 停止计时器
                if (state.ScrollThrottleTimer != null)
                {
                    state.ScrollThrottleTimer.Stop();
                }
                
                // 检查是否有挂起的滚动事件
                if (state.PendingScrollViewer != null && state.PendingViewModel != null)
                {
                    // 异步处理可视区域封面加载
                    Task.Run(async () => {
                        await LoadVisibleSongCoversAsync(state.PendingViewModel, state.PendingScrollViewer, state.PendingViewport, state.PendingScrollOffset);
                    });

                    // 异步处理清理
                    Task.Run(async () => {
                        await CleanupInvisibleSongCovers(state.PendingViewModel, state.PendingScrollViewer, state.PendingViewport, state.PendingScrollOffset);
                    });
                   
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 处理节流滚动事件失败: {ex.Message}");
            }
        }

        private static async Task LoadVisibleSongCoversAsync(object viewModel, ScrollViewer scrollViewer, Rect viewport, Point scrollOffset)
        {
            try
            {
                var songsToLoad = new List<Song>();
                
                // 扩大可视区域检测范围，实现预加载
                // 在当前可视区域上下各增加1.5倍视口高度的预加载区域
                double preloadFactor = 1.5;
                var extendedViewport = new Rect(
                    viewport.X,
                    viewport.Y - viewport.Height * preloadFactor,
                    viewport.Width,
                    viewport.Height * (1 + 2 * preloadFactor)
                );
                
                // 获取过滤后的播放列表
                var filteredPlaylist = GetFilteredPlaylist(viewModel);
                
                // 遍历过滤后的歌曲项，找出需要加载封面的歌曲
                for (int i = 0; i < filteredPlaylist.Count; i++)
                {
                    var song = filteredPlaylist[i];
                    
                    // 跳过已有封面的项
                    if (song.AlbumArt != null) continue;
                    
                    // 获取项的位置
                    if (GetItemBounds(scrollViewer, i, out Rect itemBounds))
                    {
                        // 检查项是否在扩展的预加载区域内
                        var itemViewport = itemBounds;
                        itemViewport.Offset(-scrollOffset.X, -scrollOffset.Y);
                        
                        if (extendedViewport.IntersectsWith(itemViewport))
                        {
                            // 在预加载区域内，添加到加载列表
                            songsToLoad.Add(song);
                        }
                    }
                }
                
                // 使用并行加载，提高预加载速度，限制并行度为5
                var parallelOptions = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 5 };
                await System.Threading.Tasks.Parallel.ForEachAsync(songsToLoad, parallelOptions, async (song, cancellationToken) =>
                {
                    try
                    {
                        await LoadSongCoverAsync(song);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 异步加载歌曲 {song.Title} 封面失败: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 加载可视区域封面失败: {ex.Message}");
            }
        }

        private static async Task CleanupInvisibleSongCovers(object viewModel, ScrollViewer scrollViewer, Rect viewport, Point scrollOffset)
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
                
                // 获取过滤后的播放列表
                var filteredPlaylist = GetFilteredPlaylist(viewModel);
                
                // 遍历过滤后的歌曲项
                for (int i = 0; i < filteredPlaylist.Count; i++)
                {
                    var song = filteredPlaylist[i];
                    
                    // 跳过没有封面的项
                    if (song.AlbumArt == null) continue;
                    
                    // 获取项的位置
                    if (GetItemBounds(scrollViewer, i, out Rect itemBounds))
                    {
                        // 检查项是否在扩展视口外
                        var itemViewport = itemBounds;
                        itemViewport.Offset(-scrollOffset.X, -scrollOffset.Y);
                        
                        if (!extendedViewport.IntersectsWith(itemViewport))
                        {
                            // 在扩展视口外，清理封面
                            CleanupSongCover(song);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 清理不可见区域封面失败: {ex.Message}");
            }
        }

        private static async Task LoadSongCoverAsync(Song song)
        {
            try
            {
                // 使用AlbumArtLoader异步加载封面
                var bitmap = await AlbumArtLoader.LoadAlbumArtAsync(song.FilePath);
                if (bitmap != null)
                {
                    // 在UI线程更新封面
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        // 先设置为null，再设置为新图片，强制UI更新
                        song.AlbumArt = null;
                        song.AlbumArt = bitmap;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 加载歌曲封面失败: {ex.Message}");
            }
        }

        private static void CleanupSongCover(Song song)
        {
            try
            {
                // 清理封面，释放内存 - 实现即走即清策略
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    // 清理AlbumArt（缩略图），实现即走即清
                    if (song.AlbumArt != null)
                    {
                        song.AlbumArt = null;
                    }
                    // 同时清理OriginalAlbumArt（高清大图）
                    song.OriginalAlbumArt = null;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 清理歌曲封面失败: {ex.Message}");
            }
        }

        private static bool GetItemBounds(ScrollViewer scrollViewer, int itemIndex, out Rect bounds)
        {
            // 假设每个项的高度为70（在XAML中定义的Padding和内容高度）
            const double itemHeight = 70;
            
            // 计算项目位置
            var y = itemIndex * itemHeight;
            
            bounds = new Rect(
                0,
                y,
                scrollViewer.ViewportWidth,
                itemHeight
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

        // 获取FilteredPlaylist集合
        private static ObservableCollection<Song> GetFilteredPlaylist(object viewModel)
        {
            if (viewModel is IPlaylistViewModel playlistViewModel)
            {
                return playlistViewModel.FilteredPlaylist;
            }
            else if (viewModel is IPlaylistDetailViewModel playlistDetailViewModel)
            {
                return playlistDetailViewModel.FilteredPlaylist;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"NewPlaylistAlbumArtBehavior: 未知的ViewModel类型: {viewModel.GetType().FullName}");
                return new ObservableCollection<Song>();
            }
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