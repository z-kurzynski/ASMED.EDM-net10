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
    private readonly IP1PatientVerificationService? _p1VerificationService;

    public PatientService(
        IUnitOfWork unitOfWork,
        ILogger<PatientService> logger,
        IAuditService auditService,
        IP1PatientVerificationService? p1VerificationService = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _p1VerificationService = p1VerificationService;
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

            // Informacyjna weryfikacja pacjenta w P1 (nie blokuje operacji)
            await VerifyPatientInP1Async(createdPatient, cancellationToken);

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

            // Informacyjna weryfikacja pacjenta w P1 (nie blokuje operacji)
            await VerifyPatientInP1Async(patient, cancellationToken);

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

    // ==================== P1 (E-ZDROWIE) ====================

    /// <summary>
    /// Informacyjna weryfikacja pacjenta w P1 po numerze PESEL.
    /// Wynik logowany (bez PESEL) — rozbieżność danych nie blokuje operacji.
    /// </summary>
    private async Task VerifyPatientInP1Async(Patient patient, CancellationToken cancellationToken)
    {
        if (_p1VerificationService is null || string.IsNullOrWhiteSpace(patient.IdentificationNumber))
        {
            return;
        }

        try
        {
            var result = await _p1VerificationService.VerifyPatientByPeselAsync(patient, cancellationToken);

            if (!result.Found)
            {
                _logger.LogWarning("Pacjent ID {PatientId} nie został odnaleziony w P1", patient.Id);
            }
            else if (!result.DataMatches)
            {
                _logger.LogWarning("Dane pacjenta ID {PatientId} różnią się od danych w P1", patient.Id);
            }
            else
            {
                _logger.LogInformation("Pacjent ID {PatientId} zweryfikowany pozytywnie w P1", patient.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd weryfikacji pacjenta ID {PatientId} w P1", patient.Id);
        }
    }
}
