using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models.BadaniaNew;
using ASMED.EDM.UI.Services;
using CommunityToolkit.Mvvm.Input;

namespace ASMED.EDM.UI.ViewModels.Cenniki;

/// <summary>
/// ViewModel dla widoku Cenniki (Baza Danych → Ustawienia → Cenniki)
/// Obsługuje trzy kolumny: Firmy | Lista cenników | Edycja cennika
/// </summary>
public partial class CennikiViewModel : ViewModelBase
{
    private readonly BadaniaNewService _service;

    // ─── Backing collections ──────────────────────────────────────────────────
    private ObservableCollection<FirmaWithCennikDto> _allFirmy = new();
    private ObservableCollection<CennikItem>         _allCenniki = new();

    // ─── KOLUMNA 1: Firmy ─────────────────────────────────────────────────────
    private ObservableCollection<FirmaWithCennikDto> _firmyFiltered = new();
    public ObservableCollection<FirmaWithCennikDto> FirmyFiltered
    {
        get => _firmyFiltered;
        private set => SetProperty(ref _firmyFiltered, value);
    }

    private FirmaWithCennikDto? _selectedFirma;
    public FirmaWithCennikDto? SelectedFirma
    {
        get => _selectedFirma;
        set => SetProperty(ref _selectedFirma, value);
    }

    private string _searchTextFirmy = string.Empty;
    public string SearchTextFirmy
    {
        get => _searchTextFirmy;
        set { SetProperty(ref _searchTextFirmy, value); ApplyFirmyFilter(); }
    }

    public ObservableCollection<string> FilterFirmyOptions { get; } =
        new() { "Wszystkie", "Z cennikiem", "Bez cennika" };

    private string _selectedFilterFirmy = "Wszystkie";
    public string SelectedFilterFirmy
    {
        get => _selectedFilterFirmy;
        set { SetProperty(ref _selectedFilterFirmy, value); ApplyFirmyFilter(); }
    }

    public int ZaznaczoneFirmyCount => _allFirmy.Count(f => f.IsSelected);

    // ─── KOLUMNA 2: Cenniki ───────────────────────────────────────────────────
    private ObservableCollection<CennikItem> _cennikiList = new();
    public ObservableCollection<CennikItem> CennikiList
    {
        get => _cennikiList;
        private set => SetProperty(ref _cennikiList, value);
    }

    private ObservableCollection<CennikItem> _cennikiFiltered = new();
    public ObservableCollection<CennikItem> CennikiFiltered
    {
        get => _cennikiFiltered;
        private set => SetProperty(ref _cennikiFiltered, value);
    }

    private CennikItem? _selectedCennik;
    public CennikItem? SelectedCennik
    {
        get => _selectedCennik;
        set
        {
            if (SetProperty(ref _selectedCennik, value))
                LoadCennikPozycje(value?.Nazwa);
        }
    }

    private CennikItem? _selectedNowyCennik;
    public CennikItem? SelectedNowyCennik
    {
        get => _selectedNowyCennik;
        set => SetProperty(ref _selectedNowyCennik, value);
    }

    private string _searchTextCenniki = string.Empty;
    public string SearchTextCenniki
    {
        get => _searchTextCenniki;
        set { SetProperty(ref _searchTextCenniki, value); ApplyCennikiFilter(); }
    }

    private bool _isNowyCennikVisible;
    public bool IsNowyCennikVisible
    {
        get => _isNowyCennikVisible;
        set => SetProperty(ref _isNowyCennikVisible, value);
    }

    private string _nowyCennikNazwa = string.Empty;
    public string NowyCennikNazwa
    {
        get => _nowyCennikNazwa;
        set => SetProperty(ref _nowyCennikNazwa, value);
    }

    // ─── KOLUMNA 3: Edycja cennika ────────────────────────────────────────────
    public ObservableCollection<CennikPozycjaDto> CennikPozycje { get; } = new();

    // ─── Komendy ─────────────────────────────────────────────────────────────
    public ICommand OdswiezFirmyCommand             { get; }
    public ICommand ClearSearchFirmyCommand         { get; }
    public ICommand ZaznaczWszystkieFirmyCommand    { get; }
    public ICommand WyczyscZaznaczenieFirmCommand   { get; }
    public ICommand ZmienCennikFirmCommand          { get; }

