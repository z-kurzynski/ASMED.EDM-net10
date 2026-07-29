using System;
using System.Globalization;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    /// <summary>
    /// Converter odwracaj¹cy wartoœæ bool (true ? false, false ? true)
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}
