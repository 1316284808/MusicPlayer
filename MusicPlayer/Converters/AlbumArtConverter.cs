using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 专辑封面转换器 - 将Song.AlbumArt转换为BitmapImage
    /// </summary>
    public class AlbumArtConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // 参数判断：是检查是否有图像还是转换图像
                bool isCheckOnly = parameter?.ToString() == "HasImage";
                
                if (value is Song song)
                {
                    if (isCheckOnly)
                    {
                        // 只检查是否有图像
                        bool hasImage = song.AlbumArt != null;
                        return hasImage;
                    }
                    else
                    {
                        // 直接返回AlbumArt，Song类会自动确保封面已加载
                        return song.AlbumArt;
                    }
                }
                
                return isCheckOnly ? false : null;
            }
            catch (Exception ex)
            {
                return parameter?.ToString() == "HasImage" ? false : null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}