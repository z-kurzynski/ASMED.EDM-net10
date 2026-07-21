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
