using System.Collections.Concurrent;
using System.Diagnostics;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services
{
    /// <summary>
    /// 播放队列服务实现 - 管理插队播放的歌曲队列
    /// 使用 ConcurrentQueue 保证线程安全
    /// </summary>
    public class PlayQueueService : IPlayQueueService
    {
        private readonly ConcurrentQueue<Song> _queue = new();

        /// <summary>
        /// 将歌曲添加到插队队列
        /// </summary>
        public void EnqueueSong(Song song)
        {
            if (song == null)
            {
                Debug.WriteLine("PlayQueueService: 尝试添加空歌曲到队列，操作已忽略");
                return;
            }

            _queue.Enqueue(song);
            Debug.WriteLine($"PlayQueueService: 歌曲已添加到插队队列 - {song.Title}，当前队列长度: {_queue.Count}");
        }

        /// <summary>
        /// 从插队队列取出下一首歌曲（移除并返回）
        /// </summary>
        public Song? DequeueSong()
        {
            if (_queue.TryDequeue(out var song))
            {
                Debug.WriteLine($"PlayQueueService: 从队列取出歌曲 - {song.Title}，剩余队列长度: {_queue.Count}");
                return song;
            }

            Debug.WriteLine("PlayQueueService: 队列为空，无法取出歌曲");
            return null;
        }

        /// <summary>
        /// 查看插队队列中的下一首歌曲（不移除）
        /// </summary>
        public Song? PeekSong()
        {
            if (_queue.TryPeek(out var song))
            {
                return song;
            }

            return null;
        }

        /// <summary>
        /// 检查插队队列是否有歌曲
        /// </summary>
        public bool HasSongs => !_queue.IsEmpty;

        /// <summary>
        /// 获取插队队列中的歌曲数量
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 清空插队队列
        /// </summary>
        public void Clear()
        {
            var count = _queue.Count;
            while (_queue.TryDequeue(out _)) { }
            Debug.WriteLine($"PlayQueueService: 队列已清空，清除了 {count} 首歌曲");
        }
    }
}
