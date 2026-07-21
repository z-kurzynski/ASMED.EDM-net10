using ASMED.WPF.Models;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Barcode;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Helper do generowania wydruków listy pacjentów na wybrany dzień
    /// Generuje PDF używając Syncfusion i zapisuje w A:\Rejestracja
    /// </summary>
    public static class WizytyPrintHelper
    {
        private const string OUTPUT_DIR = @"A:\Rejestracja";

        /// <summary>
        /// Generuje PDF z listą pacjentów i zwraca ścieżkę do pliku
        /// </summary>
        public static string GenerujPdfListyPacjentow(
            IEnumerable<RejestracjaItem> pacjenci,
            DateTime data)
        {
            try
            {
                // Upewnij się że katalog istnieje
                if (!Directory.Exists(OUTPUT_DIR))
                {
                    Directory.CreateDirectory(OUTPUT_DIR);
                }

                // ✅ ZMIENIONE: Nazwa bez godziny - pliki się nadpisują
                var fileName = $"Lista_pacjentow_{data:yyyyMMdd}.pdf";
                var fullPath = Path.Combine(OUTPUT_DIR, fileName);

                using (var document = new PdfDocument())
                {
                    var page = document.Pages.Add();
                    var g = page.Graphics;

                    // Fonty
                    PdfFont titleFont;
                    PdfFont headerFont;
                    PdfFont normalFont;
                    PdfFont boldFont;
                    PdfFont smallFont;

                    var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

                    try
                    {
                        if (File.Exists(arialPath))
                        {
                            titleFont = new PdfTrueTypeFont(new Font("Arial", 16f, FontStyle.Bold), true);
                            headerFont = new PdfTrueTypeFont(new Font("Arial", 10f, FontStyle.Bold), true);
                            normalFont = new PdfTrueTypeFont(new Font("Arial", 8f, FontStyle.Regular), true);
                            boldFont = new PdfTrueTypeFont(new Font("Arial", 9f, FontStyle.Bold), true);
                            smallFont = new PdfTrueTypeFont(new Font("Arial", 7f, FontStyle.Regular), true);
                        }
                        else
                        {
                            titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
                            headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
                            normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
                            boldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold);
                            smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 7);
                        }
                    }
                    catch
                    {
                        titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
                        headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
                        normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
                        boldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold);
                        smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 7);
                    }

                    // Marginesy minimalne
                    float margin = 14f;
                    float y = 10f;
                    float pageWidth = page.GetClientSize().Width;

                    // Kolor niebieski
                    var blueBrush = new PdfSolidBrush(new PdfColor(25, 118, 210));

                    // TYTUŁ
                    var titleRect = new RectangleF(0, y, pageWidth, 25f);
                    var centerFormat = new PdfStringFormat { Alignment = PdfTextAlignment.Center };
                    g.DrawString("📋 LISTA PACJENTÓW", titleFont, blueBrush, titleRect, centerFormat);
                    y += 28f;

                    // DATA + STATYSTYKI
                    var infoText = $"{data:dddd, dd MMMM yyyy}  |  Liczba pacjentów: {pacjenci.Count()}";
                    var infoRect = new RectangleF(0, y, pageWidth, 16f);
                    g.DrawString(infoText, normalFont, PdfBrushes.Gray, infoRect, centerFormat);
                    y += 20f;

                    // SEPARATOR
                    g.DrawLine(new PdfPen(PdfBrushes.LightGray, 0.5f), margin, y, pageWidth - margin, y);
                    y += 8f;

                    // ✅ TABELA - NOWE PROPORCJE (z kolumną Barcode)
                    var sortedPacjenci = pacjenci.OrderBy(p => p.R_GG_MM).ToList();

                    // Szerokości kolumn
                    float[] colWidths = {
                        25f,   // Lp
                        40f,   // Godz.
                        40f,   // Skier.
                        140f,  // Pacjent (2 wiersze: imię + nazwisko)
                        70f,   // QR Code
                        110f   // Barcode (nazwisko ASCII)
                    };
                    string[] headers = { "Lp", "Godz.", "Skier.", "Pacjent", "QR Code", "Barcode" };

                    // Nagłówek tabeli
                    var headerBg = new PdfSolidBrush(new PdfColor(25, 118, 210));
                    float x = margin;
                    float headerHeight = 16f;

                    for (int i = 0; i < headers.Length; i++)
                    {
                        g.DrawRectangle(headerBg, x, y, colWidths[i], headerHeight);
                        g.DrawString(headers[i], headerFont, PdfBrushes.White, x + 3, y + 3);
                        x += colWidths[i];
                    }

                    y += headerHeight + 3f;

                    // ✅ Wysokość wiersza: 40pt (było 60pt, zmniejszamy o 1/3)
                    int lp = 1;
                    foreach (var pacjent in sortedPacjenci)
                    {
                        float rowHeight = 40f; // ✅ 3 linie tekstu + padding

                        // Sprawdź nową stronę
                        if (y + rowHeight > page.GetClientSize().Height - 20)
                        {
                            page = document.Pages.Add();
                            g = page.Graphics;
                            y = 10f;

                            // Nagłówek na nowej stronie
                            x = margin;
                            for (int i = 0; i < headers.Length; i++)
                            {
                                g.DrawRectangle(headerBg, x, y, colWidths[i], headerHeight);
                                g.DrawString(headers[i], headerFont, PdfBrushes.White, x + 3, y + 3);
                                x += colWidths[i];
                            }
                            y += headerHeight + 3f;
                        }

                        x = margin;

                        // Tło naprzemienne
                        if (lp % 2 == 0)
                        {
                            var rowBg = new PdfSolidBrush(new PdfColor(248, 248, 248));
                            g.DrawRectangle(rowBg, margin, y, colWidths.Sum(), rowHeight);
                        }

                        // Ramka wiersza
                        g.DrawRectangle(new PdfPen(new PdfColor(230, 230, 230), 0.5f), margin, y, colWidths.Sum(), rowHeight);

                        // Lp - wyśrodkowany
                        g.DrawString(lp.ToString(), normalFont, PdfBrushes.Black, x + 8, y + (rowHeight / 2) - 4);
                        x += colWidths[0];

                        // Godzina - bold
                        g.DrawString(pacjent.GodzinaFormatted ?? "-", boldFont, PdfBrushes.Black, x + 3, y + (rowHeight / 2) - 4);
                        x += colWidths[1];

                        // ✅ Skierowanie z prefiksem # (MAX 6 cyfr)
                        var skierNumer = pacjent.SkierowanieNumer ?? "0";
                        if (skierNumer.Length > 6) skierNumer = skierNumer.Substring(0, 6);
                        var skierText = $"#{skierNumer}";
                        g.DrawString(skierText, normalFont, blueBrush, x + 3, y + (rowHeight / 2) - 4);
                        x += colWidths[2];

                        // ✅ Pacjent - 2 wiersze (Imię + Nazwisko BOLD)
                        var imie = pacjent.P_Imie ?? "";
                        var nazwisko = pacjent.P_Nazwisko ?? "";

                        // Wiersz 1: Imię (normalFont)
                        g.DrawString(imie, normalFont, PdfBrushes.Black, x + 3, y + 5);

                        // Wiersz 2: Nazwisko (większa czcionka + bold)
                        g.DrawString(nazwisko, boldFont, PdfBrushes.Black, x + 3, y + 20);

                        x += colWidths[3];

                        // ✅ QR CODE - bez zmian
                        try
                        {
                            var qrData = $"#{skierNumer}";
                            var qrBarcode = new PdfQRBarcode();
                            qrBarcode.ErrorCorrectionLevel = PdfErrorCorrectionLevel.Medium;
                            qrBarcode.XDimension = 1.2f;
                            qrBarcode.Text = qrData;

                            float qrSize = 35f;
                            float qrX = x + (colWidths[4] - qrSize) / 2;
                            float qrY = y + (rowHeight - qrSize) / 2;

                            qrBarcode.Draw(g, new PointF(qrX, qrY), new SizeF(qrSize, qrSize));
                        }
                        catch (Exception)
                        {
                            // System.Diagnostics.Debug.WriteLine($"⚠️ QR Error: {ex.Message}");
                            var qrRect = new RectangleF(x + 18, y + 5, 35, 35);
                            g.DrawRectangle(new PdfPen(blueBrush, 1f), qrRect);
                            g.DrawString("QR", smallFont, PdfBrushes.Gray, x + 28, y + 18);
                        }
                        x += colWidths[4];

                        // ✅ BARCODE - nazwisko bez polskich liter (Code128)
                        try
                        {
                            var nazwiskoAscii = RemovePolishChars(nazwisko);
                            if (!string.IsNullOrWhiteSpace(nazwiskoAscii))
                            {
                                var barcode = new PdfCode128Barcode();
                                barcode.Text = nazwiskoAscii.ToUpper();

                                // Rozmiar barcode (dopasowany do kolumny)
                                float barcodeWidth = colWidths[5] - 6;
                                float barcodeHeight = 28f;
                                float barcodeX = x + 3;
                                float barcodeY = y + (rowHeight - barcodeHeight) / 2;

                                barcode.Draw(g, new PointF(barcodeX, barcodeY), new SizeF(barcodeWidth, barcodeHeight));
                            }
                        }
                        catch (Exception)
                        {
                            // System.Diagnostics.Debug.WriteLine($"⚠️ Barcode Error: {ex.Message}");
                            // Fallback - tekst
                            var nazwiskoAscii = RemovePolishChars(nazwisko);
                            g.DrawString(nazwiskoAscii, smallFont, PdfBrushes.Gray, x + 3, y + (rowHeight / 2) - 4);
                        }

                        y += rowHeight;
                        lp++;
                    }

                    // STOPKA
                    y = page.GetClientSize().Height - 15;
                    var footerText = $"Wydrukowano: {DateTime.Now:dd.MM.yyyy HH:mm} | ASMED";
                    var footerRect = new RectangleF(0, y, pageWidth, 12f);
                    g.DrawString(footerText, smallFont, PdfBrushes.Gray, footerRect, centerFormat);

                    // Zapisz
                    using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                    {
                        document.Save(fs);
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"✅ Wygenerowano PDF: {fullPath}");
                return fullPath;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd generowania PDF: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Pokazuje podgląd PDF w PdfPreviewWindow
        /// </summary>
        public static void ShowPrintPreview(string pdfPath, string title = "Podgląd wydruku")
        {
            try
            {
                if (File.Exists(pdfPath))
                {
                    var preview = new ASMED.WPF.Views.PdfPreviewWindow();
                    preview.LoadFile(pdfPath);

                    // ✅ DODANE: Ustaw okno na wierzchu i wyśrodkuj
                    preview.Owner = System.Windows.Application.Current.MainWindow;
                    preview.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    preview.Topmost = true; // Na wierzchu przy pierwszym wyświetleniu

                    preview.ShowDialog();
                }
                else
                {
                    System.Windows.MessageBox.Show($"Plik nie istnieje: {pdfPath}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd otwierania podglądu: {ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Usuwa polskie znaki diakrytyczne z tekstu (dla barcode)
        /// </summary>
        private static string RemovePolishChars(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text
                .Replace("ą", "a").Replace("Ą", "A")
                .Replace("ć", "c").Replace("Ć", "C")
                .Replace("ę", "e").Replace("Ę", "E")
                .Replace("ł", "l").Replace("Ł", "L")
                .Replace("ń", "n").Replace("Ń", "N")
                .Replace("ó", "o").Replace("Ó", "O")
                .Replace("ś", "s").Replace("Ś", "S")
                .Replace("ź", "z").Replace("Ź", "Z")
                .Replace("ż", "z").Replace("Ż", "Z")
                .Trim();
        }
    }
}
