using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ASMED.WPF.Helpers;
using Microsoft.Win32;

namespace ASMED.WPF.ViewModels
{
    public class KonfiguracjaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string?_dbPathProdukcyjna;
        public string?DbPathProdukcyjna
        {
            get => _dbPathProdukcyjna;
            set { _dbPathProdukcyjna = value; OnPropertyChanged(); }
        }

        private string?_dbPathTestowa;
        public string?DbPathTestowa
        {
            get => _dbPathTestowa;
            set { _dbPathTestowa = value; OnPropertyChanged(); }
        }

        private bool _czyBazaProdukcyjna;
        public bool CzyBazaProdukcyjna
        {
            get => _czyBazaProdukcyjna;
            set
            {
                _czyBazaProdukcyjna = value;
                OnPropertyChanged();
                if (value)
                {
                    DatabaseConfiguration.UstawBazeProdukcyjna();
                    AktualnaBazaDanych = $"Aktywna: Produkcyjna - {DatabaseConfiguration.UzywanaDbPath}";
                }
            }
        }

        private bool _czyBazaTestowa;
        public bool CzyBazaTestowa
        {
            get => _czyBazaTestowa;
            set
            {
                _czyBazaTestowa = value;
                OnPropertyChanged();
                if (value)
                {
                    DatabaseConfiguration.UstawBazeTestowa();
                    AktualnaBazaDanych = $"Aktywna: Testowa - {DatabaseConfiguration.UzywanaDbPath}";
                }
            }
        }

        private string?_aktualnaBazaDanych;
        public string?AktualnaBazaDanych
        {
            get => _aktualnaBazaDanych;
            set { _aktualnaBazaDanych = value; OnPropertyChanged(); }
        }

