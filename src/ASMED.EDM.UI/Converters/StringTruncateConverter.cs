using System.Globalization;
using System.Windows.Data;

namespace ASMED.EDM.UI.Converters;

/// <summary>
/// Przycina tekst do maksymalnej długości podanej jako ConverterParameter (domyślnie10).
/// </summary>
public sealed class StringTruncateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string text) return value;

        int maxLen = 10;
        if (parameter is string p && int.TryParse(p, out int parsed))
            maxLen = parsed;

        return text.Length <= maxLen ? text : text[..maxLen];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}
