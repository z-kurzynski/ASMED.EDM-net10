using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Services;

/// <summary>
/// Serwis zarządzania dokumentacją medyczną
/// </summary>
public interface IMedicalRecordService
{
    Task<MedicalRecord?> GetRecordByIdAsync(int recordId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MedicalRecord>> GetPatientRecordsAsync(int patientId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MedicalRecord>> GetVisitRecordsAsync(int visitId, CancellationToken cancellationToken = default);
    Task<MedicalRecord> CreateRecordAsync(MedicalRecord record, CancellationToken cancellationToken = default);
    Task<MedicalRecord> UpdateRecordAsync(MedicalRecord record, CancellationToken cancellationToken = default);
    Task DeleteRecordAsync(int recordId, int userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Serwis zarządzania receptami
/// </summary>
public interface IPrescriptionService
{
    Task<Prescription?> GetPrescriptionByIdAsync(int prescriptionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Prescription>> GetPatientPrescriptionsAsync(int patientId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Prescription>> GetVisitPrescriptionsAsync(int visitId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Prescription>> GetUndispensedPrescriptionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Prescription>> GetExpiringPrescriptionsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default);
    Task<Prescription> CreatePrescriptionAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task MarkAsDispensedAsync(int prescriptionId, DateTime dispensedDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Serwis audytu i logowania operacji
/// </summary>
public interface IAuditService
{
    Task LogOperationAsync(
        int userId,
        string operationType,
        string entityName,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        bool isSuccess = true,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<AuditLog>> GetUserActivityAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityName, string entityId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetFailedOperationsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditsByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
