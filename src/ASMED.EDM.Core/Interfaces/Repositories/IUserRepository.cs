using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Repositories;

/// <summary>
/// Repozytorium użytkowników systemu
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Pobiera użytkownika po nazwie użytkownika
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera użytkownika po email
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera użytkownika z powiązanym lekarzem (jeśli istnieje)
    /// </summary>
    Task<User?> GetWithDoctorAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera wszystkich aktywnych użytkowników
    /// </summary>
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera użytkowników po roli
    /// </summary>
    Task<IEnumerable<User>> GetByRoleAsync(Enums.UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sprawdza czy username jest zajęty
    /// </summary>
    Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null, CancellationToken cancellationToken = default);
}
