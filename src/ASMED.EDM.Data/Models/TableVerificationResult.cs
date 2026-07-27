namespace ASMED.EDM.Data.Models;

/// <summary>
/// Wynik weryfikacji zgodności liczby rekordów między Access a MySQL dla jednej tabeli.
/// </summary>
public class TableVerificationResult
{
    public string TableName    { get; init; } = string.Empty;
    public int    AccessCount  { get; init; }
    public int    MySqlCount   { get; init; }
    public bool   IsMatch      => AccessCount == MySqlCount;
    public string StatusIcon   => IsMatch ? "✅" : "⚠️";

    /// <summary>Różnica MySql − Access (ujemna = brakuje rekordów w MySQL).</summary>
    public int Diff => MySqlCount - AccessCount;

    public string DiffText => Diff == 0 ? "—" : (Diff > 0 ? $"+{Diff}" : $"{Diff}");
}
