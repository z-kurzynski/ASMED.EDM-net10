using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

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

    /// <summary>
    /// Wywoływane po kliknięciu "Dodaj Kartę Badań" - code-behind widoku podpina tutaj nawigację.
    /// </summary>
    public Action? NavigateToNowaKartaBadan { get; set; }

    /// <summary>
    /// Wywoływane po kliknięciu przycisku edycji wiersza - przekazuje wybrane skierowanie.
    /// </summary>
    public Action<SkierowanieListItem>? NavigateToEdit { get; set; }

    // ─── Konstruktor ──────────────────────────────────────────

    public SkierowaniaViewModel(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;

        ClearSearchTextCommand = new RelayCommand<object?>(_ => SearchText = string.Empty);
        ApplyCustomDateFilterCommand = new RelayCommand<object?>(_ => FilterSkierowania());
        EditSkierowanieCommand = new RelayCommand<object?>(EditSkierowanie);
        OpenSkierPacjWoborCommand = new RelayCommand<object?>(_ => NavigateToNowaKartaBadan?.Invoke());

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
                    reg.R_Data,
                    fk.FK_Numer
                FROM B_Skierowania b
                INNER JOIN P_Pacjent p ON p.P_ID = b.B_Pacjent_ID
                LEFT JOIN Firma f ON f.id = p.P_Firma_id
                LEFT JOIN Badanie bad ON bad.Bad_ID = b.B_Badanie_ID
                LEFT JOIN Faktura fk ON fk.FK_ID = bad.Bad_F_ID
                LEFT JOIN (SELECT R_S_ID, MAX(R_Data) AS R_Data FROM Rejestracja GROUP BY R_S_ID) reg
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
                    NumerFaktury = reader["FK_Numer"]?.ToString()
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
