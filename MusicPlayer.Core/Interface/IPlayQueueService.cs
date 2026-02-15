using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放队列服务接口 - 管理插队播放的歌曲队列
    /// 支持将歌曲添加到下一首播放位置
    /// </summary>
    public interface IPlayQueueService
    {
        /// <summary>
        /// 将歌曲添加到插队队列
        /// </summary>
        /// <param name="song">要插队的歌曲</param>
        void EnqueueSong(Song song);

        /// <summary>
        /// 从插队队列取出下一首歌曲（移除并返回）
        /// </summary>
        /// <returns>插队队列中的下一首歌曲，如果队列为空则返回null</returns>
        Song? DequeueSong();

        /// <summary>
        /// 查看插队队列中的下一首歌曲（不移除）
        /// </summary>
        /// <returns>插队队列中的下一首歌曲，如果队列为空则返回null</returns>
        Song? PeekSong();

        /// <summary>
        /// 检查插队队列是否有歌曲
        /// </summary>
        bool HasSongs { get; }

        /// <summary>
        /// 获取插队队列中的歌曲数量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 清空插队队列
        /// </summary>
        void Clear();
    }
}
