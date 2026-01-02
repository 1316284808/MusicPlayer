using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using Microsoft.Extensions.Logging;
using MusicPlayer.Services.Messages;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 错误处理和日志记录服务
    /// 集中处理应用中的错误、警告和信息消息
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService, IDisposable
    {
        private readonly IMessagingService _messagingService;
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly Dictionary<string, int> _errorCounts = new();
        private readonly List<ErrorInfo> _errorHistory = new();
        private readonly object _lockObject = new();
        private bool _disposed = false;

        // 配置选项
        public int MaxErrorHistoryCount { get; set; } = 100;
        public bool EnableErrorNotifications { get; set; } = true;
        public bool EnableDetailedLogging { get; set; } = true;

        public ErrorHandlingService(IMessagingService messagingService, ILogger<ErrorHandlingService> logger)
        {
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 注册错误消息处理器
            _messagingService.Register<ErrorMessage>(this, OnErrorMessageReceived);
        }

        /// <summary>
        /// 处理错误消息
        /// </summary>
        private void OnErrorMessageReceived(object recipient, ErrorMessage message)
        {
            try
            {
                var errorInfo = message.Value;
                LogError(errorInfo);
                TrackError(errorInfo);
                SendNotification(errorInfo);
            }
            catch (Exception ex)
            {
                // 避免错误处理中产生异常导致递归
                System.Diagnostics.Debug.WriteLine($"Error in error handler: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        private void LogError(ErrorInfo errorInfo)
        {
            if (EnableDetailedLogging)
            {
                _logger.LogError(errorInfo.Exception, 
                    "[{Code}] {Message} - Source: {Source} - Details: {Details}",
                    errorInfo.Code,
                    errorInfo.Message,
                    errorInfo.Source,
                    errorInfo.Details);
            }
            else
            {
                _logger.LogError("[{Code}] {Message} - Source: {Source}",
                    errorInfo.Code,
                    errorInfo.Message,
                    errorInfo.Source);
            }
        }

        /// <summary>
        /// 追踪错误统计
        /// </summary>
        private void TrackError(ErrorInfo errorInfo)
        {
            lock (_lockObject)
            {
                // 更新错误计数
                var key = $"{errorInfo.Code}:{errorInfo.Source}";
                if (_errorCounts.ContainsKey(key))
                {
                    _errorCounts[key]++;
                }
                else
                {
                    _errorCounts[key] = 1;
                }

                // 添加到历史记录
                _errorHistory.Add(errorInfo);

                // 限制历史记录数量
                if (_errorHistory.Count > MaxErrorHistoryCount)
                {
                    _errorHistory.RemoveAt(0);
                }

                // 检查是否为重复错误
                var count = _errorCounts[key];
                if (count > 5)
                {
                    // 同一错误频繁发生，发送特殊通知
                    var notificationInfo = new SystemNotificationInfo
                    {
                        Title = "重复错误警告",
                        Message = $"错误 '{errorInfo.Message}' 已发生 {count} 次",
                        Type = "Warning",
                        Duration = 5000
                    };
                    _messagingService.Send(new SystemNotificationMessage(notificationInfo));
                }
            }
        }

        /// <summary>
        /// 发送错误通知
        /// </summary>
        private void SendNotification(ErrorInfo errorInfo)
        {
            if (!EnableErrorNotifications) return;

            // 根据错误严重程度决定是否发送通知
            var notificationType = GetNotificationType(errorInfo);
            
            var notificationInfo = new SystemNotificationInfo
            {
                Title = GetNotificationTitle(errorInfo),
                Message = errorInfo.Message,
                Type = notificationType,
                Duration = GetNotificationDuration(errorInfo)
            };

            _messagingService.Send(new SystemNotificationMessage(notificationInfo));
        }

        /// <summary>
        /// 获取通知类型
        /// </summary>
        private string GetNotificationType(ErrorInfo errorInfo)
        {
            return errorInfo.Code switch
            {
                "CRITICAL_ERROR" or "FATAL_ERROR" => "Error",
                "WARNING" or "REPEATED_ERROR" => "Warning",
                "INFO" => "Info",
                _ => "Error"
            };
        }

        /// <summary>
        /// 获取通知标题
        /// </summary>
        private string GetNotificationTitle(ErrorInfo errorInfo)
        {
            return errorInfo.Code switch
            {
                "PLAYER_ERROR" => "播放器错误",
                "PLAYLIST_ERROR" => "播放列表错误",
                "SYSTEM_HANDLER_ERROR" => "系统错误",
                "UI_ERROR" => "界面错误",
                "FILE_ERROR" => "文件错误",
                "NETWORK_ERROR" => "网络错误",
                "CRITICAL_ERROR" => "严重错误",
                "REPEATED_ERROR" => "重复错误",
                _ => "应用错误"
            };
        }

        /// <summary>
        /// 获取通知显示时长
        /// </summary>
        private int GetNotificationDuration(ErrorInfo errorInfo)
        {
            return errorInfo.Code switch
            {
                "CRITICAL_ERROR" => 8000,
                "WARNING" or "REPEATED_ERROR" => 6000,
                "INFO" => 3000,
                _ => 4000
            };
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        public void HandleException(Exception exception, string source = "Unknown", string? customMessage = null)
        {
            var errorInfo = new ErrorInfo
            {
                Code = GetExceptionCode(exception),
                Message = customMessage ?? exception.Message,
                Details = exception.StackTrace ?? string.Empty,
                Source = source,
                Exception = exception
            };

            _messagingService.Send(new ErrorMessage(errorInfo));
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        public void LogWarning(string message, string source = "Unknown", string? details = null)
        {
            var warningInfo = new WarningInfo
            {
                Message = message,
                Source = source,
                Timestamp = DateTime.Now
            };

            // 使用Error级别记录警告，确保写入到日志文件
            _logger.LogError("[WARNING] [{Source}] {Message} - Details: {Details}", source, message, details);
            _messagingService.Send(new WarningMessage(warningInfo));
        }

        /// <summary>
        /// 记录信息
        /// </summary>
        public void LogInfo(string message, string source = "Unknown")
        {
            var infoData = new InfoData
            {
                Message = message,
                Source = source,
                Timestamp = DateTime.Now
            };

            // 只在关键信息时记录到日志文件，使用Error级别
            if (source.Contains("ApplicationInitialization") || 
                source.Contains("ApplicationShutdown") ||
                message.Contains("started") ||
                message.Contains("shutdown"))
            {
                _logger.LogError("[INFO] [{Source}] {Message}", source, message);
            }
            
            _messagingService.Send(new InfoMessage(infoData));
        }

        /// <summary>
        /// 根据异常类型获取错误代码
        /// </summary>
        private string GetExceptionCode(Exception exception)
        {
            // 处理继承关系，先检查具体类型
            if (exception is FileNotFoundException)
                return "FILE_NOT_FOUND";
            if (exception is DirectoryNotFoundException)
                return "DIRECTORY_NOT_FOUND";
            if (exception is ArgumentNullException)
                return "NULL_ARGUMENT";
            if (exception is ArgumentException)
                return "INVALID_ARGUMENT";
            if (exception is UnauthorizedAccessException)
                return "ACCESS_DENIED";
            if (exception is IOException)
                return "IO_ERROR";
            if (exception is InvalidOperationException)
                return "INVALID_OPERATION";
            if (exception is TimeoutException)
                return "TIMEOUT_ERROR";
            if (exception is TaskCanceledException)
                return "TASK_CANCELLED";
            if (exception is OutOfMemoryException)
                return "OUT_OF_MEMORY";
            if (exception is StackOverflowException)
                return "STACK_OVERFLOW";
            
            return "UNKNOWN_ERROR";
        }

        /// <summary>
        /// 获取错误统计信息
        /// </summary>
        public Dictionary<string, int> GetErrorStatistics()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, int>(_errorCounts);
            }
        }

        /// <summary>
        /// 获取错误历史记录
        /// </summary>
        public List<ErrorInfo> GetErrorHistory(int? maxCount = null)
        {
            lock (_lockObject)
            {
                if (maxCount.HasValue && maxCount.Value < _errorHistory.Count)
                {
                    return _errorHistory.GetRange(_errorHistory.Count - maxCount.Value, maxCount.Value);
                }
                return new List<ErrorInfo>(_errorHistory);
            }
        }

        /// <summary>
        /// 清除错误统计和历史记录
        /// </summary>
        public void ClearErrorHistory()
        {
            lock (_lockObject)
            {
                _errorCounts.Clear();
                _errorHistory.Clear();
            }

            LogInfo("错误历史记录已清除", "ErrorHandlingService");
        }

        /// <summary>
        /// 生成错误报告
        /// </summary>
        public string GenerateErrorReport()
        {
            lock (_lockObject)
            {
                var report = new System.Text.StringBuilder();
                report.AppendLine("=== MusicPlayer 错误报告 ===");
                report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"总错误数: {_errorHistory.Count}");
                report.AppendLine();

                report.AppendLine("错误统计:");
                foreach (var kvp in _errorCounts)
                {
                    report.AppendLine($"  {kvp.Key}: {kvp.Value} 次");
                }
                report.AppendLine();

                report.AppendLine("最近的错误:");
                var recentErrors = GetErrorHistory(10);
                foreach (var error in recentErrors)
                {
                    report.AppendLine($"  [{error.Timestamp:HH:mm:ss}] {error.Code} - {error.Message}");
                }

                return report.ToString();
            }
        }

        /// <summary>
        /// 保存错误报告到文件
        /// </summary>
        public async Task SaveErrorReportAsync(string? filePath = null)
        {
            try
            {
                filePath ??= $"ErrorReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                
                var report = GenerateErrorReport();
                await File.WriteAllTextAsync(filePath, report);
                
                LogInfo($"错误报告已保存到: {filePath}", "ErrorHandlingService");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save error report: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _messagingService.Unregister(this);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 错误处理服务接口
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// 处理异常
        /// </summary>
        void HandleException(Exception exception, string source = "Unknown", string? customMessage = null);

        /// <summary>
        /// 记录警告
        /// </summary>
        void LogWarning(string message, string source = "Unknown", string? details = null);

        /// <summary>
        /// 记录信息
        /// </summary>
        void LogInfo(string message, string source = "Unknown");

        /// <summary>
        /// 获取错误统计信息
        /// </summary>
        Dictionary<string, int> GetErrorStatistics();

        /// <summary>
        /// 获取错误历史记录
        /// </summary>
        List<ErrorInfo> GetErrorHistory(int? maxCount = null);

        /// <summary>
        /// 清除错误历史记录
        /// </summary>
        void ClearErrorHistory();

        /// <summary>
        /// 生成错误报告
        /// </summary>
        string GenerateErrorReport();

        /// <summary>
        /// 保存错误报告到文件
        /// </summary>
        Task SaveErrorReportAsync(string? filePath = null);
    }
}