using System;
using System.Globalization;
using System.Windows.Data;
using MusicPlayer.Core.Enums;
using MusicPlayer.Core.Models;
using MusicPlayer.ViewModels;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 排序规则到图标类型的转换器
    /// </summary>
    public class SortRuleToSymbolConverter : IValueConverter
    {
        public static readonly SortRuleToSymbolConverter Instance = new SortRuleToSymbolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SortRule sortRule;
            
            if (value is SortRule rule)
            {
                sortRule = rule;
            }
            else if (value is SortOption sortOption)
            {
                sortRule = sortOption.Value;
            }
            else
            {
                return IconKind.SortDefault; // 默认排序图标
            }
            
            return sortRule switch
            {
                SortRule.ByAddedTime => IconKind.Clock,     // 时钟图标
                SortRule.ByTitle => IconKind.Text,         // 文本图标
                SortRule.ByArtist => IconKind.InPerson,      // 人物图标
                SortRule.ByAlbum => IconKind.InAlbum,        // 专辑图标
                SortRule.ByDuration => IconKind.Time,      // 时间图标
                SortRule.ByFileSize => IconKind.Storage,   // 存储图标
                _ => IconKind.SortDefault                  // 默认排序图标
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}