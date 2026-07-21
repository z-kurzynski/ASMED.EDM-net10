using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Services;

/// <summary>
/// Serwis zarządzania pacjentami
/// </summary>
public interface IPatientService
{
    // Queries
    Task<Patient?> GetPatientByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Patient?> GetPatientByPeselAsync(string pesel, CancellationToken cancellationToken = default);
    Task<IEnumerable<Patient>> SearchPatientsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Patient>> GetAllPatientsAsync(CancellationToken cancellationToken = default);
    Task<Patient?> GetPatientWithHistoryAsync(int patientId, CancellationToken cancellationToken = default);

    // Commands
    Task<Patient> CreatePatientAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<Patient> UpdatePatientAsync(Patient patient, CancellationToken cancellationToken = default);
    Task DeletePatientAsync(int patientId, int userId, CancellationToken cancellationToken = default);
    Task<bool> ValidatePatientDataAsync(Patient patient, CancellationToken cancellationToken = default);
}
