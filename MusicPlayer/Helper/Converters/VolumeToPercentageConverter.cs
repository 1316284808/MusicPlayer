using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 音量值转百分比转换器 - 将0-1范围的音量值转换为百分比字符串
    /// </summary>
    public class VolumeToPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is float volume)
                {
                    return $"{Math.Round(volume * 100)}%";
                }
                return "0%";
            }
            catch (Exception)
            {
                return "0%";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}