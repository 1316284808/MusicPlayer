using System.Collections.Generic;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放上下文提供者接口
    /// 负责根据特定的播放上下文获取歌曲列表和下一曲/上一曲逻辑
    /// </summary>
    public interface IPlaybackContextProvider
    {
        /// <summary>
        /// 根据播放上下文获取歌曲列表
        /// </summary>
        /// <param name="context">播放上下文</param>
        /// <returns>歌曲列表</returns>
        List<Song> GetSongsForContext(PlaybackContext context);

        /// <summary>
        /// 根据播放上下文获取下一首歌曲
        /// </summary>
        /// <param name="context">播放上下文</param>
        /// <param name="currentSong">当前歌曲</param>
        /// <param name="playMode">播放模式</param>
        /// <returns>下一首歌曲，如果没有则返回null</returns>
        Song? GetNextSong(PlaybackContext context, Song currentSong, PlayMode playMode);

        /// <summary>
        /// 根据播放上下文获取上一首歌曲
        /// </summary>
        /// <param name="context">播放上下文</param>
        /// <param name="currentSong">当前歌曲</param>
        /// <param name="playMode">播放模式</param>
        /// <returns>上一首歌曲，如果没有则返回null</returns>
        Song? GetPreviousSong(PlaybackContext context, Song currentSong, PlayMode playMode);
    }
}