using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Core.Interface;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using static System.Net.Mime.MediaTypeNames;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 标题栏视图模型 - 负责窗口控制相关逻辑
    /// 使用 MusicPlayer.Core.Interface.IMessagingService与其他ViewModel进行通信
    /// </summary>
    public class TitleBarViewModel : ObservableObject, ITitleBarViewModel
    {
        private readonly IMessagingService _messagingService;
        private string _title = "Music Player";
        private bool _isWindowMaximized;

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// 窗口是否最大化（True显示还原图标，False显示最大化图标）
        /// </summary>
        public bool MaximizeButtonIcon
        {
            get => _isWindowMaximized;
            set
            {
                if (_isWindowMaximized != value)
                {
                    _isWindowMaximized = value;
                    OnPropertyChanged(nameof(MaximizeButtonIcon));
                }
            }
        }

        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        public ICommand CloseWindowCommand { get; }

        /// <summary>
        /// 最小化窗口命令
        /// </summary>
        public ICommand MinimizeWindowCommand { get; }

        /// <summary>
        /// 最大化/还原窗口命令
        /// </summary>
        public ICommand MaximizeRestoreWindowCommand { get; }
        
        /// <summary>
        /// 最大化/还原窗口命令（用于XAML绑定的别名）
        /// </summary>
        public ICommand MaximizeOrRestoreCommand => MaximizeRestoreWindowCommand;

        /// <summary>
        /// 返回上一页命令
        /// </summary>
        public ICommand GoBackCommand { get; }

        public TitleBarViewModel(IMessagingService messagingService)
        {
            _messagingService = messagingService;
            
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindow);
            MinimizeWindowCommand = new RelayCommand(ExecuteMinimizeWindow);
            MaximizeRestoreWindowCommand = new RelayCommand(ExecuteMaximizeRestoreWindow);
            GoBackCommand = new RelayCommand(ExecuteGoBack);

            // 订阅窗口状态变化消息
            _messagingService.Register<WindowStateChangedMessage>(this, OnWindowStateChanged);
        }

        /// <summary>
        /// 执行关闭窗口操作
        /// </summary>
        private void ExecuteCloseWindow()
        {
            System.Diagnostics.Debug.WriteLine("TitleBarViewModel.ExecuteCloseWindow called");
            // CloseWindowMessage 是 RequestMessage<bool>，需要使用带返回值的 Send 方法
            var result = _messagingService.Send<CloseWindowMessage, bool>(new CloseWindowMessage());
            System.Diagnostics.Debug.WriteLine($"TitleBarViewModel.ExecuteCloseWindow: result = {result}");
        }

        /// <summary>
        /// 执行最小化窗口操作
        /// </summary>
        private void ExecuteMinimizeWindow()
        {
            // MinimizeWindowMessage 是 RequestMessage<bool>，需要使用带返回值的 Send 方法
            _messagingService.Send<MinimizeWindowMessage, bool>(new MinimizeWindowMessage());
        }

        /// <summary>
        /// 执行最大化/还原窗口操作
        /// </summary>
        private void ExecuteMaximizeRestoreWindow()
        {
            // ToggleMaximizeWindowMessage 是 RequestMessage<bool>，需要使用带返回值的 Send 方法
            _messagingService.Send<ToggleMaximizeWindowMessage, bool>(new ToggleMaximizeWindowMessage());
        }

        /// <summary>
        /// 执行返回上一页操作
        /// </summary>
        private void ExecuteGoBack()
        {
            // 发送返回上一页的消息
            _messagingService.Send<GoBackMessage>(new GoBackMessage());
        }

        /// <summary>
        /// 根据窗口状态更新最大化按钮图标
        /// </summary>
        /// <param name="windowState">窗口状态</param>
        private void UpdateMaximizeButtonIcon(WindowState windowState)
        {
            MaximizeButtonIcon = (windowState == WindowState.Maximized);
        }

        /// <summary>
        /// 处理窗口状态变化消息
        /// </summary>
        private void OnWindowStateChanged(object recipient, WindowStateChangedMessage message)
        {
            UpdateMaximizeButtonIcon(message.Value);
        }

        /// <summary>
        /// 初始化ViewModel
        /// </summary>
        public override void Initialize()
        {
            // TitleBarViewModel的初始化逻辑
        }

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            // 注销消息处理器
            _messagingService.Unregister(this);
        }

        
    }

   
}