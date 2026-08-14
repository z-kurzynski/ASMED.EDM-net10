using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class LoginHistoryRepository : ILoginHistoryRepository
{
    private readonly AsmedDbContext _context;
    private readonly DbSet<LoginHistory> _dbSet;

    public LoginHistoryRepository(AsmedDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<LoginHistory>();
    }

    public async Task<IEnumerable<LoginHistory>> GetByUserIdAsync(int userId, int maxRecords = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.LoginTime)
            .Take(maxRecords)
            .ToListAsync(cancellationToken);
    }

    public async Task<LoginHistory> AddAsync(LoginHistory entity, CancellationToken cancellationToken = default)
    {
        var result = await _dbSet.AddAsync(entity, cancellationToken);
        return result.Entity;
    }
}
