using System.Security.Cryptography;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Implementacja autoryzacji podpisu lekarza w oparciu o TOTP (RFC 6238).
/// Sekret przechowywany jest w bazie w formie zaszyfrowanej (DPAPI, per-maszyna),
/// a stan autoryzacji sesji trzymany jest wyłącznie w pamięci procesu (singleton),
/// niezależnie od logowania do aplikacji.
/// </summary>
public class DoctorSignatureAuthorizationService : IDoctorSignatureAuthorizationService
{
    private const string OtpIssuer = "ASMED EDM";
    private static readonly byte[] DpapiEntropy = System.Text.Encoding.UTF8.GetBytes("ASMED.EDM.DoctorSignature.v1");

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DoctorSignatureAuthorizationService> _logger;

    // Tymczasowe (niezatwierdzone) sekrety enrollmentu, trzymane w pamięci do czasu potwierdzenia kodem.
    private static readonly Dictionary<int, string> _pendingSecrets = new();

    // Autoryzacja sesji: doctorId -> potwierdzone (na czas trwania procesu aplikacji).
    private static readonly HashSet<int> _authorizedSessions = new();
    private static readonly object _sessionLock = new();

    public DoctorSignatureAuthorizationService(IUnitOfWork unitOfWork, ILogger<DoctorSignatureAuthorizationService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsEnrolledAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(doctorId, cancellationToken);
        return doctor is { TotpEnabled: true } && !string.IsNullOrEmpty(doctor.TotpSecretKeyEncrypted);
    }

    public Task<TotpEnrollmentResult> BeginEnrollmentAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secretBase32 = Base32Encoding.ToString(secretBytes);

        lock (_sessionLock)
        {
            _pendingSecrets[doctorId] = secretBase32;
        }

        var otpAuthUri = $"otpauth://totp/{Uri.EscapeDataString(OtpIssuer)}:doctor{doctorId}?secret={secretBase32}&issuer={Uri.EscapeDataString(OtpIssuer)}&digits=6&period=30";

        return Task.FromResult(new TotpEnrollmentResult
        {
            SecretBase32 = secretBase32,
            OtpAuthUri = otpAuthUri
        });
    }

    public async Task<bool> ConfirmEnrollmentAsync(int doctorId, string code, CancellationToken cancellationToken = default)
    {
        string? pendingSecret;
        lock (_sessionLock)
        {
            _pendingSecrets.TryGetValue(doctorId, out pendingSecret);
        }

        if (string.IsNullOrEmpty(pendingSecret))
        {
            _logger.LogWarning("Brak oczekującego sekretu TOTP dla lekarza {DoctorId} — najpierw wywołaj BeginEnrollmentAsync.", doctorId);
            return false;
        }

        if (!IsCodeValid(pendingSecret, code))
        {
            return false;
        }

        var doctor = await _unitOfWork.Doctors.GetByIdAsync(doctorId, cancellationToken)
            ?? throw new InvalidOperationException($"Nie znaleziono lekarza o Id={doctorId}.");

        doctor.TotpSecretKeyEncrypted = Encrypt(pendingSecret);
        doctor.TotpEnabled = true;
        doctor.TotpEnrolledAt = DateTime.UtcNow;

        _unitOfWork.Doctors.Update(doctor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        lock (_sessionLock)
        {
            _pendingSecrets.Remove(doctorId);
            _authorizedSessions.Add(doctorId);
        }

        _logger.LogInformation("Skonfigurowano autoryzację podpisu (TOTP) dla lekarza {DoctorId}.", doctorId);
        return true;
    }

    public async Task<bool> VerifyCodeAsync(int doctorId, string code, CancellationToken cancellationToken = default)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(doctorId, cancellationToken);
        if (doctor is not { TotpEnabled: true } || string.IsNullOrEmpty(doctor.TotpSecretKeyEncrypted))
        {
            _logger.LogWarning("Lekarz {DoctorId} nie ma skonfigurowanej autoryzacji podpisu (TOTP).", doctorId);
            return false;
        }

        string secret;
        try
        {
            secret = Decrypt(doctor.TotpSecretKeyEncrypted);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Nie udało się odszyfrować sekretu TOTP lekarza {DoctorId}.", doctorId);
            return false;
        }

        var isValid = IsCodeValid(secret, code);
        if (isValid)
        {
            MarkSessionAuthorized(doctorId);
        }

        return isValid;
    }

    public async Task ResetEnrollmentAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(doctorId, cancellationToken);
        if (doctor == null) return;

        doctor.TotpEnabled = false;
        doctor.TotpSecretKeyEncrypted = null;
        doctor.TotpEnrolledAt = null;

        _unitOfWork.Doctors.Update(doctor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        ClearSessionAuthorization(doctorId);
    }

    public bool IsSessionAuthorized(int doctorId)
    {
        lock (_sessionLock)
        {
            return _authorizedSessions.Contains(doctorId);
        }
    }

    public void MarkSessionAuthorized(int doctorId)
    {
        lock (_sessionLock)
        {
            _authorizedSessions.Add(doctorId);
        }
    }

    public void ClearSessionAuthorization(int doctorId)
    {
        lock (_sessionLock)
        {
            _authorizedSessions.Remove(doctorId);
        }
    }

    private static bool IsCodeValid(string secretBase32, string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var totp = new Totp(Base32Encoding.ToBytes(secretBase32));
        // Tolerancja +/-1 okresu (30s) na rozjazdy zegara telefonu.
        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));
    }

    private static string Encrypt(string plainText)
    {
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(plainBytes, DpapiEntropy, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string Decrypt(string encryptedBase64)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedBase64);
        var plainBytes = ProtectedData.Unprotect(encryptedBytes, DpapiEntropy, DataProtectionScope.LocalMachine);
        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
