extern alias PdfWpf;
extern alias XlsWpf;
using ASMED.EDM.Data.Services;
using ASMED.EDM.Printing.Services;
using ASMED.EDM.UI.Models;
using ASMED.EDM.UI.Views.Skierowania;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Input;
using PdfWpf::Syncfusion.Pdf;
using PdfWpf::Syncfusion.Pdf.Graphics;
using XlsWpf::Syncfusion.XlsIO;
using Microsoft.Win32;

namespace ASMED.EDM.UI.ViewModels.Skierowania;

/// <summary>
/// ViewModel dla widoku listy pacjentów w zakładce Skierowania.
/// Ładuje pacjentów z tabeli P_Pacjent (MySQL) wraz z liczbą kart badań.
/// </summary>
public class SkierListaPacjentowViewModel : ViewModelBase
{
    private readonly DbConnectionFactory _dbFactory;

    // ─── Kolekcje ───────────────────────────────────────────────────────────

    public ObservableCollection<PacjentSkier> Pacjenci { get; } = new();
    public ObservableCollection<PacjentSkier> PacjenciFiltered { get; } = new();

    public void PrintList(System.Collections.Generic.IEnumerable<PacjentSkier>? visibleItems = null)
    {
        try
        {
            var pacjenci = (visibleItems ?? PacjenciFiltered).ToList();

            var tempPath = Path.Combine(Path.GetTempPath(), $"Lista_pacjentow_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            using (var document = new PdfDocument())
            {
                document.PageSettings.Orientation = PdfPageOrientation.Portrait;
                var page = document.Pages.Add();
                var g = page.Graphics;
                float pageWidth = page.GetClientSize().Width;

                PdfFont titleFont;
                PdfFont headerFont;
                PdfFont normalFont;

                var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                try
                {
                    if (File.Exists(arialPath))
                    {
                        titleFont = new PdfTrueTypeFont(new Font("Arial", 14f, System.Drawing.FontStyle.Bold), true);
                        headerFont = new PdfTrueTypeFont(new Font("Arial", 9f, System.Drawing.FontStyle.Bold), true);
                        normalFont = new PdfTrueTypeFont(new Font("Arial", 8f, System.Drawing.FontStyle.Regular), true);
                    }
                    else
                    {
                        titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
                        headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold);
                        normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
                    }
                }
                catch
                {
                    titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
                    headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9, PdfFontStyle.Bold);
                    normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
                }

                const string documentName = "Lista pacjentów";
                float y = 10f;

                // Nagłówek: nazwa wydruku + ilość rekordów
                g.DrawString(documentName, titleFont, PdfBrushes.Black, new RectangleF(0, y, pageWidth, 20f));
                y += 20f;
                g.DrawString($"Liczba rekordów: {pacjenci.Count}", normalFont, PdfBrushes.Gray, new RectangleF(0, y, pageWidth, 16f));
                y += 20f;

                g.DrawLine(new PdfPen(PdfBrushes.LightGray, 0.5f), 0, y, pageWidth, y);
                y += 8f;

                // Kolumny: ID | Imię | Nazwisko | PESEL | Firma | Data do
                float[] colWidths = { 40f, 90f, 90f, 80f, 140f, 70f };
                string[] headers = { "ID", "Imię", "Nazwisko", "PESEL", "Firma", "Data do" };

                void DrawRow(string[] values, PdfFont font, PdfBrush brush)
                {
                    float x = 0f;
                    for (int i = 0; i < values.Length; i++)
                    {
                        g.DrawString(values[i] ?? string.Empty, font, brush, new RectangleF(x, y, colWidths[i], 14f));
                        x += colWidths[i];
                    }
                    y += 14f;
                }

                DrawRow(headers, headerFont, PdfBrushes.Black);
                g.DrawLine(new PdfPen(PdfBrushes.LightGray, 0.5f), 0, y, pageWidth, y);
                y += 2f;

                foreach (var p in pacjenci)
                {
                    if (y > page.GetClientSize().Height - 20f)
                    {
                        page = document.Pages.Add();
                        g = page.Graphics;
                        y = 10f;
                    }

                    var firma = p.Company ?? string.Empty;
                    if (firma.Length > 30)
                        firma = firma.Substring(0, 30) + "...";

                    DrawRow(new[]
                    {
                        p.P_ID.ToString(),
                        p.FirstName,
                        p.LastName,
                        p.PESEL,
                        firma,
                        p.BadWazneDo?.ToString("yyyy-MM-dd") ?? string.Empty
                    }, normalFont, PdfBrushes.Black);
                }

                document.Save(tempPath);
            }

            var printService = new PrintService();
            printService.PrintPdfWithDefaults(tempPath);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd podczas generowania wydruku:\n\n{ex.Message}",
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public void ExportToExcel(System.Collections.Generic.IEnumerable<PacjentSkier>? visibleItems = null)
    {
        try
        {
            var pacjenci = (visibleItems ?? PacjenciFiltered).ToList();

            var saveDialog = new SaveFileDialog
            {
                Filter = "Plik Excel (*.xlsx)|*.xlsx",
                FileName = $"Lista_pacjentow_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog() != true)
                return;

            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;
            var workbook = application.Workbooks.Create(1);
            var sheet = workbook.Worksheets[0];
            sheet.Name = "Lista pacjentów";

            // Nagłówek: nazwa wydruku + ilość rekordów
            sheet.Range["A1"].Text = "Lista pacjentów";
            sheet.Range["A1"].CellStyle.Font.Bold = true;
            sheet.Range["A1"].CellStyle.Font.Size = 14;

            sheet.Range["A2"].Text = $"Liczba rekordów: {pacjenci.Count}";
            sheet.Range["A2"].CellStyle.Font.Italic = true;

            // Nagłówki kolumn
            string[] headers = { "ID", "Imię", "Nazwisko", "PESEL", "Firma", "Data do" };
            int headerRow = 4;
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = sheet.Range[headerRow, col + 1];
                cell.Text = headers[col];
                cell.CellStyle.Font.Bold = true;
            }

            int row = headerRow + 1;
            foreach (var p in pacjenci)
            {
                sheet.Range[row, 1].Number = p.P_ID;
                sheet.Range[row, 2].Text = p.FirstName;
                sheet.Range[row, 3].Text = p.LastName;
                sheet.Range[row, 4].Text = p.PESEL;
                sheet.Range[row, 5].Text = p.Company ?? string.Empty; // pełna, nieobcięta nazwa
                sheet.Range[row, 6].Text = p.BadWazneDo?.ToString("yyyy-MM-dd") ?? string.Empty;
                row++;
            }

            sheet.UsedRange.AutofitColumns();

            workbook.SaveAs(saveDialog.FileName);

            System.Windows.MessageBox.Show("Eksport do Excela zakończony pomyślnie.",
                "Eksport", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd podczas eksportu do Excela:\n\n{ex.Message}",
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    // ─── Filtry ─────────────────────────────────────────────────────────

    public List<string> FilterTypes { get; } = new() { "All", "Imię", "Nazwisko", "PESEL", "Firma" };

    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterPacjenci();
        }
    }

    private string _activeFilterType = "All";
    public string ActiveFilterType
    {
        get => _activeFilterType;
        set
        {
            if (SetProperty(ref _activeFilterType, value))
                FilterPacjenci();
        }
    }

    // ─── Komendy ────────────────────────────────────────────────────────────

    public ICommand ClearSearchTextCommand { get; }
    public ICommand EditPatientNewCommand { get; }
    public ICommand EditPatientCommand { get; }
    public ICommand OpenHistoriaCommand { get; }
    public ICommand OpenReferralCommand { get; }

    /// <summary>
    /// Wywołane gdy użytkownik chce otworzyć edytor pacjenta w tej samej zakładce.
    /// Code-behind widoku podpina tutaj swoją implementację nawigacji.
    /// </summary>
    public Action<SkierPacjentaEditViewModel>? NavigateToEdit { get; set; }

    /// <summary>
    /// Wywołane gdy użytkownik chce otworzyć formularz nowej karty badań w tej samej zakładce.
    /// </summary>
    public Action<PacjentSkier>? NavigateToReferral { get; set; }

    // ─── Konstruktor ────────────────────────────────────────────────────────

    public SkierListaPacjentowViewModel(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;

        ClearSearchTextCommand = new RelayCommand(_ => SearchText = string.Empty);
        EditPatientNewCommand  = new RelayCommand(_ => OpenNowyPacjent());
        EditPatientCommand     = new RelayCommand(p => OpenEdytujPacjent(p));
        OpenHistoriaCommand    = new RelayCommand<object?>(OpenHistoria);
        OpenReferralCommand    = new RelayCommand(p => OpenReferral(p));

        LoadPacjenciFromDb();
    }

    // ─── Ładowanie danych ───────────────────────────────────────────────────

    /// <summary>
    /// Ładuje listę pacjentów z bazy MySQL.
    /// JOIN z B_Skierowania żeby policzyć karty badań.
    /// </summary>
    private void LoadPacjenciFromDb()
    {
        Pacjenci.Clear();

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    P.P_ID,
                    P.P_Imie,
                    P.P_Nazwisko,
                    P.P_PESEL,
                    P.P_Firma,
                    COUNT(B.B_ID) AS LiczbaKartBadan,
                    (
                        SELECT MAX(Bad.Bad_Data_Do)
                        FROM B_Skierowania BS
                        LEFT JOIN Badanie Bad ON BS.B_ID = Bad.Bad_S_ID
                        WHERE BS.B_Pacjent_ID = P.P_ID
                    ) AS BadWazneDo
                FROM
                    P_Pacjent AS P
                    LEFT JOIN B_Skierowania AS B ON P.P_ID = B.B_Pacjent_ID
                GROUP BY
                    P.P_ID, P.P_Imie, P.P_Nazwisko, P.P_PESEL, P.P_Firma
                ORDER BY
                    P.P_Nazwisko, P.P_Imie";

            using var reader = cmd.ExecuteReader();
            int lineNumber = 1;
            while (reader.Read())
            {
                Pacjenci.Add(new PacjentSkier
                {
                    LineNumber    = lineNumber++,
                    P_ID          = Convert.ToInt32(reader["P_ID"]),
                    FirstName     = reader["P_Imie"]?.ToString()    ?? string.Empty,
                    LastName      = reader["P_Nazwisko"]?.ToString() ?? string.Empty,
                    PESEL         = reader["P_PESEL"]?.ToString()    ?? string.Empty,
                    Company       = reader["P_Firma"]?.ToString()    ?? string.Empty,
                    LiczbaKartBadan = Convert.ToInt32(reader["LiczbaKartBadan"]),
                    BadWazneDo = reader["BadWazneDo"] == DBNull.Value ? null : Convert.ToDateTime(reader["BadWazneDo"])
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd ładowania listy pacjentów:\n\n{ex.Message}",
                "Błąd", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }

        FilterPacjenci();
    }

    /// <summary>
    /// Odświeża listę z bazy danych (wywoływana z przycisku ↺).
    /// </summary>
    public void RefreshList()
    {
        Pacjenci.Clear();
        PacjenciFiltered.Clear();
        LoadPacjenciFromDb();
    }

    // ─── Filtrowanie ────────────────────────────────────────────────────────

    private void FilterPacjenci()
    {
        PacjenciFiltered.Clear();

        // Normalizujemy zapytanie – użytkownik może pisać bez polskich znaków
        var query = NormalizePL(SearchText?.Trim() ?? string.Empty);

        if (string.IsNullOrEmpty(query))
        {
            foreach (var p in Pacjenci)
                PacjenciFiltered.Add(p);
            return;
        }

        foreach (var p in Pacjenci)
        {
            bool match = ActiveFilterType switch
            {
                "Imię"     => NormalizePL(p.FirstName).Contains(query, StringComparison.OrdinalIgnoreCase),
                "Nazwisko" => NormalizePL(p.LastName).Contains(query, StringComparison.OrdinalIgnoreCase),
                "PESEL"    => p.PESEL.Contains(query, StringComparison.OrdinalIgnoreCase),
                "Firma"    => NormalizePL(p.Company).Contains(query, StringComparison.OrdinalIgnoreCase),
                _          => NormalizePL(p.FullName).Contains(query, StringComparison.OrdinalIgnoreCase)
                           || p.PESEL.Contains(query, StringComparison.OrdinalIgnoreCase)
                           || NormalizePL(p.Company).Contains(query, StringComparison.OrdinalIgnoreCase)
            };

            if (match)
                PacjenciFiltered.Add(p);
        }
    }

    /// <summary>
    /// Sprowadza ciąg do wersji bez polskich znaków diakrytycznych (lowercase).
    /// Dzięki temu wpisanie "l" znajdzie "ł", "a" → "ą", "s" → "ś" itp.
    /// </summary>
    private static string NormalizePL(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input.ToLowerInvariant())
        {
            sb.Append(ch switch
            {
                'ą' => 'a',
                'ć' => 'c',
                'ę' => 'e',
                'ł' => 'l',
                'ń' => 'n',
                'ó' => 'o',
                'ś' => 's',
                'ź' => 'z',
                'ż' => 'z',
                _   => ch
            });
        }
        return sb.ToString();
    }

    // ─── Akcje przycisków ───────────────────────────────────────────────────

    private void OpenReferral(object? parameter)
    {
        if (parameter is not PacjentSkier pacjent || pacjent.P_ID <= 0)
            return;
        NavigateToReferral?.Invoke(pacjent);
    }

    private void OpenEdytujPacjent(object? parameter)
    {
        if (parameter is not PacjentSkier pacjent || pacjent.P_ID <= 0)
            return;

        try
        {
            if (System.Windows.Application.Current is not App app || app.Host == null)
                return;

            var vm = app.Host.Services.GetService(typeof(SkierPacjentaEditViewModel))
                     as SkierPacjentaEditViewModel;
            if (vm == null) return;

            vm.LoadPacjentById(pacjent.P_ID);

            NavigateToEdit?.Invoke(vm);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd otwierania edycji pacjenta:\n{ex.Message}",
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void OpenNowyPacjent(object? _ = null)
    {
        try
        {
            if (System.Windows.Application.Current is not App app || app.Host == null)
                return;

            var vm = app.Host.Services.GetService(typeof(SkierPacjentaEditViewModel))
                     as SkierPacjentaEditViewModel;
            if (vm == null) return;

            // Powiadom widok — niech otworzy edytor w tej samej zakładce
            NavigateToEdit?.Invoke(vm);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd otwierania formularza:\n{ex.Message}",
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void OpenHistoria(object? obj)
    {
        if (obj is not PacjentSkier pacjent || pacjent.P_ID <= 0)
        {
            System.Windows.MessageBox.Show("Wybierz pacjenta z listy.",
                "Informacja", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var dialog = new Views.Skierowania.PacjentHistoriaDialog(
            pacjentId: pacjent.P_ID,
            imie:      pacjent.FirstName,
            nazwisko:  pacjent.LastName,
            pesel:     pacjent.PESEL,
            firma:     pacjent.Company)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        dialog.ShowDialog();
    }
}
