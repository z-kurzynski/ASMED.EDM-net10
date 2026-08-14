using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Collections.ObjectModel;
using System.Globalization;
using Syncfusion.UI.Xaml.Scheduler;
using System.Threading.Tasks;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// ViewModel dla modułu Wizyt
/// Legacy: ViewModels\Wizyty\WizytyViewViewModel.cs
/// </summary>
public partial class VisitsViewModel : ViewModelBase
{
    private readonly ILogger<VisitsViewModel> _logger;
    private readonly DbConnectionFactory? _dbFactory;
    private readonly INotificationService _notificationService;
    private bool _isLoadingDetails = false; // Flaga zapobiegająca rekurencji

    public VisitsViewModel(ILogger<VisitsViewModel> logger, DbConnectionFactory? dbFactory = null, INotificationService? notificationService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbFactory = dbFactory;
        _notificationService = notificationService ?? new NotificationService();
        _logger.LogInformation("VisitsViewModel initialized");

        // Zaladuj rejestracje (wizyty) z bazy MySQL
        LoadAppointmentsFromDb();

        // Załaduj dane autocomplete dla formularza edycji pacjenta
        LoadAutoCompleteData();
    }

    // ═══════════════════════════════════════════════════════
    // ✅ AUTOCOMPLETE — dane podpowiedzi dla pól edycji
    // ═══════════════════════════════════════════════════════
    public ObservableCollection<string> ImionaItems   { get; } = new();
    public ObservableCollection<string> NazwiskaItems { get; } = new();
    public ObservableCollection<string> ZawodyItems   { get; } = new();
    public ObservableCollection<string> UliceItems    { get; } = new();
    public ObservableCollection<string> MiastoItems   { get; } = new();

