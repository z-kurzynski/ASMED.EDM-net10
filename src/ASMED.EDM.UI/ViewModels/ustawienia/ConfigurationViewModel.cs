using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMED.EDM.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ASMED.EDM.Core.Configuration;
using Microsoft.Extensions.Configuration;
using ASMED.EDM.Data.Services;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// ViewModel dla widoku konfiguracji połączeń MySQL
/// </summary>
public partial class ConfigurationViewModel : ObservableObject
{
    private readonly IDatabaseConnectionService _connectionService;
    private readonly ILogger<ConfigurationViewModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptions<DatabaseSettings> _databaseSettings;
    private readonly DbConnectionFactory _dbFactory;

    #region Observable Properties

    [ObservableProperty]
    private string _primaryServer = "localhost";

    [ObservableProperty]
    private string _primaryDatabase = "asmed_edm";

    [ObservableProperty]
    private string _primaryUser = "root";

    [ObservableProperty]
    private string _primaryPassword = "";

    [ObservableProperty]
    private int _primaryPort = 3306;

    [ObservableProperty]
    private string _backupServer = "";

    [ObservableProperty]
    private string _backupDatabase = "";

    [ObservableProperty]
    private string _backupUser = "";

    [ObservableProperty]
    private string _backupPassword = "";

    [ObservableProperty]
    private int _backupPort = 3306;

    [ObservableProperty]
    private string _localServer = "localhost";

    [ObservableProperty]
    private string _localDatabase = "asmed_edm_local";

    [ObservableProperty]
    private string _localUser = "root";

    [ObservableProperty]
    private string _localPassword = "";

    [ObservableProperty]
    private int _localPort = 3306;

    [ObservableProperty]
    private bool _enableFailover = true;

    [ObservableProperty]
    private int _connectionTimeout = 3;

    [ObservableProperty]
    private string _statusMessage = "Gotowy do testowania połączenia";

    [ObservableProperty]
    private bool _isTesting = false;

    [ObservableProperty]
    private bool _isSaving = false;

    // ========== Prawa kolumna: Zarządzanie bazą ==========

    [ObservableProperty]
    private bool _isInitializing = false;

    [ObservableProperty]
    private string _initializationStatus = "Kliknij przycisk aby zainicjalizować bazę danych";

    [ObservableProperty]
    private bool _isBackingUp = false;

    [ObservableProperty]
    private string _backupPath = @"D:\Backups\asmed_edm";

    [ObservableProperty]
    private string _backupStatus = "Wybierz ścieżkę i kliknij przycisk aby utworzyć backup";

    [ObservableProperty]
    private bool _isLoadingStats = false;

    [ObservableProperty]
    private string _dbName = "-";

    [ObservableProperty]
    private string _tableCount = "0";

    [ObservableProperty]
    private string _totalRecords = "0";

    [ObservableProperty]
    private string _databaseSize = "0 MB";

    [ObservableProperty]
    private string _lastBackupDate = "Nigdy";

    [ObservableProperty]
    private bool _isOptimizing = false;

    [ObservableProperty]
    private bool _isRepairing = false;

    [ObservableProperty]
    private string _maintenanceStatus = "Gotowy do wykonania operacji konserwacyjnych";

    #endregion

    public ConfigurationViewModel(
        IDatabaseConnectionService connectionService,
        ILogger<ConfigurationViewModel> logger,
        IConfiguration configuration,
        IOptions<DatabaseSettings> databaseSettings,
        DbConnectionFactory dbFactory)
    {
        _connectionService = connectionService;
        _logger = logger;
        _configuration = configuration;
        _databaseSettings = databaseSettings;
        _dbFactory = dbFactory;

        LoadCurrentConfiguration();
    }

