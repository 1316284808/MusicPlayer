using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer.Core.Models
{
    public partial class LyricLine : ObservableObject
    {
        [ObservableProperty]
        private TimeSpan _time;
        
        [ObservableProperty]
        private string _text = string.Empty;
    }
}