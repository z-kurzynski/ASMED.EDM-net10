using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Implementacja serwisu zarządzania użytkownikami
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IAuditService _auditService;

    public UserService(
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    // ==================== QUERIES ====================

    public async Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie użytkownika o ID: {UserId}", userId);
        return await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie użytkownika po nazwie: {Username}", username);
        return await _unitOfWork.Users.GetByUsernameAsync(username, cancellationToken);
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie użytkownika po email: {Email}", email);
        return await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Pobieranie aktywnych użytkowników");
        return await _unitOfWork.Users.GetActiveUsersAsync(cancellationToken);
    }

    // ==================== COMMANDS ====================

    public async Task<User> CreateUserAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tworzenie nowego użytkownika: {Username}", user.Username);

        try
        {
            // Walidacja
            if (string.IsNullOrWhiteSpace(user.Username))
            {
                throw new InvalidOperationException("Nazwa użytkownika jest wymagana");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                throw new InvalidOperationException("Hasło musi mieć minimum 8 znaków");
            }

            // Sprawdzenie unikalności nazwy użytkownika
            var existingByUsername = await _unitOfWork.Users.GetByUsernameAsync(user.Username, cancellationToken);
            if (existingByUsername != null)
            {
                throw new InvalidOperationException($"Użytkownik o nazwie {user.Username} już istnieje");
            }

            // Sprawdzenie unikalności email
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var existingByEmail = await _unitOfWork.Users.GetByEmailAsync(user.Email, cancellationToken);
                if (existingByEmail != null)
                {
                    throw new InvalidOperationException($"Użytkownik z adresem {user.Email} już istnieje");
                }
            }

            // Hash hasła
            user.PasswordHash = HashPassword(password);
            user.IsActive = true;
            user.CreatedAt = DateTime.UtcNow;

            var createdUser = await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditService.LogOperationAsync(
                userId: createdUser.Id,
                operationType: "CREATE_USER",
                entityName: nameof(User),
                entityId: createdUser.Id.ToString(),
                newValues: $"Username: {user.Username}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Utworzono użytkownika ID: {UserId}", createdUser.Id);
            return createdUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia użytkownika");
            throw;
        }
    }

    public async Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aktualizacja użytkownika ID: {UserId}", user.Id);

        try
        {
            var existing = await _unitOfWork.Users.GetByIdAsync(user.Id, cancellationToken);
            if (existing == null)
            {
                throw new InvalidOperationException($"Użytkownik o ID {user.Id} nie istnieje");
            }

            // Nie zmieniamy hasła w tej metodzie
            user.PasswordHash = existing.PasswordHash;
            user.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: user.ModifiedById ?? user.Id,
                operationType: "UPDATE_USER",
                entityName: nameof(User),
                entityId: user.Id.ToString(),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Zaktualizowano użytkownika ID: {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji użytkownika ID: {UserId}", user.Id);
            throw;
        }
    }

    public async Task DeleteUserAsync(int userId, int deletedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Usuwanie użytkownika ID: {UserId}", userId);

        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"Użytkownik o ID {userId} nie istnieje");
            }

            await _unitOfWork.Users.SoftDeleteAsync(userId, deletedBy, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: deletedBy,
                operationType: "DELETE_USER",
                entityName: nameof(User),
                entityId: userId.ToString(),
                oldValues: user.Username,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Usunięto użytkownika ID: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania użytkownika ID: {UserId}", userId);
            throw;
        }
    }

    // ==================== AUTHENTICATION & SECURITY ====================

    public async Task<User?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Próba logowania użytkownika: {Username}", username);

        try
        {
            var user = await _unitOfWork.Users.GetByUsernameAsync(username, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Użytkownik {Username} nie istnieje", username);
                return null;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Użytkownik {Username} jest nieaktywny", username);
                return null;
            }

            if (user.IsLocked)
            {
                _logger.LogWarning("Konto użytkownika {Username} jest zablokowane", username);
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Nieprawidłowe hasło dla użytkownika {Username}", username);

                user.FailedLoginAttempts++;
                user.LastFailedLoginAt = DateTime.UtcNow;

                // Blokada po 5 nieudanych próbach
                if (user.FailedLoginAttempts >= 5)
                {
                    user.IsLocked = true;
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                    _logger.LogWarning("Konto {Username} zablokowane po 5 nieudanych próbach", username);
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return null;
            }

            // Sukces logowania
            user.LastLoginAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LastFailedLoginAt = null;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: user.Id,
                operationType: "LOGIN",
                entityName: nameof(User),
                entityId: user.Id.ToString(),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Zalogowano użytkownika {Username}", username);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas uwierzytelniania użytkownika {Username}", username);
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Zmiana hasła dla użytkownika ID: {UserId}", userId);

        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return false;
            }

            if (!VerifyPassword(oldPassword, user.PasswordHash))
            {
                _logger.LogWarning("Nieprawidłowe stare hasło dla użytkownika ID: {UserId}", userId);
                return false;
            }

            if (newPassword.Length < 8)
            {
                throw new InvalidOperationException("Nowe hasło musi mieć minimum 8 znaków");
            }

            user.PasswordHash = HashPassword(newPassword);
            user.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditService.LogOperationAsync(
                userId: userId,
                operationType: "CHANGE_PASSWORD",
                entityName: nameof(User),
                entityId: userId.ToString(),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Zmieniono hasło dla użytkownika ID: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zmiany hasła użytkownika ID: {UserId}", userId);
            return false;
        }
    }

    public async Task<string> GeneratePasswordResetTokenAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generowanie tokenu resetowania hasła dla użytkownika ID: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"Użytkownik o ID {userId} nie istnieje");
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wygenerowano token resetowania hasła dla użytkownika ID: {UserId}", userId);
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resetowanie hasła za pomocą tokenu");

        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        var user = users.FirstOrDefault(u => u.PasswordResetToken == token);

        if (user == null)
        {
            _logger.LogWarning("Nieprawidłowy token resetowania hasła");
            return false;
        }

        if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning("Token resetowania hasła wygasł");
            return false;
        }

        user.PasswordHash = HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.ModifiedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogOperationAsync(
            userId: user.Id,
            operationType: "RESET_PASSWORD",
            entityName: nameof(User),
            entityId: user.Id.ToString(),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Zresetowano hasło dla użytkownika ID: {UserId}", user.Id);
        return true;
    }

    public async Task LockAccountAsync(int userId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Blokowanie konta użytkownika ID: {UserId}, powód: {Reason}", userId, reason);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"Użytkownik o ID {userId} nie istnieje");
        }

        user.IsLocked = true;
        user.LockedUntil = null; // Blokada manualna bez limitu czasowego

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogOperationAsync(
            userId: userId,
            operationType: "LOCK_ACCOUNT",
            entityName: nameof(User),
            entityId: userId.ToString(),
            newValues: reason,
            cancellationToken: cancellationToken);
    }

    public async Task UnlockAccountAsync(int userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Odblokowywanie konta użytkownika ID: {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"Użytkownik o ID {userId} nie istnieje");
        }

        user.IsLocked = false;
        user.LockedUntil = null;
        user.FailedLoginAttempts = 0;
        user.LastFailedLoginAt = null;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogOperationAsync(
            userId: userId,
            operationType: "UNLOCK_ACCOUNT",
            entityName: nameof(User),
            entityId: userId.ToString(),
            cancellationToken: cancellationToken);
    }

    // ==================== HELPERS ====================

    private static string HashPassword(string password)
    {
        // W produkcji użyj BCrypt, Argon2 lub PasswordHasher z Identity
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }
}
