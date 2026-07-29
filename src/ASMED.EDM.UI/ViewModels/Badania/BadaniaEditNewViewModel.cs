using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models.BadaniaEdit;
using ASMED.EDM.UI.Models.BadaniaNew;
using ASMED.EDM.UI.Services;

namespace ASMED.EDM.UI.ViewModels.Badania;

public class BadaniaEditNewViewModel : INotifyPropertyChanged
{
    private readonly BadaniaEditService _badaniaEditService;
    private string _filterText = string.Empty;
    private object? _selectedFilter;
    private ObservableCollection<BadaniaEditListDto> _skierowania = new();
    private ObservableCollection<BadaniaEditListDto> _allSkierowania = new();
    private BadaniaEditListDto? _selectedSkierowanie;
    private BadaniaEditDetailsDto? _selectedDetails;
    private int _skierowaniaCount;

    // Cennik
    private ObservableCollection<CennikDto> _cennikList = new();
    private CennikDto? _selectedCennik;

    // Daty
    private DateTime _dataBadania;
    private DateTime _dataWaznosci;
    private int _validityYears = 3;

    // Wynik
    private string _selectedWynik = "pozytywne";

    // Nr Ksiegi
    private string? _nrKsiegi;

    // Debug / Status - widoczny w UI
    private string _statusMessage = "--- brak zaznaczenia ---";
    private string _debugInfo = string.Empty;

    // Ceny z cennika
    private decimal? _priceBasic;
    private decimal? _priceLaryngologist;
    private decimal? _priceOphthalmologist;
    private decimal? _priceSanitary;
    private decimal? _priceLipidogram;
    private decimal? _priceEKG;
    private decimal? _priceHealthClinic;
    private decimal? _priceOther;

    // Wybrane ceny
    private decimal? _cena1;
    private decimal? _cena2;
    private decimal? _cena3;
    private decimal? _cena4;
    private decimal? _cena5;
    private decimal? _cena6;
    private decimal? _cena7;
    private decimal? _cena8;

    public event PropertyChangedEventHandler? PropertyChanged;

    public BadaniaEditNewViewModel(DbConnectionFactory? dbFactory = null)
    {
        try
        {
            Console.WriteLine("[EDIT VM] Constructor started");
            _badaniaEditService = new BadaniaEditService(dbFactory);

            _dataBadania = DateTime.Today;
            _dataWaznosci = DateTime.Today.AddYears(3);
            InitializeValidityMonthOptions();
            UpdateValidityYearLabels();

            ClearCommand = new RelayCommand(ExecuteClearCommand);
            RefreshCommand = new RelayCommand(ExecuteRefreshCommand);
            SaveBadanieCommand = new RelayCommand(() => { /* TODO */ });

            LoadData();
            Console.WriteLine("[EDIT VM] Constructor completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EDIT VM] Constructor failed: {ex.Message}");
            throw;
        }
    }

    // ---- Filtr ----
    public string FilterText
    {
        get => _filterText;
        set { if (_filterText != value) { _filterText = value; OnPropertyChanged(); ApplyFilter(); } }
    }

    public object? SelectedFilter
    {
        get => _selectedFilter;
        set { if (_selectedFilter != value) { _selectedFilter = value; OnPropertyChanged(); ApplyFilter(); } }
    }

    // ---- Lista skierowania ----
    public ObservableCollection<BadaniaEditListDto> Skierowania
    {
        get => _skierowania;
        set { _skierowania = value; OnPropertyChanged(); SkierowaniaCount = value?.Count ?? 0; }
    }

    public int SkierowaniaCount
    {
        get => _skierowaniaCount;
        private set { _skierowaniaCount = value; OnPropertyChanged(); }
    }

