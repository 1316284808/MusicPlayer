using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 采样率转换器 - 将Hz转换为kHz格式
    /// </summary>
    public class SampleRateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string label = parameter?.ToString();
                string result;
                
                if (value is int sampleRate)
                {
                    if (sampleRate <= 0)
                        result = "N/A";
                    else
                    {
                        double kHz = sampleRate / 1000.0;
                        result = $"{kHz:0.#} kHz";
                    }
                }
                else
                {
                    result = "N/A";
                }
                
                // 如果有标签参数，添加前缀
                return string.IsNullOrEmpty(label) ? result : $"{label}: {result}";
            }
            catch (Exception)
            {
                string label = parameter?.ToString();
                return string.IsNullOrEmpty(label) ? "N/A" : $"{label}: N/A";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
