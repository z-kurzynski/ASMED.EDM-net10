using System.Data.Common;
using ASMED.EDM.Core.Configuration;
using ASMED.EDM.Core.Helpers;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace ASMED.EDM.Data.Services;

/// <summary>
/// Fabryka połączeń MySQL z obsługą Registry jako primary source + appsettings.json jako fallback.
/// </summary>
public class DbConnectionFactory
{
    private readonly IOptions<DatabaseSettings> _settings;

    public DbConnectionFactory(IOptions<DatabaseSettings> settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Aktywne połączenie (z Registry lub appsettings.json fallback).
    /// </summary>
    public string ActiveConnectionString
    {
        get
        {
            var activeType = ActiveConnectionType;
            return activeType switch
            {
                "Backup" => BackupConnectionString,
                "Local" => LocalConnectionString,
                _ => PrimaryConnectionString
            };
        }
    }

    /// <summary>
    /// Aktywny typ połączenia: "Primary", "Backup", "Local".
    /// </summary>
    public string ActiveConnectionType
    {
        get => RegistryConfigHelper.GetValue(
            RegistryConfigHelper.KeyActiveConnection,
            "Primary") ?? "Primary";
        set
        {
            RegistryConfigHelper.SetValue(RegistryConfigHelper.KeyActiveConnection, value);
            ConnectionTypeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Event wywoływany gdy zmienia się aktywny typ połączenia.
    /// </summary>
    public event EventHandler? ConnectionTypeChanged;

    /// <summary>
    /// Primary connection string (Registry → appsettings.json fallback).
    /// </summary>
    public string PrimaryConnectionString =>
        EnsureMySqlCharset(
            RegistryConfigHelper.GetValue(
                RegistryConfigHelper.KeyMySqlPrimaryConnection,
                _settings.Value.PrimaryConnection));

    /// <summary>
    /// Backup connection string (Registry → appsettings.json fallback).
    /// </summary>
    public string BackupConnectionString =>
        EnsureMySqlCharset(
            RegistryConfigHelper.GetValue(
                RegistryConfigHelper.KeyMySqlBackupConnection,
                _settings.Value.BackupConnection));

    /// <summary>
    /// Local connection string (Registry → appsettings.json fallback).
    /// </summary>
    public string LocalConnectionString =>
        EnsureMySqlCharset(
            RegistryConfigHelper.GetValue(
                RegistryConfigHelper.KeyMySqlLocalConnection,
                _settings.Value.LocalConnection));

    /// <summary>
    /// Czy tryb failover jest włączony.
    /// </summary>
    public bool EnableFailover
    {
        get => RegistryConfigHelper.GetBoolValue(
            RegistryConfigHelper.KeyEnableFailover,
            _settings.Value.EnableFailover);
        set => RegistryConfigHelper.SetBoolValue(
            RegistryConfigHelper.KeyEnableFailover, value);
    }

    /// <summary>
    /// Connection timeout w sekundach.
    /// </summary>
    public int ConnectionTimeout
    {
        get => RegistryConfigHelper.GetIntValue(
            RegistryConfigHelper.KeyConnectionTimeout,
            _settings.Value.ConnectionTimeout);
        set => RegistryConfigHelper.SetIntValue(
            RegistryConfigHelper.KeyConnectionTimeout, value);
    }

    /// <summary>
    /// Tworzy połączenie MySQL używając aktywnego connection stringa.
    /// </summary>
    public DbConnection CreateConnection() =>
        new MySqlConnection(ActiveConnectionString);

    /// <summary>
    /// Tworzy połączenie MySQL używając podanego connection stringa.
    /// </summary>
    public DbConnection CreateConnectionFromString(string connectionString) =>
        new MySqlConnection(EnsureMySqlCharset(connectionString));

    /// <summary>
    /// Testuje połączenie MySQL (async).
    /// </summary>
    public async Task<(bool Success, string Message, long Ms)> TestConnectionAsync(string? connectionString = null)
    {
        var cs = connectionString ?? ActiveConnectionString;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await Task.Run(() =>
            {
                using var conn = new MySqlConnection(cs);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.ExecuteScalar();
            });
            sw.Stop();
            return (true, "Połączono pomyślnie (MySQL)", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, $"Błąd: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Zapisuje primary connection string do Registry.
    /// </summary>
    public void SavePrimaryConnection(string connectionString)
    {
        RegistryConfigHelper.SetValue(
            RegistryConfigHelper.KeyMySqlPrimaryConnection,
            connectionString);
    }

    /// <summary>
    /// Zapisuje backup connection string do Registry.
    /// </summary>
    public void SaveBackupConnection(string connectionString)
    {
        RegistryConfigHelper.SetValue(
            RegistryConfigHelper.KeyMySqlBackupConnection,
            connectionString);
    }

    /// <summary>
    /// Zapisuje local connection string do Registry.
    /// </summary>
    public void SaveLocalConnection(string connectionString)
    {
        RegistryConfigHelper.SetValue(
            RegistryConfigHelper.KeyMySqlLocalConnection,
            connectionString);
    }

    /// <summary>
    /// Sprawdza czy connection string zawiera CharSet=utf8mb4, jeśli nie - dodaje.
    /// </summary>
    private static string EnsureMySqlCharset(string? cs)
    {
        if (string.IsNullOrEmpty(cs))
            return "Server=localhost;Database=asmed_edm;User=root;Password=;CharSet=utf8mb4;";

        if (cs.Contains("CharSet=", StringComparison.OrdinalIgnoreCase) ||
            cs.Contains("Charset=", StringComparison.OrdinalIgnoreCase) ||
            cs.Contains("charset=", StringComparison.OrdinalIgnoreCase))
            return cs;

        return cs.TrimEnd(';') + ";CharSet=utf8mb4;";
    }
}
