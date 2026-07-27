using ASMED.EDM.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;
using System.IO;
using System.Windows;
using ASMED.EDM.Core.Services;
using ASMED.EDM.UI.ViewModels.ustawienia;

namespace ASMED.EDM.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public IHost Host => _host;

    public App()
    {
        //Register Syncfusion license 34.x.x
        SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JAaF5cX2pCd1p/TH5YfUNzdUVEY1ZUTXxaS1ZhSXxVdkJhWH5bdXBRRGBUU0J9XEY=");

        // Ustawienie polskiej kultury
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("pl-PL");
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new System.Globalization.CultureInfo("pl-PL");
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("pl-PL");
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("pl-PL");

        // Konfiguracja Generic Host
        _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Użyj AppContext.BaseDirectory zamiast GetCurrentDirectory() 
                // - działa poprawnie zarówno w debug jak i w ClickOnce
                var basePath = AppContext.BaseDirectory;
                config.SetBasePath(basePath);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context.Configuration, services);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();
    }

    private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        // Rejestracja Data Layer (DbContext + Connection Management + Repositories + Services)
        services.AddAsmedDatabase(configuration);

        // Rejestracja UI Services
        services.AddSingleton<Services.IDialogService, Services.DialogService>();

        // Rejestracja Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<Views.Patients.PatientsView>();
        services.AddTransient<Views.Settings.SettingsView>();
        services.AddTransient<Views.Settings.ConfigurationView>();
        services.AddTransient<Views.Settings.PriceListsView>();
        services.AddTransient<Views.Settings.FacilityDataView>();
        services.AddTransient<Views.Settings.UsersView>();
        services.AddTransient<Views.Visits.VisitsView>();
        services.AddTransient<Views.Migration.MigrationView>();

        // Rejestracja ViewModels
        services.AddSingleton<ViewModels.MainWindowViewModel>();
        services.AddSingleton<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.PatientsViewModel>();
        services.AddTransient<ViewModels.ConfigurationViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ViewModels.VisitsViewModel>();
        services.AddTransient<ViewModels.MigrationViewModel>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            await _host.StartAsync();

            // Pokazanie głównego okna (tryb offline-first dla development)
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Test połączenia z bazą danych W TLE (bez blokowania UI thread)
            _ = Task.Run(async () =>
            {
                var connectionService = _host.Services.GetRequiredService<IDatabaseConnectionService>();
                var logger = _host.Services.GetRequiredService<ILogger<App>>();

                try
                {
                    var connectionString = await connectionService.GetActiveConnectionStringAsync();
                    logger.LogInformation(
                        "✅ Połączono z bazą danych. Typ połączenia: {ConnectionType}",
                        connectionService.CurrentConnectionType);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "⚠️ Brak połączenia z bazą danych - tryb offline");
                }
            });
        }
        catch (Exception ex)
        {
            // Zapisz błąd do pliku i pokaż użytkownikowi
            var errorLogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ASMED.EDM.UI",
                "startup_error.log");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(errorLogPath)!);
                File.WriteAllText(errorLogPath, 
                    $"ASMED EDM UI - Błąd startowy ({DateTime.Now:yyyy-MM-dd HH:mm:ss})\n\n" +
                    $"Wiadomość: {ex.Message}\n\n" +
                    $"StackTrace:\n{ex.StackTrace}\n\n" +
                    $"BaseDirectory: {AppContext.BaseDirectory}\n" +
                    $"CurrentDirectory: {Directory.GetCurrentDirectory()}\n");
            }
            catch { /* ignore log errors */ }

            MessageBox.Show(
                $"Nie udało się uruchomić aplikacji.\n\n" +
                $"Błąd: {ex.Message}\n\n" +
                $"Szczegóły zapisano w:\n{errorLogPath}",
                "ASMED EDM UI - Błąd uruchomienia",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }

        base.OnExit(e);
    }
}

