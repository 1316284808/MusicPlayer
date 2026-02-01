using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer.Core.Models
{
    public partial class LyricLine : ObservableObject
    {
        [ObservableProperty]
        private TimeSpan _time;
        
        // 原文（歌曲的原始语言）
        [ObservableProperty]
        private string _originalText = string.Empty;
        
        // 翻译文本
        [ObservableProperty]
        private string _translatedText = string.Empty;

        [ObservableProperty]
        private double _progress = 0;
    }
}