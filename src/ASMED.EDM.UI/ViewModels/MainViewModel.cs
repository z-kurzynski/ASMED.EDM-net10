using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.Core.Services;
using ASMED.EDM.UI.ViewModels.ustawienia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// Główny ViewModel aplikacji
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IUserService _userService;
    private readonly IDatabaseConnectionService _connectionService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    [ObservableProperty]
    private object? _pacjentWidok;

    [ObservableProperty]
    private object? _ustawieniaWidok;

    [ObservableProperty]
    private object? _wizytyWidok;

    [ObservableProperty]
    private string _databaseInfo = "Łączenie z bazą danych...";

    public MainViewModel(
        IUserService userService,
        IDatabaseConnectionService connectionService,
        PatientsViewModel patientsViewModel,
        SettingsViewModel settingsViewModel,
        VisitsViewModel visitsViewModel,
        ILogger<MainViewModel> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ✅ Weryfikacja null PRZED przypisaniem (fail-fast)
        ArgumentNullException.ThrowIfNull(patientsViewModel);
        ArgumentNullException.ThrowIfNull(settingsViewModel);
        ArgumentNullException.ThrowIfNull(visitsViewModel);

        // Ustaw widok Pacjentów jako domyślny
        PacjentWidok = patientsViewModel;
        UstawieniaWidok = settingsViewModel;
        WizytyWidok = visitsViewModel;
        CurrentViewModel = patientsViewModel;

        // Inicjalizacja menu
        InitializeMenuItems();

        // Pobierz info o połączeniu do bazy (fire-and-forget is OK here - UI pokazuje placeholder)
        _ = InitializeDatabaseInfoAsync();
    }

    /// <summary>
    /// Elementy menu (do późniejszej implementacji)
    /// </summary>
    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();

    private void InitializeMenuItems()
    {
        MenuItems.Add(new MenuItemViewModel
        {
            Title = "Dashboard",
            Icon = "🏠",
            Command = NavigateToDashboardCommand
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Title = "Pacjenci",
            Icon = "👥",
            Command = NavigateToPatientsCommand
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Title = "Wizyty",
            Icon = "📅",
            Command = NavigateToVisitsCommand
        });

        MenuItems.Add(new MenuItemViewModel
        {
            Title = "Lekarze",
            Icon = "👨‍⚕️",
            Command = NavigateToDoctorsCommand
        });
    }

    private async Task InitializeDatabaseInfoAsync()
    {
        try
        {
            var connectionString = await _connectionService.GetActiveConnectionStringAsync();

            if (!string.IsNullOrEmpty(connectionString))
            {
                // Wyodrębnij database name z connection string
                var dbName = ExtractDatabaseName(connectionString);
                DatabaseInfo = $"✅ Połączono: {dbName} ({_connectionService.CurrentConnectionType})";
                _logger.LogInformation("Successfully connected to database: {DatabaseName}", dbName);
            }
            else
            {
                DatabaseInfo = "⚠️ Brak połączenia - skonfiguruj w Ustawieniach";
                _logger.LogWarning("Failed to establish database connection");
            }
        }
        catch (Exception ex)
        {
            DatabaseInfo = "⚠️ Brak połączenia - skonfiguruj w Ustawieniach";
            _logger.LogError(ex, "Error initializing database connection info");
        }
    }

    /// <summary>
    /// Publiczna metoda do odświeżenia statusu połączenia (np. po zmianie konfiguracji)
    /// </summary>
    public async Task RefreshDatabaseConnectionAsync()
    {
        DatabaseInfo = "🔄 Sprawdzanie połączenia...";
        await InitializeDatabaseInfoAsync();
    }

    private static string ExtractDatabaseName(string connectionString)
    {
        var parts = connectionString.Split(';');
        var dbPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(dbPart))
        {
            return dbPart.Split('=')[1].Trim();
        }
        return "Unknown DB";
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        _logger.LogInformation("Nawigacja do Dashboard");
        // TODO: Przełącz na DashboardViewModel
    }

    [RelayCommand]
    private void NavigateToPatients()
    {
        _logger.LogInformation("Nawigacja do Pacjentów");
        // TODO: Przełącz na PatientsViewModel
    }

    [RelayCommand]
    private void NavigateToVisits()
    {
        _logger.LogInformation("Nawigacja do Wizyt");
        // TODO: Przełącz na VisitsViewModel
    }

    [RelayCommand]
    private void NavigateToDoctors()
    {
        _logger.LogInformation("Nawigacja do Lekarzy");
        // TODO: Przełącz na DoctorsViewModel
    }
}

/// <summary>
/// ViewModel reprezentujący element menu
/// </summary>
public class MenuItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public IRelayCommand? Command { get; set; }
}