    public ICommand OdswiezCennikiCommand           { get; }
    public ICommand ClearSearchCennikiCommand       { get; }
    public ICommand ToggleNowyCennikCommand         { get; }
    public ICommand DodajCennikCommand              { get; }
    public ICommand UsunCennikCommand               { get; }

    public ICommand ZapiszCennikCommand             { get; }
    public ICommand WyczyscCennikCommand            { get; }

    // ─── Konstruktor ─────────────────────────────────────────────────────────
    public CennikiViewModel(DbConnectionFactory? dbFactory = null)
    {
        _service = new BadaniaNewService(dbFactory);

        OdswiezFirmyCommand           = new RelayCommand(LoadFirmy);
        ClearSearchFirmyCommand       = new RelayCommand(() => SearchTextFirmy = string.Empty);
        ZaznaczWszystkieFirmyCommand  = new RelayCommand(ZaznaczWszystkieFirmy);
        WyczyscZaznaczenieFirmCommand = new RelayCommand(WyczyscZaznaczenieFirm);
        ZmienCennikFirmCommand        = new RelayCommand(ZmienCennikFirm);

        OdswiezCennikiCommand         = new RelayCommand(LoadCenniki);
        ClearSearchCennikiCommand     = new RelayCommand(() => SearchTextCenniki = string.Empty);
        ToggleNowyCennikCommand       = new RelayCommand(() => IsNowyCennikVisible = !IsNowyCennikVisible);
        DodajCennikCommand            = new RelayCommand(DodajCennik);
        UsunCennikCommand             = new RelayCommand<CennikItem>(UsunCennik);

        ZapiszCennikCommand           = new RelayCommand(ZapiszCennik);
        WyczyscCennikCommand          = new RelayCommand(WyczyscCennik);

        LoadAll();
    }

    // ─── Inicjalizacja ────────────────────────────────────────────────────────
    private void LoadAll()
    {
        LoadFirmy();
        LoadCenniki();
    }

