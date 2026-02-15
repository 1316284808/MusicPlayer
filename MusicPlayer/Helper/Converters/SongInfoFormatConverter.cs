using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 歌曲信息格式化转换器，用于在绑定值前添加标签
    /// 使用方式：Converter={StaticResource SongInfoFormatConverter}, ConverterParameter='标签名'
    /// </summary>
    public class SongInfoFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string label = parameter?.ToString() ?? "";
            string displayValue = value?.ToString() ?? "N/A";
            
            // 特殊处理数值类型
            if (value is int intValue)
            {
                // 检查参数来确定格式
                if (label.Contains("码率"))
                {
                    return $"{label}: {intValue} kbps";
                }
                else if (label.Contains("位深"))
                {
                    return $"{label}: {intValue} bit";
                }
                return $"{label}: {intValue}";
            }
            else if (value is long longValue)
            {
                // 文件大小会在FileSizeConverter中处理
                return $"{label}: {displayValue}";
            }
            else if (value is null || (value is string str && string.IsNullOrEmpty(str)))
            {
                return $"{label}: N/A";
            }
            
            return $"{label}: {displayValue}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
