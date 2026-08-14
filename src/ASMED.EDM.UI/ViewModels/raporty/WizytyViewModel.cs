using System;
using System.Collections.ObjectModel;
using System.Linq;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models.Raporty;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ASMED.EDM.UI.ViewModels.Raporty;

/// <summary>
/// ViewModel dla zakładki Raporty -> Wizyty.
/// Przeniesiony z legacy ASMED.WPF.ViewModels.WizytyViewModel (legacy-xaml-backup\ViewModels\raporty\WizytyViewModel.cs).
/// Kolumna 1: niezafakturowane badania pogrupowane po firmie.
/// Kolumna 2: statystyki miesięczne (skierowania/badania/wizyty/wartość).
/// Kolumna 3: wykres miesięczny.
/// </summary>
public partial class WizytyViewModel : ObservableObject
{
    private readonly ILogger<WizytyViewModel> _logger;
    private readonly DbConnectionFactory? _dbFactory;
    private System.Windows.Threading.DispatcherTimer? _filterDebounceTimer;

    public WizytyViewModel(ILogger<WizytyViewModel> logger, DbConnectionFactory? dbFactory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbFactory = dbFactory;

        DostepneRoki = new ObservableCollection<int>(Enumerable.Range(2020, 11));
        _wybranyRok = DateTime.Now.Year;

        LoadNiezafakturowaneBadania();
        LoadStatystyki();
    }

    // ════════════════════════════════════════════════
    // Niezafakturowane badania (Kolumna 1)
    // ════════════════════════════════════════════════

    [ObservableProperty]
    private ObservableCollection<NiezafakturowaneBadaniaDto> _niezafakturowaneBadania = new();

    partial void OnNiezafakturowaneBadaniaChanged(ObservableCollection<NiezafakturowaneBadaniaDto> value)
    {
        OnPropertyChanged(nameof(LiczbaRekordow));
        OnPropertyChanged(nameof(LiczbaBadanRazem));
        OnPropertyChanged(nameof(SumaWartosciWyfiltrowanych));
        OnPropertyChanged(nameof(SumaWartosciWyfiltrowychFormatted));
    }

    private string? _filterFirmaNazwa = string.Empty;
    public string? FilterFirmaNazwa
    {
        get => _filterFirmaNazwa;
        set
        {
            if (_filterFirmaNazwa == value) return;
            _filterFirmaNazwa = value ?? string.Empty;
            OnPropertyChanged();

            _filterDebounceTimer?.Stop();
            _filterDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _filterDebounceTimer.Tick += (s, e) =>
            {
                _filterDebounceTimer?.Stop();
                LoadNiezafakturowaneBadania();
            };
            _filterDebounceTimer.Start();
        }
    }

    public int LiczbaRekordow => NiezafakturowaneBadania?.Count ?? 0;

    public int LiczbaBadanRazem => NiezafakturowaneBadania?.Sum(x => x.LiczbaBadan) ?? 0;

    public decimal SumaWartosciWyfiltrowanych => NiezafakturowaneBadania?.Sum(x => x.SumaWartosci ?? 0) ?? 0;

    public string SumaWartosciWyfiltrowychFormatted => $"{SumaWartosciWyfiltrowanych:N2} zł";

    [RelayCommand]
    private void Odswiez()
    {
        LoadNiezafakturowaneBadania();
        LoadStatystyki();
    }

    [RelayCommand]
    private void ApplyFilter() => LoadNiezafakturowaneBadania();

    [RelayCommand]
    private void ClearFilter() => FilterFirmaNazwa = string.Empty;

