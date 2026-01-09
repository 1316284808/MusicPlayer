using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 项目路径管理类 - 统一管理所有硬件地址相关的路径
    /// 所有文件路径都应通过此类获取，避免硬编码
    /// </summary>
    public static class Paths
    {
        /// <summary>
        /// 程序执行目录（可执行文件所在目录）
        /// </summary>
        public static string ExecutableDirectory { get; } = 
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;

        // ========== 配置文件路径 ==========
        
        /// <summary>
        /// 应用配置文件路径 
        /// </summary>
        public static string AppSettingPath { get; } = Path.Combine(ExecutableDirectory, "MusicPlayer.db");



        /// <summary>
        /// 播放列表数据库路径 (MusicPlayer.db)
        /// </summary>
        public static string PlaylistDatabasePath { get; } = Path.Combine(ExecutableDirectory, "MusicPlayer.db");

        // ========== 歌词文件路径 ==========
        
        /// <summary>
        /// 获取指定歌曲文件对应的 LRC 歌词文件路径
        /// </summary>
        /// <param name="songFilePath">歌曲文件路径</param>
        /// <returns>LRC 歌词文件路径</returns>
        public static string GetLrcFilePath(string songFilePath)
        {
            return Path.ChangeExtension(songFilePath, ".lrc");
        }
        
        /// <summary>
        /// 获取指定歌曲文件对应的 SRT 歌词文件路径
        /// </summary>
        /// <param name="songFilePath">歌曲文件路径</param>
        /// <returns>SRT 歌词文件路径</returns>
        public static string GetSrtFilePath(string songFilePath)
        {
            return Path.ChangeExtension(songFilePath, ".srt");
        }

     
        

        // ========== 目录路径 ==========
        
        /// <summary>
        /// 日志文件夹路径
        /// </summary>
        public static string LogsDirectory { get; } = Path.Combine(ExecutableDirectory, "logs");
        
        /// <summary>
        /// 封面缓存文件夹路径
        /// </summary>
        public static string AlbumArtCacheDirectory { get; } = Path.Combine(ExecutableDirectory, "cache", "albumarts");
        
        /// <summary>
        /// 获取程序执行目录
        /// </summary>
        /// <returns>程序执行目录路径</returns>
        public static string GetExecutableDirectory()
        {
            return ExecutableDirectory;
        }

        // ========== 工具方法 ==========
        
        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件是否存在</returns>
        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }
        
        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>目录是否存在</returns>
        public static bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }
        
        /// <summary>
        /// 确保目录存在，如不存在则创建
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        public static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
         
    }
}
