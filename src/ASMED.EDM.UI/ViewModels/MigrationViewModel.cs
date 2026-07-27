using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using ASMED.EDM.Data.Models;
using ASMED.EDM.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// ViewModel dla zakładki Migracja w sekcji Baza Danych.
/// Obsługuje migrację danych z bazy Access do docelowej bazy MySQL.
/// </summary>
public partial class MigrationViewModel : ViewModelBase
{
    private readonly IMigrationService _migrationService;
    private readonly ILogger<MigrationViewModel> _logger;
    private readonly DbConnectionFactory _dbFactory;

    private CancellationTokenSource? _cts;

    // ── Właściwości bindowane ──────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartMigrationCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerifyCommand))]
    private string _accessDatabasePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartMigrationCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerifyCommand))]
    private DatabaseConnectionItem? _selectedMySqlDatabase;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private double _tableProgress;

    [ObservableProperty]
    private string _statusMessage = "Wybierz plik Access i docelową bazę MySQL, następnie zaznacz tabele do migracji.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartMigrationCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelMigrationCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerifyCommand))]
    private bool _isMigrating;

    [ObservableProperty]
    private bool _backupCreated;

    [ObservableProperty]
    private string _backupPath = string.Empty;

    // ── właściwości dla przywracania/kopiowania ──────────────────────────────

    [ObservableProperty]
    private string _restoreFilePath = string.Empty;

    [ObservableProperty]
    private DatabaseConnectionItem? _copySourceDatabase;

    [ObservableProperty]
    private DatabaseConnectionItem? _copyTargetDatabase;

    // ── Kolekcje ───────────────────────────────────────────────────────────

    public ObservableCollection<DatabaseConnectionItem>    MySqlDatabases      { get; } = [];
    public ObservableCollection<TableGroupViewModel>       TableGroups         { get; } = [];
    public ObservableCollection<string>                    MigrationLog        { get; } = [];
    public ObservableCollection<TableVerificationResult>   VerificationResults { get; } = [];

    // ── Właściwości obliczane ──────────────────────────────────────────────

    public bool CanStartMigration =>
        !IsMigrating &&
        !string.IsNullOrWhiteSpace(AccessDatabasePath) &&
        File.Exists(AccessDatabasePath) &&
        SelectedMySqlDatabase != null &&
        TableGroups.SelectMany(g => g.Tables).Any(t => t.IsSelected);

    public bool CanCancel => IsMigrating;

    public bool CanVerify =>
        !IsMigrating &&
        !string.IsNullOrWhiteSpace(AccessDatabasePath) &&
        File.Exists(AccessDatabasePath) &&
        SelectedMySqlDatabase != null;

    public bool CanRestoreBackup =>
        !IsMigrating &&
        !string.IsNullOrWhiteSpace(RestoreFilePath) &&
        File.Exists(RestoreFilePath) &&
        SelectedMySqlDatabase != null;

    public bool CanCopyDatabase =>
        !IsMigrating &&
        CopySourceDatabase != null &&
        CopyTargetDatabase != null &&
        CopySourceDatabase != CopyTargetDatabase;

    // ── Konstruktor ────────────────────────────────────────────────────────

    public MigrationViewModel(
        IMigrationService migrationService,
        DbConnectionFactory dbFactory,
        ILogger<MigrationViewModel> logger)
    {
        _migrationService = migrationService;
        _dbFactory = dbFactory;
        _logger = logger;

        LoadMySqlDatabases();
        LoadTableGroups();
    }

    // ── Partial property-change hooks ────────────────────────────────────────

    partial void OnRestoreFilePathChanged(string value)
    {
        RestoreBackupCommand.NotifyCanExecuteChanged();
    }

    partial void OnCopySourceDatabaseChanged(DatabaseConnectionItem? value)
    {
        CopyDatabaseCommand.NotifyCanExecuteChanged();
    }

    partial void OnCopyTargetDatabaseChanged(DatabaseConnectionItem? value)
    {
        CopyDatabaseCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsMigratingChanged(bool value)
    {
        RestoreBackupCommand.NotifyCanExecuteChanged();
        CopyDatabaseCommand.NotifyCanExecuteChanged();
    }

    // ── Inicjalizacja ──────────────────────────────────────────────────────

    private void LoadMySqlDatabases()
    {
        MySqlDatabases.Clear();

        // DbConnectionFactory czyta z rejestru Windows (Registry → appsettings.json fallback)
        var primary = _dbFactory.PrimaryConnectionString;
        var backup  = _dbFactory.BackupConnectionString;
        var local   = _dbFactory.LocalConnectionString;

        // Domyślny placeholder — nie dodajemy pustych/niezdefiniowanych połączeń
        const string emptyPlaceholder = "Server=localhost;Database=asmed_edm;User=root;Password=;CharSet=utf8mb4;";

        if (!string.IsNullOrWhiteSpace(primary) && primary != emptyPlaceholder)
            MySqlDatabases.Add(new DatabaseConnectionItem("Główna (Primary)", primary));

        if (!string.IsNullOrWhiteSpace(backup) && backup != emptyPlaceholder)
            MySqlDatabases.Add(new DatabaseConnectionItem("Zapasowa (Backup)", backup));

        if (!string.IsNullOrWhiteSpace(local) && local != emptyPlaceholder)
            MySqlDatabases.Add(new DatabaseConnectionItem("Lokalna (Local)", local));

        SelectedMySqlDatabase = MySqlDatabases.FirstOrDefault();
    }

    private void LoadTableGroups()
    {
        TableGroups.Clear();

        var allTables = _migrationService.GetAvailableTables();

        var groups = allTables
            .GroupBy(t => t.Category)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var groupVm = new TableGroupViewModel(group.Key, group.ToList());
            groupVm.SelectionChanged += (_, _) => StartMigrationCommand.NotifyCanExecuteChanged();
            TableGroups.Add(groupVm);
        }
    }

    // ── Komendy ────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task BrowseAccessAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Wybierz plik bazy danych Access",
            Filter = "Baza Access (*.accdb;*.mdb)|*.accdb;*.mdb|Wszystkie pliki (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog() != true) return;

        AccessDatabasePath = dlg.FileName;
        AddLog($"Wybrano plik: {AccessDatabasePath}");

        // Skanuj tabele w Access i porównaj z listą MySQL
        await ScanAndMatchAccessTablesAsync();
    }

    private async Task ScanAndMatchAccessTablesAsync()
    {
        if (string.IsNullOrWhiteSpace(AccessDatabasePath)) return;

        StatusMessage = "Skanowanie tabel w bazie Access...";
        var (success, accessTables, message) = await _migrationService.ScanAccessTablesAsync(AccessDatabasePath);
        AddLog($"[Skan Access] {message}");

        if (!success) { StatusMessage = message; return; }

        var expectedNames = TableGroups
            .SelectMany(g => g.Tables)
            .Select(t => t.Name)
            .ToList();

        var missing  = expectedNames.Where(n => !accessTables.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();
        var extra    = accessTables.Where(n => !expectedNames.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();
        var matched  = expectedNames.Count - missing.Count;

        AddLog($"   ✅ Dopasowanych tabel: {matched}/{expectedNames.Count}");

        if (missing.Count > 0)
        {
            AddLog($"   ⚠️ Brak w Access ({missing.Count} szt.) — zostaną pominięte podczas migracji:");
            foreach (var t in missing)
                AddLog($"      • {t}");
        }

        if (extra.Count > 0)
        {
            AddLog($"   ℹ️ Dodatkowe tabele w Access (nieuwzględnione w migracji: {extra.Count} szt.):");
            foreach (var t in extra)
                AddLog($"      • {t}");
        }

        StatusMessage = missing.Count == 0
            ? $"✅ Wszystkie {matched} tabele znalezione w bazie Access."
            : $"⚠️ Znaleziono {matched}/{expectedNames.Count} tabel — {missing.Count} brakuje w Access.";
    }

    [RelayCommand]
    private async Task TestAccessAsync()
    {
        if (string.IsNullOrWhiteSpace(AccessDatabasePath))
        {
            MessageBox.Show("Wybierz plik bazy danych Access.", "Brak pliku", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StatusMessage = "Testowanie połączenia z Access...";
        var (success, message) = await _migrationService.TestAccessConnectionAsync(AccessDatabasePath);
        AddLog($"[Access] {message}");
        StatusMessage = message;

        MessageBox.Show(message,
            success ? "Połączenie Access — OK" : "Błąd połączenia Access",
            MessageBoxButton.OK,
            success ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    [RelayCommand]
    private async Task TestMySqlAsync()
    {
        if (SelectedMySqlDatabase == null)
        {
            MessageBox.Show("Wybierz docelową bazę MySQL.", "Brak bazy", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StatusMessage = "Testowanie połączenia z MySQL...";
        var (success, message) = await _migrationService.TestMySqlConnectionAsync(SelectedMySqlDatabase.ConnectionString);
        AddLog($"[MySQL] {message}");
        StatusMessage = message;

        MessageBox.Show(message,
            success ? "Połączenie MySQL — OK" : "Błąd połączenia MySQL",
            MessageBoxButton.OK,
            success ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (SelectedMySqlDatabase == null)
        {
            MessageBox.Show("Wybierz docelową bazę MySQL.", "Brak bazy", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedTables = GetSelectedTableNames();
        if (selectedTables.Count == 0)
        {
            MessageBox.Show("Zaznacz przynajmniej jedną tabelę.", "Brak tabel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        BusyMessage = "Tworzenie kopii zapasowej...";
        StatusMessage = "Tworzenie kopii zapasowej MySQL...";
        AddLog("Rozpoczynam tworzenie kopii zapasowej...");

        try
        {
            var (success, path, message) = await _migrationService.CreateBackupAsync(
                SelectedMySqlDatabase.ConnectionString, selectedTables);

            AddLog($"[KOPIA] {message}");
            StatusMessage = message;

            if (success)
            {
                BackupCreated = true;
                BackupPath = path;
                MessageBox.Show($"Kopia zapasowa zapisana:\n{path}", "Kopia zapasowa — OK",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(message, "Błąd kopii zapasowej", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartMigration))]
    private async Task StartMigrationAsync()
    {
        var selectedTables = GetSelectedTableNames();

        // Potwierdzenie operacji
        var tableList = string.Join("\n  • ", selectedTables);
        var result = MessageBox.Show(
            $"Migracja NADPISZE dane w {selectedTables.Count} tabelach docelowej bazy MySQL:\n\n  • {tableList}\n\n" +
            $"Baza docelowa: {SelectedMySqlDatabase!.DisplayName}\n\n" +
            (BackupCreated ? $"✅ Kopia zapasowa: {BackupPath}\n\n" : "⚠️ BRAK kopii zapasowej!\n\n") +
            "Kontynuować?",
            "Potwierdzenie migracji",
            MessageBoxButton.YesNo,
            BackupCreated ? MessageBoxImage.Question : MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        _cts = new CancellationTokenSource();
        IsMigrating = true;
        OverallProgress = 0;
        TableProgress = 0;
        MigrationLog.Clear();
        StatusMessage = "Migracja w toku...";
        AddLog($"▶ Rozpoczynam migrację {selectedTables.Count} tabel do: {SelectedMySqlDatabase.DisplayName}");
        AddLog($"   Plik Access: {AccessDatabasePath}");
        AddLog(new string('─', 60));

        OnPropertyChanged(nameof(CanStartMigration));
        OnPropertyChanged(nameof(CanCancel));

        try
        {
            var progress = new Progress<MigrationProgress>(p =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    OverallProgress = p.OverallProgress;
                    TableProgress = p.TableProgress;
                    StatusMessage = p.Message;

                    if (!string.IsNullOrEmpty(p.Message))
                        AddLog(p.HasError ? $"❌ {p.Message}" : $"   {p.Message}");

                    if (p.IsCompleted)
                        AddLog(new string('─', 60));
                });
            });

            await _migrationService.MigrateTablesAsync(
                AccessDatabasePath,
                SelectedMySqlDatabase.ConnectionString,
                selectedTables,
                progress,
                _cts.Token);

            StatusMessage = $"✅ Migracja zakończona pomyślnie. Przetworzone tabele: {selectedTables.Count}";
            AddLog($"✅ Migracja zakończona.");
            MessageBox.Show(StatusMessage, "Migracja — sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "⚠️ Migracja anulowana przez użytkownika.";
            AddLog("⚠️ Anulowano.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Błąd migracji: {ex.Message}";
            AddLog($"❌ BŁĄD: {ex.Message}");
            _logger.LogError(ex, "Błąd podczas migracji");
            MessageBox.Show($"Błąd migracji:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsMigrating = false;
            OverallProgress = IsMigrating ? OverallProgress : 100;
            _cts?.Dispose();
            _cts = null;
            OnPropertyChanged(nameof(CanStartMigration));
            OnPropertyChanged(nameof(CanCancel));
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelMigration()
    {
        _cts?.Cancel();
        StatusMessage = "Anulowanie migracji...";
        AddLog("⚠️ Żądanie anulowania...");
    }

    [RelayCommand(CanExecute = nameof(CanVerify))]
    private async Task VerifyAsync()
    {
        if (SelectedMySqlDatabase is null) return;

        VerificationResults.Clear();
        StatusMessage = "Weryfikacja liczby rekordów...";
        AddLog("🔍 Rozpoczynam weryfikację zgodności rekordów Access ↔ MySQL...");

        var tableNames = GetSelectedTableNames();
        if (tableNames.Count == 0)
        {
            // brak zaznaczonych — weryfikuj wszystkie dostępne
            tableNames = _migrationService.GetAvailableTables()
                                          .Select(t => t.Name)
                                          .ToList();
        }

        try
        {
            var results = await _migrationService.VerifyRowCountsAsync(
                AccessDatabasePath,
                SelectedMySqlDatabase.ConnectionString,
                tableNames);

            int ok      = 0;
            int mismatch = 0;

            foreach (var r in results)
            {
                VerificationResults.Add(r);
                if (r.IsMatch)
                {
                    ok++;
                    AddLog($"✅ {r.TableName}: Access={r.AccessCount}, MySQL={r.MySqlCount}");
                }
                else
                {
                    mismatch++;
                    AddLog($"⚠️ {r.TableName}: Access={r.AccessCount}, MySQL={r.MySqlCount} (różnica: {r.DiffText})");
                }
            }

            StatusMessage = mismatch == 0
                ? $"✅ Weryfikacja zakończona — wszystkie {ok} tabele zgodne."
                : $"⚠️ Weryfikacja: {ok} zgodnych, {mismatch} rozbieżnych — sprawdź log.";

            AddLog($"🏁 Wynik: {ok} ✅  |  {mismatch} ⚠️");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd weryfikacji: {ex.Message}";
            AddLog($"❌ Błąd: {ex.Message}");
            _logger.LogError(ex, "Błąd weryfikacji rekordów");
        }
    }

    // ── BrowseRestoreFile ────────────────────────────────────────────────────

    [RelayCommand]
    private void BrowseRestoreFile()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Wybierz plik kopii zapasowej SQL",
            Filter = "Pliki SQL (*.sql)|*.sql|Wszystkie pliki (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
            RestoreFilePath = dlg.FileName;
    }

    // ── RestoreBackupAsync ───────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanRestoreBackup))]
    private async Task RestoreBackupAsync()
    {
        if (SelectedMySqlDatabase is null) return;

        var confirm = System.Windows.MessageBox.Show(
            $"Przywrócenie kopii NADPISZE wszystkie dane w bazie:\n{SelectedMySqlDatabase.DisplayName}\n\nKontynuować?",
            "Potwierdzenie przywracania",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsMigrating = true;
        StatusMessage = "Przywracanie kopii zapasowej...";
        AddLog($"♻️ Przywracanie kopii z pliku: {RestoreFilePath}");
        AddLog($"📋 Docelowa baza: {SelectedMySqlDatabase.DisplayName}");

        _cts = new CancellationTokenSource();

        try
        {
            var progressHandler = new Progress<string>(msg =>
            {
                StatusMessage = msg;
                AddLog($"   {msg}");
            });

            var (success, executed, message) = await _migrationService.RestoreBackupAsync(
                RestoreFilePath,
                SelectedMySqlDatabase.ConnectionString,
                progressHandler,
                _cts.Token);

            StatusMessage = message;
            AddLog(success ? $"✅ {message}" : $"❌ {message}");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Przywracanie anulowane.";
            AddLog("⚠️ Przywracanie anulowane przez użytkownika.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            AddLog($"❌ Błąd przywracania: {ex.Message}");
            _logger.LogError(ex, "Błąd przywracania kopii");
        }
        finally
        {
            IsMigrating = false;
        }
    }

    // ── CopyDatabaseAsync ────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanCopyDatabase))]
    private async Task CopyDatabaseAsync()
    {
        if (CopySourceDatabase is null || CopyTargetDatabase is null) return;

        var confirm = System.Windows.MessageBox.Show(
            $"Skopiowanie bazy NADPISZE wszystkie dane w:\n{CopyTargetDatabase.DisplayName}\n\nŹródło: {CopySourceDatabase.DisplayName}\nCel: {CopyTargetDatabase.DisplayName}\n\nKontynuować?",
            "Potwierdzenie kopiowania bazy",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsMigrating = true;
        StatusMessage = $"Kopiowanie: {CopySourceDatabase.DisplayName} → {CopyTargetDatabase.DisplayName}...";
        AddLog($"📋 Kopiowanie bazy: {CopySourceDatabase.DisplayName} → {CopyTargetDatabase.DisplayName}");

        _cts = new CancellationTokenSource();

        try
        {
            var progressHandler = new Progress<MigrationProgress>(p =>
            {
                StatusMessage = p.Message;
                OverallProgress = p.OverallProgress;
            });

            var tableNames = TableGroups.SelectMany(g => g.Tables)
                                        .Where(t => t.IsSelected)
                                        .Select(t => t.Name)
                                        .ToList();

            var (success, tablesCopied, message) = await _migrationService.CopyMySqlDatabaseAsync(
                CopySourceDatabase.ConnectionString,
                CopyTargetDatabase.ConnectionString,
                tableNames.Count > 0 ? tableNames : null,
                progressHandler,
                _cts.Token);

            StatusMessage = message;
            AddLog(success ? $"✅ {message}" : $"❌ {message}");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Kopiowanie anulowane.";
            AddLog("⚠️ Kopiowanie anulowane przez użytkownika.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
            AddLog($"❌ Błąd kopiowania: {ex.Message}");
            _logger.LogError(ex, "Błąd kopiowania bazy MySQL");
        }
        finally
        {
            IsMigrating = false;
            OverallProgress = 0;
        }
    }

    [RelayCommand]
    private void SelectAllTables()
    {
        foreach (var group in TableGroups)
            group.SelectAll();
        OnPropertyChanged(nameof(CanStartMigration));
    }

    [RelayCommand]
    private void DeselectAllTables()
    {
        foreach (var group in TableGroups)
            group.DeselectAll();
        OnPropertyChanged(nameof(CanStartMigration));
    }

    // ── Metody pomocnicze ──────────────────────────────────────────────────

    private List<string> GetSelectedTableNames() =>
        TableGroups
            .SelectMany(g => g.Tables)
            .Where(t => t.IsSelected)
            .Select(t => t.Name)
            .ToList();

    private void AddLog(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        MigrationLog.Add(entry);
        _logger.LogInformation("{Entry}", entry);
    }
}

// ── Model połączenia MySQL ─────────────────────────────────────────────────

/// <summary>
/// Reprezentuje jedno z dostępnych połączeń MySQL do wyboru
/// </summary>
public class DatabaseConnectionItem(string displayName, string connectionString)
{
    public string DisplayName { get; } = displayName;
    public string ConnectionString { get; } = connectionString;
    public override string ToString() => DisplayName;
}

// ── ViewModel grupy tabel ──────────────────────────────────────────────────

/// <summary>
/// ViewModel grupy tabel (Główne / Słownikowe / Pomocnicze)
/// </summary>
public partial class TableGroupViewModel : ObservableObject
{
    public string GroupName { get; }
    public ObservableCollection<TableInfo> Tables { get; }

    public event EventHandler? SelectionChanged;

    private bool _isAllSelected;

    public bool IsAllSelected
    {
        get => _isAllSelected;
        set
        {
            if (SetProperty(ref _isAllSelected, value))
            {
                foreach (var t in Tables)
                    t.IsSelected = value;
            }
        }
    }

    public TableGroupViewModel(TableCategory category, IEnumerable<TableInfo> tables)
    {
        GroupName = category switch
        {
            TableCategory.Glowne => "🗂️ Tabele główne",
            TableCategory.Slownikowe => "📖 Tabele słownikowe",
            TableCategory.Pomocnicze => "🔧 Tabele pomocnicze",
            _ => category.ToString()
        };

        Tables = new ObservableCollection<TableInfo>(tables);

        foreach (var t in Tables)
        {
            t.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TableInfo.IsSelected))
                {
                    OnPropertyChanged(nameof(IsAllSelected));
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            };
        }
    }

    public void SelectAll() => IsAllSelected = true;
    public void DeselectAll() => IsAllSelected = false;
}
