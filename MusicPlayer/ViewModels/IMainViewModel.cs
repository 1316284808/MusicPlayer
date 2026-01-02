using System.Windows.Input;

using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 主视图模型接口
    /// </summary>
    public interface IMainViewModel : IViewModelLifecycle
    {
        /// <summary>
        /// 控制栏视图模型
        /// </summary>
        IControlBarViewModel ControlBarViewModel { get; }

        /// <summary>
        /// 播放列表视图模型
        /// </summary>
        IPlaylistViewModel PlaylistViewModel { get; }

        /// <summary>
        /// 标题栏视图模型
        /// </summary>
        ITitleBarViewModel TitleBarViewModel { get; }

        /// <summary>
        /// 中心内容视图模型
        /// </summary>
        ICenterContentViewModel CenterContentViewModel { get; }
        
        /// <summary>
        /// 显示主窗口命令
        /// </summary>
        ICommand ShowMainWindowCommand { get; }
        
        /// <summary>
        /// 播放/暂停命令
        /// </summary>
        ICommand TogglePlayPauseCommand { get; }
        
        /// <summary>
        /// 上一曲命令
        /// </summary>
        ICommand PreviousTrackCommand { get; }
        
        /// <summary>
        /// 下一曲命令
        /// </summary>
        ICommand NextTrackCommand { get; }
        
        /// <summary>
        /// 退出应用程序命令
        /// </summary>
        ICommand ExitApplicationCommand { get; }
    }
}