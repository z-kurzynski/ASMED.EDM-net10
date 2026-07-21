using ASMED.WPF.Helpers;
using ASMED.WPF.Models;
using ASMED.WPF.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Notifications.Wpf;
using Windows.Graphics.Printing.Workflow;
using ASMED.WPF.ViewModels; // for RelayCommand
using System.Linq;
using System.Collections.Generic;
using System.IO;
//using Syncfusion.UI.Xaml.Schedule;
using Syncfusion.UI.Xaml.Scheduler;


namespace ASMED.WPF.ViewModels.Skierowania
{
    public class SkierPacjentaViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private object _prawyWidokVM;
        public object PrawyWidokVM
        {
            get => _prawyWidokVM;
            set { _prawyWidokVM = value; OnPropertyChanged(); }
        }

        public ICommand ?NoweSkierowanieCommand { get; }
        public ICommand ?PrintKartaCommand { get; }
        public ICommand ?PrintOrzeczenieCommand { get; }
        public ICommand ?PrintAnkietaCommand { get; }
        public ICommand ?PrintZaswiadczenieCommand { get; }
        public ICommand ?PrintSanitarneComand { get; }

        // --- DANE PACJENTA I FIRMY ---
        private string?_patientFirstName = string.Empty;
        public string?PatientFirstName
        {
            get => _patientFirstName;
            set { if (_patientFirstName != value) { _patientFirstName = value; OnPropertyChanged(); } }
        }

        private string?_patientLastName = string.Empty;
        public string?PatientLastName
        {
            get => _patientLastName;
            set { if (_patientLastName != value) { _patientLastName = value; OnPropertyChanged(); } }
        }

        private string?_patientPesel = string.Empty;
        public string?PatientPesel
        {
            get => _patientPesel;
            set { if (_patientPesel != value) { _patientPesel = value; OnPropertyChanged(); } }
        }

        private string?_patientGender = string.Empty;
        public string?PatientGender
        {
            get => _patientGender;
            set { if (_patientGender != value) { _patientGender = value; OnPropertyChanged(); } }
        }

        private DateTime _patientBirthDate = DateTime.MinValue;
        public DateTime PatientBirthDate
        {
            get => _patientBirthDate;
            set { if (_patientBirthDate != value) { _patientBirthDate = value; OnPropertyChanged(); } }
        }

        private string?_patientJobTitle = string.Empty;
        public string?PatientJobTitle
        {
            get => _patientJobTitle;
            set { if (_patientJobTitle != value) { _patientJobTitle = value; OnPropertyChanged(); } }
        }

        private string?_patientPostalCode = string.Empty;
        public string?PatientPostalCode
        {
            get => _patientPostalCode;
            set { if (_patientPostalCode != value) { _patientPostalCode = value; OnPropertyChanged(); } }
        }

        private string?_patientCity = string.Empty;
        public string?PatientCity
        {
            get => _patientCity;
            set { if (_patientCity != value) { _patientCity = value; OnPropertyChanged(); } }
        }

        private string?_patientStreet = string.Empty;
        public string?PatientStreet
        {
            get => _patientStreet;
            set { if (_patientStreet != value) { _patientStreet = value; OnPropertyChanged(); } }
        }

        private int _patientId = 0;
        public int PatientId
        {
            get => _patientId;
            set { if (_patientId != value) { _patientId = value; OnPropertyChanged(); } }
        }

        private string?_uwagi = string.Empty;
        public string?Uwagi
        {
            get => _uwagi;
            set { if (_uwagi != value) { _uwagi = value; OnPropertyChanged(); } }
        }

        private int _companyId = 0;
        public int CompanyId
        {
            get => _companyId;
            set { if (_companyId != value) { _companyId = value; OnPropertyChanged(); } }
        }

        private string?_companyName = string.Empty;
        public string?CompanyName
        {
            get => _companyName;
            set { if (_companyName != value) { _companyName = value; OnPropertyChanged(); } }
        }

        private string?_companyPostalCode = string.Empty;
        public string?CompanyPostalCode
        {
            get => _companyPostalCode;
            set { if (_companyPostalCode != value) { _companyPostalCode = value; OnPropertyChanged(); } }
        }

        private string?_companyCity = string.Empty;
        public string?CompanyCity
        {
            get => _companyCity;
            set { if (_companyCity != value) { _companyCity = value; OnPropertyChanged(); } }
        }

        private string?_companyStreet = string.Empty;
        public string?CompanyStreet
        {
            get => _companyStreet;
            set { if (_companyStreet != value) { _companyStreet = value; OnPropertyChanged(); } }
        }

