using ASMED.EDM.Printing.Services;
using Syncfusion.Pdf.Parsing;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows;

namespace ASMED.EDM.Printing.Views;

/// <summary>
/// Podgląd i druk dokumentu PDF (Syncfusion PdfViewerControl).
/// Port z legacy: ASMED.WPF.Views.PdfPreviewWindow.
/// Zamiast MessageBox/NotificationHelper zgłasza zdarzenia PrintSucceeded/PrintFailed,
/// tak aby warstwa UI mogła pokazać własny toast (INotificationService).
/// </summary>
public partial class PdfPreviewWindow : Window
{
    private readonly IPrintSettingsStore _settingsStore;
    private string? _filePath = string.Empty;
    private bool _isPrinting = false;

    /// <summary>Zgłaszane po pomyślnym wysłaniu dokumentu do drukarki.</summary>
    public event Action<string>? PrintSucceeded;

    /// <summary>Zgłaszane po błędzie podczas drukowania/ładowania pliku.</summary>
    public event Action<string>? PrintFailed;

    public PdfPreviewWindow() : this(new FilePrintSettingsStore())
    {
    }

    public PdfPreviewWindow(IPrintSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        InitializeComponent();
    }

    public void LoadFile(string path)
    {
        if (!File.Exists(path))
        {
            PrintFailed?.Invoke($"Plik nie istnieje: {path}");
            return;
        }
        _filePath = path;
        PdfViewer.Load(path);
    }

    public void PrintLoaded()
    {
        if (!string.IsNullOrEmpty(_filePath))
        {
            try
            {
                PdfViewer.Print();
            }
            catch (Exception ex)
            {
                PrintFailed?.Invoke($"Błąd podczas drukowania: {ex.Message}");
            }
        }
        else
        {
            PrintFailed?.Invoke("Brak załadowanego pliku do wydruku.");
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            {
                PrintFailed?.Invoke("Brak załadowanego pliku do druku.");
                return;
            }

            if (_isPrinting)
            {
                PrintFailed?.Invoke("Druk jest już w trakcie. Proszę czekać...");
                return;
            }

            var customPrintDialog = new Dialogs.CustomPrintDialog(_settingsStore)
            {
                Owner = this
            };

            var documentName = Path.GetFileNameWithoutExtension(_filePath);
            customPrintDialog.SetDocumentInfo(documentName, 1);

            if (customPrintDialog.ShowDialog() == true)
            {
                var settings = customPrintDialog.Settings;
                if (settings != null)
                {
                    _settingsStore.Save(settings);
                    await PrintDirectAsync(settings);
                }
            }
        }
        catch (Exception ex)
        {
            PrintFailed?.Invoke($"Błąd podczas drukowania: {ex.Message}");
        }
    }

    /// <summary>
    /// Drukuje dokument bezpośrednio z podanymi ustawieniami (rasteryzacja stron do bitmapy).
    /// </summary>
    private async Task PrintDirectAsync(Models.PrintSettings settings)
    {
        try
        {
            _isPrinting = true;
            BtnPrint.IsEnabled = false;
            BtnClose.IsEnabled = false;

            if (ProgressBar2 != null)
            {
                ProgressBar2.IsIndeterminate = true;
                ProgressBar2.Visibility = Visibility.Visible;
            }

            var filePath = _filePath!;

            await Task.Run(() =>
            {
                using var pdfDoc = new PdfLoadedDocument(filePath);
                var printerSettings = new PrinterSettings();

                if (!string.IsNullOrEmpty(settings.PrinterName))
                    printerSettings.PrinterName = settings.PrinterName;

                if (printerSettings.CanDuplex && settings.Duplex != Duplex.Simplex)
                {
                    printerSettings.Duplex = settings.Duplex == Duplex.Horizontal ?
                        Duplex.Horizontal : Duplex.Vertical;
                }

                printerSettings.Copies = settings.Copies;
                printerSettings.Collate = settings.Collate;

                var printDoc = new PrintDocument
                {
                    PrinterSettings = printerSettings
                };

                foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes)
                {
                    if (ps.Kind == PaperKind.A4)
                    {
                        printDoc.DefaultPageSettings.PaperSize = ps;
                        break;
                    }
                }

                printDoc.DefaultPageSettings.Landscape = settings.Landscape;
                printDoc.DefaultPageSettings.Margins = new Margins(50, 20, 20, 20);

                int pageIndex = 0;
                printDoc.PrintPage += (sender, e) =>
                {
                    if (pageIndex < pdfDoc.Pages.Count)
                    {
                        using var bmp = pdfDoc.ExportAsImage(pageIndex);

                        Rectangle printArea = e.MarginBounds;

                        float scaleX = (float)printArea.Width / bmp.Width;
                        float scaleY = (float)printArea.Height / bmp.Height;
                        float scale = Math.Min(scaleX, scaleY);

                        int destWidth = (int)(bmp.Width * scale);
                        int destHeight = (int)(bmp.Height * scale);

                        int x = printArea.Left + (printArea.Width - destWidth) / 2;
                        int y = printArea.Top + (printArea.Height - destHeight) / 2;

                        e.Graphics!.DrawImage(bmp, x, y, destWidth, destHeight);

                        pageIndex++;
                        e.HasMorePages = pageIndex < pdfDoc.Pages.Count;
                    }
                    else
                    {
                        e.HasMorePages = false;
                    }
                };

                printDoc.Print();
            });

            PrintSucceeded?.Invoke("Dokument został wysłany do drukarki z wybranymi ustawieniami.");
        }
        catch (Exception ex)
        {
            PrintFailed?.Invoke($"Błąd podczas drukowania: {ex.Message}");
        }
        finally
        {
            _isPrinting = false;
            BtnPrint.IsEnabled = true;
            BtnClose.IsEnabled = true;
            if (ProgressBar2 != null)
            {
                ProgressBar2.IsIndeterminate = false;
                ProgressBar2.Visibility = Visibility.Collapsed;
            }
        }
    }
}
