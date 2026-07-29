using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.P1.Clients;

/// <summary>
/// Implementacja stub klientów P1 — do pracy bez certyfikatów / połączenia z P1.
/// Aktywowana flagą P1Settings:UseStub = true.
/// </summary>
public class StubP1Client : IP1PatientVerificationService, IMedicalEventService
{
    private readonly ILogger<StubP1Client> _logger;

    public StubP1Client(ILogger<StubP1Client> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<P1PatientVerificationResult> VerifyPatientByPeselAsync(
        Patient patient,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patient);
        _logger.LogInformation("[STUB P1] Weryfikacja pacjenta {PatientId} — symulacja pozytywnej odpowiedzi", patient.Id);

        return Task.FromResult(new P1PatientVerificationResult
        {
            Found = true,
            DataMatches = true,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            P1PatientId = $"stub-patient-{patient.Id}"
        });
    }

    public Task<P1MedicalEventResult> RegisterVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visit);
        _logger.LogInformation("[STUB P1] Rejestracja ZM dla wizyty {VisitId} — symulacja sukcesu", visit.Id);
        return Task.FromResult(new P1MedicalEventResult
        {
            Success = true,
            P1EventId = $"stub-event-{visit.Id}-{Guid.NewGuid():N}"
        });
    }

    public Task<P1MedicalEventResult> UpdateVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visit);
        _logger.LogInformation("[STUB P1] Aktualizacja ZM dla wizyty {VisitId} — symulacja sukcesu", visit.Id);
        return Task.FromResult(new P1MedicalEventResult { Success = true, P1EventId = visit.P1EventId });
    }

    public Task<P1MedicalEventResult> CancelVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visit);
        _logger.LogInformation("[STUB P1] Anulowanie ZM dla wizyty {VisitId} — symulacja sukcesu", visit.Id);
        return Task.FromResult(new P1MedicalEventResult { Success = true, P1EventId = visit.P1EventId });
    }

    public Task<P1PatientVerificationResult> SearchByPeselDirectAsync(
        string pesel,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[STUB P1] Wyszukiwanie po PESEL {Pesel} — symulacja", pesel.Length > 4 ? pesel[..4] + "***" : "***");
        return Task.FromResult(new P1PatientVerificationResult
        {
            Found       = true,
            DataMatches = true,
            FirstName   = "Jan",
            LastName    = "Kowalski",
            DateOfBirth = new DateTime(1985, 6, 15),
            P1PatientId = $"stub-pesel-{Guid.NewGuid():N}",
            Address     = "ul. Testowa 1, 00-001 Warszawa",
            Phone       = "+48 123 456 789",
            Gender      = "Male"
        });
    }
}