    private void LoadNiezafakturowaneBadania()
    {
        if (_dbFactory == null)
        {
            _logger.LogWarning("DbConnectionFactory not available, skipping NiezafakturowaneBadania load");
            return;
        }

        try
        {
            var loaded = new ObservableCollection<NiezafakturowaneBadaniaDto>();

            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();

            var sql = @"
                SELECT
                    Firma.Nazwa AS Nazwa,
                    SUM(Badanie.Bad_Razem) AS SumaOfBad_Razem,
                    COUNT(*) AS ilosc
                FROM Badanie
                INNER JOIN P_Pacjent ON Badanie.Bad_P_ID = P_Pacjent.P_ID
                INNER JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
                WHERE (Badanie.Bad_L_ID IS NULL OR Badanie.Bad_L_ID <= 0)";

            if (!string.IsNullOrWhiteSpace(FilterFirmaNazwa))
            {
                sql += " AND Firma.Nazwa LIKE @filter";
            }

            sql += " GROUP BY Firma.Nazwa ORDER BY SUM(Badanie.Bad_Razem) DESC";

            cmd.CommandText = sql;

            if (!string.IsNullOrWhiteSpace(FilterFirmaNazwa))
            {
                cmd.Parameters.AddWithValue("@filter", $"%{FilterFirmaNazwa}%");
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                loaded.Add(new NiezafakturowaneBadaniaDto
                {
                    NazwaFirmy = reader["Nazwa"]?.ToString(),
                    SumaWartosci = reader["SumaOfBad_Razem"] != DBNull.Value ? Convert.ToDecimal(reader["SumaOfBad_Razem"]) : null,
                    LiczbaBadan = reader["ilosc"] != DBNull.Value ? Convert.ToInt32(reader["ilosc"]) : 0
                });
            }

            NiezafakturowaneBadania = loaded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WizytyViewModel: error loading NiezafakturowaneBadania");
        }
    }

    // ════════════════════════════════════════════════
    // Statystyki miesięczne (Kolumna 2)
    // ════════════════════════════════════════════════

    private ObservableCollection<StatystykaMiesiecznaDto>? _statystykiMiesieczne;
    public ObservableCollection<StatystykaMiesiecznaDto>? StatystykiMiesieczne
    {
        get => _statystykiMiesieczne;
        set
        {
            if (SetProperty(ref _statystykiMiesieczne, value))
            {
                OnPropertyChanged(nameof(StatystykiMiesieczneSkalowane));
                OnPropertyChanged(nameof(StatystykiPodsumowanie));
            }
        }
    }

    public ObservableCollection<StatystykaMiesiecznaSkalowanaDto> StatystykiMiesieczneSkalowane =>
        StatystykaMiesiecznaSkalowanaDto.FromStatystyki(StatystykiMiesieczne);

    private int _wybranyRok;
    public int WybranyRok
    {
        get => _wybranyRok;
        set
        {
            if (SetProperty(ref _wybranyRok, value))
            {
                LoadStatystyki();
            }
        }
    }

    public ObservableCollection<int> DostepneRoki { get; }

    public StatystykaMiesiecznaDto StatystykiPodsumowanie
    {
        get
        {
            if (StatystykiMiesieczne == null || StatystykiMiesieczne.Count == 0)
                return new StatystykaMiesiecznaDto { MiesiacNazwa = "RAZEM" };

            return new StatystykaMiesiecznaDto
            {
                Rok = WybranyRok,
                Miesiac = 0,
                MiesiacNazwa = "RAZEM",
                LiczbaSkierowan = StatystykiMiesieczne.Sum(x => x.LiczbaSkierowan),
                BadaniaOkresowe = StatystykiMiesieczne.Sum(x => x.BadaniaOkresowe),
                BadaniaWstepne = StatystykiMiesieczne.Sum(x => x.BadaniaWstepne),
                BadaniaKontrolne = StatystykiMiesieczne.Sum(x => x.BadaniaKontrolne),
                BadaniaInne = StatystykiMiesieczne.Sum(x => x.BadaniaInne),
                LiczbaKsiazeczek = StatystykiMiesieczne.Sum(x => x.LiczbaKsiazeczek),
                BadaniaOdbyte = StatystykiMiesieczne.Sum(x => x.BadaniaOdbyte),
                WizytyZarejestrowane = StatystykiMiesieczne.Sum(x => x.WizytyZarejestrowane),
                WartoscBadan = StatystykiMiesieczne.Sum(x => x.WartoscBadan)
            };
        }
    }

    private static readonly string[] MiesiacNazwy =
    {
        "", "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
        "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
    };

    private void LoadStatystyki()
    {
        if (_dbFactory == null)
        {
            _logger.LogWarning("DbConnectionFactory not available, skipping Statystyki load");
            return;
        }

        var rok = WybranyRok;
        var result = new ObservableCollection<StatystykaMiesiecznaDto>();
        for (int m = 1; m <= 12; m++)
        {
            result.Add(new StatystykaMiesiecznaDto { Rok = rok, Miesiac = m, MiesiacNazwa = MiesiacNazwy[m] });
        }

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();

            cmd.CommandText = @"
                SELECT
                    s.B_TypBadania,
                    s.B_ksiazeczka,
                    s.B_RegistrationDate,
                    b.Bad_Data,
                    b.Bad_Razem,
                    r.R_Data
                FROM B_Skierowania s
                LEFT JOIN Badanie b ON s.B_ID = b.Bad_S_ID
                LEFT JOIN Rejestracja r ON r.R_S_ID = s.B_ID
                WHERE YEAR(s.B_RegistrationDate) = @rok
                   OR YEAR(b.Bad_Data) = @rok
                   OR YEAR(r.R_Data) = @rok";
            cmd.Parameters.AddWithValue("@rok", rok);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var typBadania = reader["B_TypBadania"]?.ToString()?.ToUpperInvariant();
                var ksiazeczka = reader["B_ksiazeczka"] != DBNull.Value && Convert.ToBoolean(reader["B_ksiazeczka"]);

                if (reader["B_RegistrationDate"] != DBNull.Value)
                {
                    var dataSkierowania = Convert.ToDateTime(reader["B_RegistrationDate"]);
                    if (dataSkierowania.Year == rok)
                    {
                        var stat = result[dataSkierowania.Month - 1];
                        stat.LiczbaSkierowan++;

                        switch (typBadania)
                        {
                            case "O": stat.BadaniaOkresowe++; break;
                            case "W": stat.BadaniaWstepne++; break;
                            case "K": stat.BadaniaKontrolne++; break;
                            default: stat.BadaniaInne++; break;
                        }

                        if (ksiazeczka) stat.LiczbaKsiazeczek++;
                    }
                }

                if (reader["Bad_Data"] != DBNull.Value)
                {
                    var dataBadania = Convert.ToDateTime(reader["Bad_Data"]);
                    if (dataBadania.Year == rok)
                    {
                        var stat = result[dataBadania.Month - 1];
                        stat.BadaniaOdbyte++;

                        if (reader["Bad_Razem"] != DBNull.Value)
                        {
                            stat.WartoscBadan += Convert.ToDecimal(reader["Bad_Razem"]);
                        }
                    }
                }

                if (reader["R_Data"] != DBNull.Value)
                {
                    var dataWizyty = Convert.ToDateTime(reader["R_Data"]);
                    if (dataWizyty.Year == rok)
                    {
                        var stat = result[dataWizyty.Month - 1];
                        stat.WizytyZarejestrowane++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WizytyViewModel: error loading Statystyki");
        }

        StatystykiMiesieczne = result;
    }

    // ════════════════════════════════════════════════
    // Wykres (Kolumna 3)
    // ════════════════════════════════════════════════

    [ObservableProperty]
    private bool _isLineChart = true;

    public bool IsColumnChart => !IsLineChart;

    partial void OnIsLineChartChanged(bool value)
    {
        OnPropertyChanged(nameof(IsColumnChart));
    }

    [ObservableProperty]
    private bool _wykresKartyVisible = true;

    [ObservableProperty]
    private bool _wykresBadanVisible = true;

    [ObservableProperty]
    private bool _wykresWizytVisible = true;

    [ObservableProperty]
    private bool _wykresWartosciVisible = true;

    [RelayCommand]
    private void SetLineChart() => IsLineChart = true;

    [RelayCommand]
    private void SetColumnChart() => IsLineChart = false;

    // ════════════════════════════════════════════════
    // Widoczność kolumny statystyk (toggle)
    // ════════════════════════════════════════════════

    [ObservableProperty]
    private bool _isStatystykiVisible = true;

    [RelayCommand]
    private void ToggleStatystyki() => IsStatystykiVisible = !IsStatystykiVisible;
}
