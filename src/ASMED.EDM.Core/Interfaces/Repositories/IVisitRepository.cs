using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium wizyt
/// </summary>
public interface IVisitRepository : IRepository<Visit>
{
    /// <summary>
    /// Pobiera wizytę wraz z pacjentem i lekarzem
    /// </summary>
    Task<Visit?> GetWithDetailsAsync(int visitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wizyty pacjenta
    /// </summary>
    Task<IEnumerable<Visit>> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wizyty lekarza
    /// </summary>
    Task<IEnumerable<Visit>> GetByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wizyty w danym dniu
    /// </summary>
    Task<IEnumerable<Visit>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wizyty lekarza w danym dniu
    /// </summary>
    Task<IEnumerable<Visit>> GetByDoctorAndDateAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wizyty w zakresie dat
    /// </summary>
    Task<IEnumerable<Visit>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wizyty o określonym statusie
    /// </summary>
    Task<IEnumerable<Visit>> GetByStatusAsync(VisitStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera nadchodzące wizyty pacjenta
    /// </summary>
    Task<IEnumerable<Visit>> GetUpcomingVisitsByPatientAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera nadchodzące wizyty lekarza
    /// </summary>
    Task<IEnumerable<Visit>> GetUpcomingVisitsByDoctorAsync(int doctorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza czy lekarz ma wizytę w danym czasie
    /// </summary>
    Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime startTime, DateTime endTime, int? excludeVisitId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera nieopłacone wizyty
    /// </summary>
    Task<IEnumerable<Visit>> GetUnpaidVisitsAsync(CancellationToken cancellationToken = default);
}
