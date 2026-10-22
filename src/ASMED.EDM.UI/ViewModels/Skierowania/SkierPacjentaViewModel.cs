using ASMED.EDM.Data.Services;
using ASMED.EDM.Printing.Models;
using ASMED.EDM.Printing.Services;
using ASMED.EDM.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using MySqlConnector;
using Syncfusion.UI.Xaml.Scheduler;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ASMED.EDM.UI.ViewModels.Skierowania;

/// <summary>
/// ViewModel dla widoku karty badan istniejacego pacjenta (SkierPacjentaView).
/// W odroznieniu od SkierNewPacjentaViewModel, laduje juz istniejaca kartę
/// (B_Skierowania) z bazy MySQL na podstawie SkierowanieListItem przekazanego z listy.
/// </summary>
public class SkierPacjentaViewModel : ObservableObject
{
    private readonly DbConnectionFactory? _dbFactory;

    // ─── Zdarzenie powrotu do listy ─────────────────────────────────────────
    public event Action? RequestPowrot;

    /// <summary>
    /// Ustawiane przez widok — pozwala na otwarcie edytora pacjenta w tej samej zakladce.
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

    // ─── Komendy naglowka ───────────────────────────────────────────────────
    public ICommand RejestracjaCommand    { get; }
    public ICommand NowaKartaCommand      { get; }
    public ICommand ZakonczBadanieCommand { get; }
    public ICommand ListaDoFakturCommand  { get; }
    public ICommand CloseCommand          { get; }
    public ICommand SaveCommand           { get; }
    public ICommand EditPatientCommand    { get; }
    public ICommand AddAppointmentCommand { get; }
    public ICommand AddTodayAppointmentCommand { get; }
    public ICommand PrintKartaCommand          { get; }
    public ICommand PrintOrzeczenieCommand     { get; }
    public ICommand PrintSanitarneComand       { get; }
    public ICommand PrintAnkietaCommand        { get; }
    public ICommand PrintZaswiadczenieCommand  { get; }
    public ICommand PrintUrlpoCommand          { get; }
    public ICommand PrintAllCommand            { get; }
    public ICommand DeleteSkierowanieCommand   { get; }

    public SkierPacjentaViewModel() : this(null)
    {
    }

    public SkierPacjentaViewModel(DbConnectionFactory? dbFactory)
    {
        _dbFactory = dbFactory;

        RejestracjaCommand       = new RelayCommand(_ => { /* TODO: nawigacja obslugiwana w code-behind */ });
        NowaKartaCommand         = new RelayCommand(_ => { /* TODO: nawigacja obslugiwana w code-behind */ });
        ZakonczBadanieCommand    = new RelayCommand(_ => { /* TODO: nawigacja obslugiwana w code-behind */ });
        ListaDoFakturCommand     = new RelayCommand(_ => { /* TODO: nawigacja obslugiwana w code-behind */ });
        CloseCommand             = new RelayCommand(_ => RequestPowrot?.Invoke());
        SaveCommand              = new RelayCommand(_ => ZapiszSkierowanie());
        EditPatientCommand       = new RelayCommand(_ => EditPatientRequested?.Invoke());
        AddAppointmentCommand    = new RelayCommand(param => AddAppointmentFromSelectedSlot(param as DateTime?));
        AddTodayAppointmentCommand = new RelayCommand(_ => AddAppointmentToday());
        PrintKartaCommand        = new RelayCommand(_ => PrintForm(PatientFormTemplates.Karta));
        PrintOrzeczenieCommand   = new RelayCommand(_ => PrintForm(PatientFormTemplates.Orzeczenie));
        PrintSanitarneComand     = new RelayCommand(_ => PrintForm(PatientFormTemplates.Sanitarne));
        PrintAnkietaCommand      = new RelayCommand(_ => PrintForm(PatientFormTemplates.Ankieta));
        PrintZaswiadczenieCommand = new RelayCommand(_ => PrintForm(PatientFormTemplates.Komplet));
        PrintAllCommand          = new RelayCommand(_ => PrintForm(PatientFormTemplates.Komplet));
        PrintUrlpoCommand        = new RelayCommand(_ => SkierowanieWydruki.PrintUrlop());
        DeleteSkierowanieCommand  = new RelayCommand(_ => DeleteSkierowanie());
    }

    // ─── Dane pacjenta ───────────────────────────────────────────────────────
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

    private string _rejestrcjaData = string.Empty;
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
    private string _testType = "W";
    public string TestType
    {
        get => _testType;
        set => SetProperty(ref _testType, value);
    }

    // Zagrozenia
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

    private string _certificateDescription = string.Empty;
    public string CertificateDescription { get => _certificateDescription; set => SetProperty(ref _certificateDescription, value); }

