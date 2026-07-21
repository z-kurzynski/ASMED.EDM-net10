using System;
using System.Configuration;
using System.IO;
using System.Windows;

namespace ASMED.WPF.Helpers
{
    public static class DatabaseConfiguration
    {
        private const string CONFIG_FILE = @"A:\dbconfig.txt";
        private const string LOG_FILE = @"A:\dbconfig_log.txt";

        private static string _dbPathProdukcyjna = @"A:\MedPracyView.accdb";
        private static string _dbPathTestowa = @"A:\MedPracyView_TEST.accdb";
        private static string _aktywnaDbTyp = "Produkcyjna";
        private static string _archivePath = @"A:\Archiwa";

        public static event EventHandler? DatabaseChanged;

        public static string DbPathProdukcyjna
        {
            get => _dbPathProdukcyjna;
            set
            {
                _dbPathProdukcyjna = value;
                SaveConfiguration();
                DatabaseChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static string DbPathTestowa
        {
            get => _dbPathTestowa;
            set
            {
                _dbPathTestowa = value;
                SaveConfiguration();
                DatabaseChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static string AktywnaDbTyp
        {
            get => _aktywnaDbTyp;
            set
            {
                _aktywnaDbTyp = value;
                SaveConfiguration();
                DatabaseChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static string ArchivePath
        {
            get => _archivePath;
            set
            {
                _archivePath = value;
                SaveConfiguration();
            }
        }

        public static string UzywanaDbPath
        {
            get
            {
                return _aktywnaDbTyp == "Produkcyjna" ? _dbPathProdukcyjna : _dbPathTestowa;
            }
        }

        static DatabaseConfiguration()
        {
            LoadConfiguration();
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(LOG_FILE, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
            }
            catch
            {
                // Ignoruj błędy logowania
            }
        }

        private static void LoadConfiguration()
        {
            try
            {
                Log($"LoadConfiguration START - Plik: {CONFIG_FILE}");
                Log($"Plik istnieje: {File.Exists(CONFIG_FILE)}");

                if (File.Exists(CONFIG_FILE))
                {
                    var lines = File.ReadAllLines(CONFIG_FILE);
                    Log($"Odczytano {lines.Length} linii");

                    if (lines.Length >= 3)
                    {
                        _dbPathProdukcyjna = lines[0] ?? @"A:\MedPracyView.accdb";
                        _dbPathTestowa = lines[1] ?? @"A:\MedPracyView_TEST.accdb";
                        _aktywnaDbTyp = lines[2] ?? "Produkcyjna";

                        if (lines.Length >= 4)
                        {
                            _archivePath = lines[3] ?? @"A:\Archiwa";
                        }

                        Log($"Załadowano: Prod={_dbPathProdukcyjna}, Test={_dbPathTestowa}, Aktywna={_aktywnaDbTyp}, Archive={_archivePath}");
                    }
                    else
                    {
                        Log($"Za mało linii w pliku ({lines.Length}), używam wartości domyślnych");
                    }
                }
                else
                {
                    Log("Plik nie istnieje - używam wartości domyślnych");
                }

                Log("LoadConfiguration SUCCESS");
            }
            catch (Exception ex)
            {
                Log($"LoadConfiguration ERROR: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void SaveConfiguration()
        {
            try
            {
                Log($"SaveConfiguration START - Prod={_dbPathProdukcyjna}, Test={_dbPathTestowa}, Aktywna={_aktywnaDbTyp}, Archive={_archivePath}");

                File.WriteAllLines(CONFIG_FILE, new[]
                {
                    _dbPathProdukcyjna ?? "",
                    _dbPathTestowa ?? "",
                    _aktywnaDbTyp ?? "Produkcyjna",
                    _archivePath ?? @"A:\Archiwa"
                });

                Log("SaveConfiguration SUCCESS");

                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Konfiguracja zapisana!", "DatabaseConfiguration", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"SaveConfiguration ERROR: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");

                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Błąd zapisu konfiguracji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        }

        public static void UstawBazeProdukcyjna()
        {
            AktywnaDbTyp = "Produkcyjna";
        }

        public static void UstawBazeTestowa()
        {
            AktywnaDbTyp = "Testowa";
        }
    }
}
