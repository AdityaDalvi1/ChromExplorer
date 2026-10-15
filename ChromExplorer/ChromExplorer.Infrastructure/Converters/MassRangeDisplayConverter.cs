using System;
using System.Globalization;
using System.Windows.Data;
namespace ChromExplorer.Infrastructure.Converters
{
    [ValueConversion(typeof(double), typeof(string))]
    public class MassRangeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is double))
                return "Please select";

            var mass = (double)value;

            if (double.IsNaN(mass))
                return "Please select";

            return mass.ToString("0.####", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}