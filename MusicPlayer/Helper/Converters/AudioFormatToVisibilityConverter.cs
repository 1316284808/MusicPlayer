using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 音频格式到可见性转换器
    /// 根据参数值决定是显示HiRes还是SQ徽标
    /// </summary>
    public class AudioFormatToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string format = value.ToString().ToLowerInvariant();
            string badgeType = parameter.ToString().ToLowerInvariant();

            // HiRes格式：FLAC/APE/WAV
            if (badgeType == "hires")
            {
                return (format == ".flac" || format == ".ape" || format == ".wav") 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            // SQ格式：MP3/AAC
            if (badgeType == "sq")
            {
                return (format == ".mp3" || format == ".aac" || format == ".m4a") 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}