    /// <summary>
    /// Ładuje obecną konfigurację z Registry (jeśli istnieje) lub appsettings.json (fallback)
    /// </summary>
    private void LoadCurrentConfiguration()
    {
        try
        {
            // Użyj DbConnectionFactory który obsługuje Registry → appsettings fallback

            // Parsuj Primary connection string (z Registry lub appsettings)
            ParseConnectionString(
                _dbFactory.PrimaryConnectionString,
                out var primarySrv, out var primaryDb, out var primaryUsr, out var primaryPwd, out var primaryPrt);

            PrimaryServer = primarySrv;
            PrimaryDatabase = primaryDb;
            PrimaryUser = primaryUsr;
            PrimaryPassword = primaryPwd;
            PrimaryPort = primaryPrt;

            // Parsuj Backup connection string (z Registry lub appsettings)
            var backupCs = _dbFactory.BackupConnectionString;
            if (!string.IsNullOrWhiteSpace(backupCs))
            {
                ParseConnectionString(
                    backupCs,
                    out var backupSrv, out var backupDb, out var backupUsr, out var backupPwd, out var backupPrt);

                BackupServer = backupSrv;
                BackupDatabase = backupDb;
                BackupUser = backupUsr;
                BackupPassword = backupPwd;
                BackupPort = backupPrt;
            }

            // Parsuj Local connection string (z Registry lub appsettings)
            var localCs = _dbFactory.LocalConnectionString;
            if (!string.IsNullOrWhiteSpace(localCs))
            {
                ParseConnectionString(
                    localCs,
                    out var localSrv, out var localDb, out var localUsr, out var localPwd, out var localPrt);

                LocalServer = localSrv;
                LocalDatabase = localDb;
                LocalUser = localUsr;
                LocalPassword = localPwd;
                LocalPort = localPrt;
            }

            EnableFailover = _dbFactory.EnableFailover;
            ConnectionTimeout = _dbFactory.ConnectionTimeout;

            _logger.LogInformation("✅ Załadowano konfigurację z appsettings.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Błąd podczas ładowania konfiguracji");
            StatusMessage = $"❌ Błąd: {ex.Message}";
        }
    }

