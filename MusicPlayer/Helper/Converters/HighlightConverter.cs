using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 逐字高亮转换器
    /// </summary>
    public class HighlightConverter : IMultiValueConverter
    {
        /// <summary>
        /// 将文本转换为高亮部分
        /// </summary>
        /// <param name="values">值数组，第一个元素是完整文本，第二个元素是高亮索引</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>高亮部分的文本</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not string text || values[1] is not int highlightIndex)
            {
                return string.Empty;
            }

            // 确保索引在有效范围内
            highlightIndex = Math.Max(0, Math.Min(text.Length, highlightIndex));

            // 返回高亮部分的文本
            return text.Substring(0, highlightIndex);
        }

        /// <summary>
        /// 反向转换，未实现
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
