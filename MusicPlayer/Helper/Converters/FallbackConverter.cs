using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 回退值转换器
    /// 用于在第一个值为空时返回第二个值
    /// </summary>
    public class FallbackConverter : IMultiValueConverter
    {
        /// <summary>
        /// 转换逻辑
        /// </summary>
        /// <param name="values">绑定值数组：[0]为主值，[1]为回退值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">文化信息</param>
        /// <returns>转换后的结果</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // 确保values数组有至少两个元素
                if (values.Length < 2)
                    return string.Empty;

                // 获取主值
                string primaryValue = values[0] as string;
                // 如果主值不为空，返回主值
                if (!string.IsNullOrEmpty(primaryValue))
                    return primaryValue;

                // 获取回退值
                string fallbackValue = values[1] as string;
                // 返回回退值，即使回退值也为空
                return fallbackValue ?? string.Empty;
            }
            catch (Exception)
            {
                // 转换失败时返回空字符串
                return string.Empty;
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