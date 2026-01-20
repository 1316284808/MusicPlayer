using System;
using System.Globalization;
using System.Windows.Data;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 布尔值到图标类型转换器
    /// </summary>
    public class BoolToIconConverter : IValueConverter, IMultiValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorited)
            {
                return isFavorited ? IconKind.Heart : IconKind.HeartFill;
            }
            return IconKind.Heart;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is bool isFavorited)
            {
                return isFavorited ? IconKind.HeartFill : IconKind.Heart;
            }
            return IconKind.Heart;
        }
    }
}