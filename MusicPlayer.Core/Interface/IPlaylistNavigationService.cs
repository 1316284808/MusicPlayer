using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放列表导航服务接口
    /// 职责：处理播放列表中的导航逻辑（上一首、下一首）
    /// </summary>
    public interface IPlaylistNavigationService
    {
        /// <summary>
        /// 获取下一首歌曲
        /// </summary>
        /// <param name="playMode">播放模式</param>
        /// <param name="currentSong">当前歌曲（可选，如果为null则使用内部状态）</param>
        /// <returns>下一首歌曲，如果没有则返回null</returns>
        Song? GetNextSong(PlayMode playMode = PlayMode.RepeatAll, Song? currentSong = null);

        /// <summary>
        /// 获取上一首歌曲
        /// </summary>
        /// <param name="playMode">播放模式</param>
        /// <param name="currentSong">当前歌曲（可选，如果为null则使用内部状态）</param>
        /// <returns>上一首歌曲，如果没有则返回null</returns>
        Song? GetPreviousSong(PlayMode playMode = PlayMode.RepeatAll, Song? currentSong = null);

        /// <summary>
        /// 获取歌曲在列表中的索引
        /// </summary>
        /// <param name="song">要查找的歌曲</param>
        /// <returns>歌曲索引，如果找不到返回-1</returns>
        int GetSongIndex(Song song);
    }
}