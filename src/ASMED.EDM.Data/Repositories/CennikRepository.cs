using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class CennikRepository : Repository<Cennik>, ICennikRepository
{
    public CennikRepository(AsmedDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Cennik>> GetActiveCennikiAsync(CancellationToken cancellationToken = default)
    {
        // IsActiv != false obsługuje NULL (migrowane rekordy z Access mogą mieć NULL)
        return await _dbSet
            .Where(c => c.IsActiv != false)
            .OrderBy(c => c.Nazwa)
            .ToListAsync(cancellationToken);
    }

    public async Task<Cennik?> GetByNazwaAsync(string nazwa, CancellationToken cancellationToken = default)
    {
        var trimmed = nazwa.Trim().ToLower();
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Nazwa.Trim().ToLower() == trimmed, cancellationToken);
    }
}
