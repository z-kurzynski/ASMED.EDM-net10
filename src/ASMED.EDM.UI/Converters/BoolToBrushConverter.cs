using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ASMED.EDM.UI.Converters;

/// <summary>
/// Konwertuje bool na SolidColorBrush.
/// TrueColor i FalseColor można ustawiać w XAML lub pozostawić domyślne.
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    public Color TrueColor  { get; set; } = Colors.Green;
    public Color FalseColor { get; set; } = Color.FromRgb(0x78, 0x90, 0x9C);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => new SolidColorBrush(value is true ? TrueColor : FalseColor);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
