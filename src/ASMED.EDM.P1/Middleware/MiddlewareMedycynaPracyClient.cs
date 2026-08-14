using ASMED.EDM.P1.Configuration;
using ASMED.EDM.P1.Middleware.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ASMED.EDM.P1.Middleware;

/// <summary>
/// Klient modułu MedycynaPracy API Middleware (endpointy /medycynapracy/*).
/// Obsługuje wyszukiwanie/odczyt/rejestrację/aktualizację pacjenta oraz weryfikację
/// (bez zapisu) i wystawianie orzeczeń lekarza medycyny pracy.
/// </summary>
public interface IMiddlewareMedycynaPracyClient
{
    /// <summary>POST /medycynapracy/pacjent/wyszukaj — wyszukanie pacjenta w Rejestrze Pacjentów P1.</summary>
    Task<MiddlewarePacjentResponse<MpPacjentSearchBundle>> WyszukajPacjentaAsync(MpKryteriaWyszukiwaniaPacjenta kryteria, CancellationToken cancellationToken = default);

    /// <summary>POST /medycynapracy/pacjent — rejestracja nowego pacjenta w Rejestrze Pacjentów P1.</summary>
    Task<MiddlewarePacjentResponse<MpPacjentCreatedResult>> ZarejestrujPacjentaAsync(MpPacjent pacjent, CancellationToken cancellationToken = default);

    /// <summary>POST /medycynapracy/pacjent/odczyt — odczyt pacjenta po identyfikatorze zasobu (id z rejestracji/wyszukania).</summary>
    Task<MiddlewarePacjentResponse<MpPacjent>> OdczytajPacjentaAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>PUT /medycynapracy/pacjent — aktualizacja danych osobowych pacjenta.</summary>
    Task<MiddlewarePacjentResponse<MpPacjentCreatedResult>> AktualizujPacjentaAsync(string id, MpPacjent pacjent, CancellationToken cancellationToken = default);

    /// <summary>POST /medycynapracy/orzeczenie/weryfikuj — walidacja orzeczenia bez zapisu (weryfikacjaDokumentu).</summary>
    Task<MiddlewareOrzeczenieResponse<object>> WeryfikujOrzeczenieAsync(MpOrzeczenie orzeczenie, CancellationToken cancellationToken = default);

    /// <summary>POST /medycynapracy/orzeczenie — wystawienie (przesłanie) orzeczenia lekarza medycyny pracy do P1.</summary>
    Task<MiddlewareOrzeczenieResponse<object>> WystawOrzeczenieAsync(MpOrzeczenie orzeczenie, CancellationToken cancellationToken = default);
}

public class MiddlewareMedycynaPracyClient : IMiddlewareMedycynaPracyClient
{
    private readonly HttpClient _httpClient;
    private readonly IMiddlewareAuthTokenProvider _tokenProvider;
    private readonly IOptions<P1Settings> _settings;
    private readonly ILogger<MiddlewareMedycynaPracyClient> _logger;

