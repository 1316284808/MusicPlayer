using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 文件大小转换器 - 将字节数转换为友好的文件大小格式（KB, MB, GB）
    /// </summary>
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string label = parameter?.ToString();
                string result;
                
                if (value is long bytes)
                {
                    if (bytes <= 0)
                        result = "0 B";
                    else
                    {
                        string[] units = { "B", "KB", "MB", "GB", "TB" };
                        int unitIndex = 0;
                        double size = bytes;

                        while (size >= 1024 && unitIndex < units.Length - 1)
                        {
                            size /= 1024;
                            unitIndex++;
                        }

                        // 根据单位决定小数位数
                        result = unitIndex == 0 
                            ? $"{size:0} {units[unitIndex]}" 
                            : $"{size:0.##} {units[unitIndex]}";
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