    private void LoadAutoCompleteData()
    {
        if (_dbFactory == null) return;
        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            LoadSimpleList(conn, "SELECT DISTINCT P_imie       FROM P_Pacjent WHERE P_imie       IS NOT NULL ORDER BY P_imie",       "P_imie",       ImionaItems);
            LoadSimpleList(conn, "SELECT DISTINCT P_nazwisko   FROM P_Pacjent WHERE P_nazwisko   IS NOT NULL ORDER BY P_nazwisko",   "P_nazwisko",   NazwiskaItems);
            LoadSimpleList(conn, "SELECT DISTINCT P_zawod      FROM P_Pacjent WHERE P_zawod      IS NOT NULL ORDER BY P_zawod",      "P_zawod",      ZawodyItems);
            LoadSimpleList(conn, "SELECT S_Ulica FROM S__Ulice ORDER BY S_Ulica",                                                    "S_Ulica",      UliceItems);
            LoadSimpleList(conn, "SELECT DISTINCT P_Ades_miasto FROM P_Pacjent WHERE P_Ades_miasto IS NOT NULL ORDER BY P_Ades_miasto", "P_Ades_miasto", MiastoItems);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AutoComplete data load failed (non-critical)");
        }
    }

    private static void LoadSimpleList(System.Data.Common.DbConnection conn, string sql, string column, ObservableCollection<string> target)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();
        target.Clear();
        while (reader.Read())
        {
            var val = reader[column]?.ToString();
            if (!string.IsNullOrWhiteSpace(val))
                target.Add(val);
        }
    }

    private int GetStatusRank(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return int.MaxValue;
        status = status.Trim().ToLowerInvariant();
        return status switch
        {
            "rejestracja" => 0,
            "dokumentacja" => 1,
            "w trakcie" => 2,
            "wywiad" => 3,
            "orzeczenie" => 4,
            "zamknięta" => 5,
            _ => 10
        };
    }

    #region Properties

    [ObservableProperty]
    private DateTime? _selectedDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _displayDate = DateTime.Today;

    [ObservableProperty]
    private bool _useCustomDefaultDateCalendar;

    [ObservableProperty]
    private DateTime _customDefaultDateCalendar = DateTime.Today;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<VisitPatientItem> _pacjenciNaDzien = new();

    [ObservableProperty]
    private ObservableCollection<VisitPatientItem> _filteredPacjenciNaDzien = new();

    [ObservableProperty]
    private VisitPatientItem? _selectedPacjent;

    public VisitPatientItem? WybranyPacjent
    {
        get => SelectedPacjent;
        set => SelectedPacjent = value;
    }

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _completedCount;
    [ObservableProperty]
    private int _otherStatusCount;

    /// <summary>
    /// Kolekcja appointmentów dla Syncfusion SfScheduler
    /// </summary>
    [ObservableProperty]
    private ScheduleAppointmentCollection _appointments = new();

    /// <summary>
    /// Widoczność widoku szczegółów karty badań (gdy pacjent wybrany)
    /// </summary>
    [ObservableProperty]
    private bool _showDetailsView;

    /// <summary>
    /// Widoczność widoku statystyk (domyślnie widoczny gdy brak wyboru)
    /// </summary>
    [ObservableProperty]
    private bool _showStatsView = true;

    [ObservableProperty]
    private bool _brakPesel;

    public bool BrakPESEL
    {
        get => BrakPesel;
        set => BrakPesel = value;
    }

    // IsGroupW nowe metody do grupowania wizyt w widoku kalendarza (Syncfusion SfScheduler)

    // Typ badania W/O/K
    private bool _isGroupW = true;
    public bool IsGroupW { get => _isGroupW; set { SetProperty(ref _isGroupW, value); UpdateTestType(); } }

    private bool _isGroupO;
    public bool IsGroupO { get => _isGroupO; set { SetProperty(ref _isGroupO, value); UpdateTestType(); } }

    private bool _isGroupK;
    public bool IsGroupK { get => _isGroupK; set { SetProperty(ref _isGroupK, value); UpdateTestType(); } }

    private bool _isGroupS;
    public bool IsGroupS { get => _isGroupS; set { SetProperty(ref _isGroupS, value); UpdateTestType(); } }

    private bool _isGroupU;
    public bool IsGroupU { get => _isGroupU; set { SetProperty(ref _isGroupU, value); UpdateTestType(); } }

    /// <summary>
    /// Aktualizuje TestType oraz zapisuje wybrany typ badania bezpośrednio do SelectedPacjent.B_TypBadania
    /// (pole tekstowe ToolTip="Wybrany typ badania" jest bindowane właśnie do tej właściwości modelu).
    /// </summary>
    private void UpdateTestType()
    {
        TestType = IsGroupW ? "W" : IsGroupO ? "O" : IsGroupS ? "S" : IsGroupK ? "K" : IsGroupU ? "U" : string.Empty;

        if (SelectedPacjent != null && SelectedPacjent.B_TypBadania != TestType)
        {
            SelectedPacjent.B_TypBadania = TestType;
            // VisitPatientItem nie implementuje INotifyPropertyChanged, więc wymuszamy odświeżenie
            // powiązanego z nim bindowania (np. TextBox Text="{Binding SelectedPacjent.B_TypBadania}").
            OnPropertyChanged(nameof(SelectedPacjent));
        }
    }

    private string _testType = "W";
    public string TestType
    {
        get => _testType;
        set => SetProperty(ref _testType, value);
    }

    /// <summary>
    /// Synchronizuje stan radiobuttonów W/O/K/S/U z wartością SelectedPacjent.B_TypBadania.
    /// Wywoływane po zmianie/wczytaniu wybranego pacjenta, aby radiobutton odzwierciedlał zapisane dane.
    /// </summary>
    private void SyncTypBadaniaGroupsFromSelectedPacjent()
    {
        var typ = SelectedPacjent?.B_TypBadania;

        _isGroupW = typ == "W";
        _isGroupO = typ == "O";
        _isGroupK = typ == "K" || typ == "K/O";
        _isGroupS = typ == "S";
        _isGroupU = typ == "U";

        OnPropertyChanged(nameof(IsGroupW));
        OnPropertyChanged(nameof(IsGroupO));
        OnPropertyChanged(nameof(IsGroupK));
        OnPropertyChanged(nameof(IsGroupS));
        OnPropertyChanged(nameof(IsGroupU));

        _testType = typ ?? string.Empty;
        OnPropertyChanged(nameof(TestType));
    }

    public string SelectedDateFormatted =>
        SelectedDate?.ToString("dddd, dd MMMM yyyy", new CultureInfo("pl-PL")) ?? "Wybierz datę w kalendarzu";

    #endregion

    #region Wydruki

    [ObservableProperty]
    private bool _dodajDateBadaniaDoWydruku;

    [ObservableProperty]
    private DateTime? _dataBadaniaDoWydruku = DateTime.Today;

    private readonly ASMED.EDM.Printing.Services.PatientFormPrintService _printService = new();

        /// <summary>
        /// Mapuje aktualnie wybranego pacjenta (VisitPatientItem) na wspólny DTO drukowania (PatientPrintData).
        /// </summary>
        private ASMED.EDM.Printing.Models.PatientPrintData? MapSelectedPacjentToPrintData()
        {
            if (SelectedPacjent == null)
                return null;

            return new ASMED.EDM.Printing.Models.PatientPrintData
            {
                B_ID = SelectedPacjent.B_ID,
                P_ID = SelectedPacjent.P_ID,
                P_Imie = SelectedPacjent.P_Imie,
                P_Nazwisko = SelectedPacjent.P_Nazwisko,
                P_Pesel = SelectedPacjent.P_Pesel,
                P_Plec = SelectedPacjent.P_Plec,
                P_DataUrodzenia = SelectedPacjent.P_Data_Urodzenia,
                P_Zawod = SelectedPacjent.P_Zawod,
                P_AdresUlica = SelectedPacjent.P_AdresUlica,
                P_AdresKod = SelectedPacjent.P_AdresKod,
                P_AdresMiasto = SelectedPacjent.P_AdresMiasto,
                Firma_Nazwa = SelectedPacjent.Firma_Nazwa,
                Firma_Ulica = SelectedPacjent.Firma_Ulica,
                Firma_Kod = SelectedPacjent.Firma_Kod,
                Firma_Miejscowosc = SelectedPacjent.Firma_Miejscowosc,
                Firma_Email = SelectedPacjent.Firma_Email,
                B_DataSkierowania = SelectedPacjent.B_DataSkierowania,
                B_TypBadania = SelectedPacjent.B_TypBadania,
                B_Stanowisko = SelectedPacjent.B_Stanowisko,
                B_RegistrationDate = SelectedPacjent.B_RegistrationDate,
                B_CzynnikFizyczny = SelectedPacjent.B_CzynnikFizyczny,
                B_CzynnikFizycznyOpis = SelectedPacjent.B_CzynnikFizycznyOpis,
                B_CzynnikPylowy = SelectedPacjent.B_CzynnikPylowy,
                B_CzynnikPylowyOpis = SelectedPacjent.B_CzynnikPylowyOpis,
                B_CzynnikChemiczny = SelectedPacjent.B_CzynnikChemiczny,
                B_CzynnikChemicznyOpis = SelectedPacjent.B_CzynnikChemicznyOpis,
                B_CzynnikBiologiczny = SelectedPacjent.B_CzynnikBiologiczny,
                B_CzynnikBiologicznyOpis = SelectedPacjent.B_CzynnikBiologicznyOpis,
                B_CzynnikInny = SelectedPacjent.B_CzynnikInny,
                B_CzynnikInnyOpis = SelectedPacjent.B_CzynnikInnyOpis,
                SkierowanieNumer = SelectedPacjent.SkierowanieNumer,
                DataBadaniaDoWydruku = DodajDateBadaniaDoWydruku ? DataBadaniaDoWydruku : null
            };
        }

        /// <summary>
        /// Wspólna logika wydruku formularza pacjenta - wypełnia szablon i pokazuje podgląd PDF.
        /// </summary>
        private void PrintPatientForm(string templateFileName, string successMessage)
        {
            if (SelectedPacjent == null || !SelectedPacjent.B_ID.HasValue)
            {
                _notificationService.ShowToast("Wybierz pacjenta z listy aby wydrukować formularz.", ToastType.Warning);
                return;
            }

            var data = MapSelectedPacjentToPrintData();
            if (data == null)
                return;

            var result = _printService.ShowPreview(data, templateFileName, System.Windows.Application.Current.MainWindow);

            if (result.Success)
            {
                _notificationService.ShowToast(successMessage, ToastType.Success);
            }
            else
            {
                _notificationService.ShowToast($"Błąd wydruku: {result.ErrorMessage}", ToastType.Error);
            }
        }

        [RelayCommand]
        private void PrintAll() => PrintPatientForm("ASMED__Karta badania Orzeczenie.pdf", "Wygenerowano Komplet formularzy");

        [RelayCommand]
        private void PrintKarta() => PrintPatientForm(ASMED.EDM.Printing.Services.PatientFormTemplates.Karta, "Wygenerowano Kartę badania");

        [RelayCommand]
        private void PrintOrzeczenie() => PrintPatientForm(ASMED.EDM.Printing.Services.PatientFormTemplates.Orzeczenie, "Wygenerowano Orzeczenie");

        [RelayCommand]
        private void PrintSanitarne() => PrintPatientForm(ASMED.EDM.Printing.Services.PatientFormTemplates.Sanitarne, "Wygenerowano książeczkę sanitarną");

        [RelayCommand]
        private void PrintUrlop() { /* TODO: implementacja wydruku urlopowego */ }

        [RelayCommand]
        private void PrintAnkieta() => PrintPatientForm(ASMED.EDM.Printing.Services.PatientFormTemplates.Ankieta, "Wygenerowano Ankietę");

    #endregion

    #region Status karty badań

    /// <summary>Wywoływane przez code-behind widoku — przełącza zakładkę na Zakończ Badanie.</summary>
    public event Action? NavigateToZakonczBadanie;

    [RelayCommand]
    private void SetStatusDokumenty()
    {
        ChangePatientStatus("Dokumentacja");
    }

    [RelayCommand]
    private void SetStatusWizyta()
    {
        ChangePatientStatus("W trakcie");
    }

    [RelayCommand]
    private void SetStatusRejestracja()
    {
        ChangePatientStatus("Rejestracja");
    }

    [RelayCommand]
    private void SetStatusZamknij()
    {
        BackToList();
        NavigateToZakonczBadanie?.Invoke();
    }

    /// <summary>
    /// Zmienia status karty pacjenta w bazie danych.
    /// Walidacja: dozwolone tylko ze stanów 'rejestracja', 'dokumentacja', 'w trakcie'
    /// </summary>
    private void ChangePatientStatus(string newStatus)
    {
        if (SelectedPacjent == null)
        {
            _logger.LogWarning("Nie wybrano pacjenta do zmiany statusu");
            return;
        }

        var currentStatus = SelectedPacjent.R_Status?.ToLower();

        // Walidacja: dozwolone tylko ze stanów rejestracja, dokumentacja, w trakcie
        if (currentStatus != "rejestracja" && currentStatus != "dokumentacja" && currentStatus != "w trakcie")
        {
            _logger.LogWarning("Zmiana statusu niemożliwa. Aktualny status: {Status}", SelectedPacjent.R_Status);
            return;
        }

        if (_dbFactory == null)
        {
            _logger.LogError("DbConnectionFactory jest null - nie można zaktualizować statusu");
            return;
        }

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();

            cmd.CommandText = "UPDATE Rejestracja SET R_Status = @status WHERE R_ID = @rid";
            cmd.Parameters.AddWithValue("@status", newStatus);
            cmd.Parameters.AddWithValue("@rid", SelectedPacjent.R_ID);

            int rows = cmd.ExecuteNonQuery();

            if (rows > 0)
            {
                // Aktualizuj lokalnie
                SelectedPacjent.R_Status = newStatus;
                OnPropertyChanged(nameof(SelectedPacjent));

                // Odśwież listę z bazy danych, aby zaktualizować sortowanie
                LoadAppointmentsFromDb();

                _logger.LogInformation("Status zmieniony: R_ID={RID} → {NewStatus}", SelectedPacjent.R_ID, newStatus);
            }
            else
            {
                _logger.LogWarning("Nie zaktualizowano żadnego rekordu dla R_ID={RID}", SelectedPacjent.R_ID);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zmiany statusu dla R_ID={RID}", SelectedPacjent.R_ID);
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Refresh()
    {
        _logger.LogInformation("Refreshing visits list");
        LoadAppointmentsFromDb();
        LoadPacjenciNaDzien();
    }

    /// <summary>
    /// Alias dla RefreshCommand - zgodny z legacy binding "OdswiezCommand"
    /// </summary>
    public IRelayCommand OdswiezCommand => RefreshCommand;

    [RelayCommand]
    private void AddNewVisit()
    {
        _logger.LogInformation("Adding new visit");
        // TODO: Open add visit dialog
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void BackToList()
    {
        SelectedPacjent = null;
    }

    [RelayCommand]
    private async Task SaveSelectedPacjent()
    {
        if (SelectedPacjent == null)
        {
            _logger.LogWarning("Save requested but no SelectedPacjent is set");
            _notificationService.ShowToast("Brak wybranego pacjenta do zapisu.", ToastType.Warning);
            return;
        }

        _logger.LogInformation("Saving SelectedPacjent P_ID={Id}", SelectedPacjent.P_ID);
        try
        {
            if (_dbFactory == null)
            {
                _logger.LogWarning("Cannot save - DbConnectionFactory not available");
                _notificationService.ShowToast("Nie można zapisać - brak połączenia z bazą danych.", ToastType.Error);
                return;
            }

            var imie = SelectedPacjent.P_Imie ?? string.Empty;
            var nazwisko = SelectedPacjent.P_Nazwisko ?? string.Empty;

            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();
            using var cmd = (MySqlCommand)conn.CreateCommand();

            cmd.CommandText = @"
                UPDATE P_Pacjent
                SET P_imie = @imie,
                    P_nazwisko = @nazwisko,
                    P_pesel = @pesel,
                    P_brak = @brakPesel,
                    P_telefon = @telefon,
                    P_email = @email,
                    P_plec = @plec,
                    P_Data_Urodzenia = @dataUrodzenia,
                    P_zawod = @zawod,
                    P_Adres_ulica_numer = @adresUlica,
                    P_Ades_kod = @adresKod,
                    P_Ades_miasto = @adresMiasto
                WHERE P_ID = @id";

            cmd.Parameters.AddWithValue("@imie", SelectedPacjent.P_Imie ?? string.Empty);
            cmd.Parameters.AddWithValue("@nazwisko", SelectedPacjent.P_Nazwisko ?? string.Empty);
            if (BrakPESEL || string.IsNullOrWhiteSpace(SelectedPacjent.P_Pesel))
            {
                cmd.Parameters.AddWithValue("@pesel", DBNull.Value);
                cmd.Parameters.AddWithValue("@brakPesel", 1);
            }
            else
            {
                cmd.Parameters.AddWithValue("@pesel", SelectedPacjent.P_Pesel);
                cmd.Parameters.AddWithValue("@brakPesel", 0);
            }

            cmd.Parameters.AddWithValue("@telefon", SelectedPacjent.P_Telefon ?? string.Empty);
            cmd.Parameters.AddWithValue("@email", SelectedPacjent.P_Email ?? string.Empty);
            cmd.Parameters.AddWithValue("@plec", SelectedPacjent.P_Plec ?? string.Empty);
            cmd.Parameters.AddWithValue("@dataUrodzenia", SelectedPacjent.P_Data_Urodzenia is null ? (object)DBNull.Value : SelectedPacjent.P_Data_Urodzenia);
            cmd.Parameters.AddWithValue("@zawod", SelectedPacjent.P_Zawod ?? string.Empty);
            cmd.Parameters.AddWithValue("@adresUlica", SelectedPacjent.P_AdresUlica ?? string.Empty);
            cmd.Parameters.AddWithValue("@adresKod", SelectedPacjent.P_AdresKod ?? string.Empty);
            cmd.Parameters.AddWithValue("@adresMiasto", SelectedPacjent.P_AdresMiasto ?? string.Empty);
            cmd.Parameters.AddWithValue("@id", SelectedPacjent.P_ID ?? (object)DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("SaveSelectedPacjent updated rows: {Rows}", rows);

            // Zaktualizuj dane skierowania (B_Skierowania), jeśli pacjent je posiada
            if (SelectedPacjent.B_ID.HasValue)
            {
                using var skierowanieCmd = (MySqlCommand)conn.CreateCommand();
                skierowanieCmd.CommandText = @"
                    UPDATE B_Skierowania
                    SET B_DataSkierowania = @dataSkierowania,
                        B_TypBadania = @typBadania,
                        B_Stanowisko = @stanowisko,
                        B_ksiazeczka = @ksiazeczka,
                        B_Zaswiadczenie = @zaswiadczenie
                    WHERE B_ID = @bId";

                skierowanieCmd.Parameters.AddWithValue("@dataSkierowania", SelectedPacjent.B_DataSkierowania is null ? (object)DBNull.Value : SelectedPacjent.B_DataSkierowania);
                skierowanieCmd.Parameters.AddWithValue("@typBadania", SelectedPacjent.B_TypBadania ?? string.Empty);
                skierowanieCmd.Parameters.AddWithValue("@stanowisko", SelectedPacjent.B_Stanowisko ?? string.Empty);
                skierowanieCmd.Parameters.AddWithValue("@ksiazeczka", SelectedPacjent.B_Ksiazeczka is null ? (object)DBNull.Value : SelectedPacjent.B_Ksiazeczka.Value);
                skierowanieCmd.Parameters.AddWithValue("@zaswiadczenie", SelectedPacjent.B_Zaswiadczenie is null ? (object)DBNull.Value : SelectedPacjent.B_Zaswiadczenie.Value);
                skierowanieCmd.Parameters.AddWithValue("@bId", SelectedPacjent.B_ID.Value);

                var skierowanieRows = await skierowanieCmd.ExecuteNonQueryAsync();
                _logger.LogInformation("SaveSelectedPacjent updated B_Skierowania rows: {Rows}", skierowanieRows);

                if (skierowanieRows == 0)
                {
                    _logger.LogWarning("Nie zaktualizowano żadnego wiersza B_Skierowania dla B_ID={BId}", SelectedPacjent.B_ID);
                    _notificationService.ShowToast("Dane pacjenta zapisano, ale nie udało się zaktualizować danych skierowania.", ToastType.Warning);
                }
            }

            // Refresh local collections to reflect saved changes
            LoadPacjenciNaDzien();
            OnPropertyChanged(nameof(FilteredPacjenciNaDzien));
            _logger.LogInformation("SaveSelectedPacjent completed");
            _notificationService.ShowToast($"Dane pacjenta {imie} {nazwisko} zostały zapisane.", ToastType.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving SelectedPacjent");
            _notificationService.ShowToast($"Błąd zapisu: {ex.Message}", ToastType.Error);
        }
    }

    [RelayCommand]
    private void WydrukListy()
    {
        _logger.LogInformation("Printing patient list");
        // TODO: Implement patient list printing
    }

    #endregion

    #region Methods

    partial void OnSelectedDateChanged(DateTime? value)
    {
        _logger.LogInformation("📅 SelectedDate changed to: {Date}", value?.ToString("yyyy-MM-dd") ?? "NULL");
        OnPropertyChanged(nameof(SelectedDateFormatted));
        LoadPacjenciNaDzien();
    }

    // Reaguj na zmianę checkboxa UseCustomDefaultDateCalendar (legacy behavior)
    partial void OnUseCustomDefaultDateCalendarChanged(bool value)
    {
        _logger.LogInformation("UseCustomDefaultDateCalendar changed: {Value}", value);
        if (value)
        {
            // Włączone -> wymuś wybraną domyślną datę
            SelectedDate = CustomDefaultDateCalendar;
        }
        else
        {
            // Wyłączone -> ustaw dzisiaj
            SelectedDate = DateTime.Now;
        }
    }

    // Reaguj na zmianę CustomDefaultDateCalendar gdy checkbox jest włączony
    partial void OnCustomDefaultDateCalendarChanged(DateTime value)
    {
        _logger.LogInformation("CustomDefaultDateCalendar changed: {Value}", value.ToString("yyyy-MM-dd"));
        if (UseCustomDefaultDateCalendar)
        {
            SelectedDate = value;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterPacjenci();
    }

    partial void OnSelectedPacjentChanged(VisitPatientItem? value)
    {
        // Przełączamy widoki w prawej kolumnie
        if (value != null)
        {
            ShowDetailsView = true;
            ShowStatsView = false;
            _logger.LogInformation("Selected patient: {Name}, switching to details view", value.FullName);

            // Zsynchronizuj radiobuttony W/O/K/S/U z aktualnym B_TypBadania wybranego pacjenta
            SyncTypBadaniaGroupsFromSelectedPacjent();

            // Załaduj pełne dane pacjenta i skierowania z DB po B_ID (jeśli dostępne)
            // Tylko jeśli nie jesteśmy już w trakcie ładowania (zapobiega rekurencji)
            if (!_isLoadingDetails && value.B_ID.HasValue)
            {
                try
                {
                    LoadPatientAndReferralDetails(value.B_ID.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading full patient/referral details for B_ID={BId}", value.B_ID);
                }
            }
        }
        else
        {
            ShowDetailsView = false;
            ShowStatsView = true;
            _logger.LogInformation("No patient selected, switching to stats view");
        }
    }

    /// <summary>
    /// Ładuje szczegółowe dane pacjenta i skierowania według B_Skierowania.B_ID
    /// Używane gdy użytkownik kliknie pozycję na liście (przekazujemy B_ID).
    /// </summary>
    private void LoadPatientAndReferralDetails(int bId)
    {
        if (_dbFactory == null)
        {
            _logger.LogWarning("DbConnectionFactory not available");
            return;
        }

        _isLoadingDetails = true;
        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();

            cmd.CommandText = @"
                SELECT
                    P_Pacjent.P_ID,
                    P_Pacjent.P_imie,
                    P_Pacjent.P_nazwisko,
                    P_Pacjent.P_pesel,
                    P_Pacjent.P_brak,
                    P_Pacjent.P_plec,
                    P_Pacjent.P_data_urodzenia,
                    P_Pacjent.P_zawod,
                    P_Pacjent.P_telefon,
                    P_Pacjent.P_email,
                    P_Pacjent.P_Ades_kod,
                    P_Pacjent.P_Ades_miasto,
                    P_Pacjent.P_Adres_ulica_numer,
                    Firma.Nazwa AS Firma_Nazwa,
                    Firma.Kod AS Firma_Kod,
                    Firma.Miejscowosc AS Firma_Miejscowosc,
                    Firma.Ulica AS Firma_Ulica,
                    B_Skierowania.B_ID,
                    B_Skierowania.B_Stanowisko,
                    B_Skierowania.B_ksiazeczka,
                    B_Skierowania.B_Zaswiadczenie,
                    B_Skierowania.B_TypBadania,
                    B_Skierowania.B_DataSkierowania,
                    Rejestracja.R_ID,
                    Rejestracja.R_Data,
                    Rejestracja.R_Status,
                    Rejestracja.R_GG_MM
                FROM
                    (Firma
                    INNER JOIN (P_Pacjent
                        INNER JOIN B_Skierowania ON P_Pacjent.P_ID = B_Skierowania.B_Pacjent_ID)
                    ON Firma.id = P_Pacjent.P_Firma_id)
                    INNER JOIN Rejestracja ON B_Skierowania.B_ID = Rejestracja.R_S_ID
                WHERE B_Skierowania.B_ID = @bId
                LIMIT 1";

            cmd.Parameters.AddWithValue("@bId", bId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var item = new VisitPatientItem
                {
                    P_ID = reader["P_ID"] is DBNull ? null : Convert.ToInt32(reader["P_ID"]),
                    P_Imie = reader["P_imie"]?.ToString(),
                    P_Nazwisko = reader["P_nazwisko"]?.ToString(),
                    P_Pesel = reader["P_pesel"]?.ToString(),
                    P_Brak = reader["P_brak"] != DBNull.Value && Convert.ToBoolean(reader["P_brak"]),
                    P_Plec = reader["P_plec"]?.ToString(),
                    P_Data_Urodzenia = reader["P_data_urodzenia"] != DBNull.Value ? Convert.ToDateTime(reader["P_data_urodzenia"]) : null,
                    P_Zawod = reader["P_zawod"]?.ToString(),
                    P_Telefon = reader["P_telefon"]?.ToString(),
                    P_Email = reader["P_email"]?.ToString(),
                    P_AdresKod = reader["P_Ades_kod"]?.ToString(),
                    P_AdresMiasto = reader["P_Ades_miasto"]?.ToString(),
                    P_AdresUlica = reader["P_Adres_ulica_numer"]?.ToString(),
                    Firma_Nazwa = reader["Firma_Nazwa"]?.ToString(),
                    Firma_Kod = reader["Firma_Kod"]?.ToString(),
                    Firma_Miejscowosc = reader["Firma_Miejscowosc"]?.ToString(),
                    Firma_Ulica = reader["Firma_Ulica"]?.ToString(),
                    B_ID = reader["B_ID"] is DBNull ? null : Convert.ToInt32(reader["B_ID"]),
                    B_Stanowisko = reader["B_Stanowisko"]?.ToString(),
                    B_Ksiazeczka = reader["B_ksiazeczka"] != DBNull.Value && Convert.ToBoolean(reader["B_ksiazeczka"]),
                    B_Zaswiadczenie = reader["B_Zaswiadczenie"] != DBNull.Value && Convert.ToBoolean(reader["B_Zaswiadczenie"]),
                    B_TypBadania = reader["B_TypBadania"]?.ToString(),
                    B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : null,
                    R_ID = reader["R_ID"] is DBNull ? null : Convert.ToInt32(reader["R_ID"]),
                    R_Data = reader["R_Data"] != DBNull.Value ? Convert.ToDateTime(reader["R_Data"]) : null,
                    R_GG_MM = reader["R_GG_MM"] != DBNull.Value ? Convert.ToDateTime(reader["R_GG_MM"]) : null,
                    R_Status = reader["R_Status"]?.ToString()
                };

                // Ustaw załadowany obiekt jako wybrany
                SelectedPacjent = item;

                // Ustaw checkbox BrakPESEL
                BrakPESEL = item.P_Brak ?? false;

                _logger.LogInformation("Loaded details for patient: {Name}, PESEL: {Pesel}, B_ID: {BId}, TypBadania: {Typ}, DataSkierowania: {Data}", 
                    item.FullName, item.P_Pesel, item.B_ID, item.B_TypBadania, item.B_DataSkierowania);
            }
            else
            {
                _logger.LogWarning("No record found for B_ID={BId}", bId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading patient details for B_ID={BId}", bId);
        }
        finally
        {
            _isLoadingDetails = false;
        }
    }

    /// <summary>
    /// Laduje wszystkie rejestracje (wizyty) z bazy MySQL i wypelnia scheduler.
    /// Legacy: AccessDbContext.GetRejestracje()
    /// </summary>
    /// <param name="silent">
    /// Gdy true: dane s? scalane z istniej?c? kolekcj? FilteredPacjenciNaDzien (bez podmiany instancji),
    /// dzi?ki czemu zachowywane s? zaznaczenie oraz pozycja przewijania listy w UI.
    /// U?ywane przez cykliczne, ciche od?wie?anie (np. timer w wielostanowiskowym trybie pracy).
    /// </param>
    private void LoadAppointmentsFromDb(bool silent = false)
    {
        if (_dbFactory == null)
        {
            _logger.LogWarning("DbConnectionFactory not available, skipping appointments load");
            return;
        }

        try
        {
            var newAppointments = new ScheduleAppointmentCollection();
            var newPacjenci = new ObservableCollection<VisitPatientItem>();

            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            // Zapytanie pierwotne: rejestracje + pacjent + firma + skierowania
            cmd.CommandText = @"
                SELECT 
                    r.R_ID, r.R_Data, r.R_GG_MM, r.R_Status, r.R_Subject, r.R_Uwagi, r.R_S_ID, r.R_P_ID,
                    p.P_ID, p.P_imie, p.P_nazwisko, p.P_brak, p.P_pesel, p.P_telefon, p.P_email,
                    p.P_Adres_ulica_numer, p.P_Ades_kod, p.P_Ades_miasto, p.P_Firma_id,
                    f.Nazwa AS Firma_Nazwa, f.Kod AS Firma_Kod, f.Miejscowosc AS Firma_Miejscowosc, 
                    f.Ulica AS Firma_Ulica, f.FKemail AS Firma_Email,
                    s.B_ID, s.B_DataSkierowania, s.B_TypBadania, s.B_Stanowisko, s.B_RegistrationDate,
                    s.B_czynnik_fizyczny, s.B_czynnik_fizyczny_opis,
                    s.B_czynnik_chemiczny, s.B_czynnik_chemiczny_opis,
                    s.B_czynnik_biologiczny, s.B_czynnik_biologiczny_opis,
                    s.B_czynnik_inny, s.B_czynnik_inny_opis,
                    s.B_Zaswiadczenie
                FROM Rejestracja r
                INNER JOIN B_Skierowania s ON r.R_S_ID = s.B_ID
                INNER JOIN P_Pacjent p ON s.B_Pacjent_ID = p.P_ID
                INNER JOIN Firma f ON p.P_Firma_id = f.id
                ORDER BY r.R_Data DESC, r.R_GG_MM DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader["R_GG_MM"] is DBNull) continue;

                DateTime start;
                var ggmm = reader["R_GG_MM"];
                if (ggmm is DateTime dtGgMm)
                {
                    start = dtGgMm;
                }
                else if (reader["R_Data"] is DateTime dtData && TimeSpan.TryParse(ggmm?.ToString(), out var timeOfDay))
                {
                    start = dtData.Date + timeOfDay;
                }
                else if (!DateTime.TryParse(ggmm?.ToString(), out start))
                {
                    continue;
                }

                var imie = reader["P_imie"] as string ?? string.Empty;
                var nazwisko = reader["P_nazwisko"] as string ?? string.Empty;
                var fullName = $"{imie} {nazwisko}".Trim();
                var subject = reader["R_Subject"]?.ToString() ?? reader["R_Uwagi"]?.ToString() ?? fullName;

                newAppointments.Add(new ScheduleAppointment
                {
                    StartTime = start,
                    EndTime = start.AddMinutes(15),
                    Subject = subject,
                    Notes = $"R_ID:{reader["R_ID"]} {reader["R_Uwagi"]}"
                });

                // ═══════════════════════════════════════════════════════════
                // Pełne wypełnienie VisitPatientItem jak w legacy GetRejestracje()
                // ═══════════════════════════════════════════════════════════
                newPacjenci.Add(new VisitPatientItem
                {
                    // REJESTRACJA
                    R_ID = reader["R_ID"] as int? ?? (reader["R_ID"] != DBNull.Value ? Convert.ToInt32(reader["R_ID"]) : null),
                    R_Data = reader["R_Data"] != DBNull.Value ? Convert.ToDateTime(reader["R_Data"]) : null,
                    R_GG_MM = reader["R_GG_MM"] != DBNull.Value ? Convert.ToDateTime(reader["R_GG_MM"]) : null,
                    R_Status = reader["R_Status"]?.ToString(),
                    R_Subject = reader["R_Subject"]?.ToString(),
                    R_Uwagi = reader["R_Uwagi"]?.ToString(),
                    R_S_ID = reader["R_S_ID"] is int sid ? sid : int.TryParse(reader["R_S_ID"]?.ToString(), out var sid2) ? sid2 : 0,
                    R_P_ID = reader["R_P_ID"] is int pid ? pid : int.TryParse(reader["R_P_ID"]?.ToString(), out var pid2) ? pid2 : 0,

                    // PACJENT
                    P_ID = reader["P_ID"] is int p_id ? p_id : int.TryParse(reader["P_ID"]?.ToString(), out var p_id2) ? p_id2 : null,
                    P_Imie = reader["P_imie"]?.ToString(),
                    P_Nazwisko = reader["P_nazwisko"]?.ToString(),
                    P_Brak = reader["P_brak"] is bool b ? b : (reader["P_brak"]?.ToString() == "True" || reader["P_brak"]?.ToString() == "1"),
                    P_Pesel = reader["P_pesel"]?.ToString(),
                    P_Telefon = reader["P_telefon"]?.ToString(),
                    P_Email = reader["P_email"]?.ToString(),
                    // P_Plec, P_DataUrodzenia, P_Zawod - kolumny z polskimi znakami, pomijamy
                    P_AdresUlica = reader["P_Adres_ulica_numer"]?.ToString(),
                    P_AdresKod = reader["P_Ades_kod"]?.ToString(),
                    P_AdresMiasto = reader["P_Ades_miasto"]?.ToString(),
                    P_FirmaId = reader["P_Firma_id"] is int fid ? fid : int.TryParse(reader["P_Firma_id"]?.ToString(), out var fid2) ? fid2 : null,

                    // FIRMA
                    Firma_Nazwa = reader["Firma_Nazwa"]?.ToString(),
                    Firma_Kod = reader["Firma_Kod"]?.ToString(),
                    Firma_Miejscowosc = reader["Firma_Miejscowosc"]?.ToString(),
                    Firma_Ulica = reader["Firma_Ulica"]?.ToString(),
                    Firma_Email = reader["Firma_Email"]?.ToString() ?? "info@adres.pl",

                    // SKIEROWANIE
                    B_ID = reader["B_ID"] is int bid ? bid : int.TryParse(reader["B_ID"]?.ToString(), out var bid2) ? bid2 : null,
                    B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : null,
                    B_TypBadania = reader["B_TypBadania"]?.ToString(),
                    B_Stanowisko = reader["B_Stanowisko"]?.ToString(),
                    B_RegistrationDate = reader["B_RegistrationDate"] != DBNull.Value ? Convert.ToDateTime(reader["B_RegistrationDate"]) : null,

                    // Czynniki szkodliwe (bez pyłowego - kolumna z polskim znakiem)
                    B_CzynnikFizyczny = reader["B_czynnik_fizyczny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_fizyczny"]) : null,
                    B_CzynnikFizycznyOpis = reader["B_czynnik_fizyczny_opis"]?.ToString(),
                    B_CzynnikChemiczny = reader["B_czynnik_chemiczny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_chemiczny"]) : null,
                    B_CzynnikChemicznyOpis = reader["B_czynnik_chemiczny_opis"]?.ToString(),
                    B_CzynnikBiologiczny = reader["B_czynnik_biologiczny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_biologiczny"]) : null,
                    B_CzynnikBiologicznyOpis = reader["B_czynnik_biologiczny_opis"]?.ToString(),
                    B_CzynnikInny = reader["B_czynnik_inny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_inny"]) : null,
                    B_CzynnikInnyOpis = reader["B_czynnik_inny_opis"]?.ToString(),

                    // Dokumenty (bez książeczka - kolumna z polskim znakiem)
                    B_Zaswiadczenie = reader["B_Zaswiadczenie"] != DBNull.Value ? Convert.ToBoolean(reader["B_Zaswiadczenie"]) : null
                });
            }

            Appointments = newAppointments;
            PacjenciNaDzien = newPacjenci;

            if (silent)
            {
                MergeFilteredPacjenci();
            }
            else
            {
                FilterPacjenci();
            }

            _logger.LogInformation("Loaded {Count} appointments from database", newAppointments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading appointments from database");
        }
    }

    /// <summary>
    /// Ciche od?wie?enie listy rejestracji (bez przeladowania calej zakladki).
    /// Zachowuje zaznaczenie oraz pozycje przewijania w ListBox "lista_rejestracja".
    /// Przeznaczone do wywolywania cyklicznie (np. z timera w code-behind), gdy widok ma fokus.
    /// </summary>
    public void RefreshSilent()
    {
        // Nie odswie?aj w trakcie edycji szczeg?l?w pacjenta ani gdy trwa juz inne ladowanie,
        // aby nie przerywac u?ytkownikowi pracy.
        if (_isLoadingDetails || ShowDetailsView)
        {
            return;
        }

        try
        {
            LoadAppointmentsFromDb(silent: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Silent refresh failed (non-critical)");
        }
    }

    /// <summary>
    /// Filtruje i sortuje PacjenciNaDzien tak samo jak FilterPacjenci, ale scala wynik
    /// z istniej?c? kolekcj? FilteredPacjenciNaDzien "w miejscu" (bez podmiany instancji ObservableCollection),
    /// dzi?ki czemu ListBox nie traci zaznaczenia ani pozycji scrolla.
    /// </summary>
    private void MergeFilteredPacjenci()
    {
        var previousSelectedId = SelectedPacjent?.R_ID;

        var filtered = PacjenciNaDzien.AsEnumerable();

        if (SelectedDate.HasValue)
        {
            filtered = filtered.Where(p => p.AppointmentDate.Date == SelectedDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var fraza = SearchText.Trim();
            filtered = filtered.Where(p =>
                (p.FullName?.Contains(fraza, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.P_Pesel != null && p.P_Pesel.Contains(fraza)));
        }

        var ordered = filtered
            .OrderBy(p => GetStatusRank(p.R_Status))
            .ThenBy(p => p.R_GG_MM.HasValue ? p.R_GG_MM.Value.TimeOfDay : TimeSpan.MaxValue)
            .ToList();

        // Usu? z widocznej kolekcji rekordy, kt?rych ju? nie ma w nowych danych
        for (int i = FilteredPacjenciNaDzien.Count - 1; i >= 0; i--)
        {
            var existing = FilteredPacjenciNaDzien[i];
            if (!ordered.Any(p => p.R_ID == existing.R_ID))
            {
                FilteredPacjenciNaDzien.RemoveAt(i);
            }
        }

        // Zaktualizuj/dodaj rekordy zachowuj?c kolejno?? z "ordered"
        for (int targetIndex = 0; targetIndex < ordered.Count; targetIndex++)
        {
            var newItem = ordered[targetIndex];
            var currentIndex = -1;
            for (int i = 0; i < FilteredPacjenciNaDzien.Count; i++)
            {
                if (FilteredPacjenciNaDzien[i].R_ID == newItem.R_ID)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex == -1)
            {
                // Nowy rekord - wstaw na wla?ciw? pozycj?
                FilteredPacjenciNaDzien.Insert(Math.Min(targetIndex, FilteredPacjenciNaDzien.Count), newItem);
            }
            else
            {
                // Zaktualizuj dane rekordu (podmiana obiektu) i przenie? na wla?ciw? pozycje, je?li trzeba
                if (currentIndex != targetIndex)
                {
                    FilteredPacjenciNaDzien.Move(currentIndex, Math.Min(targetIndex, FilteredPacjenciNaDzien.Count - 1));
                }
                FilteredPacjenciNaDzien[targetIndex] = newItem;
            }
        }

        UpdateStatistics();

        // Przywr??/zachowaj zaznaczenie po scaleniu danych
        if (previousSelectedId.HasValue)
        {
            var match = FilteredPacjenciNaDzien.FirstOrDefault(p => p.R_ID == previousSelectedId);
            if (match != null && !ReferenceEquals(SelectedPacjent, match))
            {
                SelectedPacjent = match;
            }
        }
    }

    private void LoadPacjenciNaDzien()
    {
        _logger.LogInformation("Loading patients for date: {Date}", SelectedDate);
        FilterPacjenci();
    }

    private void FilterPacjenci()
    {
        var filtered = PacjenciNaDzien.AsEnumerable();

        _logger.LogInformation("🔍 FilterPacjenci - Total patients before filter: {Count}, SelectedDate: {Date}", 
            PacjenciNaDzien.Count, SelectedDate?.ToString("yyyy-MM-dd") ?? "NULL");

        // Filtruj po wybranej dacie
        if (SelectedDate.HasValue)
        {
            filtered = filtered.Where(p => p.AppointmentDate.Date == SelectedDate.Value.Date);
            _logger.LogInformation("   📅 After date filter: {Count} patients", filtered.Count());
        }

        // Filtruj po tekście wyszukiwania (imię/nazwisko lub PESEL)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var fraza = SearchText.Trim();
            filtered = filtered.Where(p =>
                (p.FullName?.Contains(fraza, StringComparison.OrdinalIgnoreCase) ?? false)
                || (p.P_Pesel != null && p.P_Pesel.Contains(fraza)));
            _logger.LogInformation("   🔎 After search filter: {Count} patients", filtered.Count());
        }

        // Sortuj według priorytetu statusów: rejestracja, dokumentacja, w trakcie, wywiad, orzeczenie, zamknięta, pozostałe
        var ordered = filtered
            .OrderBy(p => GetStatusRank(p.R_Status))
            // następnie sortuj po godzinie wizyty jeśli dostępna
            .ThenBy(p => p.R_GG_MM.HasValue ? p.R_GG_MM.Value.TimeOfDay : TimeSpan.MaxValue);

        FilteredPacjenciNaDzien = new ObservableCollection<VisitPatientItem>(ordered);

        _logger.LogInformation("   ✅ Final filtered count: {Count}", FilteredPacjenciNaDzien.Count);

        UpdateStatistics();

        // Jeśli brak wyników na wybrany dzień i mamy tekst wyszukiwania, spróbuj znaleźć pacjenta
        // wśród wszystkich rejestracji o statusie "rejestracja" i zaproponować przeniesienie do wybranej daty
        if (FilteredPacjenciNaDzien.Count == 0 && !string.IsNullOrWhiteSpace(SearchText) && SelectedDate.HasValue)
        {
            // TryOfferAddPatientToSelectedDate(SearchText.Trim());
        }
    }

    private void TryOfferAddPatientToSelectedDate(string fraza)
    {
        if (!SelectedDate.HasValue)
            return;

        try
        {
            // Szukaj w pełnej liście rejestracji status == "rejestracja"

            var candidates = PacjenciNaDzien
                .Where(p => string.Equals(p.R_Status, "rejestracja", StringComparison.OrdinalIgnoreCase))
                .Where(p => (p.FullName?.Contains(fraza, StringComparison.OrdinalIgnoreCase) ?? false)
                            || (p.P_Pesel != null && p.P_Pesel.Contains(fraza)))
                .ToList();

            if (candidates.Count == 0)
                return;

            // Zawsze pokazuj dialog wyboru (nawet gdy tylko jedno dopasowanie),
            // żeby użytkownik mógł potwierdzić wybór.
            var dialog = new ASMED.EDM.UI.Views.wizytyview.SelectPatientDialog(candidates);
            if (System.Windows.Application.Current?.MainWindow != null)
                dialog.Owner = System.Windows.Application.Current.MainWindow;

            var dialogResult = dialog.ShowDialog();
            if (dialogResult != true)
                return;

            var candidate = dialog.SelectedItem as VisitPatientItem;
            if (candidate == null)
                return;

            if (_dbFactory == null)
            {
                _logger.LogWarning("Brak DbConnectionFactory - nie można zaktualizować daty wizyty");
                System.Windows.MessageBox.Show("Brak połączenia z bazą danych.", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlConnector.MySqlCommand)conn.CreateCommand();

            // Nowa data wizyty (tylko data)
            var newDate = SelectedDate.Value.Date;

            // Jeśli istnieje R_GG_MM zachowaj godzinę, przenieś tylko datę
            object newRggmm = DBNull.Value;
            if (candidate.R_GG_MM.HasValue)
            {
                var t = candidate.R_GG_MM.Value.TimeOfDay;
                var dt = new DateTime(newDate.Year, newDate.Month, newDate.Day, t.Hours, t.Minutes, t.Seconds);
                newRggmm = dt;
            }

            cmd.CommandText = "UPDATE Rejestracja SET R_Data = @rdata, R_GG_MM = @rggmm WHERE R_ID = @rid";
            cmd.Parameters.AddWithValue("@rdata", newDate);
            cmd.Parameters.AddWithValue("@rggmm", newRggmm);
            cmd.Parameters.AddWithValue("@rid", candidate.R_ID ?? 0);

            var rows = cmd.ExecuteNonQuery();
            if (rows > 0)
            {
                System.Windows.MessageBox.Show("Pacjent został dodany do wybranego dnia.", "Gotowe", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                // Przeładuj dane
                LoadAppointmentsFromDb();
                // Upewnij się, że filtr stosuje się do wybranej daty
                LoadPacjenciNaDzien();
            }
            else
            {
                System.Windows.MessageBox.Show("Nie udało się zaktualizować wizyty.", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas próby przeniesienia pacjenta do wybranego dnia");
            System.Windows.MessageBox.Show($"Błąd: {ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void UpdateStatistics()
    {
        TotalCount = FilteredPacjenciNaDzien.Count;
        CompletedCount = FilteredPacjenciNaDzien.Count(p => p.Status == "Odbyta");
        OtherStatusCount = FilteredPacjenciNaDzien.Count(p => !(p.R_Status ?? string.Empty).Equals("rejestracja", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}

/// <summary>
/// Item reprezentujący pacjenta na liście wizyt
/// Legacy: RejestracjaRecord z AccessDbContext
/// </summary>
public class VisitPatientItem
{
    // ═══════════════════════════════════════════════════════════
    // ✅ REJESTRACJA
    // ═══════════════════════════════════════════════════════════
    public int? R_ID { get; set; }
    public DateTime? R_Data { get; set; }
    public DateTime? R_GG_MM { get; set; }
    public string? R_Status { get; set; }
    public string? R_Subject { get; set; }
    public string? R_Uwagi { get; set; }
    public int R_S_ID { get; set; }
    public int R_P_ID { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ✅ PACJENT (z JOIN P_Pacjent)
    // ═══════════════════════════════════════════════════════════
    public int? P_ID { get; set; }
    public string? P_Imie { get; set; }
    public string? P_Nazwisko { get; set; }
    public bool? P_Brak { get; set; }
    public string? P_Pesel { get; set; }
    public string? P_Telefon { get; set; }
    public string? P_Email { get; set; }
    public string? P_Uwagi { get; set; }
    public string? P_Plec { get; set; }
    public DateTime? P_Data_Urodzenia { get; set; }
    public string? P_Zawod { get; set; }
    public string? P_AdresUlica { get; set; }
    public string? P_AdresKod { get; set; }
    public string? P_AdresMiasto { get; set; }
    public int? P_FirmaId { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ✅ FIRMA (z JOIN Firma)
    // ═══════════════════════════════════════════════════════════
    public string? Firma_Nazwa { get; set; }
    public string? Firma_Kod { get; set; }
    public string? Firma_Miejscowosc { get; set; }
    public string? Firma_Ulica { get; set; }
    public string? Firma_Email { get; set; }

    public string Firma_Nazwa_Dane => string.IsNullOrWhiteSpace(Firma_Nazwa) ? "-" : Firma_Nazwa.Trim();

    public string Firma_Adres_Dane => FormatFirmaAdres();

    private string FormatFirmaAdres()
    {
        var parts = new[]
        {
            string.IsNullOrWhiteSpace(Firma_Ulica) ? null : Firma_Ulica.Trim(),
            string.IsNullOrWhiteSpace(Firma_Kod) ? null : Firma_Kod.Trim(),
            string.IsNullOrWhiteSpace(Firma_Miejscowosc) ? null : Firma_Miejscowosc.Trim()
        }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        return parts.Length == 0 ? "-" : string.Join(", ", parts);
    }

    // ═══════════════════════════════════════════════════════════
    // ✅ SKIEROWANIE (z JOIN B_Skierowania)
    // ═══════════════════════════════════════════════════════════
    public int? B_ID { get; set; }
    public DateTime? B_DataSkierowania { get; set; }
    public string? B_TypBadania { get; set; }
    public string? B_Stanowisko { get; set; }
    public DateTime? B_RegistrationDate { get; set; }

    // Czynniki szkodliwe
    public bool? B_CzynnikFizyczny { get; set; }
    public string? B_CzynnikFizycznyOpis { get; set; }
    public bool? B_CzynnikPylowy { get; set; }
    public string? B_CzynnikPylowyOpis { get; set; }
    public bool? B_CzynnikChemiczny { get; set; }
    public string? B_CzynnikChemicznyOpis { get; set; }
    public bool? B_CzynnikBiologiczny { get; set; }
    public string? B_CzynnikBiologicznyOpis { get; set; }
    public bool? B_CzynnikInny { get; set; }
    public string? B_CzynnikInnyOpis { get; set; }

    // Dokumenty
    public bool? B_Zaswiadczenie { get; set; }
    public bool? B_Ksiazeczka { get; set; }

    // ═══════════════════════════════════════════════════════════
    // ✅ Pola pomocnicze dla UI
    // ═══════════════════════════════════════════════════════════
    public string FullName => $"{P_Imie} {P_Nazwisko}".Trim();
    public string AppointmentTime => R_GG_MM?.ToString("HH:mm") ?? string.Empty;
    public DateTime AppointmentDate => R_Data?.Date ?? DateTime.MinValue;
    public string Status => R_Status ?? string.Empty;
    public string StatusWizytyTekst => R_Status ?? string.Empty;

    /// <summary>
    /// Numer skierowania (B_ID jako string) - używane w UI
    /// Legacy: SkierowanieNumer => R_S_ID (w naszym modelu to B_ID)
    /// </summary>
    public string SkierowanieNumer => B_ID?.ToString() ?? "-";

    /// <summary>
    /// Godzina wizyty sformatowana (HH:mm)
    /// </summary>
    public string GodzinaFormatted => R_GG_MM?.ToString("HH:mm") ?? "-";

    // Wiek obliczany z daty urodzenia (jeśli dostępna)
    public int Wiek
    {
        get
        {
            if (P_Data_Urodzenia == null) return 0;
            var today = DateTime.Today;
            var age = today.Year - P_Data_Urodzenia.Value.Year;
            if (P_Data_Urodzenia.Value.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    public string? B_urlop { get; internal set; }

}
