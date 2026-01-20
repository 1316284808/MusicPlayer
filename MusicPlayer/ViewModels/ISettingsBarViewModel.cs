using System.Windows.Input;

using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 设置栏视图模型接口
    /// </summary>
    public interface ISettingsBarViewModel : IViewModelLifecycle
    {
        /// <summary>
        /// 控制其他按钮是否可见
        /// </summary>
        bool AreOtherButtonsVisible { get; }

        /// <summary>
        /// 当前过滤模式：0=全部，1=收藏
        /// </summary>
        int CurrentFilterMode { get; }
        
        /// <summary>
    /// 切换按钮可见性命令
    /// </summary>
    ICommand ToggleButtonsVisibilityCommand { get; }
    
    /// <summary>
    /// 鼠标进入命令
    /// </summary>
    ICommand MouseEnterCommand { get; }
    
    /// <summary>
    /// 鼠标离开命令
    /// </summary>
    ICommand MouseLeaveCommand { get; }
    
    /// <summary>
    /// 控制所有按钮是否可见
    /// </summary>
    bool AreAllButtonsVisible { get; }
    }
}