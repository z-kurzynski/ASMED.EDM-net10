using ASMED.WPF.Helpers;
using Syncfusion.Windows.Controls.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using System.Windows;
using System;
using System.Windows.Input;
using System.Linq;

namespace ASMED.WPF.ViewModels.Skierowania
{
    public class SkierPacjentaEditViewModel : INotifyPropertyChanged
    {

        private MainWindowViewModel _mainWindowViewModel;
        public ICommand ?PowrotDoListyCommand { get; set; }

        public SkierPacjentaEditViewModel(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
            PowrotDoListyCommand = new RelayCommand(_ => PowrotDoListy());
            UtworzSkierowanieCommand = new RelayCommand(_ => UtworzSkierowanie());

            // Inicjalizacja pozostałych komend i danych
            SaveCommandskr = new RelayCommand(_ => ZapiszNowegoPacjenta());
            AnulujCommand = new RelayCommand(_ => CloseDialog());
            ClearSearchCommand = new RelayCommand(_ => ClearSearch()); // ✅ DODANE
            
            // ✅ NOWE: Komendy dla przycisków odświeżania i dodawania firmy
            RefreshCommand = new RelayCommand(_ => RefreshFirmy());
            NowaFirmaDialogCommand = new RelayCommand(_ => OtworzDialogNowaFirma());
            
            LoadFirmyFromDb();
            LoadImionaFromDb();
            LoadNazwiskaFromDb();
            LoadUliceFromDb();
            LoadZawodyFromDb();
            LoadMiastaFromDb();
        }



        private void PowrotDoListy()
        {
            _mainWindowViewModel.NowaKartaBadanWidok = new SkierListaPacjentowViewModel();
        }

        // ---------------- IMIE --- -P_imie FROM P_Pacjent GROUP BY P_imie -----------------------------
        private ObservableCollection<string> _imionaItems = new();
        public ObservableCollection<string> ImionaItems
        {
            get => _imionaItems;
            set { _imionaItems = value; OnPropertyChanged(nameof(ImionaItems)); }
        }

        private string? _wybraneImie;
        public string? WybraneImie
        {
            get => _wybraneImie;
            set
            {
                if (_wybraneImie != value)
                {
                    _wybraneImie = value;
                    Imie = value;
                    OnPropertyChanged(nameof(WybraneImie));
                    OnPropertyChanged(nameof(Imie));
                }
            }
        }

