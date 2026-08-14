using System.Drawing.Printing;
using System.IO;

namespace ASMED.EDM.Printing.Services;

public class PrintResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static PrintResult Ok() => new() { Success = true };
    public static PrintResult Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Orkiestruje drukowanie dokumentów PDF - otwiera podgląd (PdfPreviewWindow) z opcją druku.
/// Port z legacy: ASMED.WPF.Services.PrintService. Bez MessageBox - błędy zwracane jako <see cref="PrintResult"/>,
/// warstwa UI (ViewModel) decyduje jak je zaprezentować (np. INotificationService.ShowToast).
/// </summary>
public class PrintService
{
    private readonly IPrintSettingsStore _settingsStore;

    public PrintService(IPrintSettingsStore? settingsStore = null)
    {
        _settingsStore = settingsStore ?? new FilePrintSettingsStore();
    }

    /// <summary>
    /// Drukuje plik PDF z podanymi ustawieniami - otwiera <see cref="Views.PdfPreviewWindow"/> z opcją druku.
    /// </summary>
    public PrintResult PrintPdf(string pdfFilePath, Models.PrintSettings settings, System.Windows.Window? owner = null)
    {
        try
        {
            if (!File.Exists(pdfFilePath))
            {
                return PrintResult.Fail($"Nie znaleziono pliku PDF:\n{pdfFilePath}");
            }

            // Zapisz ustawienia druku aby PdfPreviewWindow mogło ich użyć
            _settingsStore.Save(settings);

            var previewWindow = new Views.PdfPreviewWindow(_settingsStore);
            previewWindow.LoadFile(pdfFilePath);

            if (owner != null)
            {
                previewWindow.Owner = owner;
            }

            previewWindow.ShowDialog();

            return PrintResult.Ok();
        }
        catch (Exception ex)
        {
            return PrintResult.Fail($"Błąd podczas otwierania podglądu PDF:\n\n{ex.Message}");
        }
    }

    /// <summary>
    /// Drukuje plik PDF z domyślnymi ustawieniami.
    /// </summary>
    public PrintResult PrintPdfWithDefaults(string pdfFilePath, System.Windows.Window? owner = null)
    {
        var settings = new Models.PrintSettings
        {
            PrinterName = new PrinterSettings().PrinterName,
            Duplex = Duplex.Vertical,
            Landscape = false,
            Copies = 1,
            Collate = true,
            Quality = Models.PrintQuality.Normal,
            DocumentName = Path.GetFileNameWithoutExtension(pdfFilePath),
            PdfFilePath = pdfFilePath
        };

        return PrintPdf(pdfFilePath, settings, owner);
    }

    /// <summary>
    /// Sprawdza czy podana drukarka jest dostępna.
    /// </summary>
    public static bool IsPrinterAvailable(string printerName)
    {
        try
        {
            var printerSettings = new PrinterSettings { PrinterName = printerName };
            return printerSettings.IsValid;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Zwraca domyślną drukarkę systemową.
    /// </summary>
    public static string? GetDefaultPrinter()
    {
        try
        {
            return new PrinterSettings().PrinterName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sprawdza czy drukarka obsługuje druk dwustronny.
    /// </summary>
    public static bool SupportsDuplex(string printerName)
    {
        try
        {
            var printerSettings = new PrinterSettings { PrinterName = printerName };
            return printerSettings.IsValid && printerSettings.CanDuplex;
        }
        catch
        {
            return false;
        }
    }
}
