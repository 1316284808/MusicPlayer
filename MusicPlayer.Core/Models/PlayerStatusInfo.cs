using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 播放器状态信息
    /// </summary>
    public class PlayerStatusInfo
    {
        public bool IsPlaying { get; set; }
        public bool IsMuted { get; set; }
        public float Volume { get; set; }
        public double CurrentPosition { get; set; }
        public double MaxPosition { get; set; }
        public Song? CurrentSong { get; set; }
        public PlayMode PlayMode { get; set; }
    }
}