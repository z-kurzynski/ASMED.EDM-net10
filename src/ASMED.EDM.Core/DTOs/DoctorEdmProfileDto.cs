namespace ASMED.EDM.Core.DTOs;

/// <summary>
/// Dane lekarza wymagane do autoryzacji i wystawiania dokumentów w EDM / P1
/// (sekcja "author" oraz "doctor_pass" w Middleware Medycyny Pracy).
/// </summary>
public class DoctorEdmProfileDto
{
    /// <summary>Numer prawa wykonywania zawodu lekarza (NPWZ, 7 cyfr).</summary>
    public string MedicalLicenseNumber { get; set; } = string.Empty;

    /// <summary>Tytuł naukowy (np. lek., dr n. med.).</summary>
    public string? Title { get; set; }

    /// <summary>Specjalizacja.</summary>
    public string? Specialization { get; set; }

    /// <summary>Identyfikator okręgowej izby lekarskiej.</summary>
    public string? IdIzbyLekarskiej { get; set; }

    /// <summary>Identyfikator autora w module Medycyny Pracy.</summary>
    public string? AuthorMedycynaPracyId { get; set; }

    /// <summary>
    /// Hasło do certyfikatu podpisu lekarza ("doctor_pass").
    /// Null = pozostaw dotychczasowe hasło bez zmian.
    /// </summary>
    public string? DoctorCertificatePassword { get; set; }

    /// <summary>Identyfikator użytkownika wykonującego operację (audyt).</summary>
    public int? ModifiedById { get; set; }
}