        private string?_archivePath;
        public string?ArchivePath
        {
            get => _archivePath;
            set { _archivePath = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ArchiveInfo> _archivesList;
        public ObservableCollection<ArchiveInfo> ArchivesList
        {
            get => _archivesList;
            set { _archivesList = value; OnPropertyChanged(); }
        }

        private ArchiveInfo _selectedArchive;
        public ArchiveInfo SelectedArchive
        {
            get => _selectedArchive;
            set { _selectedArchive = value; OnPropertyChanged(); }
        }

        private int _iloscSkierowan;
        public int IloscSkierowan
        {
            get => _iloscSkierowan;
            set { _iloscSkierowan = value; OnPropertyChanged(); }
        }

        private int _iloscBadan;
        public int IloscBadan
        {
            get => _iloscBadan;
            set { _iloscBadan = value; OnPropertyChanged(); }
        }

        private int _iloscFaktur;
        public int IloscFaktur
        {
            get => _iloscFaktur;
            set { _iloscFaktur = value; OnPropertyChanged(); }
        }

        private int _iloscListDoFaktur;
        public int IloscListDoFaktur
        {
            get => _iloscListDoFaktur;
            set { _iloscListDoFaktur = value; OnPropertyChanged(); }
        }

        private int _iloscPacjentow;
        public int IloscPacjentow
        {
            get => _iloscPacjentow;
            set { _iloscPacjentow = value; OnPropertyChanged(); }
        }

        // ── DatabaseInitializer ─────────────────────────────────────────────
        private string? _dbInitStatus;
        public string? DbInitStatus
        {
            get => _dbInitStatus;
            set { _dbInitStatus = value; OnPropertyChanged(); }
        }

        private string? _dbInitDetails;
        public string? DbInitDetails
        {
            get => _dbInitDetails;
            set { _dbInitDetails = value; OnPropertyChanged(); }
        }

        private bool _dbInitSuccess;
        public bool DbInitSuccess
        {
            get => _dbInitSuccess;
            set { _dbInitSuccess = value; OnPropertyChanged(); }
        }

        private string?_outlookPstPath;
        public string?OutlookPstPath
        {
            get => _outlookPstPath;
            set
            {
                _outlookPstPath = value;
                OnPropertyChanged();
            }
        }

        private string?_pdfExportPath;
        public string?PdfExportPath
        {
            get => _pdfExportPath;
            set
            {
                _pdfExportPath = value;
                OnPropertyChanged();
            }
        }

        public ICommand ?ZapiszUstawieniaDbCommand { get; }
        public ICommand ?TestPolaczeniaCommand { get; }
        public ICommand ?CreateBackupCommand { get; }
        public ICommand ?RestoreBackupCommand { get; }
        public ICommand ?RefreshArchivesCommand { get; }
        public ICommand ?OdswiezStatystykiCommand { get; }
        public ICommand ?BrowseOutlookPstCommand { get; }
        public ICommand ?BrowsePdfExportCommand { get; }
        public ICommand ?SearchOutlookCommand { get; }
        public ICommand ?InitializeDatabaseCommand { get; }

        public KonfiguracjaViewModel()
        {
            ZapiszUstawieniaDbCommand = new RelayCommand(_ => ZapiszUstawieniaDb());
            TestPolaczeniaCommand = new RelayCommand(_ => TestPolaczenia());
            CreateBackupCommand = new RelayCommand(_ => WykonajKopieZapasowa());
            RestoreBackupCommand = new RelayCommand(_ => PrzywrocKopieZapasowa());
            RefreshArchivesCommand = new RelayCommand(_ => OdswiezListeArchiwow());
            OdswiezStatystykiCommand = new RelayCommand(_ => OdswiezStatystyki());
            BrowseOutlookPstCommand = new RelayCommand(_ => BrowseOutlookPst());
            BrowsePdfExportCommand = new RelayCommand(_ => BrowsePdfExport());
            SearchOutlookCommand = new RelayCommand(_ => SearchOutlook());
            InitializeDatabaseCommand = new RelayCommand(_ => InitializeDatabase());

            ArchivesList = new ObservableCollection<ArchiveInfo>();

            LoadDbSettings();
            LoadOutlookSettings();
            OdswiezListeArchiwow();
            OdswiezStatystyki();
        }

        private void LoadDbSettings()
        {
            _dbPathProdukcyjna = DatabaseConfiguration.DbPathProdukcyjna;
            _dbPathTestowa = DatabaseConfiguration.DbPathTestowa;
            _archivePath = DatabaseConfiguration.ArchivePath;

            _czyBazaProdukcyjna = DatabaseConfiguration.AktywnaDbTyp == "Produkcyjna";
            _czyBazaTestowa = DatabaseConfiguration.AktywnaDbTyp == "Testowa";

            OnPropertyChanged(nameof(DbPathProdukcyjna));
            OnPropertyChanged(nameof(DbPathTestowa));
            OnPropertyChanged(nameof(ArchivePath));
            OnPropertyChanged(nameof(CzyBazaProdukcyjna));
            OnPropertyChanged(nameof(CzyBazaTestowa));

            AktualnaBazaDanych = $"Aktywna: {DatabaseConfiguration.AktywnaDbTyp} - {DatabaseConfiguration.UzywanaDbPath}";
        }

        private void LoadOutlookSettings()
        {
            // ✅ LOAD from OutlookConfiguration
            _outlookPstPath = OutlookConfiguration.PstPath;
            _pdfExportPath = OutlookConfiguration.PdfExportPath;

            OnPropertyChanged(nameof(OutlookPstPath));
            OnPropertyChanged(nameof(PdfExportPath));

            // System.Diagnostics.Debug.WriteLine($"LoadOutlookSettings: PST='{_outlookPstPath}', Export='{_pdfExportPath}'");
        }

        private void ZapiszUstawieniaDb()
        {
            DatabaseConfiguration.DbPathProdukcyjna = DbPathProdukcyjna ?? string.Empty;
            DatabaseConfiguration.DbPathTestowa = DbPathTestowa ?? string.Empty;
            DatabaseConfiguration.ArchivePath = ArchivePath ?? string.Empty;

            // ✅ SAVE Outlook settings to OutlookConfiguration
            OutlookConfiguration.PstPath = OutlookPstPath ?? string.Empty;
            OutlookConfiguration.PdfExportPath = PdfExportPath ?? string.Empty;

            AktualnaBazaDanych = $"Zapisano! Aktywna: {DatabaseConfiguration.AktywnaDbTyp} - {DatabaseConfiguration.UzywanaDbPath}";
            MessageBox.Show("Ustawienia zostaly zapisane!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TestPolaczenia()
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                dbHelper.TestConnection();
                AktualnaBazaDanych = $"✓ Polaczenie udane: {DatabaseConfiguration.UzywanaDbPath}";
                MessageBox.Show($"Polaczenie z baza danych zakonczone sukcesem!\n\n{DatabaseConfiguration.UzywanaDbPath}",
                    "Test polacenia", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AktualnaBazaDanych = $"✗ Blad polacenia: {ex.Message}";
                MessageBox.Show($"Nie mozna polaczyc sie z baza danych:\n\n{ex.Message}",
                    "Blad polacenia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeDatabase()
        {
            DbInitStatus = "⏳ Inicjalizacja...⁠";
            DbInitDetails = string.Empty;
            DbInitSuccess = false;

            try
            {
                var initializer = new DatabaseInitializer();
                initializer.Initialize();

                DbInitSuccess = initializer.Success;
                DbInitStatus  = initializer.StatusMessage;
                DbInitDetails = initializer.Details;
            }
            catch (Exception ex)
            {
                DbInitSuccess = false;
                DbInitStatus  = $"❌ Błąd: {ex.Message}";
                DbInitDetails = ex.ToString();
            }
        }

        private void BrowseOutlookPst()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Pliki Outlook PST|*.pst|Wszystkie pliki|*.*",
                Title = "Wybierz plik PST Outlook"
            };

            if (dialog.ShowDialog() == true)
            {
                OutlookPstPath = dialog.FileName;
            }
        }

        private void BrowsePdfExport()
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Wybierz folder eksportu PDF"
            };

            if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                PdfExportPath = dialog.FileName;
            }
        }

        private void SearchOutlook()
        {
            try
            {
                // ✅ DIAGNOSTYKA: Sprawdź datę systemową
                // System.Diagnostics.Debug.WriteLine($"SearchOutlook: DateTime.Now = {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // ✅ WALIDACJA PRZEZ OutlookConfiguration
                var (isValid, errorMessage) = OutlookConfiguration.ValidatePaths();

                if (!isValid)
                {
                    MessageBox.Show(
                        $"{errorMessage}\n\n" +
                        "Prosze sprawdzic ustawienia w sekcji 'Konfiguracja importu z Outlook'.",
                        "Blad konfiguracji",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // ✅ UTWÓRZ FOLDER EKSPORTU JEŚLI NIE ISTNIEJE
                if (!Directory.Exists(OutlookConfiguration.PdfExportPath))
                {
                    var result = MessageBox.Show(
                        $"Folder eksportu nie istnieje:\n\n{OutlookConfiguration.PdfExportPath}\n\n" +
                        "Czy chcesz utworzyc ten folder?",
                        "Folder nie existeje",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Directory.CreateDirectory(OutlookConfiguration.PdfExportPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Nie mozna utworzyc folderu:\n\n{ex.Message}",
                                "Blad",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                // ✅ OBLICZ DATĘ OD (7 dni wstecz)
                DateTime? dateFrom = DateTime.Now.AddDays(-7);

                // ✅ DIAGNOSTYKA: Wyświetl obliczoną datę
                // System.Diagnostics.Debug.WriteLine($"SearchOutlook: Calculated dateFrom = {dateFrom:yyyy-MM-dd}");
                // System.Diagnostics.Debug.WriteLine("=== SearchOutlook: Creating OutlookImportWindow ===");

                // Parametry konstruktora będą IGNOROWANE - okno odczyta z OutlookConfiguration
                var importWindow = new Views.OutlookImportWindow(null, null, dateFrom)
                {
                    Owner = Application.Current.MainWindow,
                    ShowInTaskbar = false
                };

                // System.Diagnostics.Debug.WriteLine("OutlookImportWindow created successfully");
                importWindow.Show();
                // System.Diagnostics.Debug.WriteLine("OutlookImportWindow.Show() completed");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Exception in SearchOutlook: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Blad wyszukiwania w Outlook:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WykonajKopieZapasowa()
        {
            try
            {
                string backupPath = ArchiveManager.CreateBackup();
                MessageBox.Show($"Kopia zapasowa utworzona pomyslnie!\n\n{backupPath}",
                    "Kopia zapasowa", MessageBoxButton.OK, MessageBoxImage.Information);
                OdswiezListeArchiwow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Blad tworzenia kopii zapasowej:\n\n{ex.Message}",
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrzywrocKopieZapasowa()
        {
            if (SelectedArchive == null)
            {
                MessageBox.Show("Wybierz kopie zapasowa do przywrocenia.",
                    "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz przywrocic baze danych z kopii?\n\n{SelectedArchive.DisplayName}\n\nBiezaca baza zostanie automatycznie zarchiwizowana przed przywroceniem.",
                "Potwierdzenie przywracania",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ArchiveManager.RestoreBackup(SelectedArchive.FullPath);
                    MessageBox.Show("Baza danych przywrocona pomyslnie!\n\nBiezaca baza zostala zarchiwizowana przed przywroceniem.",
                        "Przywracanie", MessageBoxButton.OK, MessageBoxImage.Information);
                    OdswiezListeArchiwow();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Blad przywracania kopii zapasowej:\n\n{ex.Message}",
                        "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OdswiezListeArchiwow()
        {
            try
            {
                var archives = ArchiveManager.GetArchivesList(5);
                ArchivesList.Clear();
                foreach (var archive in archives)
                {
                    ArchivesList.Add(archive);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Blad odswiezania listy archiwow:\n\n{ex.Message}",
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OdswiezStatystyki()
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM B_Skierowania";
                    var result = cmd.ExecuteScalar();
                    IloscSkierowan = result != null ? Convert.ToInt32(result) : 0;
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Badanie";
                    var result = cmd.ExecuteScalar();
                    IloscBadan = result != null ? Convert.ToInt32(result) : 0;
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Faktura";
                    var result = cmd.ExecuteScalar();
                    IloscFaktur = result != null ? Convert.ToInt32(result) : 0;
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM ListyBadan";
                    var result = cmd.ExecuteScalar();
                    IloscListDoFaktur = result != null ? Convert.ToInt32(result) : 0;
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM P_Pacjent WHERE P_activ = True";
                    var result = cmd.ExecuteScalar();
                    IloscPacjentow = result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Blad pobierania statystyk: {ex.Message}");
                MessageBox.Show($"Blad pobierania statystyk:\n\n{ex.Message}",
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
