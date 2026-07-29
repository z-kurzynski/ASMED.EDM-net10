using System.Text.Json;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.P1.Client;
using ASMED.EDM.P1.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASMED.EDM.P1.Services;

/// <summary>
/// Serwis wyszukiwania pacjenta w P1 po PESEL.
/// </summary>
public class P1PatientService : IP1PatientVerificationService
{
    private readonly P1Client _client;
    private readonly ILogger<P1PatientService> _logger;
    private readonly string _peselOid;

    public P1PatientService(P1Client client, IOptions<P1Settings> settings, ILogger<P1PatientService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _peselOid = settings?.Value?.PeselOid ?? "2.16.840.1.113883.3.4424.1.1.616";
    }

    public Task<P1PatientVerificationResult> VerifyPatientByPeselAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        if (patient == null)
            throw new ArgumentNullException(nameof(patient));

        // Deleguj do SearchByPeselDirectAsync
        return SearchByPeselDirectAsync(patient.IdentificationNumber, cancellationToken);
    }

    public async Task<P1PatientVerificationResult> SearchByPeselDirectAsync(string pesel, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pesel))
        {
            throw new ArgumentException("PESEL nie może być pusty.", nameof(pesel));
        }

        _logger.LogInformation("Weryfikacja pacjenta PESEL: {Pesel} w P1", pesel);

        try
        {
            // P1 wymaga POST z parametrami FHIR w JSON wewnątrz XML Envelope
            // Dla wyszukiwania Patient używamy endpoint z parametrami jako JSON body
            string searchJson = $$"""
            {
              "resourceType": "Parameters",
              "parameter": [
                {
                  "name": "identifier",
                  "valueString": "urn:oid:{{_peselOid}}|{{pesel}}"
                }
              ]
            }
            """;

            string url = "Patient/_search";
            string xmlResponse = await _client.SendFhirAsync(url, searchJson, cancellationToken);

            // Wyodrębnij JSON z XML Envelope
            string fhirJson = ExtractFhirJsonFromEnvelope(xmlResponse);

            // Parsuj FHIR Bundle
            return ParsePatientBundle(fhirJson, pesel);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Błąd HTTP podczas weryfikacji PESEL {Pesel} w P1", pesel);
            return new P1PatientVerificationResult
            {
                Found = false,
                ErrorMessage = $"Błąd połączenia z P1: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas weryfikacji PESEL {Pesel} w P1", pesel);
            return new P1PatientVerificationResult
            {
                Found = false,
                ErrorMessage = $"Nieoczekiwany błąd: {ex.Message}"
            };
        }
    }

    private string ExtractFhirJsonFromEnvelope(string xmlEnvelope)
    {
        // Prosta ekstrakcja: znajdź <FhirData>...</FhirData> i unescape XML
        int start = xmlEnvelope.IndexOf("<FhirData");
        if (start < 0) return xmlEnvelope; // brak envelope — zwróć cały XML

        start = xmlEnvelope.IndexOf('>', start) + 1;
        int end = xmlEnvelope.IndexOf("</FhirData>", start);
        if (end < 0) return xmlEnvelope;

        string escaped = xmlEnvelope.Substring(start, end - start).Trim();
        return System.Net.WebUtility.HtmlDecode(escaped);
    }

    private P1PatientVerificationResult ParsePatientBundle(string fhirJson, string pesel)
    {
        using var doc = JsonDocument.Parse(fhirJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("resourceType", out var rt) || rt.GetString() != "Bundle")
        {
            _logger.LogWarning("Odpowiedź P1 nie jest Bundle: {Json}", fhirJson);
            return new P1PatientVerificationResult
            {
                Found = false,
                ErrorMessage = "Nieprawidłowa odpowiedź P1 (brak Bundle)"
            };
        }

        if (!root.TryGetProperty("entry", out var entries) || entries.GetArrayLength() == 0)
        {
            _logger.LogInformation("Brak wpisu dla PESEL {Pesel} w P1", pesel);
            return new P1PatientVerificationResult
            {
                Found = false,
                ErrorMessage = "Nie znaleziono pacjenta w P1"
            };
        }

        var firstEntry = entries[0];
        if (!firstEntry.TryGetProperty("resource", out var resource))
        {
            return new P1PatientVerificationResult
            {
                Found = false,
                ErrorMessage = "Brak resource w Bundle entry"
            };
        }

        // ID pacjenta
        string? patientId = resource.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        // Imię i nazwisko
        string firstName = string.Empty;
        string lastName = string.Empty;

        if (resource.TryGetProperty("name", out var names) && names.GetArrayLength() > 0)
        {
            var name = names[0];
            if (name.TryGetProperty("given", out var givens) && givens.GetArrayLength() > 0)
            {
                firstName = givens[0].GetString() ?? string.Empty;
            }
            if (name.TryGetProperty("family", out var fam))
            {
                lastName = fam.GetString() ?? string.Empty;
            }
        }

        // Data urodzenia
        DateTime? birthDate = null;
        if (resource.TryGetProperty("birthDate", out var bdProp))
        {
            if (DateTime.TryParse(bdProp.GetString(), out var bd))
            {
                birthDate = bd;
            }
        }

        // Płeć
        string? gender = resource.TryGetProperty("gender", out var gProp) ? gProp.GetString() : null;

        // Adres
        string? address = null;
        if (resource.TryGetProperty("address", out var addrs) && addrs.GetArrayLength() > 0)
        {
            var addr = addrs[0];
            List<string> parts = new();
            if (addr.TryGetProperty("line", out var lines))
            {
                foreach (var line in lines.EnumerateArray())
                {
                    parts.Add(line.GetString() ?? string.Empty);
                }
            }
            if (addr.TryGetProperty("city", out var city))
            {
                parts.Add(city.GetString() ?? string.Empty);
            }
            if (addr.TryGetProperty("postalCode", out var postal))
            {
                parts.Add(postal.GetString() ?? string.Empty);
            }
            address = string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        // Telefon
        string? phone = null;
        if (resource.TryGetProperty("telecom", out var telecoms))
        {
            foreach (var tc in telecoms.EnumerateArray())
            {
                if (tc.TryGetProperty("system", out var sys) && sys.GetString() == "phone")
                {
                    if (tc.TryGetProperty("value", out var val))
                    {
                        phone = val.GetString();
                        break;
                    }
                }
            }
        }

        _logger.LogInformation("Znaleziono pacjenta P1 ID={Id}, {FirstName} {LastName}, ur. {BirthDate}", 
            patientId, firstName, lastName, birthDate);

        return new P1PatientVerificationResult
        {
            Found = true,
            DataMatches = true, // TODO: porównaj z danymi lokalnymi jeśli dostępne
            P1PatientId = patientId,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = birthDate,
            Gender = gender,
            Address = address,
            Phone = phone
        };
    }
}
