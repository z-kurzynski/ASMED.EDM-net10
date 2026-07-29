using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMED.EDM.UI.Converters;

/// <summary>
/// Konwerter mapujący status wizyty na kolor tła (zgodnie z legacy)
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString()?.ToLower();

        return status switch
        {
            // kolory zgodne z legacy WPF
            "rejestracja" => new SolidColorBrush(Color.FromRgb(52, 152, 219)),    // #3498DB niebieski
            "dokumentacja" => new SolidColorBrush(Color.FromRgb(241, 196, 15)),  // #F1C40F żółty
            "wywiad" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),    // #2196F3 niebieski
            "odbyta" => new SolidColorBrush(Color.FromRgb(39, 174, 96)),          // #27AE60 zielony
            "anulowana" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),       // #F44336 czerwony
            "w trakcie" => new SolidColorBrush(Color.FromRgb(28, 88, 148)),       // #1C5894 turkus/ciemnieszy
            "przełożona" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),     // #9C27B0 fioletowy
            "nieobecność" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),    // #607D8B ciemny szary
            "zamknięta" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),      // fallback fiolet
            "orzeczenie" => new SolidColorBrush(Color.FromRgb(139, 174, 96)),     //  #27AE60 zielony
            _ => new SolidColorBrush(Color.FromRgb(112, 112, 112))                // domyślny szary #707070
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
