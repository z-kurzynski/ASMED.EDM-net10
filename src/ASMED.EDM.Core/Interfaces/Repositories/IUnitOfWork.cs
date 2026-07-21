namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Unit of Work - zarządza transakcjami i zapisem zmian
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repositories
    IPatientRepository Patients { get; }
    IUserRepository Users { get; }
    IDoctorRepository Doctors { get; }
    IVisitRepository Visits { get; }
    IDoctorScheduleRepository DoctorSchedules { get; }
    IMedicalRecordRepository MedicalRecords { get; }
    IPrescriptionRepository Prescriptions { get; }
    IAuditLogRepository AuditLogs { get; }

    /// <summary>
    /// Zapisuje wszystkie zmiany do bazy danych
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rozpoczyna transakcję
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commituje transakcję
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wycofuje transakcję
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Czyści ChangeTracker (odczepia wszystkie encje)
    /// </summary>
    void DetachAllEntities();
}