    // Krótszy timeout niż domyślny HttpClient (100s) — pozwala szybciej wykryć sytuację,
    // gdy backend Middleware "wisi" (np. wolne/martwe wywołanie do zewnętrznego systemu P1),
    // zamiast czekać kilkadziesiąt sekund bez żadnej informacji dla użytkownika.
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);
    private const int MaxTimeoutRetries = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MiddlewareMedycynaPracyClient(
        HttpClient httpClient,
        IMiddlewareAuthTokenProvider tokenProvider,
        IOptions<P1Settings> settings,
        ILogger<MiddlewareMedycynaPracyClient> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _settings = settings;
        _logger = logger;
    }

    public Task<MiddlewarePacjentResponse<MpPacjentSearchBundle>> WyszukajPacjentaAsync(MpKryteriaWyszukiwaniaPacjenta kryteria, CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["author"] = SerializeAuthorMinimal(),
            ["pacjent"] = JsonSerializer.Serialize(kryteria, JsonOptions)
        };
        return SendAsync<MiddlewarePacjentResponse<MpPacjentSearchBundle>>(HttpMethod.Post, "medycynapracy/pacjent/wyszukaj", fields, cancellationToken);
    }

    public Task<MiddlewarePacjentResponse<MpPacjentCreatedResult>> ZarejestrujPacjentaAsync(MpPacjent pacjent, CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["author"] = SerializeAuthorMinimal(),
            ["pacjent"] = JsonSerializer.Serialize(pacjent, JsonOptions)
        };
        return SendAsync<MiddlewarePacjentResponse<MpPacjentCreatedResult>>(HttpMethod.Post, "medycynapracy/pacjent", fields, cancellationToken);
    }

    public Task<MiddlewarePacjentResponse<MpPacjent>> OdczytajPacjentaAsync(string id, CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["author"] = SerializeAuthorMinimal(),
            ["id"] = id
        };
        return SendAsync<MiddlewarePacjentResponse<MpPacjent>>(HttpMethod.Post, "medycynapracy/pacjent/odczyt", fields, cancellationToken);
    }

    public Task<MiddlewarePacjentResponse<MpPacjentCreatedResult>> AktualizujPacjentaAsync(string id, MpPacjent pacjent, CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["author"] = SerializeAuthorMinimal(),
            ["id"] = id,
            ["pacjent"] = JsonSerializer.Serialize(pacjent, JsonOptions)
        };
        return SendAsync<MiddlewarePacjentResponse<MpPacjentCreatedResult>>(HttpMethod.Put, "medycynapracy/pacjent", fields, cancellationToken);
    }

    public Task<MiddlewareOrzeczenieResponse<object>> WeryfikujOrzeczenieAsync(MpOrzeczenie orzeczenie, CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["author"] = SerializeAuthor(),
            ["idPodmiotu"] = _settings.Value.IdPodmiotu,
            ["orzeczenie"] = JsonSerializer.Serialize(orzeczenie, JsonOptions),
            ["doctor_pass"] = _settings.Value.DoctorCertificatePassword
        };
        return SendAsync<MiddlewareOrzeczenieResponse<object>>(HttpMethod.Post, "medycynapracy/orzeczenie/weryfikuj", fields, cancellationToken);
    }

    public Task<MiddlewareOrzeczenieResponse<object>> WystawOrzeczenieAsync(MpOrzeczenie orzeczenie, CancellationToken cancellationToken = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["author"] = SerializeAuthor(),
            ["idPodmiotu"] = _settings.Value.IdPodmiotu,
            ["orzeczenie"] = JsonSerializer.Serialize(orzeczenie, JsonOptions),
            ["doctor_pass"] = _settings.Value.DoctorCertificatePassword
        };
        return SendAsync<MiddlewareOrzeczenieResponse<object>>(HttpMethod.Post, "medycynapracy/orzeczenie", fields, cancellationToken);
    }

    private string SerializeAuthor()
    {
        var settings = _settings.Value;
        var author = new MpAuthor
        {
            Code = "LEK",
            AuthorType = "1",
            AuthorTypeDetail = "1",
            Pwz = settings.DoctorPwz,
            Name = settings.DoctorName,
            Surname = settings.DoctorSurname,
            ClinicKsr = settings.RpwdlBookNumber,
            ClinicName = settings.OrganizationName,
            ClinicPhone = settings.ClinicPhone,
            ClinicPostCode = settings.ClinicPostCode,
            ClinicCity = settings.ClinicCity,
            ClinicStreetName = settings.ClinicStreetName,
            ClinicHouseNumber = settings.ClinicHouseNumber,
            ClinicRegon = settings.Regon14,
            ClinicNfz = settings.ClinicNfz,
            IdIzbyLekarskiej = settings.IdIzbyLekarskiej,
            Res7 = settings.Res7,
            Res8 = settings.Res8,
            Res8Name = settings.Res8Name,
            AssistantName = settings.AssistantName,
            AssistantSurname = settings.AssistantSurname,
            AssistantId = settings.AssistantId,
            AuthorMedycynaPracyId = settings.AuthorMedycynaPracyId
        };
        return JsonSerializer.Serialize(author, JsonOptions);
    }

    /// <summary>
    /// Zwraca minimalny obiekt "author" (code, authorType, authorTypeDetail, pwz, clinicKsr) —
    /// dokładnie taki zestaw pól, jaki kolekcja Postman (mp_author_min) wysyła dla wszystkich
    /// operacji na pacjencie (wyszukaj/zarejestruj/odczytaj/aktualizuj). Wysłanie tam pełnego
    /// obiektu author (jak dla orzeczeń) powodowało błąd serwera (pusta odpowiedź HTML, status 200).
    /// </summary>
    private string SerializeAuthorMinimal()
    {
        var settings = _settings.Value;
        var author = new MpAuthor
        {
            Code = "LEK",
            AuthorType = "1",
            AuthorTypeDetail = "1",
            Pwz = settings.DoctorPwz,
            ClinicKsr = settings.RpwdlBookNumber
        };
        return JsonSerializer.Serialize(author, JsonOptions);
    }

    private async Task<TResponse> SendAsync<TResponse>(HttpMethod method, string relativeUrl, IDictionary<string, string> formFields, CancellationToken cancellationToken, bool isRetry = false, int timeoutAttempt = 0)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);

        _logger.LogInformation("Wysyłanie {Method} {Url} z polami: {Fields}",
            method, relativeUrl, string.Join(", ", formFields.Select(kv => $"{kv.Key}={kv.Value}")));

        using var request = new HttpRequestMessage(method, relativeUrl)
        {
            Content = new FormUrlEncodedContent(formFields)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Anulowanie nastąpiło z powodu naszego CancelAfter (timeout), a nie żądania użytkownika.
            if (timeoutAttempt < MaxTimeoutRetries)
            {
                _logger.LogWarning(
                    "Middleware nie odpowiedział dla {Url} w ciągu {Timeout}s (próba {Attempt}) — ponawiam próbę.",
                    relativeUrl, RequestTimeout.TotalSeconds, timeoutAttempt + 1);
                return await SendAsync<TResponse>(method, relativeUrl, formFields, cancellationToken, isRetry, timeoutAttempt + 1);
            }

            _logger.LogError(
                "Middleware nie odpowiedział dla {Url} w ciągu {Timeout}s po {Attempts} próbach.",
                relativeUrl, RequestTimeout.TotalSeconds, timeoutAttempt + 1);
            throw new TimeoutException(
                $"Middleware nie odpowiedział dla {relativeUrl} w ciągu {RequestTimeout.TotalSeconds:0}s (po {timeoutAttempt + 1} próbach). " +
                "Serwer prawdopodobnie zbyt długo przetwarza żądanie (np. wolne/zawieszone wywołanie do zewnętrznego systemu P1). Spróbuj ponownie później.");
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && !isRetry)
            {
                _logger.LogWarning("Middleware zwrócił 401 dla {Url} — odświeżam token i ponawiam próbę", relativeUrl);
                _tokenProvider.Invalidate();
                return await SendAsync<TResponse>(method, relativeUrl, formFields, cancellationToken, isRetry: true, timeoutAttempt);
            }

            response.EnsureSuccessStatusCode();

            var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(rawContent))
            {
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "(brak)";
                var contentLength = response.Content.Headers.ContentLength?.ToString() ?? "(brak)";
                var contentEncoding = string.Join(",", response.Content.Headers.ContentEncoding);
                var requestUri = response.RequestMessage?.RequestUri?.ToString() ?? "(nieznany)";
                _logger.LogError(
                    "Middleware zwrócił pustą odpowiedź dla {Url}. Status={Status}, RequestUri={RequestUri}, ContentType={ContentType}, ContentLength={ContentLength}, ContentEncoding={ContentEncoding}",
                    relativeUrl, (int)response.StatusCode, requestUri, contentType, contentLength, contentEncoding);
                throw new InvalidOperationException(
                    $"Middleware zwrócił pustą odpowiedź dla {relativeUrl} (status {(int)response.StatusCode}, RequestUri: {requestUri}, " +
                    $"Content-Type: {contentType}, Content-Length: {contentLength}, Content-Encoding: {(string.IsNullOrEmpty(contentEncoding) ? "(brak)" : contentEncoding)}). " +
                    "Sprawdź czy token jest ważny (spróbuj ponownie po odświeżeniu) oraz czy serwer nie zwraca skompresowanej treści bez odpowiedniego nagłówka.");
            }

            _logger.LogInformation("Odpowiedź Middleware dla {Url} (status {Status}): {Content}", relativeUrl, (int)response.StatusCode, rawContent);

            TResponse result;
            try
            {
                result = JsonSerializer.Deserialize<TResponse>(rawContent, JsonOptions)
                    ?? throw new InvalidOperationException($"Middleware zwrócił pustą odpowiedź dla {relativeUrl}.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Nie udało się sparsować odpowiedzi Middleware dla {Url}: {Content}", relativeUrl, rawContent);
                throw new InvalidOperationException(
                    $"Middleware zwrócił odpowiedź dla {relativeUrl}, której nie da się odczytać jako JSON. Treść odpowiedzi: {rawContent}", ex);
            }

            return result;
        }
    }
}
