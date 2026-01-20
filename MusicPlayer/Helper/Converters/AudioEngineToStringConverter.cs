using System;
using System.Globalization;
using System.Windows.Data;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 音频引擎枚举转字符串转换器
    /// </summary>
    public class AudioEngineToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AudioEngine audioEngine)
            {
                return audioEngine switch
                {
                    AudioEngine.Auto => "Auto",
                    AudioEngine.DirectSound => "DirectSound",
                    AudioEngine.WASAPI => "WASAPI",
                    _ => "Auto"
                };
            }
            return "Auto";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str switch
                {
                    "Auto " => AudioEngine.Auto,
                    "DirectX" => AudioEngine.DirectSound,
                    "Windows会话(独占)" => AudioEngine.WASAPI,
                    _ => AudioEngine.Auto
                };
            }
            return AudioEngine.Auto;
        }
    }
}