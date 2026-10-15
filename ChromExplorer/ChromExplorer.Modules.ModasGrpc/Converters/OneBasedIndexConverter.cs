using System;
using System.Globalization;
using System.Windows.Data;

namespace ChromExplorer.Modules.ModasGrpc.Converters
{
    // Converts a DataGridRow's zero-based AlternationIndex into a 1-based serial number for display.
    public class OneBasedIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int index ? (index + 1).ToString() : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
