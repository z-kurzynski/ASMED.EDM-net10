using System;
using System.Globalization;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    public class ZeroToEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            try
            {
                decimal dec;
                if (value is decimal d)
                    dec = d;
                else if (decimal.TryParse(value.ToString(), NumberStyles.Number, culture, out var parsed))
                    dec = parsed;
                else if (decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedInv))
                    dec = parsedInv;
                else
                    return value.ToString();

                if (dec == 0m)
                    return string.Empty;

                // format with two decimals using current culture
                return dec.ToString("N2", culture);
            }
            catch
            {
                return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not needed for display-only converter
            return Binding.DoNothing;
        }
    }
}
