using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 布尔值到可见性转换器
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // 默认情况下，true转换为Visible，false转换为Hidden
                bool invert = parameter?.ToString() == "Invert";
                
                if (invert)
                {
                    return boolValue ? Visibility.Hidden : Visibility.Visible;
                }
                else
                {
                    return boolValue ? Visibility.Visible : Visibility.Hidden;
                }
            }
            
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool invert = parameter?.ToString() == "Invert";
                
                if (invert)
                {
                    return visibility != Visibility.Visible;
                }
                else
                {
                    return visibility == Visibility.Visible;
                }
            }
            
            return false;
        }
    }
}