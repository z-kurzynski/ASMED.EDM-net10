using ASMED.EDM.Data.Models;

namespace ASMED.EDM.Data.Services;

/// <summary>
/// Interfejs serwisu eksportu danych MySQL → Access.
/// Umożliwia synchronizację danych z bazy MySQL do pliku Access
/// dla tabel ze wspólnym schematem (P_Pacjent, Firma, Badanie, Faktura, Rejestracja, ListyBadan).
/// </summary>
public interface IAccessExportService
{
    /// <summary>
    /// Zwraca listę tabel dostępnych do eksportu MySQL → Access
    /// </summary>
    IReadOnlyList<ExportTableInfo> GetExportableTables();

    /// <summary>
    /// Testuje połączenie z plikiem Access pod wskazaną ścieżką
    /// </summary>
    Task<(bool Success, string Message)> TestAccessConnectionAsync(string accessFilePath);

    /// <summary>
    /// Eksportuje wszystkie tabele z MySQL do pliku Access (pełny UPSERT).
    /// Dla każdego rekordu: UPDATE jeśli PK istnieje w Access, INSERT jeśli nie.
    /// Rekordy nieobecne w MySQL są usuwane z Access.
    /// </summary>
    Task<IReadOnlyList<ExportTableResult>> ExportToAccessAsync(
        string mysqlConnectionString,
        string accessFilePath,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Eksportuje pojedynczą tabelę z MySQL do Access (pełny UPSERT)
    /// </summary>
    Task<ExportTableResult> ExportTableToAccessAsync(
        string tableName,
        string mysqlConnectionString,
        string accessFilePath,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Eksportuje wskazany zestaw tabel sekwencyjnie
    /// </summary>
    Task<IReadOnlyList<ExportTableResult>> ExportTablesToAccessAsync(
        IEnumerable<string> tableNames,
        string mysqlConnectionString,
        string accessFilePath,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca stan ostatniego eksportu (data, ścieżka)
    /// </summary>
    ExportState GetExportState();

    /// <summary>
    /// Zapisuje stan eksportu (wywoływane po zakończonym eksporcie)
    /// </summary>
    void SaveExportState(ExportState state);
}