    // ---- Wybrane skierowanie ----
    public BadaniaEditListDto? SelectedSkierowanie
    {
        get => _selectedSkierowanie;
        set
        {
            if (_selectedSkierowanie != value)
            {
                _selectedSkierowanie = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SkierowanieInfo));

                if (value != null)
                    LoadDetailsForSelected(value.B_ID);
                else
                {
                    SelectedDetails = null;
                    StatusMessage = "--- brak zaznaczenia ---";
                    DebugInfo = string.Empty;
                }
            }
        }
    }

    // ---- Szczegoly (prawa kolumna) ----
    public BadaniaEditDetailsDto? SelectedDetails
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
                OnPropertyChanged(nameof(BadanieInfo));
            }
        }
    }

    public string PacjentInfo => SelectedDetails != null
        ? $"{SelectedDetails.P_imie} {SelectedDetails.P_nazwisko}"
        : "Brak danych pacjenta";

    public string FirmaInfo => SelectedDetails?.Firma_Nazwa ?? "Brak danych firmy";

    public string BadanieInfo => SelectedDetails != null
        ? $"Badanie: {SelectedDetails.Bad_Data:dd-MM-yyyy}"
        : "Brak danych badania";

    public string SkierowanieInfo => SelectedSkierowanie != null
        ? $"Badanie ID: {SelectedSkierowanie.B_ID} - {SelectedSkierowanie.DataBadaniaDisplay}"
        : "Wybierz badanie";

    // ---- Status / Debug widoczny w UI ----
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string DebugInfo
    {
        get => _debugInfo;
        set { _debugInfo = value; OnPropertyChanged(); }
    }

    // ---- Cennik ----
    public ObservableCollection<CennikDto> CennikList
    {
        get => _cennikList;
        private set { _cennikList = value; OnPropertyChanged(); }
    }

    public CennikDto? SelectedCennik
    {
        get => _selectedCennik;
        set
        {
            if (_selectedCennik != value)
            {
                _selectedCennik = value;
                OnPropertyChanged();
                LoadCennikPrices();
            }
        }
    }

    // ---- Wybrane ceny (edytowalne) ----
    public decimal? Cena1 { get => _cena1; set { _cena1 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); } }
    public decimal? Cena2 { get => _cena2; set { _cena2 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); } }
    public decimal? Cena3 { get => _cena3; set { _cena3 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); } }
    public decimal? Cena4 { get => _cena4; set { _cena4 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); } }
    public decimal? Cena5 { get => _cena5; set { _cena5 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); } }
    public decimal? Cena6 { get => _cena6; set { _cena6 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); } }
    public decimal? Cena7 { get => _cena7; set { _cena7 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); } }
    public decimal? Cena8 { get => _cena8; set { _cena8 = value; OnPropertyChanged(); OnPropertyChanged(nameof(SumaText)); } }

    public string SumaText => $"SUMA: {((Cena1 ?? 0) + (Cena2 ?? 0) + (Cena3 ?? 0) + (Cena4 ?? 0) + (Cena5 ?? 0) + (Cena6 ?? 0) + (Cena7 ?? 0) + (Cena8 ?? 0)):0} zl";

    // ---- Daty ----
    public DateTime DataBadania
    {
        get => _dataBadania;
        set
        {
            if (_dataBadania != value)
            {
                _dataBadania = value;
                OnPropertyChanged();
                DataWaznosci = value.AddYears(_validityYears);
                UpdateValidityYearLabels();
            }
        }
    }

    public DateTime DataWaznosci
    {
        get => _dataWaznosci;
        set { if (_dataWaznosci != value) { _dataWaznosci = value; OnPropertyChanged(); } }
    }

    // ---- Selektor lat ----
    public int ValidityYears
    {
        get => _validityYears;
        set
        {
            if (_validityYears != value)
            {
                _validityYears = value;
                OnPropertyChanged();
                int targetYear = _dataBadania.Year + value;
                int targetMonth = _dataWaznosci.Month;
                int targetDay = Math.Min(_dataBadania.Day, DateTime.DaysInMonth(targetYear, targetMonth));
                DataWaznosci = new DateTime(targetYear, targetMonth, targetDay);
            }
        }
    }

    public ObservableCollection<ValidityYearItem> ValidityYearOptions { get; } = new();

    private void UpdateValidityYearLabels()
    {
        ValidityYearOptions.Clear();
        for (int i = 1; i <= 5; i++)
            ValidityYearOptions.Add(new ValidityYearItem { Years = i, Label = (_dataBadania.Year + i).ToString() });
    }

    public class ValidityYearItem
    {
        public int Years { get; set; }
        public string? Label { get; set; }
    }

    // ---- Selektor miesiecy ----
    public int ValidityMonth
    {
        get => _dataWaznosci.Month;
        set
        {
            if (_dataWaznosci.Month != value)
            {
                int targetYear = _dataWaznosci.Year;
                int targetDay = Math.Min(_dataWaznosci.Day, DateTime.DaysInMonth(targetYear, value));
                DataWaznosci = new DateTime(targetYear, value, targetDay);
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ValidityMonthItem> ValidityMonthOptions { get; } = new();

    private void InitializeValidityMonthOptions()
    {
        ValidityMonthOptions.Clear();
        for (int i = 1; i <= 12; i++)
            ValidityMonthOptions.Add(new ValidityMonthItem { Month = i, Label = i.ToString("D2") });
    }

    public class ValidityMonthItem
    {
        public int Month { get; set; }
        public string? Label { get; set; }
    }

    // ---- Wynik ----
    public string SelectedWynik
    {
        get => _selectedWynik;
        set { if (_selectedWynik != value) { _selectedWynik = value; OnPropertyChanged(); } }
    }

    // ---- Nr Ksiegi ----
    public string? NrKsiegi
    {
        get => _nrKsiegi;
        set { if (_nrKsiegi != value) { _nrKsiegi = value; OnPropertyChanged(); } }
    }

    // ---- Komendy ----
    public ICommand ClearCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SaveBadanieCommand { get; }

    // ---- Ceny z cennika (teksty) ----
    public string PriceBasicText         => _priceBasic.HasValue          ? $"{_priceBasic:0} zl"          : "---";
    public string PriceLaryngologistText => _priceLaryngologist.HasValue  ? $"{_priceLaryngologist:0} zl"  : "---";
    public string PriceOphthalmologistText => _priceOphthalmologist.HasValue ? $"{_priceOphthalmologist:0} zl" : "---";
    public string PriceSanitaryText      => _priceSanitary.HasValue       ? $"{_priceSanitary:0} zl"       : "---";
    public string PriceLipidogramText    => _priceLipidogram.HasValue     ? $"{_priceLipidogram:0} zl"     : "---";
    public string PriceEKGText           => _priceEKG.HasValue            ? $"{_priceEKG:0} zl"            : "---";
    public string PriceHealthClinicText  => _priceHealthClinic.HasValue   ? $"{_priceHealthClinic:0} zl"   : "---";
    public string PriceOtherText         => _priceOther.HasValue          ? $"{_priceOther:0} zl"          : "---";

    // ---- Ladowanie danych ----
    private void LoadData()
    {
        try
        {
            Console.WriteLine("[EDIT VM] LoadData started");

            // Cennik
            var cennik = _badaniaEditService.GetCennikList();
            CennikList = new ObservableCollection<CennikDto>(cennik);
            Console.WriteLine($"[EDIT VM] Loaded {cennik.Count} cennik items");

            // Lista badan zamknietych
            var badania = _badaniaEditService.GetBadaniaZamkniete();
            _allSkierowania = new ObservableCollection<BadaniaEditListDto>(badania);
            Skierowania = new ObservableCollection<BadaniaEditListDto>(badania);
            Console.WriteLine($"[EDIT VM] Loaded {badania.Count} zamkniete badania");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EDIT VM] LoadData error: {ex.Message}");
        }
    }

    // ---- Ladowanie szczegolow ----
    private void LoadDetailsForSelected(int bSkierowaniaId)
    {
        try
        {
            Console.WriteLine($"[EDIT VM] LoadDetailsForSelected B_ID={bSkierowaniaId}");
            StatusMessage = $"Ladowanie B_ID={bSkierowaniaId}...";

            var details = _badaniaEditService.GetBadanieDetailsEdt(bSkierowaniaId);
            SelectedDetails = details;

            if (details != null)
            {
                Console.WriteLine($"[EDIT VM] Details loaded: {details.P_imie} {details.P_nazwisko}");

                // Data badania
                DateTime badData = details.Bad_Data ?? DateTime.Today;
                _dataBadania = badData;
                OnPropertyChanged(nameof(DataBadania));

                // Data waznosci
                if (details.Bad_Data_Do.HasValue)
                {
                    _dataWaznosci = details.Bad_Data_Do.Value;
                    OnPropertyChanged(nameof(DataWaznosci));
                    int calcYears = details.Bad_Data_Do.Value.Year - badData.Year;
                    _validityYears = Math.Max(1, Math.Min(calcYears, 5));
                    OnPropertyChanged(nameof(ValidityYears));
                }
                else
                {
                    _validityYears = 3;
                    OnPropertyChanged(nameof(ValidityYears));
                    DataWaznosci = badData.AddYears(3);
                }
                UpdateValidityYearLabels();
                OnPropertyChanged(nameof(ValidityMonth));

                // Wynik
                SelectedWynik = details.Bad_Wynik ?? "pozytywne";

                // Nr Ksiegi
                NrKsiegi = details.Bad_Nr_KS;

                // Cennik - auto-wybor po nazwie (bn_cennik string), nie po ID
                if (!string.IsNullOrEmpty(details.Bad_bn_cennik))
                {
                    Console.WriteLine($"🔍 [EDIT VM] Searching for cennik: '{details.Bad_bn_cennik}' in CennikList ({CennikList.Count} items)");
                    var foundCennik = CennikList.FirstOrDefault(c => c.bn_cennik == details.Bad_bn_cennik);
                    if (foundCennik != null)
                    {
                        SelectedCennik = foundCennik;
                        Console.WriteLine($"✅ [EDIT VM] Auto-selected cennik: '{foundCennik.bn_cennik}' (ID={foundCennik.Identyfikator})");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ [EDIT VM] Cennik '{details.Bad_bn_cennik}' NOT FOUND in CennikList");
                        foreach (var c in CennikList)
                            Console.WriteLine($"   - '{c.bn_cennik}'");
                        SelectedCennik = null;
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ [EDIT VM] Bad_bn_cennik is NULL or EMPTY");
                    SelectedCennik = null;
                }

                // Ceny z bazy
                Cena1 = details.Bad_Cena1;
                Cena2 = details.Bad_Cena2;
                Cena3 = details.Bad_Cena3;
                Cena4 = details.Bad_Cena4;
                Cena5 = details.Bad_Cena6;
                Cena6 = details.Bad_Cena7;
                Cena7 = details.Bad_Cena8;
                Cena8 = details.Bad_Cena9;

                // StatusMessage i DebugInfo
                StatusMessage = $"[OK] B_ID={details.B_ID} | {details.P_imie} {details.P_nazwisko}";
                DebugInfo =
                    $"B_ID={details.B_ID}\n" +
                    $"Pacjent: {details.P_imie} {details.P_nazwisko}  PESEL: {details.P_pesel}\n" +
                    $"Firma: {details.Firma_Nazwa}  NIP: {details.Firma_NIP}\n" +
                    $"Stanowisko: {details.B_Stanowisko}  Typ: {details.B_TypBadania}\n" +
                    $"Bad_Data: {details.Bad_Data:dd-MM-yyyy}  Bad_Data_Do: {details.Bad_Data_Do:dd-MM-yyyy}\n" +
                    $"Bad_Wynik: {details.Bad_Wynik}  Bad_Nr_KS: {details.Bad_Nr_KS}\n" +
                    $"Bad_bn_cennik: {details.Bad_bn_cennik}  Bad_Fakt: {details.Bad_Fakt}\n" +
                    $"Cena1={details.Bad_Cena1} Cena2={details.Bad_Cena2} Cena3={details.Bad_Cena3} Cena4={details.Bad_Cena4}\n" +
                    $"Cena6={details.Bad_Cena6} Cena7={details.Bad_Cena7} Cena8={details.Bad_Cena8} Cena9={details.Bad_Cena9}";

                Console.WriteLine($"[EDIT VM] StatusMessage: {StatusMessage}");
            }
            else
            {
                StatusMessage = $"[NULL] GetBadanieDetailsEdt zwrocil null dla B_ID={bSkierowaniaId}";
                DebugInfo = StatusMessage;
                Console.WriteLine($"[EDIT VM] {StatusMessage}");
                SelectedCennik = null;
                ClearSelectedPrices();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"[BLAD] {ex.Message}";
            DebugInfo = $"EXCEPTION LoadDetailsForSelected(B_ID={bSkierowaniaId}):\n{ex}";
            Console.WriteLine($"❌ [EDIT VM] ERROR: {ex.Message}\n{ex.StackTrace}");
            SelectedDetails = null;
            SelectedCennik = null;
        }
    }

    // ---- Cennik prices ----
    private void LoadCennikPrices()
    {
        try
        {
            Console.WriteLine($"🔄 [EDIT VM] LoadCennikPrices STARTED - SelectedCennik: {SelectedCennik?.bn_cennik}");

            if (SelectedCennik == null || string.IsNullOrEmpty(SelectedCennik.bn_cennik))
            {
                Console.WriteLine("⚠️ [EDIT VM] SelectedCennik is NULL or empty - clearing prices");
                ClearPrices();
                return;
            }

            var prices = _badaniaEditService.GetCennikPrices(SelectedCennik.bn_cennik);

            if (prices == null)
            {
                Console.WriteLine($"❌ [EDIT VM] GetCennikPrices returned NULL for cennik '{SelectedCennik.bn_cennik}'");
                ClearPrices();
                return;
            }

            if (prices.Count == 0)
            {
                Console.WriteLine($"⚠️ [EDIT VM] GetCennikPrices returned EMPTY dictionary for cennik '{SelectedCennik.bn_cennik}'");
                ClearPrices();
                return;
            }

            // Pokaż wszystkie odczytane wartości
            var pricesInfo = string.Join("\n", prices.Select(p => $"  {p.Key}: {p.Value:N2} zł"));
            Console.WriteLine($"✅ [EDIT VM] Loaded {prices.Count} prices:\n{pricesInfo}");

            // Mapowanie nazw z bazy na pola (tak samo jak w BadaniaNewViewModel)
            decimal? GetPrice(string[] keywords)
            {
                foreach (var keyword in keywords)
                {
                    var matchingKey = prices.Keys.FirstOrDefault(dbKey =>
                        dbKey.ToLower().Contains(keyword.ToLower()) ||
                        dbKey.ToLower().Replace(".", "").Contains(keyword.ToLower()));

                    if (matchingKey != null && prices.TryGetValue(matchingKey, out var value))
                    {
                        Console.WriteLine($"  ✅ [EDIT VM] Mapped '{keyword}' → '{matchingKey}' = {value:N2} zł");
                        return value;
                    }
                }
                Console.WriteLine($"  ⚠️ [EDIT VM] No match for keywords: {string.Join(", ", keywords)}");
                return null;
            }

            _priceBasic           = GetPrice(new[] { "lekarz", "lekasz", "MP", "basic" });
            _priceLaryngologist   = GetPrice(new[] { "laryngolog" });
            _priceOphthalmologist = GetPrice(new[] { "okulista", "okulist" });
            _priceSanitary        = GetPrice(new[] { "ksi", "książeczka", "ksiazeczka" });
            _priceLipidogram      = GetPrice(new[] { "lipidogram" });
            _priceEKG             = GetPrice(new[] { "ekg" });
            _priceHealthClinic    = GetPrice(new[] { "urlop", "zdrowie", "healthclinic" });
            _priceOther           = GetPrice(new[] { "inne", "other" });

            RefreshPriceTexts();
            Console.WriteLine("✅ [EDIT VM] LoadCennikPrices COMPLETED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [EDIT VM] LoadCennikPrices error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void ClearPrices()
    {
        _priceBasic = _priceLaryngologist = _priceOphthalmologist = _priceSanitary = null;
        _priceLipidogram = _priceEKG = _priceHealthClinic = _priceOther = null;
        RefreshPriceTexts();
    }

    private void RefreshPriceTexts()
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

    public void ClearSelectedPrices()
    {
        Cena1 = Cena2 = Cena3 = Cena4 = Cena5 = Cena6 = Cena7 = Cena8 = null;
    }

    public decimal GetPriceForExamination(string examinationType) => examinationType switch
    {
        "Basic"          => _priceBasic ?? 0m,
        "Laryngologist"  => _priceLaryngologist ?? 0m,
        "Ophthalmologist"=> _priceOphthalmologist ?? 0m,
        "Sanitary"       => _priceSanitary ?? 0m,
        "Lipidogram"     => _priceLipidogram ?? 0m,
        "EKG"            => _priceEKG ?? 0m,
        "HealthClinic"   => _priceHealthClinic ?? 0m,
        "Other"          => _priceOther ?? 0m,
        _                => 0m
    };

    public void SetCenaForExaminationType(string examinationType, decimal? value)
    {
        switch (examinationType)
        {
            case "Basic":           Cena1 = value; break;
            case "Laryngologist":   Cena2 = value; break;
            case "Ophthalmologist": Cena3 = value; break;
            case "Sanitary":        Cena4 = value; break;
            case "Lipidogram":      Cena5 = value; break;
            case "EKG":             Cena6 = value; break;
            case "HealthClinic":    Cena7 = value; break;
            case "Other":           Cena8 = value; break;
        }
    }

    private void ExecuteClearCommand()
    {
        SelectedSkierowanie = null;
        ClearSelectedPrices();
        StatusMessage = "--- wyczyszczono ---";
        DebugInfo = string.Empty;
    }

    private void ExecuteRefreshCommand()
    {
        LoadData();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            Skierowania = new ObservableCollection<BadaniaEditListDto>(_allSkierowania);
            return;
        }

        var filterLower = FilterText.ToLowerInvariant();

        if (filterLower.StartsWith("#"))
        {
            var idText = filterLower.Substring(1);
            if (int.TryParse(idText, out int searchId))
            {
                Skierowania = new ObservableCollection<BadaniaEditListDto>(_allSkierowania.Where(s => s.B_ID == searchId).ToList());
                return;
            }
        }

        var filtered = _allSkierowania.Where(s =>
        {
            if (SelectedFilter?.ToString() == "Nazwisko")
                return s.P_nazwisko?.ToLowerInvariant().Contains(filterLower) == true;
            else if (SelectedFilter?.ToString() == "Firma")
                return s.Firma_Nazwa?.ToLowerInvariant().Contains(filterLower) == true;
            else
                return s.B_ID.ToString().Contains(filterLower) ||
                       s.P_imie?.ToLowerInvariant().Contains(filterLower) == true ||
                       s.P_nazwisko?.ToLowerInvariant().Contains(filterLower) == true ||
                       s.Firma_Nazwa?.ToLowerInvariant().Contains(filterLower) == true;
        }).ToList();

        Skierowania = new ObservableCollection<BadaniaEditListDto>(filtered);
    }

    public void RecalculateTotalPrice() { OnPropertyChanged(nameof(SumaText)); }

    public void ToggleExamination(string examType)
    {
        var currentVal = GetPriceForExamination(examType);
        if (currentVal > 0)
            SetCenaForExaminationType(examType, null);
        else
            SetCenaForExaminationType(examType, GetPriceForExamination(examType));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
    }
}
