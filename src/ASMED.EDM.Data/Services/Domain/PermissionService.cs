using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Implementacja serwisu uprawnień ról do zakładek aplikacji.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(IUnitOfWork unitOfWork, ILogger<PermissionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> HasAccessAsync(UserRole role, string tabKey, CancellationToken cancellationToken = default)
    {
        // Administrator ma zawsze pełny dostęp — zabezpieczenie przed zablokowaniem się z systemu.
        if (role == UserRole.Administrator) return true;

        var keys = await _unitOfWork.RolePermissions.GetAllowedTabKeysAsync(role, cancellationToken);
        return keys.Contains(tabKey);
    }

    public async Task<HashSet<string>> GetAllowedTabKeysAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        if (role == UserRole.Administrator)
        {
            var allTabs = await _unitOfWork.AppTabs.GetAllTabsAsync(cancellationToken);
            return new HashSet<string>(allTabs.Select(t => t.Key), StringComparer.OrdinalIgnoreCase);
        }

        return await _unitOfWork.RolePermissions.GetAllowedTabKeysAsync(role, cancellationToken);
    }

    public async Task<IEnumerable<AppTab>> GetAllTabsAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AppTabs.GetAllTabsAsync(cancellationToken);
    }

    public async Task SetPermissionsForRoleAsync(UserRole role, IEnumerable<int> appTabIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aktualizacja uprawnień dla roli {Role}", role);
        await _unitOfWork.RolePermissions.SetPermissionsForRoleAsync(role, appTabIds, cancellationToken);
    }
}
