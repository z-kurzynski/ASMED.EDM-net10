using System.Text;
using System.Text.Json;

namespace ASMED.EDM.P1.Middleware.Models;

/// <summary>
/// Pole "messages" w odpowiedziach Middleware bywa zwracane w kilku różnych kształtach
/// (string z HTML-em, pojedynczy obiekt { severity, diagnostics, location } lub tablica takich obiektów).
/// Ta klasa wyciąga z niego czytelny tekst niezależnie od formatu.
/// </summary>
public static class MiddlewareMessageParser
{
    public static string? ExtractText(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Undefined:
            case JsonValueKind.Null:
                return null;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Object:
                return ExtractFromObject(element);

            case JsonValueKind.Array:
                var parts = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    var text = ExtractText(item);
                    if (!string.IsNullOrWhiteSpace(text))
                        parts.Add(text);
                }
                return parts.Count > 0 ? string.Join(" | ", parts) : null;

            default:
                return element.GetRawText();
        }
    }

    private static string? ExtractFromObject(JsonElement element)
    {
        if (element.TryGetProperty("diagnostics", out var diagnostics) && diagnostics.ValueKind == JsonValueKind.String)
        {
            var severity = element.TryGetProperty("severity", out var sev) && sev.ValueKind == JsonValueKind.String
                ? sev.GetString()
                : null;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(severity))
                sb.Append('[').Append(severity).Append("] ");
            sb.Append(diagnostics.GetString());
            return sb.ToString();
        }

        return element.GetRawText();
    }
}
