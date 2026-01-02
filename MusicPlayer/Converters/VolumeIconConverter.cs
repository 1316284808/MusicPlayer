using System;
using System.Globalization;
using System.Windows.Data;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 音量图标转换器 - 根据音量值和静音状态动态选择对应的图标
    /// </summary>
    public class VolumeIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // 检查参数数量
                if (values == null || values.Length < 2)
                    return IconKind.VolumeHigh;

                // 解析参数：第一个是静音状态，第二个是音量值
                bool isMuted = values[0] is bool muted && muted;
                float volume = values[1] is float vol ? vol : 1.0f;

                // 如果静音，显示静音图标
                if (isMuted)
                {
                    return IconKind.VolumeMute;
                }

                // 根据音量值选择不同的音量图标
                if (volume == 0f)
                    return IconKind.VolumeMute;
                if (volume > 0f && volume <= 0.3f)
                    return IconKind.VolumeLow;
                if (volume > 0.3f && volume <= 0.6f)
                    return IconKind.VolumeMedium;
                if (volume > 0.6f && volume <= 1f)
                    return IconKind.VolumeHigh;

                return IconKind.VolumeMute;
            }
            catch (Exception)
            {
                // 发生异常时返回默认图标
                return IconKind.VolumeMute;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}