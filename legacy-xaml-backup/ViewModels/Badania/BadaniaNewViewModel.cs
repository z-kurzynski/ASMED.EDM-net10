using ASMED.WPF.Helpers;
using ASMED.WPF.Views; // ✅ DODANE: Dla WizytyViewView
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
// ✅ DODANE: Dla wyszukiwania w drzewie wizualnym
using System.Windows.Media;

namespace ASMED.WPF.ViewModels.Badania
{
    public class WizytaRecordBadania
    {
        public int B_ID { get; set; }
        public int P_ID { get; set; }
        public int Firma_id { get; set; }
        public DateTime? B_DataSkierowania { get; set; }
        public string? B_TypBadania { get; set; }
        public string? P_imie { get; set; }
        public string? P_nazwisko { get; set; }
        public string? P_pesel { get; set; }
        public string? P_zawod { get; set; }
        public string? Firma_Nazwa { get; set; }
        public string? Firma_NIP { get; set; }
        public string? Firma_Cennik { get; set; }
        public int B_Badanie_ID { get; set; }

        // ✅ ZMIENIONE: Normalne properties zamiast computed
        public bool? B_ksiazeczka { get; set; }
        public bool? B_Zaswiadczenie { get; set; }

        // Computed property dla formatowania PESEL w UI
        public string?FormattedPesel
        {
            get
            {
                if (string.IsNullOrWhiteSpace(P_pesel)) return string.Empty;
                var digits = new string(P_pesel.Where(char.IsDigit).ToArray());
                if (digits.Length >= 11)
                {
                    digits = digits.Substring(0, 11);
                    return digits.Substring(0, 6) + " " + digits.Substring(6, 2) + " " + digits.Substring(8, 3);
                }
                if (digits.Length > 6)
                {
                    var part1 = digits.Substring(0, 6);
                    var rest = digits.Substring(6);
                    if (rest.Length > 2)
                        return part1 + " " + rest.Substring(0, 2) + " " + rest.Substring(2);
                    return part1 + " " + rest;
                }
                return digits;
            }
        }

        // Alias dla XAML
        public string?B_Stanowisko => P_zawod ?? string.Empty;
    }

    /// <summary>
    /// ViewModel dla widoku dwukolumnowego: lewa kolumna = lista skierowań, prawa = edycja badania.
    /// Całość niezależna od starych plików BadaniaEditViewModel.
    /// </summary>
    public class BadaniaNewViewModel : INotifyPropertyChanged
    {
        private static readonly CultureInfo PlCulture = new CultureInfo("pl-PL");
        private readonly AccessDbContext _db;

        private ObservableCollection<WizytaRecordBadania> _skierowania;
        private ObservableCollection<WizytaRecordBadania> _allSkierowania;
        private WizytaRecordBadania _selectedSkierowanie;

        // Flaga: trwa zapis+refresh — blokuje zbędne odświeżanie z IsVisibleChanged
        private bool _isRefreshingAfterSave;
        public bool IsRefreshingAfterSave => _isRefreshingAfterSave;

        // Pola edycji badania
        private DateTime _dataBadania;
        private DateTime _dataWaznosci;
        private string?_selectedWynik;
        private string?_nrKsiegi;
        private string?_selectedCennik;

