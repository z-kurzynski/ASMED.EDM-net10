namespace ASMED.EDM.Data.Models;

/// <summary>
/// Model postępu operacji migracji danych z Access do MySQL
/// </summary>
public class MigrationProgress
{
    /// <summary>
    /// Aktualnie migrowana tabela
    /// </summary>
    public string CurrentTable { get; init; } = string.Empty;

    /// <summary>
    /// Liczba przetworzonych wierszy w bieżącej tabeli
    /// </summary>
    public int ProcessedRows { get; init; }

    /// <summary>
    /// Łączna liczba wierszy w bieżącej tabeli
    /// </summary>
    public int TotalRows { get; init; }

    /// <summary>
    /// Numer aktualnie migrowanej tabeli (1-based)
    /// </summary>
    public int CurrentTableIndex { get; init; }

    /// <summary>
    /// Łączna liczba tabel do zmigrowania
    /// </summary>
    public int TotalTables { get; init; }

    /// <summary>
    /// Komunikat dla użytkownika
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Czy migracja całości została zakończona
    /// </summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// Czy wystąpił błąd
    /// </summary>
    public bool HasError { get; init; }

    /// <summary>
    /// Postęp tabeli 0-100 (procent)
    /// </summary>
    public double TableProgress => TotalRows > 0
        ? Math.Min(100.0 * ProcessedRows / TotalRows, 100.0)
        : 0;

    /// <summary>
    /// Postęp całkowity 0-100 (procent)
    /// </summary>
    public double OverallProgress => TotalTables > 0
        ? Math.Min(100.0 * (CurrentTableIndex - 1 + TableProgress / 100.0) / TotalTables, 100.0)
        : 0;
}
