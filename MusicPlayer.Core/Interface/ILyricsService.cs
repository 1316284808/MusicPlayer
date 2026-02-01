using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 歌词服务接口 - 负责歌词文件的加载和解析
    /// </summary>
    public interface ILyricsService : IDisposable
    {
        /// <summary>
        /// 加载歌词文件
        /// </summary>
        /// <param name="filePath">音频文件路径</param>
        /// <returns>歌词行集合</returns>
        List<LyricLine> LoadLyrics(string filePath);
        
        /// <summary>
        /// 异步加载歌词文件
        /// </summary>
        /// <param name="filePath">音频文件路径</param>
        /// <returns>歌词行集合</returns>
        Task<List<LyricLine>> LoadLyricsAsync(string filePath);
    }
}