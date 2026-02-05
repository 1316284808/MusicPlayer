using Microsoft.Extensions.Logging;
using System;

namespace MusicPlayer.Logging
{
    /// <summary>
    /// 文件日志扩展方法
    /// </summary>
    public static class FileLoggerExtensions
    {
        /// <summary>
        /// 添加文件日志提供程序
        /// </summary>
        /// <param name="builder">日志构建器</param>
        /// <param name="logsDirectory">日志目录路径</param>
        /// <param name="fileName">日志文件名</param>
        /// <param name="minLogLevel">最小日志级别，默认为 Error</param>
        /// <returns>日志构建器</returns>
        public static ILoggingBuilder AddFile(
            this ILoggingBuilder builder,
            string logsDirectory,
            string fileName,
            LogLevel minLogLevel = LogLevel.Error)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(logsDirectory))
            {
                throw new ArgumentException("日志目录路径不能为空", nameof(logsDirectory));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("日志文件名不能为空", nameof(fileName));
            }

            builder.AddProvider(new FileLoggerProvider(logsDirectory, fileName, minLogLevel));
            return builder;
        }

        /// <summary>
        /// 添加文件日志提供程序，使用启动时间作为文件名
        /// </summary>
        /// <param name="builder">日志构建器</param>
        /// <param name="logsDirectory">日志目录路径</param>
        /// <param name="minLogLevel">最小日志级别，默认为 Error</param>
        /// <returns>日志构建器</returns>
        public static ILoggingBuilder AddFileWithTimestamp(
            this ILoggingBuilder builder,
            string logsDirectory,
            LogLevel minLogLevel = LogLevel.Error)
        {
            // 生成文件名：yyyy-MM-dd-HHmmss.log
            var fileName = $"{DateTime.Now:yyyy-MM-dd-HHmmss}.log";
            return builder.AddFile(logsDirectory, fileName, minLogLevel);
        }
    }
}
