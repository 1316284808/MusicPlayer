using System.Windows;
using System.Windows.Controls;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 滑块行为附加属性类，用于处理滑块的拖拽事件
    /// </summary>
    public static class SliderBehavior
    {
        public static readonly DependencyProperty IsDraggingProperty =
            DependencyProperty.RegisterAttached(
                "IsDragging",
                typeof(bool),
                typeof(SliderBehavior),
                new PropertyMetadata(false, OnIsDraggingChanged));

        public static bool GetIsDragging(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDraggingProperty);
        }

        public static void SetIsDragging(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDraggingProperty, value);
        }

        public static readonly DependencyProperty HandleDragProperty =
            DependencyProperty.RegisterAttached(
                "HandleDrag",
                typeof(bool),
                typeof(SliderBehavior),
                new PropertyMetadata(false, OnHandleDragChanged));

        public static bool GetHandleDrag(DependencyObject obj)
        {
            return (bool)obj.GetValue(HandleDragProperty);
        }

        public static void SetHandleDrag(DependencyObject obj, bool value)
        {
            obj.SetValue(HandleDragProperty, value);
        }

        private static void OnHandleDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Slider slider)
            {
                if ((bool)e.NewValue)
                {
                    slider.PreviewMouseDown += Slider_PreviewMouseDown;
                    slider.PreviewMouseUp += Slider_PreviewMouseUp;
                    slider.LostMouseCapture += Slider_LostMouseCapture;
                    slider.ValueChanged += Slider_ValueChanged;
                    slider.MouseMove += Slider_MouseMove;
                }
                else
                {
                    slider.PreviewMouseDown -= Slider_PreviewMouseDown;
                    slider.PreviewMouseUp -= Slider_PreviewMouseUp;
                    slider.LostMouseCapture -= Slider_LostMouseCapture;
                    slider.ValueChanged -= Slider_ValueChanged;
                    slider.MouseMove -= Slider_MouseMove;
                }
            }
        }

        private static void Slider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                // 立即设置拖动状态并强制更新绑定
                SetIsDragging(slider, true);
                var bindingExpression = slider.GetBindingExpression(IsDraggingProperty);
                bindingExpression?.UpdateSource();
                
                // 获取当前位置并初始化到ViewModel
                var currentPosition = slider.Value;
                var valueBinding = slider.GetBindingExpression(Slider.ValueProperty);
                if (valueBinding != null)
                {
                    valueBinding.UpdateSource();
                }
                
                System.Diagnostics.Debug.WriteLine($"Slider: 鼠标按下，开始拖拽，当前位置: {currentPosition}");
            }
        }

        private static void Slider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                SetIsDragging(slider, false);
                
                // 重置静态变量，确保下次拖拽时能正常更新
                _lastUpdatedValue = double.NaN;
                _lastUpdateTime = DateTime.MinValue;
                
                System.Diagnostics.Debug.WriteLine($"Slider: 鼠标释放，结束拖拽，位置更新为 {slider.Value}");
            }
        }
        
        private static void Slider_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Slider slider)
            {
                SetIsDragging(slider, false);
            }
        }
        
        private static void OnIsDraggingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Slider slider)
            {
                bool isDragging = (bool)e.NewValue;
                System.Diagnostics.Debug.WriteLine($"Slider: 拖拽状态变更为 {isDragging}");
                
                if (isDragging)
                {
                    slider.CaptureMouse();
                }
                else
                {
                    if (slider.IsMouseCaptured)
                    {
                        slider.ReleaseMouseCapture();
                    }
                }
            }
        }
        
        private static void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider && GetIsDragging(slider))
            {
                System.Diagnostics.Debug.WriteLine($"Slider: 拖动值变化为 {e.NewValue}");
                
                // 在拖动过程中，我们只需要更新ViewModel中的临时位置
                var bindingExpression = slider.GetBindingExpression(Slider.ValueProperty);
                if (bindingExpression != null)
                {
                    // 立即更新绑定源，这会更新ViewModel中的临时位置
                    bindingExpression.UpdateSource();
                }
            }
        }
        
        // 添加一个静态字段来跟踪上次更新的值和时间
        private static double _lastUpdatedValue = double.NaN;
        private static DateTime _lastUpdateTime = DateTime.MinValue;
        private static readonly TimeSpan _updateThrottleInterval = TimeSpan.FromMilliseconds(50); // 50毫秒的节流间隔
        
        private static void Slider_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Slider slider && GetIsDragging(slider) && slider.IsMouseCaptured)
            {
                // 获取鼠标位置并转换为滑块值
                var position = e.GetPosition(slider);
                
                // 根据滑块方向计算值
                double value;
                if (slider.Orientation == Orientation.Vertical)
                {
                    // 竖向滑块：从底部到顶部（0到1）
                    value = slider.Maximum - (position.Y / slider.ActualHeight) * (slider.Maximum - slider.Minimum);
                }
                else
                {
                    // 水平滑块：从左到右
                    value = slider.Minimum + (position.X / slider.ActualWidth) * (slider.Maximum - slider.Minimum);
                }
                
                value = Math.Max(slider.Minimum, Math.Min(slider.Maximum, value));
                
                // 节流机制：只有当值变化足够大或者距离上次更新时间超过阈值时才更新
                var currentTime = DateTime.Now;
                var valueChangedSignificantly = double.IsNaN(_lastUpdatedValue) || 
                                               Math.Abs(value - _lastUpdatedValue) > 0.01; // 值变化超过0.01才更新
                var timeElapsedEnough = currentTime - _lastUpdateTime > _updateThrottleInterval;
                
                if (valueChangedSignificantly || timeElapsedEnough)
                {
                    System.Diagnostics.Debug.WriteLine($"Slider: 鼠标移动，方向={slider.Orientation}, 计算值为 {value}");
                    
                    // 更新滑块值，这会触发 ValueChanged 事件
                    slider.Value = value;
                    
                    // 记录本次更新的值和时间
                    _lastUpdatedValue = value;
                    _lastUpdateTime = currentTime;
                }
            }
        }
    }
}