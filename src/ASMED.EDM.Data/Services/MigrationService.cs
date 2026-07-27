using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Text;
using ASMED.EDM.Data.Helpers;
using ASMED.EDM.Data.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ASMED.EDM.Data.Services;

/// <summary>
/// Serwis migracji danych z bazy Access do MySQL.
/// Obsługuje testowanie połączeń, tworzenie kopii zapasowych i migrację tabel.
/// </summary>
public class MigrationService : IMigrationService
{
    private const int BatchSize = 500;

    private readonly ILogger<MigrationService> _logger;

    // ── Definicja tabel dostępnych do migracji ─────────────────────────────
    private static readonly IReadOnlyList<TableInfo> AvailableTables =
    [
        // Główne
        new TableInfo { Name = "P_Pacjent",      DisplayName = "Pacjenci (P_Pacjent)",              Category = TableCategory.Glowne },
        new TableInfo { Name = "Firma",           DisplayName = "Firmy / kontrahenci (Firma)",        Category = TableCategory.Glowne },
        new TableInfo { Name = "Umowy_Firm",      DisplayName = "Umowy z firmami (Umowy_Firm)",       Category = TableCategory.Glowne },
        new TableInfo { Name = "BAD_Lista",       DisplayName = "Lista badań (BAD_Lista)",            Category = TableCategory.Glowne },
        new TableInfo { Name = "BAD_Cennik",      DisplayName = "Cennik badań (BAD_Cennik)",          Category = TableCategory.Glowne },
        new TableInfo { Name = "B_Skierowania",   DisplayName = "Skierowania (B_Skierowania)",        Category = TableCategory.Glowne },
        new TableInfo { Name = "Badanie",         DisplayName = "Wyniki badań (Badanie)",             Category = TableCategory.Glowne },
        new TableInfo { Name = "Faktura",         DisplayName = "Faktury (Faktura)",                  Category = TableCategory.Glowne },
        new TableInfo { Name = "Rejestracja",     DisplayName = "Rejestracja / wizyty (Rejestracja)", Category = TableCategory.Glowne },
        new TableInfo { Name = "ListyBadan",      DisplayName = "Listy badań (ListyBadan)",           Category = TableCategory.Glowne },
        new TableInfo { Name = "Users",           DisplayName = "Użytkownicy systemu (Users)",        Category = TableCategory.Glowne },
        new TableInfo { Name = "LoginHistory",    DisplayName = "Historia logowań (LoginHistory)",    Category = TableCategory.Glowne },
        // Słownikowe
        new TableInfo { Name = "S_Imiona",            DisplayName = "Słownik imion (S_Imiona)",               Category = TableCategory.Slownikowe },
        new TableInfo { Name = "S_Nazwisko",           DisplayName = "Słownik nazwisk (S_Nazwisko)",            Category = TableCategory.Slownikowe },
        new TableInfo { Name = "S__Ulice",             DisplayName = "Słownik ulic (S__Ulice)",                 Category = TableCategory.Slownikowe },
        new TableInfo { Name = "Gminy",                DisplayName = "Słownik gmin (Gminy)",                    Category = TableCategory.Slownikowe },
        new TableInfo { Name = "FormatowanieTekstu",   DisplayName = "Formatowanie tekstu (FormatowanieTekstu)", Category = TableCategory.Slownikowe },
        new TableInfo { Name = "S_hints",              DisplayName = "Podpowiedzi (S_hints)",                   Category = TableCategory.Slownikowe },
        // Pomocnicze
        new TableInfo { Name = "Daj_Bad",        DisplayName = "Pomocnicza: Daj_Bad",        Category = TableCategory.Pomocnicze },
        new TableInfo { Name = "PES_Import_GOV", DisplayName = "Import GOV: PES_Import_GOV", Category = TableCategory.Pomocnicze },
    ];

    public MigrationService(ILogger<MigrationService> logger)
    {
        _logger = logger;
    }

    // ── GetAvailableTables ─────────────────────────────────────────────────

    public IReadOnlyList<TableInfo> GetAvailableTables() => AvailableTables;

    // ── ScanAccessTablesAsync ─────────────────────────────────────────────

