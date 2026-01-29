using System;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 播放状态服务接口 - 作为播放状态的唯一可信源
    /// 采用统一的可读写属性设计，避免接口设计不一致
    /// </summary>
    public interface IPlayerStateService : System.ComponentModel.INotifyPropertyChanged
    {
        /// <summary>
        /// 当前歌曲
        /// </summary>
        Song? CurrentSong { get; }
        
        /// <summary>
        /// 播放状态
        /// </summary>
        bool IsPlaying { get; set; }
        
        /// <summary>
        /// 静音状态
        /// </summary>
        bool IsMuted { get; set; }
        
        /// <summary>
        /// 音量 (0.0 - 1.0)
        /// </summary>
        float Volume { get; set; }
        
        /// <summary>
        /// 当前播放位置（秒）
        /// </summary>
        double CurrentPosition { get; set; }
    
        /// <summary>
        /// 用户拖动时设置位置
        /// </summary>
        void SetPositionByUser(double position);
        
        /// <summary>
        /// 最大播放位置（秒）
        /// </summary>
        double MaxPosition { get; set; }
        
        /// <summary>
        /// 播放模式
        /// </summary>
        PlayMode CurrentPlayMode { get; set; }
        
        /// <summary>
        /// 音频引擎
        /// </summary>
        AudioEngine CurrentAudioEngine { get; set; }
        
        /// <summary>
        /// 频谱数据
        /// </summary>
        float[] SpectrumData { get; }
        
        /// <summary>
        /// 当前播放上下文
        /// </summary>
        PlaybackContext CurrentPlaybackContext { get; set; }
        
        /// <summary>
        /// 更新当前歌曲
        /// </summary>
        void UpdateCurrentSong(Song? song);
        
        /// <summary>
        /// 更新频谱数据
        /// </summary>
        void UpdateSpectrumData(float[] spectrumData);
        
        /// <summary>
        /// 切换播放/暂停状态
        /// </summary>
        void TogglePlayPause();
        
        /// <summary>
        /// 切换播放模式
        /// </summary>
        void TogglePlayMode();
        
        /// <summary>
        /// 停止播放
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 从配置恢复状态（应用启动时调用）
        /// </summary>
        void RestoreFromConfiguration(PlayerConfiguration configuration);
        
        /// <summary>
        /// 同步状态到配置（内存同步，不持久化）
        /// </summary>
        void SyncToConfiguration(PlayerConfiguration configuration);
        
        /// <summary>
        /// 批量更新状态（避免多次触发事件）
        /// </summary>
        void UpdateStates(Action<IStateUpdater> updateAction);
        
        /// <summary>
        /// 检查是否可以播放
        /// </summary>
        bool CanPlay();
        
        /// <summary>
        /// 检查是否可以暂停
        /// </summary>
        bool CanPause();
        
        /// <summary>
        /// 事件：状态变化时触发，提供属性名和新值
        /// </summary>
        event Action<string, object>? StateChanged;
    }
}