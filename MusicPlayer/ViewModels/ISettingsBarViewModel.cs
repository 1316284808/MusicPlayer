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
        /// 导航到默认列表命令
        /// </summary>
        ICommand NavigateToDefaultListCommand { get; }
        
        /// <summary>
        /// 导航到收藏列表命令
        /// </summary>
        ICommand NavigateToFavoriteListCommand { get; }
        
        /// <summary>
        /// 导航到设置页面命令
        /// </summary>
        ICommand NavigateToSettingsCommand { get; }
        
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
        
        /// <summary>
        /// 默认列表图标状态
        /// </summary>
        bool DefaultListIconState { get; }
        
        /// <summary>
        /// 收藏列表图标状态
        /// </summary>
        bool FavoriteListIconState { get; }
        
        /// <summary>
        /// 设置图标状态
        /// </summary>
        bool SettingsIconState { get; }
        
        /// <summary>
        /// 默认列表是否选中
        /// </summary>
        bool IsDefaultListSelected { get; }
        
        /// <summary>
        /// 收藏列表是否选中
        /// </summary>
        bool IsFavoriteListSelected { get; }
        
        /// <summary>
        /// 设置是否选中
        /// </summary>
        bool IsSettingsSelected { get; }
    }
}