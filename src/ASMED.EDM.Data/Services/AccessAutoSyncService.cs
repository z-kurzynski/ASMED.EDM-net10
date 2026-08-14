using ASMED.EDM.Data.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.Data.Services;

/// <summary>
/// Serwis auto-synchronizacji MySQL → Access.
/// Gdy włączony (IsEnabled = true), nasłuchuje na zgłoszenia zmian tabel
/// i eksportuje je do Access z debounce 1000ms.
/// Używa IServiceScopeFactory żeby unikać captive-dependency
/// (Singleton trzymający Scoped IAccessExportService).
/// </summary>
public sealed class AccessAutoSyncService : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccessAutoSyncService> _logger;

    // ── Stan ────────────────────────────────────────────────────────────────────
    private bool _isEnabled;
    private string _accessPath = string.Empty;
    private string _mysqlConnectionString = string.Empty;

    // ── Kolejka zmienionych tabel z debounce ─────────────────────────────────────
    private readonly HashSet<string> _pendingTables = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private CancellationTokenSource? _debounceCts;

    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(1000);

    // ── Zdarzenia ────────────────────────────────────────────────────────────────
    /// <summary>Wywoływane po zakończeniu auto-eksportu (sukces lub błąd)</summary>
    public event EventHandler<IReadOnlyList<ExportTableResult>>? ExportCompleted;

    /// <summary>Wywoływane gdy auto-eksport napotka błąd</summary>
    public event EventHandler<string>? ExportError;

    // ── Właściwości ──────────────────────────────────────────────────────────────

    /// <summary>Czy auto-sync jest aktywny</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>Ścieżka do pliku Access</summary>
    public string AccessPath
    {
        get => _accessPath;
        set => _accessPath = value;
    }

    /// <summary>Connection string do aktywnej bazy MySQL</summary>
    public string MySqlConnectionString
    {
        get => _mysqlConnectionString;
        set => _mysqlConnectionString = value;
    }

    public AccessAutoSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<AccessAutoSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    // ── NotifyTableChanged ───────────────────────────────────────────────────────

    /// <summary>
    /// Zgłasza, że tabela uległa zmianie.
    /// Wywoływane przez repository/serwisy domenowe po zapisie.
    /// Jeśli auto-sync jest włączony, planuje eksport z opóźnieniem (debounce).
    /// </summary>
    public void NotifyTableChanged(string tableName)
    {
        if (!_isEnabled) return;

        _lock.Wait();
        try
        {
            _pendingTables.Add(tableName);
        }
        finally
        {
            _lock.Release();
        }

        ScheduleDebounced();
    }

    /// <summary>
    /// Zgłasza zmianę wielu tabel jednocześnie
    /// </summary>
    public void NotifyTablesChanged(IEnumerable<string> tableNames)
    {
        if (!_isEnabled) return;

        _lock.Wait();
        try
        {
            foreach (var t in tableNames)
                _pendingTables.Add(t);
        }
        finally
        {
            _lock.Release();
        }

        ScheduleDebounced();
    }

    // ── Debounce ─────────────────────────────────────────────────────────────────

    private void ScheduleDebounced()
    {
        // Anuluj poprzednie zaplanowane wywołanie i ustaw nowe
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, token);
                await FlushPendingAsync(token);
            }
            catch (OperationCanceledException)
            {
                // Nowe zgłoszenie anulowało poprzednie — normalne zachowanie
            }
        }, token);
    }

    private async Task FlushPendingAsync(CancellationToken ct)
    {
        List<string> tables;

        await _lock.WaitAsync(ct);
        try
        {
            tables = [.. _pendingTables];
            _pendingTables.Clear();
        }
        finally
        {
            _lock.Release();
        }

        if (tables.Count == 0) return;
        if (string.IsNullOrWhiteSpace(_accessPath) || string.IsNullOrWhiteSpace(_mysqlConnectionString)) return;

        _logger.LogInformation("Auto-sync: eksport tabel {Tables}", string.Join(", ", tables));

        try
        {
            // Tworzymy własny scope — AccessAutoSyncService jest Singleton,
            // a IAccessExportService jest Scoped.
            // Bez scope factory Singleton trzymałby zdisposowany scoped serwis.
            using var scope = _scopeFactory.CreateScope();
            var exportService = scope.ServiceProvider.GetRequiredService<IAccessExportService>();

            var results = await exportService.ExportTablesToAccessAsync(
                tables, _mysqlConnectionString, _accessPath,
                progress: null, cancellationToken: ct);

            ExportCompleted?.Invoke(this, results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd auto-sync MySQL → Access");
            ExportError?.Invoke(this, ex.Message);
        }
    }

    // ── Dispose ──────────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _lock.Dispose();
    }
}
