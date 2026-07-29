using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ASMED.WPF.Models;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.Drawing;
using System.Globalization;

namespace ASMED.WPF.Services
{
    public enum ExportType
    {
        Lista = 1, // Lp, ImiÍ, Nazwisko
        Podstawowy = 2, // Lp, ImiÍ, Nazwisko, Razem
        Szczegolowy = 3  // Lp, ImiÍ, Nazwisko, Cena Bad, Cena Lar, Cena Ok, Cena San, Inne, Razem
    }

    public class PdfExportService
    {
        private const string EXPORT_PATH = @"A:\Email\OUT";

        public string ExportToPdf(List<InvoiceItem> items, ExportType exportType, string invoiceNumber, string companyName, DateTime? invoiceDate)
        {
            try
            {
                Directory.CreateDirectory(EXPORT_PATH);
                string fileName = GenerateFileName(exportType, invoiceNumber, companyName);
                string fullPath = Path.Combine(EXPORT_PATH, fileName);

                using (var document = new PdfDocument())
                {
                    var page = document.Pages.Add();
                    var g = page.Graphics;

                    // fonts - prefer TrueType (Arial) to support Polish diacritics
                    PdfFont headerFont;
                    PdfFont titleFont;
                    PdfFont font;
                    PdfFont bold;
                    // prepare arial path in outer scope so it's available everywhere
                    var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    try
                    {
                        // If Arial is available, create PdfTrueTypeFont from System.Drawing.Font to ensure Polish characters
                        if (File.Exists(arialPath))
                        {
                            font = new PdfTrueTypeFont(new Font("Arial", 10f, FontStyle.Regular), true);
                            bold = new PdfTrueTypeFont(new Font("Arial", 11f, FontStyle.Bold), true);
                            headerFont = new PdfTrueTypeFont(new Font("Arial", 11f, FontStyle.Regular), true);
                            titleFont = new PdfTrueTypeFont(new Font("Arial", 14f, FontStyle.Bold), true);
                        }
                        else
                        {
                            font = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                            bold = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
                            headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12);
                            titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16);
                        }
                    }
                    catch
                    {
                        // fallback
                        font = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
                        bold = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
                        headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12);
                        titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16);
                    }

