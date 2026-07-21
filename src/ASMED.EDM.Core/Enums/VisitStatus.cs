namespace ASMED.EDM.Core.Enums;

/// <summary>
/// Status wizyty lekarskiej
/// </summary>
public enum VisitStatus
{
    /// <summary>
    /// Wizyta zaplanowana
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Pacjent przyszedł na wizytę
    /// </summary>
    CheckedIn = 1,

    /// <summary>
    /// Wizyta w trakcie
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Wizyta zakończona
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Wizyta odwołana
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Pacjent się nie pojawił
    /// </summary>
    NoShow = 5
}
