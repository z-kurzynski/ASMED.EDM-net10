using System;
using System.Globalization;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    public class BoolToTakNieConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "Tak" : "Nie";
            return "Nie";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                return string.Equals(s, "Tak", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}
