using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class PatientRepository : Repository<Patient>, IPatientRepository
{
    public PatientRepository(AsmedDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Patient>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearch = searchTerm.ToLower();
        return await _dbSet
            .Where(p =>
                p.FirstName.ToLower().Contains(lowerSearch) ||
                p.LastName.ToLower().Contains(lowerSearch) ||
                (p.FirstName + " " + p.LastName).ToLower().Contains(lowerSearch))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Patient?> GetByIdentificationNumberAsync(string identificationNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.IdentificationNumber == identificationNumber, cancellationToken);
    }

    public async Task<Patient?> GetWithVisitsAsync(int patientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Visits)
                .ThenInclude(v => v.Doctor)
                    .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
    }

    public async Task<IEnumerable<Patient>> GetByBirthDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.DateOfBirth >= from && p.DateOfBirth <= to)
            .OrderBy(p => p.DateOfBirth)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Patient>> GetPatientsWithUpcomingVisitsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(p => p.Visits)
            .Where(p => p.Visits.Any(v => v.ScheduledDateTime > now && v.Status == VisitStatus.Scheduled))
            .ToListAsync(cancellationToken);
    }
}
