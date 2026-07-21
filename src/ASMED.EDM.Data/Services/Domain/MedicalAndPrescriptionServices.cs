using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Implementacja serwisu zarządzania dokumentacją medyczną
/// </summary>
public class MedicalRecordService : IMedicalRecordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MedicalRecordService> _logger;
    private readonly IAuditService _auditService;

    public MedicalRecordService(
        IUnitOfWork unitOfWork,
        ILogger<MedicalRecordService> logger,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<MedicalRecord?> GetRecordByIdAsync(int recordId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie dokumentacji medycznej ID: {RecordId}", recordId);
        return await _unitOfWork.MedicalRecords.GetByIdAsync(recordId, cancellationToken);
    }

    public async Task<IEnumerable<MedicalRecord>> GetPatientRecordsAsync(int patientId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie dokumentacji medycznej pacjenta ID: {PatientId}", patientId);
        return await _unitOfWork.MedicalRecords.GetByPatientIdAsync(patientId, cancellationToken);
    }

    public async Task<IEnumerable<MedicalRecord>> GetVisitRecordsAsync(int visitId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie dokumentacji z wizyty ID: {VisitId}", visitId);
        return await _unitOfWork.MedicalRecords.GetByVisitIdAsync(visitId, cancellationToken);
    }

    public async Task<MedicalRecord> CreateRecordAsync(MedicalRecord record, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tworzenie nowego wpisu medycznego dla pacjenta ID: {PatientId}", record.PatientId);

        try
        {
            // Walidacja
            if (string.IsNullOrWhiteSpace(record.Title))
            {
                throw new InvalidOperationException("Tytuł wpisu jest wymagany");
            }

            // Sprawdzenie czy pacjent istnieje
            var patient = await _unitOfWork.Patients.GetByIdAsync(record.PatientId, cancellationToken);
            if (patient == null)
            {
                throw new InvalidOperationException($"Pacjent o ID {record.PatientId} nie istnieje");
            }

            record.RecordDate = DateTime.UtcNow;
            var created = await _unitOfWork.MedicalRecords.AddAsync(record, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: record.CreatedById ?? 0,
                operationType: "CREATE_RECORD",
                entityName: nameof(MedicalRecord),
                entityId: created.Id.ToString(),
                newValues: $"Patient: {record.PatientId}, Type: {record.RecordType}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Utworzono wpis medyczny ID: {RecordId}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia wpisu medycznego");
            throw;
        }
    }

    public async Task<MedicalRecord> UpdateRecordAsync(MedicalRecord record, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aktualizacja wpisu medycznego ID: {RecordId}", record.Id);

        try
        {
            var existing = await _unitOfWork.MedicalRecords.GetByIdAsync(record.Id, cancellationToken);
            if (existing == null)
            {
                throw new InvalidOperationException($"Wpis medyczny o ID {record.Id} nie istnieje");
            }

            _unitOfWork.MedicalRecords.Update(record);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: record.ModifiedById ?? 0,
                operationType: "UPDATE_RECORD",
                entityName: nameof(MedicalRecord),
                entityId: record.Id.ToString(),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Zaktualizowano wpis medyczny ID: {RecordId}", record.Id);
            return record;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji wpisu medycznego ID: {RecordId}", record.Id);
            throw;
        }
    }

    public async Task DeleteRecordAsync(int recordId, int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Usuwanie wpisu medycznego ID: {RecordId}", recordId);

        try
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdAsync(recordId, cancellationToken);
            if (record == null)
            {
                throw new InvalidOperationException($"Wpis medyczny o ID {recordId} nie istnieje");
            }

            await _unitOfWork.MedicalRecords.SoftDeleteAsync(recordId, userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: userId,
                operationType: "DELETE_RECORD",
                entityName: nameof(MedicalRecord),
                entityId: recordId.ToString(),
                oldValues: record.Title,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Usunięto wpis medyczny ID: {RecordId}", recordId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania wpisu medycznego ID: {RecordId}", recordId);
            throw;
        }
    }
}

/// <summary>
/// Implementacja serwisu zarządzania receptami
/// </summary>
public class PrescriptionService : IPrescriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PrescriptionService> _logger;
    private readonly IAuditService _auditService;

    public PrescriptionService(
        IUnitOfWork unitOfWork,
        ILogger<PrescriptionService> logger,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<Prescription?> GetPrescriptionByIdAsync(int prescriptionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie recepty ID: {PrescriptionId}", prescriptionId);
        return await _unitOfWork.Prescriptions.GetByIdAsync(prescriptionId, cancellationToken);
    }

    public async Task<IEnumerable<Prescription>> GetPatientPrescriptionsAsync(int patientId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie recept pacjenta ID: {PatientId}", patientId);
        return await _unitOfWork.Prescriptions.GetByPatientIdAsync(patientId, cancellationToken);
    }

    public async Task<IEnumerable<Prescription>> GetVisitPrescriptionsAsync(int visitId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie recept z wizyty ID: {VisitId}", visitId);
        return await _unitOfWork.Prescriptions.GetByVisitIdAsync(visitId, cancellationToken);
    }

    public async Task<IEnumerable<Prescription>> GetUndispensedPrescriptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie niezrealizowanych recept");
        return await _unitOfWork.Prescriptions.GetUndispensedPrescriptionsAsync(cancellationToken);
    }

    public async Task<IEnumerable<Prescription>> GetExpiringPrescriptionsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie recept wygasających w ciągu {Days} dni", daysUntilExpiry);
        return await _unitOfWork.Prescriptions.GetExpiringPrescriptionsAsync(daysUntilExpiry, cancellationToken);
    }

    public async Task<Prescription> CreatePrescriptionAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tworzenie nowej recepty dla pacjenta ID: {PatientId}", prescription.PatientId);

        try
        {
            // Walidacja
            if (string.IsNullOrWhiteSpace(prescription.MedicationName))
            {
                throw new InvalidOperationException("Nazwa leku jest wymagana");
            }

            // Sprawdzenie czy pacjent istnieje
            var patient = await _unitOfWork.Patients.GetByIdAsync(prescription.PatientId, cancellationToken);
            if (patient == null)
            {
                throw new InvalidOperationException($"Pacjent o ID {prescription.PatientId} nie istnieje");
            }

            // Sprawdzenie czy lekarz istnieje
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(prescription.DoctorId, cancellationToken);
            if (doctor == null)
            {
                throw new InvalidOperationException($"Lekarz o ID {prescription.DoctorId} nie istnieje");
            }

            prescription.PrescriptionDate = DateTime.UtcNow;
            prescription.IsDispensed = false;

            var created = await _unitOfWork.Prescriptions.AddAsync(prescription, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: prescription.CreatedById ?? 0,
                operationType: "CREATE_PRESCRIPTION",
                entityName: nameof(Prescription),
                entityId: created.Id.ToString(),
                newValues: $"Medication: {prescription.MedicationName}, Patient: {prescription.PatientId}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Utworzono receptę ID: {PrescriptionId}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia recepty");
            throw;
        }
    }

    public async Task MarkAsDispensedAsync(int prescriptionId, DateTime dispensedDate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Oznaczanie recepty ID: {PrescriptionId} jako zrealizowanej", prescriptionId);

        try
        {
            var prescription = await _unitOfWork.Prescriptions.GetByIdAsync(prescriptionId, cancellationToken);
            if (prescription == null)
            {
                throw new InvalidOperationException($"Recepta o ID {prescriptionId} nie istnieje");
            }

            if (prescription.IsDispensed)
            {
                throw new InvalidOperationException($"Recepta o ID {prescriptionId} została już zrealizowana");
            }

            prescription.IsDispensed = true;
            prescription.DispensedDate = dispensedDate;
            prescription.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Prescriptions.Update(prescription);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: prescription.ModifiedById ?? 0,
                operationType: "DISPENSE_PRESCRIPTION",
                entityName: nameof(Prescription),
                entityId: prescriptionId.ToString(),
                newValues: $"Dispensed: {dispensedDate}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Recepta ID: {PrescriptionId} została zrealizowana", prescriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas oznaczania recepty jako zrealizowanej ID: {PrescriptionId}", prescriptionId);
            throw;
        }
    }
}
