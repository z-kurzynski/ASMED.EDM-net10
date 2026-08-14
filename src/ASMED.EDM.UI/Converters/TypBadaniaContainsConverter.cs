using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ASMED.EDM.UI.Converters;

/// <summary>
/// Sprawdza, czy wartość B_TypBadania (np. "W", "K", "O", "O/K") zawiera podany
/// fragment typu przekazany jako ConverterParameter (np. "K" lub "O").
/// Używane przez DataTrigger do podświetlania przycisków Wstępne/Kontrolne/Okresowe,
/// gdy Kontrolne i Okresowe mogą współistnieć jako "O/K".
/// </summary>
public sealed class TypBadaniaContainsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var typ = value as string;
        var part = parameter as string;
        if (string.IsNullOrEmpty(typ) || string.IsNullOrEmpty(part))
            return false;

        return typ.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Contains(part);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
