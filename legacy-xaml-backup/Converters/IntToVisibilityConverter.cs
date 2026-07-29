using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    /// <summary>
    /// Konwerter: int > 0 → Visible, w przeciwnym razie → Hidden
    /// Używany do pokazywania przycisków gdy ID pacjenta jest ustawiony
    /// </summary>
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0 ? Visibility.Visible : Visibility.Hidden;
            }

            // Jeśli null lub inny typ → Hidden
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("IntToVisibilityConverter jest jednostronny (tylko Convert)");
        }
    }
}