using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using MusicPlayer.Core.Data;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 专辑封面转换器 - 从文件路径动态加载专辑封面
    /// </summary>
    public class AlbumArtConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // 参数判断：是检查是否有图像还是转换图像
                bool isCheckOnly = parameter?.ToString() == "HasImage";
                
                if (value is Song song && !string.IsNullOrEmpty(song.FilePath))
                {
                    if (isCheckOnly)
                    {
                        // 只检查是否有图像（现在无法提前判断，返回true表示尝试加载）
                        return true;
                    }
                    else
                    {
                        // 动态加载专辑封面（不再从Song.AlbumArt获取，而是从文件路径加载）
                        return AlbumArtLoader.LoadAlbumArt(song.FilePath);
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