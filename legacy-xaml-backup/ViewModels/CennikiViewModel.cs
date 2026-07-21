using ASMED.WPF.Helpers;
using ASMED.WPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels
{
    /// <summary>
    /// ViewModel dla widoku zarz�dzania cennikami firm
    /// </summary>
    public class CennikiViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly CennikiHelper _helper;

        // ==============================
        // LISTA FIRM
        // ==============================
        private ObservableCollection<FirmaRow> _firmy = new();
        private ObservableCollection<FirmaRow> _firmyFiltered = new();
        private FirmaRow? _selectedFirma;
        private string _searchTextFirmy = string.Empty;
        private string _selectedFilterFirmy = "Wszystko";

        public ObservableCollection<FirmaRow> Firmy
        {
            get => _firmy;
            set { _firmy = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FirmaRow> FirmyFiltered
        {
            get => _firmyFiltered;
            set { _firmyFiltered = value; OnPropertyChanged(); }
        }

        public FirmaRow? SelectedFirma
        {
            get => _selectedFirma;
            set { _selectedFirma = value; OnPropertyChanged(); }
        }

        public string SearchTextFirmy
        {
            get => _searchTextFirmy;
            set
            {
                _searchTextFirmy = value;
                OnPropertyChanged();
                FilterFirmy();
            }
        }

        public string SelectedFilterFirmy
        {
            get => _selectedFilterFirmy;
            set
            {
                _selectedFilterFirmy = value;
                OnPropertyChanged();
                FilterFirmy();
            }
        }

        public ObservableCollection<string> FilterFirmyOptions { get; } = new()
        {
            "Wszystko", "Cennik", "Firma"
        };

        public int ZaznaczoneFirmyCount => Firmy.Count(f => f.IsSelected);

        // ==============================
        // LISTA CENNIK�W
        // ==============================
        private ObservableCollection<CennikRow> _cenniki = new();
        private ObservableCollection<CennikRow> _cennikiFiltered = new();
        private CennikRow? _selectedCennik;
        private string _searchTextCenniki = string.Empty;
        private bool _isNowyCennikVisible = false;
        private string _nowyCennikNazwa = string.Empty;

        public ObservableCollection<CennikRow> CennikiList
        {
            get => _cenniki;
            set { _cenniki = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CennikRow> CennikiFiltered
        {
            get => _cennikiFiltered;
            set { _cennikiFiltered = value; OnPropertyChanged(); }
        }

        public CennikRow? SelectedCennik
        {
            get => _selectedCennik;
            set
            {
                if (_selectedCennik != value)
                {
                    _selectedCennik = value;
                    // System.Diagnostics.Debug.WriteLine($"SelectedCennik CHANGED: {value?.Nazwa ?? "NULL"}");
                    OnPropertyChanged();
                    LoadCennikPozycje();
                }
            }
        }

        public string SearchTextCenniki
        {
            get => _searchTextCenniki;
            set
            {
                _searchTextCenniki = value;
                OnPropertyChanged();
                FilterCenniki();
            }
        }

        public bool IsNowyCennikVisible
        {
            get => _isNowyCennikVisible;
            set { _isNowyCennikVisible = value; OnPropertyChanged(); }
        }

        public string NowyCennikNazwa
        {
            get => _nowyCennikNazwa;
            set { _nowyCennikNazwa = value; OnPropertyChanged(); }
        }

        // ==============================
        // ZMIANA CENNIKA
        // ==============================
        private CennikRow? _selectedNowyCennik;
        public CennikRow? SelectedNowyCennik
        {
            get => _selectedNowyCennik;
            set { _selectedNowyCennik = value; OnPropertyChanged(); }
        }

        // ==============================
        // EDYCJA CENNIKA
        // ==============================
        private ObservableCollection<CennikPozycjaRow> _cennikPozycje = new();
        public ObservableCollection<CennikPozycjaRow> CennikPozycje
        {
            get => _cennikPozycje;
            set { _cennikPozycje = value; OnPropertyChanged(); }
        }

        // ==============================
        // COMMANDS
        // ==============================
        public ICommand ZmienCennikFirmCommand { get; }
        public ICommand ClearSearchFirmyCommand { get; }
        public ICommand ZaznaczWszystkieFirmyCommand { get; }
        public ICommand WyczyscZaznaczenieFirmCommand { get; }
        public ICommand OdswiezFirmyCommand { get; }

        public ICommand ClearSearchCennikiCommand { get; }
        public ICommand UsunCennikCommand { get; }
        public ICommand ToggleNowyCennikCommand { get; }
        public ICommand DodajCennikCommand { get; }
        public ICommand OdswiezCennikiCommand { get; }

        public ICommand ZapiszCennikCommand { get; }
        public ICommand WyczyscCennikCommand { get; }

        // ==============================
        // KONSTRUKTOR
        // ==============================
        public CennikiViewModel()
        {
            _helper = new CennikiHelper();

            // ? Inicjalizuj kolekcje przed Commands!
            Firmy = new ObservableCollection<FirmaRow>();
            FirmyFiltered = new ObservableCollection<FirmaRow>();
            CennikiList = new ObservableCollection<CennikRow>();
            CennikiFiltered = new ObservableCollection<CennikRow>();
            CennikPozycje = new ObservableCollection<CennikPozycjaRow>();

            // Commands - Firmy
            ZmienCennikFirmCommand = new RelayCommand(_ => ZmienCennikWybranychFirm());
            ClearSearchFirmyCommand = new RelayCommand(_ => SearchTextFirmy = string.Empty);
            ZaznaczWszystkieFirmyCommand = new RelayCommand(_ => ZaznaczWszystkieFirmy());
            WyczyscZaznaczenieFirmCommand = new RelayCommand(_ => WyczyscZaznaczenieFirm());
            OdswiezFirmyCommand = new RelayCommand(_ => OdswiezFirmy());

            // Commands - Cenniki
            ClearSearchCennikiCommand = new RelayCommand(_ => SearchTextCenniki = string.Empty);
            UsunCennikCommand = new RelayCommand<CennikRow>(UsunCennik);
            ToggleNowyCennikCommand = new RelayCommand(_ => IsNowyCennikVisible = !IsNowyCennikVisible);
            DodajCennikCommand = new RelayCommand(_ => DodajNowyCennik());
            OdswiezCennikiCommand = new RelayCommand(_ => OdswiezCenniki());

            // Commands - Edycja
            ZapiszCennikCommand = new RelayCommand(_ => ZapiszCennik());
            WyczyscCennikCommand = new RelayCommand(_ => WyczyscCennik());

            // Za�aduj dane (z op�nieniem dla UI)
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(() =>
                {
                    try
                    {
                        LoadData();
                    }
                    catch (Exception)
                    {
                        // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: B��d �adowania danych: {ex.Message}");
                    }
                }));
        }

        // ==============================
        // METODY �ADOWANIA DANYCH
        // ==============================
        private void LoadData()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("CennikiViewModel: �adowanie danych...");

                // Za�aduj cenniki (najpierw, bo s� potrzebne w ComboBox)
                try
                {
                    var cenniki = _helper.GetAllCenniki();
                    CennikiList.Clear();
                    foreach (var c in cenniki)
                    {
                        CennikiList.Add(c);
                    }
                    // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: Za�adowano {CennikiList.Count} cennik�w");
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: B��d �adowania cennik�w: {ex.Message}");
                    // Kontynuuj - mo�e by� pusta tabela Cenniki
                }

                FilterCenniki();

                // Za�aduj firmy
                try
                {
                    var firmy = _helper.GetAllFirmy();
                    Firmy.Clear();
                    foreach (var f in firmy)
                    {
                        Firmy.Add(f);
                    }
                    // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: Za�adowano {Firmy.Count} firm");
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: B��d �adowania firm: {ex.Message}");
                    // Kontynuuj - mo�e by� pusta tabela Firma
                }

                FilterFirmy();

                // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: Zako�czono �adowanie danych");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: Krytyczny b��d �adowania danych: {ex.Message}");
                // Nie rzucaj wyj�tku - pozw�l UI si� za�adowa�
            }
        }

        private void LoadCennikPozycje()
        {
            try
            {
                if (SelectedCennik == null)
                {
                    // System.Diagnostics.Debug.WriteLine("LoadCennikPozycje: SelectedCennik is NULL - clearing pozycje");
                    CennikPozycje = new ObservableCollection<CennikPozycjaRow>();
                    return;
                }

                // System.Diagnostics.Debug.WriteLine($"LoadCennikPozycje: �adowanie pozycji cennika '{SelectedCennik.Nazwa}'");

                var pozycje = _helper.GetCennikPozycje(SelectedCennik.Nazwa);

                if (pozycje == null || pozycje.Count == 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"LoadCennikPozycje: ?? Brak pozycji dla cennika '{SelectedCennik.Nazwa}'");

                    // ? Je�li brak pozycji, utw�rz domy�lne (puste)
                    pozycje = new List<CennikPozycjaRow>
                    {
                        new CennikPozycjaRow { Nazwa = "Lekarz", Cena = 0m },
                        new CennikPozycjaRow { Nazwa = "Laryngolog", Cena = 0m },
                        new CennikPozycjaRow { Nazwa = "Okulista", Cena = 0m },
                        new CennikPozycjaRow { Nazwa = "Ksi��eczka (Sanepid)", Cena = 0m },
                        new CennikPozycjaRow { Nazwa = "Lipidogram", Cena = 0m },
                        new CennikPozycjaRow { Nazwa = "EKG", Cena = 0m },
                        new CennikPozycjaRow { Nazwa = "Urlop (Zdrowie)", Cena = 0m },
                        new CennikPozycjaRow { Nazwa = "Inne", Cena = 0m },
                        new CennikPozycjaRow { Nazwa = "Rezerwa1", Cena = 0m }
                    };

                    // System.Diagnostics.Debug.WriteLine("LoadCennikPozycje: Utworzono domy�lne pozycje");
                }

                CennikPozycje = new ObservableCollection<CennikPozycjaRow>(pozycje);

                // System.Diagnostics.Debug.WriteLine($"LoadCennikPozycje: ? Za�adowano {CennikPozycje.Count} pozycji");

                // Debug: Wypisz wszystkie pozycje
                foreach (var poz in CennikPozycje)
                {
                    // System.Diagnostics.Debug.WriteLine($"  - {poz.Nazwa}: {poz.Cena:N2} z�");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadCennikPozycje: ? B��d �adowania pozycji cennika: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"LoadCennikPozycje: StackTrace: {ex.StackTrace}");

                // W przypadku b��du, poka� pust� list�
                CennikPozycje = new ObservableCollection<CennikPozycjaRow>();

                MessageBox.Show($"B��d �adowania pozycji cennika:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==============================
        // FILTROWANIE
        // ==============================
        private void FilterFirmy()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchTextFirmy))
                {
                    FirmyFiltered = new ObservableCollection<FirmaRow>(Firmy);
                    return;
                }

                var searchText = SearchTextFirmy.ToLower();
                IEnumerable<FirmaRow> filtered = Firmy;

                switch (SelectedFilterFirmy)
                {
                    case "Cennik":
                        filtered = Firmy.Where(f =>
                            (f.Cennik ?? "").ToLower().Contains(searchText) ||
                            TextNormalizationHelper.ContainsIgnoringDiacritics(f.Cennik ?? "", searchText));
                        break;

                    case "Firma":
                        filtered = Firmy.Where(f =>
                            (f.Nazwa ?? "").ToLower().Contains(searchText) ||
                            TextNormalizationHelper.ContainsIgnoringDiacritics(f.Nazwa ?? "", searchText));
                        break;

                    case "Wszystko":
                    default:
                        filtered = Firmy.Where(f =>
                            (f.Cennik ?? "").ToLower().Contains(searchText) ||
                            TextNormalizationHelper.ContainsIgnoringDiacritics(f.Cennik ?? "", searchText) ||
                            (f.Nazwa ?? "").ToLower().Contains(searchText) ||
                            TextNormalizationHelper.ContainsIgnoringDiacritics(f.Nazwa ?? "", searchText));
                        break;
                }

                FirmyFiltered = new ObservableCollection<FirmaRow>(filtered);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: B��d filtrowania firm: {ex.Message}");
            }
        }

        private void FilterCenniki()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchTextCenniki))
                {
                    CennikiFiltered = new ObservableCollection<CennikRow>(CennikiList);
                    return;
                }

                var searchText = SearchTextCenniki.ToLower();
                var filtered = CennikiList.Where(c =>
                    (c.Nazwa ?? "").ToLower().Contains(searchText) ||
                    TextNormalizationHelper.ContainsIgnoringDiacritics(c.Nazwa ?? "", searchText));

                CennikiFiltered = new ObservableCollection<CennikRow>(filtered);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: B��d filtrowania cennik�w: {ex.Message}");
            }
        }

        // ==============================
        // AKCJE - FIRMY
        // ==============================
        private void OdswiezFirmy()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("OdswiezFirmy: Rozpoczynam od�wie�anie...");

                // Wyczy�� wyszukiwanie
                SearchTextFirmy = string.Empty;

                // Za�aduj firmy z bazy
                var firmy = _helper.GetAllFirmy();
                Firmy.Clear();
                foreach (var f in firmy)
                {
                    Firmy.Add(f);
                }

                FilterFirmy();

                // System.Diagnostics.Debug.WriteLine($"OdswiezFirmy: ? Od�wie�ono - {Firmy.Count} firm");

                NotificationHelper.ShowInfo(
                    "Od�wie�anie",
                    $"Lista firm zosta�a od�wie�ona. Znaleziono {Firmy.Count} firm.");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"OdswiezFirmy: ? B��d: {ex.Message}");
                MessageBox.Show($"B��d od�wie�ania listy firm:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ZmienCennikWybranychFirm()
        {
            try
            {
                if (SelectedNowyCennik == null)
                {
                    MessageBox.Show("Wybierz nowy cennik z listy.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var zaznaczone = Firmy.Where(f => f.IsSelected).ToList();
                if (!zaznaczone.Any())
                {
                    MessageBox.Show("Zaznacz firmy, kt�rym chcesz zmieni� cennik.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Czy na pewno zmieni� cennik dla {zaznaczone.Count} firm na '{SelectedNowyCennik.Nazwa}'?",
                    "Potwierdzenie",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    int successCount = 0;
                    foreach (var firma in zaznaczone)
                    {
                        if (_helper.UpdateFirmaCennik(firma.Id, SelectedNowyCennik.Nazwa))
                        {
                            firma.Cennik = SelectedNowyCennik.Nazwa;
                            firma.IsSelected = false;
                            successCount++;
                        }
                    }

                    NotificationHelper.ShowInfo(
                        "Zmiana cennika",
                        $"Zmieniono cennik dla {successCount} z {zaznaczone.Count} firm.");

                    OnPropertyChanged(nameof(ZaznaczoneFirmyCount));
                    FilterFirmy();
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: B��d zmiany cennika: {ex.Message}");
                MessageBox.Show($"B��d podczas zmiany cennika:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ZaznaczWszystkieFirmy()
        {
            foreach (var firma in FirmyFiltered)
            {
                firma.IsSelected = true;
            }
            OnPropertyChanged(nameof(ZaznaczoneFirmyCount));
        }

        private void WyczyscZaznaczenieFirm()
        {
            foreach (var firma in Firmy)
            {
                firma.IsSelected = false;
            }
            OnPropertyChanged(nameof(ZaznaczoneFirmyCount));
        }

        // ==============================
        // AKCJE - CENNIKI
        // ==============================
        private void UsunCennik(CennikRow? cennik)
        {
            try
            {
                if (cennik == null) return;

                // Sprawd� czy cennik jest u�ywany przez firmy
                var firmyUzywajace = Firmy.Count(f => f.Cennik == cennik.Nazwa);
                if (firmyUzywajace > 0)
                {
                    MessageBox.Show(
                        $"Nie mo�na usun�� cennika '{cennik.Nazwa}'.\n\nCennik jest u�ywany przez {firmyUzywajace} firm.",
                        "Nie mo�na usun��",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Czy na pewno usun�� cennik '{cennik.Nazwa}'?",
                    "Potwierdzenie usuni�cia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (_helper.DeleteCennik(cennik.Nazwa))
                    {
                        CennikiList.Remove(cennik);
                        FilterCenniki();

                        NotificationHelper.ShowInfo("Usuwanie cennika", $"Cennik '{cennik.Nazwa}' zosta� usuni�ty.");

                        if (SelectedCennik == cennik)
                        {
                            SelectedCennik = null;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nie uda�o si� usun�� cennika.", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: B��d usuwania cennika: {ex.Message}");
                MessageBox.Show($"B��d podczas usuwania cennika:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DodajNowyCennik()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NowyCennikNazwa))
                {
                    MessageBox.Show("Wprowad� nazw� cennika.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Sprawd� czy cennik ju� istnieje
                if (CennikiList.Any(c => c.Nazwa.Equals(NowyCennikNazwa, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"Cennik '{NowyCennikNazwa}' ju� istnieje.", "B��d", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_helper.CreateCennik(NowyCennikNazwa))
                {
                    var nowyCennik = new CennikRow { Nazwa = NowyCennikNazwa };
                    CennikiList.Add(nowyCennik);
                    FilterCenniki();

                    NotificationHelper.ShowInfo("Dodawanie cennika", $"Cennik '{NowyCennikNazwa}' zosta� dodany.");

                    NowyCennikNazwa = string.Empty;
                    IsNowyCennikVisible = false;
                }
                else
                {
                    MessageBox.Show("Nie uda�o si� doda� cennika.", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: B��d dodawania cennika: {ex.Message}");
                MessageBox.Show($"B��d podczas dodawania cennika:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==============================
        // AKCJE - EDYCJA
        // ==============================
        private void ZapiszCennik()
        {
            try
            {
                if (SelectedCennik == null)
                {
                    MessageBox.Show("Wybierz cennik do zapisania.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (_helper.UpdateCennikPozycje(SelectedCennik.Nazwa, CennikPozycje.ToList()))
                {
                    NotificationHelper.ShowInfo("Zapisywanie cennika", $"Cennik '{SelectedCennik.Nazwa}' zosta� zapisany.");
                }
                else
                {
                    MessageBox.Show("Nie uda�o si� zapisa� cennika.", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiViewModel: B��d zapisywania cennika: {ex.Message}");
                MessageBox.Show($"B��d podczas zapisywania cennika:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WyczyscCennik()
        {
            LoadCennikPozycje();
        }

        // ==============================
        // AKCJE - CENNIKI
        // ==============================
        private void OdswiezCenniki()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("OdswiezCenniki: Rozpoczynam od�wie�anie...");

                // Wyczy�� wyszukiwanie
                SearchTextCenniki = string.Empty;

                // Za�aduj cenniki z bazy
                var cenniki = _helper.GetAllCenniki();
                CennikiList.Clear();
                foreach (var c in cenniki)
                {
                    CennikiList.Add(c);
                }

                FilterCenniki();

                // System.Diagnostics.Debug.WriteLine($"OdswiezCenniki: ? Od�wie�ono - {CennikiList.Count} cennik�w");

                NotificationHelper.ShowInfo(
                    "Od�wie�anie",
                    $"Lista cennik�w zosta�a od�wie�ona. Znaleziono {CennikiList.Count} cennik�w.");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"OdswiezCenniki: ? B��d: {ex.Message}");
                MessageBox.Show($"B��d od�wie�ania listy cennik�w:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }

    // ==============================
    // MODELE DANYCH
    // ==============================
    public class FirmaRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int Id { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private string? _cennik;
        public string? Cennik
        {
            get => _cennik;
            set { _cennik = value; OnPropertyChanged(); }
        }

        public string? Nazwa { get; set; }
    }

    public class CennikRow
    {
        public string Nazwa { get; set; } = string.Empty;
    }

    public class CennikPozycjaRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Nazwa { get; set; } = string.Empty;

        private decimal _cena;
        public decimal Cena
        {
            get => _cena;
            set { _cena = value; OnPropertyChanged(); }
        }
    }
}
// end of file
