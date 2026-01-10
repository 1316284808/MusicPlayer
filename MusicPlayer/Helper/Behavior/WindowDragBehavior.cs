using System.Windows;
using System.Windows.Input;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 窗口拖动行为
    /// </summary>
    public static class WindowDragBehavior
    {
        public static readonly DependencyProperty EnableDragProperty =
            DependencyProperty.RegisterAttached(
                "EnableDrag",
                typeof(bool),
                typeof(WindowDragBehavior),
                new PropertyMetadata(false, OnEnableDragChanged));

        public static bool GetEnableDrag(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableDragProperty);
        }

        public static void SetEnableDrag(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableDragProperty, value);
        }

        private static void OnEnableDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                }
                else
                {
                    element.MouseLeftButtonDown -= Element_MouseLeftButtonDown;
                }
            }
        }

        private static void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement element)
            {
                // 获取窗口
                var window = Window.GetWindow(element);
                if (window != null)
                {
                    try
                    {
                        window.DragMove();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"拖动窗口失败: {ex.Message}");
                    }
                }
            }
        }
    }
}