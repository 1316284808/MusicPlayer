using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放上下文服务接口
    /// 负责管理和切换当前的播放上下文
    /// </summary>
    public interface IPlaybackContextService
    {
        /// <summary>
        /// 当前播放上下文
        /// </summary>
        PlaybackContext CurrentPlaybackContext { get; set; }

        /// <summary>
        /// 设置播放上下文
        /// </summary>
        /// <param name="type">播放上下文类型</param>
        /// <param name="identifier">标识符</param>
        /// <param name="displayName">显示名称</param>
        void SetPlaybackContext(PlaybackContextType type, string identifier, string displayName);

        /// <summary>
        /// 根据类型获取播放上下文提供者
        /// </summary>
        /// <param name="type">播放上下文类型</param>
        /// <returns>播放上下文提供者</returns>
        IPlaybackContextProvider GetProvider(PlaybackContextType type);

        /// <summary>
        /// 注册播放上下文提供者
        /// </summary>
        /// <param name="type">播放上下文类型</param>
        /// <param name="provider">播放上下文提供者</param>
        void RegisterProvider(PlaybackContextType type, IPlaybackContextProvider provider);
    }
}