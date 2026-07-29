using System;
using System.Globalization;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    // Zmieñ nazwê klasy, aby unikn¹æ konfliktu z istniej¹c¹ definicj¹
    public class InverseBoolConverter1 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value;
        }
    }
}