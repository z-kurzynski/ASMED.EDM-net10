using System.Text;
using System.Text.Json;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.P1.Client;
using ASMED.EDM.P1.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASMED.EDM.P1.Services;

/// <summary>
/// Serwis rejestracji Zdarzeń Medycznych (Encounter) w P1.
/// </summary>
public class P1EncounterService : IMedicalEventService
{
    private readonly P1Client _client;
    private readonly ILogger<P1EncounterService> _logger;
    private readonly P1Settings _settings;

    public P1EncounterService(P1Client client, IOptions<P1Settings> settings, ILogger<P1EncounterService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<P1MedicalEventResult> RegisterVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        if (visit == null)
            throw new ArgumentNullException(nameof(visit));

        _logger.LogInformation("Rejestracja Zdarzenia Medycznego dla wizyty ID={VisitId}, pacjent ID={PatientId}", 
            visit.Id, visit.PatientId);

        try
        {
            string fhirJson = BuildEncounterJson(visit, "planned");
            string xmlResponse = await _client.SendFhirAsync("Encounter", fhirJson, cancellationToken);
            string resultJson = ExtractFhirJsonFromEnvelope(xmlResponse);

            // Parsuj ID z odpowiedzi
            using var doc = JsonDocument.Parse(resultJson);
            string? eventId = doc.RootElement.TryGetProperty("id", out var idProp) 
                ? idProp.GetString() 
                : null;

            _logger.LogInformation("Zdarzenie Medyczne zarejestrowane w P1: ID={EventId}", eventId);

            return new P1MedicalEventResult
            {
                Success = true,
                P1EventId = eventId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd rejestracji Zdarzenia Medycznego dla wizyty ID={VisitId}", visit.Id);
            return new P1MedicalEventResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<P1MedicalEventResult> UpdateVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        if (visit == null)
            throw new ArgumentNullException(nameof(visit));

        _logger.LogInformation("Aktualizacja Zdarzenia Medycznego dla wizyty ID={VisitId}", visit.Id);

        try
        {
            string fhirJson = BuildEncounterJson(visit, "finished");
            string url = $"Encounter/{visit.P1EventId}"; // zakładamy że Visit.P1EventId zawiera ID z P1
            string xmlResponse = await _client.SendFhirAsync(url, fhirJson, cancellationToken);
            string resultJson = ExtractFhirJsonFromEnvelope(xmlResponse);

            using var doc = JsonDocument.Parse(resultJson);
            string? eventId = doc.RootElement.TryGetProperty("id", out var idProp) 
                ? idProp.GetString() 
                : null;

            _logger.LogInformation("Zdarzenie Medyczne zaktualizowane w P1: ID={EventId}", eventId);

            return new P1MedicalEventResult
            {
                Success = true,
                P1EventId = eventId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd aktualizacji Zdarzenia Medycznego dla wizyty ID={VisitId}", visit.Id);
            return new P1MedicalEventResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<P1MedicalEventResult> CancelVisitAsync(Visit visit, CancellationToken cancellationToken = default)
    {
        if (visit == null)
            throw new ArgumentNullException(nameof(visit));

        _logger.LogInformation("Anulowanie Zdarzenia Medycznego dla wizyty ID={VisitId}", visit.Id);

        try
        {
            string fhirJson = BuildEncounterJson(visit, "cancelled");
            string url = $"Encounter/{visit.P1EventId}";
            string xmlResponse = await _client.SendFhirAsync(url, fhirJson, cancellationToken);
            string resultJson = ExtractFhirJsonFromEnvelope(xmlResponse);

            using var doc = JsonDocument.Parse(resultJson);
            string? eventId = doc.RootElement.TryGetProperty("id", out var idProp) 
                ? idProp.GetString() 
                : null;

            _logger.LogInformation("Zdarzenie Medyczne anulowane w P1: ID={EventId}", eventId);

            return new P1MedicalEventResult
            {
                Success = true,
                P1EventId = eventId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd anulowania Zdarzenia Medycznego dla wizyty ID={VisitId}", visit.Id);
            return new P1MedicalEventResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private string BuildEncounterJson(Visit visit, string status)
    {
        var encounter = new
        {
            resourceType = "Encounter",
            status = status, // planned | arrived | triaged | in-progress | onleave | finished | cancelled
            @class = new
            {
                system = "http://terminology.hl7.org/CodeSystem/v3-ActCode",
                code = "AMB", // ambulatory (wizyta ambulatoryjna)
                display = "ambulatory"
            },
            subject = new
            {
                // Używamy PESEL jako identyfikatora pacjenta w P1
                reference = $"Patient/{visit.Patient?.IdentificationNumber ?? "unknown"}"
            },
            period = new
            {
                start = visit.ScheduledDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = visit.ScheduledDateTime.AddMinutes(visit.DurationMinutes).ToString("yyyy-MM-ddTHH:mm:ss")
            },
            serviceProvider = new
            {
                identifier = new
                {
                    system = "urn:oid:2.16.840.1.113883.3.4424.2.3.1",
                    value = _settings.OrganizationIdentifier
                }
            }
        };

        return JsonSerializer.Serialize(encounter, new JsonSerializerOptions { WriteIndented = false });
    }

    private string ExtractFhirJsonFromEnvelope(string xmlEnvelope)
    {
        int start = xmlEnvelope.IndexOf("<FhirData");
        if (start < 0) return xmlEnvelope;

        start = xmlEnvelope.IndexOf('>', start) + 1;
        int end = xmlEnvelope.IndexOf("</FhirData>", start);
        if (end < 0) return xmlEnvelope;

        string escaped = xmlEnvelope.Substring(start, end - start).Trim();
        return System.Net.WebUtility.HtmlDecode(escaped);
    }
}
