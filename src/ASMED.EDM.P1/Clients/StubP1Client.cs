using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.P1.Clients;

/// <summary>
/// Stub klienta P1 — zwraca dane testowe bez połączenia z rzeczywistym systemem P1.
/// Używany gdy UseStub = true lub certyfikaty TLS nie są skonfigurowane.
/// </summary>
public class StubP1Client : IP1PatientVerificationService, IMedicalEventService
{
    private readonly ILogger<StubP1Client> _logger;

    public StubP1Client(ILogger<StubP1Client> logger)
    {
        _logger = logger;
    }

    public Task<P1PatientVerificationResult> VerifyPatientByPeselAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("STUB: VerifyPatientByPeselAsync dla PESEL {Pesel}", patient.IdentificationNumber);
        var result = new P1PatientVerificationResult
        {
            Found        = true,
            DataMatches  = true,
            FirstName    = patient.FirstName,
            LastName     = patient.LastName,
            DateOfBirth  = patient.DateOfBirth,
            P1PatientId  = $"stub-patient-{patient.Id}",
            Gender       = "Unknown"
        };
        return Task.FromResult(result);
    }

    public Task<P1PatientVerificationResult> SearchByPeselDirectAsync(string pesel, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("STUB: SearchByPeselDirectAsync dla PESEL {Pesel}", pesel.Length > 4 ? pesel[..4] + "***" : "***");
        var result = new P1PatientVerificationResult
        {
            Found        = true,
            DataMatches  = true,
            FirstName    = "Jan",
            LastName     = "Kowalski",
            DateOfBirth  = new DateTime(1980, 1, 1),
            P1PatientId  = $"stub-patient-{pesel}",
            Gender       = "Male"
        };
        return Task.FromResult(result);
    }

    public Task<P1MedicalEventResult> RegisterVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("STUB: RegisterVisitAsync dla wizyty {VisitId}", visit.Id);
        var result = new P1MedicalEventResult
        {
            Success    = true,
            P1EventId  = $"stub-event-{visit.Id}-{Guid.NewGuid().ToString("N")[..8]}"
        };
        return Task.FromResult(result);
    }

    public Task<P1MedicalEventResult> UpdateVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("STUB: UpdateVisitAsync dla wizyty {VisitId}", visit.Id);
        var result = new P1MedicalEventResult
        {
            Success   = true,
            P1EventId = visit.P1EventId
        };
        return Task.FromResult(result);
    }

    public Task<P1MedicalEventResult> CancelVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("STUB: CancelVisitAsync dla wizyty {VisitId}", visit.Id);
        var result = new P1MedicalEventResult
        {
            Success   = true,
            P1EventId = visit.P1EventId
        };
        return Task.FromResult(result);
    }
}
