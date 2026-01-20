using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using MusicPlayer.Helper;
using MusicPlayer.Core.Enums;

namespace MusicPlayer.Converters
{
    /// <summary>
    /// 设置栏图标转换器，根据按钮类型和选中状态返回对应的图标
    /// </summary>
    public class SettingsIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return IconKind.List; // 默认图标

            // values[0] 是按钮类型 ("DefaultList", "FavoriteList", "Settings")
            // values[1] 是是否选中 (bool)
            string buttonType = values[0].ToString();
            bool isSelected = (bool)values[1];

            switch (buttonType)
            {
                case "DefaultList":
                    return isSelected ? IconKind.InList : IconKind.List;
                case "FavoriteList":
                    return isSelected ? IconKind.InHeart : IconKind.Heart;
                case "Settings":
                    return isSelected ? IconKind.InSettings : IconKind.Settings;
                default:
                    return IconKind.List;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}