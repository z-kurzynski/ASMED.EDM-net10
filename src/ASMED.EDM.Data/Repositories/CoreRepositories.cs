using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AsmedDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetWithDoctorAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(u => u.Doctor).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(u => u.IsActive).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(u => u.Role == role).ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => u.Username == username);
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        return !await query.AnyAsync(cancellationToken);
    }
}

public class DoctorRepository : Repository<Doctor>, IDoctorRepository
{
    public DoctorRepository(AsmedDbContext context) : base(context) { }

    public async Task<Doctor?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
    }

    public async Task<Doctor?> GetWithUserAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);
    }

    public async Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.User)
            .Where(d => d.Specialization != null && d.Specialization.Contains(specialization))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Doctor>> GetAcceptingNewPatientsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.User)
            .Where(d => d.IsAcceptingNewPatients)
            .ToListAsync(cancellationToken);
    }

    public async Task<Doctor?> GetByMedicalLicenseNumberAsync(string licenseNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.MedicalLicenseNumber == licenseNumber, cancellationToken);
    }

    public async Task<IEnumerable<Doctor>> GetWithSchedulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Doctors
            .Include(d => d.User)
            .Include(d => ((Doctor)d).Visits) // Workaround for schedules if needed
            .ToListAsync(cancellationToken);
    }
}

public class VisitRepository : Repository<Visit>, IVisitRepository
{
    public VisitRepository(AsmedDbContext context) : base(context) { }

    public async Task<Visit?> GetWithDetailsAsync(int visitId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
                .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.Doctor).ThenInclude(d => d.User)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.Patient)
            .Where(v => v.DoctorId == doctorId)
            .OrderByDescending(v => v.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);
        return await _dbSet
            .Include(v => v.Patient)
            .Include(v => v.Doctor).ThenInclude(d => d.User)
            .Where(v => v.ScheduledDateTime >= startOfDay && v.ScheduledDateTime < endOfDay)
            .OrderBy(v => v.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetByDoctorAndDateAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);
        return await _dbSet
            .Include(v => v.Patient)
            .Where(v => v.DoctorId == doctorId &&
                       v.ScheduledDateTime >= startOfDay &&
                       v.ScheduledDateTime < endOfDay)
            .OrderBy(v => v.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.Patient)
            .Include(v => v.Doctor).ThenInclude(d => d.User)
            .Where(v => v.ScheduledDateTime >= from && v.ScheduledDateTime <= to)
            .OrderBy(v => v.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetByStatusAsync(VisitStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.Patient)
            .Include(v => v.Doctor).ThenInclude(d => d.User)
            .Where(v => v.Status == status)
            .OrderBy(v => v.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetUpcomingVisitsByPatientAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(v => v.Doctor).ThenInclude(d => d.User)
            .Where(v => v.PatientId == patientId &&
                       v.ScheduledDateTime > now &&
                       v.Status != VisitStatus.Cancelled)
            .OrderBy(v => v.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetUpcomingVisitsByDoctorAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(v => v.Patient)
            .Where(v => v.DoctorId == doctorId &&
                       v.ScheduledDateTime > now &&
                       v.Status != VisitStatus.Cancelled)
            .OrderBy(v => v.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime startTime, DateTime endTime, int? excludeVisitId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(v =>
            v.DoctorId == doctorId &&
            v.Status != VisitStatus.Cancelled &&
            ((v.ScheduledDateTime >= startTime && v.ScheduledDateTime < endTime) ||
             (v.ScheduledDateTime.Add(TimeSpan.FromMinutes(v.DurationMinutes)) > startTime &&
              v.ScheduledDateTime.Add(TimeSpan.FromMinutes(v.DurationMinutes)) <= endTime)));

        if (excludeVisitId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVisitId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetUnpaidVisitsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(v => v.Patient)
            .Include(v => v.Doctor).ThenInclude(d => d.User)
            .Where(v => v.Status == VisitStatus.Completed && !v.IsPaid)
            .OrderBy(v => v.ScheduledDateTime)
            .ToListAsync(cancellationToken);
    }
}
