using ASMED.EDM.Data.Models;

namespace ASMED.EDM.Data.Services;

/// <summary>
/// Interfejs serwisu migracji danych z bazy Access do MySQL
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Zwraca listę tabel dostępnych do migracji (z kategoryzacją)
    /// </summary>
    IReadOnlyList<TableInfo> GetAvailableTables();

    /// <summary>
    /// Skanuje plik Access i zwraca nazwy tabel użytkownika.
    /// Pozwala wykryć niezgodności nazw przed migracją.
    /// </summary>
    Task<(bool Success, IReadOnlyList<string> TableNames, string Message)> ScanAccessTablesAsync(string accessFilePath);

    /// <summary>
    /// Testuje połączenie z plikiem bazy danych Access
    /// </summary>
    Task<(bool Success, string Message)> TestAccessConnectionAsync(string accessFilePath);

    /// <summary>
    /// Testuje połączenie z docelową bazą MySQL
    /// </summary>
    Task<(bool Success, string Message)> TestMySqlConnectionAsync(string connectionString);

    /// <summary>
    /// Tworzy kopię zapasową wskazanych tabel MySQL przed migracją.
    /// Zwraca ścieżkę do pliku kopii.
    /// </summary>
    Task<(bool Success, string BackupPath, string Message)> CreateBackupAsync(
        string connectionString,
        IEnumerable<string> tableNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Migruje jedną tabelę z Access do MySQL.
    /// Tabela docelowa jest najpierw czyszczona (TRUNCATE).
    /// </summary>
    Task<(bool Success, int RowsMigrated, string Message)> MigrateTableAsync(
        string accessFilePath,
        string mysqlConnectionString,
        string tableName,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Migruje wiele tabel sekwencyjnie.
    /// </summary>
    Task MigrateTablesAsync(
        string accessFilePath,
        string mysqlConnectionString,
        IEnumerable<string> tableNames,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Weryfikuje zgodność liczby rekordów między Access a MySQL dla wskazanych tabel.
    /// Zwraca listę wyników (tabela, rekordy Access, rekordy MySQL, zgodność).
    /// </summary>
    Task<IReadOnlyList<TableVerificationResult>> VerifyRowCountsAsync(
        string accessFilePath,
        string mysqlConnectionString,
        IEnumerable<string> tableNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Przywraca bazę MySQL z pliku kopii zapasowej (.sql).
    /// Wykonuje skrypt SQL na wskazanej bazie docelowej.
    /// </summary>
    Task<(bool Success, int StatementsExecuted, string Message)> RestoreBackupAsync(
        string backupFilePath,
        string targetConnectionString,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kopiuje dane wszystkich (lub wskazanych) tabel z bazy źródłowej do docelowej.
    /// Tabele docelowe są najpierw czyszczone (TRUNCATE).
    /// Używane np. do synchronizacji PRIMARY → BACKUP lub PRIMARY → LOCAL.
    /// </summary>
    Task<(bool Success, int TablesCopied, string Message)> CopyMySqlDatabaseAsync(
        string sourceConnectionString,
        string targetConnectionString,
        IEnumerable<string>? tableNames = null,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
