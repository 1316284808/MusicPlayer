using System.Windows.Input;

using MusicPlayer.Config;

namespace MusicPlayer.ViewModels
{
    /// <summary>
    /// 标题栏视图模型接口
    /// </summary>
    public interface ITitleBarViewModel : IViewModelLifecycle
    {
        /// <summary>
        /// 窗口标题
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        ICommand CloseWindowCommand { get; }

        /// <summary>
        /// 最小化窗口命令
        /// </summary>
        ICommand MinimizeWindowCommand { get; }

        /// <summary>
        /// 最大化/还原窗口命令
        /// </summary>
        ICommand MaximizeRestoreWindowCommand { get; }

        /// <summary>
        /// 最大化/还原窗口命令（用于XAML绑定的别名）
        /// </summary>
        ICommand MaximizeOrRestoreCommand { get; }
    }
}