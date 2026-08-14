using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ASMED.EDM.Data.Repositories;

/// <summary>
/// Implementacja Unit of Work
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AsmedDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy initialization repozytoriów
    private IPatientRepository? _patients;
    private IUserRepository? _users;
    private IDoctorRepository? _doctors;
    private IVisitRepository? _visits;
    private IDoctorScheduleRepository? _doctorSchedules;
    private IMedicalRecordRepository? _medicalRecords;
    private IPrescriptionRepository? _prescriptions;
    private IAuditLogRepository? _auditLogs;
    private IRepository<EdmDocument>? _edmDocuments;
    private IRepository<P1SyncLog>? _p1SyncLogs;

    // Moduł fakturowania
    private IFirmaRepository? _firmy;
    private ICennikRepository? _cenniki;
    private IFakturaRepository? _faktury;
    private IListaBadanRepository? _listyBadan;
    private IBadanieRepository? _badania;

    public UnitOfWork(AsmedDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // Properties z lazy loading
    public IPatientRepository Patients =>
        _patients ??= new PatientRepository(_context);

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IDoctorRepository Doctors =>
        _doctors ??= new DoctorRepository(_context);

    public IVisitRepository Visits =>
        _visits ??= new VisitRepository(_context);

    public IDoctorScheduleRepository DoctorSchedules =>
        _doctorSchedules ??= new DoctorScheduleRepository(_context);

    public IMedicalRecordRepository MedicalRecords =>
        _medicalRecords ??= new MedicalRecordRepository(_context);

    public IPrescriptionRepository Prescriptions =>
        _prescriptions ??= new PrescriptionRepository(_context);

    public IAuditLogRepository AuditLogs =>
        _auditLogs ??= new AuditLogRepository(_context);

    public IRepository<EdmDocument> EdmDocuments =>
        _edmDocuments ??= new Repository<EdmDocument>(_context);

    public IRepository<P1SyncLog> P1SyncLogs =>
        _p1SyncLogs ??= new Repository<P1SyncLog>(_context);

    // Moduł fakturowania
    public IFirmaRepository Firmy =>
        _firmy ??= new FirmaRepository(_context);

    public ICennikRepository Cenniki =>
        _cenniki ??= new CennikRepository(_context);

    public IFakturaRepository Faktury =>
        _faktury ??= new FakturaRepository(_context);

    public IListaBadanRepository ListyBadan =>
        _listyBadan ??= new ListaBadanRepository(_context);

    public IBadanieRepository Badania =>
        _badania ??= new BadanieRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to rollback.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void DetachAllEntities()
    {
        var entries = _context.ChangeTracker.Entries()
            .Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
            .ToList();

        foreach (var entry in entries)
        {
            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
