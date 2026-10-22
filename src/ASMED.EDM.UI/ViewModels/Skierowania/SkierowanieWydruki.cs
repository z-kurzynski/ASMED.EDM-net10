using ASMED.EDM.Printing.Models;
using ASMED.EDM.Printing.Services;
using System.Globalization;
using System.Windows;

namespace ASMED.EDM.UI.ViewModels.Skierowania;

/// <summary>
/// Wspólna obsługa wydruków formularzy (Karta/Orzeczenie/Sanitarne/Ankieta/Komplet)
/// dla widoków karty badań – ta sama ścieżka co w zakładce Rejestracja (VisitsViewModel).
/// </summary>
public static class SkierowanieWydruki
{
    private static readonly PatientFormPrintService PrintService = new();

    public static void Print(PatientPrintData data, string templateFileName)
    {
        if (data.B_ID is not > 0)
        {
            MessageBox.Show("Karta badan musi byc najpierw zapisana, aby wydrukowac formularz.",
                "Wydruk", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = PrintService.ShowPreview(data, templateFileName, Application.Current?.MainWindow);
        if (!result.Success)
        {
            MessageBox.Show($"Blad wydruku: {result.ErrorMessage}",
                "Wydruk", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static void PrintUrlop()
    {
        // Brak szablonu urlopowego – w Rejestracji (VisitsViewModel.PrintUrlop) również TODO
        MessageBox.Show("Wydruk urlopowy nie jest jeszcze dostepny.",
            "Wydruk", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>Parsuje datę rejestracji wyświetlaną w formacie "dd.MM.yyyy HH:mm".</summary>
    public static DateTime? ParseRegistrationDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        return DateTime.TryParseExact(text.Trim(), new[] { "dd.MM.yyyy HH:mm", "dd.MM.yyyy" },
                   CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
               || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt)
            ? dt
            : null;
    }

    public static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
