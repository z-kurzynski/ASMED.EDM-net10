using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data.Repositories;

public class DoctorScheduleRepository : Repository<DoctorSchedule>, IDoctorScheduleRepository
{
    public DoctorScheduleRepository(AsmedDbContext context) : base(context) { }

    public async Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.DoctorId == doctorId)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync(cancellationToken);
    }

    public async Task<DoctorSchedule?> GetByDoctorAndDayAsync(int doctorId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek, cancellationToken);
    }

    public async Task<IEnumerable<DoctorSchedule>> GetAvailableDoctorsOnDayAsync(DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Doctor)
                .ThenInclude(d => d.User)
            .Where(s => s.DayOfWeek == dayOfWeek && s.IsAvailable)
            .ToListAsync(cancellationToken);
    }
}

public class MedicalRecordRepository : Repository<MedicalRecord>, IMedicalRecordRepository
{
    public MedicalRecordRepository(AsmedDbContext context) : base(context) { }

    public async Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.RecordDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MedicalRecord>> GetByVisitIdAsync(int visitId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.VisitId == visitId)
            .OrderByDescending(r => r.RecordDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MedicalRecord>> GetByTypeAsync(string recordType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Patient)
            .Where(r => r.RecordType == recordType)
            .OrderByDescending(r => r.RecordDate)
            .ToListAsync(cancellationToken);
    }
}

public class PrescriptionRepository : Repository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(AsmedDbContext context) : base(context) { }

    public async Task<IEnumerable<Prescription>> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Doctor)
                .ThenInclude(d => d.User)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.PrescriptionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Prescription>> GetByVisitIdAsync(int visitId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Doctor)
                .ThenInclude(d => d.User)
            .Where(p => p.VisitId == visitId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Prescription>> GetByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Patient)
            .Where(p => p.DoctorId == doctorId)
            .OrderByDescending(p => p.PrescriptionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Prescription>> GetUndispensedPrescriptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
                .ThenInclude(d => d.User)
            .Where(p => !p.IsDispensed)
            .OrderBy(p => p.PrescriptionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Prescription>> GetExpiringPrescriptionsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTime.UtcNow.AddDays(daysUntilExpiry);
        return await _dbSet
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
                .ThenInclude(d => d.User)
            .Where(p => p.ExpiryDate.HasValue &&
                       p.ExpiryDate.Value <= expiryDate &&
                       !p.IsDispensed)
            .OrderBy(p => p.ExpiryDate)
            .ToListAsync(cancellationToken);
    }
}

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AsmedDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string entityId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.EntityName == entityName && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => l.Timestamp >= from && l.Timestamp <= to)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetFailedOperationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(l => !l.IsSuccess)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
