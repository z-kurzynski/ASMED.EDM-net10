using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Encja reprezentująca pacjenta
/// </summary>
public class Patient : BaseEntity
{
    /// <summary>
    /// Numer identyfikacyjny pacjenta (PESEL lub inny)
    /// </summary>
    public string IdentificationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Imię
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nazwisko
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Data urodzenia
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Płeć
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// Numer telefonu
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Adres email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Pełny adres zamieszkania
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Miasto
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Kod pocztowy
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Kraj
    /// </summary>
    public string Country { get; set; } = "Polska";

    /// <summary>
    /// Grupa krwi
    /// </summary>
    public string? BloodType { get; set; }

    /// <summary>
    /// Alergie (JSON lub tekst)
    /// </summary>
    public string? Allergies { get; set; }

    /// <summary>
    /// Przewlekłe choroby (JSON lub tekst)
    /// </summary>
    public string? ChronicDiseases { get; set; }

    /// <summary>
    /// Uwagi medyczne
    /// </summary>
    public string? MedicalNotes { get; set; }

    /// <summary>
    /// Osoba kontaktowa w nagłych wypadkach
    /// </summary>
    public string? EmergencyContactName { get; set; }

    /// <summary>
    /// Telefon osoby kontaktowej
    /// </summary>
    public string? EmergencyContactPhone { get; set; }

    // Navigation properties
    /// <summary>
    /// Lista wizyt pacjenta
    /// </summary>
    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    /// <summary>
    /// Pełne imię i nazwisko
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";
}
