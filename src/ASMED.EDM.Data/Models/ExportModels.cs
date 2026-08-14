namespace ASMED.EDM.Data.Models;

/// <summary>
/// Informacja o tabeli dostępnej do eksportu MySQL → Access
/// </summary>
public class ExportTableInfo
{
    /// <summary>Nazwa tabeli (identyczna w MySQL i Access)</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Nazwa kolumny klucza głównego w tej tabeli</summary>
    public string PrimaryKey { get; init; } = string.Empty;

    /// <summary>Opis wyświetlany użytkownikowi</summary>
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// Postęp eksportu pojedynczej tabeli
/// </summary>
public class ExportProgress
{
    /// <summary>Aktualnie eksportowana tabela</summary>
    public string CurrentTable { get; init; } = string.Empty;

    /// <summary>Numer tabeli w kolejce (1-based)</summary>
    public int CurrentTableIndex { get; init; }

    /// <summary>Łączna liczba tabel do eksportu</summary>
    public int TotalTables { get; init; }

    /// <summary>Liczba przetworzonych wierszy w bieżącej tabeli</summary>
    public int ProcessedRows { get; init; }

    /// <summary>Łączna liczba wierszy w bieżącej tabeli</summary>
    public int TotalRows { get; init; }

    /// <summary>Komunikat opisujący bieżącą czynność</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Postęp ogólny [0..1]</summary>
    public double OverallFraction =>
        TotalTables == 0 ? 0 :
        ((CurrentTableIndex - 1) + (TotalRows == 0 ? 0 : (double)ProcessedRows / TotalRows))
        / TotalTables;
}

/// <summary>
/// Wynik eksportu pojedynczej tabeli
/// </summary>
public class ExportTableResult
{
    /// <summary>Nazwa tabeli</summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>Eksport zakończony sukcesem</summary>
    public bool Success { get; init; }

    /// <summary>Liczba wierszy zaktualizowanych (UPDATE)</summary>
    public int Updated { get; init; }

    /// <summary>Liczba wierszy wstawionych (INSERT)</summary>
    public int Inserted { get; init; }

    /// <summary>Liczba wierszy usuniętych z Access (DELETE)</summary>
    public int Deleted { get; init; }

    /// <summary>Łączna liczba rekordów w MySQL</summary>
    public int TotalMySql { get; init; }

    /// <summary>Komunikat (OK lub opis błędu)</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Czytelne podsumowanie dla UI</summary>
    public string Summary =>
        Success
            ? $"{TableName}: {TotalMySql} rek. ({Inserted} nowych, {Updated} zmienionych, {Deleted} usuniętych)"
            : $"{TableName}: BŁĄD — {Message}";

    /// <summary>Ikona statusu dla UI</summary>
    public string StatusIcon => Success ? "✔" : "✖";
}

/// <summary>
/// Stan eksportu zapisywany na dysku (JSON)
/// </summary>
public class ExportState
{
    /// <summary>Data i godzina ostatniego eksportu (null = nigdy)</summary>
    public DateTime? LastExportAt { get; set; }

    /// <summary>Ostatnio używana ścieżka do pliku Access</summary>
    public string LastAccessPath { get; set; } = string.Empty;
}