    private void LoadFirmy()
    {
        var firmy = _service.GetAllFirmy();
        _allFirmy.Clear();
        foreach (var f in firmy)
        {
            f.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FirmaWithCennikDto.IsSelected))
                    OnPropertyChanged(nameof(ZaznaczoneFirmyCount));
            };
            _allFirmy.Add(f);
        }
        ApplyFirmyFilter();
    }

    private void LoadCenniki()
    {
        var cenniki = _service.GetAllCennikItems();
        _allCenniki = new ObservableCollection<CennikItem>(cenniki);

        // Nowe instancje → wymuszają pełne rebindowanie ComboBoxa i SfDataGrid
        CennikiList    = new ObservableCollection<CennikItem>(cenniki);
        ApplyCennikiFilter();

        Console.WriteLine($"✅ LoadCenniki: {cenniki.Count} pozycji załadowanych");
    }

    private void LoadCennikPozycje(string? cennikName)
    {
        CennikPozycje.Clear();
        if (string.IsNullOrEmpty(cennikName)) return;

        var pozycje = _service.GetCennikPozycje(cennikName);
        foreach (var p in pozycje)
            CennikPozycje.Add(p);
    }

    // ─── Filtrowanie ─────────────────────────────────────────────────────────
    private void ApplyFirmyFilter()
    {
        IEnumerable<FirmaWithCennikDto> query = _allFirmy;

        if (!string.IsNullOrWhiteSpace(SearchTextFirmy))
            query = query.Where(f =>
                (f.Nazwa?.Contains(SearchTextFirmy, StringComparison.OrdinalIgnoreCase)   ?? false) ||
                (f.NIP?.Contains(SearchTextFirmy, StringComparison.OrdinalIgnoreCase)     ?? false) ||
                (f.Cennik?.Contains(SearchTextFirmy, StringComparison.OrdinalIgnoreCase)  ?? false));

        query = SelectedFilterFirmy switch
        {
            "Z cennikiem" => query.Where(f => !string.IsNullOrEmpty(f.Cennik)),
            "Bez cennika" => query.Where(f =>  string.IsNullOrEmpty(f.Cennik)),
            _             => query
        };

        FirmyFiltered = new ObservableCollection<FirmaWithCennikDto>(query);
        OnPropertyChanged(nameof(ZaznaczoneFirmyCount));
        Console.WriteLine($"🔍 ApplyFirmyFilter: '{SearchTextFirmy}' / '{SelectedFilterFirmy}' → {FirmyFiltered.Count} firm");
    }

    private void ApplyCennikiFilter()
    {
        IEnumerable<CennikItem> query = _allCenniki;

        if (!string.IsNullOrWhiteSpace(SearchTextCenniki))
            query = query.Where(c =>
                c.Nazwa?.Contains(SearchTextCenniki, StringComparison.OrdinalIgnoreCase) ?? false);

        CennikiFiltered = new ObservableCollection<CennikItem>(query);
        Console.WriteLine($"🔍 ApplyCennikiFilter: '{SearchTextCenniki}' → {CennikiFiltered.Count} cenników");
    }

    // ─── Firmy – akcje ────────────────────────────────────────────────────────
    private void ZaznaczWszystkieFirmy()
    {
        foreach (var f in FirmyFiltered) f.IsSelected = true;
        OnPropertyChanged(nameof(ZaznaczoneFirmyCount));
    }

    private void WyczyscZaznaczenieFirm()
    {
        foreach (var f in _allFirmy) f.IsSelected = false;
        OnPropertyChanged(nameof(ZaznaczoneFirmyCount));
    }

    private void ZmienCennikFirm()
    {
        if (SelectedNowyCennik == null)
        {
            MessageBox.Show("Wybierz cennik z listy 'Nowy cennik'.", "Brak cennika",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var zaznaczone = _allFirmy.Where(f => f.IsSelected).ToList();
        if (zaznaczone.Count == 0)
        {
            MessageBox.Show("Zaznacz co najmniej jedną firmę.", "Brak zaznaczenia",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ids = zaznaczone.Select(f => f.Id).ToList();
        bool ok = _service.UpdateFirmaCennik(ids, SelectedNowyCennik.Nazwa);
        if (ok)
        {
            // Optimistic update — aktualizuj pole Cennik bezpośrednio na obiektach w pamięci
            // (nie przeładowujemy z bazy, zaznaczenia zostają zachowane)
            string nowaNazwa = SelectedNowyCennik.Nazwa ?? string.Empty;
            foreach (var firma in zaznaczone)
                firma.Cennik = nowaNazwa;

            OnPropertyChanged(nameof(ZaznaczoneFirmyCount));

            Console.WriteLine($"✅ ZmienCennikFirm: cennik '{nowaNazwa}' → {zaznaczone.Count} firm");
        }
        else
        {
            MessageBox.Show("Błąd podczas zapisywania cennika firm.", "Błąd",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─── Cenniki – akcje ─────────────────────────────────────────────────────
    private void DodajCennik()
    {
        if (string.IsNullOrWhiteSpace(NowyCennikNazwa))
        {
            MessageBox.Show("Podaj nazwę cennika.", "Brak nazwy",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool ok = _service.AddCennik(NowyCennikNazwa.Trim());
        if (ok)
        {
            NowyCennikNazwa = string.Empty;
            IsNowyCennikVisible = false;
            LoadCenniki();
        }
        else
        {
            MessageBox.Show("Błąd podczas dodawania cennika.", "Błąd",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UsunCennik(CennikItem? item)
    {
        if (item == null) return;

        var result = MessageBox.Show(
            $"Czy na pewno usunąć cennik '{item.Nazwa}' wraz ze wszystkimi cenami?",
            "Potwierdzenie usunięcia", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        bool ok = _service.DeleteCennik(item.Identyfikator, item.Nazwa ?? string.Empty);
        if (ok)
        {
            if (SelectedCennik?.Identyfikator == item.Identyfikator)
            {
                SelectedCennik = null;
                CennikPozycje.Clear();
            }
            LoadCenniki();
        }
        else
        {
            MessageBox.Show("Błąd podczas usuwania cennika.", "Błąd",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─── Edycja cennika – akcje ───────────────────────────────────────────────
    private void ZapiszCennik()
    {
        if (SelectedCennik == null)
        {
            MessageBox.Show("Wybierz cennik z listy.", "Brak cennika",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool ok = _service.SaveCennikPrices(SelectedCennik.Nazwa!, CennikPozycje);
        if (ok)
            MessageBox.Show($"Cennik '{SelectedCennik.Nazwa}' zapisany.",
                            "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show("Błąd podczas zapisywania cen.", "Błąd",
                            MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void WyczyscCennik()
    {
        SelectedCennik = null;
        CennikPozycje.Clear();
    }
}
