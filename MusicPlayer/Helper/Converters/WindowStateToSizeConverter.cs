using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 窗口状态到尺寸转换器
    /// 根据窗口是否最大化返回相应的尺寸值
    /// </summary>
    public class WindowStateToSizeConverter : IMultiValueConverter
    {
        /// <summary>
        /// 转换窗口状态为尺寸
        /// </summary>
        /// <param name="values">值数组，第一个为是否最大化布尔值，第二个为正常尺寸，第三个为最大化尺寸</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数（"cover"或"spectrum"）</param>
        /// <param name="culture">区域信息</param>
        /// <returns>转换后的尺寸</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[0] == null || values[1] == null || values[2] == null)
                return 300.0; // 默认值

            bool isMaximized = (bool)values[0];
            double normalSize = (double)values[1];
            double maxSize = (double)values[2];

            // 根据窗口状态返回相应的尺寸
            return isMaximized ? maxSize : normalSize;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 窗口状态到封面尺寸转换器
    /// 根据窗口是否最大化返回相应的封面尺寸
    /// </summary>
    public class WindowStateToCoverSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMaximized = value is bool && (bool)value;
            
            // 根据窗口状态返回相应的尺寸
            if (parameter != null && parameter.ToString() == "diameter")
            {
                // 返回封面直径
                return isMaximized ? 450.0 : 300.0; // 
            }
            else
            {
                // 返回封面半径
                return isMaximized ? 225.0 : 150.0; // 最大化时半径250，正常时半径150
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 窗口状态到频谱尺寸转换器
    /// 根据窗口是否最大化返回相应的频谱尺寸
    /// </summary>
    public class WindowStateToSpectrumSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMaximized = value is bool && (bool)value;
            
            // 根据窗口状态返回相应的尺寸
            // 频谱容器尺寸 = 封面直径 + 2 * 最大条高度
            // 正常时：封面直径300 + 2 * 75 = 450
            // 最大化时：封面直径450 + 2 * 125 = 750
            return isMaximized ? 700.0 : 450.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 窗口状态到频谱实际显示尺寸转换器
    /// 返回与封面直径相同的尺寸，确保频谱内圆与封面边缘对齐
    /// </summary>
    public class WindowStateToSpectrumDisplaySizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMaximized = value is bool && (bool)value;
            
            // 返回与封面相同的尺寸，确保频谱内圆与封面边缘对齐
            return isMaximized ? 450.0 : 300.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}