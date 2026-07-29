using ASMED.WPF.Models;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Diagnostics;

namespace ASMED.WPF.Services
{
    /// <summary>
    /// Serwis do obs�ugi drukowania dokument�w PDF
    /// </summary>
    public class PrintService
    {
        /// <summary>
        /// Drukuje plik PDF z podanymi ustawieniami - otwiera PdfPreviewWindow z opcj� druku
        /// </summary>
        /// <param name="pdfFilePath">�cie�ka do pliku PDF</param>
        /// <param name="settings">Ustawienia druku</param>
        /// <returns>True je�li okno podgl�du zosta�o otwarte</returns>
        public static bool PrintPdf(string pdfFilePath, PrintSettings settings)
        {
            try
            {
                if (!File.Exists(pdfFilePath))
                {
                    MessageBox.Show($"Nie znaleziono pliku PDF:\n{pdfFilePath}",
                        "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Debug.WriteLine($"PrintService: Przygotowywanie do druku {pdfFilePath}");
                // Debug.WriteLine($"PrintService: Ustawienia - {settings}");

                // ? ROZWI�ZANIE: U�yj PdfPreviewWindow z zapisanymi ustawieniami
                // Zapisz ustawienia druku aby PdfPreviewWindow mog�o ich u�y�
                SavePrintSettingsToRegistry(settings);

                // Otw�rz podgl�d PDF z opcj� druku
                var previewWindow = new ASMED.WPF.Views.PdfPreviewWindow();
                previewWindow.LoadFile(pdfFilePath);

                // Ustaw w�a�ciciela okna
                if (Application.Current.MainWindow != null)
                {
                    previewWindow.Owner = Application.Current.MainWindow;
                }

                previewWindow.ShowDialog();

                // Debug.WriteLine("PrintService: ? Okno podgl�du zamkni�te");
                return true;
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"PrintService ERROR: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"B��d podczas otwierania podgl�du PDF:\n\n{ex.Message}",
                    "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Zapisuje ustawienia druku do rejestru/pliku konfiguracyjnego
        /// aby PdfPreviewWindow mog�o u�y� ich podczas drukowania
        /// </summary>
        private static void SavePrintSettingsToRegistry(PrintSettings settings)
        {
            try
            {
                // Zapisz do Settings
                var appSettings = Properties.Settings.Default;
                appSettings.PrintDuplexEnabled = settings.Duplex != Duplex.Simplex;
                appSettings.PrintDuplexMode = settings.Duplex == Duplex.Horizontal ? "Horizontal" : "Vertical";
                appSettings.PrintLandscape = settings.Landscape;
                appSettings.PrintCopies = settings.Copies;
                appSettings.PrintCollate = settings.Collate;
                appSettings.Save();

                // Debug.WriteLine($"PrintService: Zapisano ustawienia druku - Duplex={settings.Duplex}, Copies={settings.Copies}");
            }
            catch (Exception)
            {
                // Debug.WriteLine($"PrintService: B��d zapisywania ustawie�: {ex.Message}");
            }
        }

        /// <summary>
        /// Drukuje plik PDF z domy�lnymi ustawieniami
        /// </summary>
        public static bool PrintPdfWithDefaults(string pdfFilePath)
        {
            var settings = new PrintSettings
            {
                PrinterName = new PrinterSettings().PrinterName, // Domy�lna drukarka
                Duplex = Duplex.Vertical, // Druk dwustronny d�uga kraw�d�
                Landscape = false,
                Copies = 1,
                Collate = true,
                Quality = Models.PrintQuality.Normal,
                DocumentName = Path.GetFileNameWithoutExtension(pdfFilePath),
                PdfFilePath = pdfFilePath
            };

            return PrintPdf(pdfFilePath, settings);
        }

        /// <summary>
        /// Sprawdza czy podana drukarka jest dost�pna
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
        /// Zwraca domy�ln� drukark� systemow�
        /// </summary>
        public static string GetDefaultPrinter()
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
        /// Sprawdza czy drukarka obs�uguje druk dwustronny
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
}
