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
    /// Najwyższa data ważności badania (MAX Bad_Data_Do) ze wszystkich skierowań pacjenta.
    /// </summary>
    public DateTime? BadWazneDo { get; set; }

    /// <summary>
    /// Identyfikator pacjenta w Rejestrze Pacjentów P1 (P_Pacjent.P_CWUB_ID).
    /// </summary>
    public string? P_CWUB_ID { get; set; }

    public bool CzyZweryfikowanoP1 => !string.IsNullOrWhiteSpace(P_CWUB_ID);

    public string WeryfikacjaP1ToolTip => CzyZweryfikowanoP1
        ? $"Pacjent zweryfikowany w P1 (CWUB ID: {P_CWUB_ID})"
        : "Brak weryfikacji pacjenta w P1";

    /// <summary>
    /// Imię i nazwisko razem (pomocnicze do filtrowania)
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
