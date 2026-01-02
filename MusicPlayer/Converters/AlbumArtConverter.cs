using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 专辑封面转换器 - 将Song.AlbumArt或AlbumArtData转换为BitmapImage
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
                        // 只检查是否有图像数据
                        bool hasImage = song.AlbumArtData != null && song.AlbumArtData.Length > 0;
                        return hasImage;
                    }
                    else
                    {
                        // 转换为实际图像
                        return ConvertSongToImage(song);
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

        /// <summary>
        /// 将Song转换为BitmapImage
        /// </summary>
        private BitmapImage ConvertSongToImage(Song song)
        {
            if (song == null || song.AlbumArtData == null || song.AlbumArtData.Length == 0)
            {
                    return null;
            }

            try
            {
              
                // 优化：直接使用字节数组，无需从Base64转换
                var imageBytes = song.AlbumArtData;
                
                // 确保在UI线程上创建BitmapImage
                if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
                {
                    BitmapImage? result = null;
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        result = ConvertSongToImage(song);
                    });
                    return result;
                }
                
                using (var stream = new MemoryStream(imageBytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    // 添加IgnoreColorProfile选项避免颜色上下文错误
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    // 设置解码像素宽度，避免内存问题
                    bitmap.DecodePixelWidth = 300;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    
                    // 冻结图像以确保线程安全
                    if (bitmap.CanFreeze)
                    {
                        bitmap.Freeze();
                    }
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                 return null;
            }
        }
    }
}