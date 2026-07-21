using ASMED.WPF.Helpers;
using ASMED.WPF.Models;
using ASMED.WPF.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels.Skierowania
{
    public class SkierListaPacjentowViewModel
    {
        public ObservableCollection<string> FilterTypes { get; } = new() { "All", "Imię", "Nazwisko", "PESEL", "Firma" };
        public ObservableCollection<Pacjent> Pacjenci { get; set; }
        public ObservableCollection<Pacjent> PacjenciFiltered { get; set; } = new();

        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    FilterPacjenci();
                }
            }
        }
        private string? _searchText;

        public string ?ActiveFilterType
        {
            get => _activeFilterType;
            set
            {
                if (_activeFilterType != value)
                {
                    _activeFilterType = value;
                    FilterPacjenci();
                }
            }
        }
        private string ?_activeFilterType = "All";

        public ICommand ?ClearSearchTextCommand { get; }
        public ICommand ?EditPatientCommand { get; }
        public ICommand ?OpenReferralCommand { get; } // ✅ POPRAWIONE: Dodaj kartę badań
        public ICommand ?RegisterCommand { get; }
        public ICommand ?EditPatientNewCommand { get; }
        public ICommand ?DeleteCommand { get; }
        public ICommand ?OpenListaSkierowanCommand { get; }
        public ICommand ?OpenEmailImportCommand { get; } // ✅ NOWE: Import PDF z Email
        public ICommand ?OpenHistoriaCommand { get; } // ✅ NOWE: Historia badań pacjenta

        public SkierListaPacjentowViewModel()
        {
            Pacjenci = new ObservableCollection<Pacjent>();
            EditPatientCommand = new RelayCommand(EditPatient);
            OpenReferralCommand = new RelayCommand(OpenReferral);
            EditPatientNewCommand = new RelayCommand(EditPatientNew);
            OpenListaSkierowanCommand = new RelayCommand(_ => OpenListaSkierowan());
            OpenEmailImportCommand = new RelayCommand(_ => OpenEmailImport()); // ✅ NOWE
            OpenHistoriaCommand = new RelayCommand(OpenHistoria); // ✅ NOWE

            ClearSearchTextCommand = new RelayCommand(_ => { SearchText = string.Empty; });
            LoadPacjenciFromDb();
        }

        // ✅ 1. EDYCJA PACJENTA - Pozostaje na tej samej zakładce (NowaKartaBadan)
        private void EditPatient(object? obj)
        {
            if (obj is Pacjent pacjent && pacjent.P_ID > 0)
            {
                var db = new AccessDbContext();
                var rec = db.GetPacjentById(pacjent.P_ID);

                if (rec != null)
                {
                    var mainVM = Application.Current.MainWindow?.DataContext as MainWindowViewModel;
                    var vm = new SkierPacjentaEditViewModel(mainVM)
                    {
                        Imie = rec.Imie,
                        Nazwisko = rec.Nazwisko,
                        PESEL = rec.PESEL,
                        BrakPESEL = rec.BrakPESEL,
                        Plec = rec.Plec,
                        Zawod = rec.Zawod,
                        comments = rec.Uwagi,
                        UlicaNumerDomu = rec.UlicaNumerDomu,
                        KodPocztowy = rec.Kod,
                        ID = rec.ID,
                        P_firma_id = rec.FirmaId,
                        FrazaFirma = rec.FirmaNazwa
                    };

                    vm.WybraneImie = vm.Imie;
                    vm.WybraneNazwisko = vm.Nazwisko;
                    vm.WybranyZawod = vm.Zawod;
                    vm.WybranaUlica = vm.UlicaNumerDomu;
                    vm.WybraneMiasto = vm.Miejscowosc;

                    if (vm.P_firma_id.HasValue && vm.Firmy != null && vm.Firmy.Count > 0)
                    {
                        var firma = vm.Firmy.FirstOrDefault(f => f.Id == vm.P_firma_id.Value);
                        if (firma != null)
                        {
                            vm.WybranaFirma = firma;
                            vm.FrazaFirma = firma.Name;
                        }
                    }
                    else if (!string.IsNullOrEmpty(vm.FrazaFirma))
                    {
                        vm.FrazaFirma = rec.FirmaNazwa;
                    }

                    if (mainVM != null)
                    {
                        mainVM.NowaKartaBadanWidok = vm; // ✅ ZMIENIONE
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ 2. DODAJ KARTĘ BADAŃ (przycisk 📝 w DataGrid) - Z WALIDACJĄ otwartych kart
        // ═══════════════════════════════════════════════════════
        private void OpenReferral(object? obj)
        {
            if (obj is not Pacjent pacjent || pacjent.P_ID <= 0)
            {
                NotificationHelper.ShowWarning("Wybierz pacjenta z listy.");
                return;
            }

            try
            {
                // System.Diagnostics.Debug.WriteLine($"🔍 OpenReferral: Sprawdzam otwarte karty dla P_ID={pacjent.P_ID}");

                // ✅ KROK 1: Sprawdź czy pacjent ma otwarte karty badań
                var otwarteKarty = SprawdzOtwarteKartyBadan(pacjent.P_ID);

                if (otwarteKarty.Count > 0)
                {
                    // ✅ KROK 2A: Pacjent ma otwarte karty → Pokaż dialog wyboru
                    // System.Diagnostics.Debug.WriteLine($"⚠️ Znaleziono {otwarteKarty.Count} otwartych kart badań");

                    var dialogVM = new ViewModels.Dialogs.OtwarteKartyBadanDialogViewModel(
                        imieNazwisko: $"{pacjent.FirstName} {pacjent.LastName}",
                        pesel: pacjent.PESEL ?? "",
                        firma: pacjent.Company ?? "",
                        otwarteKarty: otwarteKarty
                    );

                    var dialog = new Views.Dialogs.OtwarteKartyBadanDialog(dialogVM)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        if (dialogVM.Result == ViewModels.Dialogs.OtwarteKartyBadanDialogViewModel.DialogResult.EdytujKarte)
                        {
                            // ✅ Edytuj wybraną kartę
                            if (dialogVM.WybraneB_ID.HasValue)
                            {
                                EdytujIstniejacaKarte(pacjent.P_ID, dialogVM.WybraneB_ID.Value);
                            }
                        }
                        else if (dialogVM.Result == ViewModels.Dialogs.OtwarteKartyBadanDialogViewModel.DialogResult.NowaKarta)
                        {
                            // ✅ Utwórz nową kartę
                            UtworzNowaKarte(pacjent.P_ID);
                        }
                    }
                }
                else
                {
                    // ✅ KROK 2B: Brak otwartych kart → Od razu otwórz nową kartę
                    // System.Diagnostics.Debug.WriteLine("✅ Brak otwartych kart - tworzę nową");
                    UtworzNowaKarte(pacjent.P_ID);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd OpenReferral: {ex.Message}");
                MessageBox.Show($"Błąd otwierania karty badań:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Sprawdza otwarte (niezamknięte) karty badań pacjenta
        /// </summary>
        private ObservableCollection<OtwartaKartaBadanDto> SprawdzOtwarteKartyBadan(int pacjentId)
        {
            var karty = new ObservableCollection<OtwartaKartaBadanDto>();

            try
            {
                var db = new Helpers.AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT
    B_Skierowania.B_ID,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    Rejestracja.R_Data,
    Rejestracja.R_Status
FROM
    B_Skierowania
    LEFT JOIN Rejestracja ON B_Skierowania.B_ID = Rejestracja.R_S_ID
WHERE
    B_Skierowania.B_Pacjent_ID = ?
    AND B_Skierowania.B_Badanie_ID IS NULL
ORDER BY
    B_Skierowania.B_DataSkierowania DESC";

                        cmd.Parameters.AddWithValue("@id", pacjentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var karta = new OtwartaKartaBadanDto
                                {
                                    B_ID = reader["B_ID"] is int bid ? bid :
                                           int.TryParse(reader["B_ID"]?.ToString(), out var bid2) ? bid2 : 0,

                                    B_DataSkierowania = reader["B_DataSkierowania"] is DateTime ds ? ds :
                                                        DateTime.TryParse(reader["B_DataSkierowania"]?.ToString(), out var ds2) ? ds2 : (DateTime?)null,

                                    B_TypBadania = reader["B_TypBadania"]?.ToString() ?? "",

                                    R_Data = reader["R_Data"] is DateTime rd ? rd :
                                            DateTime.TryParse(reader["R_Data"]?.ToString(), out var rd2) ? rd2 : (DateTime?)null,

                                    R_Status = reader["R_Status"]?.ToString() ?? ""
                                };

                                karty.Add(karta);
                            }
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"✅ Znaleziono {karty.Count} otwartych kart dla P_ID={pacjentId}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd SprawdzOtwarteKartyBadan: {ex.Message}");
            }

            return karty;
        }

        /// <summary>
        /// ✅ POPRAWIONA: Edytuje istniejącą kartę badań (B_ID) - UŻYWA db.GetSkierowanieById()
        /// </summary>
        private void EdytujIstniejacaKarte(int pacjentId, int bId)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"✏️ Edytowanie karty: P_ID={pacjentId}, B_ID={bId}");

                // ✅ KROK 1: Przełącz na zakładkę "Nowa Karta Badań" (NowaKartaBadan)
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var nowaKartaBadanTab = mainWindow.FindName("NowaKartaBadan") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                    if (nowaKartaBadanTab != null)
                    {
                        nowaKartaBadanTab.IsSelected = true;
                        // System.Diagnostics.Debug.WriteLine("✅ Przełączono na zakładkę 'Nowa Karta Badań'");
                    }
                }

                // ✅ KROK 2: Pobierz pełne dane skierowania z bazy (UŻYWAMY GOTOWEJ METODY!)
                var db = new AccessDbContext();
                var full = db.GetSkierowanieById(bId);

                if (full == null)
                {
                    MessageBox.Show($"Nie znaleziono karty badań: B_ID={bId}",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ KROK 3: Mapuj dane do SkierNewPacjentaViewModel
                var mainVM = Application.Current.MainWindow?.DataContext as MainWindowViewModel;
                var vm = new SkierNewPacjentaViewModel();

                // Dane pacjenta
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

                // Dane firmy
                vm.CompanyId = full.Firma_id ?? 0;
                vm.CompanyName = full.Firma_Nazwa ?? string.Empty;
                vm.CompanyPostalCode = full.Firma_Kod ?? string.Empty;
                vm.CompanyCity = full.Firma_Miejscowosc ?? string.Empty;
                vm.CompanyStreet = full.Firma_Ulica ?? string.Empty;

                // Dane skierowania (karty badań)
                vm.PatientSkierowanieId = full.B_ID ?? 0;
                vm.ReferralDate = full.B_DataSkierowania;
                vm.TestType = full.B_TypBadania ?? string.Empty;
                vm.JobTitle = full.B_Stanowisko ?? string.Empty;

                // Czynniki szkodliwe
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

                // Dokumenty
                vm.IsCertificate = full.B_Zaswiadczenie ?? false;
                vm.IsbookletSanepid = full.B_książeczka ?? false;
                vm.IsAnkieta = full.B_Ankieta ?? false;
                vm.IsNew = full.B_Nowe ?? true;

                // ✅ RadioButtony dla typu badania
                vm.IsGroupW = vm.TestType == "W";
                vm.IsGroupO = vm.TestType == "O";
                vm.IsGroupK = vm.TestType == "K";

                // ✅ Wydruki dostępne (karta już istnieje)
                vm.WydrukiVisibility = Visibility.Visible;
                vm.EditButtonVisibility = Visibility.Hidden;

                // ✅ Odśwież datę rejestracji z bazy
                vm.UpdateRejestrcjaDataFromDb();

                // ✅ KROK 4: Ustaw widok w MainViewModel
                if (mainVM != null)
                {
                    mainVM.NowaKartaBadanWidok = vm;
                }

                // System.Diagnostics.Debug.WriteLine($"✅ Otwarto edycję karty: B_ID={bId}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd EdytujIstniejacaKarte: {ex.Message}");
                MessageBox.Show($"Błąd edycji karty badań:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Tworzy nową kartę badań dla pacjenta
        /// </summary>
        private void UtworzNowaKarte(int pacjentId)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"➕ Tworzenie nowej karty dla P_ID={pacjentId}");

                var db = new Helpers.AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_pesel,
    P_Pacjent.P_brak,
    P_Pacjent.P_płeć,
    P_Pacjent.P_data_urodzenia,
    P_Pacjent.P_zawód,
    P_Pacjent.P_Ades_kod,
    P_Pacjent.P_Ades_miasto,
    P_Pacjent.P_Adres_ulica_numer,
    P_Pacjent.P_ID,
    P_Pacjent.P_Uwagi,
    P_Pacjent.P_Firma_id,
    Firma.Nazwa,
    Firma.Kod,
    Firma.Miejscowosc,
    Firma.Ulica
FROM
    Firma
    INNER JOIN P_Pacjent ON Firma.id = P_Pacjent.P_Firma_id
WHERE
    P_Pacjent.P_ID = ?";
                        cmd.Parameters.AddWithValue("@id", pacjentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var mainVM = Application.Current.MainWindow?.DataContext as MainWindowViewModel;
                                var vm = new SkierNewPacjentaViewModel();

                                // ✅ Dane pacjenta
                                vm.PatientFirstName = reader["P_imie"]?.ToString() ?? string.Empty;
                                vm.PatientLastName = reader["P_nazwisko"]?.ToString() ?? string.Empty;
                                vm.PatientPesel = reader["P_pesel"]?.ToString() ?? string.Empty;
                                vm.PatientGender = reader["P_płeć"]?.ToString() ?? string.Empty;

                                if (reader["P_data_urodzenia"] is DateTime dt)
                                    vm.PatientBirthDate = dt;
                                else if (DateTime.TryParse(reader["P_data_urodzenia"]?.ToString(), out var dt2))
                                    vm.PatientBirthDate = dt2;

                                vm.PatientJobTitle = reader["P_zawód"]?.ToString() ?? string.Empty;
                                vm.PatientPostalCode = reader["P_Ades_kod"]?.ToString() ?? string.Empty;
                                vm.PatientCity = reader["P_Ades_miasto"]?.ToString() ?? string.Empty;
                                vm.PatientStreet = reader["P_Adres_ulica_numer"]?.ToString() ?? string.Empty;
                                vm.PatientId = reader["P_ID"] is int pid ? pid :
                                              int.TryParse(reader["P_ID"]?.ToString(), out var pid2) ? pid2 : 0;
                                vm.Uwagi = reader["P_Uwagi"]?.ToString() ?? string.Empty;

                                // ✅ Dane firmy
                                vm.CompanyId = reader["P_Firma_id"] is int fid ? fid :
                                              int.TryParse(reader["P_Firma_id"]?.ToString(), out var fid2) ? fid2 : 0;
                                vm.CompanyName = reader["Nazwa"]?.ToString() ?? string.Empty;
                                vm.CompanyPostalCode = reader["Kod"]?.ToString() ?? string.Empty;
                                vm.CompanyCity = reader["Miejscowosc"]?.ToString() ?? string.Empty;
                                vm.CompanyStreet = reader["Ulica"]?.ToString() ?? string.Empty;

                                // ✅ TRYB NOWEJ KARTY (ustawienia domyślne)
                                vm.PatientSkierowanieId = 0;
                                vm.ReferralDate = DateTime.Today;
                                vm.WydrukiVisibility = Visibility.Hidden;
                                vm.EditButtonVisibility = Visibility.Visible;

                                if (mainVM != null)
                                {
                                    mainVM.NowaKartaBadanWidok = vm;
                                }

                                // System.Diagnostics.Debug.WriteLine($"✅ Utworzono nową kartę dla P_ID={pacjentId}");
                            }
                            else
                            {
                                MessageBox.Show($"Nie znaleziono pacjenta o ID: {pacjentId}",
                                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd UtworzNowaKarte: {ex.Message}");
                throw;
            }
        }

        // ✅ 3. DODAJ PACJENTA (przycisk "➕ Dodaj Pacjenta") - Pozostaje na tej samej zakładce (NowaKartaBadan)
        private void EditPatientNew(object? obj)
        {
            var mainVM = Application.Current.MainWindow.DataContext as MainWindowViewModel;
            var vm = new SkierPacjentaEditViewModel(mainVM);
            if (mainVM != null)
            {
                mainVM.NowaKartaBadanWidok = new SkierPacjentaEditView { DataContext = vm }; // ✅ ZMIENIONE
            }
        }

        private void LoadPacjenciFromDb()
        {
            var db = new Helpers.AccessDbHelper();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // ✅ NOWE: JOIN z B_Skierowania aby policzyć karty badań
                    cmd.CommandText = @"
                SELECT 
                    P.P_ID,
                    P.P_Imie, 
                    P.P_Nazwisko, 
                    P.P_PESEL, 
                    P.P_Firma,
                    COUNT(B.B_ID) AS LiczbaKartBadan
                FROM 
                    P_Pacjent AS P
                    LEFT JOIN B_Skierowania AS B ON P.P_ID = B.B_Pacjent_ID
                GROUP BY 
                    P.P_ID, P.P_Imie, P.P_Nazwisko, P.P_PESEL, P.P_Firma
                ORDER BY 
                    P.P_Nazwisko, P.P_Imie";

                    using (var reader = cmd.ExecuteReader())
                    {
                        int lineNumber = 1;
                        while (reader.Read())
                        {
                            Pacjenci.Add(new Pacjent
                            {
                                LineNumber = lineNumber++,
                                P_ID = reader["P_ID"] is int id ? id : int.TryParse(reader["P_ID"].ToString(), out var id2) ? id2 : 0,
                                FirstName = reader["P_Imie"].ToString(),
                                LastName = reader["P_Nazwisko"].ToString(),
                                PESEL = reader["P_PESEL"].ToString(),
                                Company = reader["P_Firma"].ToString(),
                                LiczbaKartBadan = reader["LiczbaKartBadan"] is int count ? count :
                                                 int.TryParse(reader["LiczbaKartBadan"]?.ToString(), out var count2) ? count2 : 0 // ✅ NOWE
                            });
                        }
                    }
                }
            }
            FilterPacjenci();
        }

        /// <summary>
        /// Odświeża listę pacjentów z bazy danych
        /// </summary>
        public void RefreshList()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("RefreshList: Rozpoczynam odświeżanie...");

                // Wyczyść obecną listę
                Pacjenci.Clear();
                PacjenciFiltered.Clear();

                // Przeładuj z bazy
                LoadPacjenciFromDb();

                // System.Diagnostics.Debug.WriteLine($"RefreshList: Załadowano {Pacjenci.Count} pacjentów");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"RefreshList ERROR: {ex.Message}");
                throw;
            }
        }

        private void FilterPacjenci()
        {
            PacjenciFiltered.Clear();
            var text = SearchText?.Trim().ToLower() ?? string.Empty;

            // ✅ DODANE: Normalizacja tekstu wyszukiwania (usuń polskie znaki)
            var normalizedSearch = TextNormalizationHelper.RemovePolishDiacritics(text);

            foreach (var p in Pacjenci)
            {
                bool match = false;
                if (string.IsNullOrEmpty(text))
                {
                    match = true;
                }
                else
                {
                    switch (ActiveFilterType)
                    {
                        case "Imię":
                            // ✅ Porównuj zarówno oryginał jak i znormalizowane
                            match = (p.FirstName ?? "").ToLower().Contains(text) ||
                                   TextNormalizationHelper.ContainsIgnoringDiacritics(p.FirstName ?? "", text);
                            break;
                        case "Nazwisko":
                            // ✅ Porównuj zarówno oryginał jak i znormalizowane
                            match = (p.LastName ?? "").ToLower().Contains(text) ||
                                   TextNormalizationHelper.ContainsIgnoringDiacritics(p.LastName ?? "", text);
                            break;
                        case "PESEL":
                            match = (p.PESEL ?? "").ToLower().Contains(text);
                            break;
                        case "Firma":
                            match = (p.Company ?? "").ToLower().Contains(text) ||
                                   TextNormalizationHelper.ContainsIgnoringDiacritics(p.Company ?? "", text);
                            break;
                        case "All":
                        default:
                            // ✅ Wyszukiwanie we wszystkich polach z normalizacją
                            match = (p.FirstName ?? "").ToLower().Contains(text)
                                || TextNormalizationHelper.ContainsIgnoringDiacritics(p.FirstName ?? "", text)
                                || (p.LastName ?? "").ToLower().Contains(text)
                                || TextNormalizationHelper.ContainsIgnoringDiacritics(p.LastName ?? "", text)
                                || (p.PESEL ?? "").ToLower().Contains(text)
                                || (p.Company ?? "").ToLower().Contains(text)
                                || TextNormalizationHelper.ContainsIgnoringDiacritics(p.Company ?? "", text)
                                || p.P_ID.ToString().Contains(text); // ✅ DODANE: Wyszukiwanie po ID
                            break;
                    }
                }
                if (match)
                    PacjenciFiltered.Add(p);
            }
        }

        internal void OpenReferral(int patientId)
        {
            throw new NotImplementedException();
        }

        // ✅ 1. ZOBACZ SKIEROWANIE (przycisk "Zobacz Skierowanie") - Otwiera DIALOG (jak było)
        private void OpenListaSkierowan()
        {
            try
            {
                var listaSkierowanWindow = new ListaSkierowanWindow
                {
                    Owner = Application.Current.MainWindow,
                    ShowInTaskbar = false
                };
                listaSkierowanWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd otwierania okna Lista Skierowań:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ NOWE: Import PDF z Email
        private void OpenEmailImport()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("OpenEmailImport: Otwieranie okna importu email...");

                // Oblicz datę (domyślnie 7 dni wstecz)
                var dateFrom = DateTime.Now.AddDays(-7);

                // Otwórz okno OutlookImportWindow
                var emailImportWindow = new OutlookImportWindow(
                    pstPath: string.Empty, // Ścieżka PST z konfiguracji
                    exportPath: string.Empty, // Ścieżka eksportu z konfiguracji
                    dateFrom: dateFrom
                )
                {
                    Owner = Application.Current.MainWindow,
                    ShowInTaskbar = false
                };

                emailImportWindow.Show();

                // System.Diagnostics.Debug.WriteLine("OpenEmailImport: Okno otwarte pomyślnie");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"OpenEmailImport ERROR: {ex.Message}");
                MessageBox.Show($"Błąd otwierania okna Import PDF z Email:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ NOWE: Otwórz dialog historii badań pacjenta
        private void OpenHistoria(object? obj)
        {
            try
            {
                if (obj is not Pacjent pacjent || pacjent.P_ID <= 0)
                {
                    MessageBox.Show("Nie wybrano pacjenta lub ID pacjenta jest nieprawidłowe.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // System.Diagnostics.Debug.WriteLine($"OpenHistoria: Otwieranie historii dla pacjenta ID={pacjent.P_ID}");

                // Otwórz dialog historii
                var historiaDialog = new PacjentHistoriaDialog(
                    pacjentId: pacjent.P_ID,
                    imie: pacjent.FirstName ?? "",
                    nazwisko: pacjent.LastName ?? "",
                    pesel: pacjent.PESEL ?? "",
                    firma: pacjent.Company ?? ""
                )
                {
                    Owner = Application.Current.MainWindow,
                    ShowInTaskbar = false
                };

                historiaDialog.ShowDialog();

                // System.Diagnostics.Debug.WriteLine("OpenHistoria: Dialog zamknięty");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"OpenHistoria ERROR: {ex.Message}");
                MessageBox.Show($"Błąd otwierania historii badań:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
// end of file
