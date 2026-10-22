using System.Windows;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.UI.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Otwiera dialog danych lekarza wymaganych do autoryzacji w EDM / P1.
/// </summary>
public interface IDoctorEdmProfileDialogService
{
    /// <summary>Zwraca true, jeśli profil lekarza został zapisany.</summary>
    bool ShowDialog(int userId, string userDisplayName);
}

public class DoctorEdmProfileDialogService : IDoctorEdmProfileDialogService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserService _currentUserService;

    public DoctorEdmProfileDialogService(IServiceScopeFactory scopeFactory, ICurrentUserService currentUserService)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public bool ShowDialog(int userId, string userDisplayName)
    {
        var currentUserId = _currentUserService.CurrentUser?.Id;
        var dialog = new DoctorEdmProfileDialog(
            _scopeFactory,
            userId,
            userDisplayName,
            currentUserId is > 0 ? currentUserId : null)
        {
            Owner = Application.Current?.MainWindow
        };

        return dialog.ShowDialog() == true;
    }
}
