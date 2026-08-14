using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Przypisanie roli użytkownika do zakładki aplikacji (uprawnienie dostępu).
/// </summary>
public class RolePermission
{
    public int Id { get; set; }

    /// <summary>
    /// Rola, dla której definiowane jest uprawnienie.
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Identyfikator zakładki, do której rola ma dostęp.
    /// </summary>
    public int AppTabId { get; set; }

    public virtual AppTab? AppTab { get; set; }
}
