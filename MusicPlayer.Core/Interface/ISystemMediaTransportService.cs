using System;
using System.Threading.Tasks;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 系统媒体传输控制服务接口
    /// 负责 WPF 应用与 Windows 系统 SMTC 的集成，提供媒体控制功能
    /// </summary>
    public interface ISystemMediaTransportService : IDisposable
    {
        /// <summary>
        /// 初始化 SMTC 服务
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 更新媒体信息（标题、艺术家、专辑、封面等）
        /// </summary>
        /// <param name="song">当前播放的歌曲</param>
        Task UpdateMediaInfoAsync(Song? song);

        /// <summary>
        /// 更新播放状态
        /// </summary>
        /// <param name="isPlaying">是否正在播放</param>
        Task UpdatePlaybackStatusAsync(bool isPlaying);

        /// <summary>
        /// 启用或禁用控制按钮
        /// </summary>
        /// <param name="isPlayEnabled">播放按钮是否可用</param>
        /// <param name="isPauseEnabled">暂停按钮是否可用</param>
        /// <param name="isNextEnabled">下一首按钮是否可用</param>
        /// <param name="isPreviousEnabled">上一首按钮是否可用</param>
        void EnableControls(bool isPlayEnabled, bool isPauseEnabled, bool isNextEnabled, bool isPreviousEnabled);

        /// <summary>
        /// 媒体控制事件
        /// </summary>
        event EventHandler? PlayOrPauseRequested;
        event EventHandler? NextRequested;
        event EventHandler? PreviousRequested;
    }
}