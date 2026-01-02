using System.Windows;
using System.Windows.Input;
using MusicPlayer.ViewModels;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Controls
{
    public partial class TitleBarControl
    {
        private readonly IMessagingService? _messagingService;
        public TitleBarControl( )
        {
            InitializeComponent();
            
           
        }
      


        // 窗口拖拽逻辑（视图相关，保留在代码后置）
        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.DragMove();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"窗口拖拽操作失败: {ex.Message}");
            }
        }

        private void DragArea_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                // 可保留为空或用于其他视图交互目的
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"鼠标移动事件处理失败: {ex.Message}");
            }
        }

        // 双击标题栏最大化/还原（视图相关，保留在代码后置）
        private void DragArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
                {
                    // 通过消息系统发送最大化/还原命令，而不是直接访问ViewModel
                    // ToggleMaximizeWindowMessage 是 RequestMessage<bool>，需要使用带返回值的 Send 方法
                    _messagingService?.Send<ToggleMaximizeWindowMessage, bool>(new ToggleMaximizeWindowMessage());
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"双击标题栏操作失败: {ex.Message}");
            }
        }
    }
}