using ASMED.WPF;
using ASMED.WPF.Helpers;
using ASMED.WPF.Models;
using ASMED.WPF.Services;
using ASMED.WPF.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data; // <- dodane
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels.ListaDoFaktur
{
    public partial class ListaFaktAddViewModel : INotifyPropertyChanged
    {
        private readonly AccessDbContext _db = new AccessDbContext();

        public event Action? RequestResetSelectionState;

        // --- Price fields ---
        private decimal _priceBasic = 0m;
        private decimal _priceLaryngologist = 0m;
        private decimal _priceOphthalmologist = 0m;
        private decimal _priceSanitary = 0m;
        private decimal _priceLipidogram = 0m;
        private decimal _priceEKG = 0m;
        private decimal _priceHealthClinic = 0m;
        private decimal _priceOther = 0m; // dodatkowe pole "Inne"

        public string PriceBasicText => FormatPrice(_priceBasic);
        public string PriceLaryngologistText => FormatPrice(_priceLaryngologist);
        public string PriceOphthalmologistText => FormatPrice(_priceOphthalmologist);
        public string PriceSanitaryText => FormatPrice(_priceSanitary);
        public string PriceLipidogramText => FormatPrice(_priceLipidogram);
        public string PriceEKGText => FormatPrice(_priceEKG);
        public string PriceHealthClinicText => FormatPrice(_priceHealthClinic);
        public string PriceOtherText => FormatPrice(_priceOther); // widoczna, jeśli potrzebna w UI

        private string FormatPrice(decimal v) => v.ToString("N2") + " zł";

        public void SetPriceFields(decimal? basic, decimal? laryngologist, decimal? ophthalmologist, decimal? sanitary, decimal? lipidogram, decimal? ekg, decimal? healthClinic, decimal? other)
        {
            _priceBasic = basic ?? 0m;
            _priceLaryngologist = laryngologist ?? 0m;
            _priceOphthalmologist = ophthalmologist ?? 0m;
            _priceSanitary = sanitary ?? 0m;
            _priceLipidogram = lipidogram ?? 0m;
            _priceEKG = ekg ?? 0m;
            _priceHealthClinic = healthClinic ?? 0m;
            _priceOther = other ?? 0m;
            NotifyPriceProperties();
        }

        private void NotifyPriceProperties()
        {
            OnPropertyChanged(nameof(PriceBasicText));
            OnPropertyChanged(nameof(PriceLaryngologistText));
            OnPropertyChanged(nameof(PriceOphthalmologistText));
            OnPropertyChanged(nameof(PriceSanitaryText));
            OnPropertyChanged(nameof(PriceLipidogramText));
            OnPropertyChanged(nameof(PriceEKGText));
            OnPropertyChanged(nameof(PriceHealthClinicText));
            OnPropertyChanged(nameof(PriceOtherText));

        }

        // -- Grid 0 Badanie fields --

        private string? _NumerFaktury;
        public string? NumerFaktury
        {
            get => _NumerFaktury;
            set { if (_NumerFaktury != value) { _NumerFaktury = value; OnPropertyChanged(); } }
        }

        private DateTime? _DataWystawienia;
        public DateTime? DataWystawienia
        {
            get => _DataWystawienia;
            set { if (_DataWystawienia != value) { _DataWystawienia = value; OnPropertyChanged(); } }
        }

        private string? _Uwagi;
        public string? Uwagi
        {
            get => _Uwagi;
            set { if (_Uwagi != value) { _Uwagi = value; OnPropertyChanged(); } }
        }

        private string? _FKemail;
        public string? FKemail
        {
            get => _FKemail;
            set { if (_FKemail != value) { _FKemail = value; OnPropertyChanged(); } }
        }

        private string? _SumaBrutto;
        public string? SumaBrutto
        {
            get => _SumaBrutto;
            set { if (_SumaBrutto != value) { _SumaBrutto = value; OnPropertyChanged(); } }
        }



        // --- Data/Result fields used by Grid_1_Badanie ---
        private DateTime? _dataBadania;
        public DateTime? DataBadania
        {
            get => _dataBadania;
            set
            {
                if (_dataBadania == value) return;
                _dataBadania = value;
                OnPropertyChanged();
                if (_dataBadania.HasValue)
                    DataWaznosci = _dataBadania.Value.AddYears(3);
            }
        }

        private DateTime? _dataWaznosci;
        public DateTime? DataWaznosci
        {
            get => _dataWaznosci;
            set { if (_dataWaznosci != value) { _dataWaznosci = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<string> WynikOptions { get; } = new ObservableCollection<string>();
        private string? _selectedWynik;
        public string? SelectedWynik
        {
            get => _selectedWynik;
            set { if (_selectedWynik != value) { _selectedWynik = value; OnPropertyChanged(); } }
        }

        private string? _nrKsiegi;
        public string? NrKsiegi
        {
            get => _nrKsiegi;
            set { if (_nrKsiegi != value) { _nrKsiegi = value; OnPropertyChanged(); } }
        }

        // Typ badania oraz pola imie/nazwisko (używane w Grid_1_Badanie)
        private string? _typBadania;
        public string? TypBadania
        {
            get => _typBadania;
            set
            {
                if (_typBadania == value) return;
                _typBadania = value;
                OnPropertyChanged();
            }
        }

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
                    ApplyAvailableFilter();
                }
            }
        }

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
                    ApplyAvailableFilter();
                }
            }
        }

        private string? _nazwisko;
        public string? Nazwisko
        {
            get => _nazwisko;
            private set { if (_nazwisko != value) { _nazwisko = value; OnPropertyChanged(nameof(Nazwisko)); } }
        }

        public string? Imie { get; private set; }

        // --- Konstruktor ---
        public ListaFaktAddViewModel(DateTime? dataBadania = null)
        {
            DataBadania = dataBadania ?? DateTime.Today;
            DataWaznosci = DataBadania?.AddYears(3);
            DataWystawienia = DateTime.Today;

            WynikOptions.Add("1 - Pozytywne");
            WynikOptions.Add("2 - Negatywne");
            SelectedWynik = WynikOptions.FirstOrDefault();

            _ = Task.Run(() => LoadImionaFromDb());
            _ = Task.Run(() => LoadNazwiskaFromDb());
            _ = Task.Run(() => LoadFirmyItemsFromDb());
            _ = Task.Run(() => LoadAvailableBadania());
            _ = Task.Run(() => LoadStatusOptionsFromDb()); // <-- dodane
        }

        // Dodany publiczny konstruktor bezparametrowy (potrzebny dla XAML)
        public ListaFaktAddViewModel() : this(null)
        {
        }

        // cennik --

        public ObservableCollection<string> StatusOptions { get; } = new();

        private void LoadStatusOptionsFromDb()
        {
            try
            {
                var items = new List<string>();
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                // SQL: pobierz aktywne cenniki
                cmd.CommandText = "SELECT b_Cennik FROM BAD_Cennik WHERE b_activ = TRUE ORDER BY b_Cennik";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["b_Cennik"] != DBNull.Value)
                        items.Add(reader["b_Cennik"].ToString() ?? string.Empty);
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    StatusOptions.Clear();
                    foreach (var s in items)
                    {
                        if (!string.IsNullOrWhiteSpace(s) && !StatusOptions.Contains(s))
                            StatusOptions.Add(s);
                    }

                    // jeśli nie wybrano niczego – ustaw domyślny SelectedCennik
                    if (string.IsNullOrEmpty(SelectedCennik) && StatusOptions.Count > 0)
                        SelectedCennik = StatusOptions[0];
                }));
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadStatusOptionsFromDb failed: {ex}");
            }
        }


        // --- Cennik (wybór) ---
        private string? _selectedCennik;
        public string? SelectedCennik
        {
            get => _selectedCennik;
            set
            {
                if (_selectedCennik == value) return;
                _selectedCennik = value;
                OnPropertyChanged();
                LoadPricesForSelectedCennik();
            }
        }

        private void LoadPricesForSelectedCennik()
        {
            _priceBasic = 0m; _priceLaryngologist = 0m; _priceOphthalmologist = 0m; _priceSanitary = 0m;
            _priceLipidogram = 0m; _priceEKG = 0m; _priceHealthClinic = 0m;

            if (string.IsNullOrEmpty(SelectedCennik))
            {
                NotifyPriceProperties();
                return;
            }

            try
            {
                var repo = new WizytyRepository();
                var prices = repo.GetCennikPrices(SelectedCennik);
                if (prices != null)
                {
                    foreach (var kv in prices)
                    {
                        var name = (kv.Key ?? string.Empty).Trim().ToLowerInvariant();
                        var price = kv.Value;
                        if (name.Contains("lekasz") || name.Contains("lekarz") || name.Contains("basic")) _priceBasic = price;
                        else if (name.Contains("laryngolog")) _priceLaryngologist = price;
                        else if (name.Contains("okulista") || name.Contains("okulist")) _priceOphthalmologist = price;
                        else if (name.Contains("ksi") || name.Contains("książeczka") || name.Contains("ksiazeczka")) _priceSanitary = price;
                        else if (name.Contains("lipidogram")) _priceLipidogram = price;
                        else if (name.Contains("ekg")) _priceEKG = price;
                        else if (name.Contains("urlop")) _priceHealthClinic = price;
                    }
                }
            }
            catch { }

            NotifyPriceProperties();
        }

        // ---------------- FIRMA -----------------------------
        public ObservableCollection<string> FirmyItems { get; } = new();
        private readonly List<string> _allFirmy = new();
        private readonly List<FirmaDto> _allFirmaDtos = new();

        private string? _wybranaFirma;
        public string? WybranaFirma
        {
            get => _wybranaFirma;
            set
            {
                if (_wybranaFirma == value) return;
                _wybranaFirma = value;
                OnPropertyChanged();
                FilterFirmyItems(_wybranaFirma);

                if (string.IsNullOrWhiteSpace(_wybranaFirma))
                {
                    SelectedFirmaDto = null;
                    FKemail = null; // wyczyść pole e-mail
                    OnPropertyChanged(nameof(FKemail));
                }
                else
                {
                    var m = _allFirmaDtos.FirstOrDefault(f =>
                        string.Equals(f.Nazwa?.Trim(), _wybranaFirma.Trim(), StringComparison.OrdinalIgnoreCase));
                    SelectedFirmaDto = m;

                    // Ustaw FKemail w VM gdy mamy DTO firmy
                    FKemail = SelectedFirmaDto?.FkEmail ?? null;
                    OnPropertyChanged(nameof(FKemail));
                }

                ApplyAvailableFilter();
            }
        }

        private FirmaDto? _selectedFirmaDto;
        public FirmaDto? SelectedFirmaDto
        {
            get => _selectedFirmaDto;
            private set { if (_selectedFirmaDto != value) { _selectedFirmaDto = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedFirmaId)); } }
        }

        public int? SelectedFirmaId => SelectedFirmaDto?.Id;

        public void SetSelectedFirma(FirmaDto? dto)
        {
            if (dto == null)
            {
                SelectedFirmaDto = null;
                WybranaFirma = null;
                FKemail = null;
                OnPropertyChanged(nameof(FKemail));
                return;
            }

            SelectedFirmaDto = dto;
            WybranaFirma = dto.Nazwa;

            // Ustaw email po wyborze firmy
            FKemail = dto.FkEmail ?? null;
            OnPropertyChanged(nameof(FKemail));
        }

        // Dodano publiczną metodę pomocniczą, która przyjmuje proste wartości z dialogu i ustawia SelectedFirmaDto.
        public void SetSelectedFirmaByValues(int? id, string? nazwa)
        {
            // jeśli brak danych -> wyczyść wybór
            if (!id.HasValue && string.IsNullOrWhiteSpace(nazwa))
            {
                SelectedFirmaDto = null;
                WybranaFirma = null;
                FKemail = null;
                OnPropertyChanged(nameof(FKemail));
                return;
            }

            // jeśli mamy już załadowane dto firm, spróbuj dopasować po id lub nazwie
            FirmaDto? found = null;
            if (id.HasValue)
                found = _allFirmaDtos.FirstOrDefault(f => f.Id == id.Value);

            if (found == null && !string.IsNullOrWhiteSpace(nazwa))
                found = _allFirmaDtos.FirstOrDefault(f => string.Equals(f.Nazwa?.Trim(), nazwa?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (found != null)
            {
                SelectedFirmaDto = found;
                WybranaFirma = found.Nazwa;

                // ustaw email z odnalezionego DTO
                FKemail = found.FkEmail ?? null;
                OnPropertyChanged(nameof(FKemail));
                return;
            }

            // utwórz tymczasowy dto (wirtualny), dodaj do listy pomocniczej i ustaw jako wybrany
            var newDto = new FirmaDto
            {
                Id = id ?? 0,
                Activ = true,
                Nazwa = nazwa,
                NIP = null
            };

            _allFirmaDtos.Add(newDto);
            SelectedFirmaDto = newDto;
            WybranaFirma = nazwa;

            // brak email w danych -> wyczyść pole
            FKemail = newDto.FkEmail ?? null;
            OnPropertyChanged(nameof(FKemail));
        }

        private void LoadFirmyItemsFromDb()
        {
            // raportuj rozpoczęcie – bezpieczne przy braku splasha
            try { SplashService.Instance.SetStatus("Ładowania: firmy"); } catch { }
            try
            {
                var items = new List<string>();
                var dtos = new List<FirmaDto>();
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                // Dołączamy pole cennik z tabeli Firma
                cmd.CommandText = "SELECT id, Nazwa, activ, NIP, cennik, FKemail FROM Firma WHERE activ = TRUE ORDER BY Nazwa";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var name = reader["Nazwa"] != DBNull.Value ? reader["Nazwa"].ToString() ?? string.Empty : string.Empty;
                    var id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0;
                    var activ = reader["activ"] != DBNull.Value ? Convert.ToBoolean(reader["activ"]) : false;
                    var nip = reader["NIP"] != DBNull.Value ? reader["NIP"].ToString() : null;
                    var email = reader["FKemail"] != DBNull.Value ? reader["FKemail"].ToString() : null;

                    // Trimujemy i normalizujemy pole cennik — usuń białe znaki i traktuj puste jako null
                    string? cennik = null;
                    if (reader["cennik"] != DBNull.Value)
                    {
                        var raw = reader["cennik"].ToString();
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            cennik = raw.Trim();
                            if (string.IsNullOrWhiteSpace(cennik))
                                cennik = null;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        items.Add(name);
                        dtos.Add(new FirmaDto { Id = id, Activ = activ, Nazwa = name, NIP = nip, Cennik = cennik, FkEmail = email });
                    }
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _allFirmy.Clear();
                    _allFirmy.AddRange(items);

                    _allFirmaDtos.Clear();
                    _allFirmaDtos.AddRange(dtos);

                    FirmyItems.Clear();
                    foreach (var s in _allFirmy)
                        if (!string.IsNullOrWhiteSpace(s) && !FirmyItems.Contains(s))
                            FirmyItems.Add(s);
                }));
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadFirmyItemsFromDb failed: {ex}");
            }
        }

        private void FilterFirmyItems(string? query)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    FirmyItems.Clear();
                    if (string.IsNullOrWhiteSpace(query))
                    {
                        foreach (var s in _allFirmy) FirmyItems.Add(s);
                        return;
                    }

                    var ci = CultureInfo.CurrentCulture.CompareInfo;
                    var opts = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

                    foreach (var s in _allFirmy)
                    {
                        if (string.IsNullOrWhiteSpace(s)) continue;
                        if (ci.IndexOf(s, query.Trim(), opts) >= 0)
                        {
                            if (!FirmyItems.Contains(s)) FirmyItems.Add(s);
                        }
                    }
                }));
            }
            // Jeśli dispatcher jest null, wykonaj bezpośrednio
            else
            {
                FirmyItems.Clear();
                if (string.IsNullOrWhiteSpace(query))
                {
                    foreach (var s in _allFirmy) FirmyItems.Add(s);
                    return;
                }

                var ci = CultureInfo.CurrentCulture.CompareInfo;
                var opts = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

                foreach (var s in _allFirmy)
                {
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    if (ci.IndexOf(s, query.Trim(), opts) >= 0)
                    {
                        if (!FirmyItems.Contains(s)) FirmyItems.Add(s);
                    }
                }
            }
        }

        private void LoadImionaFromDb()
        {
            try
            {
                var results = new List<string>();
                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT P_imie FROM P_Pacjent GROUP BY P_imie";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["P_imie"] != DBNull.Value)
                                    results.Add(reader["P_imie"].ToString() ?? string.Empty);
                            }
                        }
                    }
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ImionaItems.Clear();
                    foreach (var i in results) ImionaItems.Add(i);
                }));
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadImionaFromDb failed: {ex}");
            }
        }

        private void LoadNazwiskaFromDb()
        {
            try
            {
                var results = new List<string>();
                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT P_nazwisko FROM P_Pacjent GROUP BY P_nazwisko ";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["P_nazwisko"] != DBNull.Value)
                                    results.Add(reader["P_nazwisko"].ToString() ?? string.Empty);
                            }
                        }
                    }
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    NazwiskaItems.Clear();
                    foreach (var n in results) NazwiskaItems.Add(n);
                }));
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadNazwiskaFromDb failed: {ex}");
            }
        }

        // ------------------- DODATKI DLA LISTY DOSTEPNYCH BADAN -------------------
        public ObservableCollection<AccessDbContext.AssignedBadanieDto> AvailableBadania { get; } = new();
        public ObservableCollection<AssignedBadanieWrapper> FilteredAvailableBadania { get; } = new();

        // --- Zastąp istniejącą definicję SelectedLista tym fragmentem (ustawianie etykiety przycisku) ---
        private AccessDbContext.ListyBadanDto _selectedLista = new AccessDbContext.ListyBadanDto { Badania = new System.Collections.ObjectModel.ObservableCollection<AccessDbContext.AssignedBadanieDto>() };
        public AccessDbContext.ListyBadanDto SelectedLista
        {
            get => _selectedLista;
            set
            {
                if (_selectedLista == value) return;
                _selectedLista = value ?? new AccessDbContext.ListyBadanDto { Badania = new System.Collections.ObjectModel.ObservableCollection<AccessDbContext.AssignedBadanieDto>() };
                OnPropertyChanged();
                RecalculateTotalRazem();

                // jeśli lista już ma Id -> zmień etykietę przycisku na "Modyfikuj"
                if (_selectedLista?.Identyfikator != null && _selectedLista.Identyfikator.HasValue)
                    SaveButtonLabel = "Modyfikuj listę";
                else
                    SaveButtonLabel = "Zapisz listę";
                OnPropertyChanged(nameof(SaveButtonLabel));
                OnPropertyChanged(nameof(WydrukiVisibility));
            }
        }

        private decimal _totalRazem = 0m;
        public decimal TotalRazem
        {
            get => _totalRazem;
            private set { if (_totalRazem != value) { _totalRazem = value; OnPropertyChanged(); } }
        }

        // ✅ NOWE: Licznik dostępnych badań w bazie (cache)
        private int _cachedAvailableCountInDb = 0;

        // ✅ NOWE: Liczba nowych badań dostępnych do załadowania
        private int _newBadaniaAvailableCount = 0;
        public int NewBadaniaAvailableCount
        {
            get => _newBadaniaAvailableCount;
            private set
            {
                if (_newBadaniaAvailableCount != value)
                {
                    _newBadaniaAvailableCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowRefreshBadaniaButton));
                    OnPropertyChanged(nameof(RefreshBadaniaButtonText));
                }
            }
        }

        // ✅ NOWE: Czy pokazać przycisk odświeżania
        public bool ShowRefreshBadaniaButton => NewBadaniaAvailableCount > 0;

        // ✅ NOWE: Tekst przycisku odświeżania
        public string RefreshBadaniaButtonText => $"🔄 Odśwież listę ({NewBadaniaAvailableCount} nowych)";

        // ✅ NOWE: Komenda odświeżania listy badań
        private RelayCommand<object>? _refreshAvailableBadaniaCommand;
        public ICommand ?RefreshAvailableBadaniaCommand => _refreshAvailableBadaniaCommand ??= new RelayCommand<object>(_ => RefreshAvailableBadania());

        private RelayCommand<object>? _addWithoutEditCommand;
        public ICommand ?AddWithoutEditCommand => _addWithoutEditCommand ??= new RelayCommand<object>(AddWithoutEdit);

        private AccessDbContext.AssignedBadanieDto? _selectedAssignedBadanie;
        public AccessDbContext.AssignedBadanieDto? SelectedAssignedBadanie
        {
            get => _selectedAssignedBadanie;
            set { if (_selectedAssignedBadanie != value) { _selectedAssignedBadanie = value; OnPropertyChanged(); PopulateGridFromSelected(); } }
        }

        private RelayCommand<object>? _saveSelectedAssignedBadanieCommand;
        public ICommand ?SaveSelectedAssignedBadanieCommand => _saveSelectedAssignedBadanieCommand ??= new RelayCommand<object>(_ => SaveSelectedAssignedBadanie());

        private RelayCommand<object>? _clearGridCommand;
        public ICommand ?ClearGridCommand => _clearGridCommand ??= new RelayCommand<object>(_ => ClearGridFields());

        private RelayCommand<object>? _deleteAssignedBadanieCommand;
        public ICommand ?DeleteAssignedBadanieCommand => _deleteAssignedBadanieCommand ??= new RelayCommand<object>(DeleteAssignedBadanie);

        private RelayCommand<object>? _markCommand;
        public ICommand ?MarkCommand => _markCommand ??= new RelayCommand<object>(_ => MarkAllFiltered());

        private RelayCommand<object>? _cancelSaveCommand;
        public ICommand ?CancelSaveCommand => _cancelSaveCommand ??= new RelayCommand<object>(_ => CancelAllAssignedToAvailable());

        // Dodaj poniższe pola i metody w klasie ListaFaktAddViewModel (np. obok pozostałych RelayCommandów).
        private RelayCommand<object>? _addSelectedCommand;
        public ICommand ?AddSelectedCommand => _addSelectedCommand ??= new RelayCommand<object>(_ => AddSelectedMarked());

        // NOWA IMPLEMENTACJA: polecenie NewCommand i metoda wykonujące "otwarcie nowej strony" (reset widoku)
        private RelayCommand<object>? _newCommand;
        public ICommand ?NewCommand => _newCommand ??= new RelayCommand<object>(_ => ExecuteNewList());

        // --- DODANO: polecenie wysyłki email ---
        private RelayCommand<object>? _sendEmailCommand;
        public ICommand ?SendEmailCommand => _sendEmailCommand ??= new RelayCommand<object>(_ => SendEmail());

        private void ExecuteNewList()
        {
            try
            {
                // Zresetuj SelectedLista do nowej, pustej listy
                SelectedLista = new AccessDbContext.ListyBadanDto
                {
                    Badania = new System.Collections.ObjectModel.ObservableCollection<AccessDbContext.AssignedBadanieDto>(),
                    Identyfikator = null,
                    Nazwa = null,
                    FK_Data = null,
                    FK_Numer = null,
                    FK_Kwota = null
                };

                // Wyczyść pola formularza / nagłówka faktury
                WybranaFirma = null;
                SelectedFirmaDto = null;
                NumerFaktury = null;
                DataWystawienia = DateTime.Today;
                SumaBrutto = null;
                Uwagi = null;

                // Wyczyść pola formularza badania
                ClearGridFields();

                // Zresetuj pola edytowalne cen
                EditablePriceBasic = EditablePriceLaryngologist = EditablePriceOphthalmologist =
                    EditablePriceSanitary = EditablePriceLipidogram = EditablePriceEKG =
                    EditablePriceHealthClinic = EditablePriceOther = string.Empty;

                // Zresetuj flagi i etykietę przycisku zapisu
                IsKsiazeczkaChecked = false;
                IsUrlopChecked = false;
                SaveButtonLabel = "Zapisz listę";

                // Odśwież dostępne badania i listę firm
                Task.Run(() => LoadAvailableBadania());
                Task.Run(() => LoadFirmyItemsFromDb());

                // Powiadom widok aby zresetował stan selekcji/wyświetlania
                RequestResetSelectionState?.Invoke();

                // Powiadomienia o zmianach właściwości
                OnPropertyChanged(nameof(SelectedLista));
                OnPropertyChanged(nameof(WybranaFirma));
                OnPropertyChanged(nameof(SelectedFirmaDto));
                OnPropertyChanged(nameof(NumerFaktury));
                OnPropertyChanged(nameof(DataWystawienia));
                OnPropertyChanged(nameof(SumaBrutto));
                OnPropertyChanged(nameof(Uwagi));
                OnPropertyChanged(nameof(SaveButtonLabel));
                OnPropertyChanged(nameof(WydrukiVisibility));
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ExecuteNewList error: {ex}");
                NotificationHelper.ShowError("Błąd podczas inicjowania nowej listy.");
            }
        }

        private void AddSelectedMarked()
        {
            try
            {
                // zbierz aktualnie zaznaczone wrappery -> DTO
                var markedDtos = FilteredAvailableBadania
                    .Where(w => w.IsMarked && w.Dto != null)
                    .Select(w => w.Dto!)
                    .ToList();

                if (markedDtos.Count == 0) return;

                // wykonaj na UI-thread
                if (Application.Current?.Dispatcher?.CheckAccess() == true)
                    AddMarkedDtosToSelected(markedDtos);
                else if (Application.Current?.Dispatcher != null)
                    Application.Current.Dispatcher.Invoke(() => AddMarkedDtosToSelected(markedDtos));
                // Jeśli Dispatcher jest null, wykonaj bezpośrednio (fallback)
                else
                    AddMarkedDtosToSelected(markedDtos);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"AddSelectedMarked error: {ex}");
            }
        }

        private void AddMarkedDtosToSelected(List<AccessDbContext.AssignedBadanieDto> markedDtos)
        {
            if (SelectedLista.Badania == null)
                SelectedLista.Badania = new System.Collections.ObjectModel.ObservableCollection<AccessDbContext.AssignedBadanieDto>();

            foreach (var dto in markedDtos)
            {
                // sprawdź czy już nie dodano (porównanie po Bad_ID jeśli dostępne)
                bool alreadyInSelected = dto.Bad_ID.HasValue
                    ? SelectedLista.Badania.Any(b => b.Bad_ID.HasValue && b.Bad_ID.Value == dto.Bad_ID.Value)
                    : SelectedLista.Badania.Any(b => ReferenceEquals(b, dto));

                if (alreadyInSelected) continue;

                // utwórz kopię i dodaj do SelectedLista
                var copy = new AccessDbContext.AssignedBadanieDto
                {
                    Bad_ID = dto.Bad_ID,
                    Bad_L_ID = dto.Bad_L_ID,
                    Bad_Data = dto.Bad_Data,
                    Bad_Typ = dto.Bad_Typ,
                    P_imie = dto.P_imie,
                    P_nazwisko = dto.P_nazwisko,
                    P_zawod = dto.P_zawod,
                    FirmaNazwa = dto.FirmaNazwa,
                    FirmaCennik = dto.FirmaCennik,
                    Bad_Razem = dto.Bad_Razem,
                    Bad_Cena1 = dto.Bad_Cena1,
                    Bad_Cena2 = dto.Bad_Cena2,
                    Bad_Cena3 = dto.Bad_Cena3,
                    Bad_Cena4 = dto.Bad_Cena4,
                    Bad_Cena5 = dto.Bad_Cena5,
                    Bad_Cena6 = dto.Bad_Cena6,
                    Bad_Cena7 = dto.Bad_Cena7,
                    Bad_Cena8 = dto.Bad_Cena8,
                    Bad_Cena9 = dto.Bad_Cena9,
                    Bad_Cena10 = dto.Bad_Cena10,
                    Bad_Data_Do = dto.Bad_Data_Do,
                    Bad_Wynik = dto.Bad_Wynik,
                    Bad_Nr_KS = dto.Bad_Nr_KS,
                    Bad_END = dto.Bad_END,
                    Bad_P_ID = dto.Bad_P_ID,
                    Bad_S_ID = dto.Bad_S_ID,
                    Bad_F_ID = dto.Bad_F_ID,
                    Bad_bn_cennik = dto.Bad_bn_cennik
                };

                SelectedLista.Badania.Add(copy);

                // usuń oryginał z AvailableBadania (porównanie po Bad_ID lub referencji)
                var toRemove = dto.Bad_ID.HasValue
                    ? AvailableBadania.FirstOrDefault(b => b.Bad_ID.HasValue && b.Bad_ID.Value == dto.Bad_ID.Value)
                    : AvailableBadania.FirstOrDefault(b => ReferenceEquals(b, dto));

                if (toRemove != null)
                    AvailableBadania.Remove(toRemove);

                // odznacz wrapper w FilteredAvailableBadania (jeśli istnieje) żeby checkbox zniknął
                var wrapper = FilteredAvailableBadania.FirstOrDefault(w => ReferenceEquals(w.Dto, dto) || (w.Dto.Bad_ID.HasValue && dto.Bad_ID.HasValue && w.Dto.Bad_ID.Value == dto.Bad_ID.Value));
                if (wrapper != null)
                    wrapper.IsMarked = false;
            }

            // numeracja, podsumowanie i odświeżenie filtrów
            for (int i = 0; i < SelectedLista.Badania.Count; i++)
                SelectedLista.Badania[i].Lp = i + 1;

            RecalculateTotalRazem();
            ApplyAvailableFilter(); // odświeża FilteredAvailableBadania po usunięciach z AvailableBadania
            OnPropertyChanged(nameof(SelectedLista));
            OnPropertyChanged(nameof(TotalRazem));
        }

        private void CancelAllAssignedToAvailable()
        {
            try
            {
                if (SelectedLista?.Badania == null || SelectedLista.Badania.Count == 0) return;

                // Skopiuj elementy do tymczasowej listy DTO (żeby nie modyfikować kolekcji podczas iteracji)
                var toMove = SelectedLista.Badania.Select(dto => new AccessDbContext.AssignedBadanieDto
                {
                    Bad_ID = dto.Bad_ID,
                    Bad_L_ID = null,
                    Bad_Data = dto.Bad_Data,
                    Bad_Typ = dto.Bad_Typ,
                    P_imie = dto.P_imie,
                    P_nazwisko = dto.P_nazwisko,
                    P_zawod = dto.P_zawod,
                    FirmaNazwa = dto.FirmaNazwa,
                    FirmaCennik = dto.FirmaCennik,
                    Bad_Razem = dto.Bad_Razem,
                    Bad_Cena1 = dto.Bad_Cena1,
                    Bad_Cena2 = dto.Bad_Cena2,
                    Bad_Cena3 = dto.Bad_Cena3,
                    Bad_Cena4 = dto.Bad_Cena4,
                    Bad_Cena5 = dto.Bad_Cena5,
                    Bad_Cena6 = dto.Bad_Cena6,
                    Bad_Cena7 = dto.Bad_Cena7,
                    Bad_Cena8 = dto.Bad_Cena8,
                    Bad_Cena9 = dto.Bad_Cena9,
                    Bad_Cena10 = dto.Bad_Cena10,
                    Bad_Data_Do = dto.Bad_Data_Do,
                    Bad_Wynik = dto.Bad_Wynik,
                    Bad_Nr_KS = dto.Bad_Nr_KS,
                    Bad_END = dto.Bad_END,
                    Bad_P_ID = dto.Bad_P_ID,
                    Bad_S_ID = dto.Bad_S_ID,
                    Bad_F_ID = dto.Bad_F_ID,
                    Bad_bn_cennik = dto.Bad_bn_cennik
                }).ToList();

                // Wstaw na UI-thread synchronnie, potem wyczyść listę przypisanych
                if (Application.Current?.Dispatcher?.CheckAccess() == true)
                {
                    foreach (var copy in toMove)
                    {
                        bool alreadyInAvailable = copy.Bad_ID.HasValue
                            ? AvailableBadania.Any(b => b.Bad_ID.HasValue && b.Bad_ID.Value == copy.Bad_ID.Value)
                            : AvailableBadania.Contains(copy);

                        if (!alreadyInAvailable)
                            AvailableBadania.Insert(0, copy);
                    }
                    SelectedLista.Badania.Clear();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var copy in toMove)
                        {
                            bool alreadyInAvailable = copy.Bad_ID.HasValue
                                ? AvailableBadania.Any(b => b.Bad_ID.HasValue && b.Bad_ID.Value == copy.Bad_ID.Value)
                                : AvailableBadania.Contains(copy);

                            if (!alreadyInAvailable)
                                AvailableBadania.Insert(0, copy);
                        }
                        SelectedLista.Badania.Clear();
                    });
                }

                // Odśwież filtr, numerację i sumę
                ApplyAvailableFilter();

                if (SelectedLista?.Badania != null)
                {
                    for (int i = 0; i < SelectedLista.Badania.Count; i++)
                        SelectedLista.Badania[i].Lp = i + 1;
                }

                RecalculateTotalRazem();
                OnPropertyChanged(nameof(SelectedLista));
                OnPropertyChanged(nameof(TotalRazem));
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CancelAllAssignedToAvailable error: {ex}");
            }
        }

        private void PopulateGridFromSelected()
        {
            if (SelectedAssignedBadanie == null) { ClearGridFields(); return; }

            WybraneImie = SelectedAssignedBadanie.P_imie;
            WybraneNazwisko = SelectedAssignedBadanie.P_nazwisko;
            TypBadania = SelectedAssignedBadanie.Bad_Typ;
            DataBadania = SelectedAssignedBadanie.Bad_Data;
            DataWaznosci = SelectedAssignedBadanie.Bad_Data_Do;
            SelectedWynik = SelectedAssignedBadanie.Bad_Wynik;
            NrKsiegi = SelectedAssignedBadanie.Bad_Nr_KS;

            // ustaw pola etykiety (Price...Text)
            _priceBasic = SelectedAssignedBadanie.Bad_Cena1 ?? 0m;
            _priceLaryngologist = SelectedAssignedBadanie.Bad_Cena2 ?? 0m;
            _priceOphthalmologist = SelectedAssignedBadanie.Bad_Cena3 ?? 0m;
            _priceSanitary = SelectedAssignedBadanie.Bad_Cena4 ?? 0m;
            _priceLipidogram = SelectedAssignedBadanie.Bad_Cena5 ?? 0m;
            _priceEKG = SelectedAssignedBadanie.Bad_Cena6 ?? 0m;
            _priceHealthClinic = SelectedAssignedBadanie.Bad_Cena7 ?? 0m;
            _priceOther = SelectedAssignedBadanie.Bad_Cena8 ?? 0m;

            NotifyPriceProperties();

            // ustaw edytowalne pola textboxów (format PL)
            var ci = CultureInfo.GetCultureInfo("pl-PL");
            EditablePriceBasic = (_priceBasic != 0m) ? _priceBasic.ToString("N2", ci) : string.Empty;
            EditablePriceLaryngologist = (_priceLaryngologist != 0m) ? _priceLaryngologist.ToString("N2", ci) : string.Empty;
            EditablePriceOphthalmologist = (_priceOphthalmologist != 0m) ? _priceOphthalmologist.ToString("N2", ci) : string.Empty;
            EditablePriceSanitary = (_priceSanitary != 0m) ? _priceSanitary.ToString("N2", ci) : string.Empty;
            EditablePriceLipidogram = (_priceLipidogram != 0m) ? _priceLipidogram.ToString("N2", ci) : string.Empty;
            EditablePriceEKG = (_priceEKG != 0m) ? _priceEKG.ToString("N2", ci) : string.Empty;
            EditablePriceHealthClinic = (_priceHealthClinic != 0m) ? _priceHealthClinic.ToString("N2", ci) : string.Empty;
            EditablePriceOther = (_priceOther != 0m) ? _priceOther.ToString("N2", ci) : string.Empty;

            // ustaw stany checkboxów na podstawie obecności/niezerowości cen
            IsKsiazeczkaChecked = SelectedAssignedBadanie.Bad_Cena4.HasValue && SelectedAssignedBadanie.Bad_Cena4.Value != 0m;
            IsUrlopChecked = SelectedAssignedBadanie.Bad_Cena7.HasValue && SelectedAssignedBadanie.Bad_Cena7.Value != 0m;

            // powiadom UI o zmianach pól formularza
            OnPropertyChanged(nameof(WybraneImie));
            OnPropertyChanged(nameof(WybraneNazwisko));
            OnPropertyChanged(nameof(TypBadania));
            OnPropertyChanged(nameof(DataBadania));
            OnPropertyChanged(nameof(DataWaznosci));
            OnPropertyChanged(nameof(SelectedWynik));
            OnPropertyChanged(nameof(NrKsiegi));
            OnPropertyChanged(nameof(EditablePriceBasic));
            OnPropertyChanged(nameof(EditablePriceLaryngologist));
            OnPropertyChanged(nameof(EditablePriceOphthalmologist));
            OnPropertyChanged(nameof(EditablePriceSanitary));
            OnPropertyChanged(nameof(EditablePriceLipidogram));
            OnPropertyChanged(nameof(EditablePriceEKG));
            OnPropertyChanged(nameof(EditablePriceHealthClinic));
            OnPropertyChanged(nameof(EditablePriceOther));
            OnPropertyChanged(nameof(IsKsiazeczkaChecked));
            OnPropertyChanged(nameof(IsUrlopChecked));
        }

        // --- Clear only form fields (no DB, no list modifications) ---
        private void ClearGridFields()
        {
            WybraneImie = null;
            WybraneNazwisko = null;
            TypBadania = null;
            DataBadania = null;
            DataWaznosci = null;
            SelectedWynik = WynikOptions.FirstOrDefault();
            NrKsiegi = null;
            SelectedAssignedBadanie = null;

            // wyczyść edytowalne pola cen
            EditablePriceBasic = string.Empty;
            EditablePriceLaryngologist = string.Empty;
            EditablePriceOphthalmologist = string.Empty;
            EditablePriceSanitary = string.Empty;
            EditablePriceLipidogram = string.Empty;
            EditablePriceEKG = string.Empty;
            EditablePriceHealthClinic = string.Empty;
            EditablePriceOther = string.Empty;

            // zresetuj checkboxy
            IsKsiazeczkaChecked = false;
            IsUrlopChecked = false;

            // powiadom UI
            OnPropertyChanged(nameof(WybraneImie));
            OnPropertyChanged(nameof(WybraneNazwisko));
            OnPropertyChanged(nameof(TypBadania));
            OnPropertyChanged(nameof(DataBadania));
            OnPropertyChanged(nameof(DataWaznosci));
            OnPropertyChanged(nameof(SelectedWynik));
            OnPropertyChanged(nameof(NrKsiegi));
            OnPropertyChanged(nameof(SelectedAssignedBadanie));
            OnPropertyChanged(nameof(EditablePriceBasic));
            OnPropertyChanged(nameof(EditablePriceLaryngologist));
            OnPropertyChanged(nameof(EditablePriceOphthalmologist));
            OnPropertyChanged(nameof(EditablePriceSanitary));
            OnPropertyChanged(nameof(EditablePriceLipidogram));
            OnPropertyChanged(nameof(EditablePriceEKG));
            OnPropertyChanged(nameof(EditablePriceHealthClinic));
            OnPropertyChanged(nameof(EditablePriceOther));
            OnPropertyChanged(nameof(IsKsiazeczkaChecked));
            OnPropertyChanged(nameof(IsUrlopChecked));

            DataBadania = DateTime.Today;
            DataWaznosci = DataBadania?.AddYears(3);
            DataWystawienia = DateTime.Today;

            RequestResetSelectionState?.Invoke();


        }

        // --- Save only to VM's SelectedLista (virtual save) ---
        private void SaveSelectedAssignedBadanie()
        {
            try
            {
                // If there's a selectedAssigned pointing to an existing item in SelectedLista => update it
                AccessDbContext.AssignedBadanieDto? target = null;
                if (SelectedAssignedBadanie != null && SelectedLista?.Badania != null)
                {
                    target = SelectedLista.Badania.FirstOrDefault(b =>
                        ReferenceEquals(b, SelectedAssignedBadanie) ||
                        (b.Bad_ID.HasValue && SelectedAssignedBadanie.Bad_ID.HasValue && b.Bad_ID.Value == SelectedAssignedBadanie.Bad_ID.Value));
                }

                // przygotuj ceny z edytowalnych pól (nullable)
                var c1 = ParseEditablePrice(EditablePriceBasic);
                var c2 = ParseEditablePrice(EditablePriceLaryngologist);
                var c3 = ParseEditablePrice(EditablePriceOphthalmologist);
                var c4 = ParseEditablePrice(EditablePriceSanitary);
                var c5 = ParseEditablePrice(EditablePriceLipidogram);
                var c6 = ParseEditablePrice(EditablePriceEKG);
                var c7 = ParseEditablePrice(EditablePriceHealthClinic);
                var c8 = ParseEditablePrice(EditablePriceOther);

                decimal? sumNullable = null;
                // oblicz sumę tylko jeśli przynajmniej jedna cena ma wartość
                var priceValues = new decimal?[] { c1, c2, c3, c4, c5, c6, c7, c8 };
                if (priceValues.Any(p => p.HasValue))
                {
                    sumNullable = priceValues.Where(p => p.HasValue).Sum(p => p!.Value);
                }

                if (target != null)
                {
                    // update existing virtual record
                    target.P_imie = WybraneImie;
                    target.P_nazwisko = WybraneNazwisko;
                    target.Bad_Typ = TypBadania;
                    target.Bad_Data = DataBadania;
                    target.Bad_Data_Do = DataWaznosci; // Data ważności
                    target.Bad_Wynik = SelectedWynik;
                    target.Bad_Nr_KS = NrKsiegi;

                    // zapis cen i sumy
                    target.Bad_Cena1 = c1;
                    target.Bad_Cena2 = c2;
                    target.Bad_Cena3 = c3;
                    target.Bad_Cena4 = c4;
                    target.Bad_Cena5 = c5;
                    target.Bad_Cena6 = c6;
                    target.Bad_Cena7 = c7;
                    target.Bad_Cena8 = c8;
                    target.Bad_Razem = sumNullable;
                }
                else
                {
                    // create new virtual AssignedBadanieDto from form fields
                    var newDto = new AccessDbContext.AssignedBadanieDto
                    {
                        Bad_ID = null, // virtual - DB id unknown
                        Bad_L_ID = null, // virtual - Lista ID unknown
                        Bad_Data = DataBadania,
                        Bad_Typ = TypBadania,
                        P_imie = WybraneImie,
                        P_nazwisko = WybraneNazwisko,
                        P_zawod = null,
                        FirmaNazwa = WybranaFirma,
                        FirmaCennik = SelectedCennik,
                        Bad_Razem = sumNullable,
                        Bad_Cena1 = c1,
                        Bad_Cena2 = c2,
                        Bad_Cena3 = c3,
                        Bad_Cena4 = c4,
                        Bad_Cena5 = c5,
                        Bad_Cena6 = c6,
                        Bad_Cena7 = c7,
                        Bad_Cena8 = c8,
                        Bad_Cena9 = null,
                        Bad_Cena10 = null,
                        Bad_Data_Do = DataWaznosci,
                        Bad_Wynik = SelectedWynik,
                        Bad_Nr_KS = NrKsiegi,
                        Bad_END = false,
                        Bad_P_ID = null,
                        Bad_S_ID = null,
                        Bad_F_ID = SelectedFirmaId,
                        Bad_bn_cennik = null
                    };

                    if (SelectedLista?.Badania == null)
                        SelectedLista.Badania = new System.Collections.ObjectModel.ObservableCollection<AccessDbContext.AssignedBadanieDto>();

                    SelectedLista.Badania.Add(newDto);
                }

                // update numbering and totals
                if (SelectedLista?.Badania != null)
                {
                    for (int i = 0; i < SelectedLista.Badania.Count; i++)
                        SelectedLista.Badania[i].Lp = i + 1;
                }

                RecalculateTotalRazem();
                OnPropertyChanged(nameof(SelectedLista));
                OnPropertyChanged(nameof(TotalRazem));

                // potwierdzenie przed wyczyszczeniem formularza
                // MessageBox.Show("Zapisano zmiany na liście (wirtualnie).", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                // NotificationHelper
                NotificationHelper.PrintSuccess("Zapisano zmiany na liście (wirtualnie).");

                // Uruchom ClearGridCommand aby wyczyścić pola formularza po zapisie
                try
                {
                    if (ClearGridCommand != null && ClearGridCommand.CanExecute(null))
                        ClearGridCommand.Execute(null);
                    else
                        ClearGridFields(); // fallback jeśli polecenie z jakiegoś powodu nie jest dostępne
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ClearGridCommand execution failed: {ex}");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"SaveSelectedAssignedBadanie error: {ex}");
                MessageBox.Show($"Błąd podczas zapisu (wirtualnego):\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                //NotificationHelper.ShowError($"Błąd podczas zapisu (wirtualnego): {ex.Message}");
            }
        }

        // --- Zastąp istniejącą metodę SaveListOfInvoiceAsync poniższą implementacją ---
        // Obsługa tworzenia i modyfikacji listy (przy modyfikacji: najpierw odpinamy stare Bad_L_ID, potem przypisujemy na nowo)
        // Obsługa faktury jak wcześniej (sprawdzenie istnienia po numerze, insert jeśli nowa)
        // Usuwanie niezgodnych pozycji z listy po potwierdzeniu użytkownika
        // Obsługa pól FK_... w SQL
        // sprawdzenie wybranej firmy przed zapisem
        // sprawdza ID pacjenta i używa go przy insertach
        //
        public async System.Threading.Tasks.Task SaveListOfInvoiceAsync()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    if (SelectedFirmaId == null && string.IsNullOrWhiteSpace(WybranaFirma))
                    {
                        NotificationHelper.ShowWarning("Wybierz firmę przed zapisem listy do faktury.");
                        return;
                    }

                    if (SelectedLista?.Badania == null || SelectedLista.Badania.Count == 0)
                    {
                        NotificationHelper.ShowWarning("Brak pozycji na liście — nic do zapisania.");
                        return;
                    }

                    var nonMatching = SelectedLista.Badania.Where(b => !IsSameFirma(b)).ToList();
                    if (nonMatching.Count > 0)
                    {
                        var msg = $"Znaleziono {nonMatching.Count} pozycję/pozycji niezgodnych z wybraną firmą.\nCzy usunąć te pozycje przed zapisem?";
                        var answer = System.Windows.MessageBox.Show(msg, "Pozycje niezgodne", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                        if (answer == System.Windows.MessageBoxResult.Yes)
                        {
                            foreach (var nm in nonMatching) SelectedLista.Badania.Remove(nm);
                            RecalculateTotalRazem();
                        }
                        else
                        {
                            NotificationHelper.ShowWarning("Zapis anulowany — nieusunięte pozycje niezgodne.");
                            return;
                        }
                    }

                    var db = new AccessDbHelper();
                    using var conn = db.GetConnection();
                    conn.Open();
                    using var tx = conn.BeginTransaction();
                    try
                    {
                        static object DbVal(object? v) => v ?? System.DBNull.Value;

                        int? fakturaId = null;
                        bool hasNumber = !string.IsNullOrWhiteSpace(NumerFaktury);

                        // Sprawdź/utwórz fakturę (jak wcześniej) - zmodyfikowane SQL aby używać pól FK_...
                        if (hasNumber)
                        {
                            using var cmdCheck = conn.CreateCommand();
                            cmdCheck.Transaction = tx;
                            cmdCheck.CommandText = "SELECT FK_ID FROM Faktura WHERE TRIM(FK_Numer) = TRIM(?)";
                            var pnum = cmdCheck.CreateParameter(); pnum.Value = (object)NumerFaktury ?? System.DBNull.Value; cmdCheck.Parameters.Add(pnum);
                            var obj = cmdCheck.ExecuteScalar();
                            if (obj != null && obj != System.DBNull.Value)
                            {
                                fakturaId = Convert.ToInt32(obj);
                            }
                            else
                            {
                                using var cmdIns = conn.CreateCommand();
                                cmdIns.Transaction = tx;
                                cmdIns.CommandText = "INSERT INTO Faktura (FK_Firma_ID, FK_Numer, FK_Data, FK_Kwota, FK_Uwagi, FK_Cennik, FK_Num_Listy) VALUES (?, ?, ?, ?, ?, ?,?)";
                                var AddParam = new Func<object?, System.Data.IDbDataParameter>((val) =>
                                {
                                    var par = cmdIns.CreateParameter();
                                    par.Value = DbVal(val);
                                    cmdIns.Parameters.Add(par);
                                    return par;
                                });
                                AddParam(SelectedFirmaId);
                                AddParam(NumerFaktury);
                                AddParam(DataWystawienia ?? DateTime.Today);
                                AddParam(SumaBrutto);
                                AddParam(Uwagi);
                                AddParam(SelectedCennik);
                                AddParam(SelectedLista?.Identyfikator); // FK_Num_Listy określa numer listy czyli lista istnieje

                                ExecuteNonQueryWithDetails(cmdIns);

                                using var cmdId = conn.CreateCommand();
                                cmdId.Transaction = tx;
                                cmdId.CommandText = "SELECT @@IDENTITY";
                                var idObj = cmdId.ExecuteScalar();
                                if (idObj != null && idObj != System.DBNull.Value) fakturaId = Convert.ToInt32(idObj);
                            }
                        }

                        // Rozpoznaj, czy modyfikujemy istniejącą listę
                        int? existingListaId = SelectedLista?.Identyfikator;
                        int? listaId = existingListaId;

                        // Jeśli modyfikujemy istniejącą listę -> odłączamy poprzednie powiązania Bad_L_ID
                        if (existingListaId.HasValue)
                        {
                            using var cmdUnassign = conn.CreateCommand();
                            cmdUnassign.Transaction = tx;
                            cmdUnassign.CommandText = "UPDATE Badanie SET Bad_L_ID = 0, Bad_F_ID = 0,Bad_Fakt = NULL WHERE Bad_L_ID = ?";
                            var p = cmdUnassign.CreateParameter(); p.Value = existingListaId.Value; cmdUnassign.Parameters.Add(p);
                            ExecuteNonQueryWithDetails(cmdUnassign);
                        }

                        // 4) Przetwarzanie pozycji listy (insert/update Badanie)
                        foreach (var dto in SelectedLista?.Badania?.ToList() ?? new())
                        {
                            // Pacjent (jak wcześniej) - zostaw bez zmian

                            if (!dto.Bad_P_ID.HasValue)
                            {
                                int? pid = null;
                                using (var cmdP = conn.CreateCommand())
                                {
                                    cmdP.Transaction = tx;
                                    // Poprawione: warunek na firmę uwzględnia NULL prawidłowo
                                    if (!string.IsNullOrWhiteSpace(WybranaFirma))
                                    {

                                        // Jeśli mamy firmę, szukamy z uwzględnieniem firmy
                                        cmdP.CommandText = "SELECT P_ID FROM P_Pacjent WHERE TRIM(P_imie)=TRIM(?) AND TRIM(P_nazwisko)=TRIM(?) AND TRIM(P_firma)=TRIM(?)";
                                        var pa = cmdP.CreateParameter(); pa.Value = dto.P_imie ?? (object)DBNull.Value; cmdP.Parameters.Add(pa);
                                        var pb = cmdP.CreateParameter(); pb.Value = dto.P_nazwisko ?? (object)DBNull.Value; cmdP.Parameters.Add(pb);
                                        var pc = cmdP.CreateParameter(); pc.Value = WybranaFirma; cmdP.Parameters.Add(pc);

                                    }
                                    else
                                    {
                                        // Jeśli brak firmy, szukamy tylko po imieniu i nazwisku
                                        MessageBox.Show("Searching patient without company.");
                                        cmdP.CommandText = "SELECT id FROM P_Pacjent WHERE TRIM(P_imie)=TRIM(?) AND TRIM(P_nazwisko)=TRIM(?) AND (P_firma IS NULL OR TRIM(P_firma)='')";
                                        var pa = cmdP.CreateParameter(); pa.Value = dto.P_imie ?? (object)DBNull.Value; cmdP.Parameters.Add(pa);
                                        var pb = cmdP.CreateParameter(); pb.Value = dto.P_nazwisko ?? (object)DBNull.Value; cmdP.Parameters.Add(pb);

                                    }

                                    var o = cmdP.ExecuteScalar();
                                    if (o != null && o != DBNull.Value) pid = Convert.ToInt32(o);
                                }


                                if (!pid.HasValue)
                                {
                                    // Tworzenie nowego pacjenta - bez zmian
                                    using var cmdInsP = conn.CreateCommand();
                                    cmdInsP.Transaction = tx;
                                    cmdInsP.CommandText = "INSERT INTO P_Pacjent (P_imie, P_nazwisko, P_firma, P_Firma_id, P_Nowy, P_Activ) VALUES (?, ?, ?, ?, ?, ?)";
                                    var AddP = new Func<object?, System.Data.IDbDataParameter>((val) =>
                                    {
                                        var par = cmdInsP.CreateParameter();
                                        par.Value = DbVal(val);
                                        cmdInsP.Parameters.Add(par);
                                        return par;
                                    });
                                    AddP(dto.P_imie ?? string.Empty); //1 
                                    AddP(dto.P_nazwisko ?? string.Empty); //2
                                    AddP(WybranaFirma ?? string.Empty); //3
                                    AddP(SelectedFirmaId); //4
                                    AddP(true); //5
                                    AddP(true); //6
                                    //MessageBox.Show($"Inserting new patient SQL: {cmdInsP.CommandText}");

                                    ExecuteNonQueryWithDetails(cmdInsP);

                                    using var cmdIdP = conn.CreateCommand();
                                    cmdIdP.Transaction = tx;
                                    cmdIdP.CommandText = "SELECT @@IDENTITY";
                                    var idObj = cmdIdP.ExecuteScalar();
                                    if (idObj != null && idObj != DBNull.Value) pid = Convert.ToInt32(idObj);
                                }
                                if (pid.HasValue) dto.Bad_P_ID = pid.Value;
                            }

                            // Skierowanie (jak wcześniej)
                            if (!dto.Bad_S_ID.HasValue)
                            {
                                using var cmdS = conn.CreateCommand();
                                cmdS.Transaction = tx;
                                cmdS.CommandText = "INSERT INTO B_Skierowania (B_Pacjent_ID, B_Firma_ID, B_Faktura_ID, B_TypBadania, B_DataSkierowania, B_RegistrationDate, B_książeczka, B_Zaswiadczenie, B_Nowe, B_Activ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                                var AddS = new Func<object?, System.Data.IDbDataParameter>((val) =>
                                {
                                    var par = cmdS.CreateParameter();
                                    par.Value = DbVal(val);
                                    cmdS.Parameters.Add(par);
                                    return par;
                                });
                                AddS(dto.Bad_P_ID);
                                AddS(SelectedFirmaId);
                                AddS(fakturaId);
                                AddS(dto.Bad_Typ);
                                AddS(dto.Bad_Data);
                                AddS(DateTime.Now);
                                AddS((dto.Bad_Cena4.HasValue && dto.Bad_Cena4.Value != 0m) ? (object)true : (object)false);
                                AddS((dto.Bad_Cena7.HasValue && dto.Bad_Cena7.Value != 0m) ? (object)true : (object)false);
                                AddS(true);
                                AddS(true);
                                ExecuteNonQueryWithDetails(cmdS);

                                using var cmdIdS = conn.CreateCommand();
                                cmdIdS.Transaction = tx;
                                cmdIdS.CommandText = "SELECT @@IDENTITY";
                                var idObj = cmdIdS.ExecuteScalar();
                                if (idObj != null && idObj != System.DBNull.Value) dto.Bad_S_ID = Convert.ToInt32(idObj);
                            }

                            // Badanie - insert lub update
                            if (!dto.Bad_ID.HasValue)
                            {
                                using var cmdB = conn.CreateCommand();
                                cmdB.Transaction = tx;
                                cmdB.CommandText =
                                    "INSERT INTO Badanie (Bad_S_ID, Bad_P_ID, Bad_Fakt, Bad_bn_cennik, Bad_Typ, Bad_Data, Bad_Data_Do, Bad_Wynik, Bad_Cena1, Bad_Cena2, Bad_Cena3, Bad_Cena4, Bad_Cena5, Bad_Cena6, Bad_Cena7, Bad_Cena8, Bad_Razem, Bad_Nr_KS, Bad_F_ID, Bad_L_ID) " +
                                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                                var AddB = new Func<object?, System.Data.IDbDataParameter>((val) =>
                                {
                                    var par = cmdB.CreateParameter();
                                    par.Value = DbVal(val);
                                    cmdB.Parameters.Add(par);
                                    return par;
                                });

                                AddB(dto.Bad_S_ID);
                                AddB(dto.Bad_P_ID);
                                AddB(NumerFaktury);
                                AddB(SelectedCennik);
                                AddB(dto.Bad_Typ);
                                AddB(dto.Bad_Data);
                                AddB(dto.Bad_Data_Do);
                                AddB(dto.Bad_Wynik);
                                AddB(dto.Bad_Cena1);
                                AddB(dto.Bad_Cena2);
                                AddB(dto.Bad_Cena3);
                                AddB(dto.Bad_Cena4);
                                AddB(dto.Bad_Cena5);
                                AddB(dto.Bad_Cena6);
                                AddB(dto.Bad_Cena7);
                                AddB(dto.Bad_Cena8);
                                AddB(dto.Bad_Razem);
                                AddB(dto.Bad_Nr_KS);
                                AddB(fakturaId);
                                // jeśli lista już istnieje - przypisz Bad_L_ID od razu, w przeciwnym razie NULL
                                AddB(listaId.HasValue ? (object)listaId.Value : null);
                                ExecuteNonQueryWithDetails(cmdB);

                                using var cmdIdB = conn.CreateCommand();
                                cmdIdB.Transaction = tx;
                                cmdIdB.CommandText = "SELECT @@IDENTITY";
                                var idObj = cmdIdB.ExecuteScalar();
                                if (idObj != null && idObj != System.DBNull.Value) dto.Bad_ID = Convert.ToInt32(idObj);

                                // zaktalizuj B_Badanie_ID w skierowaniu
                                if (dto.Bad_S_ID.HasValue && dto.Bad_ID.HasValue)
                                {
                                    using var cmdUpdS = conn.CreateCommand();
                                    cmdUpdS.Transaction = tx;
                                    cmdUpdS.CommandText = "UPDATE B_Skierowania SET B_Badanie_ID = ? WHERE B_ID = ?";
                                    var psb = cmdUpdS.CreateParameter(); psb.Value = dto.Bad_ID.Value; cmdUpdS.Parameters.Add(psb);
                                    var pss = cmdUpdS.CreateParameter(); pss.Value = dto.Bad_S_ID.Value; cmdUpdS.Parameters.Add(pss);
                                    ExecuteNonQueryWithDetails(cmdUpdS);
                                }
                            }
                            else
                            {
                                using var cmdUpdB = conn.CreateCommand();
                                cmdUpdB.Transaction = tx;
                                cmdUpdB.CommandText =
                                    "UPDATE Badanie SET Bad_S_ID=?, Bad_P_ID=?, Bad_Fakt=?, Bad_bn_cennik=?, Bad_Typ=?, Bad_Data=?, Bad_Data_Do=?, Bad_Wynik=?, Bad_Cena1=?, Bad_Cena2=?, Bad_Cena3=?, Bad_Cena4=?, Bad_Cena5=?, Bad_Cena6=?, Bad_Cena7=?, Bad_Cena8=?, Bad_Razem=?, Bad_Nr_KS=?, Bad_F_ID=? WHERE Bad_ID=?";
                                var AddU = new Func<object?, System.Data.IDbDataParameter>((val) =>
                                {
                                    var par = cmdUpdB.CreateParameter();
                                    par.Value = DbVal(val);
                                    cmdUpdB.Parameters.Add(par);
                                    return par;
                                });

                                AddU(dto.Bad_S_ID);
                                AddU(dto.Bad_P_ID);
                                AddU(NumerFaktury);
                                AddU(SelectedCennik);
                                AddU(dto.Bad_Typ);
                                AddU(dto.Bad_Data);
                                AddU(dto.Bad_Data_Do);
                                AddU(dto.Bad_Wynik);
                                AddU(dto.Bad_Cena1);
                                AddU(dto.Bad_Cena2);
                                AddU(dto.Bad_Cena3);
                                AddU(dto.Bad_Cena4);
                                AddU(dto.Bad_Cena5);
                                AddU(dto.Bad_Cena6);
                                AddU(dto.Bad_Cena7);
                                AddU(dto.Bad_Cena8);
                                AddU(dto.Bad_Razem);
                                AddU(dto.Bad_Nr_KS);
                                AddU(fakturaId);
                                AddU(dto.Bad_ID.Value);
                                ExecuteNonQueryWithDetails(cmdUpdB);
                            }
                        } // koniec pętli pozycji

                        // 5) Insert lub Update rekordu ListyBadan
                        if (existingListaId.HasValue)
                        {
                            // aktualizacja ListyBadan
                            using var cmdUpdL = conn.CreateCommand();
                            cmdUpdL.Transaction = tx;
                            cmdUpdL.CommandText = "UPDATE ListyBadan SET L_FK_ID = ?, L_Firma_ID = ?, L_Data = ?, L_Uwagi = ? WHERE Identyfikator = ?";
                            var p1 = cmdUpdL.CreateParameter(); p1.Value = DbVal(fakturaId); cmdUpdL.Parameters.Add(p1);
                            var p2 = cmdUpdL.CreateParameter(); p2.Value = DbVal(SelectedFirmaId); cmdUpdL.Parameters.Add(p2);
                            var p3 = cmdUpdL.CreateParameter(); p3.Value = DbVal(DateTime.Now); cmdUpdL.Parameters.Add(p3);
                            var p4 = cmdUpdL.CreateParameter(); p4.Value = DbVal(Uwagi); cmdUpdL.Parameters.Add(p4);
                            var p5 = cmdUpdL.CreateParameter(); p5.Value = existingListaId.Value; cmdUpdL.Parameters.Add(p5);
                            ExecuteNonQueryWithDetails(cmdUpdL);
                            listaId = existingListaId;
                        }
                        else
                        {
                            using (var cmdL = conn.CreateCommand())
                            {
                                cmdL.Transaction = tx;
                                cmdL.CommandText = "INSERT INTO ListyBadan (L_FK_ID, L_Firma_ID, L_Data, L_Uwagi) VALUES (?, ?, ?, ?)";
                                var q1 = cmdL.CreateParameter(); q1.Value = DbVal(fakturaId); cmdL.Parameters.Add(q1);
                                var q2 = cmdL.CreateParameter(); q2.Value = DbVal(SelectedFirmaId); cmdL.Parameters.Add(q2);
                                var q3 = cmdL.CreateParameter(); q3.Value = DbVal(DateTime.Now); cmdL.Parameters.Add(q3);
                                var q4 = cmdL.CreateParameter(); q4.Value = DbVal(Uwagi); cmdL.Parameters.Add(q4);
                                ExecuteNonQueryWithDetails(cmdL);

                                using var cmdIdL = conn.CreateCommand();
                                cmdIdL.Transaction = tx;
                                cmdIdL.CommandText = "SELECT @@IDENTITY";
                                var idObj = cmdIdL.ExecuteScalar();
                                if (idObj != null && idObj != System.DBNull.Value) listaId = Convert.ToInt32(idObj);
                            }
                        }

                        // 6) Ustaw Bad_L_ID = listaId dla wszystkich zapisanych badań
                        if (listaId.HasValue)
                        {
                            foreach (var dto in SelectedLista.Badania)
                            {
                                if (dto.Bad_ID.HasValue)
                                {
                                    using var cmdUpd = conn.CreateCommand();
                                    cmdUpd.Transaction = tx;
                                    cmdUpd.CommandText = "UPDATE Badanie SET Bad_L_ID = ? WHERE Bad_ID = ?";
                                    var pl = cmdUpd.CreateParameter(); pl.Value = listaId.Value; cmdUpd.Parameters.Add(pl);
                                    var pb = cmdUpd.CreateParameter(); pb.Value = dto.Bad_ID.Value; cmdUpd.Parameters.Add(pb);
                                    ExecuteNonQueryWithDetails(cmdUpd);
                                    dto.Bad_L_ID = listaId;
                                }
                            }
                        }

                        // 7) Zaktualizuj sumę faktury (jeśli faktura istnieje) - dodano fk_num_listy
                        if (fakturaId.HasValue)
                        {
                            decimal total = SelectedLista.Badania.Sum(b => b.Bad_Razem ?? 0m);
                            using var cmdUpdF = conn.CreateCommand();
                            cmdUpdF.Transaction = tx;
                            cmdUpdF.CommandText = "UPDATE Faktura SET FK_Suma_Bad = ?, FK_Num_Listy = ? WHERE FK_ID = ?";
                            var ps = cmdUpdF.CreateParameter(); ps.Value = total; cmdUpdF.Parameters.Add(ps);
                            var pn = cmdUpdF.CreateParameter(); pn.Value = DbVal(listaId); cmdUpdF.Parameters.Add(pn);
                            var pf = cmdUpdF.CreateParameter(); pf.Value = fakturaId.Value; cmdUpdF.Parameters.Add(pf);
                            ExecuteNonQueryWithDetails(cmdUpdF);
                        }

                        tx.Commit();

                        // Aktualizacja UI w wątku UI
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // ustaw Identyfikator listy w SelectedLista jeśli nowa
                            if (listaId.HasValue) SelectedLista.Identyfikator = listaId.Value;
                            SaveButtonLabel = listaId.HasValue ? "Modyfikuj listę" : "Zapisz listę";
                            OnPropertyChanged(nameof(SaveButtonLabel));
                            OnPropertyChanged(nameof(WydrukiVisibility));
                            RecalculateTotalRazem();
                            LoadAvailableBadania();
                            LoadFirmyItemsFromDb();
                            NotificationHelper.PrintSuccess(existingListaId.HasValue ? "Lista zmodyfikowana." : "Lista zapisana do faktury.");
                        }));
                    }
                    catch (Exception exTx)
                    {
                        try { tx.Rollback(); } catch { }
                        // System.Diagnostics.Debug.WriteLine($"SaveListOfInvoice tx error: {exTx}");
                        NotificationHelper.ShowError($"Błąd zapisu tej listy: {exTx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"SaveListOfInvoice error: {ex}");
                    NotificationHelper.ShowError($"Wewnętrzny błąd: {ex.Message}");
                }
            });
        }

        private decimal? ParseEditablePrice(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var input = s.Trim();
            // usuń symbol waluty jeśli podany
            input = input.Replace("zł", "", StringComparison.OrdinalIgnoreCase)
                         .Replace("zl", "", StringComparison.OrdinalIgnoreCase)
                         .Trim();
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out var val)) return val;
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var valInv)) return valInv;
            return null;
        }

        private void RecalculateTotalRazem()
        {
            try
            {
                var list = (IEnumerable<AccessDbContext.AssignedBadanieDto>?)SelectedLista?.Badania ?? Enumerable.Empty<AccessDbContext.AssignedBadanieDto>();
                TotalRazem = list.Sum(b => b.Bad_Razem ?? 0m);
            }
            catch { TotalRazem = 0m; }
        }

        private void AddWithoutEdit(object? param)
        {
            if (param is not AccessDbContext.AssignedBadanieDto dto) return;

            try
            {
                if (SelectedLista.Badania == null)
                    SelectedLista.Badania = new System.Collections.ObjectModel.ObservableCollection<AccessDbContext.AssignedBadanieDto>();

                // ✅ Sprawdź czy badanie już nie jest na liście
                bool alreadyInSelected = dto.Bad_ID.HasValue
                    ? SelectedLista.Badania.Any(b => b.Bad_ID.HasValue && b.Bad_ID.Value == dto.Bad_ID.Value)
                    : SelectedLista.Badania.Any(b => ReferenceEquals(b, dto));

                if (alreadyInSelected)
                {
                    // System.Diagnostics.Debug.WriteLine($"AddWithoutEdit: badanie Bad_ID={dto.Bad_ID} już istnieje na liście");
                    return;
                }

                var copy = new AccessDbContext.AssignedBadanieDto
                {
                    Bad_ID = dto.Bad_ID,
                    Bad_L_ID = dto.Bad_L_ID,
                    Bad_Data = dto.Bad_Data,
                    Bad_Typ = dto.Bad_Typ,
                    P_imie = dto.P_imie,
                    P_nazwisko = dto.P_nazwisko,
                    P_zawod = dto.P_zawod,
                    FirmaNazwa = dto.FirmaNazwa,
                    FirmaCennik = dto.FirmaCennik,
                    Bad_Razem = dto.Bad_Razem,
                    Bad_Cena1 = dto.Bad_Cena1,
                    Bad_Cena2 = dto.Bad_Cena2,
                    Bad_Cena3 = dto.Bad_Cena3,
                    Bad_Cena4 = dto.Bad_Cena4,
                    Bad_Cena5 = dto.Bad_Cena5,
                    Bad_Cena6 = dto.Bad_Cena6,
                    Bad_Cena7 = dto.Bad_Cena7,
                    Bad_Cena8 = dto.Bad_Cena8,
                    Bad_Cena9 = dto.Bad_Cena9,
                    Bad_Cena10 = dto.Bad_Cena10,
                    Bad_Data_Do = dto.Bad_Data_Do,
                    Bad_Wynik = dto.Bad_Wynik,
                    Bad_Nr_KS = dto.Bad_Nr_KS,
                    Bad_END = dto.Bad_END,
                    Bad_P_ID = dto.Bad_P_ID,
                    Bad_S_ID = dto.Bad_S_ID,
                    Bad_F_ID = dto.Bad_F_ID,
                    Bad_bn_cennik = dto.Bad_bn_cennik
                };

                SelectedLista.Badania.Add(copy);

                // ✅ POPRAWKA: Usuń oryginał z AvailableBadania (porównanie po Bad_ID lub referencji)
                var toRemove = dto.Bad_ID.HasValue
                    ? AvailableBadania.FirstOrDefault(b => b.Bad_ID.HasValue && b.Bad_ID.Value == dto.Bad_ID.Value)
                    : AvailableBadania.FirstOrDefault(b => ReferenceEquals(b, dto));

                if (toRemove != null)
                {
                    AvailableBadania.Remove(toRemove);
                    // System.Diagnostics.Debug.WriteLine($"AddWithoutEdit: usunięto badanie Bad_ID={toRemove.Bad_ID} z AvailableBadania");
                }

                // ✅ POPRAWKA: Odznacz wrapper w FilteredAvailableBadania (jeśli istnieje) - zapobiega powielaniu
                var wrapper = FilteredAvailableBadania.FirstOrDefault(w => 
                    ReferenceEquals(w.Dto, dto) || 
                    (w.Dto.Bad_ID.HasValue && dto.Bad_ID.HasValue && w.Dto.Bad_ID.Value == dto.Bad_ID.Value));
                if (wrapper != null)
                {
                    wrapper.IsMarked = false;
                    // System.Diagnostics.Debug.WriteLine($"AddWithoutEdit: odznaczono wrapper Bad_ID={dto.Bad_ID}");
                }

                // ✅ Odśwież widok filtrowany (usunie wrapper z widoku)
                ApplyAvailableFilter();

                for (int i = 0; i < SelectedLista.Badania.Count; i++)
                    SelectedLista.Badania[i].Lp = i + 1;

                RecalculateTotalRazem();
                OnPropertyChanged(nameof(SelectedLista));
                OnPropertyChanged(nameof(TotalRazem));

                // System.Diagnostics.Debug.WriteLine($"AddWithoutEdit: zakończono, AvailableBadania.Count={AvailableBadania.Count}, FilteredAvailableBadania.Count={FilteredAvailableBadania.Count}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"AddWithoutEdit error: {ex}");
            }
        }

        private void LoadAvailableBadania()
        {
            try
            {
                var rows = _db.GetBadaniaForLista(0);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    AvailableBadania.Clear();
                    if (rows == null) return;
                    foreach (var r in rows) AvailableBadania.Add(r);

                    // ✅ Zapisz aktualną liczbę badań w cache
                    _cachedAvailableCountInDb = AvailableBadania.Count;
                    NewBadaniaAvailableCount = 0; // zresetuj licznik nowych

                    ApplyAvailableFilter();
                }));
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadAvailableBadania failed: {ex}");
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Inteligentne odświeżanie listy badań
        /// - Jeśli prawa kolumna pusta lub nowa lista → automatyczne odświeżenie
        /// - W przeciwnym razie → sprawdź czy są nowe badania i pokaż przycisk
        /// </summary>
        public void SmartRefreshAvailableBadania()
        {
            try
            {
                bool isNewOrEmptyList = SelectedLista?.Badania == null || 
                                       SelectedLista.Badania.Count == 0 || 
                                       !SelectedLista.Identyfikator.HasValue;

                if (isNewOrEmptyList)
                {
                    // Automatyczne odświeżenie dla nowej/pustej listy
                    // System.Diagnostics.Debug.WriteLine("[SmartRefresh] Automatyczne odświeżenie (nowa/pusta lista)");
                    Task.Run(() => LoadAvailableBadania());
                }
                else
                {
                    // Sprawdź czy są nowe badania w bazie
                    Task.Run(() => CheckForNewBadaniaInDb());
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"SmartRefreshAvailableBadania error: {ex}");
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Sprawdza liczbę dostępnych badań w bazie vs cache
        /// </summary>
        private void CheckForNewBadaniaInDb()
        {
            try
            {
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                // Policz badania bez przypisania do listy (Bad_L_ID IS NULL)
                cmd.CommandText = "SELECT COUNT(*) FROM Badania WHERE Bad_L_ID IS NULL OR Bad_L_ID = 0";
                var countObj = cmd.ExecuteScalar();
                int currentCountInDb = countObj != null && countObj != DBNull.Value ? Convert.ToInt32(countObj) : 0;

                int newCount = currentCountInDb - _cachedAvailableCountInDb;

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (newCount > 0)
                    {
                        NewBadaniaAvailableCount = newCount;
                        // System.Diagnostics.Debug.WriteLine($"[SmartRefresh] Wykryto {newCount} nowych badań - pokazano przycisk");
                    }
                    else
                    {
                        NewBadaniaAvailableCount = 0;
                    }
                }));
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CheckForNewBadaniaInDb error: {ex}");
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Odśwież listę badań (wywołanie z przycisku)
        /// </summary>
        private void RefreshAvailableBadania()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("[RefreshAvailableBadania] Ręczne odświeżenie listy");
                Task.Run(() => LoadAvailableBadania());
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"RefreshAvailableBadania error: {ex}");
            }
        }

        private void DeleteAssignedBadanie(object? param)
        {
            if (param is not AccessDbContext.AssignedBadanieDto dto) return;

            try
            {
                // Usuń z SelectedLista.Badania (wyszukujemy po referencji lub Bad_ID)
                if (SelectedLista?.Badania != null)
                {
                    var toRemove = SelectedLista.Badania.FirstOrDefault(b =>
                        ReferenceEquals(b, dto) ||
                        (b.Bad_ID.HasValue && dto.Bad_ID.HasValue && b.Bad_ID.Value == dto.Bad_ID.Value));
                    if (toRemove != null)
                        SelectedLista.Badania.Remove(toRemove);
                }

                // Przywróć do AvailableBadania jeśli jeszcze tam nie ma (porównanie po Bad_ID)
                bool alreadyInAvailable = dto.Bad_ID.HasValue
                    ? AvailableBadania.Any(b => b.Bad_ID.HasValue && b.Bad_ID.Value == dto.Bad_ID.Value)
                    : AvailableBadania.Contains(dto);

                if (!alreadyInAvailable)
                {
                    var copy = new AccessDbContext.AssignedBadanieDto
                    {
                        Bad_ID = dto.Bad_ID,
                        Bad_L_ID = null, // nie przypisujemy do listy na tym etapie
                        Bad_Data = dto.Bad_Data,
                        Bad_Typ = dto.Bad_Typ,
                        P_imie = dto.P_imie,
                        P_nazwisko = dto.P_nazwisko,
                        P_zawod = dto.P_zawod,
                        FirmaNazwa = dto.FirmaNazwa,
                        FirmaCennik = dto.FirmaCennik,
                        Bad_Razem = dto.Bad_Razem,
                        Bad_Cena1 = dto.Bad_Cena1,
                        Bad_Cena2 = dto.Bad_Cena2,
                        Bad_Cena3 = dto.Bad_Cena3,
                        Bad_Cena4 = dto.Bad_Cena4,
                        Bad_Cena5 = dto.Bad_Cena5,
                        Bad_Cena6 = dto.Bad_Cena6,
                        Bad_Cena7 = dto.Bad_Cena7,
                        Bad_Cena8 = dto.Bad_Cena8,
                        Bad_Cena9 = dto.Bad_Cena9,
                        Bad_Cena10 = dto.Bad_Cena10,
                        Bad_Data_Do = dto.Bad_Data_Do,
                        Bad_Wynik = dto.Bad_Wynik,
                        Bad_Nr_KS = dto.Bad_Nr_KS,
                        Bad_END = dto.Bad_END,
                        Bad_P_ID = dto.Bad_P_ID,
                        Bad_S_ID = dto.Bad_S_ID,
                        Bad_F_ID = dto.Bad_F_ID,
                        Bad_bn_cennik = dto.Bad_bn_cennik
                    };

                    // WstawSynchronnie na wątku UI, aby od razu był widoczny przez ApplyAvailableFilter()
                    if (Application.Current?.Dispatcher?.CheckAccess() == true)
                    {
                        AvailableBadania.Insert(0, copy);
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() => AvailableBadania.Insert(0, copy));
                    }
                }

                // zaktualizuj widok filtrowany i numerację, sumę
                ApplyAvailableFilter();

                if (SelectedLista?.Badania != null)
                {
                    for (int i = 0; i < SelectedLista.Badania.Count; i++)
                        SelectedLista.Badania[i].Lp = i + 1;
                }

                RecalculateTotalRazem();
                OnPropertyChanged(nameof(SelectedLista));
                OnPropertyChanged(nameof(TotalRazem));

                // Uwaga: NIE zapisujemy niczego do bazy tutaj.
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"DeleteAssignedBadanie error: {ex}");
            }
        }

        private void MarkAllFiltered()
        {
            try
            {
                foreach (var w in FilteredAvailableBadania)
                    w.IsMarked = true;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"MarkAllFiltered error: {ex}");
            }
        }

        private void ApplyAvailableFilter()
        {
            try
            {
                var ci = CultureInfo.CurrentCulture.CompareInfo;
                var opts = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

                var firmQuery = WybranaFirma?.Trim();
                var imieQuery = WybraneImie?.Trim();
                var nazwiskoQuery = WybraneNazwisko?.Trim();

                var filteredDtos = AvailableBadania.Where(b =>
                {
                    if (!string.IsNullOrWhiteSpace(firmQuery))
                    {
                        var fname = b.FirmaNazwa ?? string.Empty;
                        if (ci.IndexOf(fname, firmQuery, opts) < 0) return false;
                    }

                    if (!string.IsNullOrWhiteSpace(imieQuery))
                    {
                        var pimie = b.P_imie ?? string.Empty;
                        if (ci.IndexOf(pimie, imieQuery, opts) < 0) return false;
                    }

                    if (!string.IsNullOrWhiteSpace(nazwiskoQuery))
                    {
                        var pnazw = b.P_nazwisko ?? string.Empty;
                        if (ci.IndexOf(pnazw, nazwiskoQuery, opts) < 0) return false;
                    }

                    return true;
                }).ToList();

                // Przygotuj listę wrapperów; zachowaj stan IsMarked dla już istniejących elementów
                var newWrappers = new List<AssignedBadanieWrapper>(filteredDtos.Count);
                foreach (var dto in filteredDtos)
                {
                    AssignedBadanieWrapper? existing = null;

                    if (dto.Bad_ID.HasValue)
                        existing = FilteredAvailableBadania.FirstOrDefault(w => w.Dto.Bad_ID.HasValue && w.Dto.Bad_ID.Value == dto.Bad_ID.Value);

                    if (existing == null)
                        existing = FilteredAvailableBadania.FirstOrDefault(w => ReferenceEquals(w.Dto, dto));

                    if (existing != null)
                        newWrappers.Add(existing);
                    else
                        newWrappers.Add(new AssignedBadanieWrapper(dto));
                }

                void ReplaceFiltered()
                {
                    FilteredAvailableBadania.Clear();
                    foreach (var w in newWrappers) FilteredAvailableBadania.Add(w);
                }

                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                    dispatcher.Invoke(ReplaceFiltered);
                else
                    ReplaceFiltered();
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ApplyAvailableFilter error: {ex}");
            }
        }
        // --- wrapper dla pozycji dostępnych badań ---
        public class AssignedBadanieWrapper : INotifyPropertyChanged
        {
            public AssignedBadanieWrapper(AccessDbContext.AssignedBadanieDto dto)
            {
                Dto = dto ?? throw new ArgumentNullException(nameof(dto));
            }

            public AccessDbContext.AssignedBadanieDto Dto { get; }

            // Proxy properties (get/set) — pozwalają na TwoWay binding z XAML
            public string? PacjentDisplay
            {
                get => Dto?.PacjentDisplay;
                set
                {
                    if (Dto == null) return;
                    // jeśli PacjentDisplay można zmienić, przypisz odpowiednie pola w Dto (tu przykład — jeśli potrzebne)
                    OnPropertyChanged();
                }
            }

            public DateTime? Bad_Data
            {
                get => Dto?.Bad_Data;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Data != value)
                    {
                        Dto.Bad_Data = value;
                        OnPropertyChanged();
                        // odśwież też inne powiązane pola
                        OnPropertyChanged(nameof(PacjentDisplay));
                    }
                }
            }

            public decimal? Bad_Razem
            {
                get => Dto?.Bad_Razem;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Razem != value)
                    {
                        Dto.Bad_Razem = value;
                        OnPropertyChanged();
                    }
                }
            }

            public string? FirmaNazwa
            {
                get => Dto?.FirmaNazwa;
                set
                {
                    if (Dto == null) return;
                    if (Dto.FirmaNazwa != value)
                    {
                        Dto.FirmaNazwa = value;
                        OnPropertyChanged();
                    }
                }
            }

            public string? Bad_Wynik
            {
                get => Dto?.Bad_Wynik;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Wynik != value)
                    {
                        Dto.Bad_Wynik = value;
                        OnPropertyChanged();
                    }
                }
            }

            public string? Bad_Nr_KS
            {
                get => Dto?.Bad_Nr_KS;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Nr_KS != value)
                    {
                        Dto.Bad_Nr_KS = value;
                        OnPropertyChanged();
                    }
                }
            }

            private bool _isMarked;
            public bool IsMarked
            {
                get => _isMarked;
                set
                {
                    if (_isMarked == value) return;
                    _isMarked = value;
                    OnPropertyChanged();
                }
            }

            public decimal? Bad_Cena1
            {
                get => Dto?.Bad_Cena1;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Cena1 != value)
                    {
                        Dto.Bad_Cena1 = value;
                        OnPropertyChanged();
                    }
                }
            }
            public decimal? Bad_Cena2
            {
                get => Dto?.Bad_Cena2;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Cena2 != value)
                    {
                        Dto.Bad_Cena2 = value;
                        OnPropertyChanged();
                    }
                }
            }
            public decimal? Bad_Cena3
            {
                get => Dto?.Bad_Cena3;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Cena3 != value)
                    {
                        Dto.Bad_Cena3 = value;
                        OnPropertyChanged();
                    }
                }
            }
            public decimal? Bad_Cena4
            {
                get => Dto?.Bad_Cena4;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Cena4 != value)
                    {
                        Dto.Bad_Cena4 = value;
                        OnPropertyChanged();
                    }
                }
            }
            public decimal? Bad_Cena5
            {
                get => Dto?.Bad_Cena5;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Cena5 != value)
                    {
                        Dto.Bad_Cena5 = value;
                        OnPropertyChanged();
                    }
                }
            }
            public decimal? Bad_Cena6
            {
                get => Dto?.Bad_Cena6;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Cena6 != value)
                    {
                        Dto.Bad_Cena6 = value;
                        OnPropertyChanged();
                    }
                }
            }
            public decimal? Bad_Cena7
            {
                get => Dto?.Bad_Cena7;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Cena7 != value)
                    {
                        Dto.Bad_Cena7 = value;
                        OnPropertyChanged();
                    }
                }
            }
            public decimal? Bad_Cena8
            {
                get => Dto?.Bad_Cena8;
                set
                {
                    if (Dto == null) return;
                    if (Dto.Bad_Cena8 != value)
                    {
                        Dto.Bad_Cena8 = value;
                        OnPropertyChanged();
                    }
                }
            }


            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // --- INotifyPropertyChanged ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));





        // -> Dodaj poniższe właściwości obok pól _priceOther (po NotifyPriceProperties najlepiej)

        private string? _editablePriceBasic;
        public string? EditablePriceBasic
        {
            get => _editablePriceBasic;
            set { if (_editablePriceBasic != value) { _editablePriceBasic = value; OnPropertyChanged(); } }
        }

        private string? _editablePriceLaryngologist;
        public string? EditablePriceLaryngologist
        {
            get => _editablePriceLaryngologist;
            set { if (_editablePriceLaryngologist != value) { _editablePriceLaryngologist = value; OnPropertyChanged(); } }
        }

        private string? _editablePriceOphthalmologist;
        public string? EditablePriceOphthalmologist
        {
            get => _editablePriceOphthalmologist;
            set { if (_editablePriceOphthalmologist != value) { _editablePriceOphthalmologist = value; OnPropertyChanged(); } }
        }

        private string? _editablePriceSanitary;
        public string? EditablePriceSanitary
        {
            get => _editablePriceSanitary;
            set { if (_editablePriceSanitary != value) { _editablePriceSanitary = value; OnPropertyChanged(); } }
        }

        private string? _editablePriceLipidogram;
        public string? EditablePriceLipidogram
        {
            get => _editablePriceLipidogram;
            set { if (_editablePriceLipidogram != value) { _editablePriceLipidogram = value; OnPropertyChanged(); } }
        }

        private string? _editablePriceEKG;
        public string? EditablePriceEKG
        {
            get => _editablePriceEKG;
            set { if (_editablePriceEKG != value) { _editablePriceEKG = value; OnPropertyChanged(); } }
        }

        private string? _editablePriceHealthClinic;
        public string? EditablePriceHealthClinic
        {
            get => _editablePriceHealthClinic;
            set { if (_editablePriceHealthClinic != value) { _editablePriceHealthClinic = value; OnPropertyChanged(); } }
        }

        private string? _editablePriceOther;
        public string? EditablePriceOther
        {
            get => _editablePriceOther;
            set { if (_editablePriceOther != value) { _editablePriceOther = value; OnPropertyChanged(); } }
        }

        // Dodaj w klasie ListaFaktAddViewModel (np. w sekcji pól/form) właściwości stanu checkboxów:
        private bool _isKsiazeczkaChecked;
        public bool IsKsiazeczkaChecked
        {
            get => _isKsiazeczkaChecked;
            set { if (_isKsiazeczkaChecked != value) { _isKsiazeczkaChecked = value; OnPropertyChanged(); } }
        }

        private bool _isUrlopChecked;

        public bool IsUrlopChecked
        {
            get => _isUrlopChecked;
            set { if (_isUrlopChecked != value) { _isUrlopChecked = value; OnPropertyChanged(); } }
        }

        public string ?SaveButtonLabel { get; private set; }

        public System.Windows.Visibility WydrukiVisibility =>
            (SelectedLista?.Identyfikator.HasValue == true)
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

        // --- Asynchroniczne zapisywanie listy do faktury ---

        // Walidator zgodności liczby parametrów z liczbą placeholderów '?'
        private static void ValidateParameterCount(IDbCommand cmd)
        {
            if (cmd == null) return;
            var sql = cmd.CommandText ?? string.Empty;
            int expected = sql.Count(ch => ch == '?');
            int actual = cmd.Parameters?.Count ?? 0;
            if (expected != actual)
            {
                var vals = new System.Text.StringBuilder();
                foreach (var p in cmd.Parameters)
                {
                    if (p is IDataParameter idp)
                        vals.Append(idp.Value is null ? "[null]" : $"[{idp.Value}]");
                    else
                        vals.Append("[param]");
                }

                throw new InvalidOperationException($"SQL parameter count mismatch: expected {expected} parameters but command has {actual}. SQL: {sql}. Parameter values: {vals}");
            }
        }

        // Helper: normalizuje tekst (trim, lowercase, usuwa znaki diakrytyczne)
        private static string NormalizeText(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var normalized = s.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        // Helper: porównuje jedną pozycję (AssignedBadanieDto) z wybraną firmą w VM
        private bool IsSameFirma(AccessDbContext.AssignedBadanieDto dto)
        {

            // 3) Porównanie nazw po normalizacji (priorytet: SelectedFirmaDto.Nazwa, potem WybranaFirma)
            var selNameSource = SelectedFirmaDto?.Nazwa ?? WybranaFirma;
            var selName = NormalizeText(selNameSource);
            var dtoName = NormalizeText(dto.FirmaNazwa);

            if (!string.IsNullOrEmpty(selName) && !string.IsNullOrEmpty(dtoName) && selName == dtoName)
                return true;

            // 1) Porównanie po ID — jeśli zarówno SelectedFirmaId jak i Bad_F_ID są dostępne
            if (SelectedFirmaId.HasValue && dto.Bad_F_ID.HasValue)
                return SelectedFirmaId.Value == dto.Bad_F_ID.Value;

            // 2) Jeśli mamy SelectedFirmaDto i dto.Bad_F_ID — porównaj po Id
            if (SelectedFirmaDto != null && dto.Bad_F_ID.HasValue)
                return SelectedFirmaDto.Id == dto.Bad_F_ID.Value;

            // 4) Jeżeli cennik jest dostępny i różnicuje firmy — porównaj znormalizowane cenniki
            var selCennik = NormalizeText(SelectedCennik);
            var dtoCennik = NormalizeText(dto.FirmaCennik);
            if (!string.IsNullOrEmpty(selCennik) && !string.IsNullOrEmpty(dtoCennik) && selCennik == dtoCennik)
                return true;

            // 5) Brak dopasowania
            return false;
        }

        // Helper wykonujący ExecuteNonQuery z dodatkowym logowaniem SQL i parametrów przy błędzie ODBC
        private static void ExecuteNonQueryWithDetails(IDbCommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            try
            {
                // najpierw sprawdź zgodność liczby parametrów
                ValidateParameterCount(cmd);
                // wykonaj zapytanie dokładnie raz
                cmd.ExecuteNonQuery();
            }
            catch (System.Data.Odbc.OdbcException odEx)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("OdbcException podczas ExecuteNonQuery:");
                sb.AppendLine(odEx.Message);
                sb.AppendLine("SQL:");
                sb.AppendLine(cmd.CommandText ?? string.Empty);
                sb.AppendLine("Parametry:");
                var idx = 0;
                foreach (var pObj in cmd.Parameters)
                {
                    if (pObj is IDataParameter p)
                    {
                        var val = p.Value == null || p.Value == DBNull.Value ? "[null]" : p.Value.ToString();
                        sb.AppendLine($"{idx++}: {val}");
                    }
                    else
                    {
                        sb.AppendLine($"{idx++}: [param]");
                    }
                }

                throw new InvalidOperationException(sb.ToString(), odEx);
            }
        }

        // Pdf service i komendy drukowania
        private readonly PdfExportService _pdfService = new PdfExportService();

        private RelayCommand<object>? _printListaCommand;
        public ICommand ?PrintListaCommand => _printListaCommand ??= new RelayCommand<object>(_ => ExecutePrint(Services.ExportType.Lista));

        private RelayCommand<object>? _printPodstawowyCommand;
        public ICommand ?PrintPodstawowyCommand => _printPodstawowyCommand ??= new RelayCommand<object>(_ => ExecutePrint(Services.ExportType.Podstawowy));

        private RelayCommand<object>? _printSzczegolowyCommand;

        public ICommand ?PrintSzczegolowyCommand => _printSzczegolowyCommand ??= new RelayCommand<object>(_ => ExecutePrint(Services.ExportType.Szczegolowy));


        // Wspólna metoda generująca listę InvoiceItem i wywołująca Eksport do PDF
        private void ExecutePrint(Services.ExportType exportType)
        {
            try
            {
                // Sprawdź czy są pozycje (zarówno wirtualne jak i te już zapisane)
                var source = (IEnumerable<AccessDbContext.AssignedBadanieDto>?)SelectedLista?.Badania ?? Enumerable.Empty<AccessDbContext.AssignedBadanieDto>();
                if (!source.Any())
                {
                    NotificationHelper.ShowWarning("Brak pozycji na liście do wydruku.");
                    return;
                }

                // Mapowanie DTO -> InvoiceItem (tak samo jak w ListaDoFakturViewModel)
                var items = source.Select(b => new InvoiceItem
                {
                    FirstName = b.P_imie,
                    LastName = b.P_nazwisko,
                    BadType = b.Bad_Typ, // <- dodane
                    Total = b.Bad_Razem ?? 0m,
                    ExaminationPrice = b.Bad_Cena1 ?? 0m,
                    LaryngologistPrice = b.Bad_Cena2 ?? 0m,
                    OphthalmologistPrice = b.Bad_Cena3 ?? 0m,
                    SanitaryPrice = b.Bad_Cena4 ?? 0m,
                    OtherPrice = b.Bad_Cena8 ?? 0m,
                    LipidogramPrice = b.Bad_Cena5 ?? 0m,
                    EKGPrice = b.Bad_Cena6 ?? 0m,
                    HealthClinicPrice = b.Bad_Cena7 ?? 0m
                }).ToList(); ;

                NotificationHelper.ShowInfo("Generowanie pliku PDF...", "Proszę czekać");

                var number = !string.IsNullOrWhiteSpace(SelectedLista?.FK_Numer) ? SelectedLista!.FK_Numer! : (NumerFaktury ?? string.Empty);
                var company = !string.IsNullOrWhiteSpace(SelectedLista?.Nazwa) ? SelectedLista!.Nazwa! : (WybranaFirma ?? string.Empty);

                // Jeśli lista nie ma daty faktury (np. wirtualna), użyj DataWystawienia z VM
                DateTime? invoiceDate = SelectedLista?.FK_Data ?? DataWystawienia;

                // Wywołanie serwisu (zwraca ścieżkę do pliku)
                var path = _pdfService.ExportToPdf(items, exportType, number, company, invoiceDate);

                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                {
                    MessageBox.Show("Błąd podczas tworzenia pliku PDF.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Pokaż podgląd w oknie z metadanymi dla funkcji Email
                var emailAddress = _db.GetFirmaEmailById(SelectedFirmaId);
                var numerFaktury = NumerFaktury ?? string.Empty;

                var preview = new PdfPreviewWindow();
                preview.LoadFileWithMetadata(path, emailAddress, numerFaktury);
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"ExecutePrint error: {ex}");
                NotificationHelper.ShowError($"Błąd drukowania: {ex.Message}");
            }
        }


        // --- NOWA METODA: otwarcie Outlook + załączniki jeśli istnieją ---
        // używa SelectedLista.FK_Numer lub NumerFaktury oraz SelectedFirmaDto.fkemail lub GetFirmaEmailByName
        // wywoływana z komendy SendEmailCommand
        // Działa poprawnie 

        private void SendEmail()
        {
            try
            {
                // Pobierz numer faktury (pierwszy preferowany: SelectedLista.FK_Numer, potem NumerFaktury)
                string invoiceNumberRaw = SelectedLista?.FK_Numer ?? NumerFaktury ?? string.Empty;
                if (string.IsNullOrWhiteSpace(invoiceNumberRaw))
                {
                    NotificationHelper.ShowWarning("Brak numeru faktury. Wprowadź numer przed wysłaniem e-maila.");
                    //return;
                }

                // Normalizacja numeru do nazwy pliku: zamień '/' na '_' (zgodnie z wcześniejszą konwencją)
                string invoiceNumber = invoiceNumberRaw.Replace("/", "_").Trim();

                // Nazwa firmy (opcjonalna)
                string companyName = SelectedLista?.Nazwa ?? WybranaFirma ?? string.Empty;

                // Adres e-mail firmy jeśli dostępny (wczytywany jako fkemail w LoadFirmyItemsFromDb)
                string v = SelectedFirmaDto?.FkEmail?.Trim() ?? string.Empty;
                string mailTo = v;

                if (string.IsNullOrWhiteSpace(mailTo))
                {
                    // alternatywnie spróbuj pobrać e-mail z bazy (metoda GetFirmaEmailByName)
                    mailTo = "adres@domena.pl";
                }
                else
                {
                    // jeśli jest wiele adresów oddzielonych średnikami lub przecinkami, weź pierwszy
                    var separators = new char[] { ';', ',' };
                    var parts = mailTo.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                        mailTo = parts[0].Trim();
                }

                // Ścieżka z której pobieramy pliki (możesz zmienić na konfigurację)
                string exportPath = @"A:\Email\OUT";

                // Plik faktury: "FS {invoiceNumber}.pdf"
                string fakturaFile = Path.Combine(exportPath, $"FS {invoiceNumber}.pdf");

                // Szukanie pliku listy zawierającego numer faktury (różne warianty podziałów/nakładek)
                string? listaFile = null;
                if (Directory.Exists(exportPath))
                {
                    // najpierw proste wyszukiwanie po tokenie invoiceNumber
                    var files = Directory.GetFiles(exportPath, $"lista_do_{invoiceNumber}*.pdf", SearchOption.TopDirectoryOnly);
                    if (files.Length == 0)
                    {
                        // spróbuj wariantu z zamianą '_' na spację
                        var invoiceWithSpaces = invoiceNumber.Replace("_", " ");
                        files = Directory.GetFiles(exportPath, $"lista_do_{invoiceWithSpaces}*.pdf", SearchOption.TopDirectoryOnly);
                    }
                    if (files.Length > 0)
                        listaFile = files[0];
                }

                // Przygotuj temat i treść wiadomości
                string subject = "Medycyna Pracy";
                string body = $"Dzień Dobry\r\nW załączeniu przesyłamy Fakturę nr {invoiceNumberRaw}\r\noraz załączoną listę osób.\r\n\r\nSerdecznie pozdrawiam\r\nNZOZ ASMED\r\nNIP: 113 03 31 776\r\nAl. Stanów Zjednoczonych 51 pok 204\r\n22 871 44 02";

                // Debug/diagnostyka - (możesz usunąć)
                // System.Diagnostics.Debug.WriteLine($"SendEmail: faktura='{fakturaFile}', lista='{listaFile}', to='{mailTo}'");

                // Spróbuj Outlook interop (jeśli Outlook zainstalowany)
                try
                {
                    var outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic? app = Activator.CreateInstance(outlookType);
                        dynamic mail = app?.CreateItem(0); // 0 = olMailItem
                        mail.To = mailTo;
                        mail.Subject = subject;
                        mail.Body = body;
                        if (File.Exists(fakturaFile)) mail.Attachments.Add(fakturaFile);
                        if (!string.IsNullOrWhiteSpace(listaFile) && File.Exists(listaFile)) mail.Attachments.Add(listaFile);
                        mail.Display(false); // otwiera okno edycji maila w Outlook
                        return;
                    }
                }
                catch (Exception exOutlook)
                {
                    // System.Diagnostics.Debug.WriteLine($"Outlook interop failed: {exOutlook}");
                    // nie przerywamy — pójdziemy do fallbacku mailto
                }

                // Fallback: otwarcie domyślnego klienta poczty bez załączników (mailto)
                string mailtoUrl = $"mailto:{mailTo}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
                Process.Start(new ProcessStartInfo(mailtoUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"SendEmail error: {ex}");
                NotificationHelper.ShowError($"Błąd podczas otwierania e-maila: {ex.Message}");
            }
        }

        internal void SetSelectedFirmaByValues(int? firmaIdFromLista, object firmaName)
        {
            throw new NotImplementedException();
        }
    }

}
// koniec pliku
