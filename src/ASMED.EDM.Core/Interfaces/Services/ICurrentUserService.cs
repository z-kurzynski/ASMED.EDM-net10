using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Services;

/// <summary>
/// Reprezentuje aktualnie zalogowanego użytkownika w sesji aplikacji desktopowej.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Aktualnie zalogowany użytkownik, lub null jeśli nikt nie jest zalogowany.
    /// </summary>
    User? CurrentUser { get; }

    /// <summary>
    /// Czy jakikolwiek użytkownik jest obecnie zalogowany.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Ustawia zalogowanego użytkownika po pomyślnym uwierzytelnieniu.
    /// </summary>
    void SetCurrentUser(User user);

    /// <summary>
    /// Czyści sesję (wylogowanie).
    /// </summary>
    void SignOut();
}
