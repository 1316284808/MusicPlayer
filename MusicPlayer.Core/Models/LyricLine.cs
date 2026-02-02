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
        
        // 歌词显示设置（从 CenterContentViewModel 同步）
        [ObservableProperty]
        private double _lyricFontSize = 16;
        
        [ObservableProperty]
        private double _selectedLyricFontSize = 22;
        
        [ObservableProperty]
        private System.Windows.TextAlignment _lyricTextAlignment = System.Windows.TextAlignment.Right;
        
        [ObservableProperty]
        private bool _isLyricTranslationEnabled = true;
    }
}