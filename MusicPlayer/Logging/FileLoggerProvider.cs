using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace MusicPlayer.Logging
{
    /// <summary>
    /// 文件日志提供程序
    /// 管理 FileLogger 实例的生命周期
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logsDirectory;
        private readonly string _fileName;
        private readonly LogLevel _minLogLevel;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
        private bool _disposed;

        /// <summary>
        /// 创建文件日志提供程序
        /// </summary>
        /// <param name="logsDirectory">日志目录路径</param>
        /// <param name="fileName">日志文件名</param>
        /// <param name="minLogLevel">最小日志级别</param>
        public FileLoggerProvider(string logsDirectory, string fileName, LogLevel minLogLevel)
        {
            _logsDirectory = logsDirectory ?? throw new ArgumentNullException(nameof(logsDirectory));
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _minLogLevel = minLogLevel;

            // 确保日志目录存在
            EnsureDirectoryExists();
        }

        /// <summary>
        /// 创建日志记录器实例
        /// </summary>
        /// <param name="categoryName">日志类别名称</param>
        /// <returns>ILogger 实例</returns>
        public ILogger CreateLogger(string categoryName)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FileLoggerProvider));
            }

            return _loggers.GetOrAdd(categoryName, name =>
            {
                var filePath = Path.Combine(_logsDirectory, _fileName);
                return new FileLogger(name, filePath, _minLogLevel);
            });
        }

        /// <summary>
        /// 确保日志目录存在
        /// </summary>
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_logsDirectory))
            {
                Directory.CreateDirectory(_logsDirectory);
            }
        }

        /// <summary>
        /// 释放所有日志记录器资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                foreach (var logger in _loggers.Values)
                {
                    logger.Dispose();
                }
                _loggers.Clear();
            }
        }
    }
}