        private void LoadImionaFromDb()
        {
            var db = new AccessDbHelper();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT P_imie FROM P_Pacjent GROUP BY P_imie";
                    using (var reader = cmd.ExecuteReader())
                    {
                        ImionaItems.Clear();
                        while (reader.Read())
                        {
                            if (reader["P_imie"] != DBNull.Value)
                                ImionaItems.Add(reader["P_imie"].ToString());
                        }
                    }
                }
            }
        }

        // ---------Nazwiko P_nazwisko FROM P_Pacjent GROUP BY P_nazwisko ----------------------------------------
        private ObservableCollection<string> _nazwiskaItems = new();
        public ObservableCollection<string> NazwiskaItems
        {
            get => _nazwiskaItems;
            set { _nazwiskaItems = value; OnPropertyChanged(nameof(NazwiskaItems)); }
        }

        private string? _wybraneNazwisko;
        public string? WybraneNazwisko
        {
            get => _wybraneNazwisko;
            set
            {
                if (_wybraneNazwisko != value)
                {
                    _wybraneNazwisko = value;
                    Nazwisko = value;
                    OnPropertyChanged(nameof(WybraneNazwisko));
                    OnPropertyChanged(nameof(Nazwisko));
                }
            }
        }

        private void LoadNazwiskaFromDb()
        {
            var db = new AccessDbHelper();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT P_nazwisko FROM P_Pacjent GROUP BY P_nazwisko ";
                    using (var reader = cmd.ExecuteReader())
                    {
                        NazwiskaItems.Clear();
                        while (reader.Read())
                        {
                            if (reader["P_nazwisko"] != DBNull.Value)
                                NazwiskaItems.Add(reader["P_nazwisko"].ToString());
                        }
                    }
                }
            }
        }

        // ------------------ULICA S_Ulica------------------------
        private ObservableCollection<string> _uliceItems = new();
        public ObservableCollection<string> UliceItems
        {
            get => _uliceItems;
            set { _uliceItems = value; OnPropertyChanged(nameof(UliceItems)); }
        }
        private string? _wybranaUlica;
        public string? WybranaUlica
        {
            get => _wybranaUlica;
            set
            {
                if (_wybranaUlica != value)
                {
                    _wybranaUlica = value;
                    UlicaNumerDomu = value;
                    OnPropertyChanged(nameof(WybranaUlica));
                    OnPropertyChanged(nameof(UlicaNumerDomu));
                }
            }
        }

        private void LoadUliceFromDb()
        {
            var db = new AccessDbHelper();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT S_Ulica FROM  S__Ulice";
                    using (var reader = cmd.ExecuteReader())
                    {
                        UliceItems.Clear();
                        while (reader.Read())
                        {
                            if (reader["S_Ulica"] != DBNull.Value)
                                UliceItems.Add(reader["S_Ulica"].ToString());
                        }
                    }
                }
            }
        }

        // ---------------Stanowisko ------ SELECT P_zawód FROM  P_Pacjent  GROUP BY P_zawód --------------
        private ObservableCollection<string> _zawodyItems = new();
        public ObservableCollection<string> ZawodyItems
        {
            get => _zawodyItems;
            set { _zawodyItems = value; OnPropertyChanged(nameof(ZawodyItems)); }
        }
        private string? _wybranyZawod;
        public string? WybranyZawod
        {
            get => _wybranyZawod;
            set
            {
                if (_wybranyZawod != value)
                {
                    _wybranyZawod = value;
                    Zawod = value;
                    OnPropertyChanged(nameof(WybranyZawod));
                    OnPropertyChanged(nameof(Zawod));
                }
            }
        }
        private void LoadZawodyFromDb()
        {
            var db = new AccessDbHelper();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT P_zawód FROM  P_Pacjent  GROUP BY P_zawód";
                    using (var reader = cmd.ExecuteReader())
                    {
                        ZawodyItems.Clear();
                        while (reader.Read())
                        {
                            if (reader["P_zawód"] != DBNull.Value)
                                ZawodyItems.Add(reader["P_zawód"].ToString());
                        }
                    }
                }
            }
        }

        // ---------------Miasto ------ SELECT  P_Ades_miasto FROM  P_Pacjent GROUP BY P_Ades_miasto  --------------
        private ObservableCollection<string> _miastaItems = new();
        public ObservableCollection<string> MiastaItems
        {
            get => _miastaItems;
            set { _miastaItems = value; OnPropertyChanged(nameof(MiastaItems)); }
        }
        private string? _wybraneMiasto;
        public string? WybraneMiasto
        {
            get => _wybraneMiasto;
            set
            {
                if (_wybraneMiasto != value)
                {
                    _wybraneMiasto = value;
                    Miejscowosc = value;
                    OnPropertyChanged(nameof(WybraneMiasto));
                    OnPropertyChanged(nameof(Miejscowosc));
                }
            }
        }
        private void LoadMiastaFromDb()
        {
            var db = new AccessDbHelper();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT  P_Ades_miasto FROM  P_Pacjent GROUP BY P_Ades_miasto";
                    using (var reader = cmd.ExecuteReader())
                    {
                        MiastaItems.Clear();
                        while (reader.Read())
                        {
                            if (reader["P_Ades_miasto"] != DBNull.Value)
                                MiastaItems.Add(reader["P_Ades_miasto"].ToString());
                        }
                    }
                }
            }
        }

        // ----------------------------------------------------------------

        // Pesel, BrakPesel, Plec, DataUrodzenia, Imie, Nazwisko, Zawod, UlicaNumerDomu, comments, KodPocztowy, Miejscowosc, WybranaFirma
        public event Action? RequestClose;
        private string? _pesel;
        public string? PESEL
        {
            get => _pesel;
            set
            {
                if (_pesel != value)
                {
                    _pesel = value;
                    OnPropertyChanged(nameof(PESEL));
                    WyliczDateIUstawPlecZPesel(_pesel);
                }
            }
        }

        private void WyliczDateIUstawPlecZPesel(string? pesel)
        {
            if (string.IsNullOrEmpty(pesel) || pesel.Length != 11)
            {
                DataUrodzenia = null;
                Plec = null;
                OnPropertyChanged(nameof(DataUrodzenia));
                OnPropertyChanged(nameof(Plec));
                return;
            }

            int rok = int.Parse(pesel.Substring(0, 2));
            int miesiac = int.Parse(pesel.Substring(2, 2));
            int dzien = int.Parse(pesel.Substring(4, 2));

            int wiek = miesiac / 20;
            miesiac = miesiac % 20;
            int pelnyRok = 1900 + rok;
            if (wiek == 1) pelnyRok += 100;
            if (wiek == 2) pelnyRok += 200;
            if (wiek == 3) pelnyRok += 300;
            if (wiek == 4) pelnyRok += 400;

            try
            {
                DataUrodzenia = new DateTime(pelnyRok, miesiac, dzien);
            }
            catch
            {
                DataUrodzenia = null;
            }
            OnPropertyChanged(nameof(DataUrodzenia));

            int plecCyfra = int.Parse(pesel.Substring(9, 1));
            Plec = plecCyfra % 2 == 1 ? "M" : "K";
            OnPropertyChanged(nameof(Plec));
        }
        public bool? BrakPESEL { get; set; } = false;
        public string? Plec { get; set; }
        public DateTime? DataUrodzenia { get; set; }
        private int? _id;
        public int? ID
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(ID));
                }
            }
        }
        public string? Imie { get; set; }
        public string? Nazwisko { get; set; }
        public string? Zawod { get; set; }
        public string? UlicaNumerDomu { get; set; }
        public string? KodPocztowy { get; set; }
        public ICommand ?ClearMiejscowoscCommand { get; }
        public ObservableCollection<FirmaDto> FiltrowaneFirmy { get; set; } = new();
        public ICommand ?SaveCommandskr { get; }
        public int? P_firma_id { get; set; }
        public string? comments { get; set; }
        private string ?_miejscowosc = "Warszawa";
        public string ?Miejscowosc
        {
            get => _miejscowosc;
            set
            {
                if (_miejscowosc != value)
                {
                    _miejscowosc = value;
                    OnPropertyChanged(nameof(Miejscowosc));
                }
            }
        }

        public ObservableCollection<FirmaDto> Firmy { get; set; } = new();
        private string? _frazaFirma;
        public string? FrazaFirma
        {
            get => _frazaFirma;
            set
            {
                if (_frazaFirma != value)
                {
                    _frazaFirma = value;
                    OnPropertyChanged(nameof(FrazaFirma));
                    FiltrujFirmy();
                }
            }
        }
        private void FiltrujFirmy()
        {
            FiltrowaneFirmy.Clear();
            if (string.IsNullOrWhiteSpace(FrazaFirma))
            {
                foreach (var firma in Firmy)
                    FiltrowaneFirmy.Add(firma);
            }
            else
            {
                var filtr = FrazaFirma.Trim().ToLower();
                foreach (var firma in Firmy)
                {
                    if ((firma.Name?.ToLower().Contains(filtr) ?? false) || (firma.NIP?.ToLower().Contains(filtr) ?? false) || (firma.Address?.ToLower().Contains(filtr) ?? false))
                        FiltrowaneFirmy.Add(firma);
                }
            }
        }
        private FirmaDto? _wybranaFirma;
        public FirmaDto? WybranaFirma
        {
            get => _wybranaFirma;
            set
            {
                if (_wybranaFirma != value)
                {
                    _wybranaFirma = value;
                    OnPropertyChanged(nameof(WybranaFirma));
                    OnPropertyChanged(nameof(WybranaFirmaName));
                    OnPropertyChanged(nameof(WybranaFirmaId));
                    // ✅ NOWE: Odśwież walidację widoczności Border_Pacjent
                    OnPropertyChanged(nameof(CzyWybranoFirme));
                }
            }
        }

        public bool CzyWybranoFirme => WybranaFirma != null && WybranaFirma.Id > 0;
        public string? WybranaFirmaName => WybranaFirma?.Name;
        public int? WybranaFirmaId => WybranaFirma?.Id;

        public ICommand ?SzukajFirmyCommand { get; }
        public ICommand ?AnulujCommand { get; }

        // ✅ DODANE: Command czyszczący pole wyszukiwania firmy
        public ICommand ?ClearSearchCommand { get; }
        
        // ✅ NOWE: Komendy dla odświeżania i dodawania firmy
        public ICommand ?RefreshCommand { get; }
        public ICommand ?NowaFirmaDialogCommand { get; }

        private void CloseDialog()
        {
            // Zamknij okno dialogowe PatientAdd
        }

        // ✅ DODANE: Metoda czyszcząca pole wyszukiwania firmy
        private void ClearSearch()
        {
            FrazaFirma = string.Empty;
        }

        private void ZapiszNowegoPacjenta()
        {
            // ══════════════════════════════════════════════════
            // KROK 1: Walidacja wymaganych pól
            // ══════════════════════════════════════════════════
            if (string.IsNullOrWhiteSpace(Imie))
            {
                MessageBox.Show("Pole Imię jest wymagane.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(Nazwisko))
            {
                MessageBox.Show("Pole Nazwisko jest wymagane.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(PESEL) && BrakPESEL != true)
            {
                MessageBox.Show("Podaj numer PESEL lub zaznacz checkbox 'Brak'.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dbHelper = new AccessDbHelper();
            var dbContext = new AccessDbContext();
            int firmaId = WybranaFirma?.Id ?? 0;

            // ══════════════════════════════════════════════════
            // KROK 2: Sprawdzenie duplikatu
            // ══════════════════════════════════════════════════
            int? istniejacyId = SzukajDuplikatuPacjenta(Imie!.Trim(), Nazwisko!.Trim(), firmaId, PESEL, BrakPESEL ?? false, ID);

            if (istniejacyId.HasValue)
            {
                var result = MessageBox.Show(
                    $"Znaleziono istniejącego pacjenta (ID: {istniejacyId.Value}):\n" +
                    $"{Imie?.Trim()} {Nazwisko?.Trim()} w wybranej firmie.\n\n" +
                    "Czy nadpisać dane istniejącego pacjenta?",
                    "Duplikat pacjenta",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    dbContext.UpdatePatient(
                        istniejacyId.Value,
                        PESEL, BrakPESEL ?? false, Plec, Imie, Nazwisko,
                        KodPocztowy, UlicaNumerDomu, Miejscowosc, Zawod,
                        WybranaFirma?.Id, "Polska", DataUrodzenia, "Polskie", "", "",
                        WybranaFirma?.Name);
                    ID = istniejacyId.Value;
                    RequestClose?.Invoke();
                }
                return;
            }

            // ══════════════════════════════════════════════════
            // KROK 3: Zapis (nowy lub edycja)
            // ══════════════════════════════════════════════════
            if (ID.HasValue && ID.Value > 0)
            {
                dbContext.UpdatePatient(
                    ID.Value,
                    PESEL, BrakPESEL ?? false, Plec, Imie, Nazwisko,
                    KodPocztowy, UlicaNumerDomu, Miejscowosc, Zawod,
                    WybranaFirma?.Id, "Polska", DataUrodzenia, "Polskie", "", "",
                    WybranaFirma?.Name);
            }
            else
            {
                int newId = dbContext.AddPatientAndGetId(
                    pesel: PESEL, brakPesel: BrakPESEL ?? false, plec: Plec,
                    imie: Imie, nazwisko: Nazwisko, adresKod: KodPocztowy,
                    adresUlica: UlicaNumerDomu, adresMiasto: Miejscowosc,
                    zawod: Zawod, firmaId: WybranaFirma?.Id, kraj: "Polska",
                    dataUrodzenia: DataUrodzenia, obywatelstwo: "Polskie",
                    telefon: "", email: "", firma: WybranaFirma?.Name);
                ID = newId;
            }

            RequestClose?.Invoke();
        }

        /// <summary>
        /// Szuka duplikatu pacjenta w bazie.
        /// Porównuje: imię + nazwisko + firma + (PESEL lub BrakPESEL).
        /// Przy edycji (excludeId != null) pomija własny rekord.
        /// Zwraca P_ID istniejącego duplikatu lub null.
        /// </summary>
        private int? SzukajDuplikatuPacjenta(string imie, string nazwisko, int firmaId, string? pesel, bool brakPesel, int? excludeId)
        {
            try
            {
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                if (!brakPesel && !string.IsNullOrWhiteSpace(pesel))
                {
                    // Nowy pacjent MA PESEL → duplikatem jest rekord z tym samym PESEL
                    // LUB rekord bez PESEL (import — P_pesel pusty/NULL/BrakPesel=true)
                    cmd.CommandText = @"
                        SELECT P_ID FROM P_Pacjent
                        WHERE TRIM(UCASE(P_imie)) = TRIM(UCASE(?))
                          AND TRIM(UCASE(P_nazwisko)) = TRIM(UCASE(?))
                          AND P_Firma_id = ?
                          AND (TRIM(P_pesel) = TRIM(?) OR P_pesel IS NULL OR TRIM(P_pesel) = '' OR P_brak = True)
                          AND P_activ = True";
                    var p1 = cmd.CreateParameter(); p1.Value = imie; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.Value = nazwisko; cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter(); p3.Value = firmaId; cmd.Parameters.Add(p3);
                    var p4 = cmd.CreateParameter(); p4.Value = pesel; cmd.Parameters.Add(p4);
                }
                else
                {
                    // BrakPESEL=true → szukaj po imię + nazwisko + firma
                    // (istniejący rekord też może nie mieć PESEL)
                    cmd.CommandText = @"
                        SELECT TOP 1 P_ID FROM P_Pacjent
                        WHERE TRIM(UCASE(P_imie)) = TRIM(UCASE(?))
                          AND TRIM(UCASE(P_nazwisko)) = TRIM(UCASE(?))
                          AND P_Firma_id = ?
                          AND P_activ = True";
                    var p1 = cmd.CreateParameter(); p1.Value = imie; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.Value = nazwisko; cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter(); p3.Value = firmaId; cmd.Parameters.Add(p3);
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var foundId = Convert.ToInt32(reader["P_ID"]);
                    // Przy edycji pomijamy własny rekord
                    if (excludeId.HasValue && foundId == excludeId.Value)
                        continue;
                    return foundId;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[SzukajDuplikatuPacjenta] Błąd: {ex}");
            }
            return null;
        }

        private void LoadFirmyFromDb()
        {
            var db = new AccessDbHelper();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT id,Nazwa,NIP,Ulica FROM Firma WHERE activ = True";
                    using (var reader = cmd.ExecuteReader())
                    {
                        Firmy.Clear();
                        while (reader.Read())
                        {
                            Firmy.Add(new FirmaDto
                            {
                                Id = reader["id"] is DBNull ? 0 : Convert.ToInt32(reader["id"]),
                                Name = reader["Nazwa"].ToString(),
                                NIP = reader["NIP"].ToString(),
                                Address = reader["Ulica"].ToString()
                            });
                        }
                    }
                }
            }
            FiltrujFirmy();
        }

        /// <summary>
        /// ✅ NOWA METODA: Odświeża listę firm z bazy danych
        /// </summary>
        private void RefreshFirmy()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("🔄 RefreshFirmy: Odświeżanie listy firm...");
                
                // Załaduj firmy ponownie z bazy
                LoadFirmyFromDb();
                
                NotificationHelper.ShowSuccess($"Odświeżono listę firm: {Firmy.Count} pozycji");
                // System.Diagnostics.Debug.WriteLine($"✅ Odświeżono {Firmy.Count} firm");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd odświeżania listy firm: {ex.Message}");
                NotificationHelper.ShowError($"Błąd odświeżania: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Otwiera dialog FirmaEditWindow do dodania nowej firmy
        /// </summary>
        private void OtworzDialogNowaFirma()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("➕ OtworzDialogNowaFirma: Otwieranie dialogu nowej firmy...");
                
                // ✅ Utwórz dialog dodawania nowej firmy
                var dialogWindow = new ASMED.WPF.Views.FirmaEditWindow
                {
                    Owner = System.Windows.Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                
                // ✅ Pobierz ViewModel z dialogu (aby później sprawdzić czy zapisano)
                var dialogViewModel = dialogWindow.DataContext as FirmaEditViewModel;
                
                // ✅ Otwórz dialog modalnie (czeka na zamknięcie)
                bool? result = dialogWindow.ShowDialog();
                
                // System.Diagnostics.Debug.WriteLine($"📋 Dialog zamknięty: result={result}");
                
                // ✅ KLUCZOWE: Odśwież listę firm po zamknięciu dialogu
                RefreshFirmy();
                
                // ✅ Sprawdź czy dodano nową firmę (ViewModel powinien mieć właściwość SavedFirma)
                if (dialogViewModel?.SavedFirma != null)
                {
                    var nowaFirma = dialogViewModel.SavedFirma;
                    // System.Diagnostics.Debug.WriteLine($"✅ Dodano nową firmę: {nowaFirma.Nazwa} (ID={nowaFirma.id})");
                    
                    // ✅ Ustaw nazwę nowej firmy w polu wyszukiwania
                    FrazaFirma = nowaFirma.Nazwa;
                    
                    // ✅ Automatycznie wybierz nową firmę z listy
                    var firmaZListy = FiltrowaneFirmy.FirstOrDefault(f => f.Id == nowaFirma.id);
                    if (firmaZListy != null)
                    {
                        WybranaFirma = firmaZListy;
                        NotificationHelper.ShowSuccess($"Dodano firmę: {nowaFirma.Nazwa}");
                    }
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("⚠️ Dialog zamknięty bez zapisu nowej firmy");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd otwierania dialogu nowej firmy: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public class FirmaDto
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? NIP { get; set; }
            public string? Address { get; set; }
        }

        public ICommand ?UtworzSkierowanieCommand { get; }


        private void UtworzSkierowanie()
        {
            // ✅ KROK 1: WALIDACJA - Sprawdź czy pacjent ma otwarte karty badań
            if (ID.HasValue && ID.Value > 0)
            {
                try
                {
                    // System.Diagnostics.Debug.WriteLine($"🔍 UtworzSkierowanie: Sprawdzam otwarte karty dla P_ID={ID.Value}");

                    var otwarteKarty = SprawdzOtwarteKartyBadan(ID.Value);

                    if (otwarteKarty.Count > 0)
                    {
                        // ✅ KROK 1A: Pacjent ma otwarte karty → Pokaż dialog wyboru
                        // System.Diagnostics.Debug.WriteLine($"⚠️ Znaleziono {otwarteKarty.Count} otwartych kart badań");

                        var dialogVM = new ViewModels.Dialogs.OtwarteKartyBadanDialogViewModel(
                            imieNazwisko: $"{Imie} {Nazwisko}",
                            pesel: PESEL ?? "",
                            firma: WybranaFirma?.Name ?? "",
                            otwarteKarty: otwarteKarty
                        );

                        var dialog = new Views.Dialogs.OtwarteKartyBadanDialog(dialogVM)
                        {
                            Owner = Application.Current.MainWindow
                        };

                        bool? result = dialog.ShowDialog();

                        if (result == true)
                        {
                            if (dialogVM.Result == ViewModels.Dialogs.OtwarteKartyBadanDialogViewModel.DialogResult.EdytujKarte)
                            {
                                // ✅ Edytuj wybraną kartę
                                if (dialogVM.WybraneB_ID.HasValue)
                                {
                                    EdytujIstniejacaKarte(ID.Value, dialogVM.WybraneB_ID.Value);
                                }
                            }
                            else if (dialogVM.Result == ViewModels.Dialogs.OtwarteKartyBadanDialogViewModel.DialogResult.NowaKarta)
                            {
                                // ✅ Utwórz nową kartę (poniżej)
                                UtworzNowaKarteSkierowania();
                            }
                        }
                        return; // ✅ WAŻNE: Przerwij dalsze wykonanie
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ Błąd sprawdzania otwartych kart: {ex.Message}");
                    // Kontynuuj normalnie jeśli błąd walidacji
                }
            }

            // ✅ KROK 2: Brak otwartych kart → Utwórz nową kartę
            UtworzNowaKarteSkierowania();
        }

        /// <summary>
        /// ✅ NOWA METODA: Tworzy nową kartę skierowania (wyodrębniona logika z UtworzSkierowanie)
        /// </summary>
        private void UtworzNowaKarteSkierowania()
        {
            // ✅ Pobierz pełne dane firmy z bazy (z kodem pocztowym, miastem, ulicą)
            string firmaKod = "";
            string firmaMiasto = "";
            string firmaUlica = "";

            if (WybranaFirma?.Id > 0)
            {
                try
                {
                    var db = new AccessDbHelper();
                    using (var conn = db.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT Kod, Miejscowosc, Ulica FROM Firma WHERE id = ?";
                            var p = cmd.CreateParameter();
                            p.Value = WybranaFirma.Id;
                            cmd.Parameters.Add(p);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    firmaKod = reader["Kod"]?.ToString() ?? "";
                                    firmaMiasto = reader["Miejscowosc"]?.ToString() ?? "";
                                    firmaUlica = reader["Ulica"]?.ToString() ?? "";
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"UtworzNowaKarteSkierowania: Błąd pobierania danych firmy - {ex.Message}");
                }
            }

            // ✅ Przekaż parametry w POPRAWNEJ KOLEJNOŚCI
            var vm = new SkierNewPacjentaViewModel(
                Imie ?? "",                    // patientFirstName
                Nazwisko ?? "",                // patientLastName
                PESEL ?? "",                   // patientPesel
                Plec ?? "",                    // patientGender
                DataUrodzenia ?? DateTime.MinValue, // patientBirthDate
                Zawod ?? "",                   // patientJobTitle
                KodPocztowy ?? "",             // ✅ patientPostalCode (kod pacjenta)
                Miejscowosc ?? "",             // ✅ patientCity (miasto pacjenta)
                UlicaNumerDomu ?? "",          // ✅ patientStreet (ulica pacjenta)
                ID ?? 0,                       // patientId
                comments ?? "",                // uwagi
                WybranaFirma?.Id ?? 0,         // companyId
                WybranaFirma?.Name ?? "",      // companyName
                firmaKod,                      // ✅ companyPostalCode (kod firmy z bazy!)
                firmaMiasto,                   // ✅ companyCity (miasto firmy z bazy!)
                firmaUlica                     // ✅ companyStreet (ulica firmy z bazy!)
            );

            _mainWindowViewModel.NowaKartaBadanWidok = vm;
        }

        /// <summary>
        /// ✅ NOWA METODA: Sprawdza otwarte (niezamknięte) karty badań pacjenta
        /// </summary>
        private ObservableCollection<Models.OtwartaKartaBadanDto> SprawdzOtwarteKartyBadan(int pacjentId)
        {
            var karty = new ObservableCollection<Models.OtwartaKartaBadanDto>();

            try
            {
                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT
    B_Skierowania.B_ID,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    Rejestracja.R_Data,
    Rejestracja.R_Status
FROM
    B_Skierowania
    LEFT JOIN Rejestracja ON B_Skierowania.B_ID = Rejestracja.R_S_ID
WHERE
    B_Skierowania.B_Pacjent_ID = ?
    AND B_Skierowania.B_Badanie_ID IS NULL
ORDER BY
    B_Skierowania.B_DataSkierowania DESC";

                        cmd.Parameters.AddWithValue("@id", pacjentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var karta = new Models.OtwartaKartaBadanDto
                                {
                                    B_ID = reader["B_ID"] is int bid ? bid :
                                           int.TryParse(reader["B_ID"]?.ToString(), out var bid2) ? bid2 : 0,

                                    B_DataSkierowania = reader["B_DataSkierowania"] is DateTime ds ? ds :
                                                        DateTime.TryParse(reader["B_DataSkierowania"]?.ToString(), out var ds2) ? ds2 : (DateTime?)null,

                                    B_TypBadania = reader["B_TypBadania"]?.ToString() ?? "",

                                    R_Data = reader["R_Data"] is DateTime rd ? rd :
                                            DateTime.TryParse(reader["R_Data"]?.ToString(), out var rd2) ? rd2 : (DateTime?)null,

                                    R_Status = reader["R_Status"]?.ToString() ?? ""
                                };

                                karty.Add(karta);
                            }
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"✅ Znaleziono {karty.Count} otwartych kart dla P_ID={pacjentId}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd SprawdzOtwarteKartyBadan: {ex.Message}");
            }

            return karty;
        }

        /// <summary>
        /// ✅ NOWA METODA: Edytuje istniejącą kartę badań (B_ID)
        /// </summary>
        private void EdytujIstniejacaKarte(int pacjentId, int bId)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"✏️ Edytowanie karty: P_ID={pacjentId}, B_ID={bId}");

                // ✅ Pobierz pełne dane skierowania z bazy
                var db = new AccessDbContext();
                var full = db.GetSkierowanieById(bId);

                if (full == null)
                {
                    MessageBox.Show($"Nie znaleziono karty badań: B_ID={bId}",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ Mapuj dane do SkierNewPacjentaViewModel
                var vm = new SkierNewPacjentaViewModel();

                // Dane pacjenta
                vm.PatientFirstName = full.P_imie ?? string.Empty;
                vm.PatientLastName = full.P_nazwisko ?? string.Empty;
                vm.PatientPesel = full.P_pesel ?? string.Empty;
                vm.PatientGender = full.P_plec ?? string.Empty;
                vm.PatientBirthDate = full.P_data_urodzenia ?? DateTime.MinValue;
                vm.PatientJobTitle = full.P_zawod ?? string.Empty;
                vm.PatientPostalCode = full.P_Ades_kod ?? string.Empty;
                vm.PatientCity = full.P_Ades_miasto ?? string.Empty;
                vm.PatientStreet = full.P_Adres_ulica_numer ?? string.Empty;
                vm.PatientId = full.P_ID ?? 0;
                vm.Uwagi = full.P_Uwagi ?? string.Empty;

                // Dane firmy
                vm.CompanyId = full.Firma_id ?? 0;
                vm.CompanyName = full.Firma_Nazwa ?? string.Empty;
                vm.CompanyPostalCode = full.Firma_Kod ?? string.Empty;
                vm.CompanyCity = full.Firma_Miejscowosc ?? string.Empty;
                vm.CompanyStreet = full.Firma_Ulica ?? string.Empty;

                // Dane skierowania (karty badań)
                vm.PatientSkierowanieId = full.B_ID ?? 0;
                vm.ReferralDate = full.B_DataSkierowania;
                vm.TestType = full.B_TypBadania ?? string.Empty;
                vm.JobTitle = full.B_Stanowisko ?? string.Empty;

                // Czynniki szkodliwe
                vm.IsPhysical = full.B_czynnik_fizyczny ?? false;
                vm.PhysicalDescription = full.B_czynnik_fizyczny_opis ?? string.Empty;
                vm.IsDust = full.B_czynnik_pylowy ?? false;
                vm.DustDescription = full.B_czynnik_pylowy_opis ?? string.Empty;
                vm.IsChemical = full.B_czynnik_chemiczny ?? false;
                vm.ChemicalDescription = full.B_czynnik_chemiczny_opis ?? string.Empty;
                vm.IsBiological = full.B_czynnik_biologiczny ?? false;
                vm.BiologicalDescription = full.B_czynnik_biologiczny_opis ?? string.Empty;
                vm.IsOther = full.B_czynnik_inny ?? false;
                vm.OtherDescription = full.B_czynnik_inny_opis ?? string.Empty;

                // Dokumenty
                vm.IsCertificate = full.B_Zaswiadczenie ?? false;
                vm.IsbookletSanepid = full.B_książeczka ?? false;
                vm.IsAnkieta = full.B_Ankieta ?? false;
                vm.IsNew = full.B_Nowe ?? true;

                // ✅ RadioButtony dla typu badania
                vm.IsGroupW = vm.TestType == "W";
                vm.IsGroupO = vm.TestType == "O";
                vm.IsGroupK = vm.TestType == "K";

                // ✅ Wydruki dostępne (karta już istnieje)
                vm.WydrukiVisibility = Visibility.Visible;
                vm.EditButtonVisibility = Visibility.Hidden;

                // ✅ Odśwież datę rejestracji z bazy
                vm.UpdateRejestrcjaDataFromDb();

                // ✅ Ustaw widok w MainViewModel
                _mainWindowViewModel.NowaKartaBadanWidok = vm;

                // System.Diagnostics.Debug.WriteLine($"✅ Otwarto edycję karty: B_ID={bId}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd EdytujIstniejacaKarte: {ex.Message}");
                MessageBox.Show($"Błąd edycji karty badań:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
