using ASMED.WPF.Helpers;
using ASMED.WPF.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System;

namespace ASMED.WPF.ViewModels.Skierowania
{
    public class SkierowanieDto
    {
        // B_Badania lista pól SQL
        public int? B_ID { get; set; }
        public int B_Pacjent_ID { get; set; }
        public string ?P_imie { get; set; } = string.Empty;
        public string ?P_nazwisko { get; set; } = string.Empty;
        public string ?P_pesel { get; set; } = string.Empty;
        public string ?Nazwa { get; set; } = string.Empty;
        public string? Firma_NIP { get; set; }      // ✅ DODANE
        public string? Firma_Cennik { get; set; }   // ✅ DODANE
        public DateTime? B_DataSkierowania { get; set; }
        public string ?B_TypBadania { get; set; } = string.Empty;
        public bool? B_Zaswiadczenie { get; set; }       // ✅ DODANE
        public DateTime? Bad_Data { get; internal set; }
        public DateTime? R_Data { get; internal set; }
        public string? FK_Numer { get; internal set; }
        public bool? B_Nowe { get; set; }
        public bool? B_Activ { get; set; }
        // B_Badania
        public bool? B_książeczka_sanepid { get; set; }
        public bool? B_Bad { get; set; }
        public bool? B_karta { get; set; }
        public bool? B_rejestr { get; set; }
        // Dla zgodności z XAML (MappingName) - usunięto set-only properties, zostają tylko gettery
        public string ?Firma => Nazwa;
        public string ?Imie => P_imie;
        public string ?Nazwisko => P_nazwisko;
        public string ?Pesel => P_pesel;
        public string ?Typ => B_TypBadania;
        public DateTime? Data => B_DataSkierowania;
        public int Id => B_ID ?? 0;
        public DateTime? DataBad => Bad_Data;
        public DateTime? DataRej => R_Data;
        public string? NumerFaktury => FK_Numer;

        public string? B_Stanowisko { get; internal set; }
        public string? P_zawod { get; internal set; }
    }

    public class SkierowaniaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<string> FilterTypes { get; } = new() { "All", "Imię", "Nazwisko", "PESEL", "Firma", "ID" };

        // NOWE: Opcje filtrowania dat
        public ObservableCollection<string> DateFilterOptions { get; } = new()
        {
            "All",
            "Bieżący Miesiąc",
            "Poprzedni Miesiąc",
            "Bieżący Rok",
            "Poprzedni Rok",
            "Wybrany okres"
        };

        public ObservableCollection<SkierowanieDto> Skierowania { get; set; } = new();
        public ObservableCollection<SkierowanieDto> SkierowaniaFiltered { get; set; } = new();

