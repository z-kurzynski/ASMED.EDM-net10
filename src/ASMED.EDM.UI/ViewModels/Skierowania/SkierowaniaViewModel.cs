extern alias PdfWpf;
extern alias XlsWpf;
using ASMED.EDM.Data.Services;
using ASMED.EDM.Printing.Services;
using ASMED.EDM.UI.Models;
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
/// ViewModel dla widoku listy skierowań (kart badań) - SkierowaniaView.
/// Ładuje skierowania z tabeli B_Skierowania (MySQL) wraz z powiązanymi danymi
/// pacjenta, firmy, badania oraz faktury.
/// </summary>
public class SkierowaniaViewModel : ViewModelBase
{
    private readonly DbConnectionFactory _dbFactory;

    // ─── Kolekcje ─────────────────────────────────────────────

    public ObservableCollection<SkierowanieListItem> Skierowania { get; } = new();
    public ObservableCollection<SkierowanieListItem> SkierowaniaFiltered { get; } = new();

    // ─── Filtry ───────────────────────────────────────────────

    public List<string> FilterTypes { get; } = new() { "All", "Imię", "Nazwisko", "PESEL", "Firma", "ID" };

    public List<string> DateFilterOptions { get; } = new()
    {
        "All",
        "Bieżący Miesiąc",
        "Poprzedni Miesiąc",
        "Bieżący Rok",
        "Poprzedni Rok",
        "Wybrany okres"
    };

    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterSkierowania();
        }
    }

    private string _activeFilterType = "All";
    public string ActiveFilterType
    {
        get => _activeFilterType;
        set
        {
            if (SetProperty(ref _activeFilterType, value))
                FilterSkierowania();
        }
    }

    private string _selectedDateFilter = "All";
    public string SelectedDateFilter
    {
        get => _selectedDateFilter;
        set
        {
            if (SetProperty(ref _selectedDateFilter, value))
            {
                OnPropertyChanged(nameof(IsCustomDateRangeVisible));
                FilterSkierowania();
            }
        }
    }

    private DateTime? _filterDateFrom;
    public DateTime? FilterDateFrom
    {
        get => _filterDateFrom;
        set
        {
            if (SetProperty(ref _filterDateFrom, value) && SelectedDateFilter == "Wybrany okres")
                FilterSkierowania();
        }
    }

    private DateTime? _filterDateTo;
    public DateTime? FilterDateTo
    {
        get => _filterDateTo;
        set
        {
            if (SetProperty(ref _filterDateTo, value) && SelectedDateFilter == "Wybrany okres")
                FilterSkierowania();
        }
    }

    public bool IsCustomDateRangeVisible => SelectedDateFilter == "Wybrany okres";

    // ─── Komendy ──────────────────────────────────────────────

    public ICommand ClearSearchTextCommand { get; }
    public ICommand ApplyCustomDateFilterCommand { get; }
    public ICommand EditSkierowanieCommand { get; }
    public ICommand OpenSkierPacjWoborCommand { get; }
    public ICommand ToggleRejestracjaCommand { get; }

    /// <summary>
    /// Wywoływane po kliknięciu "Dodaj Kartę Badań" - code-behind widoku podpina tutaj nawigację.
    /// Parametr: wartość pola wyszukiwania do przepisania do zakładki Nowa Karta.
    /// </summary>
    public Action<string>? NavigateToNowaKartaBadan { get; set; }

    /// <summary>
    /// Wywoływane po kliknięciu przycisku edycji wiersza - przekazuje wybrane skierowanie.
    /// </summary>
    public Action<SkierowanieListItem>? NavigateToEdit { get; set; }

    /// <summary>
    /// Wywoływane po udanym INSERT do Rejestracja — code-behind podpina przełączenie zakładki i ustawienie filtra.
    /// Parametr: Nazwisko pacjenta do wpisania jako filtr w zakładce Rejestracja.
    /// </summary>
    public Action<string>? NavigateToRejestracja { get; set; }

    // ─── Konstruktor ──────────────────────────────────────────

    public SkierowaniaViewModel(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;

        ClearSearchTextCommand = new RelayCommand<object?>(_ => SearchText = string.Empty);
        ApplyCustomDateFilterCommand = new RelayCommand<object?>(_ => FilterSkierowania());
        EditSkierowanieCommand = new RelayCommand<object?>(EditSkierowanie);
        OpenSkierPacjWoborCommand = new RelayCommand<object?>(_ => NavigateToNowaKartaBadan?.Invoke(SearchText ?? string.Empty));
        ToggleRejestracjaCommand = new RelayCommand<object?>(ExecuteToggleRejestracja);

        LoadFromDb();
    }

    // ─── Ładowanie danych ─────────────────────────────────────

    private void LoadFromDb()
    {
        Skierowania.Clear();

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    b.B_ID,
                    b.B_Pacjent_ID,
                    b.B_TypBadania,
                    b.B_DataSkierowania,
                    p.P_Imie,
                    p.P_Nazwisko,
                    p.P_PESEL,
                    f.Nazwa AS Firma_Nazwa,
                    bad.Bad_Data,
                    bad.Bad_Data_Do,
                    reg.R_Data,
                    reg.R_Status,
                    fk.FK_Numer,
                    b.B_BadanieWstepne,
                    b.B_BadanieOkresowe,
                    b.B_BadanieKontrolne,
                    b.B_laryngolog,
                    b.B_okulista,
                    b.B_Urlop,
                    b.B_Wywiad,
                    b.B_Orzeczenie,
                    b.B_EDM
                FROM B_Skierowania b
                INNER JOIN P_Pacjent p ON p.P_ID = b.B_Pacjent_ID
                LEFT JOIN Firma f ON f.id = p.P_Firma_id
                LEFT JOIN Badanie bad ON bad.Bad_ID = b.B_Badanie_ID
                LEFT JOIN Faktura fk ON fk.FK_ID = bad.Bad_F_ID
                LEFT JOIN (SELECT R_S_ID, MAX(R_Data) AS R_Data, R_Status FROM Rejestracja GROUP BY R_S_ID, R_Status) reg
                    ON reg.R_S_ID = b.B_ID
                WHERE b.B_Activ = 1
                ORDER BY b.B_ID DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Skierowania.Add(new SkierowanieListItem
                {
                    Id = reader["B_ID"] is DBNull ? 0 : Convert.ToInt32(reader["B_ID"]),
                    PacjentId = reader["B_Pacjent_ID"] is DBNull ? 0 : Convert.ToInt32(reader["B_Pacjent_ID"]),
                    Typ = reader["B_TypBadania"]?.ToString() ?? string.Empty,
                    Imie = reader["P_Imie"]?.ToString() ?? string.Empty,
                    Nazwisko = reader["P_Nazwisko"]?.ToString() ?? string.Empty,
                    Pesel = reader["P_PESEL"]?.ToString() ?? string.Empty,
                    Firma = reader["Firma_Nazwa"]?.ToString() ?? string.Empty,
                    Data = reader["B_DataSkierowania"] is DBNull ? null : Convert.ToDateTime(reader["B_DataSkierowania"]),
                    DataRej = reader["R_Data"] is DBNull ? null : Convert.ToDateTime(reader["R_Data"]),
                    DataBad = reader["Bad_Data"] is DBNull ? null : Convert.ToDateTime(reader["Bad_Data"]),
                    DataDo = reader["Bad_Data_Do"] is DBNull ? null : Convert.ToDateTime(reader["Bad_Data_Do"]),
                    StatusBad = reader["R_Status"]?.ToString() ?? string.Empty,
                    NumerFaktury = reader["FK_Numer"]?.ToString(),
                    BadanieWstepne = reader["B_BadanieWstepne"] is DBNull ? false : Convert.ToBoolean(reader["B_BadanieWstepne"]),
                    BadanieOkresowe = reader["B_BadanieOkresowe"] is DBNull ? false : Convert.ToBoolean(reader["B_BadanieOkresowe"]),
                    BadanieKontrolne = reader["B_BadanieKontrolne"] is DBNull ? false : Convert.ToBoolean(reader["B_BadanieKontrolne"]),
                    Laryngolog = reader["B_laryngolog"] is DBNull ? false : Convert.ToBoolean(reader["B_laryngolog"]),
                    Okulista = reader["B_okulista"] is DBNull ? false : Convert.ToBoolean(reader["B_okulista"]),
                    Urlop = reader["B_Urlop"] is DBNull ? false : Convert.ToBoolean(reader["B_Urlop"]),
                    Wywiad = reader["B_Wywiad"] is DBNull ? false : Convert.ToBoolean(reader["B_Wywiad"]),
                    Orzeczenie = reader["B_Orzeczenie"] is DBNull ? false : Convert.ToBoolean(reader["B_Orzeczenie"]),
                    B_EDM = reader["B_EDM"] is DBNull ? false : Convert.ToBoolean(reader["B_EDM"])
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd ładowania listy skierowań:\n\n{ex.Message}",
                "Błąd", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }

        FilterSkierowania();
    }

    /// <summary>
    /// Odświeża listę z bazy danych (wywoływana z F1 lub przy pokazaniu zakładki).
    /// </summary>
    public void RefreshFromDb()
    {
        LoadFromDb();
    }

    public void PrintList(System.Collections.Generic.IEnumerable<SkierowanieListItem>? visibleItems = null)
    {
        try
        {
            var items = (visibleItems ?? SkierowaniaFiltered).ToList();

            var tempPath = Path.Combine(Path.GetTempPath(), $"Lista_skierowan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

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
                        titleFont = new PdfTrueTypeFont(new Font("Arial", 13f, System.Drawing.FontStyle.Bold), true);
                        headerFont = new PdfTrueTypeFont(new Font("Arial", 7f, System.Drawing.FontStyle.Bold), true);
                        normalFont = new PdfTrueTypeFont(new Font("Arial", 6.5f, System.Drawing.FontStyle.Regular), true);
                    }
                    else
                    {
                        titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 13, PdfFontStyle.Bold);
                        headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);
                        normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 6.5f);
                    }
                }
                catch
                {
                    titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 13, PdfFontStyle.Bold);
                    headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);
                    normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 6.5f);
                }

                const string documentName = "Lista skierowań";
                float y = 10f;

                // Nagłówek: nazwa wydruku + ilość rekordów
                g.DrawString(documentName, titleFont, PdfBrushes.Black, new RectangleF(0, y, pageWidth, 20f));
                y += 20f;
                g.DrawString($"Liczba rekordów: {items.Count}", normalFont, PdfBrushes.Gray, new RectangleF(0, y, pageWidth, 16f));
                y += 20f;

                g.DrawLine(new PdfPen(PdfBrushes.LightGray, 0.5f), 0, y, pageWidth, y);
                y += 8f;

                // Kolumny: ID | Typ (max 3 znaki) | Pesel + Imię + Nazwisko | Firma (30 znaków) | Data Skier. | Data Rej. | Data Bad. | Data Do | Faktura
                float[] colWidths = { 30f, 25f, 150f, 120f, 52f, 52f, 52f, 52f, 60f };
                string[] headers = { "ID", "Typ", "PESEL / Imię i nazwisko", "Firma", "Data Skier.", "Data Rej.", "Data Bad.", "Data Do", "Faktura" };

                void DrawRow(string[] values, PdfFont font, PdfBrush brush)
                {
                    float x = 0f;
                    for (int i = 0; i < values.Length; i++)
                    {
                        g.DrawString(values[i] ?? string.Empty, font, brush, new RectangleF(x, y, colWidths[i], 11f));
                        x += colWidths[i];
                    }
                    y += 11f;
                }

                DrawRow(headers, headerFont, PdfBrushes.Black);
                g.DrawLine(new PdfPen(PdfBrushes.LightGray, 0.5f), 0, y, pageWidth, y);
                y += 2f;

                foreach (var s in items)
                {
                    if (y > page.GetClientSize().Height - 20f)
                    {
                        page = document.Pages.Add();
                        g = page.Graphics;
                        y = 10f;
                    }

                    var firma = s.Firma ?? string.Empty;
                    if (firma.Length > 30)
                        firma = firma.Substring(0, 30) + "...";

                    var peselImieNazwisko = $"{s.Pesel} {s.Imie} {s.Nazwisko}".Trim();

                    DrawRow(new[]
                    {
                        s.Id.ToString(),
                        s.Typ,
                        peselImieNazwisko,
                        firma,
                        s.Data?.ToString("yyyy-MM-dd") ?? string.Empty,
                        s.DataRej?.ToString("yyyy-MM-dd") ?? string.Empty,
                        s.DataBad?.ToString("yyyy-MM-dd") ?? string.Empty,
                        s.DataDo?.ToString("yyyy-MM-dd") ?? string.Empty,
                        s.NumerFaktury ?? string.Empty
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

    public void ExportToExcel(System.Collections.Generic.IEnumerable<SkierowanieListItem>? visibleItems = null)
    {
        try
        {
            var items = (visibleItems ?? SkierowaniaFiltered).ToList();

            var saveDialog = new SaveFileDialog
            {
                Filter = "Plik Excel (*.xlsx)|*.xlsx",
                FileName = $"Lista_skierowan_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog() != true)
                return;

            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;
            var workbook = application.Workbooks.Create(1);
            var sheet = workbook.Worksheets[0];
            sheet.Name = "Lista skierowań";

            // Nagłówek: nazwa wydruku + ilość rekordów
            sheet.Range["A1"].Text = "Lista skierowań";
            sheet.Range["A1"].CellStyle.Font.Bold = true;
            sheet.Range["A1"].CellStyle.Font.Size = 14;

            sheet.Range["A2"].Text = $"Liczba rekordów: {items.Count}";
            sheet.Range["A2"].CellStyle.Font.Italic = true;

            // Nagłówki kolumn
            string[] headers = { "ID", "Typ", "PESEL", "Imię", "Nazwisko", "Firma", "Data Skier.", "Data Rej.", "Data Bad.", "Data Do", "Faktura" };
            int headerRow = 4;
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = sheet.Range[headerRow, col + 1];
                cell.Text = headers[col];
                cell.CellStyle.Font.Bold = true;
            }

            int row = headerRow + 1;
            foreach (var s in items)
            {
                sheet.Range[row, 1].Number = s.Id;
                sheet.Range[row, 2].Text = s.Typ;
                sheet.Range[row, 3].Text = s.Pesel;
                sheet.Range[row, 4].Text = s.Imie;
                sheet.Range[row, 5].Text = s.Nazwisko;
                sheet.Range[row, 6].Text = s.Firma ?? string.Empty; // pełna, nieobcięta nazwa
                sheet.Range[row, 7].Text = s.Data?.ToString("yyyy-MM-dd") ?? string.Empty;
                sheet.Range[row, 8].Text = s.DataRej?.ToString("yyyy-MM-dd") ?? string.Empty;
                sheet.Range[row, 9].Text = s.DataBad?.ToString("yyyy-MM-dd") ?? string.Empty;
                sheet.Range[row, 10].Text = s.DataDo?.ToString("yyyy-MM-dd") ?? string.Empty;
                sheet.Range[row, 11].Text = s.NumerFaktury ?? string.Empty;
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

    // ─── Filtrowanie ──────────────────────────────────────────

    private void FilterSkierowania()
    {
        SkierowaniaFiltered.Clear();

        var raw = SearchText?.Trim() ?? string.Empty;
        var text = NormalizePL(raw);

        DateTime? dateFrom = null;
        DateTime? dateTo = null;

        switch (SelectedDateFilter)
        {
            case "Bieżący Miesiąc":
                dateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);
                break;
            case "Poprzedni Miesiąc":
                dateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
                dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);
                break;
            case "Bieżący Rok":
                dateFrom = new DateTime(DateTime.Now.Year, 1, 1);
                dateTo = new DateTime(DateTime.Now.Year, 12, 31);
                break;
            case "Poprzedni Rok":
                dateFrom = new DateTime(DateTime.Now.Year - 1, 1, 1);
                dateTo = new DateTime(DateTime.Now.Year - 1, 12, 31);
                break;
            case "Wybrany okres":
                dateFrom = FilterDateFrom;
                dateTo = FilterDateTo;
                break;
        }

        foreach (var s in Skierowania)
        {
            bool dateMatch = true;
            if (dateFrom.HasValue && s.Data.HasValue && s.Data.Value < dateFrom.Value)
                dateMatch = false;
            if (dateTo.HasValue && s.Data.HasValue && s.Data.Value > dateTo.Value.Date.AddDays(1).AddTicks(-1))
                dateMatch = false;

            if (!dateMatch)
                continue;

            bool textMatch = true;
            if (!string.IsNullOrEmpty(text))
            {
                textMatch = ActiveFilterType switch
                {
                    "Imię" => NormalizePL(s.Imie).Contains(text, StringComparison.OrdinalIgnoreCase),
                    "Nazwisko" => NormalizePL(s.Nazwisko).Contains(text, StringComparison.OrdinalIgnoreCase),
                    "PESEL" => (s.Pesel ?? string.Empty).ToLower().Contains(text),
                    "Firma" => NormalizePL(s.Firma).Contains(text, StringComparison.OrdinalIgnoreCase),
                    "ID" => s.Id.ToString().Contains(raw),
                    _ => NormalizePL(s.Imie).Contains(text, StringComparison.OrdinalIgnoreCase)
                         || NormalizePL(s.Nazwisko).Contains(text, StringComparison.OrdinalIgnoreCase)
                         || (s.Pesel ?? string.Empty).ToLower().Contains(text)
                         || NormalizePL(s.Firma).Contains(text, StringComparison.OrdinalIgnoreCase)
                         || s.Id.ToString().Contains(raw)
                };
            }

            if (textMatch)
                SkierowaniaFiltered.Add(s);
        }
    }

    private void ExecuteToggleRejestracja(object? param)
    {
        if (param is not SkierowanieListItem item) return;

        // Gdy DataRej == dzisiaj — przycisk zielony, kliknięcie tylko nawiguje
        if (item.Rejestracja)
        {
            NavigateToRejestracja?.Invoke(item.Nazwisko);
            return;
        }

        var subject = $"{item.Nazwisko} {item.Imie} ({item.Firma})".Trim();
        var selectedDate = DateTime.Today;
        bool doNavigate = false;

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();

            int? existingId = null;
            string existingStatus = "Rejestracja";
            using (var checkCmd = (MySqlConnector.MySqlCommand)conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT R_ID, R_Status FROM Rejestracja WHERE R_S_ID = @sid LIMIT 1";
                checkCmd.Parameters.AddWithValue("@sid", item.Id);
                using var reader = checkCmd.ExecuteReader();
                if (reader.Read())
                {
                    existingId = reader["R_ID"] is DBNull ? null : Convert.ToInt32(reader["R_ID"]);
                    existingStatus = reader["R_Status"]?.ToString() ?? "Rejestracja";
                }
            }

            // Walidacja — zmiana daty możliwa tylko przy statusie Rejestracja lub Dokumentacja
            if (existingId.HasValue &&
                existingStatus != "Rejestracja" && existingStatus != "Dokumentacja")
            {
                System.Windows.MessageBox.Show(
                    $"Nie można zmienić daty rejestracji.\nAktualny status: \"{existingStatus}\".\n\nZmiana daty jest dozwolona tylko dla statusu: Rejestracja lub Dokumentacja.",
                    "Walidacja", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);

            }
            else
            {
                using var cmd = (MySqlConnector.MySqlCommand)conn.CreateCommand();
                if (existingId.HasValue)
                {
                    cmd.CommandText = @"
                        UPDATE Rejestracja SET
                            R_Data = @data, R_Status = @status, R_GG_MM = @ggmm,
                            R_Subject = @subject, R_Uwagi = @uwagi, R_P_ID = @pid
                        WHERE R_ID = @id";
                    cmd.Parameters.AddWithValue("@id", existingId.Value);
                }
                else
                {
                    cmd.CommandText = @"
                        INSERT INTO Rejestracja (R_Data, R_Status, R_S_ID, R_GG_MM, R_Subject, R_Uwagi, R_P_ID)
                        VALUES (@data, @status, @sid, @ggmm, @subject, @uwagi, @pid)";
                    cmd.Parameters.AddWithValue("@sid", item.Id);
                }

                cmd.Parameters.AddWithValue("@data", selectedDate.Date);
                cmd.Parameters.AddWithValue("@status", existingId.HasValue ? existingStatus : "Rejestracja");
                cmd.Parameters.AddWithValue("@ggmm", selectedDate);
                cmd.Parameters.AddWithValue("@subject", subject);
                cmd.Parameters.AddWithValue("@uwagi", subject);
                cmd.Parameters.AddWithValue("@pid", item.PacjentId);

                cmd.ExecuteNonQuery();

                // Odśwież listę (DataRej pochodzi z MAX(R_Data) w SQL)
                LoadFromDb();
                doNavigate = true;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd zapisu rejestracji:\n\n{ex.Message}",
                "Błąd", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            if (doNavigate)
                NavigateToRejestracja?.Invoke(item.Nazwisko);
        }
    }

    private void EditSkierowanie(object? obj)
    {
        if (obj is SkierowanieListItem item)
            NavigateToEdit?.Invoke(item);
    }

    /// <summary>
    /// Sprowadza ciąg do wersji bez polskich znaków diakrytycznych (lowercase).
    /// </summary>
    private static string NormalizePL(string? input)
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
                _ => ch
            });
        }
        return sb.ToString();
    }

}
