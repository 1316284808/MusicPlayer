using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 静音状态变化消息
    /// </summary>
    public class MuteStateChangedMessage : ValueChangedMessage<bool> 
    { 
        public MuteStateChangedMessage(bool value) : base(value) { }
    }

    /// <summary>
    /// 音量变化消息
    /// </summary>
    public class VolumeChangedMessage : ValueChangedMessage<float> 
    { 
        public VolumeChangedMessage(float value) : base(value) { }
    }

    /// <summary>
    /// 歌曲切换时音量保持消息
    /// </summary>
    public class VolumePreserveMessage : ValueChangedMessage<float> 
    { 
        public VolumePreserveMessage(float value) : base(value) { }
    }

    /// <summary>
    /// 音量设置消息
    /// </summary>
    public class VolumeSetMessage : RequestMessage<bool>
    {
        public float Volume { get; }
        public bool IsRelative { get; }
        
        public VolumeSetMessage(float volume, bool isRelative = false)
        {
            Volume = volume;
            IsRelative = isRelative;
        }
    }
    
    /// <summary>
    /// 音量增量调整消息
    /// </summary>
    public class VolumeAdjustMessage : RequestMessage<float>
    {
        public float Delta { get; }
        
        public VolumeAdjustMessage(float delta)
        {
            Delta = delta;
        }
    }
    
    /// <summary>
    /// 静音切换消息
    /// </summary>
    public class MuteToggleMessage : RequestMessage<bool>
    {
        public bool? TargetMuteState { get; }
        
        public MuteToggleMessage(bool? targetMuteState = null)
        {
            TargetMuteState = targetMuteState;
        }
    }
}