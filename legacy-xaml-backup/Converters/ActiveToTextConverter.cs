using System;
using System.Globalization;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    // Converts a boolean to display text used on examination toggle buttons
    public class ActiveToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b) return "? AKTYWNE";
            return "? NIEAKTYWNE";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s) return s.StartsWith("?");
            return false;
        }
    }
}
