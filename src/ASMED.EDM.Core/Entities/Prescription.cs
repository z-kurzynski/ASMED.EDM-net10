namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Encja reprezentująca receptę
/// </summary>
public class Prescription : BaseEntity
{
    /// <summary>
    /// Identyfikator wizyty
    /// </summary>
    public int VisitId { get; set; }

    /// <summary>
    /// Navigation property do wizyty
    /// </summary>
    public virtual Visit Visit { get; set; } = null!;

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
    /// Data wystawienia recepty
    /// </summary>
    public DateTime PrescriptionDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Nazwa leku
    /// </summary>
    public string MedicationName { get; set; } = string.Empty;

    /// <summary>
    /// Dawkowanie
    /// </summary>
    public string Dosage { get; set; } = string.Empty;

    /// <summary>
    /// Częstotliwość przyjmowania
    /// </summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>
    /// Czas trwania kuracji (w dniach)
    /// </summary>
    public int DurationDays { get; set; }

    /// <summary>
    /// Ilość opakowań
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Czy refundowane
    /// </summary>
    public bool IsReimbursed { get; set; } = false;

    /// <summary>
    /// Procent refundacji
    /// </summary>
    public decimal? ReimbursementPercentage { get; set; }

    /// <summary>
    /// Dodatkowe instrukcje
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Data wygaśnięcia recepty
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Czy recepta została zrealizowana
    /// </summary>
    public bool IsDispensed { get; set; } = false;

    /// <summary>
    /// Data realizacji
    /// </summary>
    public DateTime? DispensedDate { get; set; }

    /// <summary>
    /// Apteka realizująca
    /// </summary>
    public string? PharmacyName { get; set; }
}
