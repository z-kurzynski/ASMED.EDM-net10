using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Barcode; // ✅ DODANE: Syncfusion QR Barcode
using System.IO;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using Syncfusion.Pdf;
using System;
using System.Reflection;
using ASMED.WPF.Helpers;

namespace ASMED.WPF.Helpers
{
    public static class PdfFormService
    {
        /// <summary>
        /// Wypełnia pola formularza PDF (AcroForm) i zapisuje plik wynikowy.
        /// </summary>
        /// <param name="templatePath">Ścieżka do szablonu PDF zawierającego pola lub nazwa pliku (rozwiązana względem A:\formularz\)</param>
        /// <param name="values">Mapa nazwa-pola -> wartość</param>
        /// <param name="outputPath">Ścieżka zapisu wypełnionego PDF (jeśli null/empty, zapis do A:\Karty_badan\ z timestampem)</param>
        /// <returns>Ścieżka do zapisanego pliku lub null przy błędzie</returns>
        public static string? FillForm(string templatePath, IDictionary<string, string> values, string outputPath)
        {
            if (!File.Exists(templatePath))
            {
                MessageBox.Show($"Nie znaleziono szablonu PDF: {templatePath}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            try
            {
                using (var loadedDocument = new PdfLoadedDocument(templatePath))
                {
                    var form = loadedDocument.Form as PdfLoadedForm;

                    if (form != null)
                    {
                        // Ustaw font TrueType dla pól (Arial/Helvetica fallback)
                        var font = new PdfTrueTypeFont(new Font("Helvetica", 11), true);

                        foreach (var kvp in values)
                        {
                            try
                            {
                                var name = kvp.Key;
                                var val = kvp.Value ?? string.Empty;
                                var fld = form.Fields[name];
                                if (fld == null)
                                    continue;

                                if (fld is PdfLoadedTextBoxField txt)
                                {
                                    txt.Text = val;
                                    txt.Font = font;
                                    txt.Flatten = true; // zamień na tekst (nieedytowalny)
                                }
                                else if (fld is PdfLoadedCheckBoxField chk)
                                {
                                    chk.Checked = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase) || val.Equals("yes", StringComparison.OrdinalIgnoreCase);
                                    chk.Flatten = true;
                                }
                                else if (fld is PdfLoadedRadioButtonListField rad)
                                {
                                    try { rad.Value = val; rad.Flatten = true; } catch { }
                                }
                                else
                                {
                                    try
                                    {
                                        dynamic d = fld;
                                        d.Text = val;
                                    }
                                    catch { }
                                }
                            }
                            catch
                            {
                                // ignoruj błędy pojedynczych pól
                            }
                        }
                    }

                    // Przygotuj wartość kodu kreskowego: tylko ID skierowania (header3_barcode)
                    string idVal = values != null && values.TryGetValue("header3_barcode", out var hv) ? hv : string.Empty;
                    string barcodeValue = string.IsNullOrWhiteSpace(idVal) ? string.Empty : idVal.Trim();

                    // Jeśli drukujemy Ankietę lub Orzeczenie, nie generujemy kodów
                    var tplName = Path.GetFileName(templatePath) ?? string.Empty;
                    bool skipBarcodes = tplName.Equals("ASMED_Orzeczenie.pdf", StringComparison.OrdinalIgnoreCase) ||
                                       tplName.Equals("ASMED__Sanitarne.pdf", StringComparison.OrdinalIgnoreCase) ||
                                       tplName.Equals("ASMED_Ankieta.pdf", StringComparison.OrdinalIgnoreCase);

                    if (skipBarcodes)
                    {
                        barcodeValue = string.Empty;
                    }

                    // ✅ QR Code (prawy górny róg) - tylko dla Karty badań
                    if (!string.IsNullOrWhiteSpace(barcodeValue))
                    {
                        try
                        {
                            var firstLoadedPage = loadedDocument.Pages[0] as PdfLoadedPage;
                            if (firstLoadedPage != null)
                            {
                                // Utwórz QR Barcode z ID skierowania (tylko PatientSkierowanieId)
                                var qrBarcode = new PdfQRBarcode
                                {
                                    Text = barcodeValue, // Tylko numer skierowania (PatientSkierowanieId)
                                    XDimension = 2f,     // Rozmiar pojedynczego modułu QR (w punktach)
                                    ErrorCorrectionLevel = PdfErrorCorrectionLevel.Medium
                                };

                                // ✅ ZAKTUALIZOWANA POZYCJA: 
                                // - 39mm od góry
                                // - 39mm od prawej
                                // Konwersja mm → punkty: 1mm ≈ 2.83465 punktów
                                float mmToPt = 2.83465f;
                                float marginRight = 39f * mmToPt;  // 39mm od prawej ≈ 110.55 pt
                                float marginTop = 40f * mmToPt;    // 40mm od góry ≈ 113.39 pt

                                // Rozmiar QR code
                                float qrSize = 58f; // 58pt ≈ 20.5mm

                                // Oblicz pozycję (od prawej i od góry)
                                var pageSize = firstLoadedPage.Size;
                                float x = pageSize.Width - marginRight - qrSize;  // Prawy margines
                                float y = marginTop;                              // Górny margines

                                // Rysuj QR code na stronie (używamy Graphics strony)
                                qrBarcode.Draw(firstLoadedPage.Graphics, new PointF(x, y), new SizeF(qrSize, qrSize));
                            }
                        }
                        catch (Exception)
                        {
                            // Loguj błąd, ale nie przerywaj procesu
                            // System.Diagnostics.Debug.WriteLine($"Błąd generowania QR code: {ex.Message}");
                        }
                    }

                    // ✅ BARCODE CODE128 z nazwiskiem pacjenta (dolny prawy róg)
                    string patientLastName = values != null && values.TryGetValue("PatientLastName", out var lastName) ? lastName : string.Empty;
                    if (!string.IsNullOrWhiteSpace(patientLastName) && !skipBarcodes)
                    {
                        try
                        {
                            var firstLoadedPage = loadedDocument.Pages[0] as PdfLoadedPage;
                            if (firstLoadedPage != null)
                            {
                                // ✅ ROZWIĄZANIE: Nazwisko BEZ polskich znaków w CODE128 + tekst czytelny pod spodem
                                string normalizedLastName = TextNormalizationHelper.RemovePolishDiacritics(patientLastName.ToUpper().Trim());

                                // CODE128 Barcode z nazwiskiem bez polskich znaków (100% kompatybilność ze skanerami)
                                var code128Barcode = new PdfCode128Barcode
                                {
                                    Text = normalizedLastName  // Znormalizowane nazwisko (bez ą, ć, ę...)
                                };

                                // Pozycja: dolny prawy róg
                                float mmToPt = 2.83465f;
                                float marginRight = 10f * mmToPt;   // 20mm od prawej
                                float marginBottom = 9f * mmToPt;  // 15mm od dołu (miejsce na tekst)

                                float barcodeWidth = 180f;   // 180pt ≈ 63.5mm
                                float barcodeHeight = 30f;   // 40pt ≈ 14.1mm

                                var pageSize = firstLoadedPage.Size;
                                float x = pageSize.Width - marginRight - barcodeWidth;
                                float y = pageSize.Height - marginBottom - barcodeHeight;

                                // Rysuj CODE128 barcode
                                code128Barcode.Draw(firstLoadedPage.Graphics, new System.Drawing.PointF(x, y), new System.Drawing.SizeF(barcodeWidth, barcodeHeight));

                                // ✅ DODAJ tekst z ORYGINALNYM nazwiskiem (z polskimi znakami) POD kodem kreskowym
                                try
                                {
                                    var fontFamily = new System.Drawing.FontFamily("Arial");
                                    var textFont = new PdfTrueTypeFont(new System.Drawing.Font(fontFamily, 10, System.Drawing.FontStyle.Bold), true);
                                    var textBrush = new PdfSolidBrush(System.Drawing.Color.Black);

                                    // Tekst poniżej kodu kreskowego
                                    float textY = y + barcodeHeight + 2f; // 2pt odstęp od kodu
                                    string displayText = patientLastName.ToUpper().Trim(); // Oryginalne nazwisko z polskimi znakami

                                    // Wycentruj tekst pod kodem
                                    var textSize = textFont.MeasureString(displayText);
                                    float centeredX = x + (barcodeWidth - textSize.Width) / 2;

                                    firstLoadedPage.Graphics.DrawString(displayText, textFont, textBrush, new System.Drawing.PointF(centeredX, textY));
                                }
                                catch (Exception texEx)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"Błąd rysowania tekstu pod kodem kreskowym: {texEx.Message}");
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // System.Diagnostics.Debug.WriteLine($"Błąd generowania CODE128 z nazwiskiem: {ex.Message}");
                        }
                    }

                    // Zapisz wynikowy dokument
                    loadedDocument.Save(outputPath);
                    loadedDocument.Close(true);
                }

                return outputPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wypełniania formularza PDF:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}
