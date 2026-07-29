using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMED.WPF.Converters
{
    public class StatusColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is bool isNew)
            {
                return isNew ? Brushes.LimeGreen : Brushes.LightGray;
            }
            return Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
