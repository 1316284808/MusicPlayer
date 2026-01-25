using System;
using System.Globalization;
using System.Windows.Data;
using MusicPlayer.Core.Models;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 歌曲和歌单转换器
    /// 用于将歌曲对象和歌单ID转换为AddSongToPlaylistCommand所需的元组参数
    /// </summary>
    public class SongAndPlaylistConverter : IMultiValueConverter
    {
        /// <summary>
        /// 转换值
        /// </summary>
        /// <param name="values">值数组</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化</param>
        /// <returns>转换后的值</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return null;

            var song = values[0] as Song;
            if (song == null)
                return null;

            if (values[1] is int playlistId)
            {
                return (song, playlistId);
            }

            return null;
        }

        /// <summary>
        /// 转换回值
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="targetTypes">目标类型数组</param>
        /// <param name="parameter">参数</param>
        /// <param name="culture">文化</param>
        /// <returns>转换回的值数组</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}