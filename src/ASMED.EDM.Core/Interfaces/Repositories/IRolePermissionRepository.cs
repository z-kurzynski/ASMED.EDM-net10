using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium przypisań ról do zakładek aplikacji (uprawnienia dostępu).
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>
    /// Pobiera wszystkie przypisania dla danej roli.
    /// </summary>
    Task<IEnumerable<RolePermission>> GetPermissionsForRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera zbiór kluczy zakładek dostępnych dla danej roli.
    /// </summary>
    Task<HashSet<string>> GetAllowedTabKeysAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nadpisuje komplet uprawnień dla danej roli zestawem identyfikatorów zakładek.
    /// </summary>
    Task SetPermissionsForRoleAsync(UserRole role, IEnumerable<int> appTabIds, CancellationToken cancellationToken = default);
}
