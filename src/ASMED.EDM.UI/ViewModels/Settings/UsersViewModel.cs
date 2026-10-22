using System.Collections.ObjectModel;
using System.Windows;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Helpers;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels.Settings;

/// <summary>
/// ViewModel dla widoku Użytkownicy (Baza Danych -> Ustawienia -> Użytkownicy).
/// Migracja: UzytkownicyViewModel (Access/ODBC) -> EF Core / MySQL, WPF MVVM.
/// </summary>
public partial class UsersViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UsersViewModel> _logger;
    private readonly INotificationService _notificationService;
    private readonly IDoctorEdmProfileDialogService _doctorEdmProfileDialogService;

    [ObservableProperty]
    private ObservableCollection<User> _users = new();

    [ObservableProperty]
    private ObservableCollection<LoginHistory> _loginHistory = new();

    [ObservableProperty]
    private User? _selectedUser;

    // Pola formularza
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveUserCommand))]
    private string? _formUsername;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveUserCommand))]
    private string? _formPassword;

    [ObservableProperty]
    private string? _formEmail;

    [ObservableProperty]
    private string? _formFirstName;

    [ObservableProperty]
    private string? _formLastName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDoctorEdmProfileAvailable))]
    private UserRole _formRole = UserRole.Receptionist;

    [ObservableProperty]
    private bool _formIsActive = true;

    [ObservableProperty]
    private bool _showPassword;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveUserCommand))]
    [NotifyPropertyChangedFor(nameof(IsDoctorEdmProfileAvailable))]
    private bool _isEditMode;

    public ObservableCollection<UserRole> AvailableRoles { get; } = new()
    {
        UserRole.Administrator,
        UserRole.Doctor,
        UserRole.Nurse,
        UserRole.Receptionist,
        UserRole.ReadOnly
    };

    public string ShowPasswordIcon => ShowPassword ? "🙈" : "👁️";

    public string SaveButtonLabel => IsEditMode ? "💾 Zapisz zmiany" : "➕ Dodaj użytkownika";

    /// <summary>Czy pokazać przycisk danych EDM lekarza (edycja użytkownika z rolą Doctor).</summary>
    public bool IsDoctorEdmProfileAvailable => IsEditMode && FormRole == UserRole.Doctor;

    public UsersViewModel(
        IServiceScopeFactory scopeFactory,
        ILogger<UsersViewModel> logger,
        INotificationService notificationService,
        IDoctorEdmProfileDialogService doctorEdmProfileDialogService)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _doctorEdmProfileDialogService = doctorEdmProfileDialogService ?? throw new ArgumentNullException(nameof(doctorEdmProfileDialogService));
    }

    public override async Task OnNavigatedToAsync()
        => await LoadUsersAsync();

    partial void OnShowPasswordChanged(bool value)
        => OnPropertyChanged(nameof(ShowPasswordIcon));

    partial void OnIsEditModeChanged(bool value)
        => OnPropertyChanged(nameof(SaveButtonLabel));

    partial void OnSelectedUserChanged(User? value)
        => PopulateFormFromSelected();

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        IsBusy = true;
        BusyMessage = "Ładowanie użytkowników...";
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var users = await unitOfWork.Users.GetAllAsync();

            Users = new ObservableCollection<User>(users.OrderBy(u => u.Username));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd ładowania użytkowników");
            _notificationService.ShowToast($"Błąd ładowania użytkowników: {ex.Message}", ToastType.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private bool CanSaveUser()
    {
        if (!IsEditMode)
        {
            return !string.IsNullOrWhiteSpace(FormUsername) && !string.IsNullOrWhiteSpace(FormPassword);
        }

        return !string.IsNullOrWhiteSpace(FormUsername);
    }

    [RelayCommand(CanExecute = nameof(CanSaveUser))]
    private async Task SaveUserAsync()
    {
        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            if (IsEditMode && SelectedUser != null)
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var existing = await unitOfWork.Users.GetByIdAsync(SelectedUser.Id);
                if (existing == null)
                {
                    _notificationService.ShowToast("Nie znaleziono użytkownika", ToastType.Error);
                    return;
                }

                var previousRole = existing.Role;

                existing.Email = string.IsNullOrWhiteSpace(FormEmail) ? null : FormEmail;
                existing.FirstName = FormFirstName ?? string.Empty;
                existing.LastName = FormLastName ?? string.Empty;
                existing.Role = FormRole;
                existing.IsActive = FormIsActive;

                await userService.UpdateUserAsync(existing);

                if (!string.IsNullOrWhiteSpace(FormPassword))
                {
                    var passwordSet = await userService.AdminSetPasswordAsync(existing.Id, FormPassword);
                    if (!passwordSet)
                    {
                        _notificationService.ShowToast(
                            $"Dane użytkownika {FormUsername} zapisane, ale nie udało się zmienić hasła (hasło musi mieć min. 6 znaków).",
                            ToastType.Error);
                        return;
                    }
                }

                _notificationService.ShowToast($"Użytkownik {FormUsername} zaktualizowany", ToastType.Success);

                if (existing.Role == UserRole.Doctor)
                {
                    await EnsureDoctorEdmProfileAsync(
                        existing.Id,
                        GetDisplayName(existing),
                        forceDialog: previousRole != UserRole.Doctor);
                }
            }
            else
            {
                var newUser = new User
                {
                    Username = FormUsername ?? string.Empty,
                    Email = string.IsNullOrWhiteSpace(FormEmail) ? null : FormEmail,
                    FirstName = FormFirstName ?? string.Empty,
                    LastName = FormLastName ?? string.Empty,
                    Role = FormRole,
                    IsActive = true
                };

                var createdUser = await userService.CreateUserAsync(newUser, FormPassword ?? string.Empty);

                _notificationService.ShowToast($"Użytkownik {FormUsername} dodany", ToastType.Success);

                if (createdUser.Role == UserRole.Doctor)
                {
                    await EnsureDoctorEdmProfileAsync(createdUser.Id, GetDisplayName(createdUser), forceDialog: true);
                }
            }

            await LoadUsersAsync();
            ClearForm();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd zapisu użytkownika");
            var detail = GetInnermostMessage(ex);
            _notificationService.ShowToast($"Błąd zapisu: {detail}", ToastType.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetInnermostMessage(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
        {
            current = current.InnerException;
        }

        return current.Message;
    }

    private static string GetDisplayName(User user)
        => string.IsNullOrWhiteSpace(user.FullName) ? user.Username : $"{user.FullName.Trim()} ({user.Username})";

    /// <summary>
    /// Dla użytkownika z rolą Doctor otwiera dialog danych EDM, jeśli profil lekarza
    /// nie istnieje lub rola została właśnie zmieniona na Doctor.
    /// </summary>
    private async Task EnsureDoctorEdmProfileAsync(int userId, string displayName, bool forceDialog)
    {
        bool hasProfile;
        using (var scope = _scopeFactory.CreateScope())
        {
            var doctorService = scope.ServiceProvider.GetRequiredService<IDoctorService>();
            hasProfile = await doctorService.GetDoctorByUserIdAsync(userId) != null;
        }

        if (hasProfile && !forceDialog)
        {
            return;
        }

        var saved = _doctorEdmProfileDialogService.ShowDialog(userId, displayName);

        if (saved)
        {
            _notificationService.ShowToast($"Zapisano dane EDM lekarza: {displayName}", ToastType.Success);
        }
        else if (!hasProfile)
        {
            _notificationService.ShowToast(
                $"Użytkownik {displayName} ma rolę Doctor, ale nie uzupełniono danych EDM — nie będzie mógł podpisywać dokumentów. " +
                "Uzupełnij je przyciskiem „Dane EDM lekarza”.",
                ToastType.Warning,
                durationMs: 6000);
        }
    }

    [RelayCommand]
    private async Task EditDoctorEdmProfileAsync()
    {
        if (SelectedUser == null)
        {
            return;
        }

        if (SelectedUser.Role != UserRole.Doctor)
        {
            _notificationService.ShowToast(
                "Najpierw zapisz zmianę roli użytkownika na Doctor.",
                ToastType.Warning);
            return;
        }

        try
        {
            await EnsureDoctorEdmProfileAsync(SelectedUser.Id, GetDisplayName(SelectedUser), forceDialog: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd obsługi danych EDM lekarza");
            _notificationService.ShowToast($"Błąd: {GetInnermostMessage(ex)}", ToastType.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteUserAsync()
    {
        if (SelectedUser == null) return;

        var result = MessageBox.Show(
            $"Czy na pewno dezaktywować użytkownika?\n\n" +
            $"Użytkownik: {SelectedUser.Username}\n" +
            $"Rola: {SelectedUser.Role}",
            "Potwierdzenie",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            await userService.DeleteUserAsync(SelectedUser.Id, SelectedUser.Id);

            _notificationService.ShowToast($"Użytkownik {SelectedUser.Username} dezaktywowany", ToastType.Success);
            await LoadUsersAsync();
            ClearForm();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd dezaktywacji użytkownika");
            _notificationService.ShowToast($"Błąd: {ex.Message}", ToastType.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        IsEditMode = false;
        ShowPassword = false;
        SelectedUser = null;
        FormUsername = string.Empty;
        FormPassword = string.Empty;
        FormEmail = string.Empty;
        FormFirstName = string.Empty;
        FormLastName = string.Empty;
        FormRole = UserRole.Receptionist;
        FormIsActive = true;
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (SelectedUser == null) return;

        var newPassword = PasswordHelper.GenerateRandomPassword();

        var result = MessageBox.Show(
            $"Nowe hasło dla użytkownika: {SelectedUser.Username}\n\n" +
            $"Hasło: {newPassword}\n\n" +
            $"Czy zastosować to hasło?",
            "Zmiana hasła",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            var success = await userService.AdminSetPasswordAsync(SelectedUser.Id, newPassword);

            if (success)
            {
                MessageBox.Show(
                    $"Hasło zmienione dla: {SelectedUser.Username}\n\nNowe hasło: {newPassword}\n\nSKOPIUJ I ZAPISZ TO HASŁO!",
                    "Sukces",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                _notificationService.ShowToast("Nie udało się zmienić hasła", ToastType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd zmiany hasła");
            _notificationService.ShowToast($"Błąd: {ex.Message}", ToastType.Error);
        }
    }

    [RelayCommand]
    private void GeneratePassword()
    {
        FormPassword = PasswordHelper.GenerateRandomPassword();
        MessageBox.Show(
            $"Wygenerowano hasło: {FormPassword}\n\nSkopiuj i zapisz to hasło!",
            "Hasło wygenerowane",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
        => ShowPassword = !ShowPassword;

    [RelayCommand]
    private async Task LoadLoginHistoryAsync()
    {
        if (SelectedUser == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var history = await unitOfWork.LoginHistories.GetByUserIdAsync(SelectedUser.Id, maxRecords: 500);

            LoginHistory = new ObservableCollection<LoginHistory>(history);

            _notificationService.ShowToast(
                $"Załadowano {LoginHistory.Count} wpisów dla {SelectedUser.Username}",
                ToastType.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd ładowania historii logowań");
            _notificationService.ShowToast($"Błąd ładowania historii: {ex.Message}", ToastType.Error);
        }
    }

    private void PopulateFormFromSelected()
    {
        if (SelectedUser == null)
        {
            return;
        }

        IsEditMode = true;
        ShowPassword = true;
        FormUsername = SelectedUser.Username;
        FormPassword = string.Empty;
        FormEmail = SelectedUser.Email;
        FormFirstName = SelectedUser.FirstName;
        FormLastName = SelectedUser.LastName;
        FormRole = SelectedUser.Role;
        FormIsActive = SelectedUser.IsActive;

        LoginHistory.Clear();
    }
}
