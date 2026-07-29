using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using ASMED.EDM.P1.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASMED.EDM.P1.Client;

/// <summary>
/// Centralny klient P1 obsługujący mutual TLS + XML Envelope + WSS Signature.
/// Zgodny z wymaganiami P1: XML envelope z podpisem WSS + escaped JSON FHIR wewnątrz.
/// </summary>
public class P1Client : IDisposable
{
    private readonly HttpClient _http;
    private readonly X509Certificate2 _wssCert;
    private readonly ILogger<P1Client> _logger;

    public P1Client(IOptions<P1Settings> settings, ILogger<P1Client> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var config = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        var handler = new SocketsHttpHandler
        {
            // Wyłącz sprawdzanie CRL/OCSP - certyfikaty testowe P1 nie mają dostępnych CRL
            SslOptions = new SslClientAuthenticationOptions
            {
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                // Wymuś TLS 1.2 lub nowszy
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            }
        };

        // TLS mutual authentication
        if (!string.IsNullOrWhiteSpace(config.TlsCertificatePath))
        {
            // Załaduj CAŁĄ kolekcję certyfikatów z P12 (client + intermediate + root)
            var certCollection = new X509Certificate2Collection();
            certCollection.Import(
                config.TlsCertificatePath,
                config.TlsCertificatePassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            _logger.LogInformation("Załadowano {Count} certyfikatów z P12: {Path}", 
                certCollection.Count, config.TlsCertificatePath);

            // Znajdź certyfikat klienta (z kluczem prywatnym) i zaloguj szczegóły
            X509Certificate2? clientCert = null;
            foreach (var cert in certCollection)
            {
                _logger.LogInformation("  - {Subject}, HasPrivateKey={HasKey}", 
                    cert.Subject, cert.HasPrivateKey);

                if (cert.HasPrivateKey)
                {
                    clientCert = cert;
                }
            }

            if (clientCert == null)
            {
                _logger.LogError("Brak certyfikatu z kluczem prywatnym w {Path}", config.TlsCertificatePath);
                throw new InvalidOperationException("Certyfikat TLS nie zawiera klucza prywatnego.");
            }

            _logger.LogInformation("Certyfikat TLS załadowany: {Subject}, ważny {NotBefore} - {NotAfter}", 
                clientCert.Subject, clientCert.NotBefore, clientCert.NotAfter);

            // Dodaj TYLKO certyfikat klienta (z kluczem prywatnym)
            // TLS handshake automatycznie użyje łańcucha z P12
            var clientCertCollection = new X509Certificate2Collection(clientCert);
            handler.SslOptions.ClientCertificates = clientCertCollection;

            // UWAGA: Certyfikat testowy P1 ma NotBefore w przyszłości (2026) - pomijamy walidację daty
            handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                _logger.LogInformation("🔐 TLS Server Certificate Validation:");
                _logger.LogInformation("   Server cert: {Subject}", cert?.Subject ?? "null");
                _logger.LogInformation("   Issuer: {Issuer}", cert?.Issuer ?? "null");
                _logger.LogInformation("   SSL Errors: {Errors}", sslPolicyErrors);

                if (chain != null)
                {
                    _logger.LogInformation("   Chain status: {Status}", chain.ChainStatus.Length > 0 
                        ? string.Join(", ", chain.ChainStatus.Select(s => s.Status)) 
                        : "OK");
                }

                // W produkcji: return sslPolicyErrors == SslPolicyErrors.None;
                // W dev: ignoruj błędy daty certyfikatu
                return true; // Akceptuj wszystkie certyfikaty (tylko dev!)
            };

            // Dodaj callback dla logowania certyfikatu klienta
            handler.SslOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCerts, remoteCert, acceptableIssuers) =>
            {
                _logger.LogInformation("🔑 TLS Client Certificate Selection:");
                _logger.LogInformation("   Target host: {Host}", targetHost);
                _logger.LogInformation("   Available client certs: {Count}", localCerts?.Count ?? 0);

                if (localCerts != null && localCerts.Count > 0)
                {
                    var selectedCert = localCerts[0];
                    _logger.LogInformation("   Selected cert: {Subject}", selectedCert?.Subject ?? "null");

                    // Sprawdź czy to X509Certificate2 z kluczem prywatnym
                    if (selectedCert is X509Certificate2 cert2)
                    {
                        _logger.LogInformation("   Has private key: {HasKey}", cert2.HasPrivateKey);
                    }

                    return selectedCert;
                }

                _logger.LogWarning("   No client certificate selected!");
                return null;
            };
        }
        else
        {
            _logger.LogWarning("Brak skonfigurowanego certyfikatu TLS — połączenie mTLS nie będzie uwierzytelnione.");
        }

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(config.BaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };

        // WSS certificate for XML signature
        if (!string.IsNullOrWhiteSpace(config.WssCertificatePath))
        {
            _wssCert = new X509Certificate2(
                config.WssCertificatePath,
                config.WssCertificatePassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            if (!_wssCert.HasPrivateKey)
            {
                _logger.LogError("Certyfikat WSS {Path} NIE zawiera klucza prywatnego — podpis XML nie zadziała!", 
                    config.WssCertificatePath);
                throw new InvalidOperationException("Certyfikat WSS nie zawiera klucza prywatnego.");
            }

            _logger.LogInformation("Certyfikat WSS załadowany: {Subject}, ważny do {NotAfter}", 
                _wssCert.Subject, _wssCert.NotAfter);
        }
        else
        {
            _logger.LogWarning("Brak skonfigurowanego certyfikatu WSS — podpis XML nie będzie możliwy.");
            _wssCert = null!;
        }
    }

