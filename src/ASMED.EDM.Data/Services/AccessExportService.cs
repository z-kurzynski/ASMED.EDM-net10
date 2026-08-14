using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Text.Json;
using ASMED.EDM.Data.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Runtime.Versioning;

namespace ASMED.EDM.Data.Services;

/// <summary>
/// Serwis eksportu danych MySQL → Access.
/// Tabele legacy (P_Pacjent, Firma, Badanie, Faktura, Rejestracja, ListyBadan)
/// mają identyczny schemat w MySQL i Access — eksport nie wymaga mapowania kolumn.
/// Strategia: UPSERT-ALL (UPDATE gdy PK istnieje, INSERT gdy brak) + DELETE nieobecnych.
/// </summary>
[SupportedOSPlatform("windows")]
public class AccessExportService : IAccessExportService
{
    private const int BatchSize = 200;

    private readonly ILogger<AccessExportService> _logger;

    // ── Ścieżka do pliku stanu na dysku ─────────────────────────────────────────
    private static readonly string StateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ASMED.EDM", "access_export_state.json");

    // ── Definicja tabel dostępnych do eksportu ───────────────────────────────────
    // Klucz główny każdej tabeli musi być zgodny z nazwą kolumny PK w Access i MySQL.
    private static readonly IReadOnlyList<ExportTableInfo> ExportableTables =
    [
        new ExportTableInfo { Name = "P_Pacjent",  PrimaryKey = "P_ID",         DisplayName = "Pacjenci (P_Pacjent)"          },
        new ExportTableInfo { Name = "Firma",       PrimaryKey = "id",           DisplayName = "Firmy / kontrahenci (Firma)"   },
        new ExportTableInfo { Name = "Badanie",     PrimaryKey = "Bad_ID",       DisplayName = "Wyniki badań (Badanie)"        },
        new ExportTableInfo { Name = "Faktura",     PrimaryKey = "FK_ID",        DisplayName = "Faktury (Faktura)"             },
        new ExportTableInfo { Name = "Rejestracja", PrimaryKey = "R_ID",         DisplayName = "Rejestracja / wizyty"          },
        new ExportTableInfo { Name = "ListyBadan",  PrimaryKey = "Identyfikator",DisplayName = "Listy badań (ListyBadan)"      },
    ];

    public AccessExportService(ILogger<AccessExportService> logger)
    {
        _logger = logger;
    }

    // ── GetExportableTables ──────────────────────────────────────────────────────

    public IReadOnlyList<ExportTableInfo> GetExportableTables() => ExportableTables;