        private string ?_searchText;
        public string ?SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    FilterSkierowania();
                    OnPropertyChanged();
                }
            }
        }

        private string ?_activeFilterType = "All";
        public string ?ActiveFilterType
        {
            get => _activeFilterType;
            set
            {
                if (_activeFilterType != value)
                {
                    _activeFilterType = value;
                    FilterSkierowania();
                    OnPropertyChanged();
                }
            }
        }

        // NOWE: Wybrany filtr okresu
        private string ?_selectedDateFilter = "All";
        public string ?SelectedDateFilter
        {
            get => _selectedDateFilter;
            set
            {
                if (_selectedDateFilter != value)
                {
                    _selectedDateFilter = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCustomDateRangeVisible));
                    FilterSkierowania();
                }
            }
        }

        // NOWE: Daty dla "Wybrany okres"
        private DateTime? _filterDateFrom;
        public DateTime? FilterDateFrom
        {
            get => _filterDateFrom;
            set
            {
                if (_filterDateFrom != value)
                {
                    _filterDateFrom = value;
                    OnPropertyChanged();
                    if (SelectedDateFilter == "Wybrany okres")
                        FilterSkierowania();
                }
            }
        }

        private DateTime? _filterDateTo;
        public DateTime? FilterDateTo
        {
            get => _filterDateTo;
            set
            {
                if (_filterDateTo != value)
                {
                    _filterDateTo = value;
                    OnPropertyChanged();
                    if (SelectedDateFilter == "Wybrany okres")
                        FilterSkierowania();
                }
            }
        }

        // NOWE: Widoczność panelu dat niestandardowych
        public bool IsCustomDateRangeVisible => SelectedDateFilter == "Wybrany okres";

        public ICommand ?ClearSearchTextCommand { get; }
        public ICommand ?OpenSkierPacjWoborCommand { get; }
        public ICommand ?EditSkierowanieCommand { get; }
        public ICommand ?ApplyCustomDateFilterCommand { get; }

        public SkierowaniaViewModel()
        {
            OpenSkierPacjWoborCommand = new RelayCommand(_ => OpenSkierPacjWobor());
            ClearSearchTextCommand = new RelayCommand(_ => { SearchText = string.Empty; });
            EditSkierowanieCommand = new RelayCommand(EditSkierowanie);
            ApplyCustomDateFilterCommand = new RelayCommand(_ => FilterSkierowania());
            LoadFromDb();
        }

        private void EditSkierowanie(object? obj)
        {
            // obj expected to be SkierowanieDto or B_ID int
            int? bId = null;
            if (obj is SkierowanieDto dto && dto.B_ID.HasValue)
                bId = dto.B_ID.Value;
            else if (obj is int i)
                bId = i;

            if (!bId.HasValue)
                return;

            var db = new AccessDbContext();
            var full = db.GetSkierowanieById(bId.Value);
            if (full == null)
            {
                MessageBox.Show($"Nie znaleziono skierowania o ID {bId}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Map full record into SkierPacjentaViewModel
            var vm = new SkierPacjentaViewModel();
            vm.PatientFirstName = full.P_imie ?? string.Empty;
            vm.PatientLastName = full.P_nazwisko ?? string.Empty;
            vm.PatientPesel = full.P_pesel ?? string.Empty;
            vm.PatientGender = full.P_plec ?? string.Empty;
            vm.PatientBirthDate = full.P_data_urodzenia ?? DateTime.MinValue;
            vm.PatientJobTitle = full.P_zawod ?? string.Empty;
            vm.PatientPostalCode = full.P_Ades_kod ?? string.Empty;
            vm.PatientCity = full.P_Ades_miasto ?? string.Empty;
            vm.PatientStreet = full.P_Adres_ulica_numer ?? string.Empty;
            vm.PatientId = full.P_ID ?? 0;
            vm.Uwagi = full.P_Uwagi ?? string.Empty;

            vm.CompanyId = full.Firma_id ?? 0;
            vm.CompanyName = full.Firma_Nazwa ?? string.Empty;
            vm.CompanyPostalCode = full.Firma_Kod ?? string.Empty;
            vm.CompanyCity = full.Firma_Miejscowosc ?? string.Empty;
            vm.CompanyStreet = full.Firma_Ulica ?? string.Empty;

            // Skierowanie fields
            vm.ReferralDate = full.B_DataSkierowania;
            vm.TestType = full.B_TypBadania ?? string.Empty;
            vm.JobTitle = full.B_Stanowisko ?? string.Empty;
            vm.IsPhysical = full.B_czynnik_fizyczny ?? false;
            vm.PhysicalDescription = full.B_czynnik_fizyczny_opis ?? string.Empty;
            vm.IsDust = full.B_czynnik_pylowy ?? false;
            vm.DustDescription = full.B_czynnik_pylowy_opis ?? string.Empty;
            vm.IsChemical = full.B_czynnik_chemiczny ?? false;
            vm.ChemicalDescription = full.B_czynnik_chemiczny_opis ?? string.Empty;
            vm.IsBiological = full.B_czynnik_biologiczny ?? false;
            vm.BiologicalDescription = full.B_czynnik_biologiczny_opis ?? string.Empty;
            vm.IsOther = full.B_czynnik_inny ?? false;
            vm.OtherDescription = full.B_czynnik_inny_opis ?? string.Empty;
            vm.IsCertificate = full.B_Zaswiadczenie ?? false;
            vm.IsbookletSanepid = full.B_książeczka ?? false;
            vm.IsAnkieta = full.B_Ankieta ?? false;
            vm.IsNew = full.B_Nowe ?? true;
            // Set existing B_ID so SaveCommand will perform update
            vm.PatientSkierowanieId = full.B_ID ?? 0;
            vm.WydrukiVisibility = Visibility.Visible;

            // ✅ WYWOŁAJ METODĘ I SPRAWDŹ WYNIK
            vm.UpdateRejestrcjaDataFromDb();

            if (Application.Current.MainWindow?.DataContext is MainWindowViewModel mainVM)
            {
                mainVM.SkierowaniaWidok = vm;
            }
        }

        // ✅ POPRAWIONA METODA: Przełącza odpowiedni widok w zależności od aktywnej zakładki
        private void OpenSkierPacjWobor()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null) return;

                var mainVM = Application.Current.MainWindow?.DataContext as MainWindowViewModel;
                if (mainVM == null) return;

                // Sprawdź, która zakładka jest aktywna
                var skierowaniaTab = mainWindow.FindName("Karta_Badan") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                var nowaKartaBadanTab = mainWindow.FindName("NowaKartaBadan") as Syncfusion.Windows.Tools.Controls.TabItemExt;

                bool isSkierowaniaActive = skierowaniaTab?.IsSelected ?? false;
                bool isNowaKartaBadanActive = nowaKartaBadanTab?.IsSelected ?? false;

                // Utwórz widok listy pacjentów
                var listaVM = new SkierListaPacjentowViewModel();

                if (isSkierowaniaActive)
                {
                    // Zakładka "Skierowania" - użyj starego widoku SkierPacjentaView
                    mainVM.SkierowaniaWidok = listaVM;
                }
                else if (isNowaKartaBadanActive)
                {
                    // Zakładka "NowaKartaBadan" - użyj nowego widoku SkierNewPacjentaView
                    mainVM.NowaKartaBadanWidok = listaVM;
                }
                else
                {
                    // Domyślnie: przełącz na NowaKartaBadan i ustaw widok
                    if (nowaKartaBadanTab != null)
                    {
                        nowaKartaBadanTab.IsSelected = true;
                        mainVM.NowaKartaBadanWidok = listaVM;
                    }
                }
            }
            catch (System.Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"OpenSkierPacjWobor error: {ex}");
                MessageBox.Show($"Błąd podczas przełączania widoku:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFromDb()
        {
            var db = new AccessDbContext();
            var lista = db.GetSkierowania();
            Skierowania.Clear();
            foreach (var item in lista)
                Skierowania.Add(item);
            FilterSkierowania();
        }

        // Public method to refresh data from DB (can be invoked when tab receives focus)
        public void RefreshFromDb()
        {
            LoadFromDb();
            OnPropertyChanged(nameof(SkierowaniaFiltered));
        }

        private void FilterSkierowania()
        {
            SkierowaniaFiltered.Clear();
            var raw = SearchText?.Trim() ?? string.Empty;
            var text = raw.ToLower() ?? string.Empty;

            // 1. Oblicz zakres dat na podstawie wybranego filtru
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

            // 2. Filtruj skierowania
            foreach (var s in Skierowania)
            {
                // 2a. Filtr daty skierowania (B_DataSkierowania)
                bool dateMatch = true;
                if (dateFrom.HasValue && s.B_DataSkierowania.HasValue && s.B_DataSkierowania.Value < dateFrom.Value)
                    dateMatch = false;
                if (dateTo.HasValue && s.B_DataSkierowania.HasValue && s.B_DataSkierowania.Value > dateTo.Value)
                    dateMatch = false;

                if (!dateMatch)
                    continue;

                // ✅ 2b. SMART FILTER: Wykryj prefix #XXXX dla ID skierowania
                bool textMatch = false;
                if (string.IsNullOrEmpty(text))
                {
                    textMatch = true;
                }
                else if (raw.StartsWith("#") && raw.Length > 1)
                {
                    // Usuń prefix # i wiodące zera
                    var idText = raw.Substring(1).TrimStart('0');

                    if (int.TryParse(idText, out int searchId))
                    {
                        // System.Diagnostics.Debug.WriteLine($"SMART FILTER: Szukam ID skierowania (B_ID) = {searchId}");

                        // Szukaj DOKŁADNIE po B_ID (ID skierowania)
                        textMatch = (s.B_ID.HasValue && s.B_ID.Value == searchId);
                    }
                    else
                    {
                        // Niepoprawny format po #
                        textMatch = false;
                    }
                }
                else
                {
                    // ✅ STANDARD FILTER: Szukaj według wybranego typu
                    switch (ActiveFilterType)
                    {
                        case "Imię":
                            textMatch = (s.P_imie ?? "").ToLower().Contains(text) ||
                                       TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_imie ?? "", text);
                            break;
                        case "Nazwisko":
                            textMatch = (s.P_nazwisko ?? "").ToLower().Contains(text) ||
                                       TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_nazwisko ?? "", text);
                            break;
                        case "ID":
                            textMatch = s.Id.ToString().Contains(raw);
                            break;
                        case "PESEL":
                            textMatch = (s.P_pesel ?? "").ToLower().Contains(text);
                            break;
                        case "Firma":
                            textMatch = (s.Nazwa ?? "").ToLower().Contains(text) ||
                                       TextNormalizationHelper.ContainsIgnoringDiacritics(s.Nazwa ?? "", text);
                            break;
                        case "All":
                        default:
                            textMatch = (s.P_imie ?? "").ToLower().Contains(text)
                                || TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_imie ?? "", text)
                                || (s.P_nazwisko ?? "").ToLower().Contains(text)
                                || TextNormalizationHelper.ContainsIgnoringDiacritics(s.P_nazwisko ?? "", text)
                                || (s.P_pesel ?? "").ToLower().Contains(text)
                                || (s.Nazwa ?? "").ToLower().Contains(text)
                                || TextNormalizationHelper.ContainsIgnoringDiacritics(s.Nazwa ?? "", text)
                                || s.Id.ToString().Contains(raw);
                            break;
                    }
                }

                if (textMatch)
                    SkierowaniaFiltered.Add(s);
            }
        }
    }
}
