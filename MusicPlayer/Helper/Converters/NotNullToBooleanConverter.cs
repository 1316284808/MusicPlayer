using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    public class NotNullToBooleanConverter : IValueConverter
    {
        public static readonly NotNullToBooleanConverter Instance = new NotNullToBooleanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = value != null;
            
            // 检查是否需要反转结果
            if (parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                result = !result;
            }
            
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}