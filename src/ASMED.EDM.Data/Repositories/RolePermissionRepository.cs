using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly AsmedDbContext _context;
    private readonly DbSet<RolePermission> _dbSet;

    public RolePermissionRepository(AsmedDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<RolePermission>();
    }

    public async Task<IEnumerable<RolePermission>> GetPermissionsForRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.AppTab)
            .Where(p => p.Role == role)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<string>> GetAllowedTabKeysAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        var keys = await _dbSet
            .Where(p => p.Role == role)
            .Select(p => p.AppTab!.Key)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
    }

    public async Task SetPermissionsForRoleAsync(UserRole role, IEnumerable<int> appTabIds, CancellationToken cancellationToken = default)
    {
        var existing = await _dbSet.Where(p => p.Role == role).ToListAsync(cancellationToken);
        _dbSet.RemoveRange(existing);

        foreach (var tabId in appTabIds.Distinct())
        {
            await _dbSet.AddAsync(new RolePermission { Role = role, AppTabId = tabId }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
