using System;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 错误信息
    /// </summary>
    public class ErrorInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Source { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// 警告信息
    /// </summary>
    public class WarningInfo
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// 信息数据
    /// </summary>
    public class InfoData
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// 配置信息
    /// </summary>
    public class ConfigurationInfo
    {
        public string Section { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public object? Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 系统通知信息
    /// </summary>
    public class SystemNotificationInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info"; // Info, Warning, Error
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int Duration { get; set; } = 3000; // 显示时长（毫秒）
    }

    /// <summary>
    /// 服务状态信息
    /// </summary>
    public class ServiceStateInfo
    {
        public string ServiceName { get; set; } = string.Empty;
        public bool IsInitialized { get; set; }
        public bool IsHealthy { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string[] Dependencies { get; set; } = Array.Empty<string>();
    }
}