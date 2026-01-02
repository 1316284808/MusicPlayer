using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 歌词列表框行为附加属性类，用于处理歌词列表的滚动事件
    /// </summary>
    public static class LyricsListBoxBehavior
    {
        public static readonly DependencyProperty IsManualScrollProperty =
            DependencyProperty.RegisterAttached(
                "IsManualScroll",
                typeof(bool),
                typeof(LyricsListBoxBehavior),
                new PropertyMetadata(false));

        public static bool GetIsManualScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsManualScrollProperty);
        }

        public static void SetIsManualScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(IsManualScrollProperty, value);
        }

        public static readonly DependencyProperty HandleScrollProperty =
            DependencyProperty.RegisterAttached(
                "HandleScroll",
                typeof(bool),
                typeof(LyricsListBoxBehavior),
                new PropertyMetadata(false, OnHandleScrollChanged));

        public static bool GetHandleScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(HandleScrollProperty);
        }

        public static void SetHandleScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(HandleScrollProperty, value);
        }

        private static void OnHandleScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if ((bool)e.NewValue)
                {
                    listBox.ScrollIntoView(listBox.SelectedItem);
                    listBox.Loaded += ListBox_Loaded;
                }
                else
                {
                    listBox.Loaded -= ListBox_Loaded;
                }
            }
        }

        private static void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                // 在UI完成渲染后查找ScrollViewer并添加事件
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var scrollViewer = GetScrollViewer(listBox);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollChanged += (s, args) =>
                        {
                            // 标记用户手动滚动的状态
                            if (args.VerticalChange != 0 && !GetIsManualScroll(listBox))
                            {
                                SetIsManualScroll(listBox, true);

                                // 3秒后重置手动滚动状态
                                var timer = new System.Windows.Threading.DispatcherTimer();
                                timer.Interval = TimeSpan.FromSeconds(3);
                                timer.Tick += (timerSender, timerArgs) =>
                                {
                                    SetIsManualScroll(listBox, false);
                                    timer.Stop();
                                };
                                timer.Start();
                            }
                        };
                    }
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        public static readonly DependencyProperty AutoScrollToSelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "AutoScrollToSelectedItem",
                typeof(bool),
                typeof(LyricsListBoxBehavior),
                new PropertyMetadata(false, OnAutoScrollToSelectedItemChanged));

        public static bool GetAutoScrollToSelectedItem(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollToSelectedItemProperty);
        }

        public static void SetAutoScrollToSelectedItem(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToSelectedItemProperty, value);
        }

        private static void OnAutoScrollToSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 该逻辑已移至HandleScroll中处理
        }

        private static void AutoScrollToSelectedItem(ListBox listBox)
        {
            if (GetIsManualScroll(listBox)) return;

            if (listBox.SelectedItem != null)
            {
                // 使用Dispatcher确保UI更新完成后再滚动
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var scrollViewer = GetScrollViewer(listBox);
                        if (scrollViewer != null)
                        {
                            // 参考项目的两阶段滚动算法
                            // 第一阶段：确保当前歌词行在视图中
                            listBox.ScrollIntoView(listBox.SelectedItem);

                            // 第二阶段：精确定位到中心位置
                            var container = listBox.ItemContainerGenerator.ContainerFromItem(listBox.SelectedItem) as ListBoxItem;
                            if (container != null)
                            {
                                // 获取当前歌词项在ScrollViewer中的实际位置
                                var transform = container.TransformToAncestor(scrollViewer);
                                var itemPosition = transform.Transform(new Point(0, 0));

                                // 计算中心偏移量：视口高度的一半减去歌词项高度的一半
                                var centerOffset = scrollViewer.ViewportHeight / 2 - container.ActualHeight / 2;

                                // 计算目标滚动偏移量
                                var targetOffset = scrollViewer.VerticalOffset + itemPosition.Y - centerOffset;

                                // 执行滚动到目标位置
                                scrollViewer.ScrollToVerticalOffset(Math.Max(0, targetOffset));
                            }
                        }
                    }
                    catch
                    {
                        // 忽略任何滚动错误
                    }
                }));
            }
        }

        private static void ResetScrollPosition(ListBox listBox)
        {
            var scrollViewer = GetScrollViewer(listBox);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToTop();
            }
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject depObj)
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
    }
}