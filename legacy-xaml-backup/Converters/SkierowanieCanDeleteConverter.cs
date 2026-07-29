using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    /// <summary>
    /// Konwerter: B_Badanie_ID == 0 lub null → Visible (można usunąć)
    ///            B_Badanie_ID > 0 → Collapsed (istnieje powiązane badanie, nie można usunąć)
    /// </summary>
    public class SkierowanieCanDeleteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            // Sprawdź czy PatientSkierowanieId > 0 (musi istnieć skierowanie)
            if (value is int skierowanieId && skierowanieId > 0)
            {
                // ✅ Skierowanie istnieje - sprawdź czy nie ma badania
                // To musi być zrobione w ViewModel poprzez dodatkową właściwość HasBadanie
                // Bo konwerter nie ma dostępu do bazy danych
                return Visibility.Hidden;
            }

            // Jeśli ID == 0 lub null → ukryj przycisk (brak skierowania do usunięcia)
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Konwerter: bool → Visibility (Inverse)
    /// True (ma badanie) → Collapsed
    /// False (nie ma badania) → Visible
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
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
            throw new NotImplementedException();
        }
    }
}