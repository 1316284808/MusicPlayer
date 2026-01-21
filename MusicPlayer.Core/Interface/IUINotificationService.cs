using System;
using System.Threading.Tasks;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// UI通知服务接口 - 抽象UI层通知功能
    /// 服务层通过此接口调用UI通知，避免直接依赖UI框架
    /// </summary>
    public interface IUINotificationService
    {
        /// <summary>
        /// 显示不需要交互的成功通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知内容</param>
        /// <param name="duration">显示持续时间</param>
        void ShowSuccess(string title, string message, TimeSpan? duration = null);

        /// <summary>
        /// 显示不需要交互的错误通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知内容</param>
        /// <param name="duration">显示持续时间</param>
        void ShowError(string title, string message, TimeSpan? duration = null);

        /// <summary>
        /// 显示不需要交互的警告通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知内容</param>
        /// <param name="duration">显示持续时间</param>
        void ShowWarning(string title, string message, TimeSpan? duration = null);

        /// <summary>
        /// 显示不需要交互的信息通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知内容</param>
        /// <param name="duration">显示持续时间</param>
        void ShowInfo(string title, string message, TimeSpan? duration = null);
    }
}