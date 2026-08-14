namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Definicja zakładki aplikacji (głównej lub podzakładki), do której można przypisać uprawnienia ról.
/// Lista zakładek jest definiowana wyłącznie programistycznie (seed) - nie jest edytowalna z poziomu UI.
/// </summary>
public class AppTab
{
    public int Id { get; set; }

    /// <summary>
    /// Unikalny klucz identyfikujący zakładkę w kodzie (np. "Rejestracja", "Ustawienia.Migracja").
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Nazwa wyświetlana w UI zarządzania uprawnieniami.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Klucz zakładki nadrzędnej (null dla zakładek głównych).
    /// </summary>
    public string? ParentKey { get; set; }

    /// <summary>
    /// Kolejność sortowania w UI.
    /// </summary>
    public int SortOrder { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
