using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ChromExplorer.DataModel.Spectrum;

namespace ChromExplorer.Infrastructure.Converters
{
    [ValueConversion(typeof(Boolean), typeof(Visibility))]
    public class SpectrumTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
             if (value == null)
                 return  Visibility.Visible;

            if(value.GetType() != typeof(SpectrumType))
            {
                return Visibility.Visible;
            }

            var st = (SpectrumType) value;

            if (st == SpectrumType.MS)
                return Visibility.Collapsed;

            return Visibility.Visible;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