    // ── TestAccessConnection ─────────────────────────────────────────────────────

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
                var schema = conn.GetSchema("Tables");
                int count = schema.Rows.Count;
                return (true, $"Połączono pomyślnie. Wykryto {count} tabel w bazie Access.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Błąd połączenia z Access: {Path}", accessFilePath);
                return (false, $"Błąd połączenia z Access: {ex.Message}");
            }
        });
    }

    // ── ExportToAccessAsync ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ExportTableResult>> ExportToAccessAsync(
        string mysqlConnectionString,
        string accessFilePath,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await ExportTablesToAccessAsync(
            ExportableTables.Select(t => t.Name),
            mysqlConnectionString,
            accessFilePath,
            progress,
            cancellationToken);
    }

    // ── ExportTablesToAccessAsync ────────────────────────────────────────────────

    public async Task<IReadOnlyList<ExportTableResult>> ExportTablesToAccessAsync(
        IEnumerable<string> tableNames,
        string mysqlConnectionString,
        string accessFilePath,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var list = tableNames.ToList();
        int total = list.Count;
        var results = new List<ExportTableResult>(total);

        for (int idx = 0; idx < total; idx++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tableName = list[idx];
            progress?.Report(new ExportProgress
            {
                CurrentTable = tableName,
                CurrentTableIndex = idx + 1,
                TotalTables = total,
                TotalRows = 0,
                ProcessedRows = 0,
                Message = $"Przygotowanie eksportu tabeli {tableName}..."
            });

            var tableProgress = progress == null ? null : new Progress<ExportProgress>(p =>
                progress.Report(new ExportProgress
                {
                    CurrentTable      = p.CurrentTable,
                    CurrentTableIndex = idx + 1,
                    TotalTables       = total,
                    TotalRows         = p.TotalRows,
                    ProcessedRows     = p.ProcessedRows,
                    Message           = p.Message
                }));

            var result = await ExportTableToAccessAsync(
                tableName, mysqlConnectionString, accessFilePath,
                tableProgress, cancellationToken);

            results.Add(result);
            _logger.LogInformation("Eksport tabeli {Table}: {Summary}", tableName, result.Summary);
        }

        return results;
    }

    // ── ExportTableToAccessAsync ─────────────────────────────────────────────────

    public async Task<ExportTableResult> ExportTableToAccessAsync(
        string tableName,
        string mysqlConnectionString,
        string accessFilePath,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rozpoczynam eksport tabeli: {Table}", tableName);

        var tableInfo = ExportableTables.FirstOrDefault(t => t.Name == tableName);
        if (tableInfo == null)
            return Fail(tableName, $"Tabela '{tableName}' nie jest zdefiniowana do eksportu.");

        return await Task.Run(async () =>
        {
            try
            {
                // 1. Pobierz dane z MySQL
                await using var mysqlConn = new MySqlConnection(mysqlConnectionString);
                await mysqlConn.OpenAsync(cancellationToken);

                var mysqlData = await ReadMySqlTableAsync(mysqlConn, tableName, cancellationToken);
                int totalRows = mysqlData.Rows.Count;

                _logger.LogInformation("Tabela {Table}: {Count} wierszy w MySQL", tableName, totalRows);

                progress?.Report(new ExportProgress
                {
                    CurrentTable = tableName,
                    TotalRows = totalRows,
                    ProcessedRows = 0,
                    Message = $"Otwieranie połączenia z Access..."
                });

                // 2. Otwórz połączenie z Access
                var accessCs = BuildAccessConnectionString(accessFilePath);
                using var accessConn = new OleDbConnection(accessCs);
                accessConn.Open();

                // 3. Sprawdź czy tabela istnieje w Access
                if (!AccessTableExists(accessConn, tableName))
                    return Fail(tableName, $"Tabela '{tableName}' nie istnieje w pliku Access.");

                // 4. Pobierz kolumny dostępne w Access (schemat)
                var accessColumns = GetAccessColumns(accessConn, tableName);
                if (accessColumns.Count == 0)
                    return Fail(tableName, $"Brak kolumn w tabeli Access '{tableName}'.");

                // 5. Wybierz wspólne kolumny MySQL ∩ Access
                var mysqlColumns = mysqlData.Columns.Cast<DataColumn>()
                    .Select(c => c.ColumnName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var commonColumns = accessColumns
                    .Where(c => mysqlColumns.Contains(c))
                    .ToList();

                if (commonColumns.Count == 0)
                    return Fail(tableName, $"Brak wspólnych kolumn między MySQL a Access dla tabeli '{tableName}'.");

                // 6. Pobierz zbiór PK istniejących w Access
                var accessPks = GetAccessPrimaryKeys(accessConn, tableName, tableInfo.PrimaryKey);

                // 7. Zbierz PK z MySQL (do usunięcia z Access)
                var mysqlPks = new HashSet<object>();
                foreach (DataRow row in mysqlData.Rows)
                {
                    var pkVal = row[tableInfo.PrimaryKey];
                    if (pkVal != DBNull.Value)
                        mysqlPks.Add(pkVal);
                }

                int inserted = 0, updated = 0, deleted = 0;
                int processed = 0;

                // 8. UPSERT — w transakcji per tabela
                using var transaction = accessConn.BeginTransaction();
                try
                {
                    // INSERT / UPDATE
                    foreach (DataRow row in mysqlData.Rows)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var pkVal = row[tableInfo.PrimaryKey];
                        bool exists = pkVal != DBNull.Value && accessPks.Contains(pkVal);

                        if (exists)
                        {
                            UpdateAccessRow(accessConn, transaction, tableName,
                                tableInfo.PrimaryKey, pkVal, row, commonColumns);
                            updated++;
                        }
                        else
                        {
                            InsertAccessRow(accessConn, transaction, tableName,
                                row, commonColumns);
                            inserted++;
                        }

                        processed++;

                        if (processed % BatchSize == 0)
                        {
                            progress?.Report(new ExportProgress
                            {
                                CurrentTable = tableName,
                                TotalRows = totalRows,
                                ProcessedRows = processed,
                                Message = $"{tableName}: {processed}/{totalRows} wierszy..."
                            });
                        }
                    }

                    // DELETE — rekordy w Access, których brak w MySQL
                    var toDelete = accessPks.Where(pk => !mysqlPks.Contains(pk)).ToList();
                    foreach (var pk in toDelete)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        DeleteAccessRow(accessConn, transaction, tableName, tableInfo.PrimaryKey, pk);
                        deleted++;
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                progress?.Report(new ExportProgress
                {
                    CurrentTable = tableName,
                    TotalRows = totalRows,
                    ProcessedRows = totalRows,
                    Message = $"{tableName}: zakończono."
                });

                return new ExportTableResult
                {
                    TableName = tableName,
                    Success = true,
                    TotalMySql = totalRows,
                    Inserted = inserted,
                    Updated = updated,
                    Deleted = deleted,
                    Message = "OK"
                };
            }
            catch (OperationCanceledException)
            {
                return Fail(tableName, "Eksport anulowany.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd eksportu tabeli {Table}", tableName);
                return Fail(tableName, ex.Message);
            }
        }, cancellationToken);
    }

    // ── GetExportState / SaveExportState ─────────────────────────────────────────

    public ExportState GetExportState()
    {
        try
        {
            if (!File.Exists(StateFilePath))
                return new ExportState();

            var json = File.ReadAllText(StateFilePath);
            return JsonSerializer.Deserialize<ExportState>(json) ?? new ExportState();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nie można odczytać stanu eksportu z: {Path}", StateFilePath);
            return new ExportState();
        }
    }

    public void SaveExportState(ExportState state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StateFilePath)!);
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StateFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nie można zapisać stanu eksportu do: {Path}", StateFilePath);
        }
    }

    // ── Pomocnicze metody prywatne ───────────────────────────────────────────────

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
        var schema = conn.GetSchema("Tables");
        return schema.Rows.Cast<DataRow>()
            .Any(r => string.Equals(r["TABLE_NAME"]?.ToString(), tableName, StringComparison.OrdinalIgnoreCase)
                   && r["TABLE_TYPE"]?.ToString() == "TABLE");
    }

    private static List<string> GetAccessColumns(OleDbConnection conn, string tableName)
    {
        var restrictions = new string?[] { null, null, tableName, null };
        var schema = conn.GetSchema("Columns", restrictions);
        return schema.Rows.Cast<DataRow>()
            .OrderBy(r => Convert.ToInt32(r["ORDINAL_POSITION"]))
            .Select(r => r["COLUMN_NAME"]!.ToString()!)
            .ToList();
    }

    private static HashSet<object> GetAccessPrimaryKeys(OleDbConnection conn, string tableName, string pkColumn)
    {
        var set = new HashSet<object>();
        using var cmd = new OleDbCommand($"SELECT [{pkColumn}] FROM [{tableName}]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var val = reader[0];
            if (val != DBNull.Value)
                set.Add(val);
        }
        return set;
    }

    private static async Task<DataTable> ReadMySqlTableAsync(
        MySqlConnection conn, string tableName, CancellationToken ct)
    {
        await using var cmd = new MySqlCommand($"SELECT * FROM `{tableName}`", conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var dt = new DataTable(tableName);
        dt.Load(reader);
        return dt;
    }

    private static void UpdateAccessRow(
        OleDbConnection conn,
        OleDbTransaction transaction,
        string tableName,
        string pkColumn,
        object pkValue,
        DataRow row,
        List<string> columns)
    {
        // Aktualizuj wszystkie kolumny oprócz PK
        var updateCols = columns.Where(c => !c.Equals(pkColumn, StringComparison.OrdinalIgnoreCase)).ToList();
        if (updateCols.Count == 0) return;

        var setClauses = string.Join(", ", updateCols.Select(c => $"[{c}] = ?"));
        var sql = $"UPDATE [{tableName}] SET {setClauses} WHERE [{pkColumn}] = ?";

        using var cmd = new OleDbCommand(sql, conn, transaction);
        foreach (var col in updateCols)
        {
            var val = row[col];
            cmd.Parameters.AddWithValue("?", val == DBNull.Value ? DBNull.Value : val);
        }
        cmd.Parameters.AddWithValue("?", pkValue);
        cmd.ExecuteNonQuery();
    }

    private static void InsertAccessRow(
        OleDbConnection conn,
        OleDbTransaction transaction,
        string tableName,
        DataRow row,
        List<string> columns)
    {
        var colList = string.Join(", ", columns.Select(c => $"[{c}]"));
        var paramList = string.Join(", ", columns.Select(_ => "?"));
        var sql = $"INSERT INTO [{tableName}] ({colList}) VALUES ({paramList})";

        using var cmd = new OleDbCommand(sql, conn, transaction);
        foreach (var col in columns)
        {
            var val = row[col];
            cmd.Parameters.AddWithValue("?", val == DBNull.Value ? DBNull.Value : val);
        }
        cmd.ExecuteNonQuery();
    }

    private static void DeleteAccessRow(
        OleDbConnection conn,
        OleDbTransaction transaction,
        string tableName,
        string pkColumn,
        object pkValue)
    {
        var sql = $"DELETE FROM [{tableName}] WHERE [{pkColumn}] = ?";
        using var cmd = new OleDbCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("?", pkValue);
        cmd.ExecuteNonQuery();
    }

    private static ExportTableResult Fail(string tableName, string message) =>
        new() { TableName = tableName, Success = false, Message = message };
}
