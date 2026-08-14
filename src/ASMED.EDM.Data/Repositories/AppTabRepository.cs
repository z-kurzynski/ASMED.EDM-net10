using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class AppTabRepository : IAppTabRepository
{
    private readonly AsmedDbContext _context;
    private readonly DbSet<AppTab> _dbSet;

    public AppTabRepository(AsmedDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<AppTab>();
    }

    public async Task<IEnumerable<AppTab>> GetAllTabsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderBy(t => t.ParentKey == null ? 0 : 1)
            .ThenBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<AppTab?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Key == key, cancellationToken);
    }
}