    /// <summary>
    /// Wysyła żądanie FHIR do P1 z podpisanym XML Envelope.
    /// </summary>
    /// <param name="relativeUrl">Ścieżka relatywna (np. "Patient?identifier=...")</param>
    /// <param name="fhirJson">JSON FHIR (pusty string dla GET)</param>
    /// <param name="cancellationToken">Token anulowania</param>
    /// <returns>Odpowiedź XML z P1</returns>
    public async Task<string> SendFhirAsync(string relativeUrl, string fhirJson, CancellationToken cancellationToken = default)
    {
        var fullUrl = new Uri(_http.BaseAddress!, relativeUrl);
        _logger.LogInformation("╔══════════════════════════════════════════════════════════════");
        _logger.LogInformation("║ P1 REQUEST");
        _logger.LogInformation("╠══════════════════════════════════════════════════════════════");
        _logger.LogInformation("║ Method: {Method}", string.IsNullOrEmpty(fhirJson) ? "GET (via POST)" : "POST");
        _logger.LogInformation("║ URL: {Url}", fullUrl);
        _logger.LogInformation("║ BaseAddress: {BaseAddress}", _http.BaseAddress);
        _logger.LogInformation("║ RelativeUrl: {RelativeUrl}", relativeUrl);
        _logger.LogInformation("╚══════════════════════════════════════════════════════════════");

        string xmlEnvelope = BuildSignedEnvelope(fhirJson);

        // Zapisz pełny XML do pliku debug
        try
        {
            var debugDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASMED.EDM.UI", "P1_Debug");
            Directory.CreateDirectory(debugDir);
            var debugFile = Path.Combine(debugDir, $"p1_request_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
            File.WriteAllText(debugFile, xmlEnvelope, Encoding.UTF8);
            _logger.LogInformation("║ Debug XML saved to: {DebugFile}", debugFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save debug XML");
        }

        _logger.LogInformation("XML Envelope (first 500 chars):\n{Xml}", 
            xmlEnvelope.Length > 500 ? xmlEnvelope.Substring(0, 500) + "..." : xmlEnvelope);

        var content = new StringContent(xmlEnvelope, Encoding.UTF8, "application/xml");

        HttpResponseMessage response;
        try
        {
            if (string.IsNullOrEmpty(fhirJson))
            {
                // GET request (z XML Envelope jako body — P1 wymaga tego formatu nawet dla GET)
                response = await _http.PostAsync(relativeUrl, content, cancellationToken);
            }
            else
            {
                // POST request
                response = await _http.PostAsync(relativeUrl, content, cancellationToken);
            }

            _logger.LogInformation("╔══════════════════════════════════════════════════════════════");
            _logger.LogInformation("║ P1 RESPONSE");
            _logger.LogInformation("╠══════════════════════════════════════════════════════════════");
            _logger.LogInformation("║ Status: {StatusCode} {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);
            _logger.LogInformation("╚══════════════════════════════════════════════════════════════");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "╔══════════════════════════════════════════════════════════════");
            _logger.LogError("║ P1 REQUEST FAILED");
            _logger.LogError("╠══════════════════════════════════════════════════════════════");
            _logger.LogError("║ URL: {Url}", fullUrl);
            _logger.LogError("║ Error: {Message}", ex.Message);
            _logger.LogError("║ Inner: {Inner}", ex.InnerException?.Message ?? "n/a");
            _logger.LogError("╚══════════════════════════════════════════════════════════════");
            throw;
        }

        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogInformation("Response body (first 500 chars):\n{Response}", 
            result.Length > 500 ? result.Substring(0, 500) + "..." : result);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("P1 błąd {StatusCode}: {Response}", response.StatusCode, result);
            throw new HttpRequestException($"P1 error {response.StatusCode}: {result}");
        }

        _logger.LogDebug("P1 Response OK: {Length} chars", result.Length);
        return result;
    }

    /// <summary>
    /// Buduje XML Envelope z podpisem WSS zgodnie z wymaganiami P1.
    /// Format: &lt;Envelope&gt;&lt;Body&gt;&lt;FhirData Id="FhirData"&gt;{escaped JSON}&lt;/FhirData&gt;&lt;/Body&gt;&lt;Signature&gt;...&lt;/Signature&gt;&lt;/Envelope&gt;
    /// </summary>
    private string BuildSignedEnvelope(string fhirJson)
    {
        // Escape JSON (XML-safe)
        string escapedJson = System.Security.SecurityElement.Escape(fhirJson);

        string xml = $@"
<Envelope xmlns=""http://schemas.xmlsoap.org/soap/envelope/"">
    <Body>
        <FhirData Id=""FhirData"">
            {escapedJson}
        </FhirData>
    </Body>
</Envelope>";

        var doc = new XmlDocument();
        doc.PreserveWhitespace = true;
        doc.LoadXml(xml);

        if (_wssCert == null)
        {
            _logger.LogWarning("Certyfikat WSS nie jest dostępny — zwracam XML bez podpisu.");
            return doc.OuterXml;
        }

        // Podpis WSS (enveloped signature)
        var signedXml = new SignedXml(doc)
        {
            SigningKey = _wssCert.GetRSAPrivateKey()
                ?? throw new InvalidOperationException("Certyfikat WSS nie zawiera klucza RSA.")
        };

        var reference = new Reference("#FhirData");
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);

        signedXml.ComputeSignature();

        XmlElement xmlSignature = signedXml.GetXml();
        doc.DocumentElement!.AppendChild(doc.ImportNode(xmlSignature, true));

        return doc.OuterXml;
    }

    public void Dispose()
    {
        _http?.Dispose();
        _wssCert?.Dispose();
        GC.SuppressFinalize(this);
    }
}
