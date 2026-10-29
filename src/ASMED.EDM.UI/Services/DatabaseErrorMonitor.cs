using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using MySqlConnector;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Wychwytuje błędy zapytań SQL (nieznana kolumna/tabela, błąd składni itp.) w momencie ich rzucenia —
/// również te, które kod przechwytuje w catch i zamienia na pustą listę.
/// Każdy błąd jest zapisywany do pliku logu i pokazywany jako powiadomienie (z ograniczeniem powtórzeń).
/// </summary>
public static class DatabaseErrorMonitor
{
    // Kody MySQL oznaczające niezgodność zapytania ze strukturą bazy lub błąd SQL
    private static readonly HashSet<int> QueryErrorCodes =
    [
        1052, // ER_NON_UNIQ_ERROR — niejednoznaczna kolumna
        1054, // ER_BAD_FIELD_ERROR — nieznana kolumna
        1064, // ER_PARSE_ERROR — błąd składni
        1109, // ER_UNKNOWN_TABLE
        1146, // ER_NO_SUCH_TABLE
        1136, // ER_WRONG_VALUE_COUNT_ON_ROW
    ];

    private static readonly TimeSpan ToastThrottle = TimeSpan.FromSeconds(30);
    private static readonly int LogRetentionDays = 30;

    private static readonly ConditionalWeakTable<Exception, object> Seen = new();
    private static readonly Dictionary<string, DateTime> LastToast = new();
    private static readonly object Sync = new();

    [ThreadStatic] private static bool _inHandler;
    private static bool _started;

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ASMED.EDM.UI",
        "logs");

    public static string CurrentLogFile => Path.Combine(LogDirectory, $"db_errors_{DateTime.Now:yyyyMMdd}.log");

    public static void Start()
    {
        if (_started)
            return;
        _started = true;

        CleanupOldLogs();
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
    }

    private static void OnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        if (_inHandler || e.Exception is not MySqlException ex || !QueryErrorCodes.Contains(ex.Number))
            return;

        _inHandler = true;
        try
        {
            // Wyjątek przechodzący przez kolejne await/throw; wywołuje zdarzenie wielokrotnie — obsługujemy raz
            lock (Sync)
            {
                if (Seen.TryGetValue(ex, out _))
                    return;
                Seen.Add(ex, Sync);
            }

            var callers = GetAppCallers();
            WriteToLog(ex, callers);
            ShowToast(ex, callers.FirstOrDefault());
        }
        catch
        {
            // Monitor nie może wpływać na działanie aplikacji
        }
        finally
        {
            _inHandler = false;
        }
    }

    private static List<string> GetAppCallers()
    {
        var result = new List<string>();
        foreach (var frame in new StackTrace(2, false).GetFrames())
        {
            var type = frame.GetMethod()?.DeclaringType;
            if (type?.FullName is not { } fullName
                || !fullName.StartsWith("ASMED.EDM", StringComparison.Ordinal)
                || type == typeof(DatabaseErrorMonitor))
                continue;

            // Metody async: ASMED.EDM.X.Klasa+<Metoda>d__12.MoveNext → Klasa.Metoda
            var asyncMatch = Regex.Match(fullName, @"^(?<cls>[^+]+)\+<(?<method>[^>]+)>");
            var name = asyncMatch.Success
                ? $"{asyncMatch.Groups["cls"].Value}.{asyncMatch.Groups["method"].Value}"
                : $"{fullName}.{frame.GetMethod()!.Name}";

            if (result.Count == 0 || result[^1] != name)
                result.Add(name);
        }
        return result;
    }

    private static void WriteToLog(MySqlException ex, List<string> callers)
    {
        var sb = new StringBuilder()
            .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MySQL {ex.Number}: {ex.Message}");
        foreach (var caller in callers)
            sb.AppendLine($"    w {caller}");
        sb.AppendLine(new string('-', 80));

        lock (Sync)
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(CurrentLogFile, sb.ToString(), Encoding.UTF8);
        }
    }

    private static void ShowToast(MySqlException ex, string? caller)
    {
        var key = $"{ex.Number}|{ex.Message}";
        lock (Sync)
        {
            var now = DateTime.Now;
            if (LastToast.TryGetValue(key, out var last) && now - last < ToastThrottle)
                return;
            LastToast[key] = now;
        }

        var shortCaller = caller is null ? null : string.Join('.', caller.Split('.').TakeLast(2));
        var message = $"Błąd zapytania do bazy danych:\n{ex.Message}" +
                      (shortCaller is null ? "" : $"\nŹródło: {shortCaller}") +
                      $"\nLog: {CurrentLogFile}";

        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                new NotificationService().ShowToast(message, ToastType.Error, 10000);
            }
            catch
            {
                // np. okno zamykane w trakcie
            }
        });
    }

    private static void CleanupOldLogs()
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
                return;

            var cutoff = DateTime.Now.AddDays(-LogRetentionDays);
            foreach (var file in Directory.EnumerateFiles(LogDirectory, "db_errors_*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch
        {
            // sprzątanie opcjonalne
        }
    }
}
