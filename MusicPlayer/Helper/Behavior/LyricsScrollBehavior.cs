using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 歌词滚动行为类 - 负责处理歌词自动滚动到可见区域中心
    /// 这个行为类封装了UI特定的滚动逻辑，遵循MVVM设计原则
    /// </summary>
    public static class LyricsScrollBehavior
    {
        public static readonly DependencyProperty AutoScrollToCenterProperty =
            DependencyProperty.RegisterAttached(
                "AutoScrollToCenter",
                typeof(bool),
                typeof(LyricsScrollBehavior),
                new PropertyMetadata(false, OnAutoScrollToCenterChanged));

        public static bool GetAutoScrollToCenter(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToCenterProperty);
        }

        public static void SetAutoScrollToCenter(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToCenterProperty, value);
        }

        public static readonly DependencyProperty IsManualScrollingProperty =
            DependencyProperty.RegisterAttached(
                "IsManualScrolling",
                typeof(bool),
                typeof(LyricsScrollBehavior),
                new PropertyMetadata(false));

        public static bool GetIsManualScrolling(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsManualScrollingProperty);
        }

        public static void SetIsManualScrolling(DependencyObject obj, bool value)
        {
            obj.SetValue(IsManualScrollingProperty, value);
        }

        public static readonly DependencyProperty ScrollDurationProperty =
            DependencyProperty.RegisterAttached(
                "ScrollDuration",
                typeof(int),
                typeof(LyricsScrollBehavior),
                new PropertyMetadata(300));

        public static int GetScrollDuration(DependencyObject obj)
        {
            return (int)obj.GetValue(ScrollDurationProperty);
        }

        public static void SetScrollDuration(DependencyObject obj, int value)
        {
            obj.SetValue(ScrollDurationProperty, value);
        }

        public static readonly DependencyProperty ScrollOnLoadProperty =
            DependencyProperty.RegisterAttached(
                "ScrollOnLoad",
                typeof(bool),
                typeof(LyricsScrollBehavior),
                new PropertyMetadata(false, OnScrollOnLoadChanged));

        public static bool GetScrollOnLoad(DependencyObject obj)
        {
            return (bool)obj.GetValue(ScrollOnLoadProperty);
        }

        public static void SetScrollOnLoad(DependencyObject obj, bool value)
        {
            obj.SetValue(ScrollOnLoadProperty, value);
        }

        private static void OnAutoScrollToCenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if ((bool)e.NewValue)
                {
                    // 初始化行为
                    listBox.Loaded += OnListBoxLoaded;
                    listBox.SelectionChanged += OnListBoxSelectionChanged;
                    
                    // 重置手动滚动状态
                    SetIsManualScrolling(listBox, false);
                }
                else
                {
                    // 清理行为
                    listBox.Loaded -= OnListBoxLoaded;
                    listBox.SelectionChanged -= OnListBoxSelectionChanged;
                    
                    // 清理ScrollViewer事件
                    var scrollViewer = GetScrollViewer(listBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;
                        // 重置滚动位置
                        scrollViewer.ScrollToVerticalOffset(0);
                    }
                    
                    // 重置手动滚动状态
                    SetIsManualScrolling(listBox, false);
                }
            }
        }

        private static void OnScrollOnLoadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if ((bool)e.NewValue)
                {
                    listBox.Loaded += OnListBoxLoadedForScroll;
                }
                else
                {
                    listBox.Loaded -= OnListBoxLoadedForScroll;
                }
            }
        }

        private static void OnListBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                // 获取ScrollViewer并添加滚动事件
                var scrollViewer = GetScrollViewer(listBox);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
                }
            }
        }

        private static void OnListBoxLoadedForScroll(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                // 延迟执行，确保UI完全加载并选中项已设置
                listBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // 检查是否有选中的歌词行
                    if (listBox.SelectedItem != null)
                    {
                        System.Diagnostics.Debug.WriteLine("LyricsScrollBehavior: 页面加载时滚动到当前歌词行");
                        ScrollToCenter(listBox);
                    }
                    else
                    {
                        // 如果没有选中项，尝试获取当前播放时间并更新歌词行
                        var dataContext = listBox.DataContext;
                        if (dataContext != null)
                        {
                            // 尝试通过数据上下文获取CenterContentViewModel
                            var centerContentViewModel = GetCenterContentViewModel(dataContext);
                            if (centerContentViewModel != null)
                            {
                                // 使用反射获取当前播放时间并更新歌词行
                                try
                                {
                                    // 获取播放时间属性或方法
                                    var updateTimeMethod = centerContentViewModel.GetType().GetMethod("UpdateCurrentLyricLine", 
                                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    if (updateTimeMethod != null)
                                    {
                                        // 调用更新方法，传入0.1秒的时间，确保至少选中第一句歌词
                                        updateTimeMethod.Invoke(centerContentViewModel, new object[] { 0.1 });
                                        
                                        // 更新完成后再次尝试触发滚动
                                        listBox.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            if (listBox.SelectedItem != null)
                                            {
                                                System.Diagnostics.Debug.WriteLine("LyricsScrollBehavior: 更新后滚动到当前歌词行");
                                                ScrollToCenter(listBox);
                                            }
                                        }), System.Windows.Threading.DispatcherPriority.Background);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"LyricsScrollBehavior: 调用UpdateCurrentLyricLine失败 - {ex.Message}");
                                    
                                    // 如果反射调用失败，直接选择第一项并触发滚动
                                    if (listBox.HasItems)
                                    {
                                        listBox.SelectedIndex = 0;
                                        System.Diagnostics.Debug.WriteLine("LyricsScrollBehavior: 直接选择第一项歌词");
                                        
                                        // 延迟触发滚动，确保UI更新完成
                                        listBox.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            ScrollToCenter(listBox);
                                        }), System.Windows.Threading.DispatcherPriority.Background);
                                    }
                                }
                            }
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private static void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 当选中项改变时，触发自动滚动
            if (sender is ListBox listBox && !GetIsManualScrolling(listBox))
            {
                ScrollToCenter(listBox);
            }
        }

        // 静态字典存储每个ListBox对应的Timer，避免重复创建和内存泄漏
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ListBox, System.Windows.Threading.DispatcherTimer> _manualScrollTimers = new();

        private static void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 检测手动滚动
            if (sender is ScrollViewer scrollViewer && e.VerticalChange > 5.0)
            {
                // 找到父级ListBox
                var listBox = FindParent<ListBox>(scrollViewer);
                if (listBox != null)
                {
                    SetIsManualScrolling(listBox, true);
                    
                    // 检查是否已存在Timer，如果存在则停止并释放
                    if (_manualScrollTimers.TryGetValue(listBox, out var existingTimer))
                    {
                        existingTimer.Stop();
                        existingTimer.Tick -= OnManualScrollTimerTick;
                        _manualScrollTimers.Remove(listBox);
                    }
                    
                    // 创建新的Timer并存储到ConditionalWeakTable
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1.5)
                    };
                    
                    // 使用Tag存储listBox引用，避免闭包捕获
                    timer.Tag = listBox;
                    timer.Tick += OnManualScrollTimerTick;
                    
                    _manualScrollTimers.Add(listBox, timer);
                    timer.Start();
                }
            }
        }

        /// <summary>
        /// 手动滚动Timer的Tick事件处理
        /// </summary>
        private static void OnManualScrollTimerTick(object? sender, EventArgs e)
        {
            if (sender is System.Windows.Threading.DispatcherTimer timer)
            {
                timer.Stop();
                timer.Tick -= OnManualScrollTimerTick;
                
                // 从Tag获取ListBox引用
                if (timer.Tag is ListBox listBox)
                {
                    SetIsManualScrolling(listBox, false);
                    _manualScrollTimers.Remove(listBox);
                }
            }
        }

        /// <summary>
        /// 滚动到指定项的中心位置 - 单阶段平滑滚动算法
        /// 移除ScrollIntoView，直接计算目标偏移量，避免两阶段算法导致的定位偏差
        /// </summary>
        public static void ScrollToCenter(ListBox listBox)
        {
            if (listBox.SelectedItem == null) return;

            listBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var scrollViewer = GetScrollViewer(listBox);
                    if (scrollViewer == null) return;

                    // 强制更新布局，确保容器已生成
                    listBox.UpdateLayout();

                    var container = listBox.ItemContainerGenerator
                        .ContainerFromItem(listBox.SelectedItem) as ListBoxItem;

                    if (container == null)
                    {
                        // 容器未生成（虚拟化），尝试先滚动到大致位置
                        var index = listBox.Items.IndexOf(listBox.SelectedItem);
                        if (index >= 0)
                        {
                            // 估算平均高度并滚动到大致位置
                            var estimatedHeight = 60.0; // 基于字体大小估算
                            var estimatedOffset = Math.Max(0, index * estimatedHeight - scrollViewer.ViewportHeight / 2 + estimatedHeight / 2);
                            scrollViewer.ScrollToVerticalOffset(estimatedOffset);

                            // 等待容器生成后再次尝试精确居中
                            listBox.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                listBox.UpdateLayout();
                                ScrollToCenterInternal(listBox, scrollViewer);
                            }), System.Windows.Threading.DispatcherPriority.Background);
                        }
                        return;
                    }

                    // 直接执行精确居中
                    ScrollToCenterInternal(listBox, scrollViewer);
                }
                catch
                {
                    // 忽略任何滚动错误
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 内部方法：计算并执行精确居中滚动
        /// </summary>
        private static void ScrollToCenterInternal(ListBox listBox, ScrollViewer scrollViewer)
        {
            var container = listBox.ItemContainerGenerator
                .ContainerFromItem(listBox.SelectedItem) as ListBoxItem;

            if (container == null || !container.IsVisible) return;

            // 计算累积偏移量：从内容顶部到当前项顶部的距离
            double accumulatedOffset = 0;
            var targetIndex = listBox.Items.IndexOf(listBox.SelectedItem);

            for (int i = 0; i < targetIndex; i++)
            {
                var item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (item != null)
                {
                    accumulatedOffset += item.ActualHeight;
                }
                else
                {
                    // 容器未生成，使用估算高度
                    accumulatedOffset += 60.0;
                }
            }

            // 计算居中偏移：目标位置 = 累积高度 - (视口高度 - 项高度) / 2
            var centerOffset = (scrollViewer.ViewportHeight - container.ActualHeight) / 2;
            var targetOffset = Math.Max(0, accumulatedOffset - centerOffset);

            // 执行平滑滚动
            SmoothScrollToVerticalOffset(scrollViewer, targetOffset, GetScrollDuration(listBox));
        }

        /// <summary>
        /// 重置滚动位置到顶部
        /// 在歌词集合更新时调用，确保新歌词从顶部开始显示
        /// </summary>
        public static void ResetScrollPosition(ListBox listBox)
        {
            if (listBox == null) return;

            listBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var scrollViewer = GetScrollViewer(listBox);
                    if (scrollViewer != null)
                    {
                        // 取消正在进行的动画
                        scrollViewer.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, null);
                        // 立即滚动到顶部
                        scrollViewer.ScrollToTop();
                        System.Diagnostics.Debug.WriteLine("LyricsScrollBehavior: 滚动位置已重置到顶部");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LyricsScrollBehavior: 重置滚动位置失败 - {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 平滑滚动到指定位置
        /// </summary>
        private static void SmoothScrollToVerticalOffset(ScrollViewer scrollViewer, double offset, int duration)
        {
            var startOffset = scrollViewer.VerticalOffset;
            var difference = offset - startOffset;
            
            if (Math.Abs(difference) < 0.1) return;
            
            var animation = new DoubleAnimation(
                startOffset, offset,
                new Duration(TimeSpan.FromMilliseconds(duration)))
            {
                EasingFunction = new CubicEase()
                {
                    EasingMode = EasingMode.EaseOut
                }
            };
            
            scrollViewer.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, animation);
        }

        /// <summary>
        /// 获取ListBox中的ScrollViewer
        /// </summary>
        private static ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer viewer)
                return viewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// 在可视化树中查找父级元素
        /// </summary>
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        /// <summary>
        /// 获取CenterContentViewModel实例
        /// </summary>
        private static object GetCenterContentViewModel(object dataContext)
        {
            // 如果dataContext就是CenterContentViewModel，直接返回
            var dataContextType = dataContext.GetType();
            if (dataContextType.Name == "CenterContentViewModel")
            {
                return dataContext;
            }
            
            // 尝试通过反射获取CenterContentViewModel属性
            try
            {
                var centerContentProperty = dataContextType.GetProperty("CenterContentViewModel");
                if (centerContentProperty != null && centerContentProperty.CanRead)
                {
                    return centerContentProperty.GetValue(dataContext);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LyricsScrollBehavior: 获取CenterContentViewModel失败 - {ex.Message}");
            }
            
            return null;
        }
    }
}