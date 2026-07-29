using System;
using System.Globalization;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    public class DateOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            if (value is DateTime dt)
            {
                if (dt == DateTime.MinValue)
                    return string.Empty;
                return dt.ToString("dd/MM/yyyy");
            }
            return string.Empty;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(value as string))
                return null;
            if (DateTime.TryParseExact(value as string, "dd/MM/yyyy", culture, DateTimeStyles.None, out var dt))
                return dt;
            return null;
        }
    }
}
