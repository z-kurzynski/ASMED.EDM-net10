using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ASMED.WPF.Helpers;
using System.Linq;
using System.Windows.Threading;

namespace ASMED.WPF.ViewModels
{
    public class WizytyViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ═══════════════════════════════════════════════════════
        // ✅ NOWE: Właściwości dla niezafakturowanych badań
        // ═══════════════════════════════════════════════════════

        private ObservableCollection<AccessDbContext.NiezafakturowaneBadaniaDto> _niezafakturowaneBadania;
        public ObservableCollection<AccessDbContext.NiezafakturowaneBadaniaDto> NiezafakturowaneBadania
        {
            get => _niezafakturowaneBadania;
            set
            {
                _niezafakturowaneBadania = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LiczbaRekordow));
                OnPropertyChanged(nameof(LiczbaBadanRazem)); // ✅ NOWE: Całkowita liczba badań
                OnPropertyChanged(nameof(SumaWartosciWyfiltrowanych));
                OnPropertyChanged(nameof(SumaWartosciWyfiltrowychFormatted));
            }
        }

        // ✅ NOWE: Timer dla debounce (opóźnione filtrowanie)
        private DispatcherTimer _filterDebounceTimer;

        private string ?_filterFirmaNazwa = string.Empty;
        public string ?FilterFirmaNazwa
        {
            get => _filterFirmaNazwa;
            set
            {
                // System.Diagnostics.Debug.WriteLine($"═════════════════════════════════════════════");
                // System.Diagnostics.Debug.WriteLine($"🔍 FilterFirmaNazwa SETTER wywoływany!");
                // System.Diagnostics.Debug.WriteLine($"   Poprzednia wartość: '{_filterFirmaNazwa}'");
                // System.Diagnostics.Debug.WriteLine($"   Nowa wartość: '{value}'");

                if (_filterFirmaNazwa != value)
                {
                    _filterFirmaNazwa = value ?? string.Empty;
                    OnPropertyChanged();

                    // System.Diagnostics.Debug.WriteLine($"   ✅ Wartość zmieniona! Startuję timer debounce...");

                    // ✅ NOWE: Automatyczne filtrowanie z debounce (500ms opóźnienie)
                    _filterDebounceTimer?.Stop();
                    _filterDebounceTimer = new DispatcherTimer
                    {
                        Interval = System.TimeSpan.FromMilliseconds(500)
                    };
                    _filterDebounceTimer.Tick += (s, e) =>
                    {
                        _filterDebounceTimer.Stop();
                        // System.Diagnostics.Debug.WriteLine($"⏰ TIMER TICK! AUTO-FILTER triggered: '{FilterFirmaNazwa}'");
                        LoadNiezafakturowaneBadania();
                    };
                    _filterDebounceTimer.Start();
                    // System.Diagnostics.Debug.WriteLine($"   ✅ Timer wystartowany!");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"   ⚠️ Wartość taka sama - NIC NIE ROBIĘ");
                }
                // System.Diagnostics.Debug.WriteLine($"═════════════════════════════════════════════");
            }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ Właściwości obliczane (aktualizowane automatycznie)
        // ═══════════════════════════════════════════════════════

        /// <summary>Liczba firm (wierszy w tabeli)</summary>
        public int LiczbaRekordow => NiezafakturowaneBadania?.Count ?? 0;

        /// <summary>Całkowita liczba badań (suma LiczbaBadan ze wszystkich firm)</summary>
        public int LiczbaBadanRazem => 
            NiezafakturowaneBadania?.Sum(x => x.LiczbaBadan) ?? 0;

        /// <summary>Suma wartości badań dla wyfiltrowanych rekordów</summary>
        public decimal SumaWartosciWyfiltrowanych => 
            NiezafakturowaneBadania?.Sum(x => x.SumaWartosci ?? 0) ?? 0;

        /// <summary>Suma wartości badań - formatowana (1 234,56 zł)</summary>
        public string ?SumaWartosciWyfiltrowychFormatted => 
            $"{SumaWartosciWyfiltrowanych:N2} zł";

        // ✅ Komendy
        public ICommand ?OdswiezCommand { get; }
        public ICommand ?ApplyFilterCommand { get; }
        public ICommand ?ClearFilterCommand { get; }
        public ICommand ?SetLineChartCommand { get; }    // ✅ NOWE
        public ICommand ?SetColumnChartCommand { get; }  // ✅ NOWE
        public ICommand ?ToggleStatystykiCommand { get; } // ✅ NOWE: Przełącznik widoczności kolumny statystyk

        // ═══════════════════════════════════════════════════════
        // ✅ STATYSTYKI - Właściwości
        // ═══════════════════════════════════════════════════════

        private ObservableCollection<AccessDbContext.StatystykaMiesiecznaDto>? _statystykiMiesieczne;
        public ObservableCollection<AccessDbContext.StatystykaMiesiecznaDto>? StatystykiMiesieczne
        {
            get => _statystykiMiesieczne;
            set
            {
                _statystykiMiesieczne = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatystykiMiesieczneSkalowane)); // ✅ NOWE: Powiadom o zmianie wersji skalowanej
            }
        }

        /// <summary>
        /// Statystyki ze skalowanymi wartościami [zł] do zakresu osi Y (ilości)
        /// </summary>
        public ObservableCollection<StatystykaMiesiecznaSkalowanaDto> StatystykiMiesieczneSkalowane
        {
            get
            {
                if (StatystykiMiesieczne == null || StatystykiMiesieczne.Count == 0)
                    return new ObservableCollection<StatystykaMiesiecznaSkalowanaDto>();

                // 1. Znajdź maksymalną wartość z ilości (Karty, Badania, Wizyty)
                var maxIlosc = Math.Max(
                    StatystykiMiesieczne.Max(x => x.LiczbaSkierowan),
                    Math.Max(
                        StatystykiMiesieczne.Max(x => x.BadaniaOdbyte),
                        StatystykiMiesieczne.Max(x => x.WizytyZarejestrowane)
                    )
                );

                // 2. Znajdź maksymalną wartość [zł]
                var maxWartosc = StatystykiMiesieczne.Max(x => x.WartoscBadan);

                // 3. Oblicz współczynnik skalowania (zabezpieczenie przed dzieleniem przez 0)
                var wspolczynnik = maxWartosc > 0 ? (double)maxIlosc / (double)maxWartosc : 0;

                // System.Diagnostics.Debug.WriteLine($"📊 Skalowanie wykresu: MaxIlość={maxIlosc}, MaxWartość={maxWartosc:N2} zł, Współczynnik={wspolczynnik:F6}");

                // 4. Stwórz kolekcję ze skalowanymi wartościami
                var result = new ObservableCollection<StatystykaMiesiecznaSkalowanaDto>();
                foreach (var item in StatystykiMiesieczne)
                {
                    result.Add(new StatystykaMiesiecznaSkalowanaDto
                    {
                        MiesiacNazwa = item.MiesiacNazwa,
                        LiczbaSkierowan = item.LiczbaSkierowan,
                        BadaniaOdbyte = item.BadaniaOdbyte,
                        WizytyZarejestrowane = item.WizytyZarejestrowane,
                        WartoscBadan = item.WartoscBadan,
                        WartoscBadanSkalowana = (double)item.WartoscBadan * wspolczynnik
                    });
                }

                return result;
            }
        }

        /// <summary>
        /// DTO z dodatkowymi polami dla skalowania wartości [zł]
        /// </summary>
        public class StatystykaMiesiecznaSkalowanaDto
        {
            public string ?MiesiacNazwa { get; set; } = string.Empty;
            public int LiczbaSkierowan { get; set; }
            public int BadaniaOdbyte { get; set; }
            public int WizytyZarejestrowane { get; set; }
            public decimal WartoscBadan { get; set; }           // Oryginalna wartość [zł]
            public double WartoscBadanSkalowana { get; set; }   // Skalowana do osi Y
        }

        private int _wybranyRok;
        public int WybranyRok
        {
            get => _wybranyRok;
            set
            {
                if (_wybranyRok != value)
                {
                    _wybranyRok = value;
                    OnPropertyChanged();
                    LoadStatystyki();
                }
            }
        }

        // ✅ Dostępne lata dla filtru (np. 2020-2030)
        public ObservableCollection<int> DostepneRoki { get; set; }

        /// <summary>Podsumowanie (suma wszystkich miesięcy)</summary>
        public AccessDbContext.StatystykaMiesiecznaDto StatystykiPodsumowanie
        {
            get
            {
                if (StatystykiMiesieczne == null || StatystykiMiesieczne.Count == 0)
                    return new AccessDbContext.StatystykaMiesiecznaDto { MiesiacNazwa = "RAZEM" };

                return new AccessDbContext.StatystykaMiesiecznaDto
                {
                    Rok = WybranyRok,
                    Miesiac = 0,
                    MiesiacNazwa = "RAZEM",
                    LiczbaSkierowan = StatystykiMiesieczne.Sum(x => x.LiczbaSkierowan),
                    BadaniaOkresowe = StatystykiMiesieczne.Sum(x => x.BadaniaOkresowe),
                    BadaniaWstepne = StatystykiMiesieczne.Sum(x => x.BadaniaWstepne),
                    BadaniaKontrolne = StatystykiMiesieczne.Sum(x => x.BadaniaKontrolne),
                    BadaniaInne = StatystykiMiesieczne.Sum(x => x.BadaniaInne),
                    LiczbaKsiazeczek = StatystykiMiesieczne.Sum(x => x.LiczbaKsiazeczek),
                    BadaniaOdbyte = StatystykiMiesieczne.Sum(x => x.BadaniaOdbyte),
                    WizytyZarejestrowane = StatystykiMiesieczne.Sum(x => x.WizytyZarejestrowane),
                    WartoscBadan = StatystykiMiesieczne.Sum(x => x.WartoscBadan)
                };
            }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ WYKRES - Właściwości dla checkboxów (włącz/wyłącz serie)
        // ═══════════════════════════════════════════════════════

        private bool _isLineChart = true; // ✅ Domyślnie wykres liniowy
        public bool IsLineChart
        {
            get => _isLineChart;
            set 
            { 
                if (_isLineChart != value) 
                { 
                    _isLineChart = value; 
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsColumnChart));

                    // ✅ KLUCZOWE: Wymuś odświeżenie wykresu przez zmianę ItemsSource
                    RefreshChart();
                } 
            }
        }

        /// <summary>Czy wykres ma być słupkowy (odwrotność IsLineChart)</summary>
        public bool IsColumnChart => !IsLineChart;

        /// <summary>
        /// Wymusza odświeżenie wykresów Syncfusion po zmianie typu
        /// </summary>
        private void RefreshChart()
        {
            // System.Diagnostics.Debug.WriteLine($"🔄 RefreshChart: IsLineChart={IsLineChart}, IsColumnChart={IsColumnChart}");

            // ✅ Trick: Null → Restore wymusza refresh Syncfusion
            var temp = StatystykiMiesieczne;
            StatystykiMiesieczne = null;
            OnPropertyChanged(nameof(StatystykiMiesieczne));

            // Krótkie opóźnienie (może nie być potrzebne, ale bezpieczniejsze)
            System.Threading.Tasks.Task.Delay(10).ContinueWith(_ =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatystykiMiesieczne = temp;
                    OnPropertyChanged(nameof(StatystykiMiesieczne));
                    // System.Diagnostics.Debug.WriteLine($"✅ Wykres odświeżony");
                });
            });
        }

        private bool _wykresKartyVisible = true; // ✅ Domyślnie włączone
        public bool WykresKartyVisible
        {
            get => _wykresKartyVisible;
            set 
            { 
                if (_wykresKartyVisible != value) 
                { 
                    _wykresKartyVisible = value; 
                    OnPropertyChanged();
                    RefreshChart(); // ✅ Wymuś odświeżenie wykresu
                } 
            }
        }

        private bool _wykresBadanVisible = true; // ✅ Domyślnie włączone
        public bool WykresBadanVisible
        {
            get => _wykresBadanVisible;
            set 
            { 
                if (_wykresBadanVisible != value) 
                { 
                    _wykresBadanVisible = value; 
                    OnPropertyChanged();
                    RefreshChart(); // ✅ Wymuś odświeżenie wykresu
                } 
            }
        }

        private bool _wykresWizytVisible = true; // ✅ Domyślnie włączone
        public bool WykresWizytVisible
        {
            get => _wykresWizytVisible;
            set 
            { 
                if (_wykresWizytVisible != value) 
                { 
                    _wykresWizytVisible = value; 
                    OnPropertyChanged();
                    RefreshChart(); // ✅ Wymuś odświeżenie wykresu
                } 
            }
        }

        private bool _wykresWartosciVisible = true; // ✅ ZMIENIONO: Domyślnie WŁĄCZONE (tak jak pozostałe)
        public bool WykresWartosciVisible
        {
            get => _wykresWartosciVisible;
            set 
            { 
                if (_wykresWartosciVisible != value) 
                { 
                    _wykresWartosciVisible = value; 
                    OnPropertyChanged();
                    RefreshChart(); // ✅ Wymuś odświeżenie wykresu
                } 
            }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ NOWE: Widoczność kolumny statystyk (toggle)
        // ═══════════════════════════════════════════════════════

        private bool _isStatystykiVisible = true;
        public bool IsStatystykiVisible
        {
            get => _isStatystykiVisible;
            set
            {
                if (_isStatystykiVisible != value)
                {
                    _isStatystykiVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public WizytyViewModel()
        {
            // ✅ Inicjalizacja komend
            OdswiezCommand = new RelayCommand(_ => LoadNiezafakturowaneBadania());
            ApplyFilterCommand = new RelayCommand(_ => ApplyFilter());
            ClearFilterCommand = new RelayCommand(_ => ClearFilter());
            SetLineChartCommand = new RelayCommand(_ => IsLineChart = true);      // ✅ NOWE
            SetColumnChartCommand = new RelayCommand(_ => IsLineChart = false);   // ✅ NOWE
            ToggleStatystykiCommand = new RelayCommand(_ => IsStatystykiVisible = !IsStatystykiVisible); // ✅ NOWE

            // ✅ Załaduj dane przy starcie
            NiezafakturowaneBadania = new ObservableCollection<AccessDbContext.NiezafakturowaneBadaniaDto>();
            LoadNiezafakturowaneBadania();

            // ✅ Inicjalizacja statystyk
            StatystykiMiesieczne = new ObservableCollection<AccessDbContext.StatystykaMiesiecznaDto>();

            // Dostępne lata (od 2020 do bieżący rok + 1)
            DostepneRoki = new ObservableCollection<int>();
            int currentYear = System.DateTime.Now.Year;
            for (int y = 2020; y <= currentYear + 1; y++)
            {
                DostepneRoki.Add(y);
            }

            // Ustaw domyślny rok (bieżący)
            _wybranyRok = currentYear;
            LoadStatystyki();
        }

        /// <summary>
        /// Ładuje niezafakturowane badania z bazy danych
        /// ✅ SQL już sortuje po wartości (ORDER BY Sum(Bad_Razem) DESC)
        /// </summary>
        private void LoadNiezafakturowaneBadania()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"📊 LoadNiezafakturowaneBadania: Filter='{FilterFirmaNazwa}'");

                var db = new AccessDbContext();
                var lista = db.GetNiezafakturowaneBadaniaPoFirmie(
                    string.IsNullOrWhiteSpace(FilterFirmaNazwa) ? null : FilterFirmaNazwa
                );

                // ✅ SQL już sortuje - nie trzeba sortować tutaj
                NiezafakturowaneBadania.Clear();
                foreach (var item in lista)
                {
                    NiezafakturowaneBadania.Add(item);
                }

                // ✅ KLUCZOWE: Ręczne wywołanie OnPropertyChanged dla liczników
                // (Clear() i Add() NIE wywołują settera ObservableCollection!)
                OnPropertyChanged(nameof(LiczbaRekordow));
                OnPropertyChanged(nameof(LiczbaBadanRazem));
                OnPropertyChanged(nameof(SumaWartosciWyfiltrowanych));
                OnPropertyChanged(nameof(SumaWartosciWyfiltrowychFormatted));

                // System.Diagnostics.Debug.WriteLine($"✅ Załadowano {NiezafakturowaneBadania.Count} firm z niezafakturowanymi badaniami");
                // System.Diagnostics.Debug.WriteLine($"   📊 Liczba badań razem: {LiczbaBadanRazem}");
                // System.Diagnostics.Debug.WriteLine($"   💰 Suma wartości: {SumaWartosciWyfiltrowychFormatted}");
            }
            catch (System.Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LoadNiezafakturowaneBadania ERROR: {ex.Message}");
                NotificationHelper.ShowError($"Błąd ładowania danych: {ex.Message}");
            }
        }

        /// <summary>
        /// Stosuje filtr (Enter w TextBox) - natychmiastowe filtrowanie
        /// </summary>
        private void ApplyFilter()
        {
            // System.Diagnostics.Debug.WriteLine($"🔍 ApplyFilter (Enter): Filter='{FilterFirmaNazwa}'");

            // ✅ Zatrzymaj debounce timer (Enter = natychmiastowe wykonanie)
            _filterDebounceTimer?.Stop();

            // ✅ Natychmiast załaduj dane
            LoadNiezafakturowaneBadania();
        }

        /// <summary>
        /// Czyści filtr i odświeża dane
        /// </summary>
        private void ClearFilter()
        {
            FilterFirmaNazwa = string.Empty;
            LoadNiezafakturowaneBadania();
        }

        // ═══════════════════════════════════════════════════════
        // ✅ STATYSTYKI - Metody
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Ładuje statystyki miesięczne dla wybranego roku
        /// </summary>
        private void LoadStatystyki()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"═════════════════════════════════════════════");
                // System.Diagnostics.Debug.WriteLine($"📊 LoadStatystyki: Rok={WybranyRok}");

                var db = new AccessDbContext();
                var lista = db.GetStatystykiMiesieczne(WybranyRok);

                // System.Diagnostics.Debug.WriteLine($"📊 Otrzymano {lista.Count} miesięcy z bazy");

                StatystykiMiesieczne?.Clear();
                foreach (var item in lista)
                {
                    StatystykiMiesieczne.Add(item);

                    // ✅ DEBUG: Wypisz wartości dla każdego miesiąca
                    if (item.LiczbaSkierowan > 0 || item.BadaniaOdbyte > 0 || item.WizytyZarejestrowane > 0)
                    {
                        // System.Diagnostics.Debug.WriteLine($"   {item.MiesiacNazwa}: Karty={item.LiczbaSkierowan}, Badania={item.BadaniaOdbyte}, Wizyty={item.WizytyZarejestrowane}, Wartość={item.WartoscBadan:N2} zł");
                    }
                }

                // ✅ Aktualizuj podsumowanie
                OnPropertyChanged(nameof(StatystykiPodsumowanie));

                // System.Diagnostics.Debug.WriteLine($"───────────────────────────────────────────");
                // System.Diagnostics.Debug.WriteLine($"✅ PODSUMOWANIE dla {WybranyRok}:");
                // System.Diagnostics.Debug.WriteLine($"   📋 Skierowań: {StatystykiPodsumowanie.LiczbaSkierowan}");
                // System.Diagnostics.Debug.WriteLine($"   🏥 Badań: {StatystykiPodsumowanie.BadaniaOdbyte}");
                // System.Diagnostics.Debug.WriteLine($"   👥 Wizyt: {StatystykiPodsumowanie.WizytyZarejestrowane}");
                // System.Diagnostics.Debug.WriteLine($"   💰 Wartość: {StatystykiPodsumowanie.WartoscBadan:N2} zł");

                if (StatystykiPodsumowanie.LiczbaSkierowan == 0 && 
                    StatystykiPodsumowanie.BadaniaOdbyte == 0 && 
                    StatystykiPodsumowanie.WizytyZarejestrowane == 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"⚠️ UWAGA: BRAK DANYCH dla roku {WybranyRok}!");
                    // System.Diagnostics.Debug.WriteLine($"   Sprawdź czy w bazie są dane z tym rokiem w:");
                    // System.Diagnostics.Debug.WriteLine($"   - B_Skierowania.B_RegistrationDate");
                    // System.Diagnostics.Debug.WriteLine($"   - Badanie.Bad_Data");
                    // System.Diagnostics.Debug.WriteLine($"   - Rejestracja.R_Data");
                }
                else
                {
                    // ✅ MAMY DANE - sprawdź checkboxy
                    // System.Diagnostics.Debug.WriteLine($"📊 STATUS WYKRESÓW (Visibility):");
                    // System.Diagnostics.Debug.WriteLine($"   🔵 Karty: {(WykresKartyVisible ? "WIDOCZNE ✅" : "UKRYTE ❌")}");
                    // System.Diagnostics.Debug.WriteLine($"   💙 Badania: {(WykresBadanVisible ? "WIDOCZNE ✅" : "UKRYTE ❌")}");
                    // System.Diagnostics.Debug.WriteLine($"   🟣 Wizyty: {(WykresWizytVisible ? "WIDOCZNE ✅" : "UKRYTE ❌")}");
                    // System.Diagnostics.Debug.WriteLine($"   🟢 Wartość: {(WykresWartosciVisible ? "WIDOCZNE ✅" : "UKRYTE ❌")}");
                }
                // System.Diagnostics.Debug.WriteLine($"═════════════════════════════════════════════");
            }
            catch (System.Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LoadStatystyki ERROR: {ex.Message}");
                NotificationHelper.ShowError($"Błąd ładowania statystyk: {ex.Message}");
            }
        }
    }
}
