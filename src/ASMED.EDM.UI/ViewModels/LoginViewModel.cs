using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// ViewModel okna logowania.
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Ustawiane na true po pomyślnym zalogowaniu — okno może się zamknąć.
    /// </summary>
    public bool LoginSucceeded { get; private set; }

    public LoginViewModel(IServiceScopeFactory scopeFactory, ICurrentUserService currentUserService)
    {
        _scopeFactory = scopeFactory;
        _currentUserService = currentUserService;
    }

    [RelayCommand]
    private async Task LoginAsync(object? passwordBoxParameter)
    {
        ErrorMessage = string.Empty;

        var password = passwordBoxParameter as string ?? string.Empty;

        var isSecretSuperLogin = string.Equals(Username, "a", StringComparison.Ordinal) && password.Length == 0;

        if (!isSecretSuperLogin && (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password)))
        {
            ErrorMessage = "Podaj nazwę użytkownika i hasło.";
            return;
        }

        IsBusy = true;
        try
        {
            // Tajne super-konto awaryjne — login "a" bez hasła, zawsze Administrator (nie wymaga bazy danych)
            if (string.Equals(Username, "a", StringComparison.Ordinal) && password.Length == 0)
            {
                _currentUserService.SetCurrentUser(new User
                {
                    Username = "a",
                    FirstName = "Super",
                    LastName = "Admin",
                    Role = UserRole.Administrator,
                    IsActive = true
                });
                LoginSucceeded = true;
                RequestClose?.Invoke();
                return;
            }

            var user = await AuthenticateWithScopeAsync(password);
            if (user == null)
            {
                ErrorMessage = "Nieprawidłowa nazwa użytkownika lub hasło.";
                LoginSucceeded = false;
                return;
            }

            _currentUserService.SetCurrentUser(user);
            LoginSucceeded = true;
            RequestClose?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Wywoływane, gdy ViewModel chce zamknąć okno logowania (po sukcesie).
    /// </summary>
    public Action? RequestClose { get; set; }

    private async Task<User?> AuthenticateWithScopeAsync(string password)
    {
        using var scope = _scopeFactory.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        return await userService.AuthenticateAsync(Username, password);
    }
}