    public async Task<(bool Success, IReadOnlyList<string> TableNames, string Message)> ScanAccessTablesAsync(
        string accessFilePath)
    {
        if (string.IsNullOrWhiteSpace(accessFilePath) || !File.Exists(accessFilePath))
            return (false, [], $"Plik nie istnieje: {accessFilePath}");

        return await Task.Run(() =>
        {
            try
            {
                var cs = BuildAccessConnectionString(accessFilePath);
                using var conn = new OleDbConnection(cs);
                conn.Open();
                var schema = conn.GetSchema("Tables");
                var names = schema.Rows.Cast<DataRow>()
                    .Where(r =>
                    {
                        var t = r["TABLE_TYPE"]?.ToString() ?? "";
                        var n = r["TABLE_NAME"]?.ToString() ?? "";
                        return t == "TABLE"
                            && !n.StartsWith("MSys", StringComparison.OrdinalIgnoreCase)
                            && !n.StartsWith("~", StringComparison.Ordinal);
                    })
                    .Select(r => r["TABLE_NAME"]!.ToString()!)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return (true, (IReadOnlyList<string>)names,
                    $"Wykryto {names.Count} tabel użytkownika w bazie Access.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Błąd skanowania Access: {Path}", accessFilePath);
                return (false, (IReadOnlyList<string>)[], $"Błąd skanowania Access: {ex.Message}");
            }
        });
    }

    // 

    public async Task<(bool Success, string Message)> TestAccessConnectionAsync(string accessFilePath)
    {
        if (string.IsNullOrWhiteSpace(accessFilePath))
            return (false, "Ścieżka do pliku Access jest pusta.");

        if (!File.Exists(accessFilePath))
            return (false, $"Plik nie istnieje: {accessFilePath}");

        return await Task.Run(() =>
        {
            try
            {
                var cs = BuildAccessConnectionString(accessFilePath);
                using var conn = new OleDbConnection(cs);
                conn.Open();
                var tables = conn.GetSchema("Tables");
                int count = tables.Rows.Count;
                return (true, $"Połączono pomyślnie. Wykryto {count} tabel w bazie Access.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Błąd połączenia z Access: {Path}", accessFilePath);
                return (false, $"Błąd połączenia z Access: {ex.Message}");
            }
        });
    }

    // ── TestMySqlConnection ────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> TestMySqlConnectionAsync(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "Connection string MySQL jest pusty.");

