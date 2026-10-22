using ASMED.EDM.Data.Services;
using ASMED.EDM.Printing.Models;
using ASMED.EDM.Printing.Services;
using ASMED.EDM.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using MySqlConnector;
using Syncfusion.UI.Xaml.Scheduler;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ASMED.EDM.UI.ViewModels.Skierowania;

/// <summary>
/// ViewModel dla formularza nowej karty badań (SkierNewPacjentaView).
/// Ładuje dane pacjenta z obiektu PacjentSkier przekazanego z listy.
/// </summary>
public class SkierNewPacjentaViewModel : ObservableObject
{
    private readonly DbConnectionFactory? _dbFactory;

    // ─── Zdarzenie powrotu do listy ──────────────────────────────────────────
    public event Action? RequestPowrot;

    /// <summary>
    /// Ustawiane przez widok — pozwala na otwarcie edytora pacjenta w tej samej zakładce.
    /// </summary>
    public Action? EditPatientRequested { get; set; }

    /// <summary>ID skierowania w bazie (B_Skierowania.B_ID). 0 = jeszcze nie zapisane.</summary>
    private int _patientSkierowanieId;
    public int PatientSkierowanieId
    {
        get => _patientSkierowanieId;
        set
        {
            if (SetProperty(ref _patientSkierowanieId, value))
            {
                OnPropertyChanged(nameof(WydrukiVisibility));
                OnPropertyChanged(nameof(EditButtonVisibility));
            }
        }
    }

    // ─── Komendy nagłówka ────────────────────────────────────────────────────
    public ICommand RejestracjaCommand    { get; }
    public ICommand KartyBadanCommand     { get; }
    public ICommand ZakonczBadanieCommand { get; }
    public ICommand ListaDoFakturCommand  { get; }
    public ICommand CloseCommand          { get; }
    public ICommand SaveCommand           { get; }
    public ICommand EditPatientCommand    { get; }
    public ICommand NewCommand            { get; }
    public ICommand AddAppointmentCommand { get; }
    public ICommand AddTodayAppointmentCommand { get; }
    public ICommand PrintKartaCommand          { get; }
    public ICommand PrintOrzeczenieCommand     { get; }
    public ICommand PrintSanitarneComand       { get; }
    public ICommand PrintAnkietaCommand        { get; }
    public ICommand PrintZaswiadczenieCommand  { get; }
    public ICommand PrintUrlpoCommand          { get; }
    public ICommand PrintAllCommand            { get; }

    public SkierNewPacjentaViewModel() : this(null)
    {
    }

    public SkierNewPacjentaViewModel(DbConnectionFactory? dbFactory)
    {
        _dbFactory = dbFactory;

        RejestracjaCommand       = new RelayCommand(_ => { /* TODO */ });
        KartyBadanCommand        = new RelayCommand(_ => { /* TODO */ });
        ZakonczBadanieCommand    = new RelayCommand(_ => { /* TODO */ });
        ListaDoFakturCommand     = new RelayCommand(_ => { /* TODO */ });
        CloseCommand             = new RelayCommand(_ => RequestPowrot?.Invoke());
        SaveCommand              = new RelayCommand(_ => ZapiszSkierowanie());
        EditPatientCommand       = new RelayCommand(_ => EditPatientRequested?.Invoke());
        NewCommand               = new RelayCommand(_ => { /* TODO */ });
        AddAppointmentCommand    = new RelayCommand(param => AddAppointmentFromSelectedSlot(param as DateTime?));
        AddTodayAppointmentCommand = new RelayCommand(_ => AddAppointmentToday());
        PrintKartaCommand        = new RelayCommand(_ => PrintForm(PatientFormTemplates.Karta));
        PrintOrzeczenieCommand   = new RelayCommand(_ => PrintForm(PatientFormTemplates.Orzeczenie));
        PrintSanitarneComand     = new RelayCommand(_ => PrintForm(PatientFormTemplates.Sanitarne));
        PrintAnkietaCommand      = new RelayCommand(_ => PrintForm(PatientFormTemplates.Ankieta));
        PrintZaswiadczenieCommand = new RelayCommand(_ => PrintForm(PatientFormTemplates.Komplet));
        PrintAllCommand          = new RelayCommand(_ => PrintForm(PatientFormTemplates.Komplet));
        PrintUrlpoCommand        = new RelayCommand(_ => SkierowanieWydruki.PrintUrlop());
    }