    /*
               Ankieta               = reader["B_Ankieta"]             is not DBNull && Convert.ToBoolean(reader["B_Ankieta"]);
               Uwagi                 = reader["B_Uwagi"]?.ToString()   ?? string.Empty;
               wywiad                = reader["B_wywiad"]             is not DBNull && Convert.ToBoolean(reader["B_wywiad"]);
               Orzeczenie            = reader["B_orzeczenie"]         is not DBNull && Convert.ToBoolean(reader["B_orzeczenie"]);
               Orzeczenie_id         = reader["B_orzeczenie_ID"]      is not DBNull ? Convert.ToInt32(reader["B_orzeczenie_ID"]) : (int?)null;
               EDM_P1                = reader["B_EDM"]                is not DBNull && Convert.ToBoolean(reader["B_EDM"]);
               EDM_document_id       = reader["B_edm_document_id"]    is not DBNull ? Convert.ToInt32(reader["B_edm_document_id"]) : (int?)null;

       */
    private bool _ankieta;
    public bool Ankieta { get => _ankieta; set => SetProperty(ref _ankieta, value); }
    private bool _wywiad;
    public bool Wywiad { get => _wywiad; set => SetProperty(ref _wywiad, value); }
    private bool _orzeczenie;
    public bool Orzeczenie { get => _orzeczenie; set => SetProperty(ref _orzeczenie, value); }
    private int? _orzeczenieId;
    public int? OrzeczenieId { get => _orzeczenieId; set => SetProperty(ref _orzeczenieId, value); }
    private bool _B_EDM;
    public bool B_EDM { get => _B_EDM; set => SetProperty(ref _B_EDM, value); }
    private int? _edmDocumentId;
    public int? EdmDocumentId { get => _edmDocumentId; set => SetProperty(ref _edmDocumentId, value); }

    // ─── Widocznosc ──────────────────────────────────────────────────────────
    public Visibility WydrukiVisibility =>
        PatientSkierowanieId > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EditButtonVisibility =>
        PatientSkierowanieId <= 0 ? Visibility.Visible : Visibility.Collapsed;

    private Visibility _deleteButtonVisibility = Visibility.Visible;
    /// <summary>Widoczny tylko gdy karta badan istnieje i nie ma powiazanego badania (B_Badanie_ID).</summary>
    public Visibility DeleteButtonVisibility
    {
        get => _deleteButtonVisibility;
        set => SetProperty(ref _deleteButtonVisibility, value);
    }

