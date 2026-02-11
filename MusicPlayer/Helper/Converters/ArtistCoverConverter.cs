using MusicPlayer.Core.Data;
using MusicPlayer.Core.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 通用艺术家封面转换器 - 支持 AlbumInfo 和 SingerInfo
    /// </summary>
    public class ArtistCoverConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? filePath = null;
            
            if (value is AlbumInfo album && !string.IsNullOrEmpty(album.FirstSongFilePath))
            {
                filePath = album.FirstSongFilePath;
            }
            else if (value is SingerInfo singer && !string.IsNullOrEmpty(singer.FirstSongFilePath))
            {
                filePath = singer.FirstSongFilePath;
            }
            
            if (!string.IsNullOrEmpty(filePath))
            {
                return AlbumArtLoader.LoadAlbumArt(filePath);
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
