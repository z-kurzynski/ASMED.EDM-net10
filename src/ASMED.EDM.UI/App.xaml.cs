using ASMED.EDM.Data;
using ASMED.EDM.P1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;
using System.IO;
using System.Windows;
using ASMED.EDM.Core.Services;
using ASMED.EDM.UI.ViewModels.ustawienia;
using ASMED.EDM.UI.ViewModels.Cenniki;

namespace ASMED.EDM.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public IHost Host => _host;

    public T GetService<T>() where T : notnull
        => _host.Services.GetRequiredService<T>();

    public App()
    {
        //Register Syncfusion license 34.x.x
        SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JBaF5cXmRCd1p/TH5YfUNzdUVEY1ZUTXxaS1ZhSXxVd0VhW35ZcHNVRWJYWUF9XEc=");

        // Globalna obsługa nieobsłużonych wyjątków — zapobiega cicha crash 0xc000041d
        DispatcherUnhandledException += (s, e) =>
        {
            LogFatalError("DispatcherUnhandledException", e.Exception);
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                LogFatalError("AppDomain.UnhandledException", ex);
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogFatalError("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

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

        // Rejestracja integracji P1 / Middleware (test podstawowy — pobranie danych pacjenta)
        services.AddP1Integration(configuration);
        services.AddMiddlewareIntegration();

        // Rejestracja UI Services
        services.AddSingleton<Services.IDialogService, Services.DialogService>();
        services.AddSingleton<Services.INotificationService, Services.NotificationService>();
        services.AddSingleton<ASMED.EDM.Core.Interfaces.Services.ICurrentUserService, Services.CurrentUserService>();

        // Rejestracja logowania
        services.AddTransient<LoginWindow>();
        services.AddTransient<ViewModels.LoginViewModel>();

        // Rejestracja Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<Views.Patients.PatientsView>();
        services.AddTransient<Views.Settings.SettingsView>();
        services.AddTransient<Views.Settings.ConfigurationView>();
        services.AddTransient<Views.Settings.PriceListsView>();
        services.AddTransient<Views.Settings.FacilityDataView>();
        services.AddTransient<ViewModels.ustawienia.FacilityDataViewModel>();
        services.AddTransient<Views.Settings.UsersView>();
        services.AddTransient<ViewModels.Settings.UsersViewModel>();
        services.AddTransient<Views.Settings.RolesPermissionsView>();
        services.AddTransient<ViewModels.Settings.RolesPermissionsViewModel>();
        services.AddTransient<Views.Migration.MigrationView>();
        services.AddTransient<Views.cenniki.CennikiView>();
        services.AddSingleton<CennikiViewModel>();
        services.AddTransient<Views.Firma.FirmaView>();
        services.AddTransient<ViewModels.Firmy.FirmaViewModel>();
        services.AddTransient<Views.Firma.FirmaEditWindow>();
        services.AddTransient<ViewModels.Firmy.FirmaEditWindowViewModel>();
        services.AddTransient<Views.faktura.FakturaView>();
        services.AddTransient<ViewModels.faktura.FakturaViewModel>();
        services.AddTransient<Views.Skierowania.SkierListaPacjentowView>();
        services.AddTransient<Views.Skierowania.SkierPacjentaEditView>();
        services.AddTransient<ViewModels.Orzeczenie.HistoriaViewModel>();

        // Fakturowanie
        services.AddTransient<Views.Faktura.FakturaAddView>();
        services.AddTransient<ViewModels.Faktura.FakturaAddViewModel>();

        // Lista do faktur
        services.AddTransient<Views.lista_do_faktur.ListaFaktAddView>();
        services.AddTransient<ViewModels.ListaDoFaktur.ListaFaktAddViewModel>();

        // Rejestracja ViewModels
        services.AddSingleton<ViewModels.MainWindowViewModel>();
        services.AddSingleton<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.PatientsViewModel>();
        services.AddTransient<ViewModels.ConfigurationViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ViewModels.VisitsViewModel>();
        services.AddTransient<ViewModels.BadanieView.BadanieViewModel>();
        services.AddTransient<ViewModels.Orzeczenie.OrzeczenieViewModel>();
        services.AddTransient<ViewModels.MigrationViewModel>();
        services.AddTransient<ViewModels.Skierowania.SkierListaPacjentowViewModel>();
        services.AddTransient<ViewModels.Skierowania.SkierPacjentaEditViewModel>();
        services.AddTransient<ViewModels.Skierowania.SkierNewPacjentaViewModel>();
        services.AddTransient<ViewModels.Skierowania.SkierowaniaViewModel>();
        services.AddTransient<ViewModels.Skierowania.SkierPacjentaViewModel>();
        services.AddTransient<ViewModels.Raporty.WizytyViewModel>();
        }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Zapobiega przedwczesnemu zamknięciu aplikacji po zamknięciu okna logowania
        // (domyślnie WPF zamyka appkę gdy zamknie się "ostatnie" okno).
        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

        try
        {
            await _host.StartAsync();

            // Pokaż okno logowania przed głównym oknem aplikacji
            var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
            var loginResult = loginWindow.ShowDialog();

            if (loginResult != true)
            {
                Shutdown(0);
                return;
            }

            // Upewnij się, że schemat bazy danych (w tym AppTab/RolePermission) istnieje
            // na aktywnym połączeniu, zanim załadują się jakiekolwiek dane/uprawnienia.
            try
            {
                var connectionService = _host.Services.GetRequiredService<IDatabaseConnectionService>();
                var initLogger = _host.Services.GetRequiredService<ILogger<App>>();
                var activeConnectionString = await connectionService.GetActiveConnectionStringAsync();
                var initResult = await ASMED.EDM.Data.Helpers.DatabaseInitializerMySQL.RunAsync(activeConnectionString);

                if (initResult.HasErrors)
                {
                    initLogger.LogWarning(
                        "⚠️ Inicjalizacja schematu bazy danych zawiera błędy: {Errors}",
                        string.Join(", ", initResult.Errors));
                }
                else
                {
                    initLogger.LogInformation(
                        "✅ Schemat bazy danych zweryfikowany. Utworzono: {Created}, już istniały: {Existed}",
                        initResult.Created.Count,
                        initResult.AlreadyExisted.Count);
                }
            }
            catch (Exception ex)
            {
                var initLogger = _host.Services.GetRequiredService<ILogger<App>>();
                initLogger.LogWarning(ex, "⚠️ Nie udało się zweryfikować/utworzyć schematu bazy danych przy starcie");
            }

            // Pokazanie głównego okna (tryb offline-first dla development)
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
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

    private static void LogFatalError(string source, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ASMED.EDM.UI",
                "fatal_error.log");

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n" +
                $"Wiadomość: {ex.Message}\n" +
                $"StackTrace:\n{ex.StackTrace}\n" +
                new string('-', 80) + "\n");
        }
        catch { /* ignoruj błędy logowania */ }
    }
}

