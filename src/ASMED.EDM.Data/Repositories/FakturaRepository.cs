using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class FakturaRepository : Repository<Faktura>, IFakturaRepository
{
    public FakturaRepository(AsmedDbContext context) : base(context)
    {
    }

    public async Task<Faktura?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(f => f.Firma)
            .Include(f => f.ListaBadan)
                .ThenInclude(l => l!.Badania)
                    .ThenInclude(b => b.Patient)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Faktura>> GetByFirmaAsync(int firmaId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(f => f.FirmaId == firmaId)
            .Include(f => f.Firma)
            .OrderByDescending(f => f.DataWystawienia)
            .ToListAsync(cancellationToken);
    }

    public async Task<Faktura?> GetByNumerAsync(string numer, CancellationToken cancellationToken = default)
    {
        var trimmed = numer.Trim();
        return await _dbSet
            .Include(f => f.Firma)
            .FirstOrDefaultAsync(f => f.NumerFaktury != null && f.NumerFaktury.Trim() == trimmed, cancellationToken);
    }

    public async Task<IEnumerable<Faktura>> GetByDataRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(f => f.DataWystawienia >= from && f.DataWystawienia <= to)
            .Include(f => f.Firma)
            .OrderByDescending(f => f.DataWystawienia)
            .ToListAsync(cancellationToken);
    }
}
