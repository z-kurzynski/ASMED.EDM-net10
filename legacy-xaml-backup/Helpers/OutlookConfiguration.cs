using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Statyczna klasa konfiguracyjna dla ustawień importu z Outlook
    /// (wzorowana na DatabaseConfiguration)
    /// </summary>
    public static class OutlookConfiguration
    {
        private const string CONFIG_FILE = @"A:\outlookconfig.txt";
        private const string LOG_FILE = @"A:\outlookconfig_log.txt";

        // Domyślne wartości
        private static string _pstPath = @"F:\Users\zk@telsa\Dokumenty\Pliki programu Outlook\2017.07.z.kurzynski@telsa.pl.pst";
        private static string _pdfExportPath = @"A:\Skierowania";
        private static List<string> _foldersToSearch = new List<string>(); // Puste = przeszukuj wszystko
        private static bool _searchSubfolders = false; // Domyślnie: tylko główne foldery

        // ? NOWE: Konfiguracja serwera email (POP3/IMAP)
        private static string _emailServer = "krone.nq.pl"; // lub "imap.gmail.com"
        private static int _emailPort = 993; // IMAP SSL: 993, POP3 SSL: 995
        private static bool _emailUseSsl = true;
        private static string _emailUsername = "";
        private static string _emailPassword = "";
        private static string _emailFolder = "INBOX"; // Folder do przeszukiwania
        private static string _emailArchiveFolder = "Archiwum"; // ? NOWE: Folder archiwum (opcjonalny)
        private static bool _emailMoveToArchive = false; // ? NOWE: Czy przenosić do archiwum po imporcie
        private static string _emailSubjectPrefix = "POBRANA__ "; // ? NOWE: Prefix do dodania do tematu po imporcie
        private static bool _emailRenameSubject = true; // ? NOWE: Czy zmieniać temat po imporcie

        // ? NOWE: Konfiguracja konwersji dokumentów Word/Excel ? PDF
        private static bool _convertDocumentsToPdf = true; // Czy konwertować Word/Excel do PDF
        private static bool _keepOriginalDocuments = false; // Czy zachować oryginalne pliki po konwersji

        public static event EventHandler? ConfigurationChanged;

        /// <summary>
        /// Ścieżka do pliku PST Outlook
        /// </summary>
        public static string PstPath
        {
            get => _pstPath;
            set
            {
                if (_pstPath != value)
                {
                    _pstPath = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Ścieżka do folderu eksportu PDF
        /// </summary>
        public static string PdfExportPath
        {
            get => _pdfExportPath;
            set
            {
                if (_pdfExportPath != value)
                {
                    _pdfExportPath = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Lista folderów do przeszukiwania (relatywne ścieżki, np. "Loc_Skrzynka odbiorcza")
        /// Jeśli pusta - przeszukuj wszystkie foldery
        /// </summary>
        public static List<string> FoldersToSearch
        {
            get => _foldersToSearch;
            set
            {
                _foldersToSearch = value ?? new List<string>();
                SaveConfiguration();
                ConfigurationChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Czy przeszukiwać podfoldery wybranych folderów
        /// </summary>
        public static bool SearchSubfolders
        {
            get => _searchSubfolders;
            set
            {
                if (_searchSubfolders != value)
                {
                    _searchSubfolders = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Serwer email (IMAP/POP3)
        /// </summary>
        public static string EmailServer
        {
            get => _emailServer;
            set
            {
                if (_emailServer != value)
                {
                    _emailServer = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Port serwera email
        /// </summary>
        public static int EmailPort
        {
            get => _emailPort;
            set
            {
                if (_emailPort != value)
                {
                    _emailPort = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Czy używać SSL
        /// </summary>
        public static bool EmailUseSsl
        {
            get => _emailUseSsl;
            set
            {
                if (_emailUseSsl != value)
                {
                    _emailUseSsl = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Nazwa użytkownika email
        /// </summary>
        public static string EmailUsername
        {
            get => _emailUsername;
            set
            {
                if (_emailUsername != value)
                {
                    _emailUsername = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Hasło do konta email
        /// </summary>
        public static string EmailPassword
        {
            get => _emailPassword;
            set
            {
                if (_emailPassword != value)
                {
                    _emailPassword = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Folder email do przeszukiwania
        /// </summary>
        public static string EmailFolder
        {
            get => _emailFolder;
            set
            {
                if (_emailFolder != value)
                {
                    _emailFolder = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Folder archiwum dla przetworzonych e-maili
        /// </summary>
        public static string EmailArchiveFolder
        {
            get => _emailArchiveFolder;
            set
            {
                if (_emailArchiveFolder != value)
                {
                    _emailArchiveFolder = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Czy przenosić e-maile do archiwum po imporcie
        /// </summary>
        public static bool EmailMoveToArchive
        {
            get => _emailMoveToArchive;
            set
            {
                if (_emailMoveToArchive != value)
                {
                    _emailMoveToArchive = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Prefix do dodania do tematu e-maila po imporcie
        /// </summary>
        public static string EmailSubjectPrefix
        {
            get => _emailSubjectPrefix;
            set
            {
                if (_emailSubjectPrefix != value)
                {
                    _emailSubjectPrefix = value ?? "POBRANA__ ";
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Czy zmieniać temat e-maila po imporcie
        /// </summary>
        public static bool EmailRenameSubject
        {
            get => _emailRenameSubject;
            set
            {
                if (_emailRenameSubject != value)
                {
                    _emailRenameSubject = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Czy konwertować dokumenty Word/Excel do PDF podczas importu
        /// </summary>
        public static bool ConvertDocumentsToPdf
        {
            get => _convertDocumentsToPdf;
            set
            {
                if (_convertDocumentsToPdf != value)
                {
                    _convertDocumentsToPdf = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Czy zachować oryginalne pliki Word/Excel po konwersji do PDF
        /// </summary>
        public static bool KeepOriginalDocuments
        {
            get => _keepOriginalDocuments;
            set
            {
                if (_keepOriginalDocuments != value)
                {
                    _keepOriginalDocuments = value;
                    SaveConfiguration();
                    ConfigurationChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        static OutlookConfiguration()
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

                    if (lines.Length >= 2)
                    {
                        _pstPath = lines[0] ?? @"F:\Users\zk@telsa\Dokumenty\Pliki programu Outlook\2017.07.z.kurzynski@telsa.pl.pst";
                        _pdfExportPath = lines[1] ?? @"A:\Skierowania";

                        // ? Odczytaj listę folderów (linia 3)
                        if (lines.Length >= 3)
                        {
                            var foldersLine = lines[2];
                            if (!string.IsNullOrWhiteSpace(foldersLine))
                            {
                                _foldersToSearch = foldersLine.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(f => f.Trim())
                                    .ToList();
                            }
                        }

                        // ? Odczytaj flagę podfolderów (linia 4)
                        if (lines.Length >= 4)
                        {
                            bool.TryParse(lines[3], out _searchSubfolders);
                        }

                        // ? NOWE: Odczytaj konfigurację email (linie 5-14)
                        if (lines.Length >= 5) _emailServer = lines[4];
                        if (lines.Length >= 6) int.TryParse(lines[5], out _emailPort);
                        if (lines.Length >= 7) bool.TryParse(lines[6], out _emailUseSsl);
                        if (lines.Length >= 8) _emailUsername = lines[7];
                        if (lines.Length >= 9) _emailPassword = lines[8];
                        if (lines.Length >= 10) _emailFolder = lines[9];
                        if (lines.Length >= 11) _emailArchiveFolder = lines[10];
                        if (lines.Length >= 12) bool.TryParse(lines[11], out _emailMoveToArchive);
                        if (lines.Length >= 13) _emailSubjectPrefix = lines[12]; // ? NOWE
                        if (lines.Length >= 14) bool.TryParse(lines[13], out _emailRenameSubject); // ? NOWE
                        if (lines.Length >= 15) bool.TryParse(lines[14], out _convertDocumentsToPdf); // ? NOWE
                        if (lines.Length >= 16) bool.TryParse(lines[15], out _keepOriginalDocuments); // ? NOWE

                        Log($"Załadowano: PST={_pstPath}, Export={_pdfExportPath}");
                        Log($"Foldery do przeszukania: {(_foldersToSearch.Count > 0 ? string.Join(", ", _foldersToSearch) : "WSZYSTKIE")}");
                        Log($"Przeszukuj podfoldery: {_searchSubfolders}");
                        Log($"Email server: {_emailServer}:{_emailPort} (SSL: {_emailUseSsl}, User: {_emailUsername})");
                        Log($"PST exists: {File.Exists(_pstPath)}");
                        Log($"Export exists: {Directory.Exists(_pdfExportPath)}");
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
                Log($"SaveConfiguration START - PST={_pstPath}, Export={_pdfExportPath}");

                // ? NOWE: Zapisz 16 linii (PST, Export, Foldery, Podfoldery, Email config + Archive + Subject Prefix + Document Conversion)
                var lines = new List<string>
                {
                    _pstPath ?? "",
                    _pdfExportPath ?? "",
                    string.Join(";", _foldersToSearch), // Foldery oddzielone ?rednikiem
                    _searchSubfolders.ToString(), // "True" lub "False"
                    _emailServer ?? "",
                    _emailPort.ToString(),
                    _emailUseSsl.ToString(),
                    _emailUsername ?? "",
                    _emailPassword ?? "",
                    _emailFolder ?? "",
                    _emailArchiveFolder ?? "",
                    _emailMoveToArchive.ToString(),
                    _emailSubjectPrefix ?? "POBRANA__ ", // ? NOWE
                    _emailRenameSubject.ToString(), // ? NOWE
                    _convertDocumentsToPdf.ToString(), // ? NOWE: Konwersja dokumentów
                    _keepOriginalDocuments.ToString() // ? NOWE: Zachowaj oryginały
                };

                File.WriteAllLines(CONFIG_FILE, lines);

                Log("SaveConfiguration SUCCESS");
            }
            catch (Exception ex)
            {
                Log($"SaveConfiguration ERROR: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");

                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Błąd zapisu konfiguracji Outlook: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        }

        /// <summary>
        /// Waliduje ścieżki i zwraca informacje o błędach
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidatePaths()
        {
            if (string.IsNullOrWhiteSpace(_pstPath))
                return (false, "Ścieżka do pliku PST nie jest ustawiona");

            if (!File.Exists(_pstPath))
                return (false, $"Plik PST nie istnieje:\n{_pstPath}");

            if (string.IsNullOrWhiteSpace(_pdfExportPath))
                return (false, "Folder eksportu PDF nie jest ustawiony");

            // Export folder może nie istnieć - zostanie utworzony

            return (true, string.Empty);
        }
    }
}
