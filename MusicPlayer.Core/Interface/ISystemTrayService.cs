using System;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 系统托盘服务接口 - 抽象系统托盘功能
    /// 解决服务层直接依赖WPF TaskbarNotification的问题
    /// </summary>
    public interface ISystemTrayService
    {
        /// <summary>
        /// 显示系统托盘通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知消息内容</param>
        /// <param name="icon">通知图标类型</param>
        void ShowBalloonTip(string title, string message, BalloonIcon icon = BalloonIcon.Info);
        
        /// <summary>
        /// 隐藏系统托盘通知
        /// </summary>
        void HideBalloonTip();
        
        /// <summary>
        /// 检查系统托盘图标是否可用
        /// </summary>
        bool IsTrayIconAvailable { get; }
        
        /// <summary>
        /// 清理系统托盘资源
        /// </summary>
        void Cleanup();
    }
    
    /// <summary>
    /// 气球图标类型
    /// </summary>
    public enum BalloonIcon
    {
        /// <summary>
        /// 信息图标
        /// </summary>
        Info,
        
        /// <summary>
        /// 警告图标
        /// </summary>
        Warning,
        
        /// <summary>
        /// 错误图标
        /// </summary>
        Error
    }
}