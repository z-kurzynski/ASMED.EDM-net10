using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ASMED.WPF.Views
{
    public partial class SplashWindow : Window
    {
        private CancellationTokenSource? _cts;

        public SplashWindow()
        {
            InitializeComponent();

            // ✅ Ustaw wersję programu (automatyczna data kompilacji)
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                // Pobierz InformationalVersion (zawiera datę kompilacji)
                var infoVersion = assembly
                    .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion;

                if (!string.IsNullOrEmpty(infoVersion))
                {
                    // Format: "1.0.0 (Build 2025-01-28 14:30)"
                    VersionText.Text = $"v{infoVersion}";
                }
                else
                {
                    // Fallback: tylko numer wersji
                    var version = assembly.GetName().Version;
                    VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";
                }
            }
            catch
            {
                VersionText.Text = "v1.0.0";
            }
        }

        // Możesz zewnętrznie zaktualizować komunikat podczas inicjalizacji.
        public void SetStatus(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            Dispatcher.Invoke(() => StatusText.Text = message);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // (re)start animacji
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = StartProgressLoopAsync(_cts.Token);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            try { _cts?.Cancel(); } catch { }
            _cts = null;
        }

        private async Task StartProgressLoopAsync(CancellationToken token)
        {
            try
            {
                var frameDelayMs = 16; // ~60 FPS
                var cycleDuration = TimeSpan.FromMilliseconds(2000); // 2 sekundy per full cycle

                while (!token.IsCancellationRequested)
                {
                    var sw = Stopwatch.StartNew();
                    while (sw.Elapsed < cycleDuration && !token.IsCancellationRequested)
                    {
                        var progress = Math.Min(100.0, sw.Elapsed.TotalMilliseconds / cycleDuration.TotalMilliseconds * 100.0);
                        // Aktualizuj UI
                        Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                ProgressBar.Progress = progress;
                                StatusText.Text = $"Ładowanie… {Math.Min(100, (int)progress)}%";
                            }
                            catch { }
                        });

                        try { await Task.Delay(frameDelayMs, token); } catch (TaskCanceledException) { break; }
                    }

                    // Upewnij się, że zakończymy cykl na 100%
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            ProgressBar.Progress = 100;
                            StatusText.Text = "Ładowanie… 100%";
                        }
                        catch { }
                    });

                    // Krótkie zatrzymanie przed kolejnym cyklem, pozwala zobaczyć 100%
                    try { await Task.Delay(150, token); } catch (TaskCanceledException) { break; }

                    // Zresetuj do 0 i powtórz cykl
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            ProgressBar.Progress = 0;
                            StatusText.Text = "Ładowanie…";
                        }
                        catch { }
                    });

                    // mała pauza zanim zacznie się następny cykl
                    try { await Task.Delay(50, token); } catch (TaskCanceledException) { break; }
                }
            }
            catch (OperationCanceledException) { /* oczekiwane przy anulowaniu */ }
            catch (Exception)
            {
                // nie przerywamy ładowania aplikacji z powodów animacji
            }
        }
    }
}