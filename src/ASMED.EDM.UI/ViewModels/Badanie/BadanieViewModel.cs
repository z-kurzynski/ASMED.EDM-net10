using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models.Badanie;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ASMED.EDM.UI.ViewModels.BadanieView;

/// <summary>
/// ViewModel dla widoku BadanieView (lewa kolumna - lista pacjentów).
/// Wzorowany na VisitsViewModel: dane z Rejestracja + B_Skierowania + P_Pacjent + Firma.
/// Filtr uwzględnia datę rejestracji (R_Data) porównywaną z CustomDefaultDate (kontrolka data_bad_dzien).
/// </summary>
public partial class BadanieViewModel : ObservableObject
{
    private readonly ILogger<BadanieViewModel> _logger;
    private readonly DbConnectionFactory? _dbFactory;

    private ObservableCollection<BadanieListItem> _allWizyty = new();

    public BadanieViewModel(ILogger<BadanieViewModel> logger, DbConnectionFactory? dbFactory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbFactory = dbFactory;

        OpenSkierPacjWoborCommand = new RelayCommand(ExecuteOpenSkierPacjWobor);
        AnulujCommand = new RelayCommand(ExecuteAnuluj);
        ZatwierdzCommand = new RelayCommand(ExecuteZatwierdz, CanExecuteZatwierdz);

        _tabDataBadania = _customDefaultDate;

        LoadWizytyFromDb();
    }

    [ObservableProperty]
    private ObservableCollection<BadanieListItem> _wizyty = new();

    [ObservableProperty]
    private BadanieListItem? _selectedWizyta;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DateTime? _customDefaultDate = DateTime.Today;

    /// <summary>
    /// Data badania w środkowej kolumnie (Data_Badania). Jest wstępnie ustawiana
    /// wartością daty domyślnej (CustomDefaultDate), ale po tym jest niezależna
    /// i może być swobodnie edytowana bez wpływu na datę domyślną.
    /// </summary>
    [ObservableProperty]
    private DateTime? _tabDataBadania;

    public ObservableCollection<string> FilterTypes { get; } = new() { "Wszystko", "Imię", "Nazwisko", "PESEL" };

    [ObservableProperty]
    private string _activeFilterType = "Wszystko";

    /// <summary>
    /// Toggle: pokazuj pozycje ze statusem "Rejestracja" lub "Dokumentacja". Domyślnie włączony.
    /// </summary>
    [ObservableProperty]
    private bool _showRejestracjaDokumentacja = false;

    /// <summary>
    /// Toggle: pokazuj pozycje ze statusem "W trakcie". Domyślnie włączony.
    /// </summary>
    [ObservableProperty]
    private bool _showWizyta = true;

    public IRelayCommand OpenSkierPacjWoborCommand { get; }

    /// <summary>
    /// Przycisk "Anuluj" - czyści zaznaczenie na liście (brak wybranego pacjenta).
    /// </summary>
    public IRelayCommand AnulujCommand { get; }

    /// <summary>
    /// Przycisk "Zatwierdź" - na razie tylko zmienia status wybranej pozycji na "Wywiad".
    /// </summary>
    public IRelayCommand ZatwierdzCommand { get; }

    private void ExecuteOpenSkierPacjWobor()
    {
        // TODO: logika do zaimplementowania później
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

        const string newStatus = "Wywiad";

        if (!UpdateStatusInDb(rId.Value, newStatus))
        {
            return;
        }

        SelectedWizyta.R_Status = newStatus;
        _logger.LogInformation("BadanieViewModel: status zmieniony na 'Wywiad' dla R_ID={R_ID}", rId);

        // Odswiez filtrowana liste - pozycja ze statusem "Wywiad" nie spelnia juz kryteriow filtra.
        ApplyFilter();
        SelectedWizyta = null;
    }

