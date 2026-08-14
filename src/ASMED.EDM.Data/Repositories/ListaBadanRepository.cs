using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class ListaBadanRepository : Repository<ListaBadan>, IListaBadanRepository
{
    public ListaBadanRepository(AsmedDbContext context) : base(context)
    {
    }

    public async Task<ListaBadan?> GetWithBadaniaAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.Firma)
            .Include(l => l.Badania)
                .ThenInclude(b => b.Patient)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ListaBadan>> GetByFirmaAsync(int firmaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.FirmaId == firmaId)
            .Include(l => l.Firma)
            .OrderByDescending(l => l.DataUtworzenia)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ListaBadan>> GetNiezafakturowanieAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.FakturaId == null)
            .Include(l => l.Firma)
            .Include(l => l.Badania)
            .OrderByDescending(l => l.DataUtworzenia)
            .ToListAsync(cancellationToken);
    }
}
