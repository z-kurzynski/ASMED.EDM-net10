using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class BadanieRepository : Repository<Badanie>, IBadanieRepository
{
    public BadanieRepository(AsmedDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Badania dostępne do fakturowania: ListaBadanId == null (niezapisane na żadnej liście).
    /// Opcjonalnie filtruje po firmie pacjenta przez JOIN z Patient.
    /// Access: Bad_L_ID IS NULL OR Bad_L_ID = 0
    /// </summary>
    public async Task<IEnumerable<Badanie>> GetAvailableBadaniaAsync(int? firmaId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Badanie> query = _dbSet
            .Where(b => b.ListaBadanId == null)
            .Include(b => b.Patient);

        if (firmaId.HasValue)
        {
            // Filtruj badania pacjentów należących do danej firmy
            // Patient musi mieć przypisaną firmę (FirmaId) — Access: P_Pacjent.P_Firma_id
            query = query.Where(b => b.Patient != null && b.Patient.FirmaId == firmaId.Value);
        }

        return await query
            .OrderByDescending(b => b.DataBadania)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Badanie>> GetByListaBadanAsync(int listaBadanId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.ListaBadanId == listaBadanId)
            .Include(b => b.Patient)
            .OrderBy(b => b.DataBadania)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Badanie>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.PatientId == patientId)
            .Include(b => b.Patient)
            .OrderByDescending(b => b.DataBadania)
            .ToListAsync(cancellationToken);
    }

    public async Task<Badanie?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Patient)
            .Include(b => b.ListaBadan)
                .ThenInclude(l => l!.Firma)
            .Include(b => b.Faktura)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task AssignToListaAsync(IEnumerable<int> badanieIds, int listaBadanId, CancellationToken cancellationToken = default)
    {
        var ids = badanieIds.ToList();
        await _dbSet
            .Where(b => ids.Contains(b.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.ListaBadanId, listaBadanId), cancellationToken);
    }

    public async Task UnassignFromListaAsync(IEnumerable<int> badanieIds, CancellationToken cancellationToken = default)
    {
        var ids = badanieIds.ToList();
        await _dbSet
            .Where(b => ids.Contains(b.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.ListaBadanId, (int?)null)
                .SetProperty(b => b.FakturaId, (int?)null),
                cancellationToken);
    }

    public async Task SetFakturaIdAsync(int listaBadanId, int fakturaId, CancellationToken cancellationToken = default)
    {
        await _dbSet
            .Where(b => b.ListaBadanId == listaBadanId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.FakturaId, fakturaId), cancellationToken);
    }
}