    // ─── Dane pacjenta (tylko do odczytu — z listy) ──────────────────────────
    private int _patientId;
    public int PatientId
    {
        get => _patientId;
        set => SetProperty(ref _patientId, value);
    }

    private string _patientFirstName = string.Empty;
    public string PatientFirstName
    {
        get => _patientFirstName;
        set => SetProperty(ref _patientFirstName, value);
    }

    private string _patientLastName = string.Empty;
    public string PatientLastName
    {
        get => _patientLastName;
        set => SetProperty(ref _patientLastName, value);
    }

    private string _patientPesel = string.Empty;
    public string PatientPesel
    {
        get => _patientPesel;
        set => SetProperty(ref _patientPesel, value);
    }

    private string _patientGender = string.Empty;
    public string PatientGender
    {
        get => _patientGender;
        set => SetProperty(ref _patientGender, value);
    }

    private DateTime? _patientBirthDate;
    public DateTime? PatientBirthDate
    {
        get => _patientBirthDate;
        set => SetProperty(ref _patientBirthDate, value);
    }

    private string _patientJobTitle = string.Empty;
    public string PatientJobTitle
    {
        get => _patientJobTitle;
        set => SetProperty(ref _patientJobTitle, value);
    }

    private string _patientPostalCode = string.Empty;
    public string PatientPostalCode
    {
        get => _patientPostalCode;
        set => SetProperty(ref _patientPostalCode, value);
    }

    private string _patientCity = string.Empty;
    public string PatientCity
    {
        get => _patientCity;
        set => SetProperty(ref _patientCity, value);
    }

    private string _patientStreet = string.Empty;
    public string PatientStreet
    {
        get => _patientStreet;
        set => SetProperty(ref _patientStreet, value);
    }

    private string _uwagi = string.Empty;
    public string Uwagi
    {
        get => _uwagi;
        set => SetProperty(ref _uwagi, value);
    }

    // ─── Dane firmy ──────────────────────────────────────────────────────────
    private int _companyId;
    public int CompanyId
    {
        get => _companyId;
        set => SetProperty(ref _companyId, value);
    }

