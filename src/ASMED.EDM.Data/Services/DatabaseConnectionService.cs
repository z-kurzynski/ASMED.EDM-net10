using ASMED.EDM.Core.Configuration;
using ASMED.EDM.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace ASMED.EDM.Data.Services;

/// <summary>
/// Implementacja zarządzania połączeniami z bazą danych MySQL z failover.
/// Używa DbConnectionFactory jako źródła connection stringów (rejestr → appsettings fallback).
/// Automatycznie testuje Primary → Backup → Local i zapisuje wynik w rejestrze.
/// </summary>
public class DatabaseConnectionService : IDatabaseConnectionService
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly ILogger<DatabaseConnectionService> _logger;
    private ConnectionType _currentConnectionType = ConnectionType.Primary;

    public event EventHandler<ConnectionType>? ConnectionChanged;

    public ConnectionType CurrentConnectionType => _currentConnectionType;

    public DatabaseConnectionService(
        DbConnectionFactory dbFactory,
        ILogger<DatabaseConnectionService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;

        // Ustaw bieżący typ na podstawie tego co jest w rejestrze
        if (Enum.TryParse<ConnectionType>(_dbFactory.ActiveConnectionType, out var saved))
            _currentConnectionType = saved;
    }

    /// <summary>
    /// Pobiera aktywny connection string z automatycznym failover.
    /// Kolejność prób: Primary → Backup → Local.
    /// Wynik (aktywny typ) jest zapisywany w rejestrze per-stacja.
    /// </summary>
    public async Task<string> GetActiveConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (!_dbFactory.EnableFailover)
        {
            _logger.LogInformation("Failover wyłączony, używam Primary connection");
            return _dbFactory.PrimaryConnectionString;
        }

        // Automatyczny failover tylko: Primary → Local
        // Backup jest wyłącznie do ręcznego przełączania (testy, podgląd danych)
        var connections = new[]
        {
            (Type: ConnectionType.Primary, ConnectionString: _dbFactory.PrimaryConnectionString),
            (Type: ConnectionType.Local,   ConnectionString: _dbFactory.LocalConnectionString)
        };

        foreach (var (type, connectionString) in connections)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("Connection string dla {ConnectionType} jest pusty, pomijam", type);
                continue;
            }

            _logger.LogDebug("Testuję połączenie {ConnectionType}...", type);

            if (await TestConnectionAsync(connectionString, cancellationToken))
            {
                if (_currentConnectionType != type)
                {
                    _logger.LogWarning(
                        "⚠️ Failover: zmiana połączenia {OldType} → {NewType}",
                        _currentConnectionType,
                        type);

                    _currentConnectionType = type;

                    // Zapisz aktywny typ do rejestru — kolejne scope DbContextu użyją właściwego połączenia
                    _dbFactory.ActiveConnectionType = type.ToString();

                    ConnectionChanged?.Invoke(this, type);
                }

                _logger.LogInformation("✅ Używam połączenia {ConnectionType}", type);
                return connectionString;
            }
        }

        _logger.LogError("❌ Wszystkie połączenia z bazą danych są niedostępne!");
        throw new InvalidOperationException(
            "Nie można nawiązać połączenia z żadną bazą danych (Primary/Backup/Local)");
    }

    /// <summary>
    /// Testuje połączenie z bazą MySQL
    /// </summary>
    public async Task<bool> TestConnectionAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = (uint)_dbFactory.ConnectionTimeout,
                Pooling = false
            };

            await using var connection = new MySqlConnection(builder.ToString());
            await connection.OpenAsync(cancellationToken);
            await connection.CloseAsync();

            _logger.LogDebug("Połączenie z bazą danych OK");
            return true;
        }
        catch (MySqlException ex)
        {
            _logger.LogWarning(ex, "Błąd połączenia z bazą MySQL: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nieoczekiwany błąd podczas testowania połączenia");
            return false;
        }
    }
}
