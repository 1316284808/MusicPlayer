using System;
using System.Collections.Generic;
using System.Linq;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Services.Providers
{
    /// <summary>
    /// 播放上下文提供者基类
    /// 包含通用的下一曲/上一曲逻辑实现
    /// </summary>
    public abstract class BasePlaybackContextProvider : IPlaybackContextProvider
    {
        /// <summary>
        /// 根据播放上下文获取歌曲列表
        /// 子类必须实现此方法
        /// </summary>
        /// <param name="context">播放上下文</param>
        /// <returns>歌曲列表</returns>
        public abstract List<Song> GetSongsForContext(PlaybackContext context);

        /// <summary>
        /// 根据播放上下文获取下一首歌曲
        /// </summary>
        /// <param name="context">播放上下文</param>
        /// <param name="currentSong">当前歌曲</param>
        /// <param name="playMode">播放模式</param>
        /// <returns>下一首歌曲，如果没有则返回null</returns>
        public virtual Song? GetNextSong(PlaybackContext context, Song currentSong, PlayMode playMode)
        {
            var playlist = GetSongsForContext(context);
            
            if (playlist == null || playlist.Count == 0)
                return null;

            switch (playMode)
            {
                case PlayMode.RepeatOne:
                    return currentSong;

                case PlayMode.Shuffle:
                    return GetRandomSong(playlist, currentSong);

                case PlayMode.RepeatAll:
                default:
                    return GetNextSequentialSong(playlist, currentSong);
            }
        }

        /// <summary>
        /// 根据播放上下文获取上一首歌曲
        /// </summary>
        /// <param name="context">播放上下文</param>
        /// <param name="currentSong">当前歌曲</param>
        /// <param name="playMode">播放模式</param>
        /// <returns>上一首歌曲，如果没有则返回null</returns>
        public virtual Song? GetPreviousSong(PlaybackContext context, Song currentSong, PlayMode playMode)
        {
            var playlist = GetSongsForContext(context);
            
            if (playlist == null || playlist.Count == 0)
                return null;

            switch (playMode)
            {
                case PlayMode.RepeatOne:
                    return currentSong;

                case PlayMode.Shuffle:
                    return GetRandomSong(playlist, currentSong);

                case PlayMode.RepeatAll:
                default:
                    return GetPreviousSequentialSong(playlist, currentSong);
            }
        }

        /// <summary>
        /// 获取顺序播放的下一首歌曲
        /// </summary>
        /// <param name="playlist">播放列表</param>
        /// <param name="currentSong">当前歌曲</param>
        /// <returns>下一首歌曲</returns>
        protected static Song? GetNextSequentialSong(List<Song> playlist, Song currentSong)
        {
            if (playlist.Count == 1)
                return currentSong;

            var currentIndex = playlist.FindIndex(s => s.Id == currentSong.Id);
            if (currentIndex < 0)
                return playlist[0];

            var nextIndex = (currentIndex + 1) % playlist.Count;
            return playlist[nextIndex];
        }

        /// <summary>
        /// 获取顺序播放的上一首歌曲
        /// </summary>
        /// <param name="playlist">播放列表</param>
        /// <param name="currentSong">当前歌曲</param>
        /// <returns>上一首歌曲</returns>
        protected static Song? GetPreviousSequentialSong(List<Song> playlist, Song currentSong)
        {
            if (playlist.Count == 1)
                return currentSong;

            var currentIndex = playlist.FindIndex(s => s.Id == currentSong.Id);
            if (currentIndex < 0)
                return playlist[0];

            var prevIndex = currentIndex - 1;
            if (prevIndex < 0)
                prevIndex = playlist.Count - 1;

            return playlist[prevIndex];
        }

        /// <summary>
        /// 获取随机播放的下一首歌曲
        /// </summary>
        /// <param name="playlist">播放列表</param>
        /// <param name="currentSong">当前歌曲</param>
        /// <returns>随机选择的歌曲</returns>
        protected static Song? GetRandomSong(List<Song> playlist, Song currentSong)
        {
            if (playlist.Count == 1)
                return currentSong;

            var random = new Random();
            Song? nextSong;
            
            // 确保不会连续播放同一首歌（除非只有一首歌）
            do
            {
                var randomIndex = random.Next(playlist.Count);
                nextSong = playlist[randomIndex];
            } while (playlist.Count > 1 && nextSong.Id == currentSong.Id);

            return nextSong;
        }
    }
}