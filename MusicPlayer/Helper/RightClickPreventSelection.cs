using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 右键点击阻止选择行为附加属性类
    /// 用于阻止ListBox在右键点击时自动选择项，避免触发播放
    /// </summary>
    public static class RightClickPreventSelection
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(RightClickPreventSelection),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                if ((bool)e.NewValue)
                {
                    listBox.PreviewMouseRightButtonDown += ListBox_PreviewMouseRightButtonDown;
                }
                else
                {
                    listBox.PreviewMouseRightButtonDown -= ListBox_PreviewMouseRightButtonDown;
                }
            }
        }

        /// <summary>
        /// ListBox右键点击预览事件处理程序
        /// 在右键点击时阻止默认选择行为
        /// </summary>
        private static void ListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 获取点击位置下的项
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is ListBoxItem))
            {
                dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
            }

            if (dep is ListBoxItem clickedItem)
            {
                // 标记事件为已处理，阻止默认选择行为
                e.Handled = true;
                
                // 手动打开上下文菜单
                if (clickedItem.ContextMenu != null)
                {
                    // 设置菜单的PlacementTarget为点击的项
                    clickedItem.ContextMenu.PlacementTarget = clickedItem;
                    // 显示菜单
                    clickedItem.ContextMenu.IsOpen = true;
                }
            }
        }
    }
}