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

    #endregion

    public ConfigurationViewModel(
        IDatabaseConnectionService connectionService,
        ILogger<ConfigurationViewModel> logger,
        IConfiguration configuration,
        IOptions<DatabaseSettings> databaseSettings)
    {
        _connectionService = connectionService;
        _logger = logger;
        _configuration = configuration;
        _databaseSettings = databaseSettings;

        LoadCurrentConfiguration();
    }

    /// <summary>
    /// Ładuje obecną konfigurację z appsettings.json
    /// </summary>
    private void LoadCurrentConfiguration()
    {
        try
        {
            var settings = _databaseSettings.Value;

            // Parsuj Primary connection string
            ParseConnectionString(
                settings.PrimaryConnection,
                out var primarySrv, out var primaryDb, out var primaryUsr, out var primaryPwd, out var primaryPrt);

            PrimaryServer = primarySrv;
            PrimaryDatabase = primaryDb;
            PrimaryUser = primaryUsr;
            PrimaryPassword = primaryPwd;
            PrimaryPort = primaryPrt;

            // Parsuj Backup connection string (jeśli istnieje)
            if (!string.IsNullOrWhiteSpace(settings.BackupConnection))
            {
                ParseConnectionString(
                    settings.BackupConnection,
                    out var backupSrv, out var backupDb, out var backupUsr, out var backupPwd, out var backupPrt);

                BackupServer = backupSrv;
                BackupDatabase = backupDb;
                BackupUser = backupUsr;
                BackupPassword = backupPwd;
                BackupPort = backupPrt;
            }

            // Parsuj Local connection string (jeśli istnieje)
            if (!string.IsNullOrWhiteSpace(settings.LocalConnection))
            {
                ParseConnectionString(
                    settings.LocalConnection,
                    out var localSrv, out var localDb, out var localUsr, out var localPwd, out var localPrt);

                LocalServer = localSrv;
                LocalDatabase = localDb;
                LocalUser = localUsr;
                LocalPassword = localPwd;
                LocalPort = localPrt;
            }

            EnableFailover = settings.EnableFailover;
            ConnectionTimeout = settings.ConnectionTimeout;

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
            var success = await _connectionService.TestConnectionAsync(connectionString);

            if (success)
            {
                StatusMessage = $"✅ {name}: Połączenie OK!";
                _logger.LogInformation("✅ Test połączenia {Name} zakończony sukcesem", name);
            }
            else
            {
                StatusMessage = $"❌ {name}: Nie można połączyć się z bazą";
                _logger.LogWarning("❌ Test połączenia {Name} nieudany", name);
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

            // TODO: Tutaj trzeba zaktualizować appsettings.json
            // Na razie tylko logujemy - implementacja zapisu do pliku w następnym kroku
            _logger.LogInformation("🔧 Nowa konfiguracja:");
            _logger.LogInformation("  Primary: {Primary}", primaryConnStr);
            _logger.LogInformation("  Backup: {Backup}", backupConnStr);
            _logger.LogInformation("  Local: {Local}", localConnStr);
            _logger.LogInformation("  EnableFailover: {EnableFailover}", EnableFailover);
            _logger.LogInformation("  ConnectionTimeout: {Timeout}s", ConnectionTimeout);

            // Symulacja zapisu (na razie)
            await Task.Delay(500);

            StatusMessage = "✅ Konfiguracja zapisana! (TODO: implementacja zapisu do appsettings.json)";

            // TODO: Wywołaj MainViewModel.RefreshDatabaseConnectionAsync()
            // Wymaga przekazania MainViewModel przez DI lub event bus

            MessageBox.Show(
                "✅ Konfiguracja zapisana!\n\n⚠️ UWAGA: Implementacja zapisu do appsettings.json będzie w następnym kroku.\nNa razie tylko walidacja i podgląd w logach.",
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
}
