using System;
using System.Globalization;
using System.Windows.Data;

namespace ASMED.EDM.UI.Converters;

/// <summary>
/// Konwertuje bool na tekst (np. "☑"/"☐", "STUB"/"PROD").
/// TrueText i FalseText ustawiane w XAML.
/// </summary>
public sealed class BoolToTextConverter : IValueConverter
{
    public string TrueText  { get; set; } = "tak";
    public string FalseText { get; set; } = "nie";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? TrueText : FalseText;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
