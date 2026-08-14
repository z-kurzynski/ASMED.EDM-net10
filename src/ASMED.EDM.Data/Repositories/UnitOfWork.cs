using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Runtime.Versioning;

namespace ASMED.EDM.Data.Repositories;

/// <summary>
/// Implementacja Unit of Work
/// </summary>
[SupportedOSPlatform("windows")]
public class UnitOfWork : IUnitOfWork
{
    private readonly AsmedDbContext _context;
    private readonly DbConnectionFactory _connFactory;
    private readonly AccessAutoSyncService _autoSync;
    private IDbContextTransaction? _transaction;

    // Mapowanie typu encji → nazwa tabeli Access (legacy tabele ze wspólnym schematem)
    private static readonly Dictionary<Type, string> EntityToAccessTable = new()
    {
        [typeof(Firma)]      = "Firma",
        [typeof(Badanie)]    = "Badanie",
        [typeof(Faktura)]    = "Faktura",
        [typeof(ListaBadan)] = "ListyBadan",
    };

    // Lazy initialization repozytoriów
    private IPatientRepository? _patients;
    private IUserRepository? _users;
    private IDoctorRepository? _doctors;
    private IVisitRepository? _visits;
    private IDoctorScheduleRepository? _doctorSchedules;
    private IMedicalRecordRepository? _medicalRecords;
    private IPrescriptionRepository? _prescriptions;
    private IAuditLogRepository? _auditLogs;
    private ILoginHistoryRepository? _loginHistories;
    private IRepository<EdmDocument>? _edmDocuments;
    private IRepository<P1SyncLog>? _p1SyncLogs;
    private IAppTabRepository? _appTabs;
    private IRolePermissionRepository? _rolePermissions;

    // Moduł fakturowania
    private IFirmaRepository? _firmy;
    private ICennikRepository? _cenniki;
    private IFakturaRepository? _faktury;
    private IListaBadanRepository? _listyBadan;
    private IBadanieRepository? _badania;

    public UnitOfWork(
        AsmedDbContext context,
        DbConnectionFactory connFactory,
        AccessAutoSyncService autoSync)
    {
        _context     = context     ?? throw new ArgumentNullException(nameof(context));
        _connFactory = connFactory ?? throw new ArgumentNullException(nameof(connFactory));
        _autoSync    = autoSync    ?? throw new ArgumentNullException(nameof(autoSync));
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

    public ILoginHistoryRepository LoginHistories =>
        _loginHistories ??= new LoginHistoryRepository(_context);

    public IRepository<EdmDocument> EdmDocuments =>
        _edmDocuments ??= new Repository<EdmDocument>(_context);

    public IRepository<P1SyncLog> P1SyncLogs =>
        _p1SyncLogs ??= new Repository<P1SyncLog>(_context);

    public IAppTabRepository AppTabs =>
        _appTabs ??= new AppTabRepository(_context);

    public IRolePermissionRepository RolePermissions =>
        _rolePermissions ??= new RolePermissionRepository(_context);

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
        _badania ??= new BadanieRepository(_context, _connFactory);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Wykryj zmienione legacy tabele przed zapisem (ChangeTracker wie o zmianach)
        var changedTables = _context.ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added
                               or EntityState.Modified
                               or EntityState.Deleted)
            .Select(e => e.Entity.GetType())
            .Distinct()
            .Where(t => EntityToAccessTable.ContainsKey(t))
            .Select(t => EntityToAccessTable[t])
            .ToList();

        var result = await _context.SaveChangesAsync(cancellationToken);

        // Powiadom auto-sync dopiero po pomyślnym zapisie
        if (changedTables.Count > 0)
            _autoSync.NotifyTablesChanged(changedTables);

        return result;
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
            await SaveChangesAsync(cancellationToken);
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

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync<CancellationToken, bool>(cancellationToken, async (_, state, ct) =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            _transaction = transaction;
            try
            {
                await operation();
                await SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
            finally
            {
                _transaction = null;
            }
        }, verifySucceeded: null, cancellationToken);
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
