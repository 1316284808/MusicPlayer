using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 反高亮转换器 - 显示未高亮的部分
    /// </summary>
    public class InverseHighlightConverter : IMultiValueConverter
    {
        /// <summary>
        /// 将文本转换为未高亮部分
        /// </summary>
        /// <param name="values">值数组，第一个元素是完整文本，第二个元素是高亮索引</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>未高亮部分的文本</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not string text || values[1] is not int highlightIndex)
            {
                return string.Empty;
            }

            // 确保索引在有效范围内
            highlightIndex = Math.Max(0, Math.Min(text.Length, highlightIndex));

            // 如果索引等于文本长度，返回空字符串
            if (highlightIndex >= text.Length)
            {
                return string.Empty;
            }

            // 返回未高亮部分的文本
            return text.Substring(highlightIndex);
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
