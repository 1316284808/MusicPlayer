using System;
using System.Globalization;
using System.Windows.Data;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Interface;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 播放模式图标转换器 - 根据播放模式动态选择对应的图标
    /// </summary>
    public class PlayModeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // 调试输出，验证转换器是否被调用
                System.Diagnostics.Debug.WriteLine($"PlayModeIconConverter.Convert called with value: {value?.GetType().Name} = {value}");
                
                if (value is PlayMode playMode)
                {
                    var result = playMode switch
                    {
                        PlayMode.RepeatOne => IconKind.RepeatOne,
                        PlayMode.RepeatAll => IconKind.RepeatAll,
                        PlayMode.Shuffle => IconKind.Shuffle,
                        _ => IconKind.RepeatAll
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"PlayModeIconConverter returning: {result}");
                    return result;
                }
                
                // 默认返回列表循环播放模式图标
                System.Diagnostics.Debug.WriteLine("PlayModeIconConverter returning default: RepeatAll");
                return IconKind.RepeatAll;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlayModeIconConverter exception: {ex.Message}");
                // 发生异常时返回默认图标
                return IconKind.PlayModeDefault;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}