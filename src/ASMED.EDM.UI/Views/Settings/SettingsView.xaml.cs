using System.Windows;
using System.Windows.Controls;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.Settings;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += SettingsView_Loaded;
    }

    private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        await ApplyTabPermissionsAsync();
    }

    /// <summary>
    /// Ukrywa podzak\u0142adki Ustawie\u0144 niedost\u0119pne dla roli aktualnie zalogowanego u\u017Cytkownika.
    /// </summary>
    private async Task ApplyTabPermissionsAsync()
    {
        try
        {
            var app = System.Windows.Application.Current as App
                      ?? throw new InvalidOperationException("App nie jest dost\u0119pna.");

            var currentUserService = app.Host.Services.GetRequiredService<ICurrentUserService>();
            var currentUser = currentUserService.CurrentUser;
            if (currentUser is null) return;

            using var scope = app.Host.Services.CreateScope();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

            var allowedKeys = await permissionService.GetAllowedTabKeysAsync(currentUser.Role);

            var tabKeyMap = new Dictionary<string, TabItem?>
            {
                ["Ustawienia.Konfiguracja"] = tabKonfiguracja,
                ["Ustawienia.Cenniki"] = tabCenniki,
                ["Ustawienia.DanePlacowki"] = tabDanePlacowki,
                ["Ustawienia.Uzytkownicy"] = tabUzytkownicy,
                ["Ustawienia.RoleUprawnienia"] = tabRoleUprawnienia,
            };

            foreach (var (key, tab) in tabKeyMap)
            {
                if (tab == null) continue;
                tab.Visibility = allowedKeys.Contains(key) ? Visibility.Visible : Visibility.Collapsed;
            }

            var currentlySelected = settingsTabControl.SelectedItem as TabItem;
            if (currentlySelected == null || currentlySelected.Visibility != Visibility.Visible)
            {
                var firstVisibleTab = settingsTabControl.Items
                    .OfType<TabItem>()
                    .FirstOrDefault(t => t.Visibility == Visibility.Visible);

                settingsTabControl.SelectedItem = firstVisibleTab;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("\u274C SettingsView: b\u0142\u0105d stosowania uprawnie\u0144 do zak\u0142adek: " + ex.Message);
        }
    }
}
