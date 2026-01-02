using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 枚举值到描述文本的转换器
    /// </summary>
    public class EnumToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            // 获取枚举值
            Type enumType = value.GetType();
            if (!enumType.IsEnum)
                return value.ToString();

            // 获取描述特性
            var fieldInfo = enumType.GetField(value.ToString());
            if (fieldInfo == null)
                return value.ToString();

            var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;

            // 如果没有描述特性，返回枚举值的字符串表示
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                // 尝试从描述转换回枚举值
                if (targetType.IsEnum)
                {
                    foreach (var field in targetType.GetFields())
                    {
                        var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
                        if (attributes != null && attributes.Length > 0 && attributes[0].Description == stringValue)
                        {
                            return Enum.Parse(targetType, field.Name);
                        }
                    }
                    
                    // 如果找不到匹配的描述，尝试直接解析
                    if (Enum.IsDefined(targetType, stringValue))
                    {
                        return Enum.Parse(targetType, stringValue);
                    }
                }
            }

            return value;
        }
    }
}