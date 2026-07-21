using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Implementacja serwisu zarządzania lekarzami
/// </summary>
public class DoctorService : IDoctorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DoctorService> _logger;
    private readonly IAuditService _auditService;

    public DoctorService(
        IUnitOfWork unitOfWork,
        ILogger<DoctorService> logger,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // ==================== QUERIES ====================

    public async Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie lekarza o ID: {DoctorId}", doctorId);
        return await _unitOfWork.Doctors.GetWithUserAsync(doctorId, cancellationToken);
    }

    public async Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie lekarza dla użytkownika ID: {UserId}", userId);
        return await _unitOfWork.Doctors.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie wszystkich lekarzy");
        return await _unitOfWork.Doctors.GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie lekarzy o specjalizacji: {Specialization}", specialization);
        return await _unitOfWork.Doctors.GetBySpecializationAsync(specialization, cancellationToken);
    }

    public async Task<IEnumerable<Doctor>> GetAcceptingNewPatientsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie lekarzy przyjmujących nowych pacjentów");
        return await _unitOfWork.Doctors.GetAcceptingNewPatientsAsync(cancellationToken);
    }

    // ==================== COMMANDS ====================

    public async Task<Doctor> CreateDoctorAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tworzenie nowego lekarza: {License}", doctor.MedicalLicenseNumber);

        try
        {
            // Walidacja numeru licencji
            if (string.IsNullOrWhiteSpace(doctor.MedicalLicenseNumber))
            {
                throw new InvalidOperationException("Numer licencji lekarskiej jest wymagany");
            }

            // Sprawdzenie duplikatu licencji
            var existing = await _unitOfWork.Doctors
                .GetByMedicalLicenseNumberAsync(doctor.MedicalLicenseNumber, cancellationToken);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"Lekarz z numerem licencji {doctor.MedicalLicenseNumber} już istnieje");
            }

            // Sprawdzenie czy User istnieje
            var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"Użytkownik o ID {doctor.UserId} nie istnieje");
            }

            // Dodanie lekarza
            var createdDoctor = await _unitOfWork.Doctors.AddAsync(doctor, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditService.LogOperationAsync(
                userId: doctor.CreatedById ?? 0,
                operationType: "CREATE",
                entityName: nameof(Doctor),
                entityId: createdDoctor.Id.ToString(),
                newValues: $"License: {doctor.MedicalLicenseNumber}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Utworzono lekarza ID: {DoctorId}", createdDoctor.Id);
            return createdDoctor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia lekarza");
            throw;
        }
    }

    public async Task<Doctor> UpdateDoctorAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aktualizacja lekarza ID: {DoctorId}", doctor.Id);

        try
        {
            var existing = await _unitOfWork.Doctors.GetByIdAsync(doctor.Id, cancellationToken);
            if (existing == null)
            {
                throw new InvalidOperationException($"Lekarz o ID {doctor.Id} nie istnieje");
            }

            _unitOfWork.Doctors.Update(doctor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: doctor.ModifiedById ?? 0,
                operationType: "UPDATE",
                entityName: nameof(Doctor),
                entityId: doctor.Id.ToString(),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Zaktualizowano lekarza ID: {DoctorId}", doctor.Id);
            return doctor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji lekarza ID: {DoctorId}", doctor.Id);
            throw;
        }
    }

    public async Task DeleteDoctorAsync(int doctorId, int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Usuwanie lekarza ID: {DoctorId}", doctorId);

        try
        {
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(doctorId, cancellationToken);
            if (doctor == null)
            {
                throw new InvalidOperationException($"Lekarz o ID {doctorId} nie istnieje");
            }

            await _unitOfWork.Doctors.SoftDeleteAsync(doctorId, userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: userId,
                operationType: "DELETE",
                entityName: nameof(Doctor),
                entityId: doctorId.ToString(),
                oldValues: doctor.MedicalLicenseNumber,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Usunięto lekarza ID: {DoctorId}", doctorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania lekarza ID: {DoctorId}", doctorId);
            throw;
        }
    }

    // ==================== SCHEDULES ====================

    public async Task<DoctorSchedule> AddScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dodawanie grafiku dla lekarza ID: {DoctorId}", schedule.DoctorId);

        try
        {
            // Sprawdzenie czy lekarz istnieje
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(schedule.DoctorId, cancellationToken);
            if (doctor == null)
            {
                throw new InvalidOperationException($"Lekarz o ID {schedule.DoctorId} nie istnieje");
            }

            // Sprawdzenie czy grafik na ten dzień już istnieje
            var existing = await _unitOfWork.DoctorSchedules
                .GetByDoctorAndDayAsync(schedule.DoctorId, schedule.DayOfWeek, cancellationToken);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"Grafik dla lekarza {schedule.DoctorId} w dzień {schedule.DayOfWeek} już istnieje");
            }

            var created = await _unitOfWork.DoctorSchedules.AddAsync(schedule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Dodano grafik ID: {ScheduleId}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas dodawania grafiku");
            throw;
        }
    }

    public async Task<IEnumerable<DoctorSchedule>> GetDoctorScheduleAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie grafiku lekarza ID: {DoctorId}", doctorId);
        return await _unitOfWork.DoctorSchedules.GetByDoctorIdAsync(doctorId, cancellationToken);
    }

    public async Task UpdateScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aktualizacja grafiku ID: {ScheduleId}", schedule.Id);

        try
        {
            _unitOfWork.DoctorSchedules.Update(schedule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Zaktualizowano grafik ID: {ScheduleId}", schedule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji grafiku ID: {ScheduleId}", schedule.Id);
            throw;
        }
    }
}
