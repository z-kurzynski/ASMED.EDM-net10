using ASMED.EDM.P1.Configuration;
using ASMED.EDM.P1.Middleware.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace ASMED.EDM.P1.Middleware;

/// <summary>
/// Dostarcza i buforuje token JWT Middleware (endpoint POST /login),
/// odświeżając go automatycznie po wygaśnięciu.
/// </summary>
public interface IMiddlewareAuthTokenProvider
{
    /// <summary>Zwraca ważny token dostępowy, logując się ponownie w razie potrzeby.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Unieważnia bieżący token (np. po otrzymaniu 401 z API).</summary>
    void Invalidate();
}

public class MiddlewareAuthTokenProvider : IMiddlewareAuthTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<P1Settings> _settings;
    private readonly ILogger<MiddlewareAuthTokenProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    // Margines bezpieczeństwa przed wygaśnięciem tokenu, aby uniknąć wyścigu 401.
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(30);

    public MiddlewareAuthTokenProvider(HttpClient httpClient, IOptions<P1Settings> settings, ILogger<MiddlewareAuthTokenProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt - RefreshMargin)
            return _accessToken;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt - RefreshMargin)
                return _accessToken;

            await LoginAsync(cancellationToken);
            return _accessToken!;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate()
    {
        _accessToken = null;
        _expiresAt = DateTimeOffset.MinValue;
    }

    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        var settings = _settings.Value;
        _logger.LogInformation("Logowanie do Middleware jako {Username}", settings.MiddlewareUsername);

        // API wymaga danych logowania jako parametrów query string (potwierdzone kolekcją Postman),
        // nie jako multipart/form-data — inaczej serwer zwraca pustą stronę HTML zamiast JSON.
        var query = $"login?username={Uri.EscapeDataString(settings.MiddlewareUsername)}&password={Uri.EscapeDataString(settings.MiddlewarePassword)}";

        using var response = await _httpClient.PostAsync(query, content: null, cancellationToken);

        // 3xx nie jest sukcesem przy wyłączonym auto-redirect — sprawdzamy jawnie, aby dać czytelny komunikat.
        if ((int)response.StatusCode is >= 300 and < 400)
        {
            var location = response.Headers.Location?.ToString() ?? "(brak nagłówka Location)";
            _logger.LogError("Middleware /login zwrócił przekierowanie {StatusCode} -> {Location}", (int)response.StatusCode, location);
            throw new InvalidOperationException(
                $"Middleware /login zwrócił przekierowanie {(int)response.StatusCode} na adres: {location}. " +
                $"Sprawdź MiddlewareBaseUrl — żądanie POST prawdopodobnie trafia na niewłaściwy URL (np. brak/nadmiar 'https://', przekierowanie http->https, lub zły host).");
        }

        var rawContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Middleware /login zwrócił {StatusCode}: {Content}", (int)response.StatusCode, rawContent);
            throw new InvalidOperationException(
                $"Middleware /login zwrócił błąd {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                $"Sprawdź login/hasło (MiddlewareUsername/MiddlewarePassword) oraz URL Middleware. Treść odpowiedzi: {rawContent}");
        }

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "(brak)";
            var contentLength = response.Content.Headers.ContentLength?.ToString() ?? "(brak)";
            var contentEncoding = string.Join(",", response.Content.Headers.ContentEncoding);
            var requestUri = response.RequestMessage?.RequestUri?.ToString() ?? "(nieznany)";
            _logger.LogError(
                "Middleware /login zwrócił pustą odpowiedź. Status={Status}, RequestUri={RequestUri}, ContentType={ContentType}, ContentLength={ContentLength}, ContentEncoding={ContentEncoding}",
                (int)response.StatusCode, requestUri, contentType, contentLength, contentEncoding);
            throw new InvalidOperationException(
                $"Middleware /login zwrócił pustą odpowiedź (status {(int)response.StatusCode}, RequestUri: {requestUri}, Content-Type: {contentType}, " +
                $"Content-Length: {contentLength}, Content-Encoding: {(string.IsNullOrEmpty(contentEncoding) ? "(brak)" : contentEncoding)}). " +
                "Sprawdź czy MiddlewareBaseUrl wskazuje poprawny adres API oraz czy serwer wymaga dodatkowych nagłówków (np. Accept).");
        }

        MiddlewareLoginResponse result;
        try
        {
            result = JsonSerializer.Deserialize<MiddlewareLoginResponse>(rawContent)
                ?? throw new InvalidOperationException("Middleware /login zwrócił pustą odpowiedź.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Nie udało się sparsować odpowiedzi /login: {Content}", rawContent);
            throw new InvalidOperationException(
                $"Middleware /login zwrócił odpowiedź, której nie da się odczytać jako JSON. Treść odpowiedzi: {rawContent}", ex);
        }

        _accessToken = result.AccessToken;
        _expiresAt = result.ExpiresIn > 0
            ? DateTimeOffset.UtcNow.AddSeconds(result.ExpiresIn)
            : DateTimeOffset.UtcNow.AddMinutes(15);
    }
}
