using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 索引转换器 - 获取ItemsControl中当前项的索引
    /// </summary>
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 这个转换器主要用于支持绑定语法，实际索引由其他方式获取
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 频谱索引转换器 - 替代方案，使用AlternationCount获取索引
    /// </summary>
    public class SpectrumIndexConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is DependencyObject container && values[1] is int totalCount)
            {
                // 获取容器中的索引位置
                var itemsControl = ItemsControl.GetItemsOwner(container);
                if (itemsControl != null)
                {
                    int index = itemsControl.ItemContainerGenerator.IndexFromContainer(container);
                    return index;
                }
            }
            
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}