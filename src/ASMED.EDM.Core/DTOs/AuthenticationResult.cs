using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.DTOs;

/// <summary>
/// Powód nieudanego uwierzytelnienia.
/// </summary>
public enum AuthenticationFailureReason
{
    None = 0,
    InvalidCredentials,
    Inactive,
    Locked
}

/// <summary>
/// Wynik próby uwierzytelnienia użytkownika.
/// </summary>
/// <param name="User">Zalogowany użytkownik (null przy niepowodzeniu).</param>
/// <param name="FailureReason">Powód niepowodzenia.</param>
/// <param name="LockedUntilUtc">Koniec blokady (UTC) — null dla blokady bezterminowej.</param>
public sealed record AuthenticationResult(
    User? User,
    AuthenticationFailureReason FailureReason,
    DateTime? LockedUntilUtc = null)
{
    public bool Succeeded => User != null;

    public static AuthenticationResult Success(User user) => new(user, AuthenticationFailureReason.None);

    public static AuthenticationResult Failed(AuthenticationFailureReason reason, DateTime? lockedUntilUtc = null)
        => new(null, reason, lockedUntilUtc);
}
