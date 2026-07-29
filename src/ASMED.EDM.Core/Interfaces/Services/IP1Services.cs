using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Interfaces.Services;

/// <summary>
/// Wynik weryfikacji pacjenta w systemie P1 po numerze PESEL
/// </summary>
public class P1PatientVerificationResult
{
    /// <summary>Czy pacjent został odnaleziony w P1</summary>
    public bool Found { get; set; }

    /// <summary>Czy dane lokalne (imię/nazwisko) zgadzają się z danymi w P1</summary>
    public bool DataMatches { get; set; }

    /// <summary>Imię pacjenta wg P1</summary>
    public string? FirstName { get; set; }

    /// <summary>Nazwisko pacjenta wg P1</summary>
    public string? LastName { get; set; }

    /// <summary>Data urodzenia wg P1</summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>Identyfikator zasobu Patient w P1 (FHIR)</summary>
    public string? P1PatientId { get; set; }

    /// <summary>Adres pacjenta wg P1</summary>
    public string? Address { get; set; }

    /// <summary>Telefon kontaktowy wg P1</summary>
    public string? Phone { get; set; }

    /// <summary>Płeć wg P1 (Male/Female/Other/Unknown)</summary>
    public string? Gender { get; set; }

    /// <summary>Komunikat błędu (jeśli weryfikacja się nie powiodła technicznie)</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Wynik operacji na Zdarzeniu Medycznym w P1
/// </summary>
public class P1MedicalEventResult
{
    /// <summary>Czy operacja się powiodła</summary>
    public bool Success { get; set; }

    /// <summary>Identyfikator Zdarzenia Medycznego nadany przez P1</summary>
    public string? P1EventId { get; set; }

    /// <summary>Komunikat błędu</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Serwis weryfikacji pacjenta w systemie P1 po numerze PESEL
/// </summary>
public interface IP1PatientVerificationService
{
    /// <summary>
    /// Weryfikuje pacjenta w P1 po numerze PESEL i porównuje z danymi lokalnymi.
    /// </summary>
    Task<P1PatientVerificationResult> VerifyPatientByPeselAsync(Patient patient, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wyszukuje dane pacjenta w P1 bezpośrednio po numerze PESEL
    /// (bez potrzeby posiadania rekordu w lokalnej bazie).
    /// </summary>
    Task<P1PatientVerificationResult> SearchByPeselDirectAsync(string pesel, CancellationToken cancellationToken = default);
}

/// <summary>
/// Serwis rejestracji Zdarzeń Medycznych (ZM) w systemie P1
/// </summary>
public interface IMedicalEventService
{
    /// <summary>Rejestruje Zdarzenie Medyczne (wizytę) w P1.</summary>
    Task<P1MedicalEventResult> RegisterVisitAsync(Visit visit, CancellationToken cancellationToken = default);

    /// <summary>Aktualizuje wcześniej zarejestrowane Zdarzenie Medyczne w P1.</summary>
    Task<P1MedicalEventResult> UpdateVisitAsync(Visit visit, CancellationToken cancellationToken = default);

    /// <summary>Anuluje Zdarzenie Medyczne w P1.</summary>
    Task<P1MedicalEventResult> CancelVisitAsync(Visit visit, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dane wejściowe do wystawienia orzeczenia o zdolności do pracy
/// </summary>
public class WorkAbilityCertificateRequest
{
    public int PatientId { get; set; }
    public int? VisitId { get; set; }
    public int DoctorId { get; set; }

    /// <summary>Czy pacjent jest zdolny do pracy</summary>
    public bool IsAbleToWork { get; set; }

    /// <summary>Stanowisko / rodzaj pracy, którego dotyczy orzeczenie</summary>
    public string? WorkPosition { get; set; }

    /// <summary>Nazwa pracodawcy</summary>
    public string? EmployerName { get; set; }

    /// <summary>Treść / uzasadnienie orzeczenia</summary>
    public string? Statement { get; set; }

    /// <summary>Data następnego badania okresowego</summary>
    public DateTime? NextExaminationDate { get; set; }
}

/// <summary>
/// Serwis lokalnego repozytorium dokumentów EDM (generacja CDA, podpis, zapis, indeksacja w P1)
/// </summary>
public interface IEdmDocumentService
{
    /// <summary>
    /// Generuje dokument HL7 CDA orzeczenia o zdolności do pracy, podpisuje pieczęcią placówki
    /// i zapisuje w lokalnym repozytorium (baza + plik XML).
    /// </summary>
    Task<EdmDocument> CreateWorkAbilityCertificateAsync(WorkAbilityCertificateRequest request, int userId, CancellationToken cancellationToken = default);

    /// <summary>Pobiera dokument EDM po identyfikatorze.</summary>
    Task<EdmDocument?> GetDocumentByIdAsync(int documentId, CancellationToken cancellationToken = default);

    /// <summary>Pobiera dokumenty EDM pacjenta.</summary>
    Task<IEnumerable<EdmDocument>> GetPatientDocumentsAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>Odczytuje zawartość XML dokumentu z repozytorium plikowego.</summary>
    Task<string> GetDocumentContentAsync(int documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indeksuje dokument EDM w P1 (etap 2 — implementacja może być stubem).
    /// </summary>
    Task<P1MedicalEventResult> IndexDocumentInP1Async(int documentId, CancellationToken cancellationToken = default);
}
