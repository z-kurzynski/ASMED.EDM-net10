using System.Text.Json.Serialization;

namespace ASMED.EDM.P1.Middleware.Models;

/// <summary>
/// Odpowiedź endpointu POST /login Middleware.
/// </summary>
public class MiddlewareLoginResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