        // ✅ NOWE: Selektor lat ważności
        private int _validityYears = 3;
        public int ValidityYears
        {
            get => _validityYears;
            set
            {
                if (_validityYears == value) return;
                _validityYears = value;
                OnPropertyChanged();
                DataWaznosci = _dataBadania.AddYears(value);
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
                    Label = $"{targetYear}"
                });
            }
        }

        public class ValidityYearItem
        {
            public int Years { get; set; }
            public string?Label { get; set; } = string.Empty;
        }

        // ✅ NOWE: Domyślna data badania
        private bool _useCustomDefaultDate;
        private DateTime _customDefaultDate;

        // 8 pól cenowych - ZMIANA NA NULLABLE
        private decimal? _cena1;
        private decimal? _cena2;
        private decimal? _cena3;
        private decimal? _cena4;
        private decimal? _cena5;
        private decimal? _cena6;
        private decimal? _cena7;
        private decimal? _cena8;

        // Ceny z cennika (do wyświetlenia w labelach)
        private decimal? _priceBasic;
        private decimal? _priceLaryngologist;
        private decimal? _priceOphthalmologist;
        private decimal? _priceSanitary;
        private decimal? _priceLipidogram;
        private decimal? _priceEKG;
        private decimal? _priceHealthClinic;
        private decimal? _priceOther;

        // Pola filtrowania
        private string?_filterText;
        private string?_selectedFilter;

        public ObservableCollection<WizytaRecordBadania> Skierowania
        {
            get => _skierowania;
            set
            {
                _skierowania = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SkierowaniaCount));
            }
        }

        // Opcje filtrowania
        public ObservableCollection<string> FilterOptions { get; }

        public string?FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public string?SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public WizytaRecordBadania SelectedSkierowanie
        {
            get => _selectedSkierowanie;
            set
            {
                _selectedSkierowanie = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SkierowanieInfo));

                // Po wybraniu skierowania, załaduj jego dane do edycji
                if (_selectedSkierowanie != null)
                {
                    LoadSkierowanieForEdit(_selectedSkierowanie);
                }
            }
        }

        public int SkierowaniaCount => Skierowania?.Count ?? 0;

        public string?SkierowanieInfo => SelectedSkierowanie != null
            ? $"Karta Badań #{SelectedSkierowanie.B_ID} - {SelectedSkierowanie.P_imie} {SelectedSkierowanie.P_nazwisko} "
            : "Wybierz skierowanie z listy";

        // ✅ NOWE: Domyślna data badania (checkbox + DatePicker)
        public bool UseCustomDefaultDate
        {
            get => _useCustomDefaultDate;
            set
            {
                _useCustomDefaultDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime CustomDefaultDate
        {
            get => _customDefaultDate;
            set
            {
                _customDefaultDate = value;
                OnPropertyChanged();
            }
        }

        // Dane badania
        public DateTime DataBadania
        {
            get => _dataBadania;
            set
            {
                _dataBadania = value;
                OnPropertyChanged();
                // Automatycznie ustaw datę ważności wg wybranej liczby lat
                DataWaznosci = value.AddYears(_validityYears);
                // Odśwież etykiety selektora (dwie ostatnie cyfry roku)
                UpdateValidityYearLabels();
            }
        }

        public DateTime DataWaznosci
        {
            get => _dataWaznosci;
            set
            {
                _dataWaznosci = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> WynikOptions { get; }
        public string?SelectedWynik
        {
            get => _selectedWynik;
            set
            {
                _selectedWynik = value;
                OnPropertyChanged();
            }
        }

        public string?NrKsiegi
        {
            get => _nrKsiegi;
            set
            {
                _nrKsiegi = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> CennikOptions { get; }
        public string?SelectedCennik
        {
            get => _selectedCennik;
            set
            {
                _selectedCennik = value;
                OnPropertyChanged();
                LoadCennikPrices();
            }
        }

        // 8 pól cenowych z automatycznym przeliczaniem sumy
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

        // Teksty cen z cennika (dla labelów w UI)
        public string?PriceBasicText => _priceBasic.HasValue ? $"{_priceBasic.Value:N2} zł" : "";
        public string?PriceLaryngologistText => _priceLaryngologist.HasValue ? $"{_priceLaryngologist.Value:N2} zł" : "";
        public string?PriceOphthalmologistText => _priceOphthalmologist.HasValue ? $"{_priceOphthalmologist.Value:N2} zł" : "";
        public string?PriceSanitaryText => _priceSanitary.HasValue ? $"{_priceSanitary.Value:N2} zł" : "";
        public string?PriceLipidogramText => _priceLipidogram.HasValue ? $"{_priceLipidogram.Value:N2} zł" : "";
        public string?PriceEKGText => _priceEKG.HasValue ? $"{_priceEKG.Value:N2} zł" : "";
        public string?PriceHealthClinicText => _priceHealthClinic.HasValue ? $"{_priceHealthClinic.Value:N2} zł" : "";
        public string?PriceOtherText => _priceOther.HasValue ? $"{_priceOther.Value:N2} zł" : "";

        // SUMA - obsługa nullable
        public string SumaText => $"SUMA: {((Cena1 ?? 0) + (Cena2 ?? 0) + (Cena3 ?? 0) + (Cena4 ?? 0) + (Cena5 ?? 0) + (Cena6 ?? 0) + (Cena7 ?? 0) + (Cena8 ?? 0)):0} zł";

        public ICommand ?RefreshCommand { get; }
        public ICommand ?SaveBadanieCommand { get; }
        public ICommand ?ClearCommand { get; }
        // ✅ NOWA KOMENDA: Przejście do zakładki Rejestracja
        public ICommand ?GoToRegistrationCommand { get; }

        // ✅ POPRAWIONE: Event do resetowania przycisków toggle
        public event Action? ToggleButtonsResetRequested;

        public BadaniaNewViewModel()
        {
            _db = new AccessDbContext();
            _skierowania = new ObservableCollection<WizytaRecordBadania>();
            _allSkierowania = new ObservableCollection<WizytaRecordBadania>();

            // Inicjalizacja opcji filtrowania
            FilterOptions = new ObservableCollection<string> { "All", "ID", "Imię", "Nazwisko", "Firma" };
            SelectedFilter = "All";
            FilterText = string.Empty;

            // Inicjalizacja danych badania
            DataBadania = DateTime.Now;
            DataWaznosci = DateTime.Now.AddYears(3);

            // ✅ NOWE: Inicjalizacja selektora lat ważności (domyślnie +3 lata)
            _validityYears = 3;
            UpdateValidityYearLabels();

            // ✅ NOWE: Inicjalizacja domyślnej daty badania
            UseCustomDefaultDate = false; // Domyślnie wyłączone
            CustomDefaultDate = DateTime.Now; // Domyślnie dzisiejsza data

            WynikOptions = new ObservableCollection<string> { "Pozytywne", "Negatywne" };
            SelectedWynik = "Pozytywne";

            // Załaduj dostępne cenniki z bazy
            CennikOptions = new ObservableCollection<string>();
            try
            {
                // Użyj WizytyRepository bezpośrednio (niezależnie od AccessDbContext)
                var repo = new WizytyRepository();
                var cenniki = repo.GetCennikOptions();
                foreach (var c in cenniki)
                    CennikOptions.Add(c);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"Error loading cennik options: {ex}");
            }

            RefreshCommand = new RelayCommand(_ => RefreshFromDb());
            SaveBadanieCommand = new RelayCommand(_ => SaveBadanie());
            ClearCommand = new RelayCommand(_ => ClearEditForm());
            // ✅ NOWA KOMENDA: Inicjalizacja GoToRegistrationCommand
            GoToRegistrationCommand = new RelayCommand(_ => GoToRegistration());

            // Załaduj dane przy starcie
            RefreshFromDb();
        }

        public void RefreshFromDb()
        {
            try
            {
                var allSkierowania = _db.GetSkierowania();

                var skierowaniaDoZamkniecia = allSkierowania
                    .Where(s => s.Bad_Data == null)
                    .Select(s => new WizytaRecordBadania
                    {
                        B_ID = s.B_ID ?? 0,
                        P_ID = s.B_Pacjent_ID,
                        Firma_id = 0,
                        B_DataSkierowania = s.B_DataSkierowania,
                        B_TypBadania = s.B_TypBadania,
                        P_imie = s.P_imie,
                        P_nazwisko = s.P_nazwisko,
                        P_pesel = s.P_pesel,
                        P_zawod = s.P_zawod ?? string.Empty,
                        Firma_Nazwa = s.Nazwa,
                        Firma_NIP = s.Firma_NIP ?? string.Empty,
                        Firma_Cennik = s.Firma_Cennik ?? string.Empty,
                        B_ksiazeczka = s.B_książeczka_sanepid,      // ✅ DODANE
                        B_Zaswiadczenie = s.B_Zaswiadczenie,        // ✅ DODANE
                        B_Badanie_ID = 0
                    })
                    .ToList();

                _allSkierowania = new ObservableCollection<WizytaRecordBadania>(skierowaniaDoZamkniecia);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"BadaniaNewViewModel.RefreshFromDb error: {ex}");
                MessageBox.Show($"Błąd podczas ładowania skierowań: {ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _allSkierowania = new ObservableCollection<WizytaRecordBadania>();
                Skierowania = new ObservableCollection<WizytaRecordBadania>();
            }
        }

        private void LoadSkierowanieForEdit(WizytaRecordBadania skierowanie)
        {
            try
            {
                // ✅ NAJPIERW wyczyść pola cen
                ClearPrices();

                // ✅ NOWE: Ustaw datę badania na podstawie checkboxa
                if (UseCustomDefaultDate)
                {
                    DataBadania = CustomDefaultDate;
                }
                else
                {
                    DataBadania = DateTime.Now;
                }

                // ✅ Reset selektora ważności do domyślnych +3 lat
                ValidityYears = 3;

                // ✅ POTEM ustaw cennik z firmy (to wywołuje LoadCennikPrices())
                if (!string.IsNullOrEmpty(skierowanie.Firma_Cennik))
                {
                    SelectedCennik = skierowanie.Firma_Cennik;
                }
                else if (CennikOptions.Count > 0)
                {
                    SelectedCennik = CennikOptions[0];
                }
                else
                {
                    // brak dostępnych cenników
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadSkierowanieForEdit error: {ex}");
            }
        }

        private void LoadCennikPrices()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedCennik))
                    return;

                var cennik = _db.GetCennikByName(SelectedCennik);
                if (cennik != null)
                {
                    _priceBasic = cennik.CenaPodstawowa;
                    _priceLaryngologist = cennik.CenaLaryngolog;
                    _priceOphthalmologist = cennik.CenaOkulista;
                    _priceSanitary = cennik.CenaSanitariusz;
                    _priceLipidogram = cennik.CenaLipidogram;
                    _priceEKG = cennik.CenaEKG;
                    _priceHealthClinic = cennik.CenaPoradnia;
                    _priceOther = cennik.CenaInne;

                    // Odśwież wszystkie teksty cen
                    OnPropertyChanged(nameof(PriceBasicText));
                    OnPropertyChanged(nameof(PriceLaryngologistText));
                    OnPropertyChanged(nameof(PriceOphthalmologistText));
                    OnPropertyChanged(nameof(PriceSanitaryText));
                    OnPropertyChanged(nameof(PriceLipidogramText));
                    OnPropertyChanged(nameof(PriceEKGText));
                    OnPropertyChanged(nameof(PriceHealthClinicText));
                    OnPropertyChanged(nameof(PriceOtherText));
                }
                // cennik nie znaleziony — pola cen pozostają bez zmian
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadCennikPrices error: {ex.Message}\n{ex.StackTrace}");
            }
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

        private void SaveBadanie()
        {
            try
            {
                if (SelectedSkierowanie == null)
                {
                    MessageBox.Show("Nie wybrano skierowania.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var pacjent = _db.GetPacjentById(SelectedSkierowanie.P_ID);
                if (pacjent == null)
                {
                    MessageBox.Show("Nie można odnaleźć danych pacjenta.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ Jedno pobranie rejestracji – współdzielone między walidacją a zmianą statusu
                var rejestracje = _db.GetRejestracje();

                // ✅ WALIDACJA: Uzupełnij brakujące daty skierowania i rejestracji
                UzupelnijBrakujaceDaty(SelectedSkierowanie, pacjent, rejestracje);

                // ✅ Konwertuj nullable decimal na decimal (null → 0)
                var badanieRec = new AccessDbContext.BadanieRecord
                {
                    Bad_S_ID = SelectedSkierowanie.B_ID,
                    Bad_P_ID = SelectedSkierowanie.P_ID,
                    Bad_F_ID = pacjent.FirmaId ?? 0,
                    Bad_bn_cennik = SelectedCennik,
                    Bad_Typ = SelectedSkierowanie.B_TypBadania,
                    Bad_Data = DataBadania,
                    Bad_Data_Do = DataWaznosci,
                    Bad_Wynik = SelectedWynik,
                    Bad_Cena1 = Cena1 ?? 0m,
                    Bad_Cena2 = Cena2 ?? 0m,
                    Bad_Cena3 = Cena3 ?? 0m,
                    Bad_Cena4 = Cena4 ?? 0m,
                    Bad_Cena5 = Cena5 ?? 0m,
                    Bad_Cena6 = Cena6 ?? 0m,
                    Bad_Cena7 = Cena7 ?? 0m,
                    Bad_Cena8 = Cena8 ?? 0m,
                    Bad_Razem = (Cena1 ?? 0) + (Cena2 ?? 0) + (Cena3 ?? 0) + (Cena4 ?? 0) +
                                (Cena5 ?? 0) + (Cena6 ?? 0) + (Cena7 ?? 0) + (Cena8 ?? 0),
                    Bad_Nr_KS = NrKsiegi,
                    Bad_END = false
                };

                int newBadId = _db.AddBadanie(badanieRec);

                if (newBadId > 0)
                {
                    bool linkOk = _db.UpdateSkierowanieBadanieId(SelectedSkierowanie.B_ID, newBadId);

                    if (linkOk)
                    {
                        // ✅ Zmień status wizyty na "zamknięta" (rejestracje już pobrane wyżej)
                        ZmienStatusWizytyNaOdbyta(SelectedSkierowanie.B_ID, rejestracje);
                        _isRefreshingAfterSave = true;
                        ClearEditForm();
                        RefreshFromDb();
                        _isRefreshingAfterSave = false;
                    }
                    else
                    {
                        MessageBox.Show("Badanie zapisane, ale nie udało się przypisać go do skierowania.",
                            "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Zapis badania nie powiódł się.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisu badania:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                // System.Diagnostics.Debug.WriteLine($"SaveBadanie error: {ex}");
            }
        }

        /// <summary>
        /// Sprawdza i uzupełnia brakującą datę skierowania oraz brakujący rekord rejestracji.
        /// Używa daty badania jako wartości zastępczej.
        /// </summary>
        private void UzupelnijBrakujaceDaty(WizytaRecordBadania skierowanie, AccessDbContext.PacjentRecord pacjent, List<AccessDbContext.RejestracjaRecord> rejestracje)
        {
            try
            {
                var dataZastępcza = DataBadania;

                // ── 1. Brakująca data skierowania ──────────────────────────────
                if (!skierowanie.B_DataSkierowania.HasValue)
                {
                    var skierRec = new AccessDbContext.SkierowanieRecord
                    {
                        PacjentId    = skierowanie.P_ID,
                        FirmaId      = pacjent.FirmaId,
                        DataSkierowania = dataZastępcza,
                        TypBadania   = skierowanie.B_TypBadania,
                        Stanowisko   = skierowanie.P_zawod,
                        Zaswiadczenie = skierowanie.B_Zaswiadczenie ?? false,
                        Ksiazeczka   = skierowanie.B_ksiazeczka ?? false,
                        Activ        = true
                    };

                    _db.UpdateSkierowanie(skierowanie.B_ID, skierRec);

                    // Zaktualizuj lokalny obiekt
                    skierowanie.B_DataSkierowania = dataZastępcza;
                }

                // ── 2. Brakujący rekord rejestracji (używa przekazanej listy) ─
                bool rejestracjaIstnieje = rejestracje.Any(r => r.R_S_ID == skierowanie.B_ID);

                if (!rejestracjaIstnieje)
                {
                    var rejRec = new AccessDbContext.RejestracjaRecord
                    {
                        R_S_ID   = skierowanie.B_ID,
                        R_P_ID   = skierowanie.P_ID,
                        R_Data   = dataZastępcza,
                        R_GG_MM  = dataZastępcza,
                        RStatus  = "zamknięta",
                        R_Subject = $"{skierowanie.P_imie} {skierowanie.P_nazwisko}",
                        R_Uwagi  = "Rekord uzupełniony automatycznie przy zapisie badania"
                    };

                    _db.AddRejestracja(rejRec);
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[UzupelnijBrakujaceDaty] Błąd: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Zmienia status wizyty na "Odbyta" po zapisaniu badania
        /// </summary>
        private bool ZmienStatusWizytyNaOdbyta(int bId, List<AccessDbContext.RejestracjaRecord> rejestracje)
        {
            try
            {
                // Używa przekazanej listy rejestracji (bez ponownego pobierania z DB)
                var wizyta = rejestracje.FirstOrDefault(r => r.R_S_ID == bId);

                if (wizyta == null || !wizyta.R_ID.HasValue)
                    return false;

                // Zmień status na "zamknięta"
                var record = new AccessDbContext.RejestracjaRecord
                {
                    R_Data = wizyta.R_Data,
                    RStatus = "zamknięta",
                    R_S_ID = wizyta.R_S_ID,
                    R_GG_MM = wizyta.R_GG_MM,
                    R_Subject = wizyta.R_Subject,
                    R_Uwagi = wizyta.R_Uwagi
                };

                return _db.UpdateRejestracja(wizyta.R_ID.Value, record);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ ZmienStatusWizytyNaOdbyta error: {ex.Message}");
                return false;
            }
        }

        private void ClearEditForm()
        {
            DataBadania = DateTime.Now;
            DataWaznosci = DateTime.Now.AddYears(3);
            SelectedWynik = "Pozytywne";
            NrKsiegi = string.Empty;
            ClearPrices();
            // RefreshFromDb() usunięte — wywoływane jawnie tam gdzie potrzeba
        }

        private void ClearPrices()
        {
            // ✅ Ustaw pola cen na null (TextBoxy będą puste)
            Cena1 = Cena2 = Cena3 = Cena4 = Cena5 = Cena6 = Cena7 = Cena8 = null;

            // Odśwież wyświetlanie cen z cennika
            OnPropertyChanged(nameof(PriceBasicText));
            OnPropertyChanged(nameof(PriceLaryngologistText));
            OnPropertyChanged(nameof(PriceOphthalmologistText));
            OnPropertyChanged(nameof(PriceSanitaryText));
            OnPropertyChanged(nameof(PriceLipidogramText));
            OnPropertyChanged(nameof(PriceEKGText));
            OnPropertyChanged(nameof(PriceHealthClinicText));
            OnPropertyChanged(nameof(PriceOtherText));

            // ✅ DODANE: Wywołaj reset przycisków toggle
            ToggleButtonsResetRequested?.Invoke();
        }

        // Zastosuj filtr do listy skierowań
        private void ApplyFilter()
        {
            try
            {
                if (_allSkierowania == null || _allSkierowania.Count == 0)
                {
                    Skierowania = new ObservableCollection<WizytaRecordBadania>();
                    return;
                }

                // Jeśli brak tekstu filtrowania, pokaż wszystko
                if (string.IsNullOrWhiteSpace(FilterText))
                {
                    Skierowania = new ObservableCollection<WizytaRecordBadania>(_allSkierowania);
                    return;
                }

                var searchText = FilterText.Trim();
                IEnumerable<WizytaRecordBadania> filtered;

                // ✅ SMART FILTER: Wykryj prefix #XXXX dla ID skierowania
                if (searchText.StartsWith("#") && searchText.Length > 1)
                {
                    // Usuń prefix # i wiodące zera
                    var idText = searchText.Substring(1).TrimStart('0');

                    if (int.TryParse(idText, out int searchId))
                    {

                        // Szukaj DOKŁADNIE po B_ID (ID skierowania)
                        filtered = _allSkierowania.Where(s => s.B_ID == searchId);
                    }
                    else
                    {
                        // Niepoprawny format po #
                        filtered = Enumerable.Empty<WizytaRecordBadania>();
                    }
                }
                else
                {
                    // ✅ STANDARD FILTER: Szukaj według wybranego filtra
                    var filterTextLower = searchText.ToLower();

                    switch (SelectedFilter)
                    {
                        case "ID":
                            filtered = _allSkierowania.Where(s =>
                                s.B_ID.ToString().Contains(searchText));
                            break;

                        case "Imię":
                            filtered = _allSkierowania.Where(s =>
                                (s.P_imie ?? "").ToLower().Contains(filterTextLower) ||
                                TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_imie ?? "", filterTextLower));
                            break;

                        case "Nazwisko":
                            filtered = _allSkierowania.Where(s =>
                                (s.P_nazwisko ?? "").ToLower().Contains(filterTextLower) ||
                                TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_nazwisko ?? "", filterTextLower));
                            break;

                        case "Firma":
                            filtered = _allSkierowania.Where(s =>
                                (s.Firma_Nazwa ?? "").ToLower().Contains(filterTextLower) ||
                                TextNormalizationHelper.ContainsIgnoringDiacritics(s.Firma_Nazwa ?? "", filterTextLower));
                            break;

                        case "All":
                        default:
                            // Szukaj we wszystkich polach z normalizacją polskich znaków
                            filtered = _allSkierowania.Where(s =>
                                s.B_ID.ToString().Contains(searchText) ||
                                (s.P_imie ?? "").ToLower().Contains(filterTextLower) ||
                                TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_imie ?? "", filterTextLower) ||
                                (s.P_nazwisko ?? "").ToLower().Contains(filterTextLower) ||
                                TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_nazwisko ?? "", filterTextLower) ||
                                (s.Firma_Nazwa ?? "").ToLower().Contains(filterTextLower) ||
                                TextNormalizationHelper.ContainsIgnoringDiacritics(s.Firma_Nazwa ?? "", filterTextLower));
                            break;
                    }
                }

                Skierowania = new ObservableCollection<WizytaRecordBadania>(filtered);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ApplyFilter error: {ex}");
                Skierowania = new ObservableCollection<WizytaRecordBadania>(_allSkierowania);
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Przełącza na zakładkę "Rejestracja" (WizytyViewView)
        /// </summary>
        private void GoToRegistration()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                    return;

                var tabControl = FindVisualChild<Syncfusion.Windows.Tools.Controls.TabControlExt>(mainWindow);
                if (tabControl == null)
                    return;

                // Znajdź zakładkę "Rejestracja" (x:Name="Rejestracja" lub inny Name)
                foreach (var item in tabControl.Items)
                {
                    if (item is Syncfusion.Windows.Tools.Controls.TabItemExt tabItem)
                    {
                        // Sprawdź Name lub Header zakładki
                        if (tabItem.Name == "Rejestracja" ||
                            (tabItem.Header?.ToString()?.Contains("Rejestracja") ?? false))
                        {
                            tabControl.SelectedItem = tabItem;

                            if (tabItem.Content is WizytyViewView wizytyView &&
                                wizytyView.DataContext is WizytyViewViewModel wizytyVM)
                            {
                                wizytyVM.RefreshFromDb();

                                if (wizytyVM.SelectedDate.HasValue)
                                {
                                    // LoadPacjenciNaDzien() jest wywoływane automatycznie przez setter SelectedDate,
                                    // więc wystarczy "dotknąć" właściwość aby wywołać ponowne ładowanie
                                    var currentDate = wizytyVM.SelectedDate;
                                    wizytyVM.SelectedDate = null;
                                    wizytyVM.SelectedDate = currentDate;
                                }

                                NotificationHelper.ShowSuccess("Odświeżono listę pacjentów");
                            }
                            else
                            {
                                NotificationHelper.ShowSuccess("Przełączono na zakładkę Rejestracja");
                            }

                            return;
                        }
                    }
                }

                // nie znaleziono zakładki Rejestracja
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ GoToRegistration error: {ex.Message}");
                NotificationHelper.ShowError($"Błąd przełączania zakładki: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ HELPER: Znajduje kontrolkę wizualną typu T w drzewie wizualnym
        /// </summary>
        private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                    return result;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }

            return null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
