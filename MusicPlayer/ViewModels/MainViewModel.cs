using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer.Services;
using MusicPlayer.Services.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 主视图模型 - 父容器不实现任何实际功能
    /// </summary>
    public partial class MainViewModel : ObservableObject, IMainViewModel
    {
        private readonly IServiceCoordinator _serviceCoordinator;
        private readonly WindowManagerService _windowManagerService;
        private readonly IMessagingService _messagingService;
        private readonly IServiceProvider _serviceProvider;
      
        public IControlBarViewModel ControlBarViewModel { get; private set; }
        public ITitleBarViewModel TitleBarViewModel { get; private set; }
        
        // Transient ViewModel不再直接持有，通过ServiceProvider获取
        public IPlaylistViewModel PlaylistViewModel => _serviceProvider.GetRequiredService<IPlaylistViewModel>();
        public ICenterContentViewModel CenterContentViewModel => _serviceProvider.GetRequiredService<ICenterContentViewModel>();

    public MainViewModel(
        IControlBarViewModel controlBarViewModel,
        ITitleBarViewModel titleBarViewModel,
        IServiceCoordinator serviceCoordinator,
        WindowManagerService windowManagerService,
        IMessagingService messagingService,
        IServiceProvider serviceProvider)
    {
        // 注入服务
        _serviceCoordinator = serviceCoordinator ?? throw new ArgumentNullException(nameof(serviceCoordinator));
        _windowManagerService = windowManagerService ?? throw new ArgumentNullException(nameof(windowManagerService));
        _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        // 使用依赖注入的子ViewModel（单例）
        ControlBarViewModel = controlBarViewModel ?? throw new ArgumentNullException(nameof(controlBarViewModel));
        TitleBarViewModel = titleBarViewModel ?? throw new ArgumentNullException(nameof(titleBarViewModel));
      
        // 初始化命令
        ShowMainWindowCommand = new RelayCommand(ShowMainWindow);
        TogglePlayPauseCommand = new RelayCommand(TogglePlayPause);
        PreviousTrackCommand = new RelayCommand(PreviousTrack);
        NextTrackCommand = new RelayCommand(NextTrack);
        ExitApplicationCommand = new RelayCommand(ExitApplication);
    }
    
    public ICommand ShowMainWindowCommand { get; private set; }
    public ICommand TogglePlayPauseCommand { get; private set; }
    public ICommand PreviousTrackCommand { get; private set; }
    public ICommand NextTrackCommand { get; private set; }
    public ICommand ExitApplicationCommand { get; private set; }
    
    /// <summary>
    /// 显示主窗口
    /// </summary>
    private void ShowMainWindow()
    {
        _windowManagerService.RestoreFromTray();
    }
    
    /// <summary>
    /// 播放/暂停
    /// </summary>
    private void TogglePlayPause()
    {
        _messagingService.Send(new PlayPauseMessage());
    }
    
    /// <summary>
    /// 上一曲
    /// </summary>
    private void PreviousTrack()
    {
        _messagingService.Send(new PreviousSongMessage());
    }
    
    /// <summary>
    /// 下一曲
    /// </summary>
    private void NextTrack()
    {
        _messagingService.Send(new NextSongMessage());
    }
    
    /// <summary>
    /// 退出应用程序
    /// </summary>
    private void ExitApplication()
    {
        _windowManagerService.CloseApplication();
    }
       
        public void Dispose()
        {
            Cleanup();
        }

        /// <summary>
        /// 清理ViewModel资源
        /// </summary>
        public override void Cleanup()
        {
            // MainViewModel是Singleton，不应该持有Transient ViewModel的引用
            // 也不应该在Cleanup中调用它们的Cleanup，因为这会导致Transient ViewModel无法释放
            // 清理逻辑应该由各个Page在Dispose时自行处理
        }
    }

     
}