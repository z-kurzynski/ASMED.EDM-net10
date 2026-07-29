using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMED.WPF.Converters
{
    /// <summary>
    /// Konwerter mapujący status wizyty na kolor tła
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value?.ToString()?.ToLower();

            return status switch
            {
                // opcja 1 - użycie kolorów zdefiniowanych poniżej
                "rejestracja" => new SolidColorBrush(Color.FromRgb(52, 152, 219)),    // 📝 niebieski #3498DB (priorytet 1)
                "dokumentacja" => new SolidColorBrush(Color.FromRgb(241, 196, 15)),  // 📑 żółty #F1C40F (priorytet 2)
                "zaplanowana" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),    // Niebieski #2196F3
                "odbyta" => new SolidColorBrush(Color.FromRgb(39, 174, 96)),          // ✅ Zielony #27AE60 
                "anulowana" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),       // Czerwony #F44336
                "w trakcie" => new SolidColorBrush(Color.FromRgb(28, 88, 148)),       // 👨‍⚕️turkus #16A085 
                "przełożona" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),     // Fioletowy #9C27B0
                "nieobecność" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),    // Ciemny szary #607D8B
                "zamknięta" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),     // Fioletowy #9C27B0
                _ => new SolidColorBrush(Color.FromRgb(112, 112, 112))                // Domyślny szary #707070

                /*  opcja 2 - jeśli w przyszłości chcemy użyć innych kolorów niż te zdefiniowane powyżej
                "rejestracja" => new SolidColorBrush(Color.FromRgb(255, 235, 59)),    // Żółty #FFEB3B (priorytet 1)
                "dokumentacja" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),  // ✅ NOWY: Szary #9E9E9E (priorytet 2)
                "zaplanowana" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),    // Niebieski #2196F3
                "odbyta" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),          // Zielony #4CAF50
                "anulowana" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),       // Czerwony #F44336
                "w trakcie" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),       // Pomarańczowy #FF9800
                "przełożona" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),     // Fioletowy #9C27B0
                "nieobecność" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),    // Ciemny szary #607D8B
                _ => new SolidColorBrush(Color.FromRgb(224, 224, 224))                // Domyślny jasny szary #E0E0E0
                */
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
