using System.Windows;
using System.Windows.Input;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 音量滚轮行为附加属性类，用于在鼠标悬浮时通过滚轮调节音量
    /// </summary>
    public static class VolumeWheelBehavior
    {
        /// <summary>
        /// 是否启用滚轮调节音量
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(VolumeWheelBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>
        /// 音量调节步长（百分比，默认5%）
        /// </summary>
        public static readonly DependencyProperty StepProperty =
            DependencyProperty.RegisterAttached(
                "Step",
                typeof(double),
                typeof(VolumeWheelBehavior),
                new PropertyMetadata(0.05));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static double GetStep(DependencyObject obj)
        {
            return (double)obj.GetValue(StepProperty);
        }

        public static void SetStep(DependencyObject obj, double value)
        {
            obj.SetValue(StepProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.PreviewMouseWheel += Element_PreviewMouseWheel;
                }
                else
                {
                    element.PreviewMouseWheel -= Element_PreviewMouseWheel;
                }
            }
        }

        private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext != null)
            {
                // 通过反射从 DataContext 获取 Volume 属性
                var volumeProperty = element.DataContext.GetType().GetProperty("Volume");
                if (volumeProperty != null && volumeProperty.PropertyType == typeof(float))
                {
                    var volumeValue = volumeProperty.GetValue(element.DataContext);
                    if (volumeValue is float currentVolume)
                    {
                        var step = GetStep(element);

                        // 根据滚轮方向调整音量
                        // e.Delta > 0 表示向上滚动（增加），< 0 表示向下滚动（减少）
                        float delta = (e.Delta > 0 ? 1 : -1) * (float)step;
                        float newVolume = Math.Clamp(currentVolume + delta, 0.0f, 1.0f);

                        volumeProperty.SetValue(element.DataContext, newVolume);

                     
                        // 标记事件已处理，防止事件冒泡
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
