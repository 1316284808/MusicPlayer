using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Effects;

namespace MusicPlayer.Converters
{
    public class BlurEffectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double radius && radius > 0)
            {
                return new BlurEffect
                {
                    Radius = radius,
                    KernelType = KernelType.Gaussian
                };
            }
            return new BlurEffect { Radius = 0 };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
