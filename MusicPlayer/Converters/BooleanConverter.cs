using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 布尔值转换器 - 作为中转，将TreeViewItem的IsSelected状态转换为可用于子项绑定的布尔值
    /// </summary>
    public class BooleanConverter : IValueConverter
    {
        public static readonly BooleanConverter Instance = new BooleanConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 直接返回传入的布尔值
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}