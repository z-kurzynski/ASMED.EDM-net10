using ASMED.EDM.Core.Entities;

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
    ILoginHistoryRepository LoginHistories { get; }
    IRepository<EdmDocument> EdmDocuments { get; }
    IRepository<P1SyncLog> P1SyncLogs { get; }
    IAppTabRepository AppTabs { get; }
    IRolePermissionRepository RolePermissions { get; }

    // Moduł fakturowania
    IFirmaRepository Firmy { get; }
    ICennikRepository Cenniki { get; }
    IFakturaRepository Faktury { get; }
    IListaBadanRepository ListyBadan { get; }
    IBadanieRepository Badania { get; }

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

    /// <summary>
    /// Wykonuje operację w transakcji z obsługą strategii ponawiania (CreateExecutionStrategy)
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
