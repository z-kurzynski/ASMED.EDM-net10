using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMED.EDM.UI.Models.Orzeczenie;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using ASMED.EDM.Data.Services;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ASMED.EDM.UI.ViewModels.Orzeczenie;

/// <summary>
/// ViewModel dla OrzeczenieView.
/// Zarządza bistabilnymi przyciskami w button_badania
/// oraz widocznością powiązanych zakładek w prawa_orzecz.
/// Stan przycisków będzie docelowo sterowany z bazy danych.
///
/// Odpowiada również za listę pacjentów w lewej kolumnie (analogicznie do BadanieViewModel):
/// ładowanie z bazy, filtrowanie po statusie/dacie/wyszukiwaniu, cichy auto-refresh,
/// zaznaczenie oraz przyciski Anuluj / Zatwierdź.
/// </summary>
public partial class OrzeczenieViewModel : ViewModelBase
{
    private readonly ILogger<OrzeczenieViewModel>? _logger;
    private readonly DbConnectionFactory? _dbFactory;

    private ObservableCollection<OrzeczenieListItem> _allWizyty = new();

    /// <summary>
    /// Konstruktor — synchronizuje stan przycisków z domyślnym DTO
    /// (lub z DTO załadowanym z bazy przez LoadFromDto).
    /// </summary>
    public OrzeczenieViewModel(ILogger<OrzeczenieViewModel>? logger = null, DbConnectionFactory? dbFactory = null)
    {
        _logger = logger;
        _dbFactory = dbFactory;

        LoadFromDto(_currentDto);

        OpenSkierPacjWoborCommand = new RelayCommand(ExecuteOpenSkierPacjWobor);
        AnulujCommand = new RelayCommand(ExecuteAnuluj);
        ZatwierdzCommand = new RelayCommand(ExecuteZatwierdz, CanExecuteZatwierdz);

        _waznoscBadaniaDo = (_customDefaultDate ?? DateTime.Today).AddYears(3);
        _validityYears = 3;
        UpdateValidityYearLabels();
        InitializeValidityMonthOptions();

        _tabDataBadania = _customDefaultDate;
        _laryngDataBadania = _customDefaultDate;
        _okuDataBadania = _customDefaultDate;
        _sanitarneDataBadania = _customDefaultDate;

        LoadWizytyFromDb();
    }

    // ── Selektor "Ważność Badania do roku / miesiąca" (analogicznie do BadaniaNewViewModel) ──

    private DateTime _waznoscBadaniaDo;

