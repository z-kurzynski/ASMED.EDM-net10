using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium grafików lekarzy
/// </summary>
public interface IDoctorScheduleRepository : IRepository<DoctorSchedule>
{
    /// <summary>
    /// Pobiera grafik lekarza
    /// </summary>
    Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera grafik lekarza na dany dzień tygodnia
    /// </summary>
    Task<DoctorSchedule?> GetByDoctorAndDayAsync(int doctorId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich dostępnych lekarzy w danym dniu tygodnia
    /// </summary>
    Task<IEnumerable<DoctorSchedule>> GetAvailableDoctorsOnDayAsync(DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repozytorium dokumentacji medycznej
/// </summary>
public interface IMedicalRecordRepository : IRepository<MedicalRecord>
{
    /// <summary>
    /// Pobiera dokumentację pacjenta
    /// </summary>
    Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera dokumentację z wizyty
    /// </summary>
    Task<IEnumerable<MedicalRecord>> GetByVisitIdAsync(int visitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera dokumentację po typie
    /// </summary>
    Task<IEnumerable<MedicalRecord>> GetByTypeAsync(string recordType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repozytorium recept
/// </summary>
public interface IPrescriptionRepository : IRepository<Prescription>
{
    /// <summary>
    /// Pobiera recepty pacjenta
    /// </summary>
    Task<IEnumerable<Prescription>> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera recepty z wizyty
    /// </summary>
    Task<IEnumerable<Prescription>> GetByVisitIdAsync(int visitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera recepty lekarza
    /// </summary>
    Task<IEnumerable<Prescription>> GetByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera niezrealizowane recepty
    /// </summary>
    Task<IEnumerable<Prescription>> GetUndispensedPrescriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera recepty wygasające w ciągu N dni
    /// </summary>
    Task<IEnumerable<Prescription>> GetExpiringPrescriptionsAsync(int daysUntilExpiry, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repozytorium logów audytowych
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Pobiera logi użytkownika
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera logi dla encji
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera logi z zakresu dat
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera nieudane operacje
    /// </summary>
    Task<IEnumerable<AuditLog>> GetFailedOperationsAsync(CancellationToken cancellationToken = default);
}
