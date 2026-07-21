namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Encja reprezentująca dokument medyczny / historię choroby
/// </summary>
public class MedicalRecord : BaseEntity
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
    /// Relacja do wizyty (opcjonalna)
    /// </summary>
    public int? VisitId { get; set; }

    /// <summary>
    /// Navigation property do wizyty
    /// </summary>
    public virtual Visit? Visit { get; set; }

    /// <summary>
    /// Data zapisu
    /// </summary>
    public DateTime RecordDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Typ dokumentu (wynik badania, konsultacja, diagnoza)
    /// </summary>
    public string RecordType { get; set; } = string.Empty;

    /// <summary>
    /// Tytuł dokumentu
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Treść dokumentu
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Załącznik (ścieżka do pliku lub blob)
    /// </summary>
    public string? AttachmentPath { get; set; }

    /// <summary>
    /// Rozpoznanie ICD-10
    /// </summary>
    public string? IcdCode { get; set; }

    /// <summary>
    /// Uwagi
    /// </summary>
    public string? Notes { get; set; }
}
