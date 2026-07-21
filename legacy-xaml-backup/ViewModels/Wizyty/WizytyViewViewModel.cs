using ASMED.WPF.Helpers;
using ASMED.WPF.Models;
using ASMED.WPF.ViewModels.Skierowania;
using ASMED.WPF.Views;
using ASMED.WPF.Views.lista_do_faktur;
using Syncfusion.UI.Xaml.Scheduler;
using Syncfusion.Windows.Tools.Controls; // ✅ DODANE: Dla TabControlExt i TabItemExt
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels
{

    public class WizytyViewViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region ✅ NOWE: Właściwości dla UI

        private DateTime? _selectedDate;
        private System.Threading.CancellationTokenSource _loadCancellation;

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedDateFormatted));

                    // ✅ Anuluj poprzednie ładowanie
                    _loadCancellation?.Cancel();
                    _loadCancellation = new System.Threading.CancellationTokenSource();

                    // ✅ Uruchom asynchronicznie z małym opóźnieniem (debouncing)
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await System.Threading.Tasks.Task.Delay(100, _loadCancellation.Token);

                            if (!_loadCancellation.Token.IsCancellationRequested)
                            {
                                // Wykonaj na wątku UI
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    LoadPacjenciNaDzien();
                                    ObliczStatystyki();
                                });
                            }
                        }
                        catch (System.Threading.Tasks.TaskCanceledException)
                        {
                            // Normalne anulowanie - ignoruj
                        }
                        catch (Exception)
                        {
                            // System.Diagnostics.Debug.WriteLine($"❌ Błąd async load: {ex.Message}");
                        }
                    });
                }
            }
        }


        public string SelectedDateFormatted => SelectedDate?.ToString("dddd, dd MMMM yyyy") ?? "Wybierz datę w kalendarzu";

        // ✅ NOWE: Domyślna data kalendarza wizyt
        private bool _useCustomDefaultDateCalendar;
        private DateTime _customDefaultDateCalendar;

        public bool UseCustomDefaultDateCalendar
        {
            get => _useCustomDefaultDateCalendar;
            set
            {
                _useCustomDefaultDateCalendar = value;
                OnPropertyChanged();

                // ✅ KLUCZOWE: Aktualizuj SelectedDate gdy checkbox się zmienia
                if (value)
                {
                    // Checkbox zaznaczony → użyj CustomDefaultDateCalendar
                    SelectedDate = CustomDefaultDateCalendar;
                    // System.Diagnostics.Debug.WriteLine($"UseCustomDefaultDateCalendar: ✅ Włączono - SelectedDate = {CustomDefaultDateCalendar:dd-MM-yyyy}");
                }
                else
                {
                    // Checkbox odznaczony → użyj DateTime.Now
                    SelectedDate = DateTime.Now;
                    // System.Diagnostics.Debug.WriteLine($"UseCustomDefaultDateCalendar: ❌ Wyłączono - SelectedDate = {DateTime.Now:dd-MM-yyyy}");
                }
            }
        }

        public DateTime CustomDefaultDateCalendar
        {
            get => _customDefaultDateCalendar;
            set
            {
                _customDefaultDateCalendar = value;
                OnPropertyChanged();

                // ✅ KLUCZOWE: Aktualizuj SelectedDate gdy zmienia się data (i checkbox jest zaznaczony)
                if (UseCustomDefaultDateCalendar)
                {
                    SelectedDate = value;
                    // System.Diagnostics.Debug.WriteLine($"CustomDefaultDateCalendar: 📅 Zmieniono datę - SelectedDate = {value:dd-MM-yyyy}");
                }
            }
        }

        private ObservableCollection<RejestracjaItem> _pacjenciNaDzien;
        public ObservableCollection<RejestracjaItem> PacjenciNaDzien
        {
            get => _pacjenciNaDzien;
            set
            {
                _pacjenciNaDzien = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LiczbaPacjentow));
                ObliczStatystyki();
            }
        }

        public int LiczbaPacjentow => PacjenciNaDzien?.Count ?? 0;

        // ✅ NOWE: Wybrany pacjent z listy

        private RejestracjaItem? _wybranyPacjent;

        // ✅ NOWE: Pomocnicze pole do przechowywania pacjenta podczas zmiany terminu
        private RejestracjaItem? _pacjentDoZmianyTerminu;


        public RejestracjaItem? WybranyPacjent
        {
            get => _wybranyPacjent;
            set
            {
                if (_wybranyPacjent != value)
                {
                    // ✅ Odsubskrybuj poprzedni obiekt
                    if (_wybranyPacjent != null)
                    {
                        _wybranyPacjent.PropertyChanged -= WybranyPacjent_PropertyChanged;
                    }

                    _wybranyPacjent = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CzyWybranoPacjenta));

                    // ✅ NOWE: Odśwież RadioButton dla B_TypBadania
                    OnPropertyChanged(nameof(IsTypBadaniaWstepne));
                    OnPropertyChanged(nameof(IsTypBadaniaPeriodyczne));
                    OnPropertyChanged(nameof(IsTypBadaniaKontrolne));
                    OnPropertyChanged(nameof(IsTypBadaniaKoncowe));

                    // ✅ NOWE: Subskrybuj nowy obiekt (nasłuchuj zmiany PESEL)
                    if (_wybranyPacjent != null)
                    {
                        _wybranyPacjent.PropertyChanged += WybranyPacjent_PropertyChanged;
                    }

                    // System.Diagnostics.Debug.WriteLine($"🔄 WybranyPacjent zmieniony: B_TypBadania='{_wybranyPacjent?.B_TypBadania}'");
                }

            }
        }
        // ✅ NOWE: Filtr wyszukiwania po nazwisku
        private string?_filterTextNazwisko = string.Empty;
        public string?FilterTextNazwisko
        {
            get => _filterTextNazwisko;
            set
            {
                if (_filterTextNazwisko != value)
                {
                    _filterTextNazwisko = value ?? string.Empty;
                    OnPropertyChanged();
                    ApplyFilterNazwisko(); // Odśwież listę po zmianie filtra
                }
            }
        }



        // ✅ NOWE: Lista PEŁNA (przed filtrowaniem)
        private ObservableCollection<RejestracjaItem> _allPacjenciNaDzien;


        public bool CzyWybranoPacjenta => WybranyPacjent != null;

        // ✅ NOWE: Właściwość dla widoku zmiany terminu (czy jest wybrany pacjent do zmiany)
        public bool CzyWybranoPacjentaDoZmiany => _pacjentDoZmianyTerminu != null;

        /// <summary>
        /// ✅ NOWE: Obsługa zmiany właściwości WybranyPacjent (walidacja PESEL)
        /// </summary>
        private void WybranyPacjent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RejestracjaItem.P_Pesel))
            {
                if (WybranyPacjent != null && !string.IsNullOrWhiteSpace(WybranyPacjent.P_Pesel))
                {
                    // ✅ Waliduj PESEL przy każdej zmianie
                    bool peselPrawidlowy = WalidujPesel(WybranyPacjent.P_Pesel);

                    if (!peselPrawidlowy)
                    {
                        // System.Diagnostics.Debug.WriteLine($"⚠️ Wprowadzono nieprawidłowy PESEL: {WybranyPacjent.P_Pesel}");
                    }
                }
            }
        }
        public bool ShowRightColumnStats => ShowListView || ShowRescheduleView;
        // ═══════════════════════════════════════════════════════
        // ✅ NOWE: Widoczność kolumn (Lista vs Szczegóły)
        // ═══════════════════════════════════════════════════════
        private bool _showListView = true;
        public bool ShowListView
        {
            get => _showListView;
            set
            {
                if (_showListView != value)
                {
                    _showListView = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowRightColumnStats)); // ✅ DODANE
                }
            }
        }

        private bool _showDetailsView = false;
        public bool ShowDetailsView
        {
            get => _showDetailsView;
            set
            {
                if (_showDetailsView != value)
                {
                    _showDetailsView = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowRightColumnStats)); // ✅ DODANE
                }
            }
        }

        // ✅ NOWE: Statystyki
        private int _liczbaWizytDzisiaj;
        public int LiczbaWizytDzisiaj
        {
            get => _liczbaWizytDzisiaj;
            set
            {
                _liczbaWizytDzisiaj = value;
                OnPropertyChanged();
            }
        }

        // ✅ NOWE: Liczba wizyt w realizacji lub zrealizowanych (status != rejestracja/dokumentacja/przełożona)
        private int _liczbaWizytWRealizacji;
        public int LiczbaWizytWRealizacji
        {
            get => _liczbaWizytWRealizacji;
            set
            {
                _liczbaWizytWRealizacji = value;
                OnPropertyChanged();
            }
        }

        private int _wolneSloty;
        public int WolneSloty
        {
            get => _wolneSloty;
            set
            {
                _wolneSloty = value;
                OnPropertyChanged();
            }
        }

        private TimeSpan _pierwszaWizyta;
        public TimeSpan PierwszaWizyta
        {
            get => _pierwszaWizyta;
            set
            {
                _pierwszaWizyta = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PierwszaWizytaFormatted));
            }
        }

        public string?PierwszaWizytaFormatted => PierwszaWizyta != TimeSpan.Zero
            ? PierwszaWizyta.ToString(@"hh\:mm")
            : "-";

        private TimeSpan _ostatniaWizyta;
        public TimeSpan OstatniaWizyta
        {
            get => _ostatniaWizyta;
            set
            {
                _ostatniaWizyta = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OstatniaWizytaFormatted));
            }
        }

        public string?OstatniaWizytaFormatted => OstatniaWizyta != TimeSpan.Zero
            ? OstatniaWizyta.ToString(@"hh\:mm")
            : "-";

        // ═══════════════════════════════════════════════════════
        // ✅ NOWE: Właściwości dla wydruku z datą badania
        // ═══════════════════════════════════════════════════════

        private bool _dodajDateBadaniaDoWydruku;
        public bool DodajDateBadaniaDoWydruku
        {
            get => _dodajDateBadaniaDoWydruku;
            set
            {
                if (_dodajDateBadaniaDoWydruku != value)
                {
                    _dodajDateBadaniaDoWydruku = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime? _dataBadaniaDoWydruku = DateTime.Now;
        public DateTime? DataBadaniaDoWydruku
        {
            get => _dataBadaniaDoWydruku;
            set
            {
                if (_dataBadaniaDoWydruku != value)
                {
                    _dataBadaniaDoWydruku = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region ✅ NOWE: Walidacja PESEL

        /// <summary>
        /// Waliduje numer PESEL i automatycznie uzupełnia płeć oraz datę urodzenia
        /// </summary>
        /// <param name="pesel">Numer PESEL do walidacji</param>
        /// <returns>True jeśli PESEL jest prawidłowy</returns>
        private bool WalidujPesel(string pesel)
        {
            if (string.IsNullOrWhiteSpace(pesel) || pesel.Length != 11)
                return false;

            // Sprawdź czy wszystkie znaki to cyfry
            if (!pesel.All(char.IsDigit))
                return false;

            try
            {
                // ✅ KROK 1: Walidacja sumy kontrolnej
                int[] wagi = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
                int suma = 0;

                for (int i = 0; i < 10; i++)
                {
                    suma += wagi[i] * int.Parse(pesel[i].ToString());
                }

                int cyfraKontrolna = (10 - (suma % 10)) % 10;
                int ostatniaCyfra = int.Parse(pesel[10].ToString());

                if (cyfraKontrolna != ostatniaCyfra)
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ PESEL nieprawidłowy (błąd sumy kontrolnej): {pesel}");
                    return false;
                }

                // ✅ KROK 2: Wyodrębnij datę urodzenia
                int rok = int.Parse(pesel.Substring(0, 2));
                int miesiac = int.Parse(pesel.Substring(2, 2));
                int dzien = int.Parse(pesel.Substring(4, 2));

                // Określ wiek na podstawie miesiąca
                if (miesiac >= 1 && miesiac <= 12)
                {
                    rok += 1900; // XIX wiek
                }
                else if (miesiac >= 21 && miesiac <= 32)
                {
                    rok += 2000; // XX wiek
                    miesiac -= 20;
                }
                else if (miesiac >= 81 && miesiac <= 92)
                {
                    rok += 1800; // XVIII wiek
                    miesiac -= 80;
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ PESEL nieprawidłowy (błędny miesiąc): {pesel}");
                    return false;
                }

                // Sprawdź poprawność daty
                if (!DateTime.TryParse($"{rok}-{miesiac:D2}-{dzien:D2}", out DateTime dataUrodzenia))
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ PESEL nieprawidłowy (nieprawidłowa data): {pesel}");
                    return false;
                }

                // ✅ KROK 3: Określ płeć (cyfra 10: parzysta=K, nieparzysta=M)
                int cyfraPłci = int.Parse(pesel[9].ToString());
                string plec = (cyfraPłci % 2 == 0) ? "K" : "M";

                // ✅ KROK 4: Automatycznie uzupełnij dane
                if (WybranyPacjent != null)
                {
                    WybranyPacjent.P_DataUrodzenia = dataUrodzenia;
                    WybranyPacjent.P_Plec = plec;

                    OnPropertyChanged(nameof(WybranyPacjent));
                    // System.Diagnostics.Debug.WriteLine($"✅ PESEL prawidłowy: {pesel}");
                    // System.Diagnostics.Debug.WriteLine($"   Data urodzenia: {dataUrodzenia:yyyy-MM-dd}");
                    // System.Diagnostics.Debug.WriteLine($"   Płeć: {plec}");
                }

                return true;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd walidacji PESEL: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ✅ NOWE: Pomocnicze właściwości dla RadioButton B_TypBadania
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Czy wybrano "W" (Wstępne) dla Typ Badania
        /// </summary>
        public bool IsTypBadaniaWstepne
        {
            get => WybranyPacjent?.B_TypBadania == "W";
            set
            {
                if (value && WybranyPacjent != null)
                {
                    WybranyPacjent.B_TypBadania = "W";
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsTypBadaniaPeriodyczne));
                    OnPropertyChanged(nameof(IsTypBadaniaKontrolne));
                    OnPropertyChanged(nameof(IsTypBadaniaKoncowe));
                    // System.Diagnostics.Debug.WriteLine($"✅ Typ Badania zmieniony na: W");
                }
            }
        }

        /// <summary>
        /// Czy wybrano "O" (Okresowe/Badania Kontrolne) dla Typ Badania
        /// </summary>
        public bool IsTypBadaniaPeriodyczne
        {
            get => WybranyPacjent?.B_TypBadania == "O";
            set
            {
                if (value && WybranyPacjent != null)
                {
                    WybranyPacjent.B_TypBadania = "O";
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsTypBadaniaWstepne));
                    OnPropertyChanged(nameof(IsTypBadaniaKontrolne));
                    OnPropertyChanged(nameof(IsTypBadaniaKoncowe));
                    // System.Diagnostics.Debug.WriteLine($"✅ Typ Badania zmieniony na: O");
                }
            }
        }

        /// <summary>
        /// Czy wybrano "K" (Kontrolne) dla Typ Badania
        /// </summary>
        public bool IsTypBadaniaKontrolne
        {
            get => WybranyPacjent?.B_TypBadania == "K";
            set
            {
                if (value && WybranyPacjent != null)
                {
                    WybranyPacjent.B_TypBadania = "K";
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsTypBadaniaWstepne));
                    OnPropertyChanged(nameof(IsTypBadaniaPeriodyczne));
                    OnPropertyChanged(nameof(IsTypBadaniaKoncowe));
                    // System.Diagnostics.Debug.WriteLine($"✅ Typ Badania zmieniony na: K");
                }
            }
        }

        /// <summary>
        /// Czy wybrano "Końcowe" dla Typ Badania (opcjonalny 4. RadioButton)
        /// </summary>
        public bool IsTypBadaniaKoncowe
        {
            get => WybranyPacjent?.B_TypBadania == "Końcowe";
            set
            {
                if (value && WybranyPacjent != null)
                {
                    WybranyPacjent.B_TypBadania = "Końcowe";
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsTypBadaniaWstepne));
                    OnPropertyChanged(nameof(IsTypBadaniaPeriodyczne));
                    OnPropertyChanged(nameof(IsTypBadaniaKontrolne));
                    // System.Diagnostics.Debug.WriteLine($"✅ Typ Badania zmieniony na: Końcowe");
                }
            }
        }

        #endregion

        #region Existing Properties

        // ObservableCollection to hold appointments
        public ObservableCollection<ScheduleAppointment> Appointments { get; set; }

        #endregion

        #region Commands

        public ICommand ?AddAppointmentCommand { get; set; }
        public ICommand ?WydrukListyCommand { get; set; }
        public ICommand ?OdswiezCommand { get; set; }

        // ✅ NOWE: Komendy zmiany statusu
        public ICommand ?ZmienStatusWTrakcieCommand { get; set; }
        public ICommand ?ZmienStatusOdbytaCommand { get; set; }
        public ICommand ?ZmienStatusDokumentacjaCommand { get; set; }
        public ICommand ?ZmienStatusNieobecnoscCommand { get; set; }
        public ICommand ?ZmienStatusAnulowanaCommand { get; set; }

        // ✅ NOWE: Komenda edycji (przełączanie widoków)
        public ICommand ?EdytujCommand { get; set; }
        public ICommand ?PowrotDoListyCommand { get; set; }
        public ICommand ?SavePersonalData { get; set; }

        // ✅ NOWE: Komendy nawigacji
        public ICommand ?OtworzNowaKartaCommand { get; set; }
        public ICommand ?ZakonczBadanieCommand { get; set; }
        public ICommand ?OtworzListaDoFaktorCommand { get; set; }
        public ICommand ?BadanieEndCommand { get; set; }

        // ✅ NOWE: Komendy wydruków formularzy
        public ICommand ?PrintAllCommand { get; set; }
        public ICommand ?PrintKartaCommand { get; set; }
        public ICommand ?PrintOrzeczenieCommand { get; set; }
        public ICommand ?PrintSanitarneCommand { get; set; }
        public ICommand ?ClearFilterCommand { get; set; }

        // ═══════════════════════════════════════════════════════
        // ✅ NOWE: Właściwości dla widoku zmiany terminu
        // ═══════════════════════════════════════════════════════

        private bool _showRescheduleView;
        public bool ShowRescheduleView
        {
            get => _showRescheduleView;
            set
            {
                if (_showRescheduleView != value)
                {
                    _showRescheduleView = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowRightColumnStats)); // ✅ DODANE
                }
            }
        }

        private DateTime? _newAppointmentDate;
        private bool _isUpdatingNewAppointmentDate = false; // ✅ Flaga zapobiegająca cyklicznym aktualizacjom

        public DateTime? NewAppointmentDate
        {
            get => _newAppointmentDate;
            set
            {
                if (_newAppointmentDate != value && !_isUpdatingNewAppointmentDate)
                {
                    _isUpdatingNewAppointmentDate = true;
                    _newAppointmentDate = value;
                    OnPropertyChanged();
                    // System.Diagnostics.Debug.WriteLine($"📅 NewAppointmentDate zmieniona na: {value:yyyy-MM-dd HH:mm}");
                    _isUpdatingNewAppointmentDate = false;
                }
            }
        }

        // ✅ NOWE: Komendy dla zmiany terminu
        public ICommand ?OpenRescheduleViewCommand { get; set; }
        public ICommand ?ConfirmRescheduleCommand { get; set; }
        public ICommand ?CancelAppointmentCommand { get; set; }
        public ICommand ?CancelRescheduleCommand { get; set; }
        #endregion

        #region Constructor

        public WizytyViewViewModel()
        {
            Appointments = new ObservableCollection<ScheduleAppointment>();
            PacjenciNaDzien = new ObservableCollection<RejestracjaItem>();

            // ✅ NOWE: Inicjalizacja domyślnej daty kalendarza
            UseCustomDefaultDateCalendar = false; // Domyślnie wyłączone
            CustomDefaultDateCalendar = DateTime.Now; // Domyślnie dzisiejsza data

            AddAppointmentCommand = new RelayCommand(AddAppointment);
            WydrukListyCommand = new RelayCommand(WydrukListy);
            OdswiezCommand = new RelayCommand(_ => RefreshlistaFromDb());

            // ✅ NOWE: Inicjalizacja komend zmiany statusu

            // ✅ NOWE: Inicjalizacja komend zmiany statusu
            ZmienStatusWTrakcieCommand = new RelayCommand(ZmienStatusWTrakcie);
            ZmienStatusOdbytaCommand = new RelayCommand(ZmienStatusOdbyta);
            ZmienStatusDokumentacjaCommand = new RelayCommand(ZmienStatusDokumentacja);
            ZmienStatusNieobecnoscCommand = new RelayCommand(ZmienStatusNieobecnosc); // ✅ DODANE

            // ✅ NOWE: Inicjalizacja komend edycji
            EdytujCommand = new RelayCommand(EdytujPacjenta);
            PowrotDoListyCommand = new RelayCommand(PowrotDoListy);
            SavePersonalData = new RelayCommand(SavePersonalDataExecute);

            // ✅ NOWE: Inicjalizacja komend nawigacji
            OtworzNowaKartaCommand = new RelayCommand(OtworzNowaKarta);
            ZakonczBadanieCommand = new RelayCommand(ZakonczBadanie);
            BadanieEndCommand = new RelayCommand(BadanieEnd);
            OtworzListaDoFaktorCommand = new RelayCommand(OtworzListaDoFaktor);

            // ✅ NOWE: Inicjalizacja komend wydruków
            PrintAllCommand = new RelayCommand(PrintAll);
            PrintKartaCommand = new RelayCommand(PrintKarta);
            PrintOrzeczenieCommand = new RelayCommand(PrintOrzeczenie);
            PrintSanitarneCommand = new RelayCommand(PrintSanitarne);
            ClearFilterCommand = new RelayCommand(_ => FilterTextNazwisko = string.Empty);

            // ═══════════════════════════════════════════════════════
            // ✅ NOWE: Inicjalizacja komend zmiany terminu
            OpenRescheduleViewCommand = new RelayCommand(OpenRescheduleView);
            ConfirmRescheduleCommand = new RelayCommand(ConfirmReschedule);
            CancelAppointmentCommand = new RelayCommand(CancelAppointment);
            CancelRescheduleCommand = new RelayCommand(CancelReschedule);

            LoadSampleAppointments();
            RefreshFromDb();

            // ✅ NOWE: Ustaw domyślną datę kalendarza na podstawie checkboxa
            if (UseCustomDefaultDateCalendar)
            {
                SelectedDate = CustomDefaultDateCalendar;
            }
            else
            {
                SelectedDate = DateTime.Now;
            }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ NOWE METODY: Logika zmiany terminu wizyty
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Otwiera widok zmiany terminu wizyty (kalendarz)





        private void OpenRescheduleView(object? parameter)
        {
            if (WybranyPacjent == null)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta przed zmianą terminu.");
                return;
            }

            // ✅ WALIDACJA: Sprawdź czy wizyta nie jest zamknięta
            if (CzyWizytaZamknieta())
                return;

            // ✅ WALIDACJA: Sprawdź status wizyty
            var status = WybranyPacjent.R_Status?.ToLower();

            if (status != "rejestracja" && status != "dokumentacja" && status != "przełożona" && status != "zamknięta")
            {
                ShowTopMostMessageBox(
                    $"Nie można zmienić daty wizyty.\n\n" +
                    $"Aktualny status: {WybranyPacjent.R_Status}\n\n" +
                    "Zmiana terminu jest możliwa tylko dla wizyt ze statusem:\n" +
                    "• Rejestracja\n" +
                    "• Dokumentacja\n" +
                    "• Przełożona",
                    "Zmiana terminu niemożliwa");
                return;
            }

            // ✅ KLUCZOWE: Zachowaj wybranego pacjenta
            _pacjentDoZmianyTerminu = WybranyPacjent;
            OnPropertyChanged(nameof(CzyWybranoPacjentaDoZmiany));

            // ✅ Ustaw domyślną datę na obecną datę wizyty (zamiast wymuszać jutro)
            if (WybranyPacjent.R_GG_MM.HasValue)
            {
                NewAppointmentDate = WybranyPacjent.R_GG_MM.Value;
            }
            else if (WybranyPacjent.R_Data.HasValue)
            {
                NewAppointmentDate = WybranyPacjent.R_Data.Value;
            }
            else
            {
                NewAppointmentDate = DateTime.Today; // Fallback: dzisiaj
            }

            // System.Diagnostics.Debug.WriteLine($"📅 Domyślna data dla zmiany terminu: {NewAppointmentDate:yyyy-MM-dd HH:mm}");

            // ✅ Przełącz widok
            ShowListView = false;
            ShowDetailsView = false;
            ShowRescheduleView = true;
        }

        private void ConfirmReschedule(object? parameter)
        {
            if (_pacjentDoZmianyTerminu == null || !NewAppointmentDate.HasValue)
            {
                NotificationHelper.ShowWarning("Wybierz nową datę wizyty.");
                return;
            }

            try
            {
                // ✅ WALIDACJA: Nowa data nie może być w przeszłości
                if (NewAppointmentDate.Value.Date < DateTime.Today)
                {
                    NotificationHelper.ShowWarning("Nowa data wizyty nie może być w przeszłości.");
                    return;
                }

                // ✅ KLUCZOWE: Użyj NOWEJ daty i godziny wybranej przez użytkownika
                var nowaDataCzas = NewAppointmentDate.Value;

                // System.Diagnostics.Debug.WriteLine($"📅 Zmiana terminu: {_pacjentDoZmianyTerminu.R_GG_MM:yyyy-MM-dd HH:mm} → {nowaDataCzas:yyyy-MM-dd HH:mm}");
                var statusStarejWizyty = _pacjentDoZmianyTerminu.R_Status;

                // ✅ Zaktualizuj w bazie
                var db = new AccessDbContext();
                var record = new AccessDbContext.RejestracjaRecord
                {
                    R_Data = nowaDataCzas.Date,
                    RStatus = statusStarejWizyty, // ✅ bez ZMIANA STATUSU
                    R_S_ID = _pacjentDoZmianyTerminu.R_S_ID ?? 0,
                    R_GG_MM = nowaDataCzas, // ✅ KLUCZOWE: Nowa data + godzina
                    R_Subject = _pacjentDoZmianyTerminu.R_Subject,
                    R_Uwagi = $"Przełożona z {_pacjentDoZmianyTerminu.R_GG_MM:dd.MM.yyyy HH:mm} na {nowaDataCzas:dd.MM.yyyy HH:mm}"
                };

                bool sukces = db.UpdateRejestracja(_pacjentDoZmianyTerminu.R_ID, record);

                if (sukces)
                {
                    // ✅ Odśwież kalendarz i listę
                    RefreshFromDb();
                    LoadPacjenciNaDzien();

                    NotificationHelper.ShowSuccess(
                        $"Wizyta przeniesiona!\n\n" +
                        $"Stara data: {_pacjentDoZmianyTerminu.R_Data:dd.MM.yyyy}\n" +
                        $"Nowa data: {nowaDataCzas:dd.MM.yyyy HH:mm}\n" +
                        $"Status: Przełożona");

                    // System.Diagnostics.Debug.WriteLine($"✅ Wizyta przeniesiona: R_ID={_pacjentDoZmianyTerminu.R_ID} → {nowaDataCzas:yyyy-MM-dd HH:mm}");

                    // ✅ KLUCZOWE: Przelicz statystyki po zmianie terminu
                    ObliczStatystyki();

                    // ✅ KLUCZOWE: Przywróć wybranego pacjenta po odświeżeniu
                    var r_id = _pacjentDoZmianyTerminu.R_ID;
                    WybranyPacjent = PacjenciNaDzien.FirstOrDefault(p => p.R_ID == r_id);
                    _pacjentDoZmianyTerminu = null;
                    OnPropertyChanged(nameof(CzyWybranoPacjentaDoZmiany));

                    // ✅ Powrót do widoku listy
                    ShowRescheduleView = false;
                    ShowListView = true;
                    ShowDetailsView = false;
                }
                else
                {
                    NotificationHelper.ShowError("Nie udało się zmienić terminu wizyty.");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd zmiany terminu: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        /// Odwołuje wizytę (zmienia status na "Anulowana")
        /// </summary>
        private void CancelAppointment(object? parameter)
        {
            if (_pacjentDoZmianyTerminu == null)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy.");
                return;
            }

            // ✅ Potwierdzenie od użytkownika
            var result = MessageBox.Show(
                $"Czy na pewno chcesz ODWOŁAĆ wizytę?\n\n" +
                $"Pacjent: {_pacjentDoZmianyTerminu.P_Imie} {_pacjentDoZmianyTerminu.P_Nazwisko}\n" +
                $"Data: {_pacjentDoZmianyTerminu.R_Data:dd.MM.yyyy HH:mm}\n" +
                $"Firma: {_pacjentDoZmianyTerminu.Firma_Nazwa}\n\n" +
                "Status zostanie zmieniony na: ANULOWANA",
                "Potwierdzenie odwołania wizyty",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var db = new AccessDbContext();
                var record = new AccessDbContext.RejestracjaRecord
                {
                    R_Data = _pacjentDoZmianyTerminu.R_Data,
                    RStatus = "Anulowana", // ✅ STATUS: Anulowana
                    R_S_ID = _pacjentDoZmianyTerminu.R_S_ID ?? 0,
                    R_GG_MM = _pacjentDoZmianyTerminu.R_GG_MM,
                    R_Subject = _pacjentDoZmianyTerminu.R_Subject,
                    R_Uwagi = $"Wizyta odwołana: {DateTime.Now:dd.MM.yyyy HH:mm}"
                };

                bool sukces = db.UpdateRejestracja(_pacjentDoZmianyTerminu.R_ID, record);

                if (sukces)
                {
                    // ✅ Odśwież kalendarz i listę
                    RefreshFromDb();
                    LoadPacjenciNaDzien();

                    NotificationHelper.ShowSuccess("Wizyta została odwołana.");
                    // System.Diagnostics.Debug.WriteLine($"✅ Wizyta odwołana: R_ID={_pacjentDoZmianyTerminu.R_ID}");

                    // ✅ KLUCZOWE: Przelicz statystyki po odwołaniu
                    ObliczStatystyki();

                    // ✅ Wyczyść wybranego pacjenta (wizyta odwołana)
                    WybranyPacjent = null;
                    _pacjentDoZmianyTerminu = null;
                    OnPropertyChanged(nameof(CzyWybranoPacjentaDoZmiany));

                    // ✅ Powrót do widoku listy
                    ShowRescheduleView = false;
                    ShowListView = true;
                    ShowDetailsView = false;
                }
                else
                {
                    NotificationHelper.ShowError("Nie udało się odwołać wizyty.");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd odwołania wizyty: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        private void CancelReschedule(object? parameter)
        {
            // ✅ KLUCZOWE: Przywróć wybranego pacjenta
            if (_pacjentDoZmianyTerminu != null)
            {
                WybranyPacjent = _pacjentDoZmianyTerminu;
                _pacjentDoZmianyTerminu = null;
                OnPropertyChanged(nameof(CzyWybranoPacjentaDoZmiany));
            }

            ShowRescheduleView = false;
            ShowListView = true;
            ShowDetailsView = false;
        }
        private static void ShowTopMostMessageBox(string message, string title)
        {
            var ownerWindow = new Window
            {
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Topmost = true,
                ShowActivated = true,
                Left = -10000,
                Top = -10000
            };

            try
            {
                ownerWindow.Show();
                ownerWindow.Activate();

                MessageBox.Show(
                    ownerWindow,
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                ownerWindow.Close();
            }
        }





        // nowa metoda do odswierzania listy pacjentow na dzien

        private void RefreshlistaFromDb()
        {
            RefreshFromDb();
            LoadPacjenciNaDzien();
            SortujListePoStatusie();
            ObliczStatystyki();
            // NotificationHelper.ShowSuccess("Dane listy zostały odświeżone.");
        }
        private void OtworzListaDoFaktor(object? obj)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"📋 Otwieranie zakładki: ListaDoFaktor");

                // ✅ ZMIENIONE: Przełącz na zakładkę TabItemExt o nazwie "NowaKartaBadan"
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // Znajdź TabControlExt w MainWindow
                    var tabControl = FindVisualChild<Syncfusion.Windows.Tools.Controls.TabControlExt>(mainWindow);
                    if (tabControl != null)
                    {
                        // Znajdź zakładkę "ListaDoFaktur" (x:Name="ListaDoFaktur")
                        foreach (var item in tabControl.Items)
                        {
                            if (item is Syncfusion.Windows.Tools.Controls.TabItemExt tabItem &&
                                tabItem.Name == "ListaDoFaktur")
                            {
                                tabControl.SelectedItem = tabItem;
                                // System.Diagnostics.Debug.WriteLine($"✅ Przełączono na zakładkę 'NListaDoFaktur'");
                                NotificationHelper.ShowSuccess("Otwarto zakładkę 'ListaDoFaktur'");
                                return;
                            }
                        }

                        // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono zakładki 'ListaDoFaktur'");
                        NotificationHelper.ShowWarning("Nie znaleziono zakładki 'ListaDoFaktur'");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono TabControlExt w MainWindow");
                        NotificationHelper.ShowWarning("Nie znaleziono kontrolki zakładek");
                    }
                }
                else
                {
                    NotificationHelper.ShowError("Nie można otworzyć zakładki - brak MainWindow");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd otwierania zakładki ListaDoFaktur: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        private void SavePersonalDataExecute(object? obj)
        {
            if (WybranyPacjent == null)
            {
                NotificationHelper.ShowWarning("Brak wybranego pacjenta do zapisu.");
                return;
            }

            try
            {
                // ✅ KROK 1: SKOPIUJ DANE PRZED ODŚWIEŻENIEM (unikamy utraty referencji)
                var imie = WybranyPacjent.P_Imie ?? "";
                var nazwisko = WybranyPacjent.P_Nazwisko ?? "";
                var r_id = WybranyPacjent.R_ID;

                // System.Diagnostics.Debug.WriteLine($"💾 Zapisywanie danych pacjenta: {imie} {nazwisko} (R_ID={r_id})");

                // ✅ ETAP 1: Aktualizuj dane pacjenta (P_Pacjent)
                bool pacjentZaktualizowany = UpdatePacjentData();

                if (!pacjentZaktualizowany)
                {
                    NotificationHelper.ShowError("Nie udało się zaktualizować danych pacjenta.");
                    return;
                }

                // ✅ ETAP 2: Aktualizuj dane skierowania (B_Skierowania)
                bool skierowanieZaktualizowane = UpdateSkierowanieData();

                if (!skierowanieZaktualizowane)
                {
                    NotificationHelper.ShowError("Nie udało się zaktualizować danych skierowania.");
                    return;
                }

                // ✅ ETAP 3: Odśwież kalendarz i listę (TO MOŻE WYCZYŚCIĆ WybranyPacjent!)
                RefreshFromDb();
                LoadPacjenciNaDzien();

                // ✅ ETAP 4: Przywróć zaznaczenie po odświeżeniu
                WybranyPacjent = PacjenciNaDzien.FirstOrDefault(p => p.R_ID == r_id);

                // ✅ ETAP 5: Powrót do widoku listy
                ShowListView = true;
                ShowDetailsView = false;

                // ✅ UŻYJ SKOPIOWANYCH DANYCH (nie WybranyPacjent który może być NULL!)
                NotificationHelper.ShowSuccess($"Dane pacjenta {imie} {nazwisko} zostały zapisane.");
                // System.Diagnostics.Debug.WriteLine($"✅ Dane pacjenta zapisane pomyślnie: {imie} {nazwisko}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd zapisu danych: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                NotificationHelper.ShowError($"Błąd zapisu: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ POPRAWIONE: Aktualizuje dane pacjenta w bazie (P_Pacjent) - używa AccessDbHelper
        /// </summary>
        private bool UpdatePacjentData()
        {
            if (WybranyPacjent?.P_ID == null)
            {
                // System.Diagnostics.Debug.WriteLine($"⚠️ Brak P_ID - pomijam aktualizację pacjenta");
                return false;
            }

            try
            {
                // ✅ POPRAWIONE: Używaj AccessDbHelper zamiast hardcoded path
                var dbHelper = new AccessDbHelper();

                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();

                    string sql = @"
                UPDATE P_Pacjent SET 
                    P_pesel = ?,
                    P_brak = ?,
                    P_płeć = ?,
                    P_imie = ?,
                    P_nazwisko = ?,
                    P_Ades_kod = ?,
                    P_Adres_ulica_numer = ?,
                    P_Ades_miasto = ?,
                    P_zawód = ?,
                    P_data_urodzenia = ?
                WHERE P_ID = ?";

                    using (var command = new OdbcCommand(sql, connection))
                    {
                        // Dodaj parametry
                        command.Parameters.AddWithValue("@P_pesel", WybranyPacjent.P_Pesel ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@P_brak", WybranyPacjent.BrakPESEL ?? false);
                        command.Parameters.AddWithValue("@P_płeć", WybranyPacjent.P_Plec ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@P_imie", WybranyPacjent.P_Imie ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@P_nazwisko", WybranyPacjent.P_Nazwisko ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@P_Ades_kod", WybranyPacjent.P_AdresKod ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@P_Adres_ulica_numer", WybranyPacjent.P_AdresUlica ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@P_Ades_miasto", WybranyPacjent.P_AdresMiasto ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@P_zawód", WybranyPacjent.P_Zawod ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@P_data_urodzenia", WybranyPacjent.P_DataUrodzenia ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@P_ID", WybranyPacjent.P_ID);

                        int rowsAffected = command.ExecuteNonQuery();

                        // System.Diagnostics.Debug.WriteLine($"✅ P_Pacjent zaktualizowany: P_ID={WybranyPacjent.P_ID}, Rows={rowsAffected}");
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd UPDATE P_Pacjent: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ✅ POPRAWIONE: Aktualizuje dane skierowania w bazie (B_Skierowania) - używa AccessDbHelper
        /// </summary>
        /// <summary>
        /// ✅ FINALNE: Aktualizuje dane skierowania w bazie (B_Skierowania)
        /// </summary>
        private bool UpdateSkierowanieData()
        {
            if (WybranyPacjent == null)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ KRYTYCZNY BŁĄD: WybranyPacjent = NULL w UpdateSkierowanieData()");
                return false;
            }

            if (WybranyPacjent.B_ID == null)
            {
                // System.Diagnostics.Debug.WriteLine($"⚠️ Brak B_ID - pomijam aktualizację skierowania");
                return true; // Nie jest błędem - może nie mieć skierowania
            }

            try
            {
                // System.Diagnostics.Debug.WriteLine($"💾 UpdateSkierowanieData START: B_ID={WybranyPacjent.B_ID}");

                // ✅ Bezpieczne kopiowanie wartości (unikamy utraty referencji)
                var b_id = WybranyPacjent.B_ID.Value;
                var b_dataSkierowania = WybranyPacjent.B_DataSkierowania;
                var b_typBadania = WybranyPacjent.B_TypBadania;
                var b_stanowisko = WybranyPacjent.B_Stanowisko;

                var dbHelper = new AccessDbHelper();

                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    // System.Diagnostics.Debug.WriteLine($"✅ Połączenie otwarte: {connection.State}");

                    // ✅ PEŁNY SQL - aktualizacja wszystkich edytowalnych pól
                    string sql = @"
                UPDATE B_Skierowania
                SET
                    B_DataSkierowania = ?,
                    B_TypBadania = ?,
                    B_Stanowisko = ?
                WHERE B_ID = ?";

                    using (var command = new OdbcCommand(sql, connection))
                    {
                        // Dodaj parametry
                        command.Parameters.AddWithValue("@B_DataSkierowania", b_dataSkierowania ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@B_TypBadania", b_typBadania ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@B_Stanowisko", b_stanowisko ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@B_ID", b_id);

                        // System.Diagnostics.Debug.WriteLine($"📊 Parametry SQL:");
                        // System.Diagnostics.Debug.WriteLine($"   B_DataSkierowania: {b_dataSkierowania}");
                        // System.Diagnostics.Debug.WriteLine($"   B_TypBadania: '{b_typBadania}'");
                        // System.Diagnostics.Debug.WriteLine($"   B_Stanowisko: '{b_stanowisko}'");
                        // System.Diagnostics.Debug.WriteLine($"   B_ID: {b_id}");

                        int rowsAffected = command.ExecuteNonQuery();

                        // System.Diagnostics.Debug.WriteLine($"✅ B_Skierowania zaktualizowane: B_ID={b_id}, Rows={rowsAffected}");

                        if (rowsAffected == 0)
                        {
                            // System.Diagnostics.Debug.WriteLine($"⚠️ UWAGA: Zaktualizowano 0 wierszy! B_ID={b_id} może nie istnieć w bazie.");
                            return false;
                        }

                        return true;
                    }
                }
            }
            catch (OdbcException odbcEx)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ BŁĄD ODBC w UpdateSkierowanieData:");
                // System.Diagnostics.Debug.WriteLine($"   Message: {odbcEx.Message}");
                // System.Diagnostics.Debug.WriteLine($"   ErrorCode: {odbcEx.ErrorCode}");

                for (int i = 0; i < odbcEx.Errors.Count; i++)
                {
                    var error = odbcEx.Errors[i];
                    // System.Diagnostics.Debug.WriteLine($"   Error[{i}]: {error.Message} (NativeError: {error.NativeError}, SQLState: {error.SQLState})");
                }

                return false;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ BŁĄD OGÓLNY w UpdateSkierowanieData:");
                // System.Diagnostics.Debug.WriteLine($"   Type: {ex.GetType().Name}");
                // System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    // System.Diagnostics.Debug.WriteLine($"   InnerException: {ex.InnerException.Message}");
                }

                return false;
            }
        }

        #endregion

        #region ✅ NOWE: Metody

        /// <summary>
        /// Ładuje pacjentów na wybrany dzień z bazy danych
        /// </summary>
        private void LoadPacjenciNaDzien()
        {
            if (!SelectedDate.HasValue)
            {
                PacjenciNaDzien?.Clear();
                return;
            }

            try
            {
                // System.Diagnostics.Debug.WriteLine($"🔄 Ładowanie pacjentów na: {SelectedDate.Value:yyyy-MM-dd}");

                var db = new AccessDbContext();
                var selectedDay = SelectedDate.Value.Date;

                // ✅ OPTYMALIZACJA: Najpierw pobierz wszystkie, potem filtruj w pamięci
                var wszystkieRejestracje = db.GetRejestracje();

                var rejestracjeNaDzien = wszystkieRejestracje
                    .Where(r => r.R_Data.HasValue &&
                                r.R_Data.Value.Date == selectedDay)
                    .OrderBy(r => r.R_GG_MM ?? DateTime.MinValue)
                    .ToList();

                // System.Diagnostics.Debug.WriteLine($"   Znaleziono: {rejestracjeNaDzien.Count} pacjentów");

                // ✅ Zapisz pełną listę PRZED filtrowaniem
                _allPacjenciNaDzien = new ObservableCollection<RejestracjaItem>();

                // ✅ Czyść kolekcję PRZED dodawaniem (uniknięcie duplikatów)
                PacjenciNaDzien.Clear();

                foreach (var r in rejestracjeNaDzien)
                {
                    var item = new RejestracjaItem
                    {
                        R_ID = r.R_ID ?? 0,
                        R_B_ID = r.R_B_ID,
                        R_Data = r.R_Data,
                        R_Status = r.RStatus,
                        R_Employee_ID = r.R_Employee_ID,
                        R_S_ID = r.R_S_ID,
                        R_P_ID = r.R_P_ID,
                        R_GG_MM = r.R_GG_MM,
                        R_Uwagi = r.R_Uwagi,
                        R_Subject = r.R_Subject,

                        // ═══════════════════════════════════════════════════════
                        // ✅ PACJENT (z JOIN P_Pacjent) - WSZYSTKIE POLA
                        // ═══════════════════════════════════════════════════════
                        P_ID = r.P_ID,                   // ✅ KLUCZOWE!
                        P_Imie = r.P_Imie,
                        P_Nazwisko = r.P_Nazwisko,
                        P_Pesel = r.P_Pesel,
                        BrakPESEL = r.BrakPESEL,         // ✅ DODANE!
                        P_Telefon = r.P_Telefon,
                        P_Email = r.P_Email,
                        P_Plec = r.P_Plec,
                        P_DataUrodzenia = r.P_DataUrodzenia,
                        P_Zawod = r.P_Zawod,
                        P_AdresUlica = r.P_AdresUlica,
                        P_AdresKod = r.P_AdresKod,
                        P_AdresMiasto = r.P_AdresMiasto,
                        P_FirmaId = r.P_FirmaId,

                        // ═══════════════════════════════════════════════════════
                        // ✅ FIRMA (z JOIN Firma) - WSZYSTKIE POLA
                        // ═══════════════════════════════════════════════════════
                        Firma_Nazwa = r.Firma_Nazwa,
                        Firma_Kod = r.Firma_Kod,
                        Firma_Miejscowosc = r.Firma_Miejscowosc,
                        Firma_Ulica = r.Firma_Ulica,
                        Firma_Email = r.Firma_Email,

                        // ═══════════════════════════════════════════════════════
                        // ✅ SKIEROWANIE (z JOIN B_Skierowania) - WSZYSTKIE POLA
                        // ═══════════════════════════════════════════════════════
                        B_ID = r.B_ID,
                        B_DataSkierowania = r.B_DataSkierowania,
                        B_TypBadania = r.B_TypBadania,
                        B_Stanowisko = r.B_Stanowisko,
                        B_RegistrationDate = r.B_RegistrationDate,

                        // Czynniki szkodliwe
                        B_CzynnikFizyczny = r.B_CzynnikFizyczny,
                        B_CzynnikFizycznyOpis = r.B_CzynnikFizycznyOpis,
                        B_CzynnikPylowy = r.B_CzynnikPylowy,
                        B_CzynnikPylowyOpis = r.B_CzynnikPylowyOpis,
                        B_CzynnikChemiczny = r.B_CzynnikChemiczny,
                        B_CzynnikChemicznyOpis = r.B_CzynnikChemicznyOpis,
                        B_CzynnikBiologiczny = r.B_CzynnikBiologiczny,
                        B_CzynnikBiologicznyOpis = r.B_CzynnikBiologicznyOpis,
                        B_CzynnikInny = r.B_CzynnikInny,
                        B_CzynnikInnyOpis = r.B_CzynnikInnyOpis,

                        // Dokumenty
                        B_Zaswiadczenie = r.B_Zaswiadczenie,
                        B_Ksiazeczka = r.B_Ksiazeczka
                    };

                    // ✅ Dodaj do OBIE list (pełna + wyświetlana)
                    _allPacjenciNaDzien.Add(item);
                    PacjenciNaDzien.Add(item);
                }

                // ✅ Zastosuj filtr po załadowaniu (jeśli użytkownik wpisał tekst)
                ApplyFilterNazwisko();

                // ✅ NOWE: Sortuj listę po statusie zaraz po załadowaniu
                SortujListePoStatusie();

                // Force UI update (już nie potrzebne bo ApplyFilterNazwisko robi OnPropertyChanged)
                // OnPropertyChanged(nameof(PacjenciNaDzien));
                // OnPropertyChanged(nameof(LiczbaPacjentow));

                // System.Diagnostics.Debug.WriteLine($"✅ Załadowano {_allPacjenciNaDzien.Count} pacjentów (pełna lista), wyświetlanych: {PacjenciNaDzien.Count}");

                // System.Diagnostics.Debug.WriteLine($"✅ Załadowano {PacjenciNaDzien.Count} pacjentów do UI (pełne dane: Pacjent + Firma + Skierowanie)");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd ładowania pacjentów: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Wydruk listy pacjentów na wybrany dzień
        /// </summary>
        private void WydrukListy(object? parameter)
        {
            // ✅ Sprawdzenie wewnątrz metody zamiast CanExecute
            if (!SelectedDate.HasValue)
            {
                NotificationHelper.ShowWarning("Wybierz datę w kalendarzu aby wygenerować wydruk.");
                return;
            }

            if (PacjenciNaDzien == null || PacjenciNaDzien.Count == 0)
            {
                NotificationHelper.ShowWarning("Brak pacjentów na wybrany dzień. Wybierz inną datę.");
                return;
            }

            try
            {
                // System.Diagnostics.Debug.WriteLine($"🖨️ Wydruk listy pacjentów na dzień: {SelectedDate.Value:yyyy-MM-dd}");
                // System.Diagnostics.Debug.WriteLine($"   Liczba pacjentów: {PacjenciNaDzien.Count}");

                // ✅ Generuj PDF (na razie TXT) w A:\Rejestracja
                var filePath = WizytyPrintHelper.GenerujPdfListyPacjentow(PacjenciNaDzien, SelectedDate.Value);

                // ✅ Pokaż podgląd (otwórz plik)
                WizytyPrintHelper.ShowPrintPreview(filePath, $"Lista pacjentów - {SelectedDate.Value:dd.MM.yyyy}");

                NotificationHelper.ShowSuccess($"Wydruk zapisano: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd wydruku: {ex.Message}");
                NotificationHelper.ShowError($"Błąd wydruku: {ex.Message}");
            }
        }

        /// <summary>
        /// Oblicza statystyki dla wybranego dnia
        /// </summary>
        private void ObliczStatystyki()
        {
            if (!SelectedDate.HasValue || PacjenciNaDzien == null)
            {
                LiczbaWizytDzisiaj = 0;
                WolneSloty = 0;
                PierwszaWizyta = TimeSpan.Zero;
                OstatniaWizyta = TimeSpan.Zero;
                return;
            }

            try
            {
                // Liczba wizyt
                LiczbaWizytDzisiaj = PacjenciNaDzien.Count;

                // ✅ NOWE: Liczba wizyt w realizacji/zrealizowanych
                // (status inny niż: rejestracja, dokumentacja, przełożona)
                LiczbaWizytWRealizacji = PacjenciNaDzien.Count(p =>
                {
                    var status = p.R_Status?.ToLower() ?? "";
                    return status != "rejestracja" &&
                           status != "dokumentacja" &&
                           status != "przełożona";
                });

                // Godziny pracy (domyślnie 11:00 - 15:00, 5min slot = 48 slotów)
                const int startHour = 11;
                const int endHour = 15;
                const int slotMinutes = 5;
                int totalSlots = ((endHour - startHour) * 60) / slotMinutes; // 48 slotów

                // Wolne sloty = total - zajęte
                WolneSloty = Math.Max(0, totalSlots - LiczbaWizytDzisiaj);

                // Pierwsza i ostatnia wizyta
                if (PacjenciNaDzien.Any())
                {
                    var wizytyZGodzina = PacjenciNaDzien
                        .Where(p => p.R_GG_MM.HasValue)
                        .OrderBy(p => p.R_GG_MM.Value)
                        .ToList();

                    if (wizytyZGodzina.Any())
                    {
                        var pierwsza = wizytyZGodzina.First().R_GG_MM.Value;
                        var ostatnia = wizytyZGodzina.Last().R_GG_MM.Value;

                        PierwszaWizyta = pierwsza.TimeOfDay;
                        OstatniaWizyta = ostatnia.TimeOfDay;
                    }
                    else
                    {
                        PierwszaWizyta = TimeSpan.Zero;
                        OstatniaWizyta = TimeSpan.Zero;
                    }
                }
                else
                {
                    PierwszaWizyta = TimeSpan.Zero;
                    OstatniaWizyta = TimeSpan.Zero;
                }

                // System.Diagnostics.Debug.WriteLine($"📊 Statystyki: Wizyty={LiczbaWizytDzisiaj}, W realizacji={LiczbaWizytWRealizacji}, Wolne={WolneSloty}, Pierwsza={PierwszaWizytaFormatted}, Ostatnia={OstatniaWizytaFormatted}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd obliczania statystyk: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA: Sortuje listę pacjentów według statusu (Zaplanowana → W trakcie → Odbyta) + godzina
        /// </summary>
        private void SortujListePoStatusie()
        {
            if (PacjenciNaDzien == null || PacjenciNaDzien.Count == 0)
                return;

            try
            {
                // Zapisz wybranego pacjenta
                var wybranyId = WybranyPacjent?.R_ID;

                // Pobierz listę i posortuj
                var posortowanaLista = PacjenciNaDzien
                    .OrderBy(p => GetStatusPriority(p.R_Status))     // 1. Status (Zaplanowana=1, W trakcie=2, Odbyta=3)
                    .ThenBy(p => p.R_GG_MM ?? DateTime.MinValue)     // 2. Godzina
                    .ToList();

                // Wyczyść i dodaj z powrotem w nowej kolejności
                PacjenciNaDzien.Clear();
                foreach (var item in posortowanaLista)
                {
                    PacjenciNaDzien.Add(item);
                }

                // Przywróć zaznaczenie
                if (wybranyId.HasValue)
                {
                    WybranyPacjent = PacjenciNaDzien.FirstOrDefault(p => p.R_ID == wybranyId.Value);
                }

                // System.Diagnostics.Debug.WriteLine($"🔄 Posortowano listę po statusie: {PacjenciNaDzien.Count} pacjentów");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd sortowania: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ Zwraca priorytet statusu dla sortowania
        /// KOLEJNOŚĆ: Rejestracja (1) → Dokumentacja (2) → Zaplanowana (3) → W trakcie (4) → Odbyta (5)
        /// </summary>
        private int GetStatusPriority(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return 3; // Brak statusu = Zaplanowana

            return status.ToLower() switch
            {
                "rejestracja" => 1,      // ✅ Najwyższy priorytet
                "dokumentacja" => 2,     // ✅ NOWY: Po rejestracji
                "zaplanowana" => 3,      // Standard
                "w trakcie" => 4,        // W trakcie realizacji
                "odbyta" => 5,           // Zakończone
                "anulowana" => 6,        // Na końcu
                "przełożona" => 3,       // Jak zaplanowana
                "nieobecność" => 5,      // Jak anulowana
                "zamknięta" => 6,        // Jak odbyta
                _ => 3                   // Domyślnie jak zaplanowana
            };
        }

        /// <summary>
        /// ✅ NOWA: Sprawdza czy wizyta ma status "Zamknięta" (nie można jej modyfikować)
        /// </summary>
        private bool CzyWizytaZamknieta()
        {
            if (WybranyPacjent == null)
                return false;

            var status = WybranyPacjent.R_Status?.ToLower();

            if (status == "zamknięta" || status == "zamknieta")
            {
                ShowTopMostMessageBox(
                    "Nie można zmienić statusu wizyty.\n\n" +
                    "Wizyta ma status: ZAMKNIĘTA\n\n" +
                    "Wizyty zamknięte nie mogą być modyfikowane.",
                    "Status wizyty: ZAMKNIĘTA");
                return true;
            }

            return false;
        }

        /// <summary>
        /// ✅ NOWA: Zmienia status wizyty na "W trakcie"
        /// </summary>
        private void ZmienStatusWTrakcie(object? parameter)
        {
            if (WybranyPacjent == null)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy.");
                return;
            }

            // ✅ WALIDACJA: Sprawdź czy wizyta nie jest zamknięta
            if (CzyWizytaZamknieta())
                return;

            try
            {
                var db = new AccessDbContext();
                var record = new AccessDbContext.RejestracjaRecord
                {
                    R_Data = WybranyPacjent.R_Data,
                    RStatus = "W trakcie",
                    R_S_ID = WybranyPacjent.R_S_ID ?? 0,
                    R_GG_MM = WybranyPacjent.R_GG_MM,
                    R_Subject = WybranyPacjent.R_Subject,
                    R_Uwagi = WybranyPacjent.R_Uwagi
                };

                bool sukces = db.UpdateRejestracja(WybranyPacjent.R_ID, record);

                if (sukces)
                {
                    // Aktualizuj lokalnie
                    WybranyPacjent.R_Status = "W trakcie";

                    // Posortuj listę
                    SortujListePoStatusie();

                    // Odśwież kalendarz
                    RefreshFromDb();

                    // ✅ KLUCZOWE: Przelicz statystyki
                    ObliczStatystyki();

                    NotificationHelper.ShowSuccess($"Status zmieniony na: W trakcie");
                    // System.Diagnostics.Debug.WriteLine($"✅ Status zmieniony: R_ID={WybranyPacjent.R_ID} → W trakcie");
                }
                else
                {
                    NotificationHelper.ShowError("Nie udało się zmienić statusu.");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd zmiany statusu: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA: Zmienia status wizyty na "Odbyta"
        /// </summary>
        private void ZmienStatusOdbyta(object? parameter)
        {
            if (WybranyPacjent == null)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy.");
                return;
            }

            // ✅ WALIDACJA: Sprawdź czy wizyta nie jest zamknięta
            if (CzyWizytaZamknieta())
                return;

            try
            {
                var db = new AccessDbContext();
                var record = new AccessDbContext.RejestracjaRecord
                {
                    R_Data = WybranyPacjent.R_Data,
                    RStatus = "Odbyta",
                    R_S_ID = WybranyPacjent.R_S_ID ?? 0,
                    R_GG_MM = WybranyPacjent.R_GG_MM,
                    R_Subject = WybranyPacjent.R_Subject,
                    R_Uwagi = WybranyPacjent.R_Uwagi
                };

                bool sukces = db.UpdateRejestracja(WybranyPacjent.R_ID, record);

                if (sukces)
                {
                    // Aktualizuj lokalnie
                    WybranyPacjent.R_Status = "Odbyta";

                    // Posortuj listę
                    SortujListePoStatusie();

                    // Odśwież kalendarz
                    RefreshFromDb();

                    // ✅ KLUCZOWE: Przelicz statystyki
                    ObliczStatystyki();

                    NotificationHelper.ShowSuccess($"Status zmieniony na: Odbyta");
                    // System.Diagnostics.Debug.WriteLine($"✅ Status zmieniony: R_ID={WybranyPacjent.R_ID} → Odbyta");
                }
                else
                {
                    NotificationHelper.ShowError("Nie udało się zmienić statusu.");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd zmiany statusu: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA: Zmienia status wizyty na "Dokumentacja"
        /// </summary>
        private void ZmienStatusDokumentacja(object? parameter)
        {
            if (WybranyPacjent == null)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy.");
                return;
            }

            // ✅ WALIDACJA: Sprawdź czy wizyta nie jest zamknięta
            if (CzyWizytaZamknieta())
                return;

            try
            {
                var db = new AccessDbContext();
                var record = new AccessDbContext.RejestracjaRecord
                {
                    R_Data = WybranyPacjent.R_Data,
                    RStatus = "Dokumentacja",
                    R_S_ID = WybranyPacjent.R_S_ID ?? 0,
                    R_GG_MM = WybranyPacjent.R_GG_MM,
                    R_Subject = WybranyPacjent.R_Subject,
                    R_Uwagi = WybranyPacjent.R_Uwagi
                };

                bool sukces = db.UpdateRejestracja(WybranyPacjent.R_ID, record);

                if (sukces)
                {
                    // Aktualizuj lokalnie
                    WybranyPacjent.R_Status = "Dokumentacja";

                    // Posortuj listę
                    SortujListePoStatusie();

                    // Odśwież kalendarz
                    RefreshFromDb();

                    // ✅ KLUCZOWE: Przelicz statystyki
                    ObliczStatystyki();

                    NotificationHelper.ShowSuccess($"Status zmieniony na: Dokumentacja");
                    // System.Diagnostics.Debug.WriteLine($"✅ Status zmieniony: R_ID={WybranyPacjent.R_ID} → Dokumentacja");
                }
                else
                {
                    NotificationHelper.ShowError("Nie udało się zmienić statusu.");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd zmiany statusu: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA: Zmienia status wizyty na "Nieobecność"
        /// </summary>
        private void ZmienStatusNieobecnosc(object? parameter)
        {
            if (WybranyPacjent == null)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy.");
                return;
            }

            // ✅ WALIDACJA: Sprawdź czy wizyta nie jest zamknięta
            if (CzyWizytaZamknieta())
                return;

            try
            {
                var db = new AccessDbContext();
                var record = new AccessDbContext.RejestracjaRecord
                {
                    R_Data = WybranyPacjent.R_Data,
                    RStatus = "Nieobecność",
                    R_S_ID = WybranyPacjent.R_S_ID ?? 0,
                    R_GG_MM = WybranyPacjent.R_GG_MM,
                    R_Subject = WybranyPacjent.R_Subject,
                    R_Uwagi = WybranyPacjent.R_Uwagi
                };

                bool sukces = db.UpdateRejestracja(WybranyPacjent.R_ID, record);

                if (sukces)
                {
                    // Aktualizuj lokalnie
                    WybranyPacjent.R_Status = "Nieobecność";

                    // Posortuj listę
                    SortujListePoStatusie();

                    // Odśwież kalendarz
                    RefreshFromDb();

                    // ✅ KLUCZOWE: Przelicz statystyki
                    ObliczStatystyki();

                    NotificationHelper.ShowSuccess($"Status zmieniony na: Nieobecność");
                    // System.Diagnostics.Debug.WriteLine($"✅ Status zmieniony: R_ID={WybranyPacjent.R_ID} → Nieobecność");
                }
                else
                {
                    NotificationHelper.ShowError("Nie udało się zmienić statusu.");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd zmiany statusu: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA: Przełącza widok z listy na szczegóły pacjenta
        /// </summary>
        private void EdytujPacjenta(object? parameter)
        {
            if (WybranyPacjent == null)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy.");
                return;
            }

            // Przełącz widok
            ShowListView = false;
            ShowDetailsView = true;

            // System.Diagnostics.Debug.WriteLine($"✅ Edycja pacjenta: {WybranyPacjent.P_Imie} {WybranyPacjent.P_Nazwisko} (R_ID={WybranyPacjent.R_ID})");
        }

        /// <summary>
        /// ✅ NOWA: Wraca do widoku listy pacjentów
        /// </summary>
        private void PowrotDoListy(object? parameter)
        {
            ShowListView = true;
            ShowDetailsView = false;

            // System.Diagnostics.Debug.WriteLine($"🔙 Powrót do listy pacjentów");
        }

        /// <summary>
        /// ✅ NOWA: Otwiera zakładkę "Nowa Karta" (SkierListaPacjentowView)
        /// </summary>
        private void OtworzNowaKarta(object? parameter)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"📋 Otwieranie zakładki: Nowa Karta");

                // ✅ ZMIENIONE: Przełącz na zakładkę TabItemExt o nazwie "NowaKartaBadan"
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // Znajdź TabControlExt w MainWindow
                    var tabControl = FindVisualChild<Syncfusion.Windows.Tools.Controls.TabControlExt>(mainWindow);
                    if (tabControl != null)
                    {
                        // Znajdź zakładkę "NowaKartaBadan" (x:Name="NowaKartaBadan")
                        foreach (var item in tabControl.Items)
                        {
                            if (item is Syncfusion.Windows.Tools.Controls.TabItemExt tabItem &&
                                tabItem.Name == "NowaKartaBadan")
                            {
                                tabControl.SelectedItem = tabItem;
                                // System.Diagnostics.Debug.WriteLine($"✅ Przełączono na zakładkę 'Nowa Karta'");
                                NotificationHelper.ShowSuccess("Otwarto zakładkę 'Nowa Karta'");
                                return;
                            }
                        }

                        // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono zakładki 'NowaKartaBadan'");
                        NotificationHelper.ShowWarning("Nie znaleziono zakładki 'Nowa Karta'");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono TabControlExt w MainWindow");
                        NotificationHelper.ShowWarning("Nie znaleziono kontrolki zakładek");
                    }
                }
                else
                {
                    NotificationHelper.ShowError("Nie można otworzyć zakładki - brak MainWindow");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd otwierania zakładki Nowa Karta: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA: Otwiera zakładkę "Zakończ Badanie" (BadaniaNewView)
        /// </summary>
        private void ZakonczBadanie(object? parameter)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"🏁 Otwieranie zakładki: Zakończ Badanie");

                // ✅ ZMIENIONE: Przełącz na zakładkę TabItemExt o nazwie "BadaniaNew"
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // Znajdź TabControlExt w MainWindow
                    var tabControl = FindVisualChild<Syncfusion.Windows.Tools.Controls.TabControlExt>(mainWindow);
                    if (tabControl != null)
                    {
                        // Znajdź zakładkę "BadaniaNew" (x:Name="BadaniaNew")
                        foreach (var item in tabControl.Items)
                        {
                            if (item is Syncfusion.Windows.Tools.Controls.TabItemExt tabItem &&
                                tabItem.Name == "BadaniaNew")
                            {
                                tabControl.SelectedItem = tabItem;
                                // System.Diagnostics.Debug.WriteLine($"✅ Przełączono na zakładkę 'Zakończ Badanie'");
                                NotificationHelper.ShowSuccess("Otwarto zakładkę 'Zakończ Badanie'");
                                return;
                            }
                        }

                        // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono zakładki 'BadaniaNew'");
                        NotificationHelper.ShowWarning("Nie znaleziono zakładki 'Zakończ Badanie'");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono TabControlExt w MainWindow");
                        NotificationHelper.ShowWarning("Nie znaleziono kontrolki zakładek");
                    }
                }
                else
                {
                    NotificationHelper.ShowError("Nie można otworzyć zakładki - brak MainWindow");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd otwierania zakładki Zakończ Badanie: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ NOWA: Kończy wizytę i przełącza na zakładkę Badania z filtrem ID skierowania
        /// </summary>
        private void BadanieEnd(object? parameter)
        {
            if (WybranyPacjent == null || !WybranyPacjent.B_ID.HasValue)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy aby zakończyć badanie.");
                return;
            }

            try
            {
                // System.Diagnostics.Debug.WriteLine($"🏁 Koniec badania: B_ID={WybranyPacjent.B_ID}");

                // ✅ ZMIENIONE: Przełącz na zakładkę TabItemExt o nazwie "BadaniaNew" z ID skierowania
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // Znajdź TabControlExt w MainWindow
                    var tabControl = FindVisualChild<Syncfusion.Windows.Tools.Controls.TabControlExt>(mainWindow);
                    if (tabControl != null)
                    {
                        // Znajdź zakładkę "BadaniaNew" (x:Name="BadaniaNew")
                        foreach (var item in tabControl.Items)
                        {
                            if (item is Syncfusion.Windows.Tools.Controls.TabItemExt tabItem &&
                                tabItem.Name == "BadaniaNew")
                            {
                                tabControl.SelectedItem = tabItem;

                                // ✅ Ustaw filtr ID skierowania w ViewModelu zakładki
                                if (tabItem.Content is BadaniaNewView badaniaView)
                                {
                                    badaniaView.SetFilterByIdSkierowania(WybranyPacjent.B_ID.Value);

                                    // System.Diagnostics.Debug.WriteLine($"✅ Przełączono na zakładkę 'Badania' z filtrem ID={WybranyPacjent.B_ID}");
                                    NotificationHelper.ShowSuccess("Otwarto zakładkę 'Badania' (zakończono wizytę)");
                                }
                                else
                                {
                                    // System.Diagnostics.Debug.WriteLine($"⚠️ Nie można ustawić filtru - zawartość zakładki nie jest typu BadaniaNewView");
                                    NotificationHelper.ShowWarning("Nie można przełączyć na zakładkę 'Badania'");
                                }

                                return;
                            }
                        }

                        // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono zakładki 'BadaniaNew'");
                        NotificationHelper.ShowWarning("Nie znaleziono zakładki 'Zakończ Badanie'");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono TabControlExt w MainWindow");
                        NotificationHelper.ShowWarning("Nie znaleziono kontrolki zakładek");
                    }
                }
                else
                {
                    NotificationHelper.ShowError("Nie można otworzyć zakładki - brak MainWindow");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd podczas kończenia badania: {ex.Message}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        #endregion

        #region ✅ NOWE: Metody wydruków formularzy

        /// <summary>
        /// Wydruk wszystkich formularzy (Karta + Orzeczenie)
        /// </summary>
        private void PrintAll(object? parameter)
        {
            if (WybranyPacjent == null || !WybranyPacjent.B_ID.HasValue)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy aby wydrukować formularze.");
                return;
            }

            try
            {
                // System.Diagnostics.Debug.WriteLine($"📑 PrintAll: B_ID={WybranyPacjent.B_ID}");

                // Wydrukuj Karte i Orzeczenie są w jednym pliku
                ShowPdfPreviewForTemplate(WybranyPacjent.B_ID.Value, "ASMED__Karta badania Orzeczenie.pdf");

                NotificationHelper.ShowSuccess("Wygenerowano Komplet formularzy");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd wydruku: {ex.Message}");
                NotificationHelper.ShowError($"Błąd wydruku: {ex.Message}");
            }
        }

        /// <summary>
        /// Wydruk Karty badania profilaktycznego
        /// </summary>
        private void PrintKarta(object? parameter)
        {
            if (WybranyPacjent == null || !WybranyPacjent.B_ID.HasValue)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy aby wydrukować Kartę badania.");
                return;
            }

            try
            {
                // System.Diagnostics.Debug.WriteLine($"🗂️ PrintKarta: B_ID={WybranyPacjent.B_ID}");
                ShowPdfPreviewForTemplate(WybranyPacjent.B_ID.Value, "ASMED__Karta badania profilaktycznego.pdf");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd wydruku Karty: {ex.Message}");
                NotificationHelper.ShowError($"Błąd wydruku: {ex.Message}");
            }
        }

        /// <summary>
        /// Wydruk Orzeczenia lekarskiego (x2)
        /// </summary>
        private void PrintOrzeczenie(object? parameter)
        {
            if (WybranyPacjent == null || !WybranyPacjent.B_ID.HasValue)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy aby wydrukować Orzeczenie.");
                return;
            }

            try
            {
                // System.Diagnostics.Debug.WriteLine($"📝 PrintOrzeczenie: B_ID={WybranyPacjent.B_ID}");
                ShowPdfPreviewForTemplate(WybranyPacjent.B_ID.Value, "ASMED_Orzeczenie.pdf");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd wydruku Orzeczenia: {ex.Message}");
                NotificationHelper.ShowError($"Błąd wydruku: {ex.Message}");
            }
        }

        /// <summary>
        /// Wydruk książeczki sanitarnej
        /// </summary>
        private void PrintSanitarne(object? parameter)
        {
            if (WybranyPacjent == null || !WybranyPacjent.B_ID.HasValue)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy aby wydrukować książeczkę sanitarną.");
                return;
            }

            try
            {
                // System.Diagnostics.Debug.WriteLine($"🧪 PrintSanitarne: B_ID={WybranyPacjent.B_ID}");
                ShowPdfPreviewForTemplate(WybranyPacjent.B_ID.Value, "ASMED__Sanitarne.pdf");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd wydruku Sanitarne: {ex.Message}");
                NotificationHelper.ShowError($"Błąd wydruku: {ex.Message}");
            }
        }

        /// <summary>
        /// Wypełnia szablon PDF danymi pacjenta i otwiera podgląd
        /// </summary>
        private void ShowPdfPreviewForTemplate(int bId, string templateFileName)
        {
            try
            {
                var templateFile = Path.Combine("A:", "formularz", templateFileName);
                var outputDir = Path.Combine("A:", "Formuarz_Druki");

                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                var safeLastName = string.IsNullOrWhiteSpace(WybranyPacjent?.P_Nazwisko) ? "patient" : WybranyPacjent.P_Nazwisko;
                var outputFile = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(templateFileName)}_{bId}_{safeLastName}.pdf");

                // ✅ PRZEKAŻ NAZWĘ PLIKU DO MAPOWANIA (aby wiedzieć czy dodawać QR/Barcode)
                var values = MapujPolaFormularza(templateFileName);
                var filled = PdfFormService.FillForm(templateFile, values, outputFile);

                if (filled != null)
                {
                    var w = new ASMED.WPF.Views.PdfPreviewWindow();

                    // ✅ NOWE: Przekaż dane dla funkcji Email (z domyślnym adresem)
                    var emailAddress = string.IsNullOrWhiteSpace(WybranyPacjent?.Firma_Email)
                        ? "info@adres.pl"
                        : WybranyPacjent.Firma_Email;
                    var numerFaktury = WybranyPacjent?.SkierowanieNumer ?? string.Empty;

                    w.LoadFileWithMetadata(filled, emailAddress, numerFaktury);
                    w.ShowDialog();
                }
                else
                {
                    MessageBox.Show($"Nie udało się wypełnić formularza: {templateFileName}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd otwierania podglądu PDF: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Mapuje pola formularza PDF (zgodnie z SkierPacjentaViewModel.MapujPolaFormularza)
        /// ✅ ZMIENIONE: Parametr templateFileName dla warunkowego QR/Barcode
        /// </summary>
        private Dictionary<string, string> MapujPolaFormularza(string templateFileName = "")
        {
            var map = new Dictionary<string, string>();

            if (WybranyPacjent == null)
                return map;

            // ✅ ASMED - Firma
            map["A_Nazwa"] = "Niepubliczny ZOZ ASMED S.C \n Kurzyńska Stefania  Kurzyński Zbigniew\n 04-028 Warszawa \n AL. Stanów Zjednoczonyc 51 pok. 204 \n tel. 22 81 44 02 \n NIP 113 83 31 776  Nr Księgi 7582";

            // ✅ Typ badania
            if (!string.IsNullOrEmpty(WybranyPacjent.B_TypBadania))
            {
                if (WybranyPacjent.B_TypBadania.ToUpper() == "W")
                    map["Typ_Bad"] = "wstępne";
                else if (WybranyPacjent.B_TypBadania.ToUpper() == "O")
                    map["Typ_Bad"] = "okresowe";
                else if (WybranyPacjent.B_TypBadania.ToUpper() == "K")
                    map["Typ_Bad"] = "kontrolne";
            }

            // ✅ Basic fields
            map["B_TypBadania"] = WybranyPacjent.B_TypBadania ?? string.Empty;
            map["B_Nazwisko_B_Imię"] = $"{WybranyPacjent.P_Nazwisko} {WybranyPacjent.P_Imie}".Trim();
            map["B_Firma"] = WybranyPacjent.Firma_Nazwa ?? string.Empty;
            map["B_zawód"] = !string.IsNullOrWhiteSpace(WybranyPacjent.B_Stanowisko)
                ? WybranyPacjent.B_Stanowisko
                : WybranyPacjent.P_Zawod ?? string.Empty;

            // ✅ Data urodzenia (ddMMyyyy)
            var dataUrodzenia = WybranyPacjent.P_DataUrodzenia.HasValue
                ? WybranyPacjent.P_DataUrodzenia.Value.ToString("ddMMyyyy")
                : "";
            for (int i = 0; i < 8; i++)
                map[$"du_{i + 1}"] = dataUrodzenia.Length > i ? dataUrodzenia[i].ToString() : string.Empty;

            // ✅ Data skierowania
            // ✅ Data skierowania - POPRAWIONE: używaj null zamiast DateTime.Now
            if (WybranyPacjent.B_DataSkierowania.HasValue)
            {
                map["B_DataSkierowania"] = WybranyPacjent.B_DataSkierowania.Value.ToString("dd.MM.yyyy");

                var registrationDate = WybranyPacjent.B_DataSkierowania.Value.ToString("ddMMyyyy");

                map["dr_1"] = registrationDate.Length > 0 ? registrationDate[0].ToString() : string.Empty;
                map["dr_2"] = registrationDate.Length > 1 ? registrationDate[1].ToString() : string.Empty;
                map["mr_1"] = registrationDate.Length > 2 ? registrationDate[2].ToString() : string.Empty;
                map["mr_2"] = registrationDate.Length > 3 ? registrationDate[3].ToString() : string.Empty;
                map["yr_1"] = registrationDate.Length > 4 ? registrationDate[4].ToString() : string.Empty;
                map["yr_2"] = registrationDate.Length > 5 ? registrationDate[5].ToString() : string.Empty;
                map["yr_3"] = registrationDate.Length > 6 ? registrationDate[6].ToString() : string.Empty;
                map["yr_4"] = registrationDate.Length > 7 ? registrationDate[7].ToString() : string.Empty;
            }
            else
            {
                // ✅ Brak daty - pozostaw puste pola
                map["B_DataSkierowania"] = string.Empty;

                map["dr_1"] = string.Empty;
                map["dr_2"] = string.Empty;
                map["mr_1"] = string.Empty;
                map["mr_2"] = string.Empty;
                map["yr_1"] = string.Empty;
                map["yr_2"] = string.Empty;
                map["yr_3"] = string.Empty;
                map["yr_4"] = string.Empty;
            }

            // ✅ NOWE: Data rejestracji (B_RegistrationDate) - rozbicie na cyfry
            if (WybranyPacjent.B_RegistrationDate.HasValue)
            {
                map["B_RegistrationDate"] = WybranyPacjent.B_RegistrationDate.Value.ToString("dd.MM.yyyy");

                var registrationDateStr = WybranyPacjent.B_RegistrationDate.Value.ToString("ddMMyyyy");

                // rr_1, rr_2 - dzień (2 cyfry)
                map["rr_1"] = registrationDateStr.Length > 0 ? registrationDateStr[0].ToString() : string.Empty;
                map["rr_2"] = registrationDateStr.Length > 1 ? registrationDateStr[1].ToString() : string.Empty;

                // rm_1, rm_2 - miesiąc (2 cyfry)
                map["rm_1"] = registrationDateStr.Length > 2 ? registrationDateStr[2].ToString() : string.Empty;
                map["rm_2"] = registrationDateStr.Length > 3 ? registrationDateStr[3].ToString() : string.Empty;

                // ry_1, ry_2, ry_3, ry_4 - rok (4 cyfry)
                map["ry_1"] = registrationDateStr.Length > 4 ? registrationDateStr[4].ToString() : string.Empty;
                map["ry_2"] = registrationDateStr.Length > 5 ? registrationDateStr[5].ToString() : string.Empty;
                map["ry_3"] = registrationDateStr.Length > 6 ? registrationDateStr[6].ToString() : string.Empty;
                map["ry_4"] = registrationDateStr.Length > 7 ? registrationDateStr[7].ToString() : string.Empty;
            }
            else
            {
                // ✅ Brak daty rejestracji - pozostaw puste pola
                map["B_RegistrationDate"] = string.Empty;

                map["rr_1"] = string.Empty;
                map["rr_2"] = string.Empty;
                map["rm_1"] = string.Empty;
                map["rm_2"] = string.Empty;
                map["ry_1"] = string.Empty;
                map["ry_2"] = string.Empty;
                map["ry_3"] = string.Empty;
                map["ry_4"] = string.Empty;
            }




            // ✅ PESEL
            var pesel = WybranyPacjent.P_Pesel ?? string.Empty;
            for (int i = 1; i <= 11; i++)
                map[$"P_{i}"] = pesel.Length >= i ? pesel[i - 1].ToString() : string.Empty;
            map["PESEL"] = pesel;

            // ✅ Płeć
            map["M"] = (WybranyPacjent.P_Plec?.ToUpper() == "M") ? "X" : string.Empty;
            map["K"] = (WybranyPacjent.P_Plec?.ToUpper() == "K") ? "X" : string.Empty;
            map["Plec"] = WybranyPacjent.P_Plec ?? string.Empty;

            // ✅ Adres pacjenta
            map["P_adr"] = $"{WybranyPacjent.P_AdresUlica} {WybranyPacjent.P_AdresMiasto}".Trim();
            var kod = WybranyPacjent.P_AdresKod ?? string.Empty;
            var kodParts = kod.Split('-');
            map["kp_1"] = kodParts.Length > 0 ? kodParts[0] : string.Empty;
            map["kp_2"] = kodParts.Length > 1 ? kodParts[1] : string.Empty;
            map["Full_Adress"] = $"{WybranyPacjent.P_AdresKod} {WybranyPacjent.P_AdresMiasto}, {WybranyPacjent.P_AdresUlica}".Trim();

            // ✅ Adres firmy
            map["F_adr"] = $"{WybranyPacjent.Firma_Ulica} {WybranyPacjent.Firma_Miejscowosc}".Trim();
            var kodF = WybranyPacjent.Firma_Kod ?? string.Empty;
            var kodFParts = kodF.Split('-');
            map["KF_1"] = kodFParts.Length > 0 ? kodFParts[0] : string.Empty;
            map["KF_2"] = kodFParts.Length > 1 ? kodFParts[1] : string.Empty;
            map["Full_Adress_Firma"] = $"{WybranyPacjent.Firma_Kod} {WybranyPacjent.Firma_Miejscowosc}, {WybranyPacjent.Firma_Ulica}".Trim();

            // ✅ Czynniki szkodliwe
            map["SB_Tak"] = "X";
            bool szkodliwy = WybranyPacjent.B_CzynnikFizyczny == true ||
                     WybranyPacjent.B_CzynnikPylowy == true ||
                     WybranyPacjent.B_CzynnikChemiczny == true ||
                     WybranyPacjent.B_CzynnikBiologiczny == true ||
                     WybranyPacjent.B_CzynnikInny == true;
            map["SZ_Tak"] = szkodliwy ? "X" : string.Empty;
            map["SZ_NIE"] = !szkodliwy ? "X" : string.Empty;

            var opisy = new[] {
        WybranyPacjent.B_CzynnikFizycznyOpis,
        WybranyPacjent.B_CzynnikPylowyOpis,
        WybranyPacjent.B_CzynnikChemicznyOpis,
        WybranyPacjent.B_CzynnikBiologicznyOpis,
        WybranyPacjent.B_CzynnikInnyOpis
    };
            bool czyOpis = opisy.Any(x => !string.IsNullOrWhiteSpace(x));
            map["IS_TAK"] = czyOpis ? "X" : string.Empty;
            map["IS_NIE"] = !czyOpis ? "X" : string.Empty;
            map["IS_OPIS"] = string.Join(" - ", opisy.Where(x => !string.IsNullOrWhiteSpace(x)));

            // ✅ Nagłówki/stopki
            map["header"] = $"({WybranyPacjent.B_ID ?? 0}) - {(WybranyPacjent.B_DataSkierowania ?? DateTime.Now):dd.MM.yyyy}".Trim();
            map["header4"] = $"{WybranyPacjent.B_ID ?? 0} - {(WybranyPacjent.B_DataSkierowania ?? DateTime.Now):dd.MM.yyyy} {WybranyPacjent.P_Imie} {WybranyPacjent.P_Nazwisko}".Trim();
            map["footer"] = "ASMED - Niepubliczny Zakład Opieki Zdrowotnej04-028 Warszawa Al. Stanów Zjednoczonych51 pok.204 tel.22814402 NIP1138331776 Nr Księgi7582";
            map["header2"] = $"ID: {WybranyPacjent.B_ID ?? 0} / {WybranyPacjent.P_ID}".Trim();
            map["header3"] = $"Karta nr: {WybranyPacjent.B_ID ?? 0} ".Trim();

            int skierowanieId = WybranyPacjent.B_ID ?? 0;
            map["header3_barcode"] = (skierowanieId > 0)
                ? $"#{skierowanieId.ToString().PadLeft(4, '0')}"
                : string.Empty;
            map["PatientLastName"] = WybranyPacjent.P_Nazwisko ?? string.Empty;


            // ✅ NOWE: Data badania (rozbicie na pojedyncze cyfry)
            if (DodajDateBadaniaDoWydruku && DataBadaniaDoWydruku.HasValue)
            {
                // Format: ddMMyyyy (np. "12012025" dla 12.01.2025)
                var dataBadaniaStr = DataBadaniaDoWydruku.Value.ToString("ddMMyyyy");

                // bd_1, bd_2 - dzień (2 cyfry)
                map["bd1"] = dataBadaniaStr.Length > 0 ? dataBadaniaStr[0].ToString() : string.Empty;
                map["bd2"] = dataBadaniaStr.Length > 1 ? dataBadaniaStr[1].ToString() : string.Empty;

                // bm_1, bm_2 - miesiąc (2 cyfry)
                map["bm1"] = dataBadaniaStr.Length > 2 ? dataBadaniaStr[2].ToString() : string.Empty;
                map["bm2"] = dataBadaniaStr.Length > 3 ? dataBadaniaStr[3].ToString() : string.Empty;

                // by_1, by_2, by_3, by_4 - rok (4 cyfry)
                map["by1"] = dataBadaniaStr.Length > 4 ? dataBadaniaStr[4].ToString() : string.Empty;
                map["by2"] = dataBadaniaStr.Length > 5 ? dataBadaniaStr[5].ToString() : string.Empty;
                map["by3"] = dataBadaniaStr.Length > 6 ? dataBadaniaStr[6].ToString() : string.Empty;
                map["by4"] = dataBadaniaStr.Length > 7 ? dataBadaniaStr[7].ToString() : string.Empty;

                // Opcjonalnie: Pełna data jako string
                map["DataBadania"] = DataBadaniaDoWydruku.Value.ToString("dd.MM.yyyy");
                map["DataBadania_Full"] = DataBadaniaDoWydruku.Value.ToString("dd.MMMM.yyyy");
                map["DataBadania_Txt"] = DataBadaniaDoWydruku.Value.ToString("dd-MMMMM-yyyy");
                map["DataBadania_Day"] = DataBadaniaDoWydruku.Value.ToString("dd");
                map["DataBadania_Month"] = DataBadaniaDoWydruku.Value.ToString("MM");
                map["DataBadania_Year"] = DataBadaniaDoWydruku.Value.ToString("yyyy");
                map["DataBadania_DayOfWeek"] = DataBadaniaDoWydruku.Value.ToString("dddd");
                map["DataBadania_Time"] = DataBadaniaDoWydruku.Value.ToString("HH:mm");
                map["DataBadania_Timestamp"] = DataBadaniaDoWydruku.Value.ToString("yyyyMMddHHmmss");


                // System.Diagnostics.Debug.WriteLine($"✅ Data badania rozbita: {dataBadaniaStr} → bd_1={map["bd_1"]}, bd_2={map["bd_2"]}, bm_1={map["bm_1"]}, bm_2={map["bm_2"]}, by_1={map["by_1"]}, by_2={map["by_2"]}, by_3={map["by_3"]}, by_4={map["by_4"]}");
            }

            return map;
        }

        #endregion

        #region Existing Methods

        private void AddAppointment(object? parameter)
        {
            var newAppointment = new ScheduleAppointment
            {
                Subject = "Nowa wizyta",
                StartTime = System.DateTime.Now,
                EndTime = System.DateTime.Now.AddHours(1),
                AppointmentBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightBlue)
            };
            Appointments.Add(newAppointment);
        }
        private void LoadSampleAppointments()
        {
            Appointments.Add(new ScheduleAppointment
            {
                Subject = "Wizyta 1",
                StartTime = System.DateTime.Now.AddHours(1),
                EndTime = System.DateTime.Now.AddHours(2),
                AppointmentBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen)
            });
            Appointments.Add(new ScheduleAppointment
            {
                Subject = "Wizyta 2",
                StartTime = System.DateTime.Now.AddHours(3),
                EndTime = System.DateTime.Now.AddHours(4),
                AppointmentBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightCoral)
            });
        }

        /// <summary>
        /// ✅ PUBLIC: Odświeża kalendarz z bazy danych (wywoływane z XAML.cs)
        /// </summary>
        public void RefreshFromDb()
        {
            Appointments.Clear();
            var db = new AccessDbContext();
            var list = db.GetRejestracje();
            foreach (var r in list)
            {
                if (!r.R_Data.HasValue || !r.R_GG_MM.HasValue) continue;
                var start = new DateTime(r.R_GG_MM.Value.Year, r.R_GG_MM.Value.Month, r.R_GG_MM.Value.Day, r.R_GG_MM.Value.Hour, r.R_GG_MM.Value.Minute, 0);
                Appointments.Add(new ScheduleAppointment
                {
                    StartTime = start,
                    EndTime = start.AddMinutes(05),
                    Subject = r.R_Subject ?? r.R_Uwagi ?? "Wizyta",
                    Notes = r.R_Uwagi
                });
            }

            // System.Diagnostics.Debug.WriteLine($"✅ Odświeżono kalendarz: {Appointments.Count} wizyt");
            NotificationHelper.ShowSuccess($"Odświeżono kalendarz: {Appointments.Count} wizyt");
        }

        #endregion

        #region Helper Methods

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

        /// <summary>
        /// ✅ Filtrowanie listy pacjentów po:
        /// - ID skierowania (B_ID)
        /// - Nazwisku (z normalizacją polskich znaków)
        /// - Imieniu (z normalizacją polskich znaków)
        /// </summary>
        private void ApplyFilterNazwisko()
        {
            if (_allPacjenciNaDzien == null || _allPacjenciNaDzien.Count == 0)
            {
                // Jeśli nie ma pełnej listy, nie filtruj
                return;
            }

            if (string.IsNullOrWhiteSpace(FilterTextNazwisko))
            {
                // Brak filtra - pokaż wszystkich
                PacjenciNaDzien = new ObservableCollection<RejestracjaItem>(_allPacjenciNaDzien);
            }
            else
            {
                var filter = FilterTextNazwisko.Trim();
                var filtered = _allPacjenciNaDzien.Where(p =>
                {
                    // 1️⃣ Szukanie po ID skierowania (B_ID)
                    if (p.B_ID.HasValue && p.B_ID.Value.ToString().Contains(filter))
                        return true;

                    // 2️⃣ Szukanie po nazwisku (z normalizacją polskich liter)
                    if (TextNormalizationHelper.ContainsIgnoringDiacritics(p.P_Nazwisko ?? "", filter))
                        return true;

                    // 3️⃣ Szukanie po imieniu (z normalizacją polskich liter)
                    if (TextNormalizationHelper.ContainsIgnoringDiacritics(p.P_Imie ?? "", filter))
                        return true;

                    return false;
                }).ToList();

                PacjenciNaDzien = new ObservableCollection<RejestracjaItem>(filtered);
            }

            OnPropertyChanged(nameof(PacjenciNaDzien));
            OnPropertyChanged(nameof(LiczbaPacjentow));
            ObliczStatystyki();
        }



        #endregion
    }
}
// End of file WizytyViewViewModel.cs
