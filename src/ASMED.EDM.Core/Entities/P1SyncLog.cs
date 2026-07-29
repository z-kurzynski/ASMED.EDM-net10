using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Log operacji synchronizacji z systemem P1 (Zdarzenia Medyczne, indeksacja EDM, weryfikacja PESEL)
/// </summary>
public class P1SyncLog : BaseEntity
{
    /// <summary>
    /// Typ operacji (np. RegisterVisit, UpdateVisit, CancelVisit, IndexDocument, VerifyPatient)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Nazwa encji, której dotyczy operacja (Visit, EdmDocument, Patient)
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Identyfikator lokalnej encji
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Status operacji
    /// </summary>
    public P1SyncStatus Status { get; set; }

    /// <summary>
    /// Identyfikator nadany przez P1 (jeśli operacja się powiodła)
    /// </summary>
    public string? P1Identifier { get; set; }

    /// <summary>
    /// Komunikat błędu (bez danych wrażliwych)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Liczba wykonanych prób
    /// </summary>
    public int AttemptCount { get; set; } = 1;

    /// <summary>
    /// Data ostatniej próby
    /// </summary>
    public DateTime LastAttemptAt { get; set; } = DateTime.UtcNow;
}
