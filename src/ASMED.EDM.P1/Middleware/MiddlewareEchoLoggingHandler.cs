using Microsoft.Extensions.Logging;
using System.Text;

namespace ASMED.EDM.P1.Middleware;

/// <summary>
/// DelegatingHandler logujący "surowy" ruch HTTP (request/response) wymieniany z Middleware,
/// bez żadnej interpretacji/dekompresji na poziomie aplikacji — pomocne przy diagnozowaniu
/// niespójności między tym co widać w Postmanie a tym co dostaje HttpClient.
/// </summary>
public class MiddlewareEchoLoggingHandler : DelegatingHandler
{
    private readonly ILogger<MiddlewareEchoLoggingHandler> _logger;

    public MiddlewareEchoLoggingHandler(ILogger<MiddlewareEchoLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestBody = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : "(brak treści)";

        var requestHeaders = FormatHeaders(request.Headers, request.Content?.Headers);

        _logger.LogInformation(
            "==> ECHO REQUEST {Method} {Uri}\nNagłówki:\n{Headers}\nTreść:\n{Body}",
            request.Method, request.RequestUri, requestHeaders, requestBody);

        var response = await base.SendAsync(request, cancellationToken);

        // Czytamy treść tak jak przyszła "z drutu" (po ewentualnej dekompresji przez handler transportowy),
        // ale bez żadnego dodatkowego przetwarzania po naszej stronie.
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseHeaders = FormatHeaders(response.Headers, response.Content.Headers);

        _logger.LogInformation(
            "<== ECHO RESPONSE {StatusCode} dla {Method} {Uri}\nNagłówki:\n{Headers}\nTreść (długość {Length}):\n{Body}",
            (int)response.StatusCode, request.Method, request.RequestUri, responseHeaders, responseBody.Length, responseBody);

        return response;
    }

    private static string FormatHeaders(System.Net.Http.Headers.HttpHeaders headers, System.Net.Http.Headers.HttpContentHeaders? contentHeaders)
    {
        var sb = new StringBuilder();
        foreach (var header in headers)
            sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");

        if (contentHeaders is not null)
            foreach (var header in contentHeaders)
                sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");

        return sb.Length == 0 ? "  (brak)" : sb.ToString().TrimEnd();
    }
}
