namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Encja reprezentująca lekarza
/// </summary>
public class Doctor : BaseEntity
{
    /// <summary>
    /// Powiązany użytkownik systemu
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property do użytkownika
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Numer prawa wykonywania zawodu (PWZ)
    /// </summary>
    public string MedicalLicenseNumber { get; set; } = string.Empty;

    /// <summary>
    /// Specjalizacja
    /// </summary>
    public string? Specialization { get; set; }

    /// <summary>
    /// Tytuł naukowy (dr, dr hab., prof.)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Dodatkowe kwalifikacje i certyfikaty
    /// </summary>
    public string? Qualifications { get; set; }

    /// <summary>
    /// Opis lekarza (biografia)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Czy lekarz przyjmuje nowych pacjentów
    /// </summary>
    public bool IsAcceptingNewPatients { get; set; } = true;

    /// <summary>
    /// Koszt wizyty (stawka)
    /// </summary>
    public decimal? ConsultationFee { get; set; }

    // ── Autoryzacja podpisu lekarza (P1 / orzeczenia medycyny pracy) ──────────

    /// <summary>
    /// Zaszyfrowany sekret TOTP (RFC 6238) używany do autoryzacji podpisu lekarza
    /// (np. przy wystawianiu/weryfikacji orzeczenia w module P1/Medycyna Pracy).
    /// Weryfikowany niezależnie od hasła logowania do aplikacji, przy użyciu
    /// zewnętrznej aplikacji authenticator (np. Google Authenticator, Microsoft Authenticator).
    /// </summary>
    public string? TotpSecretKeyEncrypted { get; set; }

    /// <summary>
    /// Czy autoryzacja podpisu TOTP została skonfigurowana i jest aktywna dla tego lekarza.
    /// </summary>
    public bool TotpEnabled { get; set; }

    /// <summary>
    /// Data i czas potwierdzenia (parowania) urządzenia authenticatora.
    /// </summary>
    public DateTime? TotpEnrolledAt { get; set; }

    /// <summary>
    /// Hasło do certyfikatu podpisu lekarza używanego przy wystawianiu/weryfikacji
    /// orzeczeń w Middleware P1 (pole "doctor_pass"). Docelowo przechowywane per-lekarz
    /// zamiast jednego globalnego hasła w ustawieniach placówki.
    /// </summary>
    public string? DoctorCertificatePassword { get; set; }

    /// <summary>
    /// Identyfikator okręgowej izby lekarskiej (pole "author.idIzbyLekarskiej" w Middleware P1).
    /// </summary>
    public string? IdIzbyLekarskiej { get; set; }

    /// <summary>
    /// Identyfikator autora w module Medycyny Pracy (pole "author.authorMedycynaPracyId" w Middleware P1).
    /// </summary>
    public string? AuthorMedycynaPracyId { get; set; }

    // Navigation properties
    /// <summary>
    /// Wizyty prowadzone przez tego lekarza
    /// </summary>
    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    /// <summary>
    /// Pełne imię i nazwisko z tytułem
    /// </summary>
    public string FullNameWithTitle => 
        !string.IsNullOrEmpty(Title) 
            ? $"{Title} {User?.FirstName} {User?.LastName}" 
            : $"{User?.FirstName} {User?.LastName}";
}
