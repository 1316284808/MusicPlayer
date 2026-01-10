using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 布尔值到宽度的转换器
    /// 当值为True时返回250，当值为False时返回60
    /// </summary>
    public class BoolToWidthConverter : IValueConverter
    {
        private static readonly BoolToWidthConverter _instance = new BoolToWidthConverter();
        
        /// <summary>
        /// 获取转换器的单例实例
        /// </summary>
        public static BoolToWidthConverter Instance => _instance;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 125 : 40.0;
            }
            return 125; // 默认值
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}