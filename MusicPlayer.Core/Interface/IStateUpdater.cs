using System;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 状态更新器接口，用于批量更新PlayerState状态
    /// </summary>
    public interface IStateUpdater
    {
        /// <summary>
        /// 更新播放状态
        /// </summary>
        IStateUpdater SetPlaying(bool isPlaying);
        
        /// <summary>
        /// 更新音量
        /// </summary>
        IStateUpdater SetVolume(float volume);
        
        /// <summary>
        /// 更新静音状态
        /// </summary>
        IStateUpdater SetMuted(bool isMuted);
        
        /// <summary>
        /// 更新播放位置
        /// </summary>
        IStateUpdater SetPosition(double position);
        
        /// <summary>
        /// 更新播放模式
        /// </summary>
        IStateUpdater SetPlayMode(PlayMode playMode);
        
        /// <summary>
        /// 更新最大位置
        /// </summary>
        IStateUpdater SetMaxPosition(double maxPosition);
        
        /// <summary>
        /// 应用所有更新
        /// </summary>
        void Apply();
    }
}