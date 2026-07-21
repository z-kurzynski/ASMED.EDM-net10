using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Interfaces.Services;

/// <summary>
/// Serwis zarządzania wizytami i harmonogramem
/// </summary>
public interface IVisitService
{
    // Queries
    Task<Visit?> GetVisitByIdAsync(int visitId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Visit>> GetPatientVisitsAsync(int patientId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Visit>> GetDoctorVisitsAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<Visit>> GetVisitsByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IEnumerable<Visit>> GetUpcomingVisitsForPatientAsync(int patientId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Visit>> GetUpcomingVisitsForDoctorAsync(int doctorId, CancellationToken cancellationToken = default);

    // Commands
    Task<Visit> ScheduleVisitAsync(Visit visit, CancellationToken cancellationToken = default);
    Task<Visit> UpdateVisitAsync(Visit visit, CancellationToken cancellationToken = default);
    Task CancelVisitAsync(int visitId, string reason, CancellationToken cancellationToken = default);
    Task<Visit> CompleteVisitAsync(int visitId, string notes, CancellationToken cancellationToken = default);
    Task<Visit> CheckInPatientAsync(int visitId, CancellationToken cancellationToken = default);

    // Business Logic
    Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime startTime, int durationMinutes, CancellationToken cancellationToken = default);
    Task<IEnumerable<TimeSlot>> GetAvailableTimeSlotsAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default);
}

/// <summary>
/// Reprezentuje wolny slot czasowy dla wizyty
/// </summary>
public record TimeSlot(DateTime StartTime, DateTime EndTime, int DurationMinutes);