    // ─── Metoda ladowania danych z listy skierowan ─────────────────────────
    /// <summary>
    /// Laduje istniejaca kartę badan na podstawie wpisu z listy skierowan (SkierowanieListItem).
    /// </summary>
    public void LoadFromSkierowanie(SkierowanieListItem item)
    {
        PatientSkierowanieId = item.Id;
        PatientId             = item.PacjentId;
        PatientFirstName      = item.Imie      ?? string.Empty;
        PatientLastName       = item.Nazwisko  ?? string.Empty;
        PatientPesel          = item.Pesel     ?? string.Empty;
        CompanyName           = item.Firma     ?? string.Empty;
        ReferralDate          = item.Data;
        TestType              = string.IsNullOrWhiteSpace(item.Typ) ? "W" : item.Typ;

        IsGroupW = TestType == "W";
        IsGroupO = TestType == "O";
        IsGroupK = TestType == "K";
        IsGroupS = TestType == "S";

        LoadPelneDaneZBazy(item.PacjentId);
        LoadSkierowanieSzczegoly(item.Id);
        RefreshAppointmentsFromDb();
        UpdateCanDeleteSkierowanie();
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

    /// <summary>
    /// Dociaga szczegoly karty badan (B_Skierowania) - uwagi, zagrozenia,
    /// zaswiadczenie/ksiazeczka/ankieta oraz date rejestracji.
    /// </summary>
    private void LoadSkierowanieSzczegoly(int skierowanieId)
    {
        if (skierowanieId <= 0 || _dbFactory == null) return;

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    B_DataSkierowania, B_TypBadania, B_Stanowisko, B_RegistrationDate,
                    B_czynnik_fizyczny, B_czynnik_fizyczny_opis,
                    B_czynnik_pylowy, B_czynnik_pylowy_opis,
                    B_czynnik_chemiczny, B_czynnik_chemiczny_opis,
                    B_czynnik_biologiczny, B_czynnik_biologiczny_opis,
                    B_czynnik_inny, B_czynnik_inny_opis,
                    B_Zaswiadczenie, B_ksiazeczka, B_Ankieta, B_Uwagi, B_wywiad, B_orzeczenie, B_orzeczenie_ID, B_EDM   
                FROM B_Skierowania
                WHERE B_ID = @id";
            cmd.Parameters.AddWithValue("@id", skierowanieId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return;

            ReferralDate = reader["B_DataSkierowania"] is DBNull
                ? ReferralDate
                : Convert.ToDateTime(reader["B_DataSkierowania"]);

            var typ = reader["B_TypBadania"]?.ToString();
            if (!string.IsNullOrWhiteSpace(typ))
            {
                TestType = typ;
                IsGroupW = typ == "W";
                IsGroupO = typ == "O";
                IsGroupK = typ == "K";
                IsGroupS = typ == "S";
            }

            if (reader["B_Stanowisko"] is not DBNull)
                PatientJobTitle = reader["B_Stanowisko"]?.ToString() ?? PatientJobTitle;

            RejestrcjaData = reader["B_RegistrationDate"] is DBNull
                ? string.Empty
                : Convert.ToDateTime(reader["B_RegistrationDate"]).ToString("dd.MM.yyyy HH:mm");

            IsPhysical            = reader["B_czynnik_fizyczny"]    is not DBNull && Convert.ToBoolean(reader["B_czynnik_fizyczny"]);
            PhysicalDescription   = reader["B_czynnik_fizyczny_opis"]?.ToString()   ?? string.Empty;
            IsDust                = reader["B_czynnik_pylowy"]      is not DBNull && Convert.ToBoolean(reader["B_czynnik_pylowy"]);
            DustDescription       = reader["B_czynnik_pylowy_opis"]?.ToString()     ?? string.Empty;
            IsChemical            = reader["B_czynnik_chemiczny"]   is not DBNull && Convert.ToBoolean(reader["B_czynnik_chemiczny"]);
            ChemicalDescription   = reader["B_czynnik_chemiczny_opis"]?.ToString()  ?? string.Empty;
            IsBiological          = reader["B_czynnik_biologiczny"] is not DBNull && Convert.ToBoolean(reader["B_czynnik_biologiczny"]);
            BiologicalDescription = reader["B_czynnik_biologiczny_opis"]?.ToString() ?? string.Empty;
            IsOther               = reader["B_czynnik_inny"]        is not DBNull && Convert.ToBoolean(reader["B_czynnik_inny"]);
            OtherDescription      = reader["B_czynnik_inny_opis"]?.ToString()       ?? string.Empty;
            IsCertificate         = reader["B_Zaswiadczenie"]       is not DBNull && Convert.ToBoolean(reader["B_Zaswiadczenie"]);
            IsbookletSanepid      = reader["B_ksiazeczka"]          is not DBNull && Convert.ToBoolean(reader["B_ksiazeczka"]);
            Ankieta               = reader["B_Ankieta"]             is not DBNull && Convert.ToBoolean(reader["B_Ankieta"]);
            Uwagi                 = reader["B_Uwagi"]?.ToString()   ?? string.Empty;
            Wywiad                = reader["B_wywiad"]             is not DBNull && Convert.ToBoolean(reader["B_wywiad"]);
            Orzeczenie            = reader["B_orzeczenie"]         is not DBNull && Convert.ToBoolean(reader["B_orzeczenie"]);
            OrzeczenieId          = reader["B_orzeczenie_ID"]      is not DBNull ? Convert.ToInt32(reader["B_orzeczenie_ID"]) : (int?)null;
            B_EDM                  = reader["B_EDM"]                is not DBNull && Convert.ToBoolean(reader["B_EDM"]);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Blad ladowania szczegolow karty badan:\n" + ex.Message,
                "Blad", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateTestType()
    {
        TestType = IsGroupW ? "W" : IsGroupO ? "O" : IsGroupK ? "K" : TestType;
    }

    private void ZapiszSkierowanie()
    {
        if (PatientSkierowanieId <= 0 || _dbFactory == null)
        {
            MessageBox.Show("Brak karty badan do zapisu.",
                "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE B_Skierowania SET
                    B_DataSkierowania = @dataSkier,
                    B_TypBadania = @typBadania,
                    B_Stanowisko = @stanowisko,
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
                    B_ksiazeczka = @ksiazeczka
                WHERE B_ID = @id";
            cmd.Parameters.AddWithValue("@id", PatientSkierowanieId);
            cmd.Parameters.AddWithValue("@dataSkier", ReferralDate.HasValue ? (object)ReferralDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@typBadania", string.IsNullOrWhiteSpace(TestType) ? (object)DBNull.Value : TestType);
            cmd.Parameters.AddWithValue("@stanowisko", string.IsNullOrWhiteSpace(PatientJobTitle) ? (object)DBNull.Value : PatientJobTitle.Trim());
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
    


            cmd.ExecuteNonQuery();

            MessageBox.Show("Karta badan zapisana pomyslnie.",
                "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Blad podczas zapisu karty badan: {ex.Message}",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─── Scheduler / Rejestracja ─────────────────────────────────────────────
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
                insCmd.Parameters.AddWithValue("@uwagi", appointment.Subject ?? string.Empty);
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
                updCmd.Parameters.AddWithValue("@uwagi", appointment.Subject ?? string.Empty);
                updCmd.Parameters.AddWithValue("@id", existingRId.Value);
                updCmd.ExecuteNonQuery();
            }

            RefreshAppointmentsFromDb();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Blad synchronizacji rejestracji: {ex.Message}",
                "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─── Usuwanie karty badan (z walidacja jak w legacy) ───────────────────

    /// <summary>
    /// Sprawdza czy skierowanie ma powiazane badanie (B_Badanie_ID > 0).
    /// Jesli tak, karty badan nie mozna usunac (trzeba najpierw usunac badanie).
    /// </summary>
    private bool HasSkierowanieBadanie(int skierowanieId)
    {
        if (skierowanieId <= 0 || _dbFactory == null) return false;

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = (MySqlCommand)conn.CreateCommand();
            cmd.CommandText = "SELECT B_Badanie_ID FROM B_Skierowania WHERE B_ID = @id";
            cmd.Parameters.AddWithValue("@id", skierowanieId);

            var result = cmd.ExecuteScalar();
            if (result == null || result is DBNull) return false;

            if (int.TryParse(result.ToString(), out var badanieId))
                return badanieId > 0;

            return false;
        }
        catch (Exception)
        {
            // W razie bledu zakladamy ze ma badanie (bezpieczniej)
            return true;
        }
    }

    /// <summary>
    /// Odswieza widocznosc przycisku "Usun Karte Badan" na podstawie tego,
    /// czy skierowanie ma juz powiazane badanie.
    /// </summary>
    public void UpdateCanDeleteSkierowanie()
    {
        try
        {
            if (PatientSkierowanieId <= 0)
            {
                DeleteButtonVisibility = Visibility.Collapsed;
                return;
            }

            DeleteButtonVisibility = HasSkierowanieBadanie(PatientSkierowanieId)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        catch (Exception)
        {
            DeleteButtonVisibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Usuwa karte badan po walidacji (brak powiazanego badania) i potwierdzeniu uzytkownika.
    /// </summary>
    private void DeleteSkierowanie()
    {
        if (PatientSkierowanieId <= 0)
        {
            MessageBox.Show("Brak karty badan do usuniecia.", "Blad",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Walidacja: nie mozna usunac karty, jesli istnieje powiazane badanie
        if (HasSkierowanieBadanie(PatientSkierowanieId))
        {
            MessageBox.Show(
                "Nie mozna usunac karty badan.\nIstnieje powiazane badanie.\n\nNajpierw usun badanie.",
                "Blad usuwania",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Czy na pewno chcesz usunac karte badan #{PatientSkierowanieId}?\n\n" +
            $"Pacjent: {PatientFirstName} {PatientLastName}\n" +
            $"Firma: {CompanyName}\n\n" +
            "Ta operacja jest nieodwracalna!",
            "Potwierdzenie usuniecia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        if (_dbFactory == null) return;

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();

            // Ponowna walidacja tuz przed usunieciem (na wypadek zmian miedzyczasu)
            using (var checkCmd = (MySqlCommand)conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT B_Badanie_ID FROM B_Skierowania WHERE B_ID = @id";
                checkCmd.Parameters.AddWithValue("@id", PatientSkierowanieId);
                var checkResult = checkCmd.ExecuteScalar();

                if (checkResult is not null && checkResult is not DBNull
                    && int.TryParse(checkResult.ToString(), out var badanieId) && badanieId > 0)
                {
                    MessageBox.Show(
                        $"Nie mozna usunac karty badan.\nIstnieje powiazane badanie (ID: {badanieId}).\n\nNajpierw usun badanie.",
                        "Blad usuwania",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            using var delCmd = (MySqlCommand)conn.CreateCommand();
            delCmd.CommandText = "DELETE FROM B_Skierowania WHERE B_ID = @id";
            delCmd.Parameters.AddWithValue("@id", PatientSkierowanieId);
            int rows = delCmd.ExecuteNonQuery();

            if (rows > 0)
            {
                RequestPowrot?.Invoke();
            }
            else
            {
                MessageBox.Show("Nie znaleziono karty badan do usuniecia.", "Blad",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Blad usuwania karty badan:\n{ex.Message}",
                "Blad bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