        try
        {
            using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync();
            return (true, $"Połączono z MySQL: {conn.Database} @ {conn.DataSource}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Błąd połączenia z MySQL");
            return (false, $"Błąd połączenia z MySQL: {ex.Message}");
        }
    }

    // ── CreateBackup ───────────────────────────────────────────────────────

    public async Task<(bool Success, string BackupPath, string Message)> CreateBackupAsync(
        string connectionString,
        IEnumerable<string> tableNames,
        CancellationToken cancellationToken = default)
    {
        var backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ASMED.EDM", "Backups");

        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(backupDir, $"backup_before_migration_{timestamp}.sql");

        try
        {
            await using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine($"-- ASMED EDM -- kopia zapasowa przed migracją -- {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"-- Baza: {conn.Database} @ {conn.DataSource}");
            sb.AppendLine();
            sb.AppendLine("SET FOREIGN_KEY_CHECKS=0;");
            sb.AppendLine();

            foreach (var tableName in tableNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ExportTableToSqlAsync(conn, tableName, sb, cancellationToken);
            }

            sb.AppendLine("SET FOREIGN_KEY_CHECKS=1;");

            await File.WriteAllTextAsync(backupPath, sb.ToString(), Encoding.UTF8, cancellationToken);

            _logger.LogInformation("Kopia zapasowa zapisana: {Path}", backupPath);
            return (true, backupPath, $"Kopia zapasowa zapisana: {backupPath}");
        }
        catch (OperationCanceledException)
        {
            return (false, string.Empty, "Tworzenie kopii zapasowej anulowane.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd tworzenia kopii zapasowej");
            return (false, string.Empty, $"Błąd tworzenia kopii zapasowej: {ex.Message}");
        }
    }

    // ── MigrateTableAsync ──────────────────────────────────────────────────

    public async Task<(bool Success, int RowsMigrated, string Message)> MigrateTableAsync(
        string accessFilePath,
        string mysqlConnectionString,
        string tableName,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rozpoczynam migrację tabeli: {Table}", tableName);

        return await Task.Run(async () =>
        {
            try
            {
                var accessCs = BuildAccessConnectionString(accessFilePath);

                using var accessConn = new OleDbConnection(accessCs);
                accessConn.Open();

                // Sprawdź czy tabela istnieje w Access
                if (!AccessTableExists(accessConn, tableName))
                {
                    return (false, 0, $"Tabela '{tableName}' nie istnieje w bazie Access — pominięto.");
                }

                // Pobierz dane z Access
                var dataTable = ReadAccessTable(accessConn, tableName);
                int total = dataTable.Rows.Count;

                _logger.LogInformation("Tabela {Table}: {Count} wierszy do migracji", tableName, total);

                progress?.Report(new MigrationProgress
                {
                    CurrentTable = tableName,
                    TotalRows = total,
                    ProcessedRows = 0,
                    Message = $"Czyszczenie tabeli {tableName}..."
                });

                await using var mysqlConn = new MySqlConnection(mysqlConnectionString);
                await mysqlConn.OpenAsync(cancellationToken);

                // Wyłącz foreign keys i wyczyść tabelę
                await ExecuteMySqlCommandAsync(mysqlConn, "SET FOREIGN_KEY_CHECKS=0;", cancellationToken);
                await ExecuteMySqlCommandAsync(mysqlConn, $"DELETE FROM `{tableName}`;", cancellationToken);

                // Resetuj AUTO_INCREMENT
                try
                {
                    await ExecuteMySqlCommandAsync(mysqlConn, $"ALTER TABLE `{tableName}` AUTO_INCREMENT = 1;", cancellationToken);
                }
                catch { /* tabela może nie mieć AUTO_INCREMENT */ }

                int rowsMigrated = 0;

                if (total > 0)
                {
                    // Pobierz kolumny dostępne w obydwu bazach
                    var mysqlColumns = await GetMySqlColumnsAsync(mysqlConn, tableName, cancellationToken);
                    var accessColumns = dataTable.Columns.Cast<DataColumn>()
                        .Select(c => c.ColumnName)
                        .ToList();

                    var commonColumns = accessColumns
                        .Where(c => mysqlColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (commonColumns.Count == 0)
                    {
                        await ExecuteMySqlCommandAsync(mysqlConn, "SET FOREIGN_KEY_CHECKS=1;", cancellationToken);
                        return (false, 0, $"Brak wspólnych kolumn między Access a MySQL dla tabeli '{tableName}'.");
                    }

                    // Wstaw dane partiami
                    for (int i = 0; i < total; i += BatchSize)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var batch = dataTable.Rows.Cast<DataRow>()
                            .Skip(i)
                            .Take(BatchSize)
                            .ToList();

                        await InsertBatchAsync(mysqlConn, tableName, commonColumns, batch, cancellationToken);
                        rowsMigrated += batch.Count;

                        progress?.Report(new MigrationProgress
                        {
                            CurrentTable = tableName,
                            TotalRows = total,
                            ProcessedRows = rowsMigrated,
                            Message = $"{tableName}: {rowsMigrated}/{total} wierszy"
                        });
                    }
                }

                await ExecuteMySqlCommandAsync(mysqlConn, "SET FOREIGN_KEY_CHECKS=1;", cancellationToken);

                _logger.LogInformation("Migracja {Table} zakończona: {Count} wierszy", tableName, rowsMigrated);
                return (true, rowsMigrated, $"OK: {rowsMigrated} wierszy");
            }
            catch (OperationCanceledException)
            {
                return (false, 0, "Migracja anulowana.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd migracji tabeli {Table}", tableName);
                return (false, 0, $"BŁĄD: {ex.Message}");
            }
        }, cancellationToken);
    }

    // ── MigrateTablesAsync ─────────────────────────────────────────────────

    public async Task MigrateTablesAsync(
        string accessFilePath,
        string mysqlConnectionString,
        IEnumerable<string> tableNames,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var list = tableNames.ToList();
        int total = list.Count;

        for (int idx = 0; idx < total; idx++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableName = list[idx];

            progress?.Report(new MigrationProgress
            {
                CurrentTable = tableName,
                CurrentTableIndex = idx + 1,
                TotalTables = total,
                TotalRows = 0,
                ProcessedRows = 0,
                Message = $"[{idx + 1}/{total}] Migruję tabelę {tableName}..."
            });

            var (success, rows, message) = await MigrateTableAsync(
                accessFilePath, mysqlConnectionString, tableName, null, cancellationToken);

            progress?.Report(new MigrationProgress
            {
                CurrentTable = tableName,
                CurrentTableIndex = idx + 1,
                TotalTables = total,
                TotalRows = rows,
                ProcessedRows = rows,
                Message = $"[{idx + 1}/{total}] {tableName}: {message}",
                HasError = !success
            });
        }

        progress?.Report(new MigrationProgress
        {
            CurrentTable = string.Empty,
            CurrentTableIndex = total,
            TotalTables = total,
            TotalRows = 0,
            ProcessedRows = 0,
            Message = $"Migracja zakończona. Przetworzone tabele: {total}",
            IsCompleted = true
        });
    }

    // ── Metody pomocnicze ──────────────────────────────────────────────────

    private static string BuildAccessConnectionString(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var provider = ext == ".accdb"
            ? "Microsoft.ACE.OLEDB.16.0"
            : "Microsoft.ACE.OLEDB.12.0";
        return $"Provider={provider};Data Source={filePath};Persist Security Info=False;";
    }

    private static bool AccessTableExists(OleDbConnection conn, string tableName)
    {
        var tables = conn.GetSchema("Tables");
        return tables.Rows.Cast<DataRow>()
            .Any(r => string.Equals(r["TABLE_NAME"]?.ToString(), tableName, StringComparison.OrdinalIgnoreCase));
    }

    private static DataTable ReadAccessTable(OleDbConnection conn, string tableName)
    {
        using var cmd = new OleDbCommand($"SELECT * FROM [{tableName}]", conn);
        using var adapter = new OleDbDataAdapter(cmd);
        var dt = new DataTable();
        adapter.Fill(dt);
        return dt;
    }

    private static async Task<HashSet<string>> GetMySqlColumnsAsync(
        MySqlConnection conn, string tableName, CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sql = $"SELECT COLUMN_NAME FROM information_schema.COLUMNS " +
                  $"WHERE TABLE_SCHEMA = '{conn.Database}' AND TABLE_NAME = '{tableName}'";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            columns.Add(reader.GetString(0));
        return columns;
    }

    private static async Task InsertBatchAsync(
        MySqlConnection conn,
        string tableName,
        List<string> columns,
        List<DataRow> rows,
        CancellationToken ct)
    {
        if (rows.Count == 0) return;

        var colList = string.Join(", ", columns.Select(c => $"`{c}`"));
        var paramNames = columns.Select((c, i) => $"@p{i}").ToList();
        var valuePlaceholder = $"({string.Join(", ", paramNames)})";
        var allValues = string.Join(",\n", rows.Select(_ => valuePlaceholder));

        var sql = $"INSERT IGNORE INTO `{tableName}` ({colList}) VALUES {allValues}";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.CommandTimeout = 120;

        for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            for (int colIdx = 0; colIdx < columns.Count; colIdx++)
            {
                var paramName = $"@p{colIdx}";
                // Aby dla wielu wierszy nazwy parametrów były unikalne, dodajemy indeks wiersza
                // powyższy placeholder to nadpisujemy:
                cmd.Parameters.Add(new MySqlParameter($"@p{rowIdx}_{colIdx}", ConvertValue(rows[rowIdx][columns[colIdx]])));
            }
        }

        // Przebuduj SQL z unikalnymi nazwami parametrów
        var sb = new StringBuilder();
        sb.Append($"INSERT IGNORE INTO `{tableName}` ({colList}) VALUES ");

        var rowPlaceholders = new List<string>();
        for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var colPlaceholders = columns.Select((_, colIdx) => $"@p{rowIdx}_{colIdx}");
            rowPlaceholders.Add($"({string.Join(", ", colPlaceholders)})");
        }

        sb.Append(string.Join(",\n", rowPlaceholders));

        // Wyczyść parametry i użyj poprawnego SQL
        cmd.Parameters.Clear();
        cmd.CommandText = sb.ToString();

        for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            for (int colIdx = 0; colIdx < columns.Count; colIdx++)
            {
                cmd.Parameters.Add(new MySqlParameter(
                    $"@p{rowIdx}_{colIdx}",
                    ConvertValue(rows[rowIdx][columns[colIdx]])));
            }
        }

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static object ConvertValue(object value)
    {
        if (value is DBNull) return DBNull.Value;
        if (value is bool b) return b ? 1 : 0;
        if (value is DateTime dt)
        {
            // Access może przechowywać daty spoza zakresu MySQL
            if (dt < new DateTime(1000, 1, 1) || dt > new DateTime(9999, 12, 31))
                return DBNull.Value;
            return dt;
        }
        return value;
    }

    private static async Task ExecuteMySqlCommandAsync(
        MySqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new MySqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task ExportTableToSqlAsync(
        MySqlConnection conn, string tableName, StringBuilder sb, CancellationToken ct)
    {
        try
        {
            sb.AppendLine($"-- Tabela: {tableName}");
            sb.AppendLine($"DELETE FROM `{tableName}`;");

            var sql = $"SELECT * FROM `{tableName}`";
            await using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 60 };
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.GetName(i))
                .ToList();

            var colList = string.Join(", ", columns.Select(c => $"`{c}`"));

            while (await reader.ReadAsync(ct))
            {
                var values = columns.Select((_, i) =>
                {
                    var val = reader.GetValue(i);
                    if (val is DBNull) return "NULL";
                    if (val is bool bv) return bv ? "1" : "0";
                    if (val is DateTime dtv) return $"'{dtv:yyyy-MM-dd HH:mm:ss}'";
                    if (val is string sv) return $"'{sv.Replace("'", "''")}'";
                    return val.ToString() ?? "NULL";
                });

                sb.AppendLine($"INSERT INTO `{tableName}` ({colList}) VALUES ({string.Join(", ", values)});");
            }

            sb.AppendLine();
        }
        catch (Exception ex)
        {
                            sb.AppendLine($"-- BŁĄD eksportu tabeli {tableName}: {ex.Message}");
                        sb.AppendLine();
                    }
                }

                // ── VerifyRowCountsAsync ─────────────────────────────────────────────────

                public async Task<IReadOnlyList<TableVerificationResult>> VerifyRowCountsAsync(
                    string accessFilePath,
                    string mysqlConnectionString,
                    IEnumerable<string> tableNames,
                    CancellationToken cancellationToken = default)
                {
                    var results = new List<TableVerificationResult>();
                    var tables  = tableNames.ToList();

                    var accessCs = BuildAccessConnectionString(accessFilePath);

                    await using var mysqlConn = new MySqlConnection(mysqlConnectionString);
                    await mysqlConn.OpenAsync(cancellationToken);

                    using var accessConn = new OleDbConnection(accessCs);
                    accessConn.Open();

                    foreach (var table in tables)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int accessCount = 0;
                        int mysqlCount  = 0;

                        try
                        {
                            using var cmd = new OleDbCommand($"SELECT COUNT(*) FROM [{table}]", accessConn);
                            var val = cmd.ExecuteScalar();
                            accessCount = val is DBNull || val is null ? 0 : Convert.ToInt32(val);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Nie można policzyć rekordów Access dla tabeli {Table}", table);
                            accessCount = -1;
                        }

                        try
                        {
                            await using var cmd = new MySqlCommand($"SELECT COUNT(*) FROM `{table}`", mysqlConn);
                            var val = await cmd.ExecuteScalarAsync(cancellationToken);
                            mysqlCount = val is DBNull || val is null ? 0 : Convert.ToInt32(val);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Nie można policzyć rekordów MySQL dla tabeli {Table}", table);
                            mysqlCount = -1;
                        }

                        results.Add(new TableVerificationResult
                        {
                            TableName   = table,
                            AccessCount = accessCount,
                            MySqlCount  = mysqlCount
                        });
                    }

                    return results;
                }

                // ── RestoreBackupAsync ───────────────────────────────────────────────────

                public async Task<(bool Success, int StatementsExecuted, string Message)> RestoreBackupAsync(
                    string backupFilePath,
        string targetConnectionString,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFilePath))
            return (false, 0, $"Plik kopii nie istnieje: {backupFilePath}");

        try
        {
            var sql = await File.ReadAllTextAsync(backupFilePath, Encoding.UTF8, cancellationToken);

            // Podziel skrypt na pojedyncze polecenia wg średnika
            var statements = sql
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0 && !s.StartsWith("--"))
                .ToList();

            _logger.LogInformation("Przywracanie kopii z {Path}: {Count} poleceń", backupFilePath, statements.Count);
            progress?.Report($"Wczytano {statements.Count} poleceń SQL z pliku kopii...");

            await using var conn = new MySqlConnection(targetConnectionString);
            await conn.OpenAsync(cancellationToken);

            int executed = 0;
            await using var disableFk = new MySqlCommand("SET FOREIGN_KEY_CHECKS=0;", conn);
            await disableFk.ExecuteNonQueryAsync(cancellationToken);

            foreach (var stmt in statements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await using var cmd = new MySqlCommand(stmt, conn);
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                    executed++;

                    if (executed % 500 == 0)
                        progress?.Report($"Wykonano {executed}/{statements.Count} poleceń...");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Błąd wykonywania polecenia SQL (pominięto): {Stmt}",
                        stmt.Length > 80 ? stmt[..80] + "..." : stmt);
                }
            }

            await using var enableFk = new MySqlCommand("SET FOREIGN_KEY_CHECKS=1;", conn);
            await enableFk.ExecuteNonQueryAsync(cancellationToken);

            var msg = $"Przywracanie zakończone. Wykonano {executed}/{statements.Count} poleceń.";
            _logger.LogInformation(msg);
            return (true, executed, msg);
        }
        catch (OperationCanceledException)
        {
            return (false, 0, "Przywracanie anulowane.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd przywracania kopii zapasowej");
            return (false, 0, $"Błąd przywracania: {ex.Message}");
        }
    }

    // ── CopyMySqlDatabaseAsync ───────────────────────────────────────────────

    public async Task<(bool Success, int TablesCopied, string Message)> CopyMySqlDatabaseAsync(
        string sourceConnectionString,
        string targetConnectionString,
        IEnumerable<string>? tableNames = null,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var srcConn = new MySqlConnection(sourceConnectionString);
            await srcConn.OpenAsync(cancellationToken);

            await using var dstConn = new MySqlConnection(targetConnectionString);
            await dstConn.OpenAsync(cancellationToken);

            // Ustal listę tabel — podana lub wszystkie ze źródła
            List<string> tables;
            if (tableNames is not null)
            {
                tables = tableNames.ToList();
            }
            else
            {
                tables = [];
                await using var listCmd = new MySqlCommand("SHOW TABLES;", srcConn);
                await using var reader = await listCmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    tables.Add(reader.GetString(0));
            }

            _logger.LogInformation("Kopiowanie bazy MySQL: {Src} → {Dst}, tabel: {Count}",
                srcConn.Database, dstConn.Database, tables.Count);

            await using var disableFk = new MySqlCommand("SET FOREIGN_KEY_CHECKS=0;", dstConn);
            await disableFk.ExecuteNonQueryAsync(cancellationToken);

            int copied = 0;

            for (int i = 0; i < tables.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tableName = tables[i];

                progress?.Report(new MigrationProgress
                {
                    CurrentTable       = tableName,
                    CurrentTableIndex  = i + 1,
                    TotalTables        = tables.Count,
                    Message            = $"Kopiowanie tabeli {tableName} ({i + 1}/{tables.Count})..."
                });

                try
                {
                    // Pobierz dane ze źródła
                    await using var selCmd = new MySqlCommand($"SELECT * FROM `{tableName}`;", srcConn);
                    await using var reader = await selCmd.ExecuteReaderAsync(cancellationToken);

                    var dt = new System.Data.DataTable();
                    dt.Load(reader);

                    // Wyczyść tabelę docelową
                    await using var truncCmd = new MySqlCommand(
                        $"DELETE FROM `{tableName}`;", dstConn);
                    await truncCmd.ExecuteNonQueryAsync(cancellationToken);

                    // Wstaw wiersze partiami
                    if (dt.Rows.Count > 0)
                    {
                        var columns = dt.Columns.Cast<System.Data.DataColumn>()
                                                .Select(c => c.ColumnName)
                                                .ToList();
                        var rows = dt.Rows.Cast<System.Data.DataRow>().ToList();

                        for (int offset = 0; offset < rows.Count; offset += BatchSize)
                        {
                            var batch = rows.Skip(offset).Take(BatchSize).ToList();
                            await InsertBatchAsync(dstConn, tableName, columns, batch, cancellationToken);
                        }
                    }

                    copied++;
                    _logger.LogInformation("Skopiowano tabelę {Table}: {Count} wierszy",
                        tableName, dt.Rows.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Błąd kopiowania tabeli {Table} — pominięto", tableName);
                }
            }

            await using var enableFk = new MySqlCommand("SET FOREIGN_KEY_CHECKS=1;", dstConn);
            await enableFk.ExecuteNonQueryAsync(cancellationToken);

            var msg = $"Kopiowanie zakończone. Skopiowano {copied}/{tables.Count} tabel.";
            return (true, copied, msg);
        }
        catch (OperationCanceledException)
        {
            return (false, 0, "Kopiowanie bazy anulowane.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd kopiowania bazy MySQL");
            return (false, 0, $"Błąd kopiowania bazy: {ex.Message}");
        }
    }
}
