using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 歌词窗口鼠标悬浮行为
    /// </summary>
    public static class LyricWindowHoverBehavior
    {
        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.RegisterAttached(
                "IsHovered",
                typeof(bool),
                typeof(LyricWindowHoverBehavior),
                new PropertyMetadata(false, OnIsHoveredChanged));

        public static bool GetIsHovered(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsHoveredProperty);
        }

        public static void SetIsHovered(DependencyObject obj, bool value)
        {
            obj.SetValue(IsHoveredProperty, value);
        }

        private static void OnIsHoveredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Border border)
            {
                if ((bool)e.NewValue)
                {
                    // 鼠标悬浮状态：背景色变为e6e6e6
                    border.Background = Brushes.LightGray;
                }
                else
                {
                    // 鼠标离开状态：背景色恢复为01000000
                    border.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 1));
                }
                
                // 更新视图模型的IsMouseOver属性
                if (border.DataContext != null)
                {
                    var property = border.DataContext.GetType().GetProperty("IsMouseOver");
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(border.DataContext, (bool)e.NewValue);
                    }
                }
            }
        }

        public static readonly DependencyProperty EnableHoverEffectProperty =
            DependencyProperty.RegisterAttached(
                "EnableHoverEffect",
                typeof(bool),
                typeof(LyricWindowHoverBehavior),
                new PropertyMetadata(false, OnEnableHoverEffectChanged));

        public static bool GetEnableHoverEffect(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableHoverEffectProperty);
        }

        public static void SetEnableHoverEffect(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableHoverEffectProperty, value);
        }

        private static void OnEnableHoverEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Border border)
            {
                if ((bool)e.NewValue)
                {
                    border.MouseEnter += Border_MouseEnter;
                    border.MouseLeave += Border_MouseLeave;
                }
                else
                {
                    border.MouseEnter -= Border_MouseEnter;
                    border.MouseLeave -= Border_MouseLeave;
                }
            }
        }

        private static void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                SetIsHovered(border, true);
            }
        }

        private static void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                SetIsHovered(border, false);
            }
        }
    }
}