    /// <summary>
    /// Data wyświetlana w Calenar_Data_Do. Zmieniana przez selektory lat/miesięcy.
    /// </summary>
    public DateTime WaznoscBadaniaDo
    {
        get => _waznoscBadaniaDo;
        set
        {
            if (_waznoscBadaniaDo != value)
            {
                _waznoscBadaniaDo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ValidityMonth));
            }
        }
    }

    private int _validityYears;

    /// <summary>
    /// Wybrany offset roku (0 = bieżący rok, 1..5 = +1..+5 lat). Ustawia rok w WaznoscBadaniaDo.
    /// </summary>
    public int ValidityYears
    {
        get => _validityYears;
        set
        {
            if (_validityYears != value)
            {
                _validityYears = value;
                OnPropertyChanged();

                int targetYear = (_customDefaultDate ?? DateTime.Today).Year + value;
                int targetMonth = _waznoscBadaniaDo.Month;
                int targetDay = (_customDefaultDate ?? DateTime.Today).Day;

                int daysInTargetMonth = DateTime.DaysInMonth(targetYear, targetMonth);
                if (targetDay > daysInTargetMonth)
                {
                    targetDay = daysInTargetMonth;
                }

                WaznoscBadaniaDo = new DateTime(targetYear, targetMonth, targetDay);
            }
        }
    }

    public ObservableCollection<ValidityYearItem> ValidityYearOptions { get; } = new();

    private void UpdateValidityYearLabels()
    {
        ValidityYearOptions.Clear();
        int baseYear = (_customDefaultDate ?? DateTime.Today).Year;
        for (int i = 0; i <= 5; i++)
        {
            int targetYear = baseYear + i;
            ValidityYearOptions.Add(new ValidityYearItem
            {
                Years = i,
                Label = targetYear.ToString()
            });
        }
    }

    public class ValidityYearItem
    {
        public int Years { get; set; }
        public string? Label { get; set; }
    }

    /// <summary>
    /// Wybrany miesiąc - odczytuje i ustawia miesiąc w WaznoscBadaniaDo.
    /// </summary>
    public int ValidityMonth
    {
        get => _waznoscBadaniaDo.Month;
        set
        {
            if (_waznoscBadaniaDo.Month != value)
            {
                int targetYear = _waznoscBadaniaDo.Year;
                int targetMonth = value;
                int targetDay = _waznoscBadaniaDo.Day;

                int daysInTargetMonth = DateTime.DaysInMonth(targetYear, targetMonth);
                if (targetDay > daysInTargetMonth)
                {
                    targetDay = daysInTargetMonth;
                }

                WaznoscBadaniaDo = new DateTime(targetYear, targetMonth, targetDay);
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ValidityMonthItem> ValidityMonthOptions { get; } = new();

    private void InitializeValidityMonthOptions()
    {
        ValidityMonthOptions.Clear();
        for (int i = 1; i <= 12; i++)
        {
            ValidityMonthOptions.Add(new ValidityMonthItem
            {
                Month = i,
                Label = i.ToString("D2")
            });
        }
    }

    public class ValidityMonthItem
    {
        public int Month { get; set; }
        public string? Label { get; set; }
    }

    // ── Lewa kolumna: lista pacjentów ────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<OrzeczenieListItem> _wizyty = new();

    [ObservableProperty]
    private OrzeczenieListItem? _selectedWizyta;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DateTime? _customDefaultDate = DateTime.Today;

    /// <summary>
    /// Data badania w zakładce Orzeczenie (Data_Badania). Jest wstępnie ustawiana
    /// wartością daty domyślnej (CustomDefaultDate), ale po tym jest niezależna
    /// i może być swobodnie edytowana bez wpływu na datę domyślną.
    /// </summary>
    [ObservableProperty]
    private DateTime? _tabDataBadania;

    /// <summary>
    /// Data badania laryngologicznego (bad_Laryn_data). Jest wstępnie ustawiana
    /// wartością daty domyślnej (CustomDefaultDate) i synchronizowana w dół przy jej zmianie,
    /// ale może być swobodnie edytowana niezależnie.
    /// </summary>
    [ObservableProperty]
    private DateTime? _laryngDataBadania;

    /// <summary>
    /// Data badania okulistycznego. Jest wstępnie ustawiana wartością daty domyślnej
    /// (CustomDefaultDate) i synchronizowana w dół przy jej zmianie,
    /// ale może być swobodnie edytowana niezależnie.
    /// </summary>
    [ObservableProperty]
    private DateTime? _okuDataBadania;

    /// <summary>
    /// Data badania sanitarno-epidemiologicznego. Jest wstępnie ustawiana wartością daty domyślnej
    /// (CustomDefaultDate) i synchronizowana w dół przy jej zmianie,
    /// ale może być swobodnie edytowana niezależnie.
    /// </summary>
    [ObservableProperty]
    private DateTime? _sanitarneDataBadania;

    /// <summary>
    /// Data początku urlopu dla poratowania zdrowia.
    /// </summary>
    [ObservableProperty]
    private DateTime? _urlopDataOd;

    /// <summary>
    /// Data końca urlopu dla poratowania zdrowia.
    /// </summary>
    [ObservableProperty]
    private DateTime? _urlopDataDo;

    public ObservableCollection<string> FilterTypes { get; } = new() { "Wszystko", "Imię", "Nazwisko", "PESEL" };

    [ObservableProperty]
    private string _activeFilterType = "Wszystko";

    /// <summary>
    /// Toggle: pokazuj pozycje ze statusem "Rejestracja" lub "Dokumentacja".
    /// </summary>
    [ObservableProperty]
    private bool _showRejestracja = false;

    /// <summary>
    /// Toggle: pokazuj pozycje ze statusem "W trakcie".
    /// </summary>
    [ObservableProperty]
    private bool _showWizyta = false;

    /// <summary>
    /// Toggle: pokazuj pozycje ze statusem "Wywiad". Domyślnie włączony.
    /// </summary>
    [ObservableProperty]
    private bool _showWywiad = true;

    public IRelayCommand OpenSkierPacjWoborCommand { get; }

    /// <summary>
    /// Przycisk "Anuluj" - czyści zaznaczenie na liście (brak wybranego pacjenta).
    /// </summary>
    public IRelayCommand AnulujCommand { get; }

    /// <summary>
    /// Przycisk "Zatwierdź" - zmienia status wybranej pozycji na "Orzeczenie".
    /// </summary>
    public IRelayCommand ZatwierdzCommand { get; }

    private void ExecuteOpenSkierPacjWobor()
    {
        // TODO: logika do zaimplementowania później
    }
    public void RefreshFromDb()
    {
       
    }
    private void ExecuteAnuluj()
    {
        SelectedWizyta = null;
    }

    private bool CanExecuteZatwierdz() => SelectedWizyta != null;

    private void ExecuteZatwierdz()
    {
        if (SelectedWizyta == null) return;

        var rId = SelectedWizyta.R_ID;
        if (rId == null) return;

        const string newStatus = "Orzeczenie";

        if (!UpdateStatusInDb(rId.Value, newStatus))
        {
            return;
        }

        SelectedWizyta.R_Status = newStatus;
        _logger?.LogInformation("OrzeczenieViewModel: status zmieniony na 'Orzeczenie' dla R_ID={R_ID}", rId);

        // Odswiez filtrowana liste - pozycja ze statusem "Orzeczenie" nie spelnia juz kryteriow filtra.
        ApplyFilter();
        SelectedWizyta = null;
    }

    private bool UpdateStatusInDb(int rId, string newStatus)
    {
        if (_dbFactory == null)
        {
            _logger?.LogWarning("DbConnectionFactory not available, cannot update R_Status");
            return false;
        }

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = "UPDATE Rejestracja SET R_Status = @status WHERE R_ID = @id";
            cmd.Parameters.AddWithValue("@status", newStatus);
            cmd.Parameters.AddWithValue("@id", rId);
            var affected = cmd.ExecuteNonQuery();

            if (affected == 0)
            {
                _logger?.LogWarning("UpdateStatusInDb: brak zaktualizowanych wierszy dla R_ID={R_ID}", rId);
            }

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Blad podczas aktualizacji R_Status dla R_ID={R_ID}", rId);
            return false;
        }
    }

    /// <summary>
    /// Przełącza (toggle) fragment typu badania ("W", "K" lub "O") w wartości
    /// SelectedWizyta.B_TypBadania i zapisuje ją do B_Skierowania.B_TypBadania.
    /// "Wstępne" (W) wyklucza "Kontrolne" i "Okresowe" (są wzajemnie sprzeczne).
    /// "Kontrolne" (K) i "Okresowe" (O) mogą współistnieć razem, tworząc wartość "O/K".
    /// </summary>
    private void ToggleTypBadania(string typ)
    {
        if (SelectedWizyta == null || SelectedWizyta.B_ID == null) return;

        var current = SelectedWizyta.B_typ ?? string.Empty;
        var parts = current.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        bool wasActive = parts.Contains(typ);

        string newValue;

        if (typ == "W")
        {
            // Wstępne wyklucza Kontrolne/Okresowe - jest samodzielne.
            newValue = wasActive ? string.Empty : "W";
        }
        else
        {
            // Kontrolne / Okresowe: usuń "W" (wykluczają się), przełącz wybrany typ.
            parts.Remove("W");
            if (wasActive)
                parts.Remove(typ);
            else
                parts.Add(typ);

            // Utrzymuj stałą kolejność O/K.
            var ordered = new List<string>();
            if (parts.Contains("O")) ordered.Add("O");
            if (parts.Contains("K")) ordered.Add("K");
            newValue = string.Join("/", ordered);
        }

        if (!UpdateTypBadaniaInDb(SelectedWizyta.B_ID.Value, newValue))
            return;

        SelectedWizyta.B_typ = newValue;
        OnPropertyChanged(nameof(SelectedWizyta));
    }

    private bool UpdateTypBadaniaInDb(int bId, string newTyp)
    {
        if (_dbFactory == null)
        {
            _logger?.LogWarning("DbConnectionFactory not available, cannot update B_TypBadania");
            return false;
        }

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = "UPDATE B_Skierowania SET B_TypBadania = @typ WHERE B_ID = @id";
            cmd.Parameters.AddWithValue("@typ", newTyp);
            cmd.Parameters.AddWithValue("@id", bId);
            var affected = cmd.ExecuteNonQuery();

            if (affected == 0)
            {
                _logger?.LogWarning("UpdateTypBadaniaInDb: brak zaktualizowanych wierszy dla B_ID={B_ID}", bId);
            }

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Blad podczas aktualizacji B_TypBadania dla B_ID={B_ID}", bId);
            return false;
        }
    }

    [RelayCommand]
    private void ToggleWstepne() => ToggleTypBadania("W");

    [RelayCommand]
    private void ToggleKontrolne() => ToggleTypBadania("K");

    [RelayCommand]
    private void ToggleOkresowe() => ToggleTypBadania("O");

    partial void OnSelectedWizytaChanged(OrzeczenieListItem? value)
    {
        ZatwierdzCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsWizytaSelected));

        if (value != null && value.B_DataSkierowania == null)
        {
            var baza = TabDataBadania ?? CustomDefaultDate ?? DateTime.Today;
            value.B_DataSkierowania = PoprzedniDzienRoboczy(baza);
            OnPropertyChanged(nameof(SelectedWizyta));
        }

        // Odczyt stanow bistabilnych przyciskow z wybranego rekordu (B_Skierowania).
        // Ustawiane bez zapisu do bazy, poniewaz OnIs*ActiveChanged wywolane tutaj
        // tylko synchronizuje CurrentDto (lokalny stan), a nie wykonuje UPDATE.
        _suppressDbSave = true;
        try
        {
            IsLaryngologActive = value?.B_laryngolog ?? false;
            IsOkulistaActive = value?.B_okulista ?? false;
            IsSanitarneActive = value?.B_ksiazeczka ?? false;
            IsUrlopActive = value?.B_Urlop ?? false;
        }
        finally
        {
            _suppressDbSave = false;
        }
    }

    private bool _suppressDbSave;

    /// <summary>
    /// Zwraca dzień poprzedzający podaną datę o 1 dzień, ale zawsze w zakresie
    /// poniedziałek-piątek (jeśli wypada na sobotę lub niedzielę, cofa się do piątku).
    /// </summary>
    private static DateTime PoprzedniDzienRoboczy(DateTime data)
    {
        var wynik = data.AddDays(-1);
        while (wynik.DayOfWeek == DayOfWeek.Saturday || wynik.DayOfWeek == DayOfWeek.Sunday)
        {
            wynik = wynik.AddDays(-1);
        }

        return wynik;
    }

    /// <summary>
    /// Czy jest wybrany rekord (wizyta/skierowanie) na liście. Steruje aktywnością
    /// przycisków bistabilnych w środkowej kolumnie (Badanie, Laryngolog, Okulista, Sanitarne, Urlop),
    /// które są domyślnie nieaktywne dopóki użytkownik nie zaznaczy rekordu.
    /// </summary>
    public bool IsWizytaSelected => SelectedWizyta != null;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnActiveFilterTypeChanged(string value) => ApplyFilter();

    partial void OnCustomDefaultDateChanged(DateTime? value)
    {
        TabDataBadania = value;
        LaryngDataBadania = value;
        OkuDataBadania = value;
        SanitarneDataBadania = value;

        ApplyFilter();
        UpdateValidityYearLabels();
        WaznoscBadaniaDo = (value ?? DateTime.Today).AddYears(3);
        _validityYears = 3;
        OnPropertyChanged(nameof(ValidityYears));
    }

    partial void OnShowRejestracjaChanged(bool value) => ApplyFilter();

    partial void OnShowWizytaChanged(bool value) => ApplyFilter();

    partial void OnShowWywiadChanged(bool value) => ApplyFilter();

    private static bool IsRejestracjaLubDokumentacja(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var s = status.Trim().ToLowerInvariant();
        return s == "rejestracja" || s == "dokumentacja";
    }

    private static bool IsWTrakcie(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        return status.Trim().ToLowerInvariant() == "w trakcie";
    }

    private static bool IsWywiad(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        return status.Trim().ToLowerInvariant() == "wywiad";
    }

    private void LoadWizytyFromDb(bool silent = false)
    {
        if (_dbFactory == null)
        {
            _logger?.LogWarning("DbConnectionFactory not available, skipping Wizyty load");
            return;
        }

        var selectedId = SelectedWizyta?.R_ID;

        try
        {
            var loaded = new ObservableCollection<OrzeczenieListItem>();

            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    r.R_ID, r.R_Data, r.R_Status,
                    s.B_ID, s.B_DataSkierowania, s.B_TypBadania, s.B_Stanowisko,
                    s.B_laryngolog, s.B_okulista, s.B_ksiazeczka, s.B_Urlop,
                    p.P_ID, p.P_imie, p.P_nazwisko, p.P_pesel,
                    p.P_Ades_miasto, p.P_Adres_ulica_numer, p.P_Ades_kod,
                    f.Nazwa AS Firma_Nazwa, f.NIP AS Firma_NIP
                FROM Rejestracja r
                INNER JOIN B_Skierowania s ON r.R_S_ID = s.B_ID
                INNER JOIN P_Pacjent p ON s.B_Pacjent_ID = p.P_ID
                LEFT JOIN Firma f ON p.P_Firma_id = f.id
                ORDER BY r.R_Data DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                loaded.Add(new OrzeczenieListItem
                {
                    R_ID = reader["R_ID"] != DBNull.Value ? Convert.ToInt32(reader["R_ID"]) : null,
                    R_Data = reader["R_Data"] != DBNull.Value ? Convert.ToDateTime(reader["R_Data"]) : null,
                    R_Status = reader["R_Status"]?.ToString(),

                    B_ID = reader["B_ID"] != DBNull.Value ? Convert.ToInt32(reader["B_ID"]) : null,
                    B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : null,
                    B_typ = reader["B_TypBadania"]?.ToString(),
                    B_Stanowisko = reader["B_Stanowisko"]?.ToString(),

                    B_laryngolog = reader["B_laryngolog"] != DBNull.Value && Convert.ToBoolean(reader["B_laryngolog"]),
                    B_okulista = reader["B_okulista"] != DBNull.Value && Convert.ToBoolean(reader["B_okulista"]),
                    B_ksiazeczka = reader["B_ksiazeczka"] != DBNull.Value && Convert.ToBoolean(reader["B_ksiazeczka"]),
                    B_Urlop = reader["B_Urlop"] != DBNull.Value && Convert.ToBoolean(reader["B_Urlop"]),

                    P_ID = reader["P_ID"] != DBNull.Value ? Convert.ToInt32(reader["P_ID"]) : null,
                    P_imie = reader["P_imie"]?.ToString(),
                    P_nazwisko = reader["P_nazwisko"]?.ToString(),
                    P_pesel = reader["P_pesel"]?.ToString(),
                    P_Miasto = reader["P_Ades_miasto"]?.ToString(),
                    P_Adres = reader["P_Adres_ulica_numer"]?.ToString(),
                    P_KodPocztowy = reader["P_Ades_kod"]?.ToString(),

                    Firma_Nazwa = reader["Firma_Nazwa"]?.ToString(),
                    Firma_NIP = reader["Firma_NIP"]?.ToString(),
                });
            }

            _allWizyty = loaded;
            _logger?.LogInformation("OrzeczenieViewModel: loaded {Count} rows", loaded.Count);

            ApplyFilter();

            if (selectedId.HasValue)
            {
                SelectedWizyta = Wizyty.FirstOrDefault(x => x.R_ID == selectedId);
            }
        }
        catch (Exception ex)
        {
            if (silent)
            {
                _logger?.LogWarning(ex, "OrzeczenieViewModel: silent refresh failed (non-critical)");
            }
            else
            {
                _logger?.LogError(ex, "OrzeczenieViewModel: error loading Wizyty from DB");
            }
        }
    }

    /// <summary>
    /// Ciche odświeżenie listy (bez przeładowania całego widoku) - wywoływane cyklicznie z code-behind.
    /// </summary>
    public void RefreshSilent()
    {
        LoadWizytyFromDb(silent: true);
    }

    private void ApplyFilter()
    {
        IEnumerable<OrzeczenieListItem> query = _allWizyty.Where(x =>
            (ShowRejestracja && IsRejestracjaLubDokumentacja(x.R_Status)) ||
            (ShowWizyta && IsWTrakcie(x.R_Status)) ||
            (ShowWywiad && IsWywiad(x.R_Status)));

        if (CustomDefaultDate.HasValue)
        {
            var date = CustomDefaultDate.Value.Date;
            query = query.Where(x => x.R_Data.HasValue && x.R_Data.Value.Date == date);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var text = SearchText.Trim();
            query = ActiveFilterType switch
            {
                "Imię" => query.Where(x => x.P_imie != null && x.P_imie.Contains(text, StringComparison.OrdinalIgnoreCase)),
                "Nazwisko" => query.Where(x => x.P_nazwisko != null && x.P_nazwisko.Contains(text, StringComparison.OrdinalIgnoreCase)),
                "PESEL" => query.Where(x => x.P_pesel != null && x.P_pesel.Contains(text, StringComparison.OrdinalIgnoreCase)),
                _ => query.Where(x =>
                    (x.P_imie != null && x.P_imie.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
                    (x.P_nazwisko != null && x.P_nazwisko.Contains(text, StringComparison.OrdinalIgnoreCase)) ||
                    (x.P_pesel != null && x.P_pesel.Contains(text, StringComparison.OrdinalIgnoreCase)))
            };
        }

        Wizyty = new ObservableCollection<OrzeczenieListItem>(query);
    }

    // ── Indeks aktywnej zakładki w TabControl prawa_orzecz ──────────────────
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    // ── Stany bistabilne przycisków ──────────────────────────────────────────

    /// <summary>
    /// Czy zakładka "Bad. sanitarne" jest aktywna (widoczna).
    /// Domyślnie false → zakładka Collapsed.
    /// </summary>
    [ObservableProperty]
    private bool _isSanitarneActive = false;

    /// <summary>
    /// Czy zakładka "Urlop dla por. zdr." jest aktywna (widoczna).
    /// Domyślnie false → zakładka Collapsed.
    /// </summary>
    [ObservableProperty]
    private bool _isUrlopActive = false;

    /// <summary>
    /// Czy przycisk "Badanie" (Karta Badań) jest aktywny.
    /// Domyślnie false → przycisk w stanie nieaktywnym.
    /// </summary>
    [ObservableProperty]
    private bool _isBadanieActive = false;

    // ── Aktywne DTO (stan przycisków + dane pacjenta/firmy) ──────────────────

    /// <summary>
    /// Bieżące DTO widoku. Synchronizowane dwukierunkowo ze stanami przycisków.
    /// Inicjalizowane jako pusty obiekt — docelowo ładowane z bazy przez LoadFromDto.
    /// </summary>
    [ObservableProperty]
    private OrzeczenieStatusDto _currentDto = new();

    // ── Callbacki VM → DTO (CommunityToolkit.Mvvm partial) ──────────────────
    // Wywoływane automatycznie przez [ObservableProperty] przy każdej zmianie.

    partial void OnIsBadanieActiveChanged(bool value)    => _currentDto.Btn_Badanie    = value;

    partial void OnIsLaryngologActiveChanged(bool value)
    {
        _currentDto.Btn_Laryngolog = value;
        if (SelectedWizyta != null)
            SelectedWizyta.B_laryngolog = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_laryngolog", value);
    }

    partial void OnIsOkulistaActiveChanged(bool value)
    {
        _currentDto.Btn_Okulista = value;
        if (SelectedWizyta != null)
            SelectedWizyta.B_okulista = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_okulista", value);
    }

    partial void OnIsSanitarneActiveChanged(bool value)
    {
        _currentDto.Btn_Sanitarne = value;
        if (SelectedWizyta != null)
            SelectedWizyta.B_ksiazeczka = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_ksiazeczka", value);
    }

    partial void OnIsUrlopActiveChanged(bool value)
    {
        _currentDto.Btn_Urlop = value;
        if (SelectedWizyta != null)
            SelectedWizyta.B_Urlop = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_Urlop", value);
    }

    /// <summary>
    /// Zapisuje wartosc flagi bistabilnego przycisku (0/1) w B_Skierowania dla
    /// aktualnie wybranego rekordu. Nazwa kolumny musi pochodzic z bezpiecznej,
    /// wewnetrznie kontrolowanej listy (nigdy z danych wejsciowych uzytkownika).
    /// </summary>
    private bool UpdateBSkierowaniaFlagInDb(string columnName, bool value)
    {
        if (SelectedWizyta?.B_ID == null)
            return false;

        if (_dbFactory == null)
        {
            _logger?.LogWarning("DbConnectionFactory not available, cannot update {Column}", columnName);
            return false;
        }

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = $"UPDATE B_Skierowania SET {columnName} = @val WHERE B_ID = @id";
            cmd.Parameters.AddWithValue("@val", value ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", SelectedWizyta.B_ID.Value);
            var affected = cmd.ExecuteNonQuery();

            if (affected == 0)
            {
                _logger?.LogWarning("UpdateBSkierowaniaFlagInDb: brak zaktualizowanych wierszy dla B_ID={B_ID}, kolumna={Column}", SelectedWizyta.B_ID, columnName);
            }

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Blad podczas aktualizacji {Column} dla B_ID={B_ID}", columnName, SelectedWizyta.B_ID);
            return false;
        }
    }

    // ── Komendy bistabilne

    /// <summary>
    /// Przełącza widoczność zakładki "Bad. sanitarne".
    /// Przy wyłączeniu (toggle off) fokus wraca do pierwszej zakładki.
    /// </summary>
    [RelayCommand]
    private void ToggleSanitarne()
    {
        IsSanitarneActive = !IsSanitarneActive;

        if (!IsSanitarneActive)
            SelectedTabIndex = 0;
        else
            SelectedTabIndex = GetTabIndex_Sanitarne();
    }

    /// <summary>
    /// Anuluje badanie sanitarno-epidemiologiczne: dezaktywuje zakładkę
    /// "Bad. sanitarne" (zostaje ukryta) i wraca do pierwszej zakładki. Nie czyści SelectedWizyta.
    /// </summary>
    [RelayCommand]
    private void AnulujSanitarne()
    {
        IsSanitarneActive = false;
        SelectedTabIndex = 0;
    }

    /// <summary>
    /// Przełącza widoczność zakładki "Urlop dla por. zdr.".
    /// Przy wyłączeniu (toggle off) fokus wraca do pierwszej zakładki.
    /// </summary>
    [RelayCommand]
    private void ToggleUrlop()
    {
        IsUrlopActive = !IsUrlopActive;

        if (!IsUrlopActive)
            SelectedTabIndex = 0;
        else
            SelectedTabIndex = GetTabIndex_Urlop();
    }

    /// <summary>
    /// Anuluje urlop dla poratowania zdrowia: dezaktywuje zakładkę
    /// "Urlop dla por. zdr." (zostaje ukryta) i wraca do pierwszej zakładki. Nie czyści SelectedWizyta.
    /// </summary>
    [RelayCommand]
    private void AnulujUrlop()
    {
        IsUrlopActive = false;
        SelectedTabIndex = 0;
    }

    /// <summary>
    /// Przełącza fokus na główną zakładkę "Orzeczenie" (OrzeczenieTabView),
    /// bez zmiany stanu przycisków bistabilnych ani danych. Używane przez przycisk
    /// "Wróć" widoczny w podzakładkach badań specjalistycznych.
    /// </summary>
    [RelayCommand]
    private void Wroc()
    {
        SelectedTabIndex = 0;
    }

    /// <summary>
    /// Przełącza aktywność przycisku "Badanie" (Karta Badań).
    /// </summary>
    [RelayCommand]
    private void ToggleBadanie()
    {
        IsBadanieActive = !IsBadanieActive;

        SelectedTabIndex = IsBadanieActive
            ? GetTabIndex_Badanie()
            : 0;
    }

    /// <summary>
    /// Czy zakładka "Laryngolog" jest aktywna (widoczna).
    /// Domyślnie false → zakładka Collapsed.
    /// </summary>
    [ObservableProperty]
    private bool _isLaryngologActive = false;

    /// <summary>
    /// Czy zakładka "Okulista" jest aktywna (widoczna).
    /// Domyślnie false → zakładka Collapsed.
    /// </summary>
    [ObservableProperty]
    private bool _isOkulistaActive = false;

    // ── Komendy bistabilne (Laryngolog / Okulista) ───────────────────────────

    /// <summary>
    /// Przełącza widoczność zakładki "Laryngolog".
    /// Przy wyłączeniu (toggle off) fokus wraca do pierwszej zakładki.
    /// </summary>
    [RelayCommand]
    private void ToggleLaryngolog()
    {
        IsLaryngologActive = !IsLaryngologActive;

        if (!IsLaryngologActive)
            SelectedTabIndex = 0;
        else
            SelectedTabIndex = GetTabIndex_Laryngolog();
    }

    /// <summary>
    /// Anuluje badanie laryngologiczne: dezaktywuje zakładkę "Laryngolog"
    /// (zostaje ukryta) i wraca do pierwszej zakładki. Nie czyści SelectedWizyta.
    /// </summary>
    [RelayCommand]
    private void AnulujLaryngolog()
    {
        IsLaryngologActive = false;
        SelectedTabIndex = 0;
    }

    /// <summary>
    /// Przełącza widoczność zakładki "Okulista".
    /// Przy wyłączeniu (toggle off) fokus wraca do pierwszej zakładki.
    /// </summary>
    [RelayCommand]
    private void ToggleOkulista()
    {
        IsOkulistaActive = !IsOkulistaActive;

        if (!IsOkulistaActive)
            SelectedTabIndex = 0;
        else
            SelectedTabIndex = GetTabIndex_Okulista();
    }

    /// <summary>
    /// Anuluje badanie okulistyczne: dezaktywuje zakładkę "Okulista"
    /// (zostaje ukryta) i wraca do pierwszej zakładki. Nie czyści SelectedWizyta.
    /// </summary>
    [RelayCommand]
    private void AnulujOkulista()
    {
        IsOkulistaActive = false;
        SelectedTabIndex = 0;
    }

    // ── Indeksy zakładek
    // Kolejność: 0=Orzeczenie, 1=Historia, 2=Zalecenia(hiden-Collapsed), 3=Karta Badań,
    //            4=Laryngolog, 5=Okulista, 6=Bad. sanitarne, 7=Urlop dla por. zdr.
    // Uwaga: zakładki Collapsed nadal zajmują miejsce w kolekcji Items,
    // dlatego używamy stałych indeksów.

    private static int GetTabIndex_Badanie()    => 0;
    private static int GetTabIndex_Historia() => 1; 
    private static int GetTabIndex_KartaBadan() => 2;
    private static int GetTabIndex_Laryngolog() => 3;
    private static int GetTabIndex_Okulista() => 4;
    private static int GetTabIndex_Zalecenia() => 5;
    private static int GetTabIndex_Sanitarne()  => 6;
    private static int GetTabIndex_Urlop()      => 7;

    // ── LoadFromDto: DTO → VM (ładowanie stanu z bazy) ───────────────────────

    /// <summary>
    /// Ładuje stany przycisków bistabilnych z DTO do ViewModelu.
    /// Callbacki OnIs*Changed automatycznie zapiszą wartości z powrotem do CurrentDto.
    /// Docelowo wywoływane po pobraniu danych z bazy przez serwis.
    /// </summary>
    public void LoadFromDto(OrzeczenieStatusDto dto)
    {
        CurrentDto = dto;

        IsBadanieActive    = dto.Btn_Badanie;
        IsLaryngologActive = dto.Btn_Laryngolog;
        IsOkulistaActive   = dto.Btn_Okulista;
        IsSanitarneActive  = dto.Btn_Sanitarne;
        IsUrlopActive      = dto.Btn_Urlop;
    }

    // ── Metody do wywołania z zewnątrz (np. przy ładowaniu danych z DB) ──────

    /// <summary>
    /// Ustawia stan przycisku Sanitarne z zewnątrz (np. z bazy danych).
    /// </summary>
    public void SetSanitarneState(bool isActive)
    {
        IsSanitarneActive = isActive;
    }

    /// <summary>
    /// Ustawia stan przycisku Urlop z zewnątrz (np. z bazy danych).
    /// </summary>
    public void SetUrlopState(bool isActive)
    {
        IsUrlopActive = isActive;
    }

    /// <summary>
    /// Ustawia stan przycisku Laryngolog z zewnątrz (np. z bazy danych).
    /// </summary>
    public void SetLaryngologState(bool isActive)
    {
        IsLaryngologActive = isActive;
    }

    /// <summary>
    /// Ustawia stan przycisku Okulista z zewnątrz (np. z bazy danych).
    /// </summary>
    public void SetOkulistaState(bool isActive)
    {
        IsOkulistaActive = isActive;
    }
}
