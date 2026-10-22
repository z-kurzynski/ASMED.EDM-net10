using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models;
using ASMED.EDM.UI.Models.BadaniaNew;
using ASMED.EDM.UI.Services;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;



namespace ASMED.EDM.UI.ViewModels.Badania;

public class BadaniaNewViewModel : INotifyPropertyChanged
{
    private readonly BadaniaNewService _badaniaNewService;
    private string _filterText = string.Empty;
    private object? _selectedFilter;
    private ObservableCollection<BadaniaNewListDto> _skierowania = new();
    private ObservableCollection<BadaniaNewListDto> _allSkierowania = new();
    private BadaniaNewListDto? _selectedSkierowanie;
    private BadaniaNewDetailsDto? _selectedDetails;
    private int _skierowaniaCount;
    private ObservableCollection<CennikDto> _cennikList = new();
    private CennikDto? _selectedCennik;
    private DateTime _dataBadania; // Będzie ustawione przez setter w konstruktorze
    private DateTime _dataWaznosci; // Będzie ustawione automatycznie przez setter DataBadania
    private int _validityYears = 3; // Domyślnie 3 lata ważności

    public event PropertyChangedEventHandler? PropertyChanged;

    public BadaniaNewViewModel(DbConnectionFactory? dbFactory = null)
    {
        try
        {
            Console.WriteLine("🔍 BadaniaNewViewModel: Constructor started");

            // 📅 Inicjalizacja domyślnej daty badania
            CustomDefaultDate = DateTime.Today;
            UseCustomDefaultDate = false;

            // 📅 Ustaw DataBadania przez setter żeby automatycznie ustawić DataWaznosci
            DataBadania = DateTime.Today; // to wywoła setter i ustawi DataWaznosci na +3 lata

            // 📅 Inicjalizuj opcje miesięcy (01-12)
            InitializeValidityMonthOptions();

            // 🔧 Inicjalizuj komendy
            ClearCommand = new RelayCommand(ExecuteClearCommand);
            RefreshCommand = new RelayCommand(ExecuteRefreshCommand);
            SaveBadanieCommand = new RelayCommand(async () => await ExecuteSaveBadanieAsync());

            _badaniaNewService = new BadaniaNewService(dbFactory);
            Console.WriteLine("✅ BadaniaNewViewModel: Service created");
            LoadData();
            Console.WriteLine("✅ BadaniaNewViewModel: Constructor completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BadaniaNewViewModel constructor error: {ex.Message}");
            Console.WriteLine($"❌ Stack: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Wykonuje czyszczenie wybranych cen i reset przycisków toggle
    /// Wywoływana przez przycisk "Wyczyść"
    /// </summary>
    private void ExecuteClearCommand()
    {
        Console.WriteLine("🧹 ExecuteClearCommand - clearing selected prices and resetting toggle buttons");
        DataBadania = DateTime.Today; // to wywoła setter i ustawi DataWaznosci na +3 lata
        ClearSelectedPrices();
    }

    /// <summary>
    /// Wykonuje przeładowanie danych z bazy (lista skierowań i cenników)
    /// Wywoływana przez przycisk "🔄 Odśwież"
    /// </summary>
    private void ExecuteRefreshCommand()
    {
        Console.WriteLine("🔄 ExecuteRefreshCommand - reloading data from database");
        LoadData();
        Console.WriteLine("✅ ExecuteRefreshCommand completed");
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (_filterText != value)
            {
                _filterText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }
    }

    public object? SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (_selectedFilter != value)
            {
                _selectedFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }
    }

    public ObservableCollection<string> FilterOptions { get; } = new() { "All", "ID", "Imię", "Nazwisko", "Firma" };

    public ObservableCollection<BadaniaNewListDto> Skierowania
    {
        get => _skierowania;
        set
        {
            _skierowania = value;
            OnPropertyChanged();
            SkierowaniaCount = value?.Count ?? 0;
        }
    }

    public BadaniaNewListDto? SelectedSkierowanie
    {
        get => _selectedSkierowanie;
        set
        {
            if (_selectedSkierowanie != value)
            {
                _selectedSkierowanie = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SkierowanieInfo));

                // Po wybraniu skierowania, załaduj szczegóły dla BadaniaNewView
                if (value != null)
                {
                    LoadDetailsForSelected(value.B_ID);
                }
                else
                {
                    SelectedDetails = null;
                }
            }
        }
    }

