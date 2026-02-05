using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Logging
{
    /// <summary>
    /// 文件日志记录器实现
    /// 将日志写入到指定的日志文件
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _filePath;
        private readonly LogLevel _minLogLevel;
        private readonly object _lockObject = new();
        private StreamWriter? _streamWriter;
        private bool _disposed;

        /// <summary>
        /// 创建文件日志记录器
        /// </summary>
        /// <param name="categoryName">日志类别名称</param>
        /// <param name="filePath">日志文件路径</param>
        /// <param name="minLogLevel">最小日志级别</param>
        public FileLogger(string categoryName, string filePath, LogLevel minLogLevel)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _minLogLevel = minLogLevel;
        }

        /// <summary>
        /// 开始作用域
        /// </summary>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>
        /// 检查是否启用指定日志级别
        /// </summary>
        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

        /// <summary>
        /// 记录日志
        /// </summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
            {
                return;
            }

            // 异步写入日志
            _ = WriteLogAsync(logLevel, eventId, message, exception);
        }

        /// <summary>
        /// 异步写入日志到文件
        /// </summary>
        private async Task WriteLogAsync(LogLevel logLevel, EventId eventId, string message, Exception? exception)
        {
            if (_disposed)
            {
                return;
            }

            var logBuilder = new StringBuilder();
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            // 构建日志行：[时间] [级别] [类别] 消息
            logBuilder.Append($"[{timestamp}] [{GetLogLevelShortName(logLevel)}] [{_categoryName}] {message}");

            // 添加异常信息
            if (exception != null)
            {
                logBuilder.AppendLine();
                logBuilder.Append($"Exception: {exception.GetType().Name}: {exception.Message}");
                if (exception.StackTrace != null)
                {
                    logBuilder.AppendLine();
                    logBuilder.Append(exception.StackTrace);
                }
            }

            logBuilder.AppendLine();
            var logLine = logBuilder.ToString();

            lock (_lockObject)
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    EnsureStreamWriter();
                    _streamWriter?.WriteLine(logLine);
                    _streamWriter?.Flush();
                }
                catch (Exception ex)
                {
                    // 如果写入失败，输出到调试窗口
                    System.Diagnostics.Debug.WriteLine($"[FileLogger] 写入日志失败: {ex.Message}");
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 确保 StreamWriter 已创建
        /// </summary>
        private void EnsureStreamWriter()
        {
            if (_streamWriter == null)
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _streamWriter = new StreamWriter(_filePath, append: true, encoding: Encoding.UTF8);
            }
        }

        /// <summary>
        /// 获取日志级别的短名称
        /// </summary>
        private static string GetLogLevelShortName(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "TRCE",
                LogLevel.Debug => "DBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERRO",
                LogLevel.Critical => "CRIT",
                _ => logLevel.ToString().ToUpperInvariant()
            };
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lockObject)
                {
                    _streamWriter?.Dispose();
                    _streamWriter = null;
                }
                _disposed = true;
            }
        }
    }
}
