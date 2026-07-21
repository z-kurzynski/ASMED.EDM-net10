namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Encja reprezentująca szablon grafiku pracy lekarza
/// </summary>
public class DoctorSchedule : BaseEntity
{
    /// <summary>
    /// Identyfikator lekarza
    /// </summary>
    public int DoctorId { get; set; }

    /// <summary>
    /// Navigation property do lekarza
    /// </summary>
    public virtual Doctor Doctor { get; set; } = null!;

    /// <summary>
    /// Dzień tygodnia (0=Niedziela, 1=Poniedziałek, ..., 6=Sobota)
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Godzina rozpoczęcia pracy
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Godzina zakończenia pracy
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Czy w tym dniu lekarz przyjmuje
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Lokalizacja (gabinet, przychodnia)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Uwagi do grafiku
    /// </summary>
    public string? Notes { get; set; }
}