                    // table font:1px smaller than main font
                    PdfFont tableFont;
                    // footer font:9px (italic)
                    PdfFont footerFont;
                    try
                    {
                        if (File.Exists(arialPath))
                        {
                            tableFont = new PdfTrueTypeFont(new Font("Arial", 9f, FontStyle.Regular), true);
                            footerFont = new PdfTrueTypeFont(new Font("Arial", 9f, FontStyle.Italic), true);
                        }
                        else
                        {
                            tableFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                            footerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Italic);
                        }
                    }
                    catch
                    {
                        tableFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
                        footerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Italic);
                    }

                    float margin = 20f;
                    float y = 20f;

                    // Title large at top (marine color, size30) centered
                    PdfFont titleFontLarge;
                    try
                    {
                        if (File.Exists(arialPath))
                            titleFontLarge = new PdfTrueTypeFont(new Font("Arial", 30f, FontStyle.Bold), true);
                        else
                            titleFontLarge = new PdfStandardFont(PdfFontFamily.Helvetica, 30);
                    }
                    catch
                    {
                        titleFontLarge = new PdfStandardFont(PdfFontFamily.Helvetica, 30);
                    }

                    //var titleText = "LISTA PACJENT”W";
                    var titleBrushMarine = new PdfSolidBrush(new PdfColor(30, 60, 120));
                    // draw centered title using PdfStringFormat
                    var pageW = page.GetClientSize().Width;
                    var titleRect = new System.Drawing.RectangleF(0, y, pageW, 40f);
                    var titleFormat = new PdfStringFormat();
                    titleFormat.Alignment = PdfTextAlignment.Center;
                    //g.DrawString(titleText, titleFontLarge, titleBrushMarine, titleRect, titleFormat);
                    // advance y below title
                    //y +=40f +8f;

                    // Header: two equal columns, left-aligned in each
                    float pageWidth = page.GetClientSize().Width;
                    float usableWidth = pageWidth - 2 * margin;
                    float colWidth = usableWidth / 2;
                    float leftX = margin;
                    float rightColX = margin + colWidth;

                    // colored brush for titles (30,60,120) using PdfColor
                    var titleBrush = new PdfSolidBrush(new PdfColor(30, 60, 120));

                    // Left column content
                    float leftY = y;
                    g.DrawString("ASMED SC", bold, titleBrush, leftX, leftY);
                    leftY += 16;
                    g.DrawString("04-028 Warszawa", font, PdfBrushes.Black, leftX, leftY);
                    leftY += 12;
                    g.DrawString("AL. StanÛw Zjednoczonych51 pok. 204", font, PdfBrushes.Black, leftX, leftY);
                    leftY += 12;
                    g.DrawString("tel:22 841 44 02", font, PdfBrushes.Black, leftX, leftY);
                    leftY += 12;
                    g.DrawString("email: nzoz@asmed.info.pl", font, PdfBrushes.Black, leftX, leftY);

                    // Right column content (left-aligned inside right column)
                    float rightY = y;
                    g.DrawString($"Nr Faktury: {invoiceNumber}", bold, titleBrush, rightColX, rightY);
                    rightY += 14;

                    // Draw invoice date (if provided)
                    if (invoiceDate.HasValue)
                    {
                        g.DrawString($"Data faktury: {invoiceDate.Value:dd.MM.yyyy}", font, PdfBrushes.Black, rightColX, rightY);
                        rightY += 14;
                    }

                    // layout company name with wrapping inside right column
                    var companyText = companyName ?? string.Empty;
                    var label = "Dla: ";
                    var layouter = new PdfStringLayouter();
                    var fmt = new PdfStringFormat() { Alignment = PdfTextAlignment.Left, LineAlignment = PdfVerticalAlignment.Top };
                    var layoutResult = layouter.Layout(label + companyText, font, fmt, new System.Drawing.SizeF(colWidth - 8, page.GetClientSize().Height));
                    var rect = new System.Drawing.RectangleF(rightColX, rightY, colWidth - 8, layoutResult.ActualSize.Height);
                    g.DrawString(label + companyText, font, PdfBrushes.Black, rect, fmt);
                    rightY += layoutResult.ActualSize.Height;

                    // advance y to max of leftY and rightY plus spacing
                    y = Math.Max(leftY, rightY) + 8;

                    // leave a small gap between header and table title
                    // draw small section title (kept previously but smaller)
                    // use headerFont for subtitle
                    y += 6f;
                    // Subtitle already drawn as large main title; we optionally show a smaller label above the table
                    g.DrawString("Lista pacjentÛw", headerFont, PdfBrushes.Black, margin, y);
                    y += 18f;

                    // Build table as real table with header background
                    // prepare header background brush and header text brush
                    var headerBg = new PdfSolidBrush(new PdfColor(30, 60, 120));
                    var headerTextBrush = PdfBrushes.White;

                    // helper to draw table given column weights and headers and row value selectors
                    void DrawTable(float[] colWeights, string[] headers, Func<InvoiceItem, string[]> rowSelector)
                    {
                        float usableW = usableWidth; // from earlier
                        float weightSum = colWeights.Sum();
                        float scale = usableW / weightSum;
                        float[] colWidths = colWeights.Select(w => w * scale).ToArray();

                        float xStart = margin;
                        float x = xStart;
                        float headerH = 18f;
                        // draw header background per column and header text
                        for (int i = 0; i < headers.Length; i++)
                        {
                            float cw = colWidths[i];
                            try { g.DrawRectangle(headerBg, x, y, cw, headerH); } catch { /* best-effort */ }
                            // text padding4
                            g.DrawString(headers[i], bold, headerTextBrush, x + 4, y + 3);
                            x += cw;
                        }
                        y += headerH + 6;

                        int lp = 1;
                        foreach (var item in items)
                        {
                            x = xStart;
                            var cols = rowSelector(item);
                            // ensure cols length matches headers
                            if (cols.Length != colWidths.Length)
                            {
                                // try to resize to fit (pad with empty strings)
                                var tmp = new string[colWidths.Length];
                                for (int k = 0; k < tmp.Length; k++) tmp[k] = k < cols.Length ? cols[k] : string.Empty;
                                cols = tmp;
                            }

                            // if first header is Lp, set it to current lp
                            if (headers.Length > 0 && headers[0].StartsWith("Lp", StringComparison.OrdinalIgnoreCase))
                            {
                                cols[0] = lp.ToString();
                            }

                            for (int i = 0; i < cols.Length; i++)
                            {
                                var text = cols[i] ?? string.Empty;
                                // treat numeric zero values as empty (handles "0", "0.00", "0,00")
                                bool isZero = false;
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var dv))
                                        isZero = dv == 0m;
                                    else if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out dv))
                                        isZero = dv == 0m;
                                }



                                // Right-align numeric price values (except Lp column index 0)
                                bool isNumeric = false;
                                decimal parsed;
                                if (i != 0 && !string.IsNullOrWhiteSpace(text))
                                {
                                    isNumeric = decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed)
                                                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed);
                                }

                                if (isNumeric)
                                {
                                    // measure and draw right-aligned with small padding
                                    var size = tableFont.MeasureString(text);
                                    float tx = x + colWidths[i] - 4f - size.Width;
                                    g.DrawString(text, tableFont, PdfBrushes.Black, tx, y);
                                }
                                else
                                {
                                    g.DrawString(text, tableFont, PdfBrushes.Black, x + 4, y);
                                }

                                x += colWidths[i];
                            }
                            y += 16;
                            if (y > page.GetClientSize().Height - 40)
                            {
                                page = document.Pages.Add();
                                g = page.Graphics;
                                y = 20;
                            }
                            lp++;
                        }
                    }

                    if (exportType == ExportType.Podstawowy)
                    {
                        // przesuniÍto kolumnÍ 'Typ' za Lp i ustawiono szerokoúÊ ~20f (weight 20f)
                        float[] weights = new float[] { 40f, 30f, 400f, 80f };
                        string[] hdrs = new string[] { "Lp.", "", "ImiÍ i Nazwisko", "WartoúÊ" };
                        DrawTable(weights, hdrs, itm => new string[] {
                            string.Empty,
                            itm.BadType ?? string.Empty,
                            // po≥πczenie imiÍ + "  " + nazwisko, obciÍcie do 400f-equivalent d≥ugoúci (tu 80 znakÛw jako bezpieczeÒstwo)
                            ((itm.FirstName ?? string.Empty) + "  " + (itm.LastName ?? string.Empty)).Length > 80
                                ? ((itm.FirstName ?? string.Empty) + "  " + (itm.LastName ?? string.Empty)).Substring(0, 80)
                                : (itm.FirstName ?? string.Empty) + "  " + (itm.LastName ?? string.Empty),
                            PdfExportService.FormatPrice(itm.Total)
                        });
                    }
                    else if (exportType == ExportType.Szczegolowy)
                    {
                        // przesuniÍto kolumnÍ 'Typ' za Lp i ustawiono szerokoúÊ ~20f (weight 20f)
                        float[] weights2 = new float[] { 30f, 30f, 300f, 80f, 70f, 70f, 70f, 70f, 70f, 80f };
                        string[] hdrs2 = new string[] { "Lp.", "", "ImiÍ i Nazwisko", "Lek.MP", "Laryn.", "Okul.", "Ksiaø.", "Lip.", "EKG", "Razem" };
                        DrawTable(weights2, hdrs2, itm => new string[] {
                            string.Empty,
                            itm.BadType ?? string.Empty,
                            ((itm.FirstName ?? string.Empty) + "  " + (itm.LastName ?? string.Empty)).Length > 80
                                ? ((itm.FirstName ?? string.Empty) + "  " + (itm.LastName ?? string.Empty)).Substring(0, 80)
                                : (itm.FirstName ?? string.Empty) + "  " + (itm.LastName ?? string.Empty),
                            PdfExportService.FormatPrice(itm.ExaminationPrice),
                            PdfExportService.FormatPrice(itm.LaryngologistPrice),
                            PdfExportService.FormatPrice(itm.OphthalmologistPrice),
                            PdfExportService.FormatPrice(itm.SanitaryPrice),
                            PdfExportService.FormatPrice(itm.LipidogramPrice),
                            PdfExportService.FormatPrice(itm.EKGPrice),
                            PdfExportService.FormatPrice(itm.Total)
                        });
                    }
                    else // Lista
                    {
                        // przesuniÍto kolumnÍ 'Typ' za Lp i ustawiono szerokoúÊ ~20f (weight 20f)
                        float[] weights3 = new float[] { 30f, 30f, 400f };
                        string[] hdrs3 = new string[] { "Lp.", "", "ImiÍ i Nazwisko" };
                        DrawTable(weights3, hdrs3, itm => new string[] {
                            string.Empty,
                            itm.BadType ?? string.Empty,
                            ((itm.FirstName ?? string.Empty) + "  " + (itm.LastName ?? string.Empty)).Length > 80
                                ? ((itm.FirstName ?? string.Empty) + "  " + (itm.LastName ?? string.Empty)).Substring(0, 80)
                                : (itm.FirstName ?? string.Empty) + "  " + (itm.LastName ?? string.Empty)
                        });
                    }

                    // total for financial reports
                    if (exportType == ExportType.Podstawowy || exportType == ExportType.Szczegolowy)
                    {
                        decimal totalAmount = items.Sum(x => x.Total);
                        y += 12;
                        g.DrawString($"SUMA CA£KOWITA: {totalAmount:N2} z≥", bold, PdfBrushes.Black, margin, y);
                    }

                    // footer
                    y = page.GetClientSize().Height - 30;
                    g.DrawString($" ASMED System Medycyna Pracy | Nr raportu: {invoiceNumber} | Data wydruku: {DateTime.Now:dd.MM.yyyy}", footerFont, PdfBrushes.Black, margin, y);

                    using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                    {
                        document.Save(fs);
                    }
                }

                return fullPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"B≥πd podczas eksportu PDF: {ex.Message}", ex);
            }
        }

        private string GenerateFileName(ExportType exportType, string invoiceNumber, string companyName)
        {
            string shortCompanyName = companyName ?? "brak_firmy";
            int nlIndex = shortCompanyName.IndexOfAny(new char[] { '\n', '\r' });
            if (nlIndex >= 0)
                shortCompanyName = shortCompanyName.Substring(0, nlIndex);
            shortCompanyName = shortCompanyName.Trim();
            string safeInvoiceNumber = (invoiceNumber ?? "brak").Replace('/', '_');
            string baseFileName = string.Format("lista_do_{0}_dla_{1}", safeInvoiceNumber, shortCompanyName);

            string forbiddenChars = "/\\?%*:|\"<>";
            string cleanFileName = baseFileName;
            foreach (char c in forbiddenChars)
            {
                cleanFileName = cleanFileName.Replace(c, '_');
            }
            cleanFileName = cleanFileName.Replace(' ', '_');

            string[] polish = { "π", "Ê", "Í", "≥", "Ò", "Û", "ú", "ü", "ø", "•", "∆", " ", "£", "—", "”", "å", "è", "Ø" };
            string[] ascii = { "a", "c", "e", "l", "n", "o", "s", "z", "z", "A", "C", "E", "L", "N", "O", "S", "Z", "Z" };
            for (int i = 0; i < polish.Length; i++)
                cleanFileName = cleanFileName.Replace(polish[i], ascii[i]);

            while (cleanFileName.Contains("__"))
            {
                cleanFileName = cleanFileName.Replace("__", "_");
            }

            cleanFileName = cleanFileName.Trim('_');
            return cleanFileName + ".pdf";
        }

        // Helper for formatting prices in PDF: empty for zero
        private static string FormatPrice(decimal v)
        {
            return v == 0m ? string.Empty : v.ToString("N2");
        }
    }
}
