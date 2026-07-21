using ASMED.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels.Badania
{
    /// <summary>
    /// Rekord badania do edycji (z istniejącym Bad_ID)
    /// </summary>
    public class BadanieRecordEdit
    {
        public int Bad_ID { get; set; }
        public int B_ID { get; set; } // ID skierowania
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

        // Dane badania
        public DateTime? Bad_Data { get; set; }
        public DateTime? Bad_Data_Do { get; set; }
        public string? Bad_Wynik { get; set; }
        public string? Bad_Nr_KS { get; set; }
        public string? Bad_bn_cennik { get; set; }
        public string? Bad_Fakt { get; set; } // ✅ DODANE: Numer faktury

        // Ceny
        public decimal? Bad_Cena1 { get; set; }
        public decimal? Bad_Cena2 { get; set; }
        public decimal? Bad_Cena3 { get; set; }
        public decimal? Bad_Cena4 { get; set; }
        public decimal? Bad_Cena5 { get; set; }
        public decimal? Bad_Cena6 { get; set; }
        public decimal? Bad_Cena7 { get; set; }
        public decimal? Bad_Cena8 { get; set; }
        public decimal? Bad_Razem { get; set; }

        public bool? B_ksiazeczka { get; set; }
        public bool? B_Zaswiadczenie { get; set; }

        // Computed properties
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

        public string?B_Stanowisko => P_zawod ?? string.Empty;
    }

    /// <summary>
    /// ViewModel dla widoku edycji badań: lewa kolumna = lista badań, prawa = edycja badania.
    /// Niezależny od starych plików BadaniaEditViewModel.
    /// </summary>
    public class BadaniaEditNewViewModel : INotifyPropertyChanged
    {
        private static readonly CultureInfo PlCulture = new CultureInfo("pl-PL");
        private readonly AccessDbContext _db;

        private ObservableCollection<BadanieRecordEdit> _badania;
        private ObservableCollection<BadanieRecordEdit> _allBadania;
        private BadanieRecordEdit _selectedBadanie;

        // Pola edycji badania
        private DateTime _dataBadania;
        private DateTime _dataWaznosci;
        private string?_selectedWynik;
        private string?_nrKsiegi;
        private string?_selectedCennik;

        // 8 pól cenowych - NULLABLE
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

        // ✅ DODANE: Filtrowanie według daty
        private string?_selectedDateFilter;
        private DateTime? _filterDateFrom;
        private DateTime? _filterDateTo;

        // ✅ NOWE: Flaga do śledzenia czy lista została już załadowana
        private bool _isFirstSearch = true;

        public ObservableCollection<BadanieRecordEdit> Skierowania
        {
            get => _badania;
            set
            {
                _badania = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SkierowaniaCount));
            }
        }

        public ObservableCollection<string> FilterOptions { get; }

        // ✅ DODANE: Opcje filtrowania dat
        public ObservableCollection<string> DateFilterOptions { get; }

        public string?FilterText
        {
            get => _filterText;
            set
            {
                // ✅ NOWE: Automatyczne odświeżanie gdy użytkownik wpisuje tekst PO RAZ PIERWSZY
                bool wasEmpty = string.IsNullOrWhiteSpace(_filterText);
                bool isNowFilled = !string.IsNullOrWhiteSpace(value);

                _filterText = value;
                OnPropertyChanged();

                // ✅ Jeśli pole było puste i teraz ma wartość - odśwież listę z bazy
                if (wasEmpty && isNowFilled && _isFirstSearch)
                {
                    // System.Diagnostics.Debug.WriteLine("FilterText: Pierwsze wyszukiwanie - odświeżam listę z bazy");
                    _isFirstSearch = false;
                    RefreshFromDb();
                }
                else
                {
                    ApplyFilter();
                }
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

        // ✅ DODANE: Wybrany filtr daty
        public string?SelectedDateFilter
        {
            get => _selectedDateFilter;
            set
            {
                _selectedDateFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCustomDateRangeVisible));
                ApplyFilter();
            }
        }

        // ✅ DODANE: Daty dla "Wybrany okres"
        public DateTime? FilterDateFrom
        {
            get => _filterDateFrom;
            set
            {
                _filterDateFrom = value;
                OnPropertyChanged();
                if (SelectedDateFilter == "Wybrany okres")
                    ApplyFilter();
            }
        }

        public DateTime? FilterDateTo
        {
            get => _filterDateTo;
            set
            {
                _filterDateTo = value;
                OnPropertyChanged();
                if (SelectedDateFilter == "Wybrany okres")
                    ApplyFilter();
            }
        }

        // ✅ DODANE: Widoczność panelu dat niestandardowych
        public bool IsCustomDateRangeVisible => SelectedDateFilter == "Wybrany okres";

        public BadanieRecordEdit SelectedSkierowanie
        {
            get => _selectedBadanie;
            set
            {
                _selectedBadanie = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SkierowanieInfo));

                if (_selectedBadanie != null)
                {
                    LoadBadanieForEdit(_selectedBadanie);
                }
            }
        }

        public int SkierowaniaCount => Skierowania?.Count ?? 0;

        public string?SkierowanieInfo => SelectedSkierowanie != null
            ? $"Badanie #{SelectedSkierowanie.Bad_ID} - {SelectedSkierowanie.P_imie} {SelectedSkierowanie.P_nazwisko}"
            : "Wybierz badanie z listy";

        // Dane badania
        public DateTime DataBadania
        {
            get => _dataBadania;
            set
            {
                _dataBadania = value;
                OnPropertyChanged();
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

        // Teksty cen z cennika
        public string?PriceBasicText => _priceBasic.HasValue ? $"{_priceBasic.Value:0} zł" : "";
        public string?PriceLaryngologistText => _priceLaryngologist.HasValue ? $"{_priceLaryngologist.Value:0} zł" : "";
        public string?PriceOphthalmologistText => _priceOphthalmologist.HasValue ? $"{_priceOphthalmologist.Value:0} zł" : "";
        public string?PriceSanitaryText => _priceSanitary.HasValue ? $"{_priceSanitary.Value:0} zł" : "";
        public string?PriceLipidogramText => _priceLipidogram.HasValue ? $"{_priceLipidogram.Value:0} zł" : "";
        public string?PriceEKGText => _priceEKG.HasValue ? $"{_priceEKG.Value:0} zł" : "";
        public string?PriceHealthClinicText => _priceHealthClinic.HasValue ? $"{_priceHealthClinic.Value:0} zł" : "";
        public string?PriceOtherText => _priceOther.HasValue ? $"{_priceOther.Value:0} zł" : "";

        public string SumaText => $"SUMA: {((Cena1 ?? 0) + (Cena2 ?? 0) + (Cena3 ?? 0) + (Cena4 ?? 0) + (Cena5 ?? 0) + (Cena6 ?? 0) + (Cena7 ?? 0) + (Cena8 ?? 0)):0} zł";

        public ICommand ?RefreshCommand { get; }
        public ICommand ?SaveBadanieCommand { get; }
        public ICommand ?ClearCommand { get; }
        public ICommand ?DeleteCommand { get; }
        public ICommand ?CleaAllCommand { get; }
        public object SelectedSkierowane { get; private set; }

        public event Action? ToggleButtonsSyncRequested; // ✅ NOWY EVENT - synchronizacja przycisków z cenami



        public BadaniaEditNewViewModel()
        {
            _db = new AccessDbContext();
            _badania = new ObservableCollection<BadanieRecordEdit>();
            _allBadania = new ObservableCollection<BadanieRecordEdit>();

            FilterOptions = new ObservableCollection<string> { "All", "ID", "Imię", "Nazwisko", "Firma", "Faktura", "Skier. ID" };
            SelectedFilter = "All";
            FilterText = string.Empty;

            // ✅ DODANE: Inicjalizacja opcji filtrowania dat
            DateFilterOptions = new ObservableCollection<string>
            {
                "All",
                "Bieżący Miesiąc",
                "Poprzedni Miesiąc",
                "Bieżący Rok",
                "Poprzedni Rok",
                "Wybrany okres"
            };
            SelectedDateFilter = "All";

            DataBadania = DateTime.Now;
            DataWaznosci = DateTime.Now.AddYears(3);

            WynikOptions = new ObservableCollection<string> { "Pozytywne", "Negatywne" };
            SelectedWynik = "Pozytywne";

            CennikOptions = new ObservableCollection<string>();
            try
            {
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
            SaveBadanieCommand = new RelayCommand(_ => UpdateBadanie());
            ClearCommand = new RelayCommand(_ => ClearEditForm());

            RefreshFromDb();
        }

        public void RefreshFromDb()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("RefreshFromDb: Ładowanie badań z bazy danych...");

                // ✅ ZRESETUJ flagę przy ręcznym odświeżeniu (F1 lub przycisk)
                _isFirstSearch = true;

                // ✅ Używamy nowej metody GetBadaniaWithSkierowania()
                var badaniaDto = _db.GetBadaniaWithSkierowania();

                // Mapuj BadanieWithSkierowanieDto → BadanieRecordEdit
                _allBadania = new ObservableCollection<BadanieRecordEdit>(
                    badaniaDto.Select(dto => new BadanieRecordEdit
                    {
                        Bad_ID = dto.Bad_ID,
                        B_ID = dto.B_ID,
                        P_ID = dto.P_ID,
                        Firma_id = dto.Firma_id,
                        B_DataSkierowania = dto.B_DataSkierowania,
                        B_TypBadania = dto.B_TypBadania,
                        P_imie = dto.P_imie,
                        P_nazwisko = dto.P_nazwisko,
                        P_pesel = dto.P_pesel,
                        P_zawod = dto.P_zawod,
                        Firma_Nazwa = dto.Firma_Nazwa,
                        Firma_NIP = dto.Firma_NIP,
                        Firma_Cennik = dto.Firma_Cennik,
                        Bad_Data = dto.Bad_Data,
                        Bad_Data_Do = dto.Bad_Data_Do,
                        Bad_Wynik = dto.Bad_Wynik,
                        Bad_Nr_KS = dto.Bad_Nr_KS,
                        Bad_bn_cennik = dto.Bad_bn_cennik,
                        Bad_Fakt = dto.Bad_Fakt, // ✅ DODANE
                        Bad_Cena1 = dto.Bad_Cena1,
                        Bad_Cena2 = dto.Bad_Cena2,
                        Bad_Cena3 = dto.Bad_Cena3,
                        Bad_Cena4 = dto.Bad_Cena4,
                        Bad_Cena5 = dto.Bad_Cena5,
                        Bad_Cena6 = dto.Bad_Cena6,
                        Bad_Cena7 = dto.Bad_Cena7,
                        Bad_Cena8 = dto.Bad_Cena8,
                        Bad_Razem = dto.Bad_Razem,
                        B_ksiazeczka = dto.B_ksiazeczka,
                        B_Zaswiadczenie = dto.B_Zaswiadczenie
                    }).ToList()
                );

                ApplyFilter();
                ClearEditForm();

                // System.Diagnostics.Debug.WriteLine($"RefreshFromDb: ✅ Załadowano {_allBadania.Count} badań");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"RefreshFromDb ERROR: {ex}");
                MessageBox.Show($"Błąd podczas ładowania badań: {ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _allBadania = new ObservableCollection<BadanieRecordEdit>();
                Skierowania = new ObservableCollection<BadanieRecordEdit>();
            }
        }

        private void LoadBadanieForEdit(BadanieRecordEdit badanie)
        {
            try
            {
                // Załaduj dane badania do formularza
                DataBadania = badanie.Bad_Data ?? DateTime.Now;
                DataWaznosci = badanie.Bad_Data_Do ?? DateTime.Now.AddYears(3);
                SelectedWynik = badanie.Bad_Wynik ?? "Pozytywne";
                NrKsiegi = badanie.Bad_Nr_KS ?? string.Empty;
                SelectedCennik = badanie.Bad_bn_cennik ?? badanie.Firma_Cennik ?? string.Empty;

                // Załaduj ceny
                Cena1 = badanie.Bad_Cena1;
                Cena2 = badanie.Bad_Cena2;
                Cena3 = badanie.Bad_Cena3;
                Cena4 = badanie.Bad_Cena4;
                Cena5 = badanie.Bad_Cena5;
                Cena6 = badanie.Bad_Cena6;
                Cena7 = badanie.Bad_Cena7;
                Cena8 = badanie.Bad_Cena8;

                // ✅ DODANE: Wywołaj event żeby widok zsynchronizował przyciski
                ToggleButtonsSyncRequested?.Invoke();

                // System.Diagnostics.Debug.WriteLine($"LoadBadanieForEdit: Loaded badanie {badanie.Bad_ID}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadBadanieForEdit error: {ex}");
            }
        }

        private void LoadCennikPrices()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedCennik))
                {
                    // System.Diagnostics.Debug.WriteLine("LoadCennikPrices: SelectedCennik is null or empty!");
                    return;
                }

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

                    OnPropertyChanged(nameof(PriceBasicText));
                    OnPropertyChanged(nameof(PriceLaryngologistText));
                    OnPropertyChanged(nameof(PriceOphthalmologistText));
                    OnPropertyChanged(nameof(PriceSanitaryText));
                    OnPropertyChanged(nameof(PriceLipidogramText));
                    OnPropertyChanged(nameof(PriceEKGText));
                    OnPropertyChanged(nameof(PriceHealthClinicText));
                    OnPropertyChanged(nameof(PriceOtherText));
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LoadCennikPrices error: {ex.Message}");
            }
        }

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

        private void UpdateBadanie()
        {
            try
            {
                if (SelectedSkierowanie == null)
                {
                    MessageBox.Show("Nie wybrano badania.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var badanieRec = new AccessDbContext.BadanieRecord
                {
                    Bad_ID = SelectedSkierowanie.Bad_ID,
                    Bad_S_ID = SelectedSkierowanie.B_ID,
                    Bad_P_ID = SelectedSkierowanie.P_ID,
                    Bad_F_ID = SelectedSkierowanie.Firma_id,
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

                bool updateOk = _db.UpdateBadanie(SelectedSkierowanie.Bad_ID, badanieRec);


                if (updateOk)
                {
                    // aktualizuj status wizyty
                    bool linkOk = _db.UpdateSkierowanieBadanieId(SelectedSkierowanie.B_ID, SelectedSkierowanie.Bad_ID);

                    if (linkOk)
                    {
                        // ✅ NOWE: Zmień status wizyty na "Odbyta" (jeśli istnieje powiązana rejestracja)
                        bool statusZmieniony = ZmienStatusWizytyNaOdbyta(SelectedSkierowanie.B_ID);

                        if (statusZmieniony)
                        {
                            // System.Diagnostics.Debug.WriteLine($"✅ Status wizyty zmieniony na 'zamknięta' dla B_ID={SelectedSkierowanie.B_ID}");
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono wizyty do zmiany statusu (B_ID={SelectedSkierowanie.B_ID})");
                        }

                        NotificationHelper.ShowInfo("Badanie zaktualizowane", $"Badanie ID = {SelectedSkierowanie.Bad_ID}");

                        // ✅ DODANE: Wyczyść przyciski toggle po aktualizacji
                        ClearEditForm();

                        RefreshFromDb();
                    }
                    else
                    {
                        MessageBox.Show("Aktualizacja badania nie powiodła się.",
                                  "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas aktualizacji badania:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                // System.Diagnostics.Debug.WriteLine($"UpdateBadanie error: {ex}");
            }
        }

        /// ✅ NOWA METODA: Zmienia status wizyty na "Odbyta" po zapisaniu badania
        /// </summary>
        private bool ZmienStatusWizytyNaOdbyta(int bId)
        {
            try
            {
                //System.Diagnostics.Debug.WriteLine($"🔄 ZmienStatusWizytyNaOdbyta: Szukam wizyty dla B_ID={bId}");
                //MessageBox.Show($"🔄 ZmienStatusWizytyNaOdbyta: Szukam wizyty dla B_ID={bId}");

                // Pobierz wszystkie rejestracje i znajdź pasującą (gdzie R_B_ID == B_ID skierowania)
                var rejestracje = _db.GetRejestracje();
                var wizyta = rejestracje.FirstOrDefault(r => r.R_S_ID == bId);

                if (wizyta == null || !wizyta.R_ID.HasValue)
                {
                    //System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono wizyty dla B_ID={bId}");
                    //MessageBox.Show($"⚠️ Nie znaleziono wizyty dla B_ID={bId}");
                    return false;
                }

                //System.Diagnostics.Debug.WriteLine($"✅ Znaleziono wizytę: R_ID={wizyta.R_ID}, aktualny status='{wizyta.RStatus}'");
                //MessageBox.Show($"✅ Znaleziono wizytę: R_ID={wizyta.R_ID}, aktualny status='{wizyta.RStatus}'");

                // Zmień status na "Odbyta"
                var record = new AccessDbContext.RejestracjaRecord
                {
                    R_Data = wizyta.R_Data,
                    RStatus = "zamknięta",
                    R_S_ID = wizyta.R_S_ID,
                    R_GG_MM = wizyta.R_GG_MM,
                    R_Subject = wizyta.R_Subject,
                    R_Uwagi = wizyta.R_Uwagi
                };

                bool sukces = _db.UpdateRejestracja(wizyta.R_ID.Value, record);

                if (sukces)
                {
                    // System.Diagnostics.Debug.WriteLine($"✅ Status wizyty zmieniony: R_ID={wizyta.R_ID} → Odbyta");
                    return true;
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ Nie udało się zmienić statusu wizyty R_ID={wizyta.R_ID}");
                    return false;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ ZmienStatusWizytyNaOdbyta error: {ex.Message}");
                return false;
            }
        }

        public Action? ClearEditForm_xaml { get; set; }

        private void ClearEditForm()
        {

            /*
            DataBadania = DateTime.Now;
            DataWaznosci = DateTime.Now.AddYears(3);
            SelectedWynik = "Pozytywne";
            NrKsiegi = string.Empty;
            ClearPrices();
            */
            ClearEditForm_xaml?.Invoke();
            ClearPrices();
        }

        private void ClearPrices()
        {
            Cena1 = Cena2 = Cena3 = Cena4 = Cena5 = Cena6 = Cena7 = Cena8 = null;

            OnPropertyChanged(nameof(PriceBasicText));
            OnPropertyChanged(nameof(PriceLaryngologistText));
            OnPropertyChanged(nameof(PriceOphthalmologistText));
            OnPropertyChanged(nameof(PriceSanitaryText));
            OnPropertyChanged(nameof(PriceLipidogramText));
            OnPropertyChanged(nameof(PriceEKGText));
            OnPropertyChanged(nameof(PriceHealthClinicText));
            OnPropertyChanged(nameof(PriceOtherText));

            // ✅ DODANE: Wywołaj synchronizację przycisków Toggle (resetuje je na nieaktywne)
            ToggleButtonsSyncRequested?.Invoke();
        }

        private void ApplyFilter()
        {
            try
            {
                if (_allBadania == null || _allBadania.Count == 0)
                {
                    Skierowania = new ObservableCollection<BadanieRecordEdit>();
                    return;
                }

                // ✅ KROK 1: Filtr daty
                DateTime? dateFrom = null;
                DateTime? dateTo = null;

                switch (SelectedDateFilter)
                {
                    case "Bieżący Miesiąc":
                        dateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);
                        break;

                    case "Poprzedni Miesiąc":
                        dateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
                        dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);
                        break;

                    case "Bieżący Rok":
                        dateFrom = new DateTime(DateTime.Now.Year, 1, 1);
                        dateTo = new DateTime(DateTime.Now.Year, 12, 31);
                        break;

                    case "Poprzedni Rok":
                        dateFrom = new DateTime(DateTime.Now.Year - 1, 1, 1);
                        dateTo = new DateTime(DateTime.Now.Year - 1, 12, 31);
                        break;

                    case "Wybrany okres":
                        dateFrom = FilterDateFrom;
                        dateTo = FilterDateTo;
                        break;

                    case "All":
                    default:
                        // Brak filtra daty
                        break;
                }

                // ✅ KROK 2: Zastosuj filtry
                IEnumerable<BadanieRecordEdit> filtered = _allBadania;

                // Filtr daty
                if (dateFrom.HasValue || dateTo.HasValue)
                {
                    filtered = filtered.Where(s =>
                    {
                        if (!s.Bad_Data.HasValue) return false;
                        bool match = true;
                        if (dateFrom.HasValue && s.Bad_Data.Value < dateFrom.Value)
                            match = false;
                        if (dateTo.HasValue && s.Bad_Data.Value > dateTo.Value)
                            match = false;
                        return match;
                    });
                }

                // ✅ KROK 3: SMART FILTER - Wykryj prefix #XXXX dla ID skierowania
                if (!string.IsNullOrWhiteSpace(FilterText))
                {
                    var searchText = FilterText.Trim();

                    // ✅ SMART FILTER: Prefix # dla ID Skierowania
                    if (searchText.StartsWith("#") && searchText.Length > 1)
                    {
                        // Usuń prefix # i wiodące zera
                        var idText = searchText.Substring(1).TrimStart('0');

                        if (int.TryParse(idText, out int searchId))
                        {
                            // System.Diagnostics.Debug.WriteLine($"SMART FILTER: Szukam ID skierowania (B_ID) = {searchId}");

                            // Szukaj DOKŁADNIE po B_ID (ID skierowania)
                            filtered = filtered.Where(s => s.B_ID == searchId);
                        }
                        else
                        {
                            // Niepoprawny format po #
                            filtered = Enumerable.Empty<BadanieRecordEdit>();
                        }
                    }
                    else
                    {
                        // ✅ STANDARD FILTER: Szukaj według wybranego filtra
                        var filterTextLower = searchText.ToLower();

                        switch (SelectedFilter)
                        {
                            case "ID":
                                filtered = filtered.Where(s =>
                                    s.Bad_ID.ToString().Contains(searchText));
                                break;

                            case "Imię":
                                filtered = filtered.Where(s =>
                                    (s.P_imie ?? "").ToLower().Contains(filterTextLower) ||
                                    TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_imie ?? "", filterTextLower));
                                break;

                            case "Nazwisko":
                                filtered = filtered.Where(s =>
                                    (s.P_nazwisko ?? "").ToLower().Contains(filterTextLower) ||
                                    TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_nazwisko ?? "", filterTextLower));
                                break;

                            case "Firma":
                                filtered = filtered.Where(s =>
                                    (s.Firma_Nazwa ?? "").ToLower().Contains(filterTextLower) ||
                                    TextNormalizationHelper.ContainsIgnoringDiacritics(s.Firma_Nazwa ?? "", filterTextLower));
                                break;

                            case "Faktura":
                                filtered = filtered.Where(s =>
                                    (s.Bad_Fakt ?? "").ToLower().Contains(filterTextLower));
                                break;

                            case "Skier. ID":
                                filtered = filtered.Where(s =>
                                    s.B_ID.ToString().Contains(searchText));
                                break;

                            case "All":
                            default:
                                filtered = filtered.Where(s =>
                                    s.Bad_ID.ToString().Contains(searchText) ||
                                    s.B_ID.ToString().Contains(searchText) ||
                                    (s.P_imie ?? "").ToLower().Contains(filterTextLower) ||
                                    TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_imie ?? "", filterTextLower) ||
                                    (s.P_nazwisko ?? "").ToLower().Contains(filterTextLower) ||
                                    TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_nazwisko ?? "", filterTextLower) ||
                                    (s.Firma_Nazwa ?? "").ToLower().Contains(filterTextLower) ||
                                    TextNormalizationHelper.ContainsIgnoringDiacritics(s.Firma_Nazwa ?? "", filterTextLower) ||
                                    (s.Bad_Fakt ?? "").ToLower().Contains(filterTextLower));
                                break;
                        }
                    }
                }

                Skierowania = new ObservableCollection<BadanieRecordEdit>(filtered);

                // System.Diagnostics.Debug.WriteLine($"ApplyFilter: Filtered {Skierowania.Count} / {_allBadania.Count} badania");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ApplyFilter error: {ex}");
                Skierowania = new ObservableCollection<BadanieRecordEdit>(_allBadania);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
