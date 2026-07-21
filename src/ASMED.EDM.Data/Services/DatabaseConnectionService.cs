using ASMED.EDM.Core.Configuration;
using ASMED.EDM.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace ASMED.EDM.Data.Services;

/// <summary>
/// Implementacja zarządzania połączeniami z bazą danych MySQL z failover
/// </summary>
public class DatabaseConnectionService : IDatabaseConnectionService
{
    private readonly DatabaseSettings _settings;
    private readonly ILogger<DatabaseConnectionService> _logger;
    private ConnectionType _currentConnectionType = ConnectionType.Primary;

    public event EventHandler<ConnectionType>? ConnectionChanged;

    public ConnectionType CurrentConnectionType => _currentConnectionType;

    public DatabaseConnectionService(
        IOptions<DatabaseSettings> settings,
        ILogger<DatabaseConnectionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Pobiera aktywny connection string z automatycznym failover
    /// </summary>
    public async Task<string> GetActiveConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableFailover)
        {
            _logger.LogInformation("Failover wyłączony, używam Primary connection");
            return _settings.PrimaryConnection;
        }

        // Próbujemy połączenia w kolejności: Primary → Backup → Local
        var connections = new[]
        {
            (Type: ConnectionType.Primary, ConnectionString: _settings.PrimaryConnection),
            (Type: ConnectionType.Backup, ConnectionString: _settings.BackupConnection),
            (Type: ConnectionType.Local, ConnectionString: _settings.LocalConnection)
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
                        "Zmiana połączenia z {OldType} na {NewType}",
                        _currentConnectionType,
                        type);

                    _currentConnectionType = type;
                    ConnectionChanged?.Invoke(this, type);
                }

                _logger.LogInformation("Używam połączenia {ConnectionType}", type);
                return connectionString;
            }
        }

        _logger.LogError("Wszystkie połączenia z bazą danych są niedostępne!");
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
        {
            return false;
        }

        try
        {
            // Tworzymy connection string z krótkim timeout dla szybkiego failover
            var builder = new MySqlConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = (uint)_settings.ConnectionTimeout,
                // Wymuszamy test połączenia
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