        // ✅ NOWE: Data rejestracji skierowania (B_RegistrationDate)
        private DateTime? _registrationDate;

        public DateTime? RegistrationDate
        {
            get => _registrationDate;
            set
            {
                if (_registrationDate != value)
                {
                    _registrationDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ?CloseCommand { get; }
        public ICommand ?NewCommand { get; }
        public ICommand ?EditPatientCommand { get; }

        // Save command
        public ICommand ?SaveCommand { get; }

        public ICommand ?BackToListCommand { get; }

        public ICommand ?OpenReferralFromPatientCommand { get; }

        // ✅ DODAJ tę właściwość w klasie SkierPacjentaViewModel

        //private Visibility _deleteButtonVisibility = Visibility.Visible;
        // ustaw na wartość z updateCanDeleteSkierowanie
        private Visibility _deleteButtonVisibility = Visibility.Visible;

        public Visibility DeleteButtonVisibility
        {

            get => _deleteButtonVisibility;
            set
            {
                if (_deleteButtonVisibility != value)
                {
                    _deleteButtonVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Sprawdza czy można usunąć skierowanie (brak powiązanego badania)
        /// </summary>
        public void UpdateCanDeleteSkierowanie()
        {
            try
            {
                {
                    if (this.PatientSkierowanieId <= 0)
                    {
                        DeleteButtonVisibility = Visibility.Collapsed;
                        return;
                    }

                    var db = new AccessDbContext();
                    bool hasBadanie = db.HasSkierowanieBadanie(this.PatientSkierowanieId);

                    DeleteButtonVisibility = hasBadanie ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateCanDeleteSkierowanie ERROR: {ex.Message}");
                DeleteButtonVisibility = Visibility.Collapsed;
            }
        }

        // ✅ DODAJ Command dla usuwania
        public ICommand ?DeleteSkierowanieCommand { get; }

        public SkierPacjentaViewModel()
        {

            // NoweSkierowanieCommand = new RelayCommand(_ => PrawyWidokVM = new SkierowaniaPacjentDodajViewModel());
            // Domyślnie lista skierowań
            //PrawyWidokVM = new SkierowaniaPacjentDodajViewModel();
            CloseCommand = new RelayCommand(_ => Close());
            NewCommand = new RelayCommand(_ => New());
            //EditPatientCommand = new RelayCommand(EditPatient2);
            EditPatientCommand = new RelayCommand(_ => EditPatient());

            SaveCommand = new RelayCommand(_ => SaveSkierowanie());
            PrintKartaCommand = new RelayCommand(_ => ExecutePrintKarta());
            PrintOrzeczenieCommand = new RelayCommand(_ => ExecutePrintOrzeczenie());
            PrintAnkietaCommand = new RelayCommand(_ => ExecutePrintAnkieta());
            PrintZaswiadczenieCommand = new RelayCommand(_ => ExecutePrintZaswiadczenie());
            PrintSanitarneComand = new RelayCommand(_ => ExecutePrintSanitarne());
            BackToListCommand = new RelayCommand(_ => BackToList());
            OpenReferralFromPatientCommand = new RelayCommand(_ => OpenReferralFromPatient());

            DeleteSkierowanieCommand = new RelayCommand(
                execute: _ => DeleteSkierowanie());
            // ❌ USUNIĘTO: canExecute: _ => CanDeleteSkierowanie

            RefreshAppointmentsFromDb();
        }


        private void Close()
        {
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel mainVm)
                mainVm.SkierowaniaWidok = new SkierowaniaViewModel();
        }
        private void New()
        {
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel mainVm)
                mainVm.SkierowaniaWidok = new SkierListaPacjentowViewModel();
        }

        private void EditPatient()
        {
            // Pobierz dane pacjenta z ViewModelu (np. SelectedPatient lub inne właściwości)
            if (PatientId > 0)
            {
                var db = new AccessDbContext();
                var rec = db.GetPacjentById(PatientId);
                // Jeśli rekord istnieje, otwórz widok edycji pacjenta
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


                    // Mapowanie dla pól z autouzupełnianiem
                    vm.WybraneImie = vm.Imie;
                    vm.WybraneNazwisko = vm.Nazwisko;
                    vm.WybranyZawod = vm.Zawod;
                    vm.WybranaUlica = vm.UlicaNumerDomu;
                    vm.WybraneMiasto = vm.Miejscowosc;

                    // Ustaw WybranaFirma po ID (jeśli lista firm już załadowana)
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
                        // Jeśli nie znaleziono po ID, ustaw nazwę firmy z rekordu (string)
                        vm.FrazaFirma = rec.FirmaNazwa;
                    }

                    if (mainVM != null)
                    {

                        mainVM.SkierowaniaWidok = vm; // vm to SkierPacjentaEditViewModel
                        // mainVM.SkierowaniaWidok = new SkierPacjentaEditView { DataContext = vm };
                    }
                }
            }
        }

        private void ExecutePrintKarta()
        { 
            if (this.PatientSkierowanieId > 0)
            {
                ShowPdfPreviewForRecord(this.PatientSkierowanieId);
            }
            else
            {
                MessageBox.Show("Brak zapisanego skierowania do wydruku. Najpierw zapisz skierowanie.",
                    "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecutePrintZaswiadczenie()
        {
            if (this.PatientSkierowanieId > 0)
                ShowPdfPreviewForTemplate(this.PatientSkierowanieId, "ASMED__Karta badania Orzeczenie.pdf");
            else
                MessageBox.Show("Brak zapisanego skierowania do wydruku. Najpierw zapisz skierowanie.",
                    "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecutePrintSanitarne()
        {
            if (this.PatientSkierowanieId > 0)
                ShowPdfPreviewForTemplate(this.PatientSkierowanieId, "ASMED__Sanitarne.pdf");
            else
                MessageBox.Show("Brak zapisanego skierowania do wydruku. Najpierw zapisz skierowanie.",
                    "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecutePrintOrzeczenie()
        {
            if (this.PatientSkierowanieId > 0)
                ShowPdfPreviewForTemplate(this.PatientSkierowanieId, "ASMED_Orzeczenie.pdf");
            else
                MessageBox.Show("Brak zapisanego skierowania do wydruku. Najpierw zapisz skierowanie.",
                    "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecutePrintAnkieta()
        {
            if (this.PatientSkierowanieId > 0)
                ShowPdfPreviewForTemplate(this.PatientSkierowanieId, "ASMED_Ankieta.pdf");
            else
                MessageBox.Show("Brak zapisanego skierowania do wydruku. Najpierw zapisz skierowanie.",
                    "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        /*
                private void ExecutePrintZaswiadczenie()
                {
                    if (this.PatientSkierowanieId > 0)
                        ShowPdfPreviewForTemplate(this.PatientSkierowanieId, "Zaswiadczenie.pdf");
                    else
                        MessageBox.Show("Brak zapisanego skierowania do wydruku. Najpierw zapisz skierowanie.", 
                            "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                }
        */
        // druk skierowań

        public int PatientSkierowanieId { get; set; }

        // EditButton visibility
        private Visibility _EditButtonVisibility = Visibility.Visible;
        public Visibility EditButtonVisibility
        {
            get => _EditButtonVisibility;
            set { if (_EditButtonVisibility != value) { _EditButtonVisibility = value; OnPropertyChanged(); } }
        }

        // Rejestracja button visibility
        private Visibility _wydrukiVisibility = Visibility.Hidden;
        public Visibility WydrukiVisibility
        {
            get => _wydrukiVisibility;
            set
            {
                if (_wydrukiVisibility != value) { _wydrukiVisibility = value; OnPropertyChanged(); }
                if (value == Visibility.Visible) EditButtonVisibility = Visibility.Hidden; else EditButtonVisibility = Visibility.Visible;
            }
        }
        private void BackToList()
        {
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel mainVm)
                mainVm.SkierowaniaWidok = new SkierowaniaViewModel();
        }

        // Stanowisko pracy
        public string?JobTitle { get; set; }

        // Czynniki szkodliwe i opisy
        public bool IsPhysical { get; set; }
        public string?PhysicalDescription { get; set; }
        public bool IsDust { get; set; }
        public string?DustDescription { get; set; }
        public bool IsChemical { get; set; }
        public string?ChemicalDescription { get; set; }
        public bool IsBiological { get; set; }
        public string?BiologicalDescription { get; set; }
        public bool IsOther { get; set; }
        public string?OtherDescription { get; set; }
        public bool IsSanepid { get; set; }
        public string?SanepidDescription { get; set; }
        public bool IsNew { get; set; } = true; // Nowe skierowanie
        public bool IsCard { get; set; } = false; // Karta badań
        public bool IsRegistration { get; set; } = false; // Rejestracja skierowania
        public bool IsTest { get; set; } = false; // Badanie
        public DateTime? TestDate { get; set; } // Data badania
        public bool Sanepid { get; set; } // Czy jest czynnik sanepidowski

        // Pozostałe
        public string?TestType { get; set; }
        public DateTime? ReferralDate { get; set; }
        public string?Comments { get; set; }

        // extra fields bound in XAML
        public bool IsCertificate { get; set; } // B_Zaswiadczenie
        public bool IsbookletSanepid { get; set; } // B_książeczka
        public bool IsAnkieta { get; set; } // B_Ankieta
        public bool IsPolling { get; set; } // ankieta flag (bound in XAML)

        //public ICommand SaveCommand { get; }
        public ICommand ?CancelCommand { get; }

        public SkierPacjentaViewModel(
            string patientFirstName,
            string patientLastName,
            string patientPesel,
            string patientGender,
            DateTime patientBirthDate,
            string patientJobTitle,
            string patientPostalCode,
            string patientCity,
            string patientStreet,
            int patientId,
            string uwagi,
            int companyId,
            string companyName,
            string companyPostalCode,
            string companyCity,
            string companyStreet,
            int patientSkierowanieId) : this()
        {
            PatientFirstName = patientFirstName ?? string.Empty;
            PatientLastName = patientLastName ?? string.Empty;
            PatientPesel = patientPesel ?? string.Empty;
            PatientGender = patientGender ?? string.Empty;
            PatientBirthDate = patientBirthDate;
            PatientJobTitle = patientJobTitle ?? string.Empty;
            PatientPostalCode = patientPostalCode ?? string.Empty;
            PatientCity = patientCity ?? string.Empty;
            PatientStreet = patientStreet ?? string.Empty;
            PatientId = patientId;
            Uwagi = uwagi ?? string.Empty;
            CompanyId = companyId;
            CompanyName = companyName ?? string.Empty;
            CompanyPostalCode = companyPostalCode ?? string.Empty;
            CompanyCity = companyCity ?? string.Empty;
            CompanyStreet = companyStreet ?? string.Empty;

            PatientSkierowanieId = patientSkierowanieId;


            // ❌ ZAKOMENTUJ TO - powoduje zawieszenie podczas ładowania
            //  UpdateCanDeleteSkierowanie();
            /*
            // ✅ Zamiast tego użyj asynchronicznego ładowania
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal, // ✅ WYŻSZY PRIORYTET!
                new Action(() => 
                {
                    UpdateCanDeleteSkierowanie();
                    CommandManager.InvalidateRequerySuggested(); // ✅ Wymuś odświeżenie UI
                })); */

            UpdateRejestrcjaDataFromDb();

            // ✅ DODANE: Załaduj datę rejestracji skierowania z bazy
            LoadRegistrationDateFromDb();
        }

        private bool _isGroupW;
        public bool IsGroupW
        {
            get => _isGroupW;
            set
            {
                if (_isGroupW != value)
                {
                    _isGroupW = value;
                    if (value)
                        TestType = "W";
                    OnPropertyChanged(nameof(IsGroupW));
                    OnPropertyChanged(nameof(TestType));
                }
            }
        }

        private bool _isGroupO;
        public bool IsGroupO
        {
            get => _isGroupO;
            set
            {
                if (_isGroupO != value)
                {
                    _isGroupO = value;
                    if (value)
                        TestType = "O";
                    OnPropertyChanged(nameof(IsGroupO));
                    OnPropertyChanged(nameof(TestType));
                }
            }
        }

        private bool _isGroupK;
        public bool IsGroupK
        {
            get => _isGroupK;
            set
            {
                if (_isGroupK != value)
                {
                    _isGroupK = value;
                    if (value)
                        TestType = "K";
                    OnPropertyChanged(nameof(IsGroupK));
                    OnPropertyChanged(nameof(TestType));
                }
            }
        }

        private void SaveSkierowanie()
        {
            try
            {
                var rec = new AccessDbContext.SkierowanieRecord
                {
                    PacjentId = this.PatientId == 0 ? (int?)null : this.PatientId,
                    FirmaId = this.CompanyId == 0 ? (int?)null : this.CompanyId,
                    BadanieId = null,
                    DataSkierowania = this.ReferralDate,
                    TypBadania = this.TestType,
                    Stanowisko = string.IsNullOrWhiteSpace(this.JobTitle) ? this.PatientJobTitle : this.JobTitle,
                    RegistrationDate = DateTime.Now,
                    CzynnikFizyczny = this.IsPhysical,
                    CzynnikFizycznyOpis = this.PhysicalDescription,
                    CzynnikPylowy = this.IsDust,
                    CzynnikPylowyOpis = this.DustDescription,
                    CzynnikChemiczny = this.IsChemical,
                    CzynnikChemicznyOpis = this.ChemicalDescription,
                    CzynnikBiologiczny = this.IsBiological,
                    CzynnikBiologicznyOpis = this.BiologicalDescription,
                    CzynnikInny = this.IsOther,
                    CzynnikInnyOpis = this.OtherDescription,
                    CzynnikSanepid = this.IsSanepid,
                    CzynnikSanepidOpis = this.SanepidDescription,
                    Zaswiadczenie = this.IsCertificate,
                    Ksiazeczka = this.IsbookletSanepid || this.IsCard,
                    Ankieta = this.IsAnkieta || this.IsPolling,
                    Nowe = this.IsNew,
                    Activ = true
                };

                var db = new AccessDbContext();

                if (this.PatientSkierowanieId > 0)
                {
                    // Update existing GetSkierowanieById
                    bool ok = db.UpdateSkierowanie(this.PatientSkierowanieId, rec);
                    if (ok)
                        if (ok)
                        {
                            WydrukiVisibility = Visibility.Visible;
                            EditButtonVisibility = Visibility.Hidden;
                            NotificationHelper.ShowInfo("Skierowanie zaktualizowane", $"ID = {this.PatientSkierowanieId}");

                        }
                        else
                        {
                            MessageBox.Show("Nie udało się zaktualizować skierowania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                }
                else
                {
                    int newId = db.AddSkierowanie(rec);
                    if (newId > 0)
                    {
                        PatientSkierowanieId = newId;
                        WydrukiVisibility = Visibility.Visible;
                        EditButtonVisibility = Visibility.Hidden;
                        NotificationHelper.ShowInfo("Skierowanie zapisane", $"ID = {newId}");

                    }
                    else
                    {
                        MessageBox.Show("Nie udało się zapisać skierowania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisu skierowania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Wyświetlanie podglądu PDF karty badań
        private void ShowPdfPreviewForRecord(int bId)
        {
            // default karta
            ShowPdfPreviewForTemplate(bId, "ASMED__Karta badania profilaktycznego.pdf");
        }

        private void ShowPdfPreviewForTemplate(int bId, string templateFileName)
        {
            LoadRegistrationDateFromDb();
            UpdateRejestrcjaDataFromDb();

            try
            {
                var templateFile = Path.Combine("A:", "formularz", templateFileName);
                var outputDir = Path.Combine("A:", "Formuarz_Druki");
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                var safeLastName = string.IsNullOrWhiteSpace(this.PatientLastName) ? "patient" : this.PatientLastName;
                var outputFile = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(templateFileName)}_{bId}_{safeLastName}.pdf");

                var values = MapujPolaFormularza();
                var filled = PdfFormService.FillForm(templateFile, values, outputFile);
                if (filled != null)
                {
                    var w = new ASMED.WPF.Views.PdfPreviewWindow();
                    w.LoadFile(filled);
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

        // Public wrappers so other classes/views can invoke the preview if needed
        public void ShowPdfPreview(int bId)
        {
            ShowPdfPreviewForRecord(bId);
        }

        public void ShowPdfPreview(int bId, string templateFileName)
        {
            ShowPdfPreviewForTemplate(bId, templateFileName);
        }

        // Mapowanie pól formularza PDF karta badan  (z MapujPolaFormularza)
        private Dictionary<string, string> MapujPolaFormularza()
        {
            var map = new Dictionary<string, string>();

            // ✅ ASMED - Firma
            map["A_Nazwa"] = "Niepubliczny ZOZ ASMED S.C \n Kurzyńska Stefania  Kurzyński Zbigniew\n 04-028 Warszawa \n AL. Stanów Zjednoczonyc 51 pok. 204 \n tel. 22 81 44 02 \n NIP 113 83 31 776  Nr Księgi 7582";

            // ✅ Typ badania
            if (!string.IsNullOrEmpty(this.TestType))
            {
                if (this.TestType.ToUpper() == "W")
                    map["Typ_Bad"] = "wstępne";
                else if (this.TestType.ToUpper() == "O")
                    map["Typ_Bad"] = "okresowe";
                else if (this.TestType.ToUpper() == "K")
                    map["Typ_Bad"] = "kontrolne";
            }


            // ✅ Basic fields
            map["B_TypBadania"] = this.TestType ?? string.Empty;
            map["B_Nazwisko_B_Imię"] = $"{this.PatientLastName} {this.PatientFirstName}".Trim();
            map["B_Firma"] = this.CompanyName ?? string.Empty;
            map["B_zawód"] = !string.IsNullOrWhiteSpace(this.JobTitle)
                ? this.JobTitle
                : this.PatientJobTitle ?? string.Empty;

            // ✅ Data urodzenia (ddMMyyyy)
            var dataUrodzenia = (this.PatientBirthDate != DateTime.MinValue)
                ? this.PatientBirthDate.ToString("ddMMyyyy")
                : "";
            for (int i = 0; i < 8; i++)
                map[$"du_{i + 1}"] = dataUrodzenia.Length > i ? dataUrodzenia[i].ToString() : string.Empty;

            // ✅ Data skierowania - POPRAWIONE: używaj null zamiast DateTime.Now
            if (this.ReferralDate.HasValue)
            {
                map["B_DataSkierowania"] = this.ReferralDate.Value.ToString("dd.MM.yyyy");

                var registrationDate = this.ReferralDate.Value.ToString("ddMMyyyy");

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
            if (this.RegistrationDate.HasValue)
            {
                map["B_RegistrationDate"] = this.RegistrationDate.Value.ToString("dd.MM.yyyy");

                var registrationDateStr = this.RegistrationDate.Value.ToString("ddMMyyyy");

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

            //data wizyty / badania

            if (!string.IsNullOrWhiteSpace(this.RejestrcjaData))
            {
                map["Data_wizyty"] = this.RejestrcjaData;
            }
            else
            {
                map["Data_wizyty"] = string.Empty;
            }

            // ✅ PESEL
            var pesel = this.PatientPesel ?? string.Empty;
            for (int i = 1; i <= 11; i++)
                map[$"P_{i}"] = pesel.Length >= i ? pesel[i - 1].ToString() : string.Empty;
            map["PESEL"] = pesel;

            // ✅ Płeć
            map["M"] = (this.PatientGender?.ToUpper() == "M") ? "X" : string.Empty;
            map["K"] = (this.PatientGender?.ToUpper() == "K") ? "X" : string.Empty;
            map["Plec"] = this.PatientGender ?? string.Empty;

            // ✅ Adres pacjenta
            map["P_adr"] = $"{this.PatientStreet} {this.PatientCity}".Trim();
            var kod = this.PatientPostalCode ?? string.Empty;
            var kodParts = kod.Split('-');
            map["kp_1"] = kodParts.Length > 0 ? kodParts[0] : string.Empty;
            map["kp_2"] = kodParts.Length > 1 ? kodParts[1] : string.Empty;
            map["Full_Adress"] = $"{this.PatientPostalCode} {this.PatientCity}, {this.PatientStreet}".Trim();

            // ✅ Adres firmy
            map["F_adr"] = $"{this.CompanyStreet} {this.CompanyCity}".Trim();
            var kodF = this.CompanyPostalCode ?? string.Empty;
            var kodFParts = kodF.Split('-');
            map["KF_1"] = kodFParts.Length > 0 ? kodFParts[0] : string.Empty;
            map["KF_2"] = kodFParts.Length > 1 ? kodFParts[1] : string.Empty;
            map["Full_Adress_Firma"] = $"{this.CompanyPostalCode} {this.CompanyCity}, {this.CompanyStreet}".Trim();

            // ✅ Czynniki szkodliwe
            map["SB_Tak"] = "X";
            bool szkodliwy = this.IsPhysical || this.IsDust || this.IsChemical || this.IsBiological || this.IsOther;
            map["SZ_Tak"] = szkodliwy ? "X" : string.Empty;
            map["SZ_NIE"] = !szkodliwy ? "X" : string.Empty;

            var opisy = new[] {
                this.PhysicalDescription,
                this.DustDescription,
                this.ChemicalDescription,
                this.BiologicalDescription,
                this.OtherDescription
            };
            bool czyOpis = opisy.Any(x => !string.IsNullOrWhiteSpace(x));
            map["IS_TAK"] = czyOpis ? "X" : string.Empty;
            map["IS_NIE"] = !czyOpis ? "X" : string.Empty;
            map["IS_OPIS"] = string.Join(" - ", opisy.Where(x => !string.IsNullOrWhiteSpace(x)));

            // ✅ Nagłówki/stopki
            map["header"] = $"({this.PatientSkierowanieId}) - {(this.ReferralDate ?? DateTime.Now):dd.MM.yyyy}".Trim();
            map["header4"] = $"{this.PatientSkierowanieId} - {(this.ReferralDate ?? DateTime.Now):dd.MM.yyyy} {this.PatientFirstName} {this.PatientLastName}".Trim();
            map["footer"] = "ASMED - Niepubliczny Zakład Opieki Zdrowotnej04-028 Warszawa Al. Stanów Zjednoczonych51 pok.204 tel.22814402 NIP1138331776 Nr Księgi7582";
            map["header2"] = $"ID: {this.PatientSkierowanieId} / {this.PatientId}".Trim();
            map["header3"] = $"Karta nr: {this.PatientSkierowanieId} ".Trim();

            int skierowanieId = this.PatientSkierowanieId;
            map["header3_barcode"] = (skierowanieId > 0)
                ? $"#{skierowanieId.ToString().PadLeft(4, '0')}"
                : string.Empty;
            map["PatientLastName"] = this.PatientLastName ?? string.Empty;

            return map;
        }
        // Koniec drukowania Karty badań

        // --- KALENDARZ I TERMINY ---

        private void Schedule_AppointmentEditorClosing(object sender, AppointmentEditorClosingEventArgs e)
        {
            if (!(e.Action.HasFlag(AppointmentEditorAction.Add) || e.Action.HasFlag(AppointmentEditorAction.Edit))
                || e.Appointment is not ScheduleAppointment sched)
            {
                return;
            }
            var vm = this.DataContext as SkierPacjentaViewModel;
            var rec = new ASMED.WPF.Helpers.AccessDbContext.RejestracjaRecord
            {
                R_Data = sched.StartTime.Date, // tylko data
                RStatus = "Rejestracja",
                R_S_ID = vm?.PatientSkierowanieId is int skierId ? skierId : 0, // Poprawka rzutowania
                R_GG_MM = sched.StartTime, // pełny DateTime, godzina i minuta
                R_Uwagi = sched.Notes,
                R_P_ID = vm?.PatientId ?? 0
            };
            var db = new ASMED.WPF.Helpers.AccessDbContext();
            db.AddRejestracja(rec);
            vm?.RefreshAppointmentsFromDb();
        }

        public ObservableCollection<ScheduleAppointment> Events { get; set; } = new();

        public void AddAppointment(DateTime start, DateTime end, string? firstName, string? lastName, string? company, string? referralId)
        {
            Events.Add(new ScheduleAppointment
            {
                StartTime = start,
                EndTime = end,
                Subject = $"{firstName} {lastName} ({company})",
                Notes = $" {referralId} {firstName} {lastName} ({company}"
            });
        }
        private string?_referralId = string.Empty;
        public string?ReferralId
        {
            get => _referralId;
            set { if (_referralId != value) { _referralId = value; OnPropertyChanged(); } }
        }


        public ICommand ?AddAppointmentCommand => new RelayCommand<DateTime?>(AddAppointmentFromSelectedSlot);

        public SkierPacjentaViewModel DataContext { get; private set; }

        private void AddAppointmentFromSelectedSlot(DateTime? selectedDate)
        {
            if (selectedDate == null) return;
            AddAppointment(selectedDate.Value, selectedDate.Value.AddMinutes(5), PatientLastName, PatientFirstName, CompanyName, ReferralId);

            var vm = this;
            var db = new ASMED.WPF.Helpers.AccessDbContext();
            var all = db.GetRejestracje();

            var existing = all.FirstOrDefault(r => r.R_S_ID == (vm?.PatientSkierowanieId is int skierId2 ? skierId2 : 0));

            var rec = new ASMED.WPF.Helpers.AccessDbContext.RejestracjaRecord
            {
                R_B_ID = existing?.R_B_ID,
                R_Data = selectedDate.Value.Date,
                RStatus = "Rejestracja",
                R_S_ID = vm?.PatientSkierowanieId is int skierId3 ? skierId3 : 0,
                R_GG_MM = selectedDate.Value,
                R_Subject = $"{PatientLastName} {PatientFirstName} ( {CompanyName} )",
                R_Uwagi = $" {PatientSkierowanieId} {PatientFirstName} {PatientLastName} ( {CompanyName})",
                R_P_ID = vm?.PatientId ?? 0
            };

            if (existing != null)
            {
                db.UpdateRejestracja(existing.R_ID.Value, rec);
            }
            else
            {
                db.AddRejestracja(rec);
            }

            vm?.RefreshAppointmentsFromDb();

            // ✅ DODANE: Odśwież datę rejestracji w polu "Data Rej.:"

            vm?.UpdateRejestrcjaDataFromDb();
        }

        // ✅ PUBLICZNA metoda (zmieniono z private na public)
        public void RefreshAppointmentsFromDb()
        {
            var db = new AccessDbContext();
            var list = db.GetRejestracje();
            Events.Clear();
            foreach (var rec in list)
            {
                if (!rec.R_Data.HasValue || !rec.R_GG_MM.HasValue)
                    continue;
                var start = new DateTime(
                    rec.R_Data.Value.Year,
                    rec.R_Data.Value.Month,
                    rec.R_Data.Value.Day,
                    rec.R_GG_MM.Value.Hour,
                    rec.R_GG_MM.Value.Minute,
                    0);
                Events.Add(new ScheduleAppointment
                {
                    StartTime = start,
                    EndTime = start.AddMinutes(5),
                    Subject = rec.R_Uwagi,
                    Notes = rec.R_Uwagi
                });
            }
        }

        private void OpenReferralFromPatient()
        {
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel mainVm)
            {
                var listaVm = new SkierListaPacjentowViewModel();
                listaVm.OpenReferral(this.PatientId); // przekazanie P_ID
                mainVm.SkierowaniaWidok = listaVm;
            }
        }

        // ✅ DODANE: Property dla pola Data_Rejestracji
        private string?_rejestrcjaData = string.Empty;
        public string?RejestrcjaData
        {
            get => _rejestrcjaData;
            set
            {
                if (_rejestrcjaData != value)
                {
                    _rejestrcjaData = value;
                    OnPropertyChanged();
                    //System.Diagnostics.Debug.WriteLine($"✅ RejestrcjaData SET: {value}");
                }
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Ładuje datę rejestracji skierowania (B_RegistrationDate) z bazy
        /// </summary>
        private void LoadRegistrationDateFromDb()
        {
            try
            {
                if (this.PatientSkierowanieId <= 0)
                {
                    RegistrationDate = null;
                    return;
                }

                var db = new AccessDbContext();
                var skierowanie = db.GetSkierowanieById(this.PatientSkierowanieId);

                if (skierowanie != null)
                {
                    this.RegistrationDate = skierowanie.B_RegistrationDate;
                    // System.Diagnostics.Debug.WriteLine($"✅ LoadRegistrationDateFromDb: B_ID={this.PatientSkierowanieId}, RegistrationDate={this.RegistrationDate}");
                }
                else
                {
                    RegistrationDate = null;
                    // System.Diagnostics.Debug.WriteLine($"⚠️ LoadRegistrationDateFromDb: Nie znaleziono skierowania B_ID={this.PatientSkierowanieId}");
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LoadRegistrationDateFromDb ERROR: {ex.Message}");
                RegistrationDate = null;
            }
        }

        /// <summary>
        /// ✅ NOWA PUBLICZNA METODA: Pobiera datę rejestracji WIZYTY z bazy (R_Rejestracja)
        /// </summary>
        public void UpdateRejestrcjaDataFromDb()
        {
            try
            {
                if (this.PatientSkierowanieId <= 0)
                {
                    RejestrcjaData = string.Empty;
                    return;
                }

                var db = new AccessDbContext();
                var all = db.GetRejestracje();

                var rejestracja = all.FirstOrDefault(r => r.R_S_ID == this.PatientSkierowanieId);

                if (rejestracja != null && rejestracja.R_GG_MM.HasValue)
                {
                    RejestrcjaData = rejestracja.R_GG_MM.Value.ToString("dd.MM.yyyy HH:mm");
                    //System.Diagnostics.Debug.WriteLine($"✅ UpdateRejestrcjaDataFromDb: {RejestrcjaData}");
                }
                else if (rejestracja != null && rejestracja.R_Data.HasValue)
                {
                    RejestrcjaData = rejestracja.R_Data.Value.ToString("dd.MM.yyyy");
                }
                else
                {
                    RejestrcjaData = string.Empty;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ UpdateRejestrcjaDataFromDb ERROR: {ex.Message}");
                RejestrcjaData = string.Empty;
            }
        }

        /// <summary>
        /// Usuwa kartę badań po potwierdzeniu użytkownika
        /// </summary>
        private void DeleteSkierowanie()
        {
            if (this.PatientSkierowanieId <= 0)
            {
                MessageBox.Show("Brak karty badań do usunięcia.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ DODANA WALIDACJA: Sprawdź czy nie ma badania
            var db = new AccessDbContext();
            if (db.HasSkierowanieBadanie(this.PatientSkierowanieId))
            {
                MessageBox.Show(
                    "Nie można usunąć karty badań.\nIstnieje powiązane badanie.\n\nNajpierw usuń badanie.",
                    "Błąd usuwania",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Potwierdź z użytkownikiem
            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć kartę badań #{this.PatientSkierowanieId}?\n\n" +
                $"Pacjent: {this.PatientFirstName} {this.PatientLastName}\n" +
                $"Firma: {this.CompanyName}\n\n" +
                "Ta operacja jest nieodwracalna!",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // Usuń z bazy
            bool success = db.DeleteSkierowanie(this.PatientSkierowanieId);

            if (success)
            {
                Close();
                // Zamknij widok po usunięciu
            }
        }
    }
}
// Koniec pliku SkierPacjentaViewModel.cs
