using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 进度到宽度的转换器
    /// 用于将Slider的值转换为已播放部分的宽度
    /// </summary>
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return 0.0;

            // values[0] = 当前值 (Value)
            // values[1] = 最大值 (Maximum)
            // values[2] = 轨道实际宽度 (ActualWidth)
            
            if (values[0] is double currentValue && 
                values[1] is double maximum && 
                values[2] is double trackWidth)
            {
                if (maximum <= 0 || trackWidth <= 0)
                    return 0.0;

                // 计算比例
                double ratio = currentValue / maximum;
                // 返回宽度（考虑Margin=8,0，所以最大宽度为trackWidth）
                return ratio * trackWidth;
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
