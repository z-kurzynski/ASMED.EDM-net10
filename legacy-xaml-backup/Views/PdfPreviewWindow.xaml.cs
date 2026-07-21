using ASMED.WPF.Helpers;
using Syncfusion.Pdf.Parsing;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.WPF.Views
{
    public partial class PdfPreviewWindow : Window
    {
        private string ?_filePath = string.Empty;
        private bool _isPrinting = false;

        // ✅ NOWE: Dane dla wysyłania emaili
        private string ?_emailAddress = string.Empty;
        private string ?_numerFaktury = string.Empty;

        public PdfPreviewWindow()
        {
            InitializeComponent();
        }

        public void LoadFile(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show($"Plik nie istnieje: {path}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _filePath = path;
            PdfViewer.Load(path);
        }

        /// <summary>
        /// ✅ NOWA: Ładuje plik PDF wraz z metadanymi dla wysyłki email
        /// </summary>
        public void LoadFileWithMetadata(string path, string emailAddress = "", string numerFaktury = "")
        {
            LoadFile(path);
            _emailAddress = emailAddress?.Trim() ?? string.Empty;
            _numerFaktury = numerFaktury?.Trim() ?? string.Empty;

            // Pokaż/ukryj przycisk Email w zależności od dostępności danych
            if (BtnEmail != null)
            {
                BtnEmail.Visibility = !string.IsNullOrWhiteSpace(_emailAddress)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            // System.Diagnostics.Debug.WriteLine($"📧 LoadFileWithMetadata: Email={_emailAddress}, Faktura={_numerFaktury}");
        }

        public void PrintLoaded()
        {
            if (!string.IsNullOrEmpty(_filePath))
            {
                try
                {
                    PdfViewer.Print();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Błąd podczas drukowania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Brak załadowanego pliku do wydruku.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                {
                    MessageBox.Show("Brak załadowanego pliku do druku.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ Jeśli druk już się odbywa, pomiń
                if (_isPrinting)
                {
                    MessageBox.Show("Druk jest już w trakcie. Proszę czekać...", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // ✅ Pokaż CustomPrintDialog
                var customPrintDialog = new Dialogs.CustomPrintDialog();
                customPrintDialog.Owner = this;

                var documentName = System.IO.Path.GetFileNameWithoutExtension(_filePath);
                customPrintDialog.SetDocumentInfo(documentName, 1);

                if (customPrintDialog.ShowDialog() == true)
                {
                    var settings = customPrintDialog.Settings;

                    // System.Diagnostics.Debug.WriteLine($"PdfPreviewWindow: Wybrane ustawienia - {settings}");
                    // MessageBox.Show($"Wybrane ustawienia druku:\n{settings}", 
                    //     "Ustawienia Druku", MessageBoxButton.OK, MessageBoxImage.Information);

                    // ✅ Zapisz ustawienia do Properties.Settings
                    SavePrintSettings(settings);

                    // ✅ NOWE: Druk asynchronicznie w tle
                    //await PrintAsync();
                    // Stara metoda (blokująca UI)
                    // PrintLoaded();
                    // NOWA metoda drukowania z ustawieniami (bezpośrednio)

                    await PrintDirectAsync(settings);
                }
            }
            catch (System.Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"BtnPrint_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd podczas drukowania: {ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /*
                /// <summary>
                /// ✅ NOWE: Drukuje dokument asynchronicznie w tle (nie blokuje UI)
                /// </summary>
                private async Task PrintAsync()
                {
                    try
                    {
                        _isPrinting = true;
                        BtnPrint.IsEnabled = false;
                        BtnClose.IsEnabled = false;

                        // ✅ Pokaż wskaźnik postępu
                        if (this.ProgressBar2 == null)
                        {
                        }
                        else
                        {
                            this.ProgressBar2.IsIndeterminate = true;
                            this.ProgressBar2.Visibility = Visibility.Visible;
                        }

                        // System.Diagnostics.Debug.WriteLine("🖨️ START: Druk w tle (asynchronicznie)...");

                        // ✅ Uruchom druk na wątku w tle (nie blokuje UI)
                        await Task.Run(() =>
                        {
                            try
                            {
                                // Druk w tle
                                PdfViewer.Print();

                                // System.Diagnostics.Debug.WriteLine("✅ GOTOWE: Wysłano do drukarki");
                            }
                            catch (Exception)
                            {
                                // System.Diagnostics.Debug.WriteLine($"❌ Błąd druku: {ex.Message}");
                                throw;
                            }
                        });

                        // ✅ Wyświetl komunikat sukcesu
                        NotificationHelper.ShowInfo(
                            "Dokument został wysłany do drukarki.\nDruk będzie kontynuowany w tle.",
                            "Sukces"
                        );
                   }
                    catch (System.Exception ex)
                    {
                        // System.Diagnostics.Debug.WriteLine($"PrintAsync ERROR: {ex.Message}");
                        MessageBox.Show(
                            $"Błąd podczas drukowania: {ex.Message}",
                            "Błąd",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                    }
                    finally
                    {
                        // ✅ Przywróć stan UI
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
        */

        /// <summary>
        /// drukuje dokument bezpośrednio z ustawieniami
        /// 
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

                await Task.Run(() =>
                {
                    using (var pdfDoc = new PdfLoadedDocument(_filePath))
                    {
                        PrinterSettings printerSettings = new PrinterSettings();

                        // Ustaw nazwę drukarki (jeśli podana)
                        if (!string.IsNullOrEmpty(settings.PrinterName))
                            printerSettings.PrinterName = settings.PrinterName;

                        // Ustaw duplex
                        if (printerSettings.CanDuplex && settings.Duplex != Duplex.Simplex)
                        {
                            printerSettings.Duplex = settings.Duplex == Duplex.Horizontal ?
                                Duplex.Horizontal : Duplex.Vertical;
                        }

                        // Kopie
                        printerSettings.Copies = settings.Copies;

                        // Kolacjonowanie
                        printerSettings.Collate = settings.Collate;

                        // Utwórz PrintDocument
                        PrintDocument printDoc = new PrintDocument();
                        printDoc.PrinterSettings = printerSettings;

                        // ✅ Ustaw domyślny format na A4
                        foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes)
                        {
                            if (ps.Kind == PaperKind.A4)
                            {
                                printDoc.DefaultPageSettings.PaperSize = ps;
                                break;
                            }
                        }

                        //printDoc.DefaultPageSettings.Landscape = false;
                        printDoc.DefaultPageSettings.Landscape = settings.Landscape;

                        // ✅ Ustaw marginesy (opcjonalnie)
                        printDoc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(50, 20, 20, 20);

                        int pageIndex = 0;
                        printDoc.PrintPage += (sender, e) =>
                        {
                            if (pageIndex < pdfDoc.Pages.Count)
                            {
                                using (var bmp = pdfDoc.ExportAsImage(pageIndex))
                                {
                                    // ✅ KROK 1: Pobierz wymiary obszaru drukowania (bez marginesów)
                                    Rectangle printArea = e.MarginBounds; // ← ZMIENIONE z PageBounds

                                    // ✅ KROK 2: Oblicz skalowanie zachowujące proporcje
                                    float scaleX = (float)printArea.Width / bmp.Width;
                                    float scaleY = (float)printArea.Height / bmp.Height;
                                    float scale = Math.Min(scaleX, scaleY); // Fit (zachowaj proporcje)

                                    int destWidth = (int)(bmp.Width * scale);
                                    int destHeight = (int)(bmp.Height * scale);

                                    // ✅ KROK 3: Wycentruj obraz w obszarze drukowania
                                    int x = printArea.Left + (printArea.Width - destWidth) / 2;
                                    int y = printArea.Top + (printArea.Height - destHeight) / 2;

                                    // ✅ KROK 4: Rysuj obraz ze skalowaniem
                                    e.Graphics.DrawImage(bmp, x, y, destWidth, destHeight);

                                    // System.Diagnostics.Debug.WriteLine($"🖨️ Strona {pageIndex + 1}: Obraz {bmp.Width}x{bmp.Height} → {destWidth}x{destHeight} @ ({x},{y})");
                                }
                                pageIndex++;
                                e.HasMorePages = pageIndex < pdfDoc.Pages.Count;
                            }
                            else
                            {
                                e.HasMorePages = false;
                            }
                        };

                        // Drukuj
                        printDoc.Print();
                    }
                });

                NotificationHelper.ShowInfo(
                    "Dokument został wysłany do drukarki z wybranymi ustawieniami.",
                    "Sukces"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas drukowania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
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

        /// <summary>
        /// Zapisuje ustawienia druku
        /// </summary>
        public string ?PrinterName { get; set; }
        private void SavePrintSettings(Models.PrintSettings? settings)
        {
            try
            {
                var appSettings = Properties.Settings.Default;
                appSettings.PrintDuplexEnabled = settings.Duplex != Duplex.Simplex;
                appSettings.PrintDuplexMode = settings.Duplex == Duplex.Horizontal ? "Horizontal" : "Vertical";
                appSettings.PrintLandscape = settings.Landscape;
                appSettings = Properties.Settings.Default;
                appSettings.PrintCopies = settings.Copies;
                appSettings.PrintCollate = settings.Collate;
                appSettings.PrintQuality = (int)settings.Quality;
                appSettings.PrintPrinterName = settings.PrinterName;
                appSettings.Save();

                // System.Diagnostics.Debug.WriteLine("SavePrintSettings: Zapisano ustawienia druku");
            }
            catch (System.Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"SavePrintSettings ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA: Obsługa przycisku Email - wysyła PDF przez Outlook
        /// </summary>
        private void BtnEmail_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            {
                MessageBox.Show("Brak załadowanego pliku PDF do wysłania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_emailAddress))
            {
                MessageBox.Show("Brak adresu email odbiorcy.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string subject = "Medycyna Pracy";
                string body = $"Dzień Dobry\r\n\r\nW załączeniu przesyłamy dokument :";

                // ✅ Dodaj nazwę pliku zamiast numeru faktury
                string fileName = System.IO.Path.GetFileName(_filePath);
                body += $" {fileName}";

                /*
                  if (!string.IsNullOrWhiteSpace(_numerFaktury))
                  {
                      body += $" - Faktura nr {_numerFaktury}";
                      //body += $" - {_filePath}";
                  }
                */

                body += ".\r\n\r\nSerdecznie pozdrawiam\r\nNZOZ ASMED\r\nNIP: 113 03 31 776\r\nAl. Stanów Zjednoczonych 51 pok 204\r\n22 871 44 02";

                // System.Diagnostics.Debug.WriteLine($"📧 SendEmail: plik='{_filePath}', to='{_emailAddress}', faktura='{_numerFaktury}'");

                // ✅ Spróbuj Outlook interop (jeśli Outlook zainstalowany)
                try
                {
                    var outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic? app = Activator.CreateInstance(outlookType);
                        dynamic mail = app?.CreateItem(0); // 0 = olMailItem
                        mail.To = _emailAddress;
                        mail.Subject = subject;
                        mail.Body = body;

                        if (File.Exists(_filePath))
                        {
                            mail.Attachments.Add(_filePath);
                        }

                        mail.Display(false); // otwiera okno edycji maila w Outlook

                        NotificationHelper.ShowSuccess("Email został otwarty w Outlook.");
                        // System.Diagnostics.Debug.WriteLine("✅ Email otwarty w Outlook");
                        return;
                    }
                }
                catch (Exception exOutlook)
                {
                    // System.Diagnostics.Debug.WriteLine($"⚠️ Outlook interop failed: {exOutlook.Message}");
                    // nie przerywamy — pójdziemy do fallbacku mailto
                }

                // ✅ Fallback: otwarcie domyślnego klienta poczty (mailto - bez załączników)
                string mailtoUrl = $"mailto:{_emailAddress}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(mailtoUrl) { UseShellExecute = true });

                NotificationHelper.ShowWarning("Email otwarty w domyślnym kliencie poczty.\n\nUWAGA: Załącznik PDF musi być dodany ręcznie\n(protokół mailto nie obsługuje załączników).");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ SendEmail error: {ex.Message}");
                MessageBox.Show($"Błąd podczas otwierania e-maila: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