    public BadaniaNewDetailsDto? SelectedDetails
    {
        get => _selectedDetails;
        private set
        {
            if (_selectedDetails != value)
            {
                _selectedDetails = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PacjentInfo));
                OnPropertyChanged(nameof(FirmaInfo));
                OnPropertyChanged(nameof(SkierowanieInfo));
            }
        }
    }

    // Computed properties dla bindingu w prawej kolumnie (BadaniaNewView)
    public string PacjentInfo => SelectedDetails != null
        ? $"{SelectedDetails.P_imie} {SelectedDetails.P_nazwisko}\nPESEL: {SelectedDetails.P_pesel}"
        : "Brak danych pacjenta";

    public string FirmaInfo => SelectedDetails != null
        ? $"{SelectedDetails.Firma_Nazwa}\nNIP: {SelectedDetails.Firma_NIP}\n{SelectedDetails.AdresDisplay}"
        : "Brak danych firmy";

    public string SkierowanieInfo => SelectedDetails != null
        ? $"Data: {SelectedDetails.DataBadaniaDisplay}\nTyp: {SelectedDetails.B_TypBadania ?? "---"}\nStanowisko: {SelectedDetails.B_Stanowisko ?? "---"}"
        : "Brak danych skierowania";

    public int SkierowaniaCount
    {
        get => _skierowaniaCount;
        private set
        {
            _skierowaniaCount = value;
            OnPropertyChanged();
        }
    }

    // Cennik ComboBox
    public ObservableCollection<CennikDto> CennikList
    {
        get => _cennikList;
        private set
        {
            _cennikList = value;
            OnPropertyChanged();
        }
    }

    public CennikDto? SelectedCennik
    {
        get => _selectedCennik;
        set
        {
            Console.WriteLine($"🔔 SelectedCennik SETTER called - OLD: {_selectedCennik?.bn_cennik}, NEW: {value?.bn_cennik}");

            if (_selectedCennik != value)
            {
                _selectedCennik = value;
                OnPropertyChanged();
                Console.WriteLine($"📋 Selected cennik CHANGED: {value?.bn_cennik} (ID: {value?.Identyfikator})");

                // Załaduj ceny dla wybranego cennika
                Console.WriteLine("🚀 Calling LoadCennikPrices()...");
                LoadCennikPrices();
            }
            else
            {
                Console.WriteLine("⚠️ SelectedCennik NOT changed (same value)");
            }
        }
    }

    // Wynik ComboBox
    public ObservableCollection<string> WynikOptions { get; } = new ObservableCollection<string> { "pozytywne", "negatywne" };

    private string _selectedWynik = "pozytywne"; // Domyślna wartość
    public string SelectedWynik
    {
        get => _selectedWynik;
        set
        {
            if (_selectedWynik != value)
            {
                _selectedWynik = value;
                OnPropertyChanged();
                Console.WriteLine($"📊 Selected wynik: {value}");
            }
        }
    }

    // Ceny z cennika (dynamiczne)
    private decimal? _priceBasic;
    private decimal? _priceLaryngologist;
    private decimal? _priceOphthalmologist;
    private decimal? _priceSanitary;
    private decimal? _priceLipidogram;
    private decimal? _priceEKG;
    private decimal? _priceHealthClinic;
    private decimal? _priceOther;

    // Teksty cen dla UI (z formatowaniem)
    public string PriceBasicText => _priceBasic.HasValue ? $"{_priceBasic.Value:N2} zł" : "";
    public string PriceLaryngologistText => _priceLaryngologist.HasValue ? $"{_priceLaryngologist.Value:N2} zł" : "";
    public string PriceOphthalmologistText => _priceOphthalmologist.HasValue ? $"{_priceOphthalmologist.Value:N2} zł" : "";
    public string PriceSanitaryText => _priceSanitary.HasValue ? $"{_priceSanitary.Value:N2} zł" : "";
    public string PriceLipidogramText => _priceLipidogram.HasValue ? $"{_priceLipidogram.Value:N2} zł" : "";
    public string PriceEKGText => _priceEKG.HasValue ? $"{_priceEKG.Value:N2} zł" : "";
    public string PriceHealthClinicText => _priceHealthClinic.HasValue ? $"{_priceHealthClinic.Value:N2} zł" : "";
    public string PriceOtherText => _priceOther.HasValue ? $"{_priceOther.Value:N2} zł" : "";

    // Wybrane ceny (8 pól cenowych z automatycznym przeliczaniem sumy)
    private decimal? _cena1;
    private decimal? _cena2;
    private decimal? _cena3;
    private decimal? _cena4;
    private decimal? _cena5;
    private decimal? _cena6;
    private decimal? _cena7;
    private decimal? _cena8;

    public decimal? Cena1
    {
        get => _cena1;
        set { _cena1 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); }
    }

    public decimal? Cena2
    {
        get => _cena2;
        set { _cena2 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); }
    }

    public decimal? Cena3
    {
        get => _cena3;
        set { _cena3 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); }
    }

    public decimal? Cena4
    {
        get => _cena4;
        set { _cena4 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); }
    }

    public decimal? Cena5
    {
        get => _cena5;
        set { _cena5 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); }
    }

    public decimal? Cena6
    {
        get => _cena6;
        set { _cena6 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); }
    }

    public decimal? Cena7
    {
        get => _cena7;
        set { _cena7 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); }
    }

    public decimal? Cena8
    {
        get => _cena8;
        set { _cena8 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); }
    }

    public string SumaText => $"SUMA: {((Cena1 ?? 0) + (Cena2 ?? 0) + (Cena3 ?? 0) + (Cena4 ?? 0) + (Cena5 ?? 0) + (Cena6 ?? 0) + (Cena7 ?? 0) + (Cena8 ?? 0)):0} zł";

    public ICommand? RefreshCommand { get; set; }
    public ICommand? GoToRegistrationCommand { get; set; }
    public ICommand? ClearCommand { get; set; }
    public ICommand? SaveBadanieCommand { get; set; }

    public bool UseCustomDefaultDate { get; set; }
    public DateTime? CustomDefaultDate { get; set; }

    public DateTime DataBadania
    {
        get => _dataBadania;
        set
        {
            if (_dataBadania != value)
            {
                _dataBadania = value;
                OnPropertyChanged();

                // 📅 Automatycznie ustaw DataWaznosci na +ValidityYears lat (zachowaj miesiąc z daty badania)
                DataWaznosci = value.AddYears(_validityYears);
                Console.WriteLine($"📅 DataWaznosci automatycznie ustawiona na: {DataWaznosci:dd-MM-yyyy} (+{_validityYears} lata)");

                // 📅 Odśwież etykiety selektora lat (ostatnie 2 cyfry roku)
                UpdateValidityYearLabels();
            }
        }
    }

    public DateTime DataWaznosci
    {
        get => _dataWaznosci;
        set
        {
            if (_dataWaznosci != value)
            {
                _dataWaznosci = value;
                OnPropertyChanged();
            }
        }
    }

    // 📅 Selektor lat ważności
    public int ValidityYears
    {
        get => _validityYears;
        set
        {
            if (_validityYears != value)
            {
                _validityYears = value;
                OnPropertyChanged();

                // Zmień rok w DataWaznosci, zachowaj miesiąc i dzień
                int targetYear = _dataBadania.Year + value;
                int targetMonth = _dataWaznosci.Month;
                int targetDay = _dataBadania.Day;

                // Sprawdź czy dzień istnieje w docelowym miesiącu
                int daysInTargetMonth = DateTime.DaysInMonth(targetYear, targetMonth);
                if (targetDay > daysInTargetMonth)
                {
                    targetDay = daysInTargetMonth;
                }

                DataWaznosci = new DateTime(targetYear, targetMonth, targetDay);
                Console.WriteLine($"📅 ValidityYears zmienione na {value} lat → DataWaznosci={DataWaznosci:dd-MM-yyyy}");
            }
        }
    }

    public ObservableCollection<ValidityYearItem> ValidityYearOptions { get; } = new();

    private void UpdateValidityYearLabels()
    {
        ValidityYearOptions.Clear();
        for (int i = 1; i <= 5; i++)
        {
            int targetYear = _dataBadania.Year + i;
            ValidityYearOptions.Add(new ValidityYearItem
            {
                Years = i,
                Label = targetYear.ToString()
            });
        }
        Console.WriteLine($"📅 Updated ValidityYearOptions: {string.Join(", ", ValidityYearOptions.Select(o => o.Label))}");
    }

    public class ValidityYearItem
    {
        public int Years { get; set; }
        public string? Label { get; set; }
    }

    // 📅 Selektor miesięcy - odczytuje i ustawia miesiąc z DataWaznosci
    public int ValidityMonth
    {
        get => _dataWaznosci.Month; // Odczytaj miesiąc z daty ważności
        set
        {
            if (_dataWaznosci.Month != value)
            {
                // Zmień tylko miesiąc w DataWaznosci, zachowuj rok i dzień
                int targetYear = _dataWaznosci.Year;
                int targetMonth = value;
                int targetDay = _dataWaznosci.Day;

                // Sprawdź czy dzień istnieje w docelowym miesiącu (np. 31 stycznia → luty)
                int daysInTargetMonth = DateTime.DaysInMonth(targetYear, targetMonth);
                if (targetDay > daysInTargetMonth)
                {
                    targetDay = daysInTargetMonth;
                }

                DataWaznosci = new DateTime(targetYear, targetMonth, targetDay);
                OnPropertyChanged();
                Console.WriteLine($"📅 ValidityMonth zmienione na {value} → DataWaznosci={DataWaznosci:dd-MM-yyyy}");
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
                Label = i.ToString("D2") // 01, 02, ..., 12
            });
        }
    }

    public class ValidityMonthItem
    {
        public int Month { get; set; }
        public string? Label { get; set; }
    }

    private void LoadData()
    {
        try
        {
            Console.WriteLine("🔄 BadaniaNewViewModel: LoadData started");

            // KROK 1: Najpierw ładujemy cennik (WYMAGANE przed listą badań)
            Console.WriteLine("📋 Step 1: Loading cennik...");
            var cennikData = _badaniaNewService.GetCennikList();
            CennikList = new ObservableCollection<CennikDto>(cennikData);
            Console.WriteLine($"✅ Loaded {cennikData.Count} cennik entries");

            // KROK 2: Ładujemy badania do zamknięcia
            Console.WriteLine("📋 Step 2: Loading badania...");
            var badania = _badaniaNewService.GetBadaniaDoZamkniecia();
            _allSkierowania = new ObservableCollection<BadaniaNewListDto>(badania);
            Skierowania = new ObservableCollection<BadaniaNewListDto>(badania);

            Console.WriteLine($"✅ Załadowano {badania.Count} badań do zamknięcia (BadaniaNewView)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading badania: {ex.Message}");
            Console.WriteLine($"❌ Stack: {ex.StackTrace}");
        }
    }

    private void LoadDetailsForSelected(int bSkierowaniaId)
    {
        try
        {
            Console.WriteLine($"🔍 BadaniaNewViewModel: Loading details for B_ID={bSkierowaniaId}");
            var details = _badaniaNewService.GetBadanieDetails(bSkierowaniaId);
            SelectedDetails = details;

            if (details != null)
            {
                Console.WriteLine($"✅ Loaded details for {details.P_imie} {details.P_nazwisko} from {details.Firma_Nazwa}");
                Console.WriteLine($"📋 Firma_Cennik from DB: '{details.Firma_Cennik}'");

                // AUTO-WYBÓR CENNIKA - szukamy obiektu w CennikList o bn_cennik == Firma_Cennik
                if (!string.IsNullOrEmpty(details.Firma_Cennik))
                {
                    Console.WriteLine($"🔍 Searching for cennik: '{details.Firma_Cennik}' in CennikList ({CennikList.Count} items)");

                    var found = CennikList.FirstOrDefault(c => c.bn_cennik == details.Firma_Cennik);

                    if (found != null)
                    {
                        SelectedCennik = found;
                        Console.WriteLine($"✅ Auto-selected cennik: '{found.bn_cennik}' (ID={found.Identyfikator})");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Cennik '{details.Firma_Cennik}' NOT FOUND in CennikList");
                        Console.WriteLine($"   Available cenniks:");
                        foreach (var c in CennikList)
                        {
                            Console.WriteLine($"      - '{c.bn_cennik}'");
                        }
                        SelectedCennik = null;
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Firma_Cennik is NULL or EMPTY");
                    SelectedCennik = null;
                }

                // 📊 Zresetuj wynik do domyślnej wartości
                SelectedWynik = "pozytywne";

                // 📅 Ustaw Data Badania: CustomDefaultDate (jeśli checkbox zaznaczony) lub dzisiaj
                // 📅 Zresetuj selektor lat do domyślnej wartości (3 lata)
                _validityYears = 3;
                OnPropertyChanged(nameof(ValidityYears));

                DateTime targetDate;
                if (UseCustomDefaultDate && CustomDefaultDate.HasValue)
                {
                    targetDate = CustomDefaultDate.Value;
                    Console.WriteLine($"📅 Data Badania ustawiona na: {targetDate:dd-MM-yyyy} (custom date)");
                }
                else
                {
                    targetDate = DateTime.Today;
                    Console.WriteLine($"📅 Data Badania ustawiona na: {targetDate:dd-MM-yyyy} (dzisiaj)");
                }

                // 📅 Wymuś przeliczenie dat nawet jeśli DataBadania == targetDate
                _dataBadania = targetDate;
                OnPropertyChanged(nameof(DataBadania));
                DataWaznosci = targetDate.AddYears(_validityYears);

                // 📅 Odśwież selektory (lat i miesięcy)
                UpdateValidityYearLabels();
                OnPropertyChanged(nameof(ValidityMonth)); // Wymuś odświeżenie selektora miesięcy

                // 🧹 Wyczyść wybrane ceny i zresetuj przyciski toggle
                ClearSelectedPrices();

                // 🔗 Powiąż przyciski toggle ze statusem zapisanym w B_Skierowania:
                // "Sanitary" (Książeczka) ↔ B_ksiazeczka, "HealthClinic" (Urlop) ↔ B_Zaswiadczenie
                var sanitaryPrice = GetPriceForExamination("Sanitary");
                Cena4 = (details.B_ksiazeczka == true && sanitaryPrice > 0) ? sanitaryPrice : null;

                var healthClinicPrice = GetPriceForExamination("HealthClinic");
                Cena7 = (details.B_Zaswiadczenie == true && healthClinicPrice > 0) ? healthClinicPrice : null;

                // "Laryngologist" ↔ B_laryngolog, "Ophthalmologist" ↔ B_okulista,
                // "EKG" ↔ B_EKG, "Lipidogram" ↔ B_Lipidogram
                var laryngologPrice = GetPriceForExamination("Laryngologist");
                Cena2 = (details.B_laryngolog == true && laryngologPrice > 0) ? laryngologPrice : null;

                var okulistaPrice = GetPriceForExamination("Ophthalmologist");
                Cena3 = (details.B_okulista == true && okulistaPrice > 0) ? okulistaPrice : null;

                var lipidogramPrice = GetPriceForExamination("Lipidogram");
                Cena5 = (details.B_Lipidogram == true && lipidogramPrice > 0) ? lipidogramPrice : null;

                var ekgPrice = GetPriceForExamination("EKG");
                Cena6 = (details.B_EKG == true && ekgPrice > 0) ? ekgPrice : null;

                Console.WriteLine($"📅 Reset complete: DataBadania={DataBadania:dd-MM-yyyy}, DataWaznosci={DataWaznosci:dd-MM-yyyy}, ValidityYears={_validityYears}, ValidityMonth={DataWaznosci.Month}");
            }
            else
            {
                Console.WriteLine($"⚠️ No details found for B_ID={bSkierowaniaId}");
                SelectedCennik = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading details: {ex.Message}");
            Console.WriteLine($"❌ Stack: {ex.StackTrace}");
            SelectedDetails = null;
            SelectedCennik = null;
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            Skierowania = new ObservableCollection<BadaniaNewListDto>(_allSkierowania);
            System.Diagnostics.Debug.WriteLine($"🔍 Filter cleared - showing all {_allSkierowania.Count} items");
            return;
        }

        var filterLower = FilterText.ToLowerInvariant();

        // Smart filter: #ID dla dokładnego ID skierowania
        if (filterLower.StartsWith("#"))
        {
            var idText = filterLower.Substring(1);
            if (int.TryParse(idText, out int searchId))
            {
                var filtered = _allSkierowania.Where(s => s.B_ID == searchId).ToList();
                Skierowania = new ObservableCollection<BadaniaNewListDto>(filtered);
                System.Diagnostics.Debug.WriteLine($"🔍 ID filter #{searchId} - showing {filtered.Count} items");
                return;
            }
        }

        // Zwykły filtr tekstowy
        var filteredByText = _allSkierowania.Where(s =>
        {
            var selectedOption = SelectedFilter?.ToString();

            if (selectedOption == "ID")
            {
                return s.B_ID.ToString().Contains(filterLower);
            }
            else if (selectedOption == "Imię")
            {
                return s.P_imie?.ToLowerInvariant().Contains(filterLower) == true;
            }
            else if (selectedOption == "Nazwisko")
            {
                return s.P_nazwisko?.ToLowerInvariant().Contains(filterLower) == true;
            }
            else if (selectedOption == "Firma")
            {
                return s.Firma_Nazwa?.ToLowerInvariant().Contains(filterLower) == true;
            }
            else // "All"
            {
                return s.B_ID.ToString().Contains(filterLower) ||
                       s.P_imie?.ToLowerInvariant().Contains(filterLower) == true ||
                       s.P_nazwisko?.ToLowerInvariant().Contains(filterLower) == true ||
                       s.Firma_Nazwa?.ToLowerInvariant().Contains(filterLower) == true;
            }
        }).ToList();

        Skierowania = new ObservableCollection<BadaniaNewListDto>(filteredByText);
        System.Diagnostics.Debug.WriteLine($"🔍 Filter '{FilterText}' applied - showing {filteredByText.Count} of {_allSkierowania.Count} items");
    }

    public void RecalculateTotalPrice()
    {
        // TODO: Implementacja gdy będą pola cen w SelectedDetails
        // Na razie placeholder
    }

    /// <summary>
    /// Pobiera cenę z cennika dla danego badania (używane przez toggle buttons w code-behind)
    /// </summary>
    public decimal GetPriceForExamination(string examinationType)
    {
        return examinationType switch
        {
            "Basic" => _priceBasic ?? 0m,
            "Laryngologist" => _priceLaryngologist ?? 0m,
            "Ophthalmologist" => _priceOphthalmologist ?? 0m,
            "Sanitary" => _priceSanitary ?? 0m,
            "Lipidogram" => _priceLipidogram ?? 0m,
            "EKG" => _priceEKG ?? 0m,
            "HealthClinic" => _priceHealthClinic ?? 0m,
            "Other" => _priceOther ?? 0m,
            _ => 0m
        };
    }

    /// <summary>
    /// Ustawia cenę dla danego typu badania (wywoływane przez toggle buttons w code-behind)
    /// </summary>
    public void SetCenaForExaminationType(string examinationType, decimal? value)
    {
        switch (examinationType)
        {
            case "Basic": Cena1 = value; break;
            case "Laryngologist": Cena2 = value; break;
            case "Ophthalmologist": Cena3 = value; break;
            case "Sanitary": Cena4 = value; break;
            case "Lipidogram": Cena5 = value; break;
            case "EKG": Cena6 = value; break;
            case "HealthClinic": Cena7 = value; break;
            case "Other": Cena8 = value; break;
        }
    }

    /// <summary>
    /// Ustawia status pola B_ksiazeczka w SelectedDetails (powiązane z przyciskiem "Sanitary"/Książeczka)
    /// i wymusza odświeżenie bindingu Status_Info (elipsa Książeczka).
    /// </summary>
    public void SetKsiazeczkaStatus(bool value)
    {
        if (SelectedDetails == null) return;
        SelectedDetails.B_ksiazeczka = value;
        OnPropertyChanged(nameof(SelectedDetails));
        _badaniaNewService.UpdateBSkierowaniaFlag(SelectedDetails.B_ID, "B_ksiazeczka", value);
    }

    /// <summary>
    /// Ustawia status pola B_Zaswiadczenie w SelectedDetails (powiązane z przyciskiem "HealthClinic"/Urlop)
    /// i wymusza odświeżenie bindingu Status_Info (elipsa Urlop).
    /// </summary>
    public void SetZaswiadczenieStatus(bool value)
    {
        if (SelectedDetails == null) return;
        SelectedDetails.B_Zaswiadczenie = value;
        OnPropertyChanged(nameof(SelectedDetails));
        _badaniaNewService.UpdateBSkierowaniaFlag(SelectedDetails.B_ID, "B_Zaswiadczenie", value);
    }

    /// <summary>
    /// Ustawia status pola B_laryngolog w SelectedDetails (powiązane z przyciskiem "Laryngologist").
    /// </summary>
    public void SetLaryngologStatus(bool value)
    {
        if (SelectedDetails == null) return;
        SelectedDetails.B_laryngolog = value;
        OnPropertyChanged(nameof(SelectedDetails));
        _badaniaNewService.UpdateBSkierowaniaFlag(SelectedDetails.B_ID, "B_laryngolog", value);
    }

    /// <summary>
    /// Ustawia status pola B_okulista w SelectedDetails (powiązane z przyciskiem "Ophthalmologist").
    /// </summary>
    public void SetOkulistaStatus(bool value)
    {
        if (SelectedDetails == null) return;
        SelectedDetails.B_okulista = value;
        OnPropertyChanged(nameof(SelectedDetails));
        _badaniaNewService.UpdateBSkierowaniaFlag(SelectedDetails.B_ID, "B_okulista", value);
    }

    /// <summary>
    /// Ustawia status pola B_EKG w SelectedDetails (powiązane z przyciskiem "EKG").
    /// </summary>
    public void SetEKGStatus(bool value)
    {
        if (SelectedDetails == null) return;
        SelectedDetails.B_EKG = value;
        OnPropertyChanged(nameof(SelectedDetails));
        _badaniaNewService.UpdateBSkierowaniaFlag(SelectedDetails.B_ID, "B_EKG", value);
    }

    /// <summary>
    /// Ustawia status pola B_Lipidogram w SelectedDetails (powiązane z przyciskiem "Lipidogram").
    /// </summary>
    public void SetLipidogramStatus(bool value)
    {
        if (SelectedDetails == null) return;
        SelectedDetails.B_Lipidogram = value;
        OnPropertyChanged(nameof(SelectedDetails));
        _badaniaNewService.UpdateBSkierowaniaFlag(SelectedDetails.B_ID, "B_Lipidogram", value);
    }

    public void ToggleExamination(string examType)
    {
        // Ta metoda jest wywoływana z code-behind (BadaniaNewView.xaml.cs)
        // ale faktyczna logika toggle (zmiana koloru, tekstu, ceny) jest w code-behind
        // tutaj tylko logujemy dla celów debugowania
        System.Diagnostics.Debug.WriteLine($"🔘 Toggle examination: {examType}");
    }

    /// <summary>
    /// Ładuje ceny z wybranego cennika do pól _priceXXX
    /// Mapuje dynamiczne kolumny z BAD_Lista na konkretne pola
    /// </summary>
    private void LoadCennikPrices()
    {
        try
        {
            Console.WriteLine($"🔄 LoadCennikPrices STARTED - SelectedCennik: {SelectedCennik?.bn_cennik}");

            if (SelectedCennik == null || string.IsNullOrEmpty(SelectedCennik.bn_cennik))
            {
                Console.WriteLine("⚠️ SelectedCennik is NULL or empty - clearing prices");
                ClearPrices();
                return;
            }

            Console.WriteLine($"📞 Calling _badaniaNewService.GetCennikPrices('{SelectedCennik.bn_cennik}')");
            var prices = _badaniaNewService.GetCennikPrices(SelectedCennik.bn_cennik);

            if (prices == null)
            {
                Console.WriteLine($"❌ GetCennikPrices returned NULL for cennik '{SelectedCennik.bn_cennik}'");
                ClearPrices();
                return;
            }

            if (prices.Count == 0)
            {
                Console.WriteLine($"⚠️ GetCennikPrices returned EMPTY dictionary for cennik '{SelectedCennik.bn_cennik}'");
                ClearPrices();
                return;
            }

            // Pokaż wszystkie odczytane kolumny
            var pricesInfo = string.Join("\n", prices.Select(p => $"{p.Key}: {p.Value:N2} zł"));
            Console.WriteLine($"✅ Loaded {prices.Count} prices from database:\n{pricesInfo}");

            // Mapowanie dynamicznych kolumn na pola (podobnie jak w legacy kodzie)
            decimal? GetPrice(string[] keywords)
            {
                foreach (var keyword in keywords)
                {
                    var matchingKey = prices.Keys.FirstOrDefault(dbKey =>
                        dbKey.ToLower().Contains(keyword.ToLower()) ||
                        dbKey.ToLower().Replace(".", "").Contains(keyword.ToLower()));

                    if (matchingKey != null && prices.TryGetValue(matchingKey, out var value))
                    {
                        Console.WriteLine($"  ✅ Mapped '{keyword}' → '{matchingKey}' = {value:N2} zł");
                        return value;
                    }
                }
                Console.WriteLine($"  ⚠️ No match for keywords: {string.Join(", ", keywords)}");
                return null;
            }

            _priceBasic = GetPrice(new[] { "lekarz", "lekasz", "MP", "basic" });
            _priceLaryngologist = GetPrice(new[] { "laryngolog" });
            _priceOphthalmologist = GetPrice(new[] { "okulista", "okulist" });
            _priceSanitary = GetPrice(new[] { "ksi", "książeczka", "ksiazeczka" });
            _priceLipidogram = GetPrice(new[] { "lipidogram" });
            _priceEKG = GetPrice(new[] { "ekg" });
            _priceHealthClinic = GetPrice(new[] { "urlop", "zdrowie", "healthclinic" });
            _priceOther = GetPrice(new[] { "inne", "other" });

            // Pokaż zmapowane ceny
            var mappedInfo = $"Basic: {_priceBasic?.ToString("N2") ?? "NULL"}\n" +
                            $"Laryngologist: {_priceLaryngologist?.ToString("N2") ?? "NULL"}\n" +
                            $"Ophthalmologist: {_priceOphthalmologist?.ToString("N2") ?? "NULL"}\n" +
                            $"Sanitary: {_priceSanitary?.ToString("N2") ?? "NULL"}\n" +
                            $"Lipidogram: {_priceLipidogram?.ToString("N2") ?? "NULL"}\n" +
                            $"EKG: {_priceEKG?.ToString("N2") ?? "NULL"}\n" +
                            $"HealthClinic: {_priceHealthClinic?.ToString("N2") ?? "NULL"}\n" +
                            $"Other: {_priceOther?.ToString("N2") ?? "NULL"}";

            Console.WriteLine($"💰 Mapped prices:\n{mappedInfo}");

            // Odśwież wszystkie teksty cen w UI
            Console.WriteLine("🔄 Calling RefreshPriceTexts()");
            RefreshPriceTexts();
            Console.WriteLine("✅ LoadCennikPrices COMPLETED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ LoadCennikPrices error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Czyści wybrane ceny (Cena1-Cena8) i sygnalizuje do UI konieczność resetu przycisków toggle
    /// </summary>
    public void ClearSelectedPrices()
    {
        Console.WriteLine("🧹 ClearSelectedPrices - clearing Cena1-Cena8");
        Cena1 = null;
        Cena2 = null;
        Cena3 = null;
        Cena4 = null;
        Cena5 = null;
        Cena6 = null;
        Cena7 = null;
        Cena8 = null;

        // Sygnalizuj do UI konieczność resetu przycisków
        ToggleButtonsResetRequested?.Invoke();
        Console.WriteLine("✅ ClearSelectedPrices completed, SumaText=" + SumaText);
    }

    /// <summary>
    /// Event wywoływany gdy trzeba zresetować przyciski toggle do stanu NIEAKTYWNE
    /// Code-behind BadaniaNewView.xaml.cs nasłuchuje tego eventu
    /// </summary>
    public event Action? ToggleButtonsResetRequested;

    private void ClearPrices()
    {
        _priceBasic = null;
        _priceLaryngologist = null;
        _priceOphthalmologist = null;
        _priceSanitary = null;
        _priceLipidogram = null;
        _priceEKG = null;
        _priceHealthClinic = null;
        _priceOther = null;
        RefreshPriceTexts();
    }

    private void RefreshPriceTexts()
    {
        Console.WriteLine("🔄 RefreshPriceTexts() - firing OnPropertyChanged for all price properties");
        OnPropertyChanged(nameof(PriceBasicText));
        OnPropertyChanged(nameof(PriceLaryngologistText));
        OnPropertyChanged(nameof(PriceOphthalmologistText));
        OnPropertyChanged(nameof(PriceSanitaryText));
        OnPropertyChanged(nameof(PriceLipidogramText));
        OnPropertyChanged(nameof(PriceEKGText));
        OnPropertyChanged(nameof(PriceHealthClinicText));
        OnPropertyChanged(nameof(PriceOtherText));

        // Weryfikacja wartości property getterów
        Console.WriteLine($"  PriceBasicText = '{PriceBasicText}'");
        Console.WriteLine($"  PriceLaryngologistText = '{PriceLaryngologistText}'");
        Console.WriteLine($"  PriceOphthalmologistText = '{PriceOphthalmologistText}'");
        Console.WriteLine($"  PriceSanitaryText = '{PriceSanitaryText}'");
        Console.WriteLine($"  PriceLipidogramText = '{PriceLipidogramText}'");
        Console.WriteLine($"  PriceEKGText = '{PriceEKGText}'");
        Console.WriteLine($"  PriceHealthClinicText = '{PriceHealthClinicText}'");
        Console.WriteLine($"  PriceOtherText = '{PriceOtherText}'");
    }

    #region === SAVE BADANIE ===

    /// <summary>
    /// Wykonuje pełny zapis badania do bazy danych
    /// Transakcja obejmuje: INSERT Badanie, UPDATE Skierowania (link + data), UPDATE/INSERT Rejestracja
    /// </summary>
    private async Task ExecuteSaveBadanieAsync()
    {
        try
        {
            Console.WriteLine("💾 ExecuteSaveBadanieAsync - START");

            // === WALIDACJA ===
            if (SelectedSkierowanie == null)
            {
                MessageBox.Show("Wybierz skierowanie z listy.", "Brak skierowania", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(SelectedCennik?.bn_cennik))
            {
                MessageBox.Show("Wybierz cennik.", "Brak cennika", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Console.WriteLine($"✅ Validation OK: B_ID={SelectedSkierowanie.B_ID}, Cennik={SelectedCennik.bn_cennik}, DataBadania={DataBadania:yyyy-MM-dd}");

            // === KROK 1: Uzupełnij brakujące daty (sprawdź B_DataSkierowania == null) ===
            await UzupelnijBrakujaceDaty();

            // === KROK 2: INSERT Badanie ===
            var badanieDto = new BadanieInsertDto
            {
                Bad_S_ID = SelectedSkierowanie.B_ID,
                Bad_P_ID = SelectedSkierowanie.P_ID,
                Bad_F_ID = SelectedSkierowanie.FirmaId ?? 0, // ⚠️ 0 jeśli NULL
                Bad_bn_cennik = SelectedCennik.bn_cennik,
                Bad_Typ = SelectedDetails?.B_TypBadania,
                Bad_Data = DataBadania,
                Bad_Data_Do = DataWaznosci,
                Bad_Wynik = SelectedWynik,
                Bad_Cena1 = Cena1 ?? 0m, // ⚠️ NULL → 0
                Bad_Cena2 = Cena2 ?? 0m,
                Bad_Cena3 = Cena3 ?? 0m,
                Bad_Cena4 = Cena4 ?? 0m,
                Bad_Cena5 = Cena5 ?? 0m,
                Bad_Cena6 = Cena6 ?? 0m,
                Bad_Cena7 = Cena7 ?? 0m,
                Bad_Cena8 = Cena8 ?? 0m,
                Bad_Razem = (Cena1 ?? 0) + (Cena2 ?? 0) + (Cena3 ?? 0) + (Cena4 ?? 0) + (Cena5 ?? 0) + (Cena6 ?? 0) + (Cena7 ?? 0) + (Cena8 ?? 0),
                Bad_Nr_KS = null, // Nie mamy NrKsiegi w tym ViewModelu
                Bad_END = false
            };

            int newBadId = await _badaniaNewService.InsertBadanieAsync(badanieDto);
            Console.WriteLine($"✅ KROK 2 OK: Badanie saved with Bad_ID={newBadId}");

            // === KROK 3: UPDATE link Skierowanie → Badanie ===
            bool linkUpdated = await _badaniaNewService.UpdateSkierowanieBadanieIdAsync(SelectedSkierowanie.B_ID, newBadId);
            if (!linkUpdated)
            {
                MessageBox.Show($"⚠️ Badanie zapisane (Bad_ID={newBadId}), ale nie udało się zlinkować skierowania.", "Ostrzeżenie", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                Console.WriteLine($"✅ KROK 3 OK: Skierowanie B_ID={SelectedSkierowanie.B_ID} → Bad_ID={newBadId}");
            }

            // === KROK 4: UPDATE status wizyty (Rejestracja) na "zamknięta" ===
            await ZmienStatusWizytyNaOdbyta(SelectedSkierowanie.B_ID);

            // === SUKCES - Windows Toast Notification ===
            ToastHelper.ShowWindowsToast(
                "✅ Badanie zapisane pomyślnie!",
                $"Bad_ID: {newBadId} | Pacjent: {SelectedSkierowanie.P_imie} {SelectedSkierowanie.P_nazwisko}");

            // === REFRESH + CLEAR ===
            ExecuteRefreshCommand();
            ClearSelectedPrices();
            Console.WriteLine("💾 ExecuteSaveBadanieAsync - COMPLETE");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ExecuteSaveBadanieAsync ERROR: {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show($"Błąd podczas zapisu badania:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// KROK 1: Walidacja i uzupełnienie danych
    /// - Jeśli B_DataSkierowania == null → UPDATE na DataBadania
    /// - Jeśli Rejestracja nie istnieje → INSERT z statusem "zamknięta"
    /// </summary>
    private async Task UzupelnijBrakujaceDaty()
    {
        if (SelectedSkierowanie == null || SelectedDetails == null)
            return;

        Console.WriteLine("🔍 UzupelnijBrakujaceDaty - checking B_DataSkierowania...");

        // === Sprawdź czy B_DataSkierowania jest NULL ===
        if (!SelectedDetails.B_DataSkierowania.HasValue)
        {
            Console.WriteLine("⚠️ B_DataSkierowania is NULL → updating to DataBadania");

            var updateDto = new SkierowanieUpdateDto
            {
                PacjentId = SelectedSkierowanie.P_ID,
                FirmaId = SelectedSkierowanie.FirmaId,
                TypBadania = SelectedDetails.B_TypBadania,
                Stanowisko = SelectedSkierowanie.P_zawod,
                Zaswiadczenie = SelectedDetails.B_Zaswiadczenie ?? false,
                Ksiazeczka = SelectedDetails.B_ksiazeczka ?? false
            };

            bool updated = await _badaniaNewService.UpdateSkierowanieDataSkierowaniaAsync(
                SelectedSkierowanie.B_ID,
                DataBadania,
                updateDto
            );

            if (updated)
            {
                Console.WriteLine($"✅ Updated B_DataSkierowania to {DataBadania:yyyy-MM-dd}");
            }
            else
            {
                Console.WriteLine("⚠️ Failed to update B_DataSkierowania");
            }
        }
        else
        {
            Console.WriteLine($"✅ B_DataSkierowania already set: {SelectedDetails.B_DataSkierowania:yyyy-MM-dd}");
        }

        // === Sprawdź czy Rejestracja istnieje ===
        var rejestracja = await _badaniaNewService.GetRejestracjaBySIdAsync(SelectedSkierowanie.B_ID);

        if (rejestracja == null)
        {
            Console.WriteLine("⚠️ Rejestracja NOT FOUND → creating new with status 'zamknięta'");

            var insertDto = new RejestracjaInsertDto
            {
                R_Data = DataBadania,
                R_Status = "zamknięta",
                R_S_ID = SelectedSkierowanie.B_ID,
                R_GG_MM = DataBadania,
                R_Subject = $"{SelectedSkierowanie.P_imie} {SelectedSkierowanie.P_nazwisko}",
                R_Uwagi = "Rekord uzupełniony automatycznie przy zapisie badania"
            };

            bool inserted = await _badaniaNewService.InsertRejestracjaAsync(insertDto);

            if (inserted)
            {
                Console.WriteLine($"✅ Created Rejestracja for R_S_ID={SelectedSkierowanie.B_ID}");
            }
            else
            {
                Console.WriteLine("⚠️ Failed to create Rejestracja");
            }
        }
        else
        {
            Console.WriteLine($"✅ Rejestracja already exists: R_ID={rejestracja.R_ID}");
        }
    }

    /// <summary>
    /// KROK 4: Zmień status Rejestracji na "zamknięta"
    /// </summary>
    private async Task ZmienStatusWizytyNaOdbyta(int bId)
    {
        Console.WriteLine($"🔍 ZmienStatusWizytyNaOdbyta - checking Rejestracja for R_S_ID={bId}");

        var rejestracja = await _badaniaNewService.GetRejestracjaBySIdAsync(bId);

        if (rejestracja != null && rejestracja.R_ID.HasValue)
        {
            Console.WriteLine($"🔄 Updating Rejestracja R_ID={rejestracja.R_ID} to status='zamknięta'");

            bool updated = await _badaniaNewService.UpdateRejestracjaStatusAsync(rejestracja.R_ID.Value, "zamknięta");

            if (updated)
            {
                Console.WriteLine($"✅ Rejestracja R_ID={rejestracja.R_ID} status changed to 'zamknięta'");
            }
            else
            {
                Console.WriteLine($"⚠️ Failed to update Rejestracja R_ID={rejestracja.R_ID}");
            }
        }
        else
        {
            Console.WriteLine($"ℹ️ No Rejestracja found for R_S_ID={bId} (or R_ID is NULL)");
        }
    }

    #endregion

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal class ToastContentBuilder
{
    public ToastContentBuilder()
    {
    }
}

/// <summary>
/// Helper class dla ICommand - prosta implementacja RelayCommand
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    public void Execute(object? parameter)
    {
        _execute();
    }
}

/// <summary>
/// Helper do wyświetlania Windows Toast Notifications
/// </summary>
internal static class ToastHelper
{
    private const string AppId = "ASMED.EDM";

    public static void ShowWindowsToast(string title, string message)
    {
        try
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var textNodes = toastXml.GetElementsByTagName("text");
            textNodes[0].AppendChild(toastXml.CreateTextNode(title));
            textNodes[1].AppendChild(toastXml.CreateTextNode(message));

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Toast notification error: {ex.Message}");
        }
    }
}
