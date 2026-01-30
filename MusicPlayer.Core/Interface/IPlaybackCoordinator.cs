using System;
using System.Threading.Tasks;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放协调器接口 - 封装播放相关的业务逻辑
    /// </summary>
    public interface IPlaybackCoordinator
    {
        /// <summary>
        /// 是否正在播放
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// 当前播放的歌曲
        /// </summary>
        Song? CurrentSong { get; }

        /// <summary>
        /// 当前播放位置（秒）
        /// </summary>
        double CurrentPosition { get; }

        /// <summary>
        /// 音量（0.0 - 1.0）
        /// </summary>
        float Volume { get; }

        /// <summary>
        /// 播放指定歌曲
        /// </summary>
        /// <param name="song">要播放的歌曲</param>
        /// <returns>异步任务</returns>
        Task PlayAsync(Song song);

        /// <summary>
        /// 暂停播放
        /// </summary>
        /// <returns>异步任务</returns>
        Task PauseAsync();

        /// <summary>
        /// 恢复播放
        /// </summary>
        /// <returns>异步任务</returns>
        Task ResumeAsync();

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <returns>异步任务</returns>
        Task StopAsync();

        /// <summary>
        /// 跳转到指定位置
        /// </summary>
        /// <param name="position">目标位置（秒）</param>
        /// <returns>异步任务</returns>
        Task SeekAsync(double position);

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="volume">音量值（0.0 - 1.0）</param>
        /// <returns>异步任务</returns>
        Task SetVolumeAsync(float volume);
    }

    /// <summary>
    /// 播放列表协调器接口 - 封装播放列表相关的业务逻辑
    /// </summary>
    public interface IPlaylistCoordinator
    {
        /// <summary>
        /// 加载播放列表数据
        /// </summary>
        /// <returns>异步任务</returns>
        Task LoadPlaylistAsync();

        /// <summary>
        /// 添加歌曲到播放列表
        /// </summary>
        /// <param name="filePaths">歌曲文件路径集合</param>
        /// <returns>异步任务</returns>
        Task AddSongsAsync(System.Collections.Generic.IEnumerable<string> filePaths);

        /// <summary>
        /// 从播放列表移除歌曲
        /// </summary>
        /// <param name="songs">要移除的歌曲集合</param>
        /// <returns>异步任务</returns>
        Task RemoveSongsAsync(System.Collections.Generic.IEnumerable<Song> songs);

        /// <summary>
        /// 切换排序规则
        /// </summary>
        /// <param name="newRule">新的排序规则</param>
        /// <returns>异步任务，返回新的排序规则</returns>
        Task<MusicPlayer.Core.Enums.SortRule> ChangeSortRuleAsync(MusicPlayer.Core.Enums.SortRule newRule);
    }
}
