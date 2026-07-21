namespace ASMED.EDM.Core.Services;

/// <summary>
/// Typ aktywnego połączenia z bazą danych
/// </summary>
public enum ConnectionType
{
    /// <summary>
    /// Główna baza produkcyjna
    /// </summary>
    Primary,

    /// <summary>
    /// Baza backup
    /// </summary>
    Backup,

    /// <summary>
    /// Baza lokalna (offline)
    /// </summary>
    Local
}

/// <summary>
/// Zarządza połączeniami z bazą danych z automatycznym failover
/// </summary>
public interface IDatabaseConnectionService
{
    /// <summary>
    /// Pobiera aktualny connection string z uwzględnieniem failover
    /// </summary>
    /// <returns>Connection string do aktywnej bazy</returns>
    Task<string> GetActiveConnectionStringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Testuje połączenie z bazą danych
    /// </summary>
    /// <param name="connectionString">Connection string do przetestowania</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>True jeśli połączenie działa</returns>
    Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Typ aktualnie używanego połączenia
    /// </summary>
    ConnectionType CurrentConnectionType { get; }

    /// <summary>
    /// Zdarzenie wywoływane przy zmianie aktywnego połączenia
    /// </summary>
    event EventHandler<ConnectionType>? ConnectionChanged;
}
