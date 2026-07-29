using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMED.WPF.Converters
{
    public class ListaToBackgroundConverter : IValueConverter
    {
        // Zwraca zielone t³o gdy wartoœæ > 0, w przeciwnym razie Transparent
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return Brushes.Transparent;

                if (value is int i)
                    // t³o zielone gdy i > 0 cyfry  
                    return i > 0 ? new SolidColorBrush(Color.FromRgb(11, 247, 11)) : Brushes.Transparent;

                if (int.TryParse(value.ToString(), out var parsed))
                    return parsed > 0 ? new SolidColorBrush(Color.FromRgb(11, 247, 11)) : Brushes.Transparent;
            }
            catch { }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}