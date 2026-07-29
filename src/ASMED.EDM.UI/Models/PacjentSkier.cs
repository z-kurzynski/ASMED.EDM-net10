namespace ASMED.EDM.UI.Models;

/// <summary>
/// Model DTO dla listy pacjentów w zakładce Skierowania
/// </summary>
public class PacjentSkier
{
    public int LineNumber { get; set; }
    public int P_ID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PESEL { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public int LiczbaKartBadan { get; set; }

    /// <summary>
    /// Imię i nazwisko razem (pomocnicze do filtrowania)
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
