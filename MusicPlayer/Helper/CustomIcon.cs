
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// 自定义图标组件 - 使用Segoe Fluent Icons字体
    /// </summary>
    public class CustomIcon : ContentControl
    {
        public static readonly DependencyProperty KindProperty =
            DependencyProperty.Register(nameof(Kind), typeof(IconKind), typeof(CustomIcon),
                new PropertyMetadata(IconKind.None, OnKindChanged));

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(CustomIcon),
                new PropertyMetadata(16.0, OnIconSizeChanged));

        public static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(CustomIcon),
                new PropertyMetadata(null, OnForegroundChanged));

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(CustomIcon),
                new PropertyMetadata(null, OnBackgroundChanged));

        public IconKind Kind
        {
            get => (IconKind)GetValue(KindProperty);
            set => SetValue(KindProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public new Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public new Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public CustomIcon()
        {
            this.Content = new TextBlock();
            IconSize = 16.0;
            base.Background = Brushes.Transparent;
            UpdateIcon();
        }

        private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = (CustomIcon)d;
            icon.UpdateIcon();
        }

        private static void OnIconSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = (CustomIcon)d;
            icon.UpdateIcon();
        }

        private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = (CustomIcon)d;
            icon.UpdateIcon();
        }

        private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = (CustomIcon)d;
            icon.UpdateIcon();
        }

        private void UpdateIcon()
        {
            if (this.Content is TextBlock textBlock)
            {
                textBlock.Text = IconService.GetIconChar(Kind);
                
                // 如果设置了自定义前景色，使用自定义颜色；否则使用默认资源
                if (Foreground != null)
                {
                    textBlock.Foreground = Foreground;
                }
                else
                {
                    // 使用动态资源设置前景色，与全局文本样式保持一致
                    textBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
                }
                
                // 设置背景色
                if (Background != null)
                {
                    base.Background = Background;
                }
                
                textBlock.FontSize = IconSize;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.FontFamily = new FontFamily("Segoe Fluent Icons");
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                textBlock.TextAlignment = TextAlignment.Center;
            }
        }
    }
}