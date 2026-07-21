using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMED.EDM.UI.Converters;

/// <summary>
/// Konwerter statusu wizyty na kolor tła
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string status)
        {
            return new SolidColorBrush(Colors.Gray);
        }

        return status.ToLowerInvariant() switch
        {
            "zaplanowana" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),  // Orange
            "w trakcie" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),   // Blue
            "odbyta" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),       // Green
            "dokumentacja" => new SolidColorBrush(Color.FromRgb(241, 196, 15)), // Yellow
            "anulowana" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),   // Gray
            "nieobecność" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Red
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
