using ASMED.EDM.Data.Services;
using MySqlConnector;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ASMED.EDM.UI.ViewModels.Skierowania
{
    /// <summary>
    /// ViewModel dla formularza dodawania/edycji pacjenta w zakładce Skierowania.
    /// Migracja z legacy SkierPacjentaEditViewModel (Access/ODBC → MySQL).
    /// </summary>
    public class SkierPacjentaEditViewModel : INotifyPropertyChanged
    {
        private readonly DbConnectionFactory _dbFactory;

        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>Zdarzenie zamknięcia widoku — wraca do listy pacjentów.</summary>
        public event Action? RequestPowrot;

        // ─── Komendy ──────────────────────────────────────────────────────────
        public ICommand SaveCommand { get; }
        public ICommand AnulujCommand { get; }
        public ICommand ClearSearchFirmyCommand { get; }
        public ICommand RefreshFirmyCommand { get; }
        public ICommand DodajKarteBadanCommand { get; }
        public ICommand UsunPacjentaCommand { get; }
        public ICommand CopyPacjentIdCommand { get; }

        /// <summary>Ustawiany przez code-behind — otwiera formularz nowego pacjenta z wstępnie uzupełnionymi danymi.</summary>
        public Action? NavigateCopyPacjent { get; set; }

        public SkierPacjentaEditViewModel(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;

            SaveCommand             = new RelayCommand(_ => ZapiszPacjenta(), _ => CzyWybranoFirme);
            AnulujCommand           = new RelayCommand(_ => RequestPowrot?.Invoke());
            ClearSearchFirmyCommand = new RelayCommand(_ => FrazaFirmy = string.Empty);
            RefreshFirmyCommand     = new RelayCommand(_ => LoadFirmyFromDb());
            DodajKarteBadanCommand  = new RelayCommand(_ => DodajKarteBadan(), _ => PacjentZapisany);
            UsunPacjentaCommand     = new RelayCommand(_ => UsunPacjenta(),    _ => PacjentZapisany);
            CopyPacjentIdCommand    = new RelayCommand(_ => NavigateCopyPacjent?.Invoke());

            LoadAutoCompleteData();
            LoadFirmyFromDb();
        }

        /// <summary>
        /// Udostępnia połączenie do bazy dla widoku (używane przez code-behind tylko do zapytań ad-hoc).
        /// </summary>
        public System.Data.Common.DbConnection GetDbConnection()
        {
            return _dbFactory.CreateConnection();
        }

        // ─── Tryb edycji ──────────────────────────────────────────────────────
        /// <summary>true po pomyślnym zapisie — odblokowuje przyciski Dodaj kartę / Usuń pacjenta.</summary>
        private bool _pacjentZapisany;
        public bool PacjentZapisany
        {
            get => _pacjentZapisany;
            private set
            {
                _pacjentZapisany = value;
                OnPropertyChanged();
            }
        }

        /// <summary>0 = nowy pacjent, >0 = edycja istniejącego</summary>
        private int _pacjentId;
        public int PacjentId
        {
            get => _pacjentId;
            set { _pacjentId = value; OnPropertyChanged(); OnPropertyChanged(nameof(TytulFormularza)); }
        }
        public string TytulFormularza => PacjentId > 0
            ? $"✏️ Edycja pacjenta (ID: {PacjentId})"
            : "🧑‍💼 Nowy pacjent";

        // ─── Dane osobowe ─────────────────────────────────────────────────────
        private string _imie = string.Empty;
        public string Imie
        {
            get => _imie;
            set { _imie = value; OnPropertyChanged(); }
        }

        private string _nazwisko = string.Empty;
        public string Nazwisko
        {
            get => _nazwisko;
            set { _nazwisko = value; OnPropertyChanged(); }
        }

        private string _pesel = string.Empty;
        public string PESEL
        {
            get => _pesel;
            set
            {
                _pesel = value;
                OnPropertyChanged();
                WyliczDateIPlecZPesel(value);
            }
        }

        private bool _brakPesel;
        public bool BrakPESEL
        {
            get => _brakPesel;
            set { _brakPesel = value; OnPropertyChanged(); OnPropertyChanged(nameof(PeselEnabled)); }
        }
        public bool PeselEnabled => !BrakPESEL;

        private string? _plec;
        public string? Plec
        {
            get => _plec;
            set { _plec = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsMezczyzna)); OnPropertyChanged(nameof(IsKobieta)); }
        }
        public bool IsMezczyzna
        {
            get => Plec == "M";
            set { if (value) Plec = "M"; }
        }
        public bool IsKobieta
        {
            get => Plec == "K";
            set { if (value) Plec = "K"; }
        }

        private DateTime? _dataUrodzenia;
        public DateTime? DataUrodzenia
        {
            get => _dataUrodzenia;
            set { _dataUrodzenia = value; OnPropertyChanged(); }
        }

        private string _zawod = string.Empty;
        public string Zawod
        {
            get => _zawod;
            set { _zawod = value; OnPropertyChanged(); }
        }

        private string _uwagi = string.Empty;
        public string Uwagi
        {
            get => _uwagi;
            set { _uwagi = value; OnPropertyChanged(); }
        }

        // ─── Adres ────────────────────────────────────────────────────────────
        private string _ulica = string.Empty;
        public string Ulica
        {
            get => _ulica;
            set { _ulica = value; OnPropertyChanged(); }
        }

        private string _kodPocztowy = string.Empty;
        public string KodPocztowy
        {
            get => _kodPocztowy;
            set { _kodPocztowy = value; OnPropertyChanged(); }
        }

        private string _miejscowosc = string.Empty;
        public string Miejscowosc
        {
            get => _miejscowosc;
            set { _miejscowosc = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }
        private string _email = string.Empty;

        private string _telefon = string.Empty;
        public string Telefon
        {
            get => _telefon;
            set { _telefon = value; OnPropertyChanged(); }
        }

        // ─── AutoComplete — kolekcje podpowiedzi ──────────────────────────────
        public ObservableCollection<string> ImionaItems     { get; } = new();
        public ObservableCollection<string> NazwiskaItems   { get; } = new();
        public ObservableCollection<string> ZawodyItems     { get; } = new();
        public ObservableCollection<string> UliceItems      { get; } = new();
        public ObservableCollection<string> MiastoItems     { get; } = new();

        private void LoadAutoCompleteData()
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                conn.Open();

                LoadSimpleList(conn, "SELECT DISTINCT P_imie    FROM P_Pacjent WHERE P_imie    IS NOT NULL ORDER BY P_imie",    "P_imie",        ImionaItems);
                LoadSimpleList(conn, "SELECT DISTINCT P_nazwisko FROM P_Pacjent WHERE P_nazwisko IS NOT NULL ORDER BY P_nazwisko", "P_nazwisko",  NazwiskaItems);
                LoadSimpleList(conn, "SELECT DISTINCT P_zawod    FROM P_Pacjent WHERE P_zawod    IS NOT NULL ORDER BY P_zawod",   "P_zawod",     ZawodyItems);
                LoadSimpleList(conn, "SELECT S_Ulica FROM S__Ulice ORDER BY S_Ulica",                                             "S_Ulica",     UliceItems);
                LoadSimpleList(conn, "SELECT DISTINCT P_Ades_miasto FROM P_Pacjent WHERE P_Ades_miasto IS NOT NULL ORDER BY P_Ades_miasto", "P_Ades_miasto", MiastoItems);
            }
            catch
            {
                // AutoComplete jest opcjonalne — błąd nie blokuje formularza
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

        // ─── Firma
        private ObservableCollection<FirmaItem> _firmy = new();
        private ObservableCollection<FirmaItem> _filtrowanaFirmy = new();

        public ObservableCollection<FirmaItem> FiltrowaneFirmy
        {
            get => _filtrowanaFirmy;
            set { _filtrowanaFirmy = value; OnPropertyChanged(); }
        }

        private string _frazaFirmy = string.Empty;
        public string FrazaFirmy
        {
            get => _frazaFirmy;
            set { _frazaFirmy = value; OnPropertyChanged(); FiltrujFirmy(); }
        }

        private FirmaItem? _wybranaFirma;
        public FirmaItem? WybranaFirma
        {
            get => _wybranaFirma;
            set
            {
                _wybranaFirma = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CzyWybranoFirme));
                OnPropertyChanged(nameof(WybranaFirmaNazwa));
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public bool CzyWybranoFirme => WybranaFirma != null && WybranaFirma.Id > 0;
        public string WybranaFirmaNazwa => WybranaFirma?.Nazwa ?? "— nie wybrano —";

        // ─── Ładowanie firm z MySQL ───────────────────────────────────────────
        public void LoadPacjentById(int id)
        {
            if (id <= 0) return;
            try
            {
                using var conn = _dbFactory.CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    "SELECT P_imie, P_nazwisko, P_pesel, P_brak, P_plec, P_data_urodzenia, " +
                    "P_zawod, P_Uwagi, P_Adres_ulica_numer, P_Ades_kod, P_Ades_miasto, P_Firma_id, P_email, P_telefon " +
                    "FROM P_Pacjent WHERE P_ID = @id";
                ((MySqlCommand)cmd).Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return;

                PacjentId     = id;
                Imie          = reader["P_imie"]?.ToString()        ?? string.Empty;
                Nazwisko      = reader["P_nazwisko"]?.ToString()    ?? string.Empty;
                BrakPESEL     = reader["P_brak"] != DBNull.Value && Convert.ToBoolean(reader["P_brak"]);
                Plec          = reader["P_plec"]?.ToString();
                DataUrodzenia = reader["P_data_urodzenia"] is DBNull
                                ? null
                                : Convert.ToDateTime(reader["P_data_urodzenia"]);
                Zawod         = reader["P_zawod"]?.ToString()       ?? string.Empty;
                Uwagi         = reader["P_Uwagi"]?.ToString()       ?? string.Empty;
                Ulica         = reader["P_Adres_ulica_numer"]?.ToString()  ?? string.Empty;
                KodPocztowy   = reader["P_Ades_kod"]?.ToString()    ?? string.Empty;
                Miejscowosc   = reader["P_Ades_miasto"]?.ToString() ?? string.Empty;
                Email         = reader["P_email"]?.ToString()       ?? string.Empty;
                Telefon       = reader["P_telefon"]?.ToString()     ?? string.Empty;

                // PESEL - ustawiamy backing field bezposrednio, zeby nie nadpisac
                // daty urodzenia i plci (juz ustawione z bazy powyzej)
                _pesel = reader["P_pesel"]?.ToString() ?? string.Empty;
                OnPropertyChanged(nameof(PESEL));

                var firmaId = reader["P_Firma_id"] is DBNull
                              ? 0
                              : Convert.ToInt32(reader["P_Firma_id"]);
                reader.Close();

                WybranaFirma = firmaId > 0
                    ? _firmy.FirstOrDefault(f => f.Id == firmaId)
                    : null;

                PacjentZapisany = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Blad ladowania danych pacjenta:\n" + ex.Message,
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadFirmyFromDb()
        {
            _firmy.Clear();
            try
            {
                using var conn = _dbFactory.CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, Nazwa, NIP, Ulica FROM Firma WHERE activ = 1 ORDER BY Nazwa";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    _firmy.Add(new FirmaItem
                    {
                        Id     = reader["id"]    is DBNull ? 0 : Convert.ToInt32(reader["id"]),
                        Nazwa  = reader["Nazwa"]?.ToString() ?? string.Empty,
                        NIP    = reader["NIP"]?.ToString()   ?? string.Empty,
                        Adres  = reader["Ulica"]?.ToString() ?? string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania firm:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            FiltrujFirmy();
        }

        private void FiltrujFirmy()
        {
            var fraza = FrazaFirmy.Trim().ToLowerInvariant();
            var lista = string.IsNullOrEmpty(fraza)
                ? _firmy
                : new ObservableCollection<FirmaItem>(
                    _firmy.Where(f =>
                        f.Nazwa.ToLowerInvariant().Contains(fraza) ||
                        f.NIP.ToLowerInvariant().Contains(fraza) ||
                        f.Adres.ToLowerInvariant().Contains(fraza)));
            FiltrowaneFirmy = lista;
        }

        // ─── PESEL → data urodzenia + płeć ───────────────────────────────────
        private void WyliczDateIPlecZPesel(string? pesel)
        {
            if (string.IsNullOrEmpty(pesel) || pesel.Length != 11
                || !pesel.All(char.IsDigit))
            {
                DataUrodzenia = null;
                Plec = null;
                return;
            }

            try
            {
                int rok   = int.Parse(pesel[..2]);
                int miesiac = int.Parse(pesel[2..4]);
                int dzien = int.Parse(pesel[4..6]);

                if (miesiac >= 81)      { rok += 1800; miesiac -= 80; }
                else if (miesiac >= 61) { rok += 2200; miesiac -= 60; }
                else if (miesiac >= 41) { rok += 2100; miesiac -= 40; }
                else if (miesiac >= 21) { rok += 2000; miesiac -= 20; }
                else                   { rok += 1900; }

                DataUrodzenia = new DateTime(rok, miesiac, dzien);
            }
            catch { DataUrodzenia = null; }

            int plecCyfra = int.Parse(pesel[9].ToString());
            Plec = plecCyfra % 2 == 0 ? "K" : "M";
        }

        // ─── Zapis ────────────────────────────────────────────────────────────
        private void ZapiszPacjenta()
        {
            if (string.IsNullOrWhiteSpace(Imie))
            {
                MessageBox.Show("Pole Imię jest wymagane.", "Walidacja",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(Nazwisko))
            {
                MessageBox.Show("Pole Nazwisko jest wymagane.", "Walidacja",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(PESEL) && !BrakPESEL)
            {
                MessageBox.Show("Podaj numer PESEL lub zaznacz 'Brak PESEL'.", "Walidacja",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var conn = _dbFactory.CreateConnection();
                conn.Open();
                using var cmd = (MySqlCommand)conn.CreateCommand();

                if (PacjentId == 0)
                {
                    // INSERT nowy pacjent
                    cmd.CommandText = @"
                        INSERT INTO P_Pacjent
                            (P_Imie, P_Nazwisko, P_PESEL, P_brak, P_Plec,
                             P_data_urodzenia, P_zawod, P_Adres_ulica_numer,
                             P_Ades_kod, P_Ades_miasto, P_Uwagi, P_Firma_id,P_firma, P_email, P_telefon, P_activ)
                        VALUES
                            (@imie, @nazwisko, @pesel, @brakPesel, @plec,
                             @dataUr, @zawod, @ulica,
                             @kod, @miasto, @uwagi, @firmaId, @firma, @email, @telefon, 1)";
                }
                else
                {
                    // UPDATE istniejący
                    cmd.CommandText = @"
                        UPDATE P_Pacjent SET
                            P_Imie = @imie, P_Nazwisko = @nazwisko,
                            P_PESEL = @pesel, P_brak = @brakPesel, P_Plec = @plec,
                            P_data_urodzenia = @dataUr, P_zawod = @zawod,
                            P_Adres_ulica_numer = @ulica, P_Ades_kod = @kod,
                            P_Ades_miasto = @miasto, P_Uwagi = @uwagi,
                            P_Firma_id = @firmaId,
                            P_firma = @firma,
                            P_email = @email, 
                            P_telefon = @telefon
                        WHERE P_ID = @id";
                    cmd.Parameters.AddWithValue("@id", PacjentId);
                }

                cmd.Parameters.AddWithValue("@imie",     Imie.Trim());
                cmd.Parameters.AddWithValue("@nazwisko",  Nazwisko.Trim());
                cmd.Parameters.AddWithValue("@pesel",     BrakPESEL ? (object)DBNull.Value : PESEL.Trim());
                cmd.Parameters.AddWithValue("@brakPesel", BrakPESEL ? 1 : 0);
                cmd.Parameters.AddWithValue("@plec",      Plec ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@dataUr",    DataUrodzenia.HasValue ? (object)DataUrodzenia.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@zawod",     Zawod.Trim());
                cmd.Parameters.AddWithValue("@ulica",     Ulica.Trim());
                cmd.Parameters.AddWithValue("@kod",       KodPocztowy.Trim());
                cmd.Parameters.AddWithValue("@miasto",    Miejscowosc.Trim());
                cmd.Parameters.AddWithValue("@uwagi",     Uwagi.Trim());
                cmd.Parameters.AddWithValue("@firmaId",   WybranaFirma!.Id);
                cmd.Parameters.AddWithValue("@email", Email.Trim());
                cmd.Parameters.AddWithValue("@telefon", Telefon.Trim());
                cmd.Parameters.AddWithValue("@firma", WybranaFirma?.Nazwa ?? string.Empty);

                cmd.ExecuteNonQuery();

                // Po INSERT pobierz nadany ID
                if (_pacjentId == 0)
                {
                    using var cmdId = (MySqlCommand)conn.CreateCommand();
                    cmdId.CommandText = "SELECT LAST_INSERT_ID()";
                    PacjentId = Convert.ToInt32(cmdId.ExecuteScalar());
                }

                PacjentZapisany = true;

                MessageBox.Show(
                    _pacjentId > 0 ? $"✅ Dane pacjenta zostały zaktualizowane." : $"✅ Pacjent został dodany (ID: {PacjentId}).",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── INotifyPropertyChanged
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void DodajKarteBadan()
        {
            // TODO: otworzyć formularz nowej karty badań powiązanej z PacjentId
            MessageBox.Show($"Funkcja 'Dodaj Kartę Badań' dla pacjenta ID {PacjentId}\nzostanie zaimplementowana w kolejnym kroku.",
                "Dodaj Kartę Badań", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UsunPacjenta()
        {
            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć pacjenta {Imie} {Nazwisko} (ID: {PacjentId})?\n\nOperacja jest nieodwracalna.",
                "Usuń pacjenta", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var conn = _dbFactory.CreateConnection();
                conn.Open();
                using var cmd = (MySqlCommand)conn.CreateCommand();
                // Usunięcie rekordu z tabeli P_Pacjent - wykonuje DELETE zamiast soft-delete
                cmd.CommandText = "DELETE FROM P_Pacjent WHERE P_ID = @id";
                cmd.Parameters.AddWithValue("@id", PacjentId);
                var affected = cmd.ExecuteNonQuery();

                if (affected == 0)
                {
                    MessageBox.Show("Rekord nie został usunięty (nie znaleziono rekordu).", "Usuń pacjenta", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show("Pacjent został usunięty.", "Usunięto",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                RequestPowrot?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd usuwania:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // ─── Model firmy ─────────────────────────────────────────────────────────
    public class FirmaItem
    {
        public int    Id    { get; set; }
        public string Nazwa { get; set; } = string.Empty;
        public string NIP   { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;

        public string DisplayText => string.IsNullOrEmpty(NIP)
            ? Nazwa
            : $"{Nazwa}  |  NIP: {NIP}";
    }

    // ─── Prosty RelayCommand ──────────────────────────────────────────────────
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? p) => _canExecute?.Invoke(p) ?? true;
        public void Execute(object? p)    => _execute(p);
        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
