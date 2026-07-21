using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace ASMED.WPF.ViewModels
{
    // ViewModel do dodawania/edycji pacjenta
    // Powiązany z PatientAddView.xaml
    // Obsługuje pola pacjenta oraz wybór firmy z listy
    // Implementuje INotifyPropertyChanged do powiadamiania widoku o zmianach właściwości
    // Używa RelayCommand do obsługi komend (Szukaj, Zapisz, Anuluj)
    // Ładuje listę firm z bazy danych przy inicjalizacji
    // Obsługuje filtrowanie firm na podstawie tekstu wyszukiwania
    // Zawiera metody do zapisywania lub anulowania operacji
    // Obsługuje powiązania danych z widokiem (TwoWay Binding)
    // Obsługuje walidację danych (np. wymagane pola)
    // ----------------------------------------------------------------
    // Pcjent
    // ----------------------------------------------------------------
    public class PatientAddViewModel : INotifyPropertyChanged
    {
        // Pesel, BrakPesel, Plec, DataUrodzenia, Imie, Nazwisko, Zawod, UlicaNumerDomu, comments, KodPocztowy, Miejscowosc, WybranaFirma
        public event Action? RequestClose;
        // Pacjent
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

            // Wylicz datę urodzenia
            int rok = int.Parse(pesel.Substring(0, 2));
            int miesiac = int.Parse(pesel.Substring(2, 2));
            int dzien = int.Parse(pesel.Substring(4, 2));

            int wiek = miesiac / 20;
            miesiac = miesiac % 20;
            int pelnyRok = 1900 + rok;
            if (wiek == 1) pelnyRok += 100; // 2000-2099
            if (wiek == 2) pelnyRok += 200; // 2100-2199
            if (wiek == 3) pelnyRok += 300; // 2200-2299
            if (wiek == 4) pelnyRok += 400; // 1800-1899

            try
            {
                DataUrodzenia = new DateTime(pelnyRok, miesiac, dzien);
            }
            catch
            {
                DataUrodzenia = null;
            }
            OnPropertyChanged(nameof(DataUrodzenia));

            // Wylicz płeć
            int plecCyfra = int.Parse(pesel.Substring(9, 1));
            Plec = (plecCyfra % 2 == 1) ? "M" : "K";
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
        //public string? Miejscowosc { get; set; }
        public ICommand ?ClearMiejscowoscCommand { get; }
        //public string? WybranaFirma { get; set; }
        public ObservableCollection<FirmaDto> FiltrowaneFirmy { get; set; } = new();
        //public FirmaDto? WybranaFirma { get; set; }
        public ICommand ?SaveCommand { get; }

        public int? P_firma_id { get; set; } // id firmy (Firma.id)
        public string? comments { get; set; }
        // Domyślna wartość dla pola Miejscowosc w oknie dialogowym

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

        // -----------------------------------------------------------------
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
                }
            }
        }
        public string? WybranaFirmaName => WybranaFirma?.Name;
        public int? WybranaFirmaId => WybranaFirma?.Id;

        public ICommand ?SzukajFirmyCommand { get; }
        //public ICommand SaveCommand { get; }
        public ICommand ?AnulujCommand { get; }

        // ✅ DODANE: Command czyszczący pole wyszukiwania firmy
        public ICommand ?ClearSearchCommand { get; }

        public PatientAddViewModel()
        {
            // SzukajFirmyCommand = new RelayCommand(_ => FilterFirmy());
            SaveCommand = new RelayCommand(_ => ZapiszNowegoPacjenta());
            AnulujCommand = new RelayCommand(_ => CloseDialog());
            ClearSearchCommand = new RelayCommand(_ => ClearSearch()); // ✅ DODANE
            LoadFirmyFromDb();
        }

        private void CloseDialog()
        {
            // Zamknij okno dialogowe PatientAdd
            var win = System.Windows.Application.Current.Windows
                .OfType<ASMED.WPF.PatientAdd>()
                .FirstOrDefault(w => w.DataContext == this);
            win?.Close();
        }

        // ✅ DODANE: Metoda czyszcząca pole wyszukiwania firmy
        private void ClearSearch()
        {
            FrazaFirma = string.Empty;
        }

        // -------------------------------------------------

        // (string pesel, bool brakPesel, string plec, string imie, string nazwisko, string adresKod, string adresUlica, string adresMiasto, string zawod, string firma, string kraj, DateTime? dataUrodzenia, string obywatelstwo, string telefon, string email)
        private void ZapiszNowegoPacjenta()
        {
            var dbHelper = new Helpers.AccessDbHelper();
            //   var dbContext = new Helpers.AccessDbContext(dbHelper.GetConnection().ConnectionString);
            var dbContext = new Helpers.AccessDbContext();

            if (ID.HasValue && ID.Value > 0)
            {
                // Edytuj istniejącego pacjenta
                // (int id, string pesel, bool brakPesel, string plec, string imie, string nazwisko, string adresKod, string adresUlica, string adresMiasto, string zawod, int? firmaId, string kraj, DateTime? dataUrodzenia, string obywatelstwo, string telefon, string email,string firma)
                //
                dbContext.UpdatePatient(
                    ID.Value,
                    PESEL,
                    BrakPESEL ?? false,
                    Plec,
                    Imie,
                    Nazwisko,
                    KodPocztowy,
                    UlicaNumerDomu,
                    Miejscowosc,
                    Zawod,
                    WybranaFirma?.Id,
                    "Polska",
                    DataUrodzenia,
                    "Polskie",
                    "",
                    "",
                    WybranaFirma?.Name
                );
            }
            else
            {
                // Dodaj nowego pacjenta i pobierz nowy ID
                // (string pesel, bool brakPesel, string plec, string imie, string nazwisko, string adresKod, string adresUlica, string adresMiasto, string zawod, int? firmaId, string kraj, DateTime? dataUrodzenia, string obywatelstwo, string telefon, string email,string firma)
                //
                int newId = dbContext.AddPatientAndGetId(
                    PESEL,
                    BrakPESEL ?? false,
                    Plec,
                    Imie,
                    Nazwisko,
                    KodPocztowy,
                    UlicaNumerDomu,
                    Miejscowosc,
                    Zawod,
                    WybranaFirma?.Id,
                    "Polska",
                    DataUrodzenia,
                    "Polskie",
                    "",
                    "",
                    WybranaFirma?.Name

                );
                ID = newId;
            }

            // Zamknij okno dialogowe po zapisie
            RequestClose?.Invoke();
        }


        // -----------------------------------------------------------------
        // Firma select
        // -----------------------------------------------------------------
        private void LoadFirmyFromDb()
        {
            var db = new Helpers.AccessDbHelper();
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



        // Komenda czyszcząca pole Miejscowosc
        private void ClearMiejscowosc()
        {
            //  Miejscowosc = string.Empty;
            //  OnPropertyChanged(nameof(Miejscowosc));
        }

        private void Zapisz()
        {
            // TODO: Insert lub Update pacjenta do bazy
            // P_firma = WybranaFirma?.Id
        }

        private void Anuluj()
        {
            // Zamknij okno dialogowe po anulowaniu
            RequestClose?.Invoke();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class FirmaDto
    {

        public int Id { get; set; }
        public string? Name { get; set; }
        public string? NIP { get; set; }
        public string? Address { get; set; }
        public string? Nazwa { get; internal set; }
        public bool Activ { get; internal set; }
        public string? Cennik { get; internal set; }
        public string? FkEmail { get; internal set; }
        public object Value { get; internal set; }
        // Dodaj inne pola według potrzeb
    }

}
