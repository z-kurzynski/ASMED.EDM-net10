using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ASMED.WPF.Converters
{
    public class PeselFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            var s = value.ToString()?.Trim() ?? string.Empty;
            // keep only digits
            var digits = new string(s.Where(char.IsDigit).ToArray());
            if (digits.Length == 11)
            {
                // format: xxxxxx xx xxx
                return string.Concat(digits.AsSpan(0, 6).ToString(), " ", digits.AsSpan(6, 2).ToString(), " ", digits.AsSpan(8, 3).ToString());
            }
            // fallback: try to insert spaces approximately if >=11
            if (digits.Length > 6 && digits.Length <= 11)
            {
                try
                {
                    var part1 = digits.Substring(0, Math.Min(6, digits.Length));
                    var part2 = digits.Length > 6 ? digits.Substring(6, Math.Min(2, digits.Length - 6)) : string.Empty;
                    var part3 = digits.Length > 8 ? digits.Substring(8) : string.Empty;
                    return string.Join(" ", new[] { part1, part2, part3 }.Where(p => !string.IsNullOrEmpty(p)));
                }
                catch
                {
                    return s;
                }
            }
            return s;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // remove spaces on convert back
            if (value == null) return null;
            var s = value.ToString();
            return new string(s.Where(char.IsDigit).ToArray());
        }
    }
}
