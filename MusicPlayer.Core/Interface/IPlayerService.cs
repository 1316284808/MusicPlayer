using System;
using MusicPlayer.Core.Models;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Core.Interface
{
    /// <summary>
    /// 音频播放器服务接口 - 负责音频设备管理、频谱分析等核心技术功能
    /// 不再直接暴露播放控制方法，所有控制通过消息机制进行
    /// </summary>
    public interface IPlayerService : IDisposable
    {
        /// <summary>
        /// 当前播放的歌曲
        /// </summary>
        Song? CurrentSong { get; }
        
        /// <summary>
        /// 播放状态
        /// </summary>
        bool IsPlaying { get; }
        
        /// <summary>
        /// 静音状态
        /// </summary>
        bool IsMuted { get; set; }
        
        /// <summary>
        /// 音量 (0.0 - 1.0)
        /// </summary>
        float Volume { get; set; }
        
        /// <summary>
        /// 当前播放位置 (秒)
        /// </summary>
        double CurrentPosition { get; set; }
        
        /// <summary>
        /// 歌曲总时长 (秒)
        /// </summary>
        double MaxPosition { get; }

        /// <summary>
        /// 播放模式
        /// </summary>
        PlayMode CurrentPlayMode { get; set; }
        
        /// <summary>
        /// 频谱数据
        /// </summary>
        float[] SpectrumData { get; }
        
        /// <summary>
        /// 音频引擎是否已初始化
        /// </summary>
        bool IsAudioEngineInitialized { get; }
        
        /// <summary>
        /// 播放状态变化事件
        /// </summary>
        event EventHandler<bool> PlaybackStateChanged;
        
        /// <summary>
        /// 当前歌曲变化事件
        /// </summary>
        event EventHandler<Song?> CurrentSongChanged;
        
        /// <summary>
        /// 播放进度变化事件
        /// </summary>
        event EventHandler<double> PlaybackProgressChanged;
        
        /// <summary>
        /// 频谱数据变化事件
        /// </summary>
        event EventHandler<float[]> SpectrumDataChanged;
        
        /// <summary>
        /// 强制加载指定歌曲（仅由PlayerControlMessageHandler调用）
        /// </summary>
        /// <param name="song">要加载的歌曲</param>
        void LoadSong(Song song);
        
        /// <summary>
        /// 强制开始播放（仅由PlayerControlMessageHandler调用）
        /// </summary>
        void StartPlayback();
        
        /// <summary>
        /// 强制暂停播放（仅由PlayerControlMessageHandler调用）
        /// </summary>
        void PausePlayback();
        
        /// <summary>
        /// 强制停止播放（仅由PlayerControlMessageHandler调用）
        /// </summary>
        void StopPlayback();
        
        /// <summary>
        /// 强制跳转到指定位置（仅由PlayerControlMessageHandler调用）
        /// </summary>
        void SeekToPosition(double position);
    } 
}