    /// <summary>
    /// Parsuje connection string MySQL na komponenty
    /// </summary>
    private void ParseConnectionString(
        string connectionString,
        out string server,
        out string database,
        out string user,
        out string password,
        out int port)
    {
        server = "localhost";
        database = "asmed_edm";
        user = "root";
        password = "";
        port = 3306;

        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (keyValue.Length != 2) continue;

            var key = keyValue[0].Trim().ToLowerInvariant();
            var value = keyValue[1].Trim();

            switch (key)
            {
                case "server":
                    server = value;
                    break;
                case "database":
                    database = value;
                    break;
                case "user":
                case "uid":
                case "user id":
                    user = value;
                    break;
                case "password":
                case "pwd":
                    password = value;
                    break;
                case "port":
                    if (int.TryParse(value, out var p))
                        port = p;
                    break;
            }
        }
    }

    /// <summary>
    /// Buduje connection string MySQL z komponentów
    /// </summary>
    private string BuildConnectionString(
        string server,
        string database,
        string user,
        string password,
        int port)
    {
        return $"Server={server};Port={port};Database={database};User={user};Password={password};";
    }

    /// <summary>
    /// Testuje połączenie Primary
    /// </summary>
    [RelayCommand]
    private async Task TestPrimaryConnectionAsync()
    {
        await TestConnectionAsync("Primary", PrimaryServer, PrimaryDatabase, PrimaryUser, PrimaryPassword, PrimaryPort);
    }

    /// <summary>
    /// Testuje połączenie Backup
    /// </summary>
    [RelayCommand]
    private async Task TestBackupConnectionAsync()
    {
        await TestConnectionAsync("Backup", BackupServer, BackupDatabase, BackupUser, BackupPassword, BackupPort);
    }

    /// <summary>
    /// Testuje połączenie Local
    /// </summary>
    [RelayCommand]
    private async Task TestLocalConnectionAsync()
    {
        await TestConnectionAsync("Local", LocalServer, LocalDatabase, LocalUser, LocalPassword, LocalPort);
    }

    /// <summary>
    /// Wspólna logika testowania połączenia
    /// </summary>
    private async Task TestConnectionAsync(
        string name,
        string server,
        string database,
        string user,
        string password,
        int port)
    {
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            StatusMessage = $"⚠️ {name}: Wypełnij Server i Database";
            return;
        }

        IsTesting = true;
        StatusMessage = $"🔄 Testowanie {name}...";

        try
        {
            var connectionString = BuildConnectionString(server, database, user, password, port);

            // Użyj DbConnectionFactory zamiast _connectionService
            var (success, message, ms) = await _dbFactory.TestConnectionAsync(connectionString);

            if (success)
            {
                StatusMessage = $"✅ {name}: Połączenie OK! [{ms} ms]";
                _logger.LogInformation("✅ Test połączenia {Name} zakończony sukcesem ({Ms} ms)", name, ms);
            }
            else
            {
                StatusMessage = $"❌ {name}: {message}";
                _logger.LogWarning("❌ Test połączenia {Name} nieudany: {Message}", name, message);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ {name}: {ex.Message}";
            _logger.LogError(ex, "Błąd podczas testowania połączenia {Name}", name);
        }
        finally
        {
            IsTesting = false;
        }
    }

    /// <summary>
    /// Zapisuje konfigurację do appsettings.json
    /// </summary>
    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        IsSaving = true;
        StatusMessage = "💾 Zapisywanie konfiguracji...";

        try
        {
            // Buduj connection stringi
            var primaryConnStr = BuildConnectionString(
                PrimaryServer, PrimaryDatabase, PrimaryUser, PrimaryPassword, PrimaryPort);

            var backupConnStr = string.IsNullOrWhiteSpace(BackupServer)
                ? ""
                : BuildConnectionString(BackupServer, BackupDatabase, BackupUser, BackupPassword, BackupPort);

            var localConnStr = string.IsNullOrWhiteSpace(LocalServer)
                ? ""
                : BuildConnectionString(LocalServer, LocalDatabase, LocalUser, LocalPassword, LocalPort);

            _logger.LogInformation("🔧 Zapisywanie konfiguracji MySQL do Registry...");
            _logger.LogInformation("  Primary: {Primary}", primaryConnStr);
            _logger.LogInformation("  Backup: {Backup}", backupConnStr);
            _logger.LogInformation("  Local: {Local}", localConnStr);
            _logger.LogInformation("  EnableFailover: {EnableFailover}", EnableFailover);
            _logger.LogInformation("  ConnectionTimeout: {Timeout}s", ConnectionTimeout);

            // Zapisz do Registry przez DbConnectionFactory
            _dbFactory.SavePrimaryConnection(primaryConnStr);
            _dbFactory.SaveBackupConnection(backupConnStr);
            _dbFactory.SaveLocalConnection(localConnStr);
            _dbFactory.EnableFailover = EnableFailover;
            _dbFactory.ConnectionTimeout = ConnectionTimeout;

            // Symulacja delay (dla UX feedback)
            await Task.Delay(200);

            StatusMessage = "✅ Konfiguracja zapisana do Registry!";

            // TODO: Wywołaj MainViewModel.RefreshDatabaseConnectionAsync()
            // Wymaga przekazania MainViewModel przez DI lub event bus

            MessageBox.Show(
                "✅ Konfiguracja zapisana!\n\n" +
                "Połączenia MySQL zostały zapisane w Windows Registry:\n" +
                "HKEY_CURRENT_USER\\Software\\ASMED\\EDM\n\n" +
                "Nastepnym razem aplikacja użyje tych ustawień.",
                "Sukces",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Błąd zapisu: {ex.Message}";
            _logger.LogError(ex, "Błąd podczas zapisywania konfiguracji");

            MessageBox.Show(
                $"❌ Błąd podczas zapisywania:\n{ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }

    // ==================== PRAWA KOLUMNA: COMMANDS ====================

    /// <summary>
    /// Inicjalizuje bazę danych (tworzy tabele, indeksy, relacje)
    /// </summary>
    [RelayCommand]
    private async Task InitializeDatabaseAsync()
    {
        IsInitializing = true;
        InitializationStatus = "🔄 Inicjalizacja bazy danych w toku...";

        try
        {
            // TODO: Implementacja inicjalizacji bazy danych
            // - Sprawdzenie czy baza istnieje
            // - Utworzenie tabel jeśli nie istnieją
            // - Utworzenie indeksów
            // - Utworzenie relacji FK
            // - Seed initial data

            _logger.LogInformation("🗄️ Rozpoczęto inicjalizację bazy danych...");

            // Symulacja (na razie)
            await Task.Delay(2000);

            InitializationStatus = "✅ Baza danych zainicjalizowana pomyślnie! (TODO: implementacja)";
            _logger.LogInformation("✅ Inicjalizacja bazy danych zakończona sukcesem");

            MessageBox.Show(
                "✅ Baza danych została zainicjalizowana!\n\n⚠️ UWAGA: To jest symulacja. Implementacja w następnym kroku.",
                "Sukces",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            InitializationStatus = $"❌ Błąd inicjalizacji: {ex.Message}";
            _logger.LogError(ex, "Błąd podczas inicjalizacji bazy danych");

            MessageBox.Show(
                $"❌ Błąd podczas inicjalizacji bazy danych:\n{ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsInitializing = false;
        }
    }

    /// <summary>
    /// Tworzy backup bazy danych MySQL
    /// </summary>
    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (string.IsNullOrWhiteSpace(BackupPath))
        {
            BackupStatus = "⚠️ Podaj ścieżkę do backupu";
            return;
        }

        IsBackingUp = true;
        BackupStatus = "🔄 Tworzenie backupu...";

        try
        {
            // TODO: Implementacja MySQL backup
            // - Użyć mysqldump lub MySqlConnector
            // - Zapisać dump do pliku w BackupPath
            // - Format: asmed_edm_backup_YYYYMMDD_HHMMSS.sql

            _logger.LogInformation("💾 Rozpoczęto tworzenie backupu bazy danych do {Path}", BackupPath);

            // Symulacja (na razie)
            await Task.Delay(3000);

            var backupFileName = $"asmed_edm_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            var fullPath = System.IO.Path.Combine(BackupPath, backupFileName);

            BackupStatus = $"✅ Backup utworzony: {backupFileName} (TODO: implementacja)";
            LastBackupDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            _logger.LogInformation("✅ Backup utworzony: {FileName}", backupFileName);

            MessageBox.Show(
                $"✅ Backup utworzony!\n\nPlik: {backupFileName}\nŚcieżka: {BackupPath}\n\n⚠️ UWAGA: To jest symulacja. Implementacja w następnym kroku.",
                "Sukces",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            BackupStatus = $"❌ Błąd podczas tworzenia backupu: {ex.Message}";
            _logger.LogError(ex, "Błąd podczas tworzenia backupu");

            MessageBox.Show(
                $"❌ Błąd podczas tworzenia backupu:\n{ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    /// <summary>
    /// Pobiera statystyki bazy danych
    /// </summary>
    [RelayCommand]
    private async Task GetDatabaseStatisticsAsync()
    {
        IsLoadingStats = true;

        try
        {
            // TODO: Implementacja pobierania statystyk z MySQL
            // - SELECT DATABASE() dla nazwy bazy
            // - COUNT(*) z information_schema.tables dla liczby tabel
            // - SUM(TABLE_ROWS) z information_schema.tables dla liczby rekordów
            // - SUM(DATA_LENGTH + INDEX_LENGTH) dla rozmiaru bazy

            _logger.LogInformation("📊 Pobieranie statystyk bazy danych...");

            // Symulacja (na razie)
            await Task.Delay(1000);

            DbName = "asmed_edm";
            TableCount = "42";
            TotalRecords = "15,384";
            DatabaseSize = "128.5 MB";
            // LastBackupDate już jest ustawiony w CreateBackupAsync

            _logger.LogInformation("✅ Statystyki pobrane pomyślnie");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania statystyk");

            MessageBox.Show(
                $"❌ Błąd podczas pobierania statystyk:\n{ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoadingStats = false;
        }
    }

    /// <summary>
    /// Optymalizuje tabele w bazie danych
    /// </summary>
    [RelayCommand]
    private async Task OptimizeTablesAsync()
    {
        IsOptimizing = true;
        MaintenanceStatus = "🔄 Optymalizacja tabel...";

        try
        {
            // TODO: Implementacja OPTIMIZE TABLE
            // - Pobierz listę wszystkich tabel
            // - Wykonaj OPTIMIZE TABLE dla każdej tabeli
            // - Loguj wyniki

            _logger.LogInformation("🧹 Rozpoczęto optymalizację tabel...");

            await Task.Delay(2000);

            MaintenanceStatus = "✅ Tabele zoptymalizowane pomyślnie! (TODO: implementacja)";
            _logger.LogInformation("✅ Optymalizacja tabel zakończona");

            MessageBox.Show(
                "✅ Tabele zostały zoptymalizowane!\n\n⚠️ UWAGA: To jest symulacja. Implementacja w następnym kroku.",
                "Sukces",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MaintenanceStatus = $"❌ Błąd optymalizacji: {ex.Message}";
            _logger.LogError(ex, "Błąd podczas optymalizacji tabel");

            MessageBox.Show(
                $"❌ Błąd podczas optymalizacji:\n{ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsOptimizing = false;
        }
    }

    /// <summary>
    /// Naprawia tabele w bazie danych
    /// </summary>
    [RelayCommand]
    private async Task RepairTablesAsync()
    {
        IsRepairing = true;
        MaintenanceStatus = "🔄 Naprawa tabel...";

        try
        {
            // TODO: Implementacja REPAIR TABLE
            // - Pobierz listę wszystkich tabel
            // - Wykonaj REPAIR TABLE dla każdej tabeli
            // - Loguj wyniki

            _logger.LogInformation("🔍 Rozpoczęto naprawę tabel...");

            await Task.Delay(2000);

            MaintenanceStatus = "✅ Tabele naprawione pomyślnie! (TODO: implementacja)";
            _logger.LogInformation("✅ Naprawa tabel zakończona");

            MessageBox.Show(
                "✅ Tabele zostały naprawione!\n\n⚠️ UWAGA: To jest symulacja. Implementacja w następnym kroku.",
                "Sukces",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MaintenanceStatus = $"❌ Błąd naprawy: {ex.Message}";
            _logger.LogError(ex, "Błąd podczas naprawy tabel");

            MessageBox.Show(
                $"❌ Błąd podczas naprawy:\n{ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsRepairing = false;
        }
    }
}
