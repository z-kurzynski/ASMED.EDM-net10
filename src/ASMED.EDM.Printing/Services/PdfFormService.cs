using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Barcode;
using Syncfusion.Pdf;
using System.Drawing;
using System.IO;

namespace ASMED.EDM.Printing.Services;

/// <summary>
/// Wypełnia formularze PDF (AcroForm) - pola tekstowe, checkboxy, radiobuttony, oraz nanosi kod QR
/// z numerem skierowania. Port z legacy: ASMED.WPF.Helpers.PdfFormService.
/// Brak zależności na WPF/MessageBox - błędy zwracane jako <see cref="PdfFormResult"/>.
/// </summary>
public static class PdfFormService
{
    /// <summary>
    /// Wypełnia pola formularza PDF (AcroForm) i zapisuje plik wynikowy.
    /// </summary>
    /// <param name="templatePath">Ścieżka do szablonu PDF zawierającego pola</param>
    /// <param name="values">Mapa nazwa-pola -> wartość</param>
    /// <param name="outputPath">Ścieżka zapisu wypełnionego PDF</param>
    public static PdfFormResult FillForm(string templatePath, IDictionary<string, string> values, string outputPath)
    {
        if (!File.Exists(templatePath))
        {
            return PdfFormResult.Fail($"Nie znaleziono szablonu PDF: {templatePath}");
        }

        try
        {
            using var loadedDocument = new PdfLoadedDocument(templatePath);
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

            // QR Code (prawy górny róg) - tylko dla Karty badań
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
                            Text = barcodeValue,
                            XDimension = 2f,
                            ErrorCorrectionLevel = PdfErrorCorrectionLevel.Medium
                        };

                        // Pozycja: 39mm od prawej, 40mm od góry
                        float mmToPt = 2.83465f;
                        float marginRight = 39f * mmToPt;
                        float marginTop = 40f * mmToPt;
                        float qrSize = 58f;

                        var pageSize = firstLoadedPage.Size;
                        float x = pageSize.Width - marginRight - qrSize;
                        float y = marginTop;

                        qrBarcode.Draw(firstLoadedPage.Graphics, new PointF(x, y), new SizeF(qrSize, qrSize));
                    }
                }
                catch
                {
                    // Błąd generowania kodu QR nie powinien przerywać zapisu dokumentu
                }
            }

            loadedDocument.Save(outputPath);
            loadedDocument.Close(true);

            return PdfFormResult.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return PdfFormResult.Fail($"Błąd podczas wypełniania formularza PDF: {ex.Message}");
        }
    }
}
