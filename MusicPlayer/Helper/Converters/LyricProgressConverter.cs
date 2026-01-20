using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 歌词进度转换器
    /// 用于将歌词进度转换为文本宽度
    /// </summary>
    public class LyricProgressConverter : IMultiValueConverter
    {
        /// <summary>
        /// 转换逻辑
        /// </summary>
        /// <param name="values">绑定值数组：[0]为进度值(0-1)，[1]为文本实际宽度</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换后的宽度值</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // 确保values数组有至少两个元素
                if (values.Length < 2)
                    return 0;

                // 解析进度值（0-1）
                double progress = values[0] is double ? (double)values[0] : 0;
                
                // 解析文本宽度
                double actualWidth = values[1] is double ? (double)values[1] : 0;

                // 计算转换后的宽度
                double resultWidth = progress * actualWidth;

                // 确保宽度不小于0
                return Math.Max(0, resultWidth);
            }
            catch (Exception)
            {
                // 转换失败时返回0
                return 0;
            }
        }

        /// <summary>
        /// 反向转换逻辑（未实现）
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}