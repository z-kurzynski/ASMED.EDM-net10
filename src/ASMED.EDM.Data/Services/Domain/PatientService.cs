using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Implementacja serwisu zarządzania pacjentami
/// </summary>
public class PatientService : IPatientService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientService> _logger;
    private readonly IAuditService _auditService;

    public PatientService(
        IUnitOfWork unitOfWork,
        ILogger<PatientService> logger,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // ==================== QUERIES ====================

    public async Task<Patient?> GetPatientByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie pacjenta o ID: {PatientId}", id);
        return await _unitOfWork.Patients.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Patient?> GetPatientByPeselAsync(string pesel, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie pacjenta po PESEL");
        return await _unitOfWork.Patients.GetByIdentificationNumberAsync(pesel, cancellationToken);
    }

    public async Task<IEnumerable<Patient>> SearchPatientsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Wyszukiwanie pacjentów: {SearchTerm}", searchTerm);
        return await _unitOfWork.Patients.SearchByNameAsync(searchTerm, cancellationToken);
    }

    public async Task<IEnumerable<Patient>> GetAllPatientsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie wszystkich pacjentów");
        return await _unitOfWork.Patients.GetAllAsync(cancellationToken);
    }

    public async Task<Patient?> GetPatientWithHistoryAsync(int patientId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie pacjenta z historią wizyt: {PatientId}", patientId);
        return await _unitOfWork.Patients.GetWithVisitsAsync(patientId, cancellationToken);
    }

    // ==================== COMMANDS ====================

    public async Task<Patient> CreatePatientAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tworzenie nowego pacjenta: {FirstName} {LastName}", patient.FirstName, patient.LastName);

        try
        {
            // Walidacja
            if (!await ValidatePatientDataAsync(patient, cancellationToken))
            {
                throw new InvalidOperationException("Dane pacjenta są nieprawidłowe");
            }

            // Sprawdzenie duplikatu PESEL
            if (!string.IsNullOrWhiteSpace(patient.IdentificationNumber))
            {
                var existing = await _unitOfWork.Patients
                    .GetByIdentificationNumberAsync(patient.IdentificationNumber, cancellationToken);

                if (existing != null)
                {
                    throw new InvalidOperationException($"Pacjent z numerem {patient.IdentificationNumber} już istnieje");
                }
            }

            // Dodanie pacjenta
            var createdPatient = await _unitOfWork.Patients.AddAsync(patient, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditService.LogOperationAsync(
                userId: patient.CreatedById ?? 0,
                operationType: "CREATE",
                entityName: nameof(Patient),
                entityId: createdPatient.Id.ToString(),
                newValues: $"{patient.FirstName} {patient.LastName}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Utworzono pacjenta ID: {PatientId}", createdPatient.Id);
            return createdPatient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia pacjenta");
            throw;
        }
    }

    public async Task<Patient> UpdatePatientAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aktualizacja pacjenta ID: {PatientId}", patient.Id);

        try
        {
            // Walidacja
            if (!await ValidatePatientDataAsync(patient, cancellationToken))
            {
                throw new InvalidOperationException("Dane pacjenta są nieprawidłowe");
            }

            var existing = await _unitOfWork.Patients.GetByIdAsync(patient.Id, cancellationToken);
            if (existing == null)
            {
                throw new InvalidOperationException($"Pacjent o ID {patient.Id} nie istnieje");
            }

            // Aktualizacja
            _unitOfWork.Patients.Update(patient);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditService.LogOperationAsync(
                userId: patient.ModifiedById ?? 0,
                operationType: "UPDATE",
                entityName: nameof(Patient),
                entityId: patient.Id.ToString(),
                oldValues: $"{existing.FirstName} {existing.LastName}",
                newValues: $"{patient.FirstName} {patient.LastName}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Zaktualizowano pacjenta ID: {PatientId}", patient.Id);
            return patient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji pacjenta ID: {PatientId}", patient.Id);
            throw;
        }
    }

    public async Task DeletePatientAsync(int patientId, int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Usuwanie pacjenta ID: {PatientId} przez użytkownika {UserId}", patientId, userId);

        try
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, cancellationToken);
            if (patient == null)
            {
                throw new InvalidOperationException($"Pacjent o ID {patientId} nie istnieje");
            }

            // Soft delete
            await _unitOfWork.Patients.SoftDeleteAsync(patientId, userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditService.LogOperationAsync(
                userId: userId,
                operationType: "DELETE",
                entityName: nameof(Patient),
                entityId: patientId.ToString(),
                oldValues: $"{patient.FirstName} {patient.LastName}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Usunięto pacjenta ID: {PatientId}", patientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania pacjenta ID: {PatientId}", patientId);
            throw;
        }
    }

    // ==================== VALIDATION ====================

    public Task<bool> ValidatePatientDataAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        if (patient == null)
            return Task.FromResult(false);

        if (string.IsNullOrWhiteSpace(patient.FirstName))
            return Task.FromResult(false);

        if (string.IsNullOrWhiteSpace(patient.LastName))
            return Task.FromResult(false);

        if (patient.DateOfBirth > DateTime.Today)
            return Task.FromResult(false);

        // Walidacja PESEL (opcjonalna - dla polskich pacjentów)
        if (!string.IsNullOrWhiteSpace(patient.IdentificationNumber))
        {
            if (patient.IdentificationNumber.Length != 11 ||
                !patient.IdentificationNumber.All(char.IsDigit))
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}
