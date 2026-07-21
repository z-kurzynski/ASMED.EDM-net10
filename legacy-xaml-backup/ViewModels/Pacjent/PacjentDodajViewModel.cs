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

namespace ASMED.WPF.ViewModels
{
    public class PacjentDodajViewModel : INotifyPropertyChanged
    {

        private MainWindowViewModel _mainWindowViewModel;
        public ICommand ?PowrotDoListyCommand { get; set; }

        public PacjentDodajViewModel(MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
            PowrotDoListyCommand = new RelayCommand(_ => PowrotDoListy());

            // Inicjalizacja pozostałych komend i danych
            SaveCommand = new RelayCommand(_ => ZapiszNowegoPacjenta());
            AnulujCommand = new RelayCommand(_ => CloseDialog());
            ClearSearchCommand = new RelayCommand(_ => ClearSearch()); // ✅ DODANE
            LoadFirmyFromDb();
            LoadImionaFromDb();
            LoadNazwiskaFromDb();
            LoadUliceFromDb();
            LoadZawodyFromDb();
            LoadMiastaFromDb();
        }

        public PacjentDodajViewModel()
        {
        }

        private void PowrotDoListy()
        {
            _mainWindowViewModel.PacjentWidok = new ListaPacjentowViewModel();
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
            var db = new Helpers.AccessDbHelper();
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
            var db = new Helpers.AccessDbHelper();
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

        // ------------------ULICA S_Ulice------------------------
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
            var db = new Helpers.AccessDbHelper();
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
            var db = new Helpers.AccessDbHelper();
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
            var db = new Helpers.AccessDbHelper();
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
        public ICommand ?ClearMiejscowoscCommand { get; }
        public ObservableCollection<FirmaDto> FiltrowaneFirmy { get; set; } = new();
        public ICommand ?SaveCommand { get; }
        public int? P_firma_id { get; set; }
        public string? comments { get; set; }
        private string?_miejscowosc = "Warszawa";
        public string?Miejscowosc
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
                }
            }
        }
        public string? WybranaFirmaName => WybranaFirma?.Name;
        public int? WybranaFirmaId => WybranaFirma?.Id;

        public ICommand ?SzukajFirmyCommand { get; }
        public ICommand ?AnulujCommand { get; }
        public ICommand ?ClearSearchCommand { get; } // ✅ DODANE: Command czyszczący pole wyszukiwania firmy

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
            var dbHelper = new Helpers.AccessDbHelper();
            var dbContext = new Helpers.AccessDbContext();

            if (ID.HasValue && ID.Value > 0)
            {
                dbContext.UpdatePatient(ID.Value,
                                        pesel: PESEL,
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
                                        WybranaFirma?.Name);
            }
            else
            {
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

            RequestClose?.Invoke();
        }

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
    }
}
