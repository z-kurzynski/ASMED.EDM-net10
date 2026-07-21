namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Encja reprezentująca wpis w logu audytowym
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// Data i czas operacji
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identyfikator użytkownika wykonującego operację
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Nazwa użytkownika
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Typ operacji (Create, Update, Delete, Login, Logout)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Nazwa encji (tabeli)
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Identyfikator encji
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Stare wartości (JSON)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Nowe wartości (JSON)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Adres IP użytkownika
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User Agent (przeglądarka, aplikacja)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Dodatkowe informacje
    /// </summary>
    public string? AdditionalInfo { get; set; }

    /// <summary>
    /// Czy operacja zakończyła się sukcesem
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Komunikat błędu (jeśli wystąpił)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
