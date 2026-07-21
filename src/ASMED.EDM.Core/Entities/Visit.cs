using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Encja reprezentująca wizytę lekarską
/// </summary>
public class Visit : BaseEntity
{
    /// <summary>
    /// Identyfikator pacjenta
    /// </summary>
    public int PatientId { get; set; }

    /// <summary>
    /// Navigation property do pacjenta
    /// </summary>
    public virtual Patient Patient { get; set; } = null!;

    /// <summary>
    /// Identyfikator lekarza
    /// </summary>
    public int DoctorId { get; set; }

    /// <summary>
    /// Navigation property do lekarza
    /// </summary>
    public virtual Doctor Doctor { get; set; } = null!;

    /// <summary>
    /// Data i godzina wizyty
    /// </summary>
    public DateTime ScheduledDateTime { get; set; }

    /// <summary>
    /// Rzeczywista data rozpoczęcia wizyty
    /// </summary>
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// Rzeczywista data zakończenia wizyty
    /// </summary>
    public DateTime? ActualEndTime { get; set; }

    /// <summary>
    /// Planowany czas trwania (w minutach)
    /// </summary>
    public int DurationMinutes { get; set; } = 30;

    /// <summary>
    /// Status wizyty
    /// </summary>
    public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

    /// <summary>
    /// Typ wizyty (badanie, kontrola, konsultacja)
    /// </summary>
    public string? VisitType { get; set; }

    /// <summary>
    /// Powód wizyty
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Dolegliwości zgłaszane przez pacjenta
    /// </summary>
    public string? Symptoms { get; set; }

    /// <summary>
    /// Rozpoznanie (diagnoza)
    /// </summary>
    public string? Diagnosis { get; set; }

    /// <summary>
    /// Przepisane leki i leczenie
    /// </summary>
    public string? Treatment { get; set; }

    /// <summary>
    /// Zalecenia dla pacjenta
    /// </summary>
    public string? Recommendations { get; set; }

    /// <summary>
    /// Notatki lekarza
    /// </summary>
    public string? DoctorNotes { get; set; }

    /// <summary>
    /// Koszt wizyty
    /// </summary>
    public decimal? VisitCost { get; set; }

    /// <summary>
    /// Czy wizyta została opłacona
    /// </summary>
    public bool IsPaid { get; set; } = false;

    /// <summary>
    /// Metoda płatności
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Data płatności
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Czy pacjent potrzebuje kolejnej wizyty
    /// </summary>
    public bool RequiresFollowUp { get; set; } = false;

    /// <summary>
    /// Sugerowana data kolejnej wizyty
    /// </summary>
    public DateTime? SuggestedFollowUpDate { get; set; }

    /// <summary>
    /// Powód odwołania wizyty
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Notatki recepcjonisty
    /// </summary>
    public string? ReceptionistNotes { get; set; }
}
