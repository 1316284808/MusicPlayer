using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Core.Models
{
    /// <summary>
    /// 播放状态响应
    /// </summary>
    public class PlayerStatusResponse
    {
        public Song? CurrentSong { get; set; }
        public TimeSpan CurrentPosition { get; set; }
        public TimeSpan MaxPosition { get; set; }
        public bool IsPlaying { get; set; }
        public double Volume { get; set; }
        public bool IsMuted { get; set; }
        public PlayMode PlayMode { get; set; }
    }
}
