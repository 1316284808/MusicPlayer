using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 音质等级到可见性转换器
    /// 根据参数值决定显示哪个音质徽标
    /// 参数值：
    /// - "hires"：当音质等级为 HiRes 时显示
    /// - "sq"：当音质等级为 SQ 时显示
    /// - "hq"：当音质等级为 HQ 时显示
    /// </summary>
    public class QualityLevelToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            // 获取音质等级
            if (!(value is AudioQualityLevel qualityLevel))
                return Visibility.Collapsed;

            string badgeType = parameter.ToString()?.ToLowerInvariant() ?? string.Empty;

            // Hi-Res 徽标
            if (badgeType == "hires")
            {
                return qualityLevel == AudioQualityLevel.HiRes 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            // SQ 徽标
            if (badgeType == "sq")
            {
                return qualityLevel == AudioQualityLevel.SQ 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            // HQ 徽标
            if (badgeType == "hq")
            {
                return qualityLevel == AudioQualityLevel.HQ 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }

            // 未知参数，不显示
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
