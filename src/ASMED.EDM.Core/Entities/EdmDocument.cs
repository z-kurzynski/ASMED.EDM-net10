using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Encja reprezentująca dokument EDM (HL7 CDA) w lokalnym repozytorium
/// </summary>
public class EdmDocument : BaseEntity
{
    /// <summary>
    /// Typ dokumentu EDM
    /// </summary>
    public EdmDocumentType DocumentType { get; set; }

    /// <summary>
    /// Identyfikator pacjenta
    /// </summary>
    public int PatientId { get; set; }

    /// <summary>
    /// Navigation property do pacjenta
    /// </summary>
    public virtual Patient Patient { get; set; } = null!;

    /// <summary>
    /// Identyfikator wizyty powiązanej z dokumentem (opcjonalny)
    /// </summary>
    public int? VisitId { get; set; }

    /// <summary>
    /// Navigation property do wizyty
    /// </summary>
    public virtual Visit? Visit { get; set; }

    /// <summary>
    /// Identyfikator lekarza wystawiającego dokument
    /// </summary>
    public int DoctorId { get; set; }

    /// <summary>
    /// Navigation property do lekarza
    /// </summary>
    public virtual Doctor Doctor { get; set; } = null!;

    /// <summary>
    /// Unikalny identyfikator instancji dokumentu (OID root + extension wg PIK HL7 CDA)
    /// </summary>
    public string DocumentOid { get; set; } = string.Empty;

    /// <summary>
    /// Tytuł dokumentu
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ścieżka do pliku XML w repozytorium plikowym
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Hash SHA-256 zawartości pliku XML (integralność)
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Status podpisu elektronicznego
    /// </summary>
    public EdmSignatureStatus SignatureStatus { get; set; } = EdmSignatureStatus.Unsigned;

    /// <summary>
    /// Data podpisania dokumentu
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Status indeksacji dokumentu w P1
    /// </summary>
    public P1SyncStatus P1IndexStatus { get; set; } = P1SyncStatus.NotSynced;

    /// <summary>
    /// Identyfikator indeksu dokumentu nadany przez P1
    /// </summary>
    public string? P1IndexId { get; set; }
}
