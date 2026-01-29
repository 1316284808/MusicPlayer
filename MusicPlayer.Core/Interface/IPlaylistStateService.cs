using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放列表状态服务接口
    /// 职责：管理播放列表相关的状态（当前歌曲、排序规则等）
    /// </summary>
    public interface IPlaylistStateService
    {
        /// <summary>
        /// 当前播放歌曲
        /// </summary>
        Song? CurrentSong { get; set; }

        /// <summary>
        /// 当前排序规则
        /// </summary>
        SortRule CurrentSortRule { get; set; }

        /// <summary>
        /// 设置当前歌曲（不发送消息，用于初始化恢复）
        /// </summary>
        /// <param name="song">要设置的歌曲</param>
        void SetCurrentSongWithoutNotification(Song? song);
    }
}