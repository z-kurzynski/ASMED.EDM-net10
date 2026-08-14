using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace ASMED.EDM.UI.ViewModels.Settings;

/// <summary>
/// Element listy uprawnień — reprezentuje jedną zakładkę z możliwością zaznaczenia dostępu dla wybranej roli.
/// </summary>
public partial class TabPermissionItem : ObservableObject
{
    public int AppTabId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSubTab { get; set; }

    [ObservableProperty]
    private bool _isGranted;
}

/// <summary>
/// ViewModel edytora uprawnień ról do zakładek aplikacji (Ustawienia -> Role i uprawnienia).
/// </summary>
public partial class RolesPermissionsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ObservableCollection<UserRole> Roles { get; } = new(Enum.GetValues<UserRole>());

    public ObservableCollection<TabPermissionItem> Tabs { get; } = new();

    [ObservableProperty]
    private UserRole _selectedRole = UserRole.Doctor;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public RolesPermissionsViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadAsync();
    }

    partial void OnSelectedRoleChanged(UserRole value)
    {
        _ = ReloadPermissionsForRoleAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

            var allTabs = await permissionService.GetAllTabsAsync();

            Tabs.Clear();
            foreach (var tab in allTabs)
            {
                Tabs.Add(new TabPermissionItem
                {
                    AppTabId = tab.Id,
                    Key = tab.Key,
                    DisplayName = tab.DisplayName,
                    IsSubTab = tab.ParentKey != null
                });
            }

            await ReloadPermissionsForRoleAsync(permissionService);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadPermissionsForRoleAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        await ReloadPermissionsForRoleAsync(permissionService);
    }

    private async Task ReloadPermissionsForRoleAsync(IPermissionService permissionService)
    {
        var allowedKeys = await permissionService.GetAllowedTabKeysAsync(SelectedRole);

        foreach (var tab in Tabs)
        {
            tab.IsGranted = allowedKeys.Contains(tab.Key);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

            var selectedTabIds = Tabs.Where(t => t.IsGranted).Select(t => t.AppTabId);
            await permissionService.SetPermissionsForRoleAsync(SelectedRole, selectedTabIds);
            StatusMessage = $"Zapisano uprawnienia dla roli {SelectedRole}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd podczas zapisu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
