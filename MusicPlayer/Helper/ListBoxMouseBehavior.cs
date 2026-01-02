using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Helper
{
    /// <summary>
    /// ListBox鼠标行为类，处理鼠标进入和离开事件
    /// </summary>
    public static class ListBoxMouseBehavior
    {
        public static readonly DependencyProperty MouseEnterCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseEnterCommand",
                typeof(ICommand),
                typeof(ListBoxMouseBehavior),
                new PropertyMetadata(null, OnMouseEnterCommandChanged));

        public static readonly DependencyProperty MouseLeaveCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseLeaveCommand",
                typeof(ICommand),
                typeof(ListBoxMouseBehavior),
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
            if (d is ListBox listBox)
            {
                // 如果之前已经设置了命令，先移除事件处理程序
                listBox.MouseEnter -= OnListBoxMouseEnter;

                // 如果新命令不为null，添加事件处理程序
                if (e.NewValue != null)
                {
                    listBox.MouseEnter += OnListBoxMouseEnter;
                }
            }
        }

        private static void OnMouseLeaveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                // 如果之前已经设置了命令，先移除事件处理程序
                listBox.MouseLeave -= OnListBoxMouseLeave;

                // 如果新命令不为null，添加事件处理程序
                if (e.NewValue != null)
                {
                    listBox.MouseLeave += OnListBoxMouseLeave;
                }
            }
        }

        private static void OnListBoxMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                var command = GetMouseEnterCommand(listBox);
                if (command != null && command.CanExecute(null))
                {
                    command.Execute(null);
                }
            }
        }

        private static void OnListBoxMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is ListBox listBox)
            {
                var command = GetMouseLeaveCommand(listBox);
                if (command != null && command.CanExecute(null))
                {
                    command.Execute(null);
                }
            }
        }
    }
}