using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Przechowuje w pamięci informacje o aktualnie zalogowanym użytkowniku (singleton na czas życia aplikacji).
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    public User? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser != null;

    public void SetCurrentUser(User user)
    {
        CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
    }

    public void SignOut()
    {
        CurrentUser = null;
    }
}
