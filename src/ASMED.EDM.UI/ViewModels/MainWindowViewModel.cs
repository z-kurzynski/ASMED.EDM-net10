using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// ViewModel dla głównego okna aplikacji
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DispatcherTimer _clockTimer;

    [ObservableProperty]
    private string _databaseName = "Nieznana";

    [ObservableProperty]
    private Brush _databaseStatusColor = Brushes.Gray;

    [ObservableProperty]
    private string _databaseType = "Nieznana";

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    private string _currentDate = DateTime.Now.ToString("dd.MM.yyyy");

    public MainWindowViewModel()
    {
        LoadDatabaseInfo();

        // Inicjalizacja timera dla zegara
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += ClockTimer_Tick;
        _clockTimer.Start();
    }

    /// <summary>
    /// Aktualizuje czas wyświetlany w zegarze
    /// </summary>
    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        CurrentTime = now.ToString("HH:mm:ss");
        CurrentDate = now.ToString("dd.MM.yyyy");
    }

    /// <summary>
    /// Ładuje informacje o bazie danych z konfiguracji
    /// </summary>
    private void LoadDatabaseInfo()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var primaryConnection = configuration["DatabaseSettings:PrimaryConnection"];
            var backupConnection = configuration["DatabaseSettings:BackupConnection"];
            var localConnection = configuration["DatabaseSettings:LocalConnection"];

            // Na razie zakładamy, że używamy Primary - w przyszłości można dodać logikę wyboru
            // lub wykrywania aktualnie używanej bazy
            var currentConnection = primaryConnection;

            if (!string.IsNullOrEmpty(currentConnection))
            {
                DetermineDatabaseType(currentConnection, primaryConnection, backupConnection, localConnection);
            }
        }
        catch
        {
            DatabaseName = "Błąd wczytywania konfiguracji";
            DatabaseType = "Nieznana";
            DatabaseStatusColor = Brushes.Gray;
        }
    }

    /// <summary>
    /// Określa typ bazy i ustawia odpowiednie właściwości
    /// </summary>
    private void DetermineDatabaseType(string currentConnection, string? primaryConnection, 
        string? backupConnection, string? localConnection)
    {
        if (currentConnection == primaryConnection)
        {
            DatabaseType = "Produkcyjna";
            DatabaseStatusColor = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Czerwony
            DatabaseName = ExtractDatabaseName(currentConnection);
        }
        else if (currentConnection == backupConnection)
        {
            DatabaseType = "Backup";
            DatabaseStatusColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Pomarańczowy
            DatabaseName = ExtractDatabaseName(currentConnection);
        }
        else if (currentConnection == localConnection)
        {
            DatabaseType = "Lokalna";
            DatabaseStatusColor = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Zielony
            DatabaseName = ExtractDatabaseName(currentConnection);
        }
        else
        {
            DatabaseType = "Nieznana";
            DatabaseStatusColor = Brushes.Gray;
            DatabaseName = "Nieznana baza";
        }
    }

    /// <summary>
    /// Wyodrębnia nazwę bazy danych z connection stringa
    /// </summary>
    private string ExtractDatabaseName(string connectionString)
    {
        try
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Split('=')[1].Trim();
                }
            }
            return "Nieznana";
        }
        catch
        {
            return "Nieznana";
        }
    }
}
