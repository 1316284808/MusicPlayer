using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// StackPanel鼠标行为类，处理鼠标进入和离开事件
    /// </summary>
    public static class StackPanelMouseBehavior
    {
        public static readonly DependencyProperty MouseEnterCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseEnterCommand",
                typeof(ICommand),
                typeof(StackPanelMouseBehavior),
                new PropertyMetadata(null, OnMouseEnterCommandChanged));

        public static readonly DependencyProperty MouseLeaveCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseLeaveCommand",
                typeof(ICommand),
                typeof(StackPanelMouseBehavior),
                new PropertyMetadata(null, OnMouseLeaveCommandChanged));

        public static ICommand GetMouseEnterCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(MouseEnterCommandProperty);
        }

        public static void SetMouseEnterCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(MouseEnterCommandProperty, value);
        }

        public static ICommand GetMouseLeaveCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(MouseLeaveCommandProperty);
        }

        public static void SetMouseLeaveCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(MouseLeaveCommandProperty, value);
        }

        private static void OnMouseEnterCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StackPanel stackPanel)
            {
                // 如果之前已经设置了命令，先移除事件处理程序
                stackPanel.MouseEnter -= OnStackPanelMouseEnter;

                // 如果新命令不为null，添加事件处理程序
                if (e.NewValue != null)
                {
                    stackPanel.MouseEnter += OnStackPanelMouseEnter;
                }
            }
        }

        private static void OnMouseLeaveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StackPanel stackPanel)
            {
                // 如果之前已经设置了命令，先移除事件处理程序
                stackPanel.MouseLeave -= OnStackPanelMouseLeave;

                // 如果新命令不为null，添加事件处理程序
                if (e.NewValue != null)
                {
                    stackPanel.MouseLeave += OnStackPanelMouseLeave;
                }
            }
        }

        private static void OnStackPanelMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is StackPanel stackPanel)
            {
                var command = GetMouseEnterCommand(stackPanel);
                if (command != null && command.CanExecute(null))
                {
                    command.Execute(null);
                }
            }
        }

        private static void OnStackPanelMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is StackPanel stackPanel)
            {
                var command = GetMouseLeaveCommand(stackPanel);
                if (command != null && command.CanExecute(null))
                {
                    command.Execute(null);
                }
            }
        }
    }
}