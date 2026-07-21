using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium pacjentów z metodami specjalistycznymi
/// </summary>
public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>
    /// Wyszukuje pacjentów po nazwisku (fuzzy match)
    /// </summary>
    Task<IEnumerable<Patient>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera pacjenta po numerze identyfikacyjnym (PESEL)
    /// </summary>
    Task<Patient?> GetByIdentificationNumberAsync(string identificationNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera pacjenta wraz z historią wizyt
    /// </summary>
    Task<Patient?> GetWithVisitsAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera pacjentów urodzonych w danym okresie
    /// </summary>
    Task<IEnumerable<Patient>> GetByBirthDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera pacjentów z nadchodzącymi wizytami
    /// </summary>
    Task<IEnumerable<Patient>> GetPatientsWithUpcomingVisitsAsync(CancellationToken cancellationToken = default);
}
