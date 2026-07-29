using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    /// <summary>
    /// Konwerter odwracający wartość bool i konwertujący na Visibility
    /// true → Collapsed, false → Visible
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return true;
        }
    }
}
