using System.Security.Cryptography;
using System.Text;

namespace ASMED.EDM.Core.Helpers;

/// <summary>
/// Wspólna logika hashowania i generowania haseł użytkowników.
/// Uwaga: SHA256 jest tu zachowane dla zgodności z istniejącymi danymi (UserService).
/// W nowych wdrożeniach rozważ użycie PBKDF2/BCrypt/Argon2.
/// </summary>
public static class PasswordHelper
{
    private const string AllowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyPassword(string password, string hash)
        => HashPassword(password) == hash;

    /// <summary>Generuje losowe, silne hasło (12 znaków).</summary>
    public static string GenerateRandomPassword(int length = 12)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);
        foreach (var b in bytes)
        {
            sb.Append(AllowedChars[b % AllowedChars.Length]);
        }
        return sb.ToString();
    }
}
