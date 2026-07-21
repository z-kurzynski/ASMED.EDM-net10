using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Encja reprezentująca użytkownika systemu
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Nazwa użytkownika (login)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hash hasła (nigdy nie przechowuj plain text!)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Imię
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Nazwisko
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Numer telefonu
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Rola użytkownika w systemie
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Czy konto jest aktywne
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Data ostatniego logowania
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Data ostatniej nieudanej próby logowania
    /// </summary>
    public DateTime? LastFailedLoginAt { get; set; }

    /// <summary>
    /// Liczba nieudanych prób logowania
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Czy konto jest zablokowane
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// Data do której konto jest zablokowane
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// Data blokady konta (po przekroczeniu limitu prób logowania) - deprecated, użyj LockedUntil
    /// </summary>
    [Obsolete("Use LockedUntil instead")]
    public DateTime? LockedOutUntil { get; set; }

    /// <summary>
    /// Token resetowania hasła
    /// </summary>
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Data wygaśnięcia tokenu resetowania hasła
    /// </summary>
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Navigation properties
    /// <summary>
    /// Powiązany lekarz (jeśli użytkownik jest lekarzem)
    /// </summary>
    public virtual Doctor? Doctor { get; set; }

    /// <summary>
    /// Wizyty utworzone przez tego użytkownika
    /// </summary>
    public virtual ICollection<Visit> CreatedVisits { get; set; } = new List<Visit>();

    /// <summary>
    /// Pełne imię i nazwisko
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";
}
