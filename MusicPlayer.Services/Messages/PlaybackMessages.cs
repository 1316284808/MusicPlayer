using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 播放/暂停消息
    /// </summary>
    public class PlayPauseMessage : RequestMessage<bool> { }

    /// <summary>
    /// 停止播放消息
    /// </summary>
    public class StopPlaybackMessage : RequestMessage<bool> { }

    /// <summary>
    /// 下一首消息
    /// </summary>
    public class NextSongMessage : RequestMessage<bool> { }

    /// <summary>
    /// 上一首消息
    /// </summary>
    public class PreviousSongMessage : RequestMessage<bool> { }

    /// <summary>
    /// 切换到下一首歌曲消息（内部使用，避免ViewModel直接调用服务）
    /// </summary>
    public class SwitchToNextSongMessage
    {
        public PlayMode PlayMode { get; }
        
        public SwitchToNextSongMessage(PlayMode playMode)
        {
            PlayMode = playMode;
        }
    }

    /// <summary>
    /// 切换到上一首歌曲消息（内部使用，避免ViewModel直接调用服务）
    /// </summary>
    public class SwitchToPreviousSongMessage
    {
        public PlayMode PlayMode { get; }
        
        public SwitchToPreviousSongMessage(PlayMode playMode)
        {
            PlayMode = playMode;
        }
    }

    /// <summary>
    /// 播放状态查询消息
    /// </summary>
    public class PlayerStatusQueryMessage : RequestMessage<PlayerStatusResponse> { }

    /// <summary>
    /// 播放状态变化消息
    /// </summary>
    public class PlaybackStateChangedMessage : ValueChangedMessage<bool> 
    { 
        public PlaybackStateChangedMessage(bool value) : base(value) { }
    }

    /// <summary>
    /// 播放进度消息
    /// </summary>
    public class PlaybackProgressMessage : ValueChangedMessage<double> 
    { 
        public PlaybackProgressMessage(double value) : base(value) { }
    }

    /// <summary>
    /// 播放进度变化消息（别名，为了清晰区分）
    /// </summary>
    public class PlaybackProgressChangedMessage : ValueChangedMessage<double> 
    { 
        public PlaybackProgressChangedMessage(double value) : base(value) { }
    }

    /// <summary>
    /// 当前歌曲变化消息
    /// </summary>
    public class CurrentSongChangedMessage : ValueChangedMessage<Song?> 
    { 
        public CurrentSongChangedMessage(Song? value) : base(value) { }
    }

    /// <summary>
    /// 播放器初始化完成消息
    /// </summary>
    public class PlayerInitializedMessage : RequestMessage<bool> { }

    /// <summary>
    /// 播放选中歌曲消息
    /// </summary>
    public class PlaySelectedSongMessage : ValueChangedMessage<Song> 
    {
        public PlaySelectedSongMessage(Song song) : base(song)
        {
        }
    }

    /// <summary>
    /// 歌曲选择消息（用于播放指定歌曲）
    /// </summary>
    public class SongSelectionMessage : RequestMessage<bool>
    {
        public Song Song { get; }
        public int Index { get; }
        
        public SongSelectionMessage(Song song, int index = -1)
        {
            Song = song;
            Index = index;
        }
    }

    /// <summary>
    /// 播放位置变化消息
    /// </summary>
    public class PlaybackPositionChangedMessage : ValueChangedMessage<double> 
    { 
        public PlaybackPositionChangedMessage(double value) : base(value) { }
    }

    /// <summary>
    /// 播放位置控制消息（用户拖动滑块等）
    /// </summary>
    public class SeekMessage : RequestMessage<double>
    {
        public double Position { get; }

        public SeekMessage(double position) : base()
        {
            Position = position;
        }
    }

    /// <summary>
    /// 最大播放位置变化消息
    /// </summary>
    public class MaxPositionChangedMessage : ValueChangedMessage<double> 
    { 
        public MaxPositionChangedMessage(double value) : base(value) { }
    }

    /// <summary>
    /// 频谱数据更新消息
    /// 优化版本：支持复用缓冲区，避免频繁创建数组
    /// </summary>
    public class SpectrumDataUpdatedMessage : ValueChangedMessage<float[]> 
    { 
        public SpectrumDataUpdatedMessage(float[] value) : base(value) { }
    }



    /// <summary>
    /// 播放模式变化消息
    /// </summary>
    public class PlayModeChangedMessage : ValueChangedMessage<PlayMode> 
    { 
        public PlayModeChangedMessage(PlayMode value) : base(value) { }
    }

    /// <summary>
    /// 切换播放模式消息
    /// </summary>
    public class TogglePlayModeMessage : RequestMessage<bool> { }

    /// <summary>
    /// 歌词更新消息
    /// </summary>
    public class LyricsUpdatedMessage : ValueChangedMessage<ObservableCollection<LyricLine>> 
    { 
        public LyricsUpdatedMessage(ObservableCollection<LyricLine> value) : base(value) { }
    }

    /// <summary>
    /// 当前歌词行变化消息
    /// </summary>
    public class CurrentLyricLineMessage : ValueChangedMessage<LyricLine> 
    { 
        public CurrentLyricLineMessage(LyricLine value) : base(value) { }
    }

    /// <summary>
    /// 配置查询消息 - 用于获取当前配置
    /// </summary>
    public class ConfigurationQueryMessage : RequestMessage<bool>
    {
        public System.Func<PlayerConfiguration, bool> QueryAction { get; }

        public ConfigurationQueryMessage(System.Func<PlayerConfiguration, bool> queryAction)
        {
            QueryAction = queryAction ?? throw new ArgumentNullException(nameof(queryAction));
        }
    }

    /// <summary>
    /// 配置变化消息 - 当配置发生变化时发送
    /// </summary>
    public class ConfigurationChangedMessage : ValueChangedMessage<PlayerConfiguration>
    {
        public ConfigurationChangedMessage(PlayerConfiguration configuration) : base(configuration) { }
    }

    /// <summary>
    /// 频谱显示状态改变消息
    /// </summary>
    public class SpectrumDisplayChangedMessage
    {
        public bool IsEnabled { get; }

        public SpectrumDisplayChangedMessage(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }

    /// <summary>
    /// 主题变化消息
    /// </summary>
    public class ThemeChangedMessage
    {
        public Theme Theme { get; }

        public ThemeChangedMessage(Theme theme)
        {
            Theme = theme;
        }
    }
}