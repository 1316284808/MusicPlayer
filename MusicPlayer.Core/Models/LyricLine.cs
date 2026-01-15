using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer.Core.Models
{
    public partial class LyricLine : ObservableObject
    {
        [ObservableProperty]
        private TimeSpan _time;
        // 双语歌词，中文部分
        [ObservableProperty]
        private string _textCN = string.Empty;
        // 双语歌词，英文部分
        [ObservableProperty]
        private string _textEN = string.Empty;

        [ObservableProperty]
        private double _progress = 0;
    }
}