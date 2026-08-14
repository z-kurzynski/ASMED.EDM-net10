using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class FirmaRepository : Repository<Firma>, IFirmaRepository
{
    public FirmaRepository(AsmedDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Firma>> GetActiveFirmyAsync(string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(f => f.Activ);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var lower = searchQuery.Trim().ToLower();
            query = query.Where(f =>
                f.Nazwa.ToLower().Contains(lower) ||
                (f.NIP != null && f.NIP.Contains(lower)));
        }

        return await query
            .OrderBy(f => f.Nazwa)
            .ToListAsync(cancellationToken);
    }

    public async Task<Firma?> GetByNazwaAsync(string nazwa, CancellationToken cancellationToken = default)
    {
        var trimmed = nazwa.Trim().ToLower();
        return await _dbSet
            .FirstOrDefaultAsync(f => f.Nazwa.Trim().ToLower() == trimmed, cancellationToken);
    }

    public async Task<Firma?> GetByNIPAsync(string nip, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(f => f.NIP != null && f.NIP == nip, cancellationToken);
    }
}
