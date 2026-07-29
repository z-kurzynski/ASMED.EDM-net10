using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Implementacja serwisu zarządzania wizytami
/// </summary>
public class VisitService : IVisitService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VisitService> _logger;
    private readonly IAuditService _auditService;
    private readonly IMedicalEventService? _medicalEventService;

    public VisitService(
        IUnitOfWork unitOfWork,
        ILogger<VisitService> logger,
        IAuditService auditService,
        IMedicalEventService? medicalEventService = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _medicalEventService = medicalEventService;
    }

    // ==================== QUERIES ====================

    public async Task<Visit?> GetVisitByIdAsync(int visitId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Visits.GetWithDetailsAsync(visitId, cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetPatientVisitsAsync(int patientId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Visits.GetByPatientIdAsync(patientId, cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetDoctorVisitsAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Visits.GetByDoctorAndDateAsync(doctorId, date, cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetVisitsByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Visits.GetByDateRangeAsync(from, to, cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetUpcomingVisitsForPatientAsync(int patientId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Visits.GetUpcomingVisitsByPatientAsync(patientId, cancellationToken);
    }

    public async Task<IEnumerable<Visit>> GetUpcomingVisitsForDoctorAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Visits.GetUpcomingVisitsByDoctorAsync(doctorId, cancellationToken);
    }

    // ==================== COMMANDS ====================

    public async Task<Visit> ScheduleVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Planowanie wizyty dla pacjenta {PatientId} z lekarzem {DoctorId}",
            visit.PatientId, visit.DoctorId);

        try
        {
            // Walidacja dostępności lekarza
            var isAvailable = await IsDoctorAvailableAsync(
                visit.DoctorId,
                visit.ScheduledDateTime,
                visit.DurationMinutes,
                cancellationToken);

            if (!isAvailable)
            {
                throw new InvalidOperationException(
                    $"Lekarz nie jest dostępny w podanym terminie: {visit.ScheduledDateTime}");
            }

            // Ustawienie statusu
            visit.Status = VisitStatus.Scheduled;

            // Zapisanie wizyty
            var createdVisit = await _unitOfWork.Visits.AddAsync(visit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit
            await _auditService.LogOperationAsync(
                userId: visit.CreatedById ?? 0,
                operationType: "SCHEDULE_VISIT",
                entityName: nameof(Visit),
                entityId: createdVisit.Id.ToString(),
                newValues: $"Patient: {visit.PatientId}, Doctor: {visit.DoctorId}, Date: {visit.ScheduledDateTime}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Wizyta zaplanowana, ID: {VisitId}", createdVisit.Id);
            return createdVisit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas planowania wizyty");
            throw;
        }
    }

    public async Task<Visit> UpdateVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aktualizacja wizyty ID: {VisitId}", visit.Id);

        try
        {
            var existing = await _unitOfWork.Visits.GetByIdAsync(visit.Id, cancellationToken);
            if (existing == null)
            {
                throw new InvalidOperationException($"Wizyta o ID {visit.Id} nie istnieje");
            }

            // Jeśli zmienia się termin, sprawdź dostępność lekarza
            if (existing.ScheduledDateTime != visit.ScheduledDateTime ||
                existing.DoctorId != visit.DoctorId)
            {
                var isAvailable = await _unitOfWork.Visits.IsDoctorAvailableAsync(
                    visit.DoctorId,
                    visit.ScheduledDateTime,
                    visit.ScheduledDateTime.AddMinutes(visit.DurationMinutes),
                    visit.Id,
                    cancellationToken);

                if (!isAvailable)
                {
                    throw new InvalidOperationException("Lekarz nie jest dostępny w nowym terminie");
                }
            }

            _unitOfWork.Visits.Update(visit);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: visit.ModifiedById ?? 0,
                operationType: "UPDATE_VISIT",
                entityName: nameof(Visit),
                entityId: visit.Id.ToString(),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Zaktualizowano wizytę ID: {VisitId}", visit.Id);
            return visit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji wizyty ID: {VisitId}", visit.Id);
            throw;
        }
    }

    public async Task CancelVisitAsync(int visitId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Anulowanie wizyty ID: {VisitId}, powód: {Reason}", visitId, reason);

        try
        {
            var visit = await _unitOfWork.Visits.GetByIdAsync(visitId, cancellationToken);
            if (visit == null)
            {
                throw new InvalidOperationException($"Wizyta o ID {visitId} nie istnieje");
            }

            visit.Status = VisitStatus.Cancelled;
            visit.CancellationReason = reason;
            visit.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Visits.Update(visit);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: visit.ModifiedById ?? 0,
                operationType: "CANCEL_VISIT",
                entityName: nameof(Visit),
                entityId: visitId.ToString(),
                newValues: $"Reason: {reason}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Wizyta anulowana ID: {VisitId}", visitId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas anulowania wizyty ID: {VisitId}", visitId);
            throw;
        }
    }

    public async Task<Visit> CompleteVisitAsync(int visitId, string notes, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Zamykanie wizyty ID: {VisitId}", visitId);

        try
        {
            var visit = await _unitOfWork.Visits.GetByIdAsync(visitId, cancellationToken);
            if (visit == null)
            {
                throw new InvalidOperationException($"Wizyta o ID {visitId} nie istnieje");
            }

            visit.Status = VisitStatus.Completed;
            visit.DoctorNotes = notes;
            visit.ActualEndTime = DateTime.UtcNow;
            visit.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Visits.Update(visit);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: visit.ModifiedById ?? 0,
                operationType: "COMPLETE_VISIT",
                entityName: nameof(Visit),
                entityId: visitId.ToString(),
                cancellationToken: cancellationToken);

            // Rejestracja Zdarzenia Medycznego w P1 (e-zdrowie)
            await RegisterMedicalEventAsync(visit, cancellationToken);

            _logger.LogInformation("Wizyta zakończona ID: {VisitId}", visitId);
            return visit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zamykania wizyty ID: {VisitId}", visitId);
            throw;
        }
    }

    public async Task<Visit> CheckInPatientAsync(int visitId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Meldowanie pacjenta na wizytę ID: {VisitId}", visitId);

        try
        {
            var visit = await _unitOfWork.Visits.GetByIdAsync(visitId, cancellationToken);
            if (visit == null)
            {
                throw new InvalidOperationException($"Wizyta o ID {visitId} nie istnieje");
            }

            visit.Status = VisitStatus.CheckedIn;
            visit.ActualStartTime = DateTime.UtcNow;
            visit.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Visits.Update(visit);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Pacjent zameldowany na wizytę ID: {VisitId}", visitId);
            return visit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas meldowania pacjenta na wizytę ID: {VisitId}", visitId);
            throw;
        }
    }

    // ==================== BUSINESS LOGIC ====================

    /// <summary>
    /// Rejestruje Zdarzenie Medyczne w P1 dla zakończonej wizyty.
    /// Błąd rejestracji nie blokuje zamknięcia wizyty — status i szczegóły trafiają do P1SyncLog (retry przez ponowienie).
    /// </summary>
    private async Task RegisterMedicalEventAsync(Visit visit, CancellationToken cancellationToken)
    {
        if (_medicalEventService is null)
        {
            return;
        }

        var syncLog = new P1SyncLog
        {
            OperationType = "RegisterVisit",
            EntityName = nameof(Visit),
            EntityId = visit.Id,
            LastAttemptAt = DateTime.UtcNow
        };

        try
        {
            var result = await _medicalEventService.RegisterVisitAsync(visit, cancellationToken);

            if (result.Success)
            {
                visit.P1EventId = result.P1EventId;
                visit.P1SyncStatus = P1SyncStatus.Synced;
                syncLog.Status = P1SyncStatus.Synced;
                syncLog.P1Identifier = result.P1EventId;
            }
            else
            {
                visit.P1SyncStatus = P1SyncStatus.Failed;
                syncLog.Status = P1SyncStatus.Failed;
                syncLog.ErrorMessage = result.ErrorMessage;
                _logger.LogWarning("Rejestracja ZM w P1 nie powiodła się dla wizyty {VisitId}: {Error}", visit.Id, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            visit.P1SyncStatus = P1SyncStatus.Failed;
            syncLog.Status = P1SyncStatus.Failed;
            syncLog.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Błąd rejestracji ZM w P1 dla wizyty {VisitId}", visit.Id);
        }

        _unitOfWork.Visits.Update(visit);
        await _unitOfWork.P1SyncLogs.AddAsync(syncLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsDoctorAvailableAsync(
        int doctorId,
        DateTime startTime,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        var endTime = startTime.AddMinutes(durationMinutes);
        return await _unitOfWork.Visits.IsDoctorAvailableAsync(
            doctorId, startTime, endTime, null, cancellationToken);
    }

    public async Task<IEnumerable<TimeSlot>> GetAvailableTimeSlotsAsync(
        int doctorId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie dostępnych slotów dla lekarza {DoctorId} w dniu {Date}", doctorId, date);

        try
        {
            // Pobierz grafik lekarza na dany dzień tygodnia
            var schedule = await _unitOfWork.DoctorSchedules
                .GetByDoctorAndDayAsync(doctorId, date.DayOfWeek, cancellationToken);

            if (schedule == null || !schedule.IsAvailable)
            {
                _logger.LogInformation("Lekarz {DoctorId} nie pracuje w dzień {DayOfWeek}",
                    doctorId, date.DayOfWeek);
                return Enumerable.Empty<TimeSlot>();
            }

            // Pobierz wizyty lekarza w danym dniu
            var existingVisits = await _unitOfWork.Visits
                .GetByDoctorAndDateAsync(doctorId, date, cancellationToken);

            // Slot duration (np. 30 minut)
            const int slotDuration = 30;

            var availableSlots = new List<TimeSlot>();
            var currentTime = date.Date.Add(schedule.StartTime);
            var endTime = date.Date.Add(schedule.EndTime);

            while (currentTime.Add(TimeSpan.FromMinutes(slotDuration)) <= endTime)
            {
                var slotEnd = currentTime.AddMinutes(slotDuration);

                // Sprawdź czy slot jest wolny
                var isOccupied = existingVisits.Any(v =>
                    v.Status != VisitStatus.Cancelled &&
                    ((v.ScheduledDateTime >= currentTime && v.ScheduledDateTime < slotEnd) ||
                     (v.ScheduledDateTime.AddMinutes(v.DurationMinutes) > currentTime &&
                      v.ScheduledDateTime.AddMinutes(v.DurationMinutes) <= slotEnd)));

                if (!isOccupied)
                {
                    availableSlots.Add(new TimeSlot(currentTime, slotEnd, slotDuration));
                }

                currentTime = slotEnd;
            }

            _logger.LogDebug("Znaleziono {Count} dostępnych slotów", availableSlots.Count);
            return availableSlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania dostępnych slotów");
            throw;
        }
    }
}
