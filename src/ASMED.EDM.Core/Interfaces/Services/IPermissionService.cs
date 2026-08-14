using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Interfaces.Services;

/// <summary>
/// Serwis obsługujący uprawnienia ról do zakładek aplikacji.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Sprawdza, czy dana rola ma dostęp do zakładki o podanym kluczu.
    /// </summary>
    Task<bool> HasAccessAsync(UserRole role, string tabKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca zbiór kluczy zakładek dostępnych dla danej roli.
    /// </summary>
    Task<HashSet<string>> GetAllowedTabKeysAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zwraca wszystkie zdefiniowane zakładki aplikacji (do zarządzania uprawnieniami).
    /// </summary>
    Task<IEnumerable<AppTab>> GetAllTabsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Nadpisuje komplet uprawnień dla danej roli.
    /// </summary>
    Task SetPermissionsForRoleAsync(UserRole role, IEnumerable<int> appTabIds, CancellationToken cancellationToken = default);
}