    private string _companyName = string.Empty;
    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }

    private string _companyPostalCode = string.Empty;
    public string CompanyPostalCode
    {
        get => _companyPostalCode;
        set => SetProperty(ref _companyPostalCode, value);
    }

    private string _companyCity = string.Empty;
    public string CompanyCity
    {
        get => _companyCity;
        set => SetProperty(ref _companyCity, value);
    }

    private string _companyStreet = string.Empty;
    public string CompanyStreet
    {
        get => _companyStreet;
        set => SetProperty(ref _companyStreet, value);
    }

    // ─── Dane skierowania ────────────────────────────────────────────────────
    private DateTime? _referralDate = DateTime.Today;
    public DateTime? ReferralDate
    {
        get => _referralDate;
        set => SetProperty(ref _referralDate, value);
    }

    private string _rejestrcjaData = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
    public string RejestrcjaData
    {
        get => _rejestrcjaData;
        set => SetProperty(ref _rejestrcjaData, value);
    }

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

    private void UpdateTestType()
    {
        TestType = IsGroupW ? "W" : IsGroupO ? "O" : IsGroupS ? "S" : IsGroupK ? "K" : IsGroupU ? "U" : string.Empty;
    }

    private string _testType = "W";
    public string TestType
    {
        get => _testType;
        set => SetProperty(ref _testType, value);
    }

    // Zagrożenia
    private bool _isPhysical;
    public bool IsPhysical { get => _isPhysical; set => SetProperty(ref _isPhysical, value); }
    private string _physicalDescription = string.Empty;
    public string PhysicalDescription { get => _physicalDescription; set => SetProperty(ref _physicalDescription, value); }

    private bool _isDust;
    public bool IsDust { get => _isDust; set => SetProperty(ref _isDust, value); }
    private string _dustDescription = string.Empty;
    public string DustDescription { get => _dustDescription; set => SetProperty(ref _dustDescription, value); }

    private bool _isChemical;
    public bool IsChemical { get => _isChemical; set => SetProperty(ref _isChemical, value); }
    private string _chemicalDescription = string.Empty;
    public string ChemicalDescription { get => _chemicalDescription; set => SetProperty(ref _chemicalDescription, value); }

    private bool _isBiological;
    public bool IsBiological { get => _isBiological; set => SetProperty(ref _isBiological, value); }
    private string _biologicalDescription = string.Empty;
    public string BiologicalDescription { get => _biologicalDescription; set => SetProperty(ref _biologicalDescription, value); }

    private bool _isOther;
    public bool IsOther { get => _isOther; set => SetProperty(ref _isOther, value); }
    private string _otherDescription = string.Empty;
    public string OtherDescription { get => _otherDescription; set => SetProperty(ref _otherDescription, value); }

    private bool _isbookletSanepid;
    public bool IsbookletSanepid { get => _isbookletSanepid; set => SetProperty(ref _isbookletSanepid, value); }

    private bool _isCertificate;
    public bool IsCertificate { get => _isCertificate; set => SetProperty(ref _isCertificate, value); }

    private bool _isLeave;
    public bool IsLeave { get => _isLeave; set => SetProperty(ref _isLeave, value); }

    // ─── Widoczność ──────────────────────────────────────────────────────────
    public System.Windows.Visibility WydrukiVisibility =>
        PatientSkierowanieId > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    public System.Windows.Visibility EditButtonVisibility =>
        PatientSkierowanieId <= 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

    // ─── Metoda ładowania danych z listy pacjentów ───────────────────────────
    public void LoadPacjent(PacjentSkier p)
    {
        PatientId        = p.P_ID;
        PatientFirstName = p.FirstName  ?? string.Empty;
        PatientLastName  = p.LastName   ?? string.Empty;
        PatientPesel     = p.PESEL      ?? string.Empty;
        CompanyName      = p.Company    ?? string.Empty;

        LoadPelneDaneZBazy(p.P_ID);
    }

    /// <summary>
    /// Dociaga pelne dane pacjenta (plec, data urodzenia, stanowisko, adres) oraz
    /// przypisanej firmy (id, kod, miejscowosc, ulica) bezposrednio z bazy danych.
    /// </summary>
    private void LoadPelneDaneZBazy(int patientId)
    {
        if (patientId <= 0 || _dbFactory == null) return;

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    p.P_plec, p.P_data_urodzenia, p.P_zawod,
                    p.P_Adres_ulica_numer, p.P_Ades_kod, p.P_Ades_miasto,
                    p.P_Firma_id,
                    f.id AS Firma_Id, f.Nazwa AS Firma_Nazwa,
                    f.Kod AS Firma_Kod, f.Miejscowosc AS Firma_Miejscowosc, f.Ulica AS Firma_Ulica
                FROM P_Pacjent p
                LEFT JOIN Firma f ON f.id = p.P_Firma_id
                WHERE p.P_ID = @id";
            cmd.Parameters.AddWithValue("@id", patientId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return;

            PatientGender     = reader["P_plec"]?.ToString() ?? string.Empty;
            PatientBirthDate  = reader["P_data_urodzenia"] is DBNull
                                 ? null
                                 : Convert.ToDateTime(reader["P_data_urodzenia"]);
            PatientJobTitle   = reader["P_zawod"]?.ToString()               ?? string.Empty;
            PatientStreet     = reader["P_Adres_ulica_numer"]?.ToString()   ?? string.Empty;
            PatientPostalCode = reader["P_Ades_kod"]?.ToString()           ?? string.Empty;
            PatientCity       = reader["P_Ades_miasto"]?.ToString()        ?? string.Empty;

            if (reader["P_Firma_id"] is not DBNull)
            {
                CompanyId          = Convert.ToInt32(reader["P_Firma_id"]);
                CompanyName        = reader["Firma_Nazwa"]?.ToString()       ?? CompanyName;
                CompanyPostalCode  = reader["Firma_Kod"]?.ToString()         ?? string.Empty;
                CompanyCity        = reader["Firma_Miejscowosc"]?.ToString() ?? string.Empty;
                CompanyStreet      = reader["Firma_Ulica"]?.ToString()       ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Blad ladowania pelnych danych pacjenta/firmy:\n" + ex.Message,
                "Blad", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ZapiszSkierowanie()
    {
        if (PatientId <= 0)
        {
            MessageBox.Show("Brak wybranego pacjenta - nie mozna zapisac karty badan.",
                "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_dbFactory == null)
        {
            MessageBox.Show("Brak konfiguracji polaczenia z baza danych.",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();

            if (PatientSkierowanieId <= 0)
            {
                cmd.CommandText = @"
                    INSERT INTO B_Skierowania
                        (B_Pacjent_ID, B_Firma_ID, B_DataSkierowania, B_TypBadania,
                         B_Stanowisko, B_RegistrationDate,
                         B_czynnik_fizyczny, B_czynnik_fizyczny_opis,
                         B_czynnik_pylowy, B_czynnik_pylowy_opis,
                         B_czynnik_chemiczny, B_czynnik_chemiczny_opis,
                         B_czynnik_biologiczny, B_czynnik_biologiczny_opis,
                         B_czynnik_inny, B_czynnik_inny_opis,
                         B_Zaswiadczenie, B_ksiazeczka, B_Ankieta, B_Urlop, B_Activ)
                    VALUES
                        (@pacjentId, @firmaId, @dataSkier, @typBadania,
                         @stanowisko, @rejData,
                         @czFiz, @czFizOpis,
                         @czPyl, @czPylOpis,
                         @czChem, @czChemOpis,
                         @czBio, @czBioOpis,
                         @czInny, @czInnyOpis,
                         @zaswiadczenie, @ksiazeczka, @ankieta, @urlop, @activ)";
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE B_Skierowania SET
                        B_Pacjent_ID = @pacjentId,
                        B_Firma_ID = @firmaId,
                        B_DataSkierowania = @dataSkier,
                        B_TypBadania = @typBadania,
                        B_Stanowisko = @stanowisko,
                        B_RegistrationDate = @rejData,
                        B_czynnik_fizyczny = @czFiz,
                        B_czynnik_fizyczny_opis = @czFizOpis,
                        B_czynnik_pylowy = @czPyl,
                        B_czynnik_pylowy_opis = @czPylOpis,
                        B_czynnik_chemiczny = @czChem,
                        B_czynnik_chemiczny_opis = @czChemOpis,
                        B_czynnik_biologiczny = @czBio,
                        B_czynnik_biologiczny_opis = @czBioOpis,
                        B_czynnik_inny = @czInny,
                        B_czynnik_inny_opis = @czInnyOpis,
                        B_Zaswiadczenie = @zaswiadczenie,
                        B_ksiazeczka = @ksiazeczka,
                        B_Ankieta = @ankieta,
                        B_Urlop = @urlop
                    WHERE B_ID = @id";
                cmd.Parameters.AddWithValue("@id", PatientSkierowanieId);
            }

            cmd.Parameters.AddWithValue("@pacjentId", PatientId);
            cmd.Parameters.AddWithValue("@firmaId", CompanyId == 0 ? (object)DBNull.Value : CompanyId);
            cmd.Parameters.AddWithValue("@dataSkier", ReferralDate.HasValue ? (object)ReferralDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@typBadania", string.IsNullOrWhiteSpace(TestType) ? (object)DBNull.Value : TestType);
            cmd.Parameters.AddWithValue("@stanowisko", string.IsNullOrWhiteSpace(PatientJobTitle) ? (object)DBNull.Value : PatientJobTitle.Trim());
            cmd.Parameters.AddWithValue("@rejData", DateTime.Now);
            cmd.Parameters.AddWithValue("@czFiz", IsPhysical);
            cmd.Parameters.AddWithValue("@czFizOpis", string.IsNullOrWhiteSpace(PhysicalDescription) ? (object)DBNull.Value : PhysicalDescription.Trim());
            cmd.Parameters.AddWithValue("@czPyl", IsDust);
            cmd.Parameters.AddWithValue("@czPylOpis", string.IsNullOrWhiteSpace(DustDescription) ? (object)DBNull.Value : DustDescription.Trim());
            cmd.Parameters.AddWithValue("@czChem", IsChemical);
            cmd.Parameters.AddWithValue("@czChemOpis", string.IsNullOrWhiteSpace(ChemicalDescription) ? (object)DBNull.Value : ChemicalDescription.Trim());
            cmd.Parameters.AddWithValue("@czBio", IsBiological);
            cmd.Parameters.AddWithValue("@czBioOpis", string.IsNullOrWhiteSpace(BiologicalDescription) ? (object)DBNull.Value : BiologicalDescription.Trim());
            cmd.Parameters.AddWithValue("@czInny", IsOther);
            cmd.Parameters.AddWithValue("@czInnyOpis", string.IsNullOrWhiteSpace(OtherDescription) ? (object)DBNull.Value : OtherDescription.Trim());
            cmd.Parameters.AddWithValue("@zaswiadczenie", IsCertificate);
            cmd.Parameters.AddWithValue("@ksiazeczka", IsbookletSanepid);
            cmd.Parameters.AddWithValue("@ankieta", false);
            cmd.Parameters.AddWithValue("@urlop", IsLeave);
            if (PatientSkierowanieId <= 0)
            {
                cmd.Parameters.AddWithValue("@activ", 1);
            }

            cmd.ExecuteNonQuery();

            if (PatientSkierowanieId <= 0)
            {
                using var cmdId = (MySqlCommand)conn.CreateCommand();
                cmdId.CommandText = "SELECT LAST_INSERT_ID()";
                PatientSkierowanieId = Convert.ToInt32(cmdId.ExecuteScalar());

                MessageBox.Show($"Karta badan zostala zapisana (ID: {PatientSkierowanieId}).",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Karta badan zostala zaktualizowana.",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Blad podczas zapisu karty badan: {ex.Message}",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────
    public ObservableCollection<ScheduleAppointment> Events { get; set; } = new();

    private DateTime _displayDate = DateTime.Today;
    public DateTime DisplayDate
    {
        get => _displayDate;
        set => SetProperty(ref _displayDate, value);
    }

    public void RefreshAppointmentsFromDb()
    {
        Events.Clear();
        if (_dbFactory == null) return;

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            // Laduje caly kalendarz (wszystkie rejestracje), tak samo jak zakladka "Rejestracja"
            // (legacy: AccessDbContext.GetRejestracje() bez filtra R_S_ID).
            cmd.CommandText = @"
                SELECT R_ID, R_Data, R_GG_MM, R_Status, R_Subject, R_Uwagi, R_S_ID, R_P_ID
                FROM Rejestracja
                ORDER BY R_Data DESC, R_GG_MM DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader["R_GG_MM"] is DBNull) continue;

                DateTime start;
                var ggmm = reader["R_GG_MM"];
                if (ggmm is DateTime dt)
                {
                    start = dt;
                }
                else if (reader["R_Data"] is DateTime dtData && TimeSpan.TryParse(ggmm?.ToString(), out var timeOfDay))
                {
                    start = dtData.Date + timeOfDay;
                }
                else if (!DateTime.TryParse(ggmm?.ToString(), out start))
                {
                    continue;
                }

                Events.Add(new ScheduleAppointment
                {
                    StartTime = start,
                    EndTime = start.AddMinutes(5),
                    Subject = reader["R_Subject"]?.ToString() ?? reader["R_Uwagi"]?.ToString(),
                    Notes = $"R_ID:{reader["R_ID"]} {reader["R_Uwagi"]}"
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Blad ladowania rejestracji z bazy: {ex.Message}",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AddAppointmentFromSelectedSlot(DateTime? selectedDate)
    {
        if (selectedDate == null) return;

        if (PatientSkierowanieId <= 0)
        {
            MessageBox.Show("Karta badan musi byc najpierw zapisana, aby dodac rejestracje.",
                "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_dbFactory == null)
        {
            MessageBox.Show("Brak konfiguracji polaczenia z baza danych.",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var subject = $"{PatientLastName} {PatientFirstName} ({CompanyName})".Trim();

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();

            int? existingId = null;
            using (var checkCmd = (MySqlCommand)conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT R_ID FROM Rejestracja WHERE R_S_ID = @sid LIMIT 1";
                checkCmd.Parameters.AddWithValue("@sid", PatientSkierowanieId);
                var result = checkCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    existingId = Convert.ToInt32(result);
            }

            using var cmd = (MySqlCommand)conn.CreateCommand();
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
                cmd.Parameters.AddWithValue("@sid", PatientSkierowanieId);
            }

            cmd.Parameters.AddWithValue("@data", selectedDate.Value.Date);
            cmd.Parameters.AddWithValue("@status", "Rejestracja");
            cmd.Parameters.AddWithValue("@ggmm", selectedDate.Value);
            cmd.Parameters.AddWithValue("@subject", subject);
            cmd.Parameters.AddWithValue("@uwagi", subject);
            cmd.Parameters.AddWithValue("@pid", PatientId);

            cmd.ExecuteNonQuery();

            RefreshAppointmentsFromDb();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Blad zapisu rejestracji do bazy: {ex.Message}",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Rejestruje bieżącą kartę badań w pierwszym wolnym terminie dzisiejszego dnia
    /// (w godzinach pracy kalendarza, niezależnie od aktualnej godziny).
    /// </summary>
    private void AddAppointmentToday()
    {
        if (PatientSkierowanieId <= 0)
        {
            MessageBox.Show("Karta badan musi byc najpierw zapisana, aby dodac rejestracje.",
                "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_dbFactory == null)
        {
            MessageBox.Show("Brak konfiguracji polaczenia z baza danych.",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        RefreshAppointmentsFromDb();

        int? ownRId;
        try
        {
            ownRId = RejestracjaTerminy.GetRejestracjaId(_dbFactory, PatientSkierowanieId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Blad odczytu rejestracji z bazy: {ex.Message}",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var slot = RejestracjaTerminy.FindFirstFreeSlot(Events, DateTime.Today, ownRId);
        if (slot == null)
        {
            MessageBox.Show($"Brak wolnych terminow na dzis ({RejestracjaTerminy.ZakresGodzinText}).",
                "Rejestracja", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        AddAppointmentFromSelectedSlot(slot);
        DisplayDate = slot.Value.Date;
    }

    // ─── Wydruki ───────────────────────────────────────────────────────────────────────
    private void PrintForm(string templateFileName) =>
        SkierowanieWydruki.Print(BuildPrintData(), templateFileName);

    private PatientPrintData BuildPrintData() => new()
    {
        B_ID                     = PatientSkierowanieId,
        P_ID                     = PatientId,
        P_Imie                   = PatientFirstName,
        P_Nazwisko               = PatientLastName,
        P_Pesel                  = PatientPesel,
        P_Plec                   = PatientGender,
        P_DataUrodzenia          = PatientBirthDate,
        P_Zawod                  = PatientJobTitle,
        P_AdresUlica             = PatientStreet,
        P_AdresKod               = PatientPostalCode,
        P_AdresMiasto            = PatientCity,
        Firma_Nazwa              = CompanyName,
        Firma_Ulica              = CompanyStreet,
        Firma_Kod                = CompanyPostalCode,
        Firma_Miejscowosc        = CompanyCity,
        B_DataSkierowania        = ReferralDate,
        B_TypBadania             = TestType,
        B_Stanowisko             = PatientJobTitle,
        B_RegistrationDate       = SkierowanieWydruki.ParseRegistrationDate(RejestrcjaData),
        B_CzynnikFizyczny        = IsPhysical,
        B_CzynnikFizycznyOpis    = SkierowanieWydruki.NullIfEmpty(PhysicalDescription),
        B_CzynnikPylowy          = IsDust,
        B_CzynnikPylowyOpis      = SkierowanieWydruki.NullIfEmpty(DustDescription),
        B_CzynnikChemiczny       = IsChemical,
        B_CzynnikChemicznyOpis   = SkierowanieWydruki.NullIfEmpty(ChemicalDescription),
        B_CzynnikBiologiczny     = IsBiological,
        B_CzynnikBiologicznyOpis = SkierowanieWydruki.NullIfEmpty(BiologicalDescription),
        B_CzynnikInny            = IsOther,
        B_CzynnikInnyOpis        = SkierowanieWydruki.NullIfEmpty(OtherDescription),
        SkierowanieNumer         = PatientSkierowanieId > 0 ? PatientSkierowanieId.ToString() : "-"
    };

    public void HandleAppointmentEditorClosed(ScheduleAppointment appointment, bool isDelete, bool isAdd)
    {
        if (_dbFactory == null) return;

        int? existingRId = null;
        var notes = appointment.Notes as string;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            var idx = notes.IndexOf("R_ID:", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var tail = notes.Substring(idx + 5);
                var digits = new string(tail.TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(digits, out var parsedRid)) existingRId = parsedRid;
            }
        }

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();

            if (isDelete)
            {
                if (existingRId.HasValue)
                {
                    using var delCmd = (MySqlCommand)conn.CreateCommand();
                    delCmd.CommandText = "DELETE FROM Rejestracja WHERE R_ID = @id";
                    delCmd.Parameters.AddWithValue("@id", existingRId.Value);
                    delCmd.ExecuteNonQuery();
                }
            }
            else if (isAdd || !existingRId.HasValue)
            {
                using var insCmd = (MySqlCommand)conn.CreateCommand();
                insCmd.CommandText = @"
                    INSERT INTO Rejestracja (R_Data, R_Status, R_S_ID, R_GG_MM, R_Subject, R_Uwagi, R_P_ID)
                    VALUES (@data, @status, @sid, @ggmm, @subject, @uwagi, @pid)";
                insCmd.Parameters.AddWithValue("@data", appointment.StartTime.Date);
                insCmd.Parameters.AddWithValue("@status", "Wizyta");
                insCmd.Parameters.AddWithValue("@sid", PatientSkierowanieId);
                insCmd.Parameters.AddWithValue("@ggmm", appointment.StartTime);
                insCmd.Parameters.AddWithValue("@subject", appointment.Subject ?? string.Empty);
                insCmd.Parameters.AddWithValue("@uwagi", appointment.Notes ?? string.Empty);
                insCmd.Parameters.AddWithValue("@pid", PatientId);
                insCmd.ExecuteNonQuery();
            }
            else
            {
                using var updCmd = (MySqlCommand)conn.CreateCommand();
                updCmd.CommandText = @"
                    UPDATE Rejestracja SET
                        R_Data = @data, R_GG_MM = @ggmm, R_Subject = @subject, R_Uwagi = @uwagi
                    WHERE R_ID = @id";
                updCmd.Parameters.AddWithValue("@data", appointment.StartTime.Date);
                updCmd.Parameters.AddWithValue("@ggmm", appointment.StartTime);
                updCmd.Parameters.AddWithValue("@subject", appointment.Subject ?? string.Empty);
                updCmd.Parameters.AddWithValue("@uwagi", appointment.Notes ?? string.Empty);
                updCmd.Parameters.AddWithValue("@id", existingRId.Value);
                updCmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Blad operacji rejestracji: {ex.Message}",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        RefreshAppointmentsFromDb();
    }

}
