namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Bazowa klasa dla wszystkich encji domenowych
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unikalny identyfikator encji
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Data utworzenia rekordu
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identyfikator użytkownika tworzącego rekord
    /// </summary>
    public int? CreatedById { get; set; }

    /// <summary>
    /// Data ostatniej modyfikacji
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Identyfikator użytkownika modyfikującego rekord
    /// </summary>
    public int? ModifiedById { get; set; }

    /// <summary>
    /// Flaga soft delete (logiczne usunięcie)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Data usunięcia (soft delete)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Identyfikator użytkownika usuwającego rekord
    /// </summary>
    public int? DeletedById { get; set; }

    /// <summary>
    /// Row version dla Optimistic Concurrency Control
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
