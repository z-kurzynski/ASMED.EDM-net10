namespace ASMED.EDM.Core.Interfaces.Services;

/// <summary>
/// Wynik wygenerowania danych do parowania z aplikacją authenticator (TOTP).
/// </summary>
public class TotpEnrollmentResult
{
    /// <summary>Nieszyfrowany sekret Base32 (do ręcznego wpisania, jeśli QR nie zadziała).</summary>
    public string SecretBase32 { get; set; } = string.Empty;

    /// <summary>URI w formacie otpauth://totp/... do wygenerowania kodu QR.</summary>
    public string OtpAuthUri { get; set; } = string.Empty;
}

/// <summary>
/// Serwis obsługujący autoryzację podpisu lekarza (TOTP, RFC 6238) — niezależną od hasła
/// logowania do aplikacji. Lekarz paruje aplikację authenticator (np. Google/Microsoft
/// Authenticator) raz, a następnie potwierdza swoją tożsamość kodem z telefonu przed
/// wystawieniem/weryfikacją orzeczenia w module P1/Medycyna Pracy.
/// </summary>
public interface IDoctorSignatureAuthorizationService
{
    /// <summary>
    /// Sprawdza, czy dany lekarz ma już skonfigurowaną (aktywną) autoryzację TOTP.
    /// </summary>
    Task<bool> IsEnrolledAsync(int doctorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generuje nowy sekret TOTP dla lekarza (jeszcze niezapisany jako aktywny —
    /// wymaga potwierdzenia pierwszym poprawnym kodem przez <see cref="ConfirmEnrollmentAsync"/>).
    /// </summary>
    Task<TotpEnrollmentResult> BeginEnrollmentAsync(int doctorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Potwierdza parowanie — weryfikuje podany kod względem ostatnio wygenerowanego
    /// (niezatwierdzonego) sekretu i, jeśli poprawny, aktywuje TOTP dla lekarza.
    /// </summary>
    Task<bool> ConfirmEnrollmentAsync(int doctorId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Weryfikuje kod TOTP wpisany przez lekarza względem jego aktywnego sekretu.
    /// </summary>
    Task<bool> VerifyCodeAsync(int doctorId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa (dezaktywuje) konfigurację TOTP lekarza — wymaga ponownego sparowania.
    /// </summary>
    Task ResetEnrollmentAsync(int doctorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Czy w ramach bieżącej sesji aplikacji dany lekarz potwierdził już tożsamość podpisu
    /// (autoryzacja jest jednorazowa na czas trwania sesji, niezależna od logowania).
    /// </summary>
    bool IsSessionAuthorized(int doctorId);

    /// <summary>
    /// Oznacza sesję jako autoryzowaną dla danego lekarza (po poprawnej weryfikacji kodu).
    /// </summary>
    void MarkSessionAuthorized(int doctorId);

    /// <summary>
    /// Czyści autoryzację sesji dla danego lekarza (np. przy wylogowaniu).
    /// </summary>
    void ClearSessionAuthorization(int doctorId);
}