    private bool UpdateStatusInDb(int rId, string newStatus)
    {
        if (_dbFactory == null)
        {
            _logger.LogWarning("DbConnectionFactory not available, cannot update R_Status");
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
                _logger.LogWarning("UpdateStatusInDb: brak zaktualizowanych wierszy dla R_ID={R_ID}", rId);
            }

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blad podczas aktualizacji R_Status dla R_ID={R_ID}", rId);
            return false;
        }
    }

    partial void OnSelectedWizytaChanged(BadanieListItem? value)
    {
        ZatwierdzCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsWizytaSelected));

        if (value != null && value.B_DataSkierowania == null)
        {
            var baza = CustomDefaultDate ?? DateTime.Today;
            value.B_DataSkierowania = PoprzedniDzienRoboczy(baza);
            OnPropertyChanged(nameof(SelectedWizyta));
        }

        // Odczyt stanow bistabilnych przyciskow z wybranego rekordu (B_Skierowania).
        _suppressDbSave = true;
        try
        {
            IsEKGActive = value?.B_EKG ?? false;
            IsLipidogramActive = value?.B_Lipidogram ?? false;
            IsSanitarneActive = value?.B_ksiazeczka ?? false;
            IsUrlopActive = value?.B_Urlop ?? false;
            IsOkulistaActive = value?.B_okulista ?? false;
            IsBadanieActive = value?.B_Ankieta ?? false;
        }
        finally
        {
            _suppressDbSave = false;
        }
    }

    private bool _suppressDbSave;

    /// <summary>
    /// Czy przycisk "EKG" jest aktywny (B_Skierowania.B_EKG).
    /// </summary>
    [ObservableProperty]
    private bool _isEKGActive = false;

    /// <summary>
    /// Czy przycisk "Lipidogram" jest aktywny (B_Skierowania.B_Lipidogram).
    /// </summary>
    [ObservableProperty]
    private bool _isLipidogramActive = false;

    partial void OnIsEKGActiveChanged(bool value)
    {
        if (SelectedWizyta != null)
            SelectedWizyta.B_EKG = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_EKG", value);
    }

    partial void OnIsLipidogramActiveChanged(bool value)
    {
        if (SelectedWizyta != null)
            SelectedWizyta.B_Lipidogram = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_Lipidogram", value);
    }

    /// <summary>
    /// Czy przycisk "Sanitarne" jest aktywny (B_Skierowania.B_ksiazeczka).
    /// </summary>
    [ObservableProperty]
    private bool _isSanitarneActive = false;

    /// <summary>
    /// Czy przycisk "Urlop" jest aktywny (B_Skierowania.B_Urlop).
    /// </summary>
    [ObservableProperty]
    private bool _isUrlopActive = false;

    /// <summary>
    /// Czy przycisk "Okulista" jest aktywny (B_Skierowania.B_okulista).
    /// </summary>
    [ObservableProperty]
    private bool _isOkulistaActive = false;

    partial void OnIsSanitarneActiveChanged(bool value)
    {
        if (SelectedWizyta != null)
            SelectedWizyta.B_ksiazeczka = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_ksiazeczka", value);
    }

    partial void OnIsUrlopActiveChanged(bool value)
    {
        if (SelectedWizyta != null)
            SelectedWizyta.B_Urlop = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_Urlop", value);
    }

    partial void OnIsOkulistaActiveChanged(bool value)
    {
        if (SelectedWizyta != null)
            SelectedWizyta.B_okulista = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_okulista", value);
    }

    [RelayCommand]
    private void ToggleEKG() => IsEKGActive = !IsEKGActive;

    [RelayCommand]
    private void ToggleLipidogram() => IsLipidogramActive = !IsLipidogramActive;

    [RelayCommand]
    private void ToggleSanitarne() => IsSanitarneActive = !IsSanitarneActive;

    [RelayCommand]
    private void ToggleUrlop() => IsUrlopActive = !IsUrlopActive;

    [RelayCommand]
    private void ToggleOkulista() => IsOkulistaActive = !IsOkulistaActive;

    /// <summary>
    /// Czy przycisk "Ankieta" (Karta Badań) jest aktywny (B_Skierowania.B_Ankieta).
    /// </summary>
    [ObservableProperty]
    private bool _isBadanieActive = false;

    partial void OnIsBadanieActiveChanged(bool value)
    {
        if (SelectedWizyta != null)
            SelectedWizyta.B_Ankieta = value;
        if (!_suppressDbSave)
            UpdateBSkierowaniaFlagInDb("B_Ankieta", value);
    }

    [RelayCommand]
    private void ToggleBadanie() => IsBadanieActive = !IsBadanieActive;

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
            _logger.LogWarning("DbConnectionFactory not available, cannot update {Column}", columnName);
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
                _logger.LogWarning("UpdateBSkierowaniaFlagInDb: brak zaktualizowanych wierszy dla B_ID={B_ID}, kolumna={Column}", SelectedWizyta.B_ID, columnName);
            }

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blad podczas aktualizacji {Column} dla B_ID={B_ID}", columnName, SelectedWizyta.B_ID);
            return false;
        }
    }

    /// <summary>
    /// Czy jest wybrany rekord (wizyta/skierowanie) na liście. Steruje aktywnością
    /// przycisków bistabilnych Wstępne/Kontrolne/Okresowe.
    /// </summary>
    public bool IsWizytaSelected => SelectedWizyta != null;

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
            _logger.LogWarning("DbConnectionFactory not available, cannot update B_TypBadania");
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
                _logger.LogWarning("UpdateTypBadaniaInDb: brak zaktualizowanych wierszy dla B_ID={B_ID}", bId);
            }

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blad podczas aktualizacji B_TypBadania dla B_ID={B_ID}", bId);
            return false;
        }
    }

    [RelayCommand]
    private void ToggleWstepne() => ToggleTypBadania("W");

    [RelayCommand]
    private void ToggleKontrolne() => ToggleTypBadania("K");

    [RelayCommand]
    private void ToggleOkresowe() => ToggleTypBadania("O");

    /// <summary>
    /// Zapisuje nową wartość B_DataSkierowania do bazy dla wybranego rekordu.
    /// Wywoływane po zmianie daty w kontrolce DatePicker (Data_Skierowania).
    /// </summary>
    public void SaveDataSkierowaniaToDb()
    {
        if (SelectedWizyta == null || SelectedWizyta.B_ID == null) return;

        if (!UpdateDataSkierowaniaInDb(SelectedWizyta.B_ID.Value, SelectedWizyta.B_DataSkierowania))
            return;

        _logger.LogInformation("BadanieViewModel: zapisano B_DataSkierowania dla B_ID={B_ID}", SelectedWizyta.B_ID);
    }

    private bool UpdateDataSkierowaniaInDb(int bId, DateTime? newData)
    {
        if (_dbFactory == null)
        {
            _logger.LogWarning("DbConnectionFactory not available, cannot update B_DataSkierowania");
            return false;
        }

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = "UPDATE B_Skierowania SET B_DataSkierowania = @data WHERE B_ID = @id";
            cmd.Parameters.AddWithValue("@data", newData.HasValue ? newData.Value : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", bId);
            var affected = cmd.ExecuteNonQuery();

            if (affected == 0)
            {
                _logger.LogWarning("UpdateDataSkierowaniaInDb: brak zaktualizowanych wierszy dla B_ID={B_ID}", bId);
            }

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blad podczas aktualizacji B_DataSkierowania dla B_ID={B_ID}", bId);
            return false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnActiveFilterTypeChanged(string value) => ApplyFilter();

    partial void OnCustomDefaultDateChanged(DateTime? value)
    {
        ApplyFilter();
        TabDataBadania = value;
    }

    partial void OnShowRejestracjaDokumentacjaChanged(bool value) => ApplyFilter();

    partial void OnShowWizytaChanged(bool value) => ApplyFilter();

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

    private void LoadWizytyFromDb(bool silent = false)
    {
        if (_dbFactory == null)
        {
            _logger.LogWarning("DbConnectionFactory not available, skipping Wizyty load");
            return;
        }

        var selectedId = SelectedWizyta?.R_ID;

        try
        {
            var loaded = new ObservableCollection<BadanieListItem>();

            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    r.R_ID, r.R_Data, r.R_Status,
                    s.B_ID, s.B_DataSkierowania, s.B_TypBadania, s.B_Stanowisko,
                    s.B_EKG, s.B_Lipidogram, s.B_ksiazeczka, s.B_Urlop, s.B_okulista, s.B_Ankieta,
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
                loaded.Add(new BadanieListItem
                {
                    R_ID = reader["R_ID"] != DBNull.Value ? Convert.ToInt32(reader["R_ID"]) : null,
                    R_Data = reader["R_Data"] != DBNull.Value ? Convert.ToDateTime(reader["R_Data"]) : null,
                    R_Status = reader["R_Status"]?.ToString(),

                    B_ID = reader["B_ID"] != DBNull.Value ? Convert.ToInt32(reader["B_ID"]) : null,
                    B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : null,
                    B_typ = reader["B_TypBadania"]?.ToString(),
                    B_Stanowisko = reader["B_Stanowisko"]?.ToString(),

                    B_EKG = reader["B_EKG"] != DBNull.Value && Convert.ToBoolean(reader["B_EKG"]),
                    B_Lipidogram = reader["B_Lipidogram"] != DBNull.Value && Convert.ToBoolean(reader["B_Lipidogram"]),
                    B_ksiazeczka = reader["B_ksiazeczka"] != DBNull.Value && Convert.ToBoolean(reader["B_ksiazeczka"]),
                    B_Urlop = reader["B_Urlop"] != DBNull.Value && Convert.ToBoolean(reader["B_Urlop"]),
                    B_okulista = reader["B_okulista"] != DBNull.Value && Convert.ToBoolean(reader["B_okulista"]),
                    B_Ankieta = reader["B_Ankieta"] != DBNull.Value && Convert.ToBoolean(reader["B_Ankieta"]),

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
            _logger.LogInformation("BadanieViewModel: loaded {Count} rows", loaded.Count);

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
                _logger.LogWarning(ex, "BadanieViewModel: silent refresh failed (non-critical)");
            }
            else
            {
                _logger.LogError(ex, "BadanieViewModel: error loading Wizyty from DB");
            }
        }
    }

    /// <summary>
    /// Ciche odswiezenie listy Wizyty (bez przeladowania calego widoku/VM).
    /// Zachowuje zaznaczenie (SelectedWizyta). Przeznaczone do cyklicznego wywolywania z timera
    /// w code-behind, gdy widok ma fokus/jest widoczny.
    /// </summary>
    public void RefreshSilent()
    {
        LoadWizytyFromDb(silent: true);
    }

    private void ApplyFilter()
    {
        IEnumerable<BadanieListItem> query = _allWizyty.Where(x =>
            (ShowRejestracjaDokumentacja && IsRejestracjaLubDokumentacja(x.R_Status)) ||
            (ShowWizyta && IsWTrakcie(x.R_Status)));

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

        Wizyty = new ObservableCollection<BadanieListItem>(query);
    }
}
