using System;
using System.Collections.Generic;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放列表服务接口 - 负责歌曲信息的提取和元数据处理
    /// </summary>
    public interface IPlaylistService : IDisposable
    {
        /// <summary>
        /// 从文件夹加载歌曲列表
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>歌曲集合</returns>
        List<Song> LoadSongsFromFolder(string folderPath);
        
        /// <summary>
        /// 提取歌曲信息
        /// </summary>
        /// <param name="filePath">音频文件路径</param>
        /// <returns>歌曲对象，如果提取失败返回null</returns>
        Song? ExtractSongInfo(string filePath);
    }
}