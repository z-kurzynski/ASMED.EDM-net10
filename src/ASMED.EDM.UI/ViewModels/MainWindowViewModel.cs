using ASMED.EDM.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using System.Windows.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// ViewModel dla głównego okna aplikacji
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DbConnectionFactory _dbFactory;

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

    private string _consoleWrite;
    private int _consoleProgress;
    private CancellationTokenSource? _consoleProgressCts;


    public MainWindowViewModel(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;

        // Subskrypcja na zdarzenie zmiany połączenia (failover lub ręczne przełączenie)
        _dbFactory.ConnectionTypeChanged += OnConnectionTypeChanged;

        RefreshDatabaseStatus();

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += ClockTimer_Tick;
        _clockTimer.Start();
    }

    /// <summary>
    /// Wywoływane automatycznie gdy DatabaseConnectionService wykryje failover
    /// lub użytkownik ręcznie zmieni połączenie w ConfigurationView.
    /// </summary>
    private void OnConnectionTypeChanged(object? sender, EventArgs e)
    {
        // Zdarzenie może przyjść z wątku tła — przełączamy na UI thread
        System.Windows.Application.Current?.Dispatcher.Invoke(RefreshDatabaseStatus);
    }

    /// <summary>
    /// Odświeża dane o aktywnej bazie w stopce na podstawie rejestru.
    /// </summary>
    public void RefreshDatabaseStatus()
    {
        try
        {
            var activeType = _dbFactory.ActiveConnectionType; // "Primary" | "Backup" | "Local"
            var activeCs   = _dbFactory.ActiveConnectionString;

            DatabaseName = ExtractDatabaseName(activeCs);

            switch (activeType)
            {
                case "Primary":
                    DatabaseType        = "🟢 Chmura (Primary)";
                    DatabaseStatusColor = new SolidColorBrush(Color.FromRgb(21, 128, 61));   // zielony
                    break;
                case "Backup":
                    DatabaseType        = "🟠 Backup";
                    DatabaseStatusColor = new SolidColorBrush(Color.FromRgb(194, 65, 12));   // pomarańczowy
                    break;
                case "Local":
                    DatabaseType        = "🔵 Lokalny (offline)";
                    DatabaseStatusColor = new SolidColorBrush(Color.FromRgb(29, 78, 216));   // niebieski
                    break;
                default:
                    DatabaseType        = "❓ Nieznana";
                    DatabaseStatusColor = Brushes.Gray;
                    break;
            }
        }
        catch
        {
            DatabaseName        = "Błąd odczytu";
            DatabaseType        = "❓ Nieznana";
            DatabaseStatusColor = Brushes.Gray;
        }
    }

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        CurrentTime = now.ToString("HH:mm:ss");
        CurrentDate = now.ToString("dd.MM.yyyy");
    }

    private string ExtractDatabaseName(string connectionString)
    {
        try
        {
            foreach (var part in connectionString.Split(';'))
            {
                if (part.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                    return part.Split('=')[1].Trim();
            }
            return "Nieznana";
        }
        catch
        {
            return "Nieznana";
        }
    }

    /// <summary>
    /// Aktualizuje tekst wyświetlany w konsoli i stopce aplikacji.
    /// </summary>
    
    public string ConsoleWrite
    {
        get => _consoleWrite;
        set
        {
            _consoleWrite = value;
            OnPropertyChanged(nameof(ConsoleWrite));
        }
    }

    /// <summary>
    /// Wartość paska postępu wyświetlana w stopce (0-100).
    /// </summary>
    public int ConsoleProgress
    {
        get => _consoleProgress;
        set
        {
            if (_consoleProgress == value) return;
            _consoleProgress = value;
            OnPropertyChanged(nameof(ConsoleProgress));
        }
    }

    /// <summary>
    /// Uruchamia pojedynczy cykl paska postępu 0..100. Każde wywołanie anuluje
    /// poprzedni cykl i zaczyna nowy.
    /// </summary>
    public void StartConsoleProgressCycle()
    {
        // Anuluj poprzedni cykl, jeśli istnieje
        try { _consoleProgressCts?.Cancel(); } catch { }
        _consoleProgressCts = new CancellationTokenSource();
        var token = _consoleProgressCts.Token;

        // Uruchom asynchroniczny cykl; używamy dispatcher'a do aktualizacji właściwości
        _ = Task.Run(async () =>
        {
            try
            {
                // Resetuj do 0 przed startem
                Application.Current?.Dispatcher.Invoke(() => ConsoleProgress = 0);
                for (int i = 0; i <= 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    // Aktualizacja na wątku UI
                    Application.Current?.Dispatcher.Invoke(() => ConsoleProgress = i);
                    await Task.Delay(6, token); // ~600ms cały cykl (można dopasować)
                }
            }
            catch (OperationCanceledException)
            {
                // Jeśli anulowano, ustaw 0
                Application.Current?.Dispatcher.Invoke(() => ConsoleProgress = 0);
            }
            catch
            {
                Application.Current?.Dispatcher.Invoke(() => ConsoleProgress = 0);
            }
            finally
            {
                // Krótkie domknięcie stanu
                Application.Current?.Dispatcher.Invoke(() => ConsoleProgress = 0);
            }
        }, token);
    }
    public void StatusWrite(string msg)
    {
        Console.WriteLine(msg);      // nadal działa dla debugera
        ConsoleWrite = msg;          // aktualizacja stopki
    }

}
