using System;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 系统通知服务接口
    /// 提供统一的跨平台通知功能，支持不同类型通知的显示
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// 初始化通知系统
        /// </summary>
        void Initialize();

        /// <summary>
        /// 显示信息类型通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        void ShowInfo(string message);

        /// <summary>
        /// 显示成功类型通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        void ShowSuccess(string message);

        /// <summary>
        /// 显示警告类型通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        void ShowWarning(string message);

        /// <summary>
        /// 显示错误类型通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        void ShowError(string message);

        /// <summary>
        /// 显示自定义通知
        /// </summary>
        /// <param name="title">通知标题</param>
        /// <param name="message">通知消息内容</param>
        /// <param name="notificationType">通知类型</param>
        void ShowNotification(string title, string message, NotificationType notificationType = NotificationType.Info);

        /// <summary>
        /// 清理通知资源
        /// </summary>
        void Cleanup();
        
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
    }

    /// <summary>
    /// 通知类型枚举
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// 信息通知
        /// </summary>
        Info,
        
        /// <summary>
        /// 成功通知
        /// </summary>
        Success,
        
        /// <summary>
        /// 警告通知
        /// </summary>
        Warning,
        
        /// <summary>
        /// 错误通知
        /// </summary>
        Error
    }

    /// <summary>
    /// 通知服务扩展方法
    /// </summary>
    public static class NotificationServiceExtensions
    {
        /// <summary>
        /// 显示播放状态变更通知
        /// </summary>
        /// <param name="notificationService">通知服务</param>
        /// <param name="isPlaying">是否正在播放</param>
        /// <param name="trackName">曲目名称</param>
        public static void ShowPlaybackStatus(this INotificationService notificationService, bool isPlaying, string trackName)
        {
            var status = isPlaying ? "开始播放" : "暂停播放";
            var message = $"{status}: {trackName}";
            
            if (isPlaying)
            {
                notificationService.ShowSuccess(message);
            }
            else
            {
                notificationService.ShowInfo(message);
            }
        }

        /// <summary>
        /// 显示曲目切换通知
        /// </summary>
        /// <param name="notificationService">通知服务</param>
        /// <param name="previousTrack">上一个曲目</param>
        /// <param name="nextTrack">下一个曲目</param>
        public static void ShowTrackChanged(this INotificationService notificationService, string previousTrack, string nextTrack)
        {
            var message = $"切换到: {nextTrack}";
            notificationService.ShowInfo(message);
        }

        /// <summary>
        /// 显示播放列表操作通知
        /// </summary>
        /// <param name="notificationService">通知服务</param>
        /// <param name="operation">操作类型</param>
        /// <param name="playlistName">播放列表名称</param>
        public static void ShowPlaylistOperation(this INotificationService notificationService, string operation, string playlistName)
        {
            var message = $"{operation}播放列表: {playlistName}";
            notificationService.ShowSuccess(message);
        }
    }
}