using System;
using System.Globalization;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    public class BoolToBrushConverter : IValueConverter
    {
        public System.Windows.Media.Brush TrueBrush { get; set; } = System.Windows.Media.Brushes.Green;
        public System.Windows.Media.Brush FalseBrush { get; set; } = System.Windows.Media.Brushes.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return TrueBrush;
            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
