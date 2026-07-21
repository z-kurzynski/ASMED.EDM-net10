using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium lekarzy
/// </summary>
public interface IDoctorRepository : IRepository<Doctor>
{
    /// <summary>
    /// Pobiera lekarza po ID użytkownika
    /// </summary>
    Task<Doctor?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera lekarza wraz z danymi użytkownika
    /// </summary>
    Task<Doctor?> GetWithUserAsync(int doctorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera lekarzy po specjalizacji
    /// </summary>
    Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera lekarzy którzy przyjmują nowych pacjentów
    /// </summary>
    Task<IEnumerable<Doctor>> GetAcceptingNewPatientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera lekarza po numerze PWZ
    /// </summary>
    Task<Doctor?> GetByMedicalLicenseNumberAsync(string licenseNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera lekarzy wraz z grafikiem pracy
    /// </summary>
    Task<IEnumerable<Doctor>> GetWithSchedulesAsync(CancellationToken cancellationToken = default);
}
