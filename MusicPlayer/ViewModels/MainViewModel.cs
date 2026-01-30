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
      
        public IControlBarViewModel ControlBarViewModel { get; private set; }
        public ITitleBarViewModel TitleBarViewModel { get; private set; }
        public IPlaylistViewModel PlaylistViewModel { get; private set; }
        public ICenterContentViewModel CenterContentViewModel { get; private set; }

    public MainViewModel(
        IControlBarViewModel controlBarViewModel,
        ITitleBarViewModel titleBarViewModel,
        ICenterContentViewModel centerContentViewModel,
        IPlaylistViewModel playlistViewModel,
        IServiceCoordinator serviceCoordinator,
        WindowManagerService windowManagerService,
        IMessagingService messagingService)
    {
        // 注入服务
        _serviceCoordinator = serviceCoordinator ?? throw new ArgumentNullException(nameof(serviceCoordinator));
        _windowManagerService = windowManagerService ?? throw new ArgumentNullException(nameof(windowManagerService));
        _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
        
        // 使用依赖注入的子ViewModel
        ControlBarViewModel = controlBarViewModel ?? throw new ArgumentNullException(nameof(controlBarViewModel));
        TitleBarViewModel = titleBarViewModel ?? throw new ArgumentNullException(nameof(titleBarViewModel));
        CenterContentViewModel = centerContentViewModel ?? throw new ArgumentNullException(nameof(centerContentViewModel));
        PlaylistViewModel = playlistViewModel ?? throw new ArgumentNullException(nameof(playlistViewModel));
      
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
            // 清理子ViewModel
            if (ControlBarViewModel is ObservableObject controlBarViewModel)
            {
                controlBarViewModel.Cleanup();
            }
            
            if (TitleBarViewModel is ObservableObject titleBarViewModel)
            {
                titleBarViewModel.Cleanup();
            }
            
            if (PlaylistViewModel is ObservableObject playlistViewModel)
            {
                playlistViewModel.Cleanup();
            }
            
            if (CenterContentViewModel is ObservableObject centerContentViewModel)
            {
                centerContentViewModel.Cleanup();
            }
        }
    }

     
}