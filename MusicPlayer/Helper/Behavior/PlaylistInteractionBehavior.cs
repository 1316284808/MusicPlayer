using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 播放列表交互行为类
    /// 实现特定的交互逻辑：左键单击选中，左键双击播放，左键单击+右键单击打开上下文菜单
    /// </summary>
    public static class PlaylistInteractionBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(PlaylistInteractionBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        // 用于跟踪左键单击状态
        private static bool _isLeftClicked = false;
        private static ListBoxItem? _leftClickedItem = null;
        private static readonly object _lockObject = new object();

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if ((bool)e.NewValue)
                {
                    listBox.PreviewMouseLeftButtonDown += ListBox_PreviewMouseLeftButtonDown;
                    listBox.PreviewMouseRightButtonDown += ListBox_PreviewMouseRightButtonDown;
                    listBox.MouseDoubleClick += ListBox_MouseDoubleClick;
                }
                else
                {
                    listBox.PreviewMouseLeftButtonDown -= ListBox_PreviewMouseLeftButtonDown;
                    listBox.PreviewMouseRightButtonDown -= ListBox_PreviewMouseRightButtonDown;
                    listBox.MouseDoubleClick -= ListBox_MouseDoubleClick;
                }
            }
        }

        /// <summary>
        /// 处理左键单击事件
        /// 只选中项目，不触发播放
        /// </summary>
        private static void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                // 获取点击位置下的项
                DependencyObject dep = (DependencyObject)e.OriginalSource;
                while (dep != null && !(dep is ListBoxItem))
                {
                    dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
                }

                if (dep is ListBoxItem clickedItem)
                {
                    lock (_lockObject)
                    {
                        _isLeftClicked = true;
                        _leftClickedItem = clickedItem;
                    }
                    
                    // 设置选中项但不触发播放
                    listBox.SelectedItem = clickedItem.Content;
                    
                    // 延迟重置状态，以便双击事件能够正常工作
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(500) // 标准双击时间间隔
                    };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        lock (_lockObject)
                        {
                            _isLeftClicked = false;
                            _leftClickedItem = null;
                        }
                    };
                    timer.Start();
                }
            }
        }

        /// <summary>
        /// 处理右键单击事件
        /// 只有在之前有左键单击的情况下才打开上下文菜单
        /// </summary>
        private static void ListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                // 获取点击位置下的项
                DependencyObject dep = (DependencyObject)e.OriginalSource;
                while (dep != null && !(dep is ListBoxItem))
                {
                    dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
                }

                if (dep is ListBoxItem clickedItem)
                {
                    bool shouldOpenContextMenu = false;
                    
                    lock (_lockObject)
                    {
                        // 检查是否是之前左键单击的同一项
                        if (_isLeftClicked && _leftClickedItem == clickedItem)
                        {
                            shouldOpenContextMenu = true;
                            _isLeftClicked = false;
                            _leftClickedItem = null;
                        }
                    }
                    
                    if (shouldOpenContextMenu)
                    {
                        // 手动打开上下文菜单
                        if (clickedItem.ContextMenu != null)
                        {
                            // 设置菜单的PlacementTarget为点击的项
                            clickedItem.ContextMenu.PlacementTarget = clickedItem;
                            // 显示菜单
                            clickedItem.ContextMenu.IsOpen = true;
                        }
                        
                        // 阻止默认行为，防止右键菜单在非预期的情况下打开
                        e.Handled = true;
                    }
                    else
                    {
                        // 重置状态
                        lock (_lockObject)
                        {
                            _isLeftClicked = false;
                            _leftClickedItem = null;
                        }
                        
                        // 阻止右键菜单打开
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// 处理双击事件
        /// 双击时播放选中的歌曲
        /// </summary>
        private static void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is Core.Models.Song selectedSong)
            {
                // 重置状态
                lock (_lockObject)
                {
                    _isLeftClicked = false;
                    _leftClickedItem = null;
                }
                
                // 获取ViewModel并调用播放命令
                if (listBox.DataContext is ViewModels.PlaylistViewModel viewModel)
                {
                    viewModel.PlaySelectedSongCommand.Execute(selectedSong);
                }
                
                // 标记事件已处理，防止冒泡
                e.Handled = true;
            }
        }
    }
}