using MusicPlayer.Core.Enums;

namespace MusicPlayer.Services.Messages
{
    /// <summary>
    /// 音频引擎变更消息
    /// </summary>
    public class AudioEngineChangedMessage
    {
        public AudioEngine AudioEngine { get; }

        public AudioEngineChangedMessage(AudioEngine audioEngine)
        {
            AudioEngine = audioEngine;
        }
    }
}