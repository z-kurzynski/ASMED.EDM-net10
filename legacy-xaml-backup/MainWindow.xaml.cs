using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ASMED.WPF.ViewModels;
using System;
using System.Timers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ASMED.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // ? Windows API dla TopMost
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        // ? NOWE: Timer dla automatycznego wyłączenia TopMost
        private DispatcherTimer? _topMostAutoDisableTimer;
        private const int TOP_MOST_DURATION_SECONDS = 10;

        public MainWindow()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pl-PL");

            // Konfiguracja timera zegara (bez uruchamiania)
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;

            // Inicjalizacja komponentów UI
            InitializeComponent();
            DataContext = new ASMED.WPF.ViewModels.MainWindowViewModel();

            // Uruchom timer DOPIERO po zainicjalizowaniu UI
            _clockTimer.Start();

            // Uruchom TopMost przy starcie
            this.Loaded += MainWindow_Loaded;
            this.SourceInitialized += MainWindow_SourceInitialized;
        }

        /// <summary>
        /// ? NOWA WERSJA: TopMost tylko przez pierwsze 10 sekund
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("MainWindow: Uruchamiam TopMost na 10 sekund...");

                // ? Ustaw TopMost
                this.Topmost = true;

                // ? Znajdź checkbox i UKRYJ go (nie będzie potrzebny)
                var chkTopMost = this.FindName("chkTopMost") as CheckBox;
                if (chkTopMost != null)
                {
                    chkTopMost.Visibility = Visibility.Collapsed;  // Ukryj checkbox
                }

                // ? Uruchom timer na 10 sekund
                _topMostAutoDisableTimer = new DispatcherTimer();
                _topMostAutoDisableTimer.Interval = TimeSpan.FromSeconds(TOP_MOST_DURATION_SECONDS);
                _topMostAutoDisableTimer.Tick += TopMostAutoDisable_Tick;
                _topMostAutoDisableTimer.Start();

                // System.Diagnostics.Debug.WriteLine($"MainWindow: TopMost aktywny przez {TOP_MOST_DURATION_SECONDS} sekund");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded error: {ex.Message}");
            }
        }

        /// <summary>
        /// ? NOWA METODA: Wyłącz TopMost po 10 sekundach
        /// </summary>
        private void TopMostAutoDisable_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Zatrzymaj timer
                _topMostAutoDisableTimer?.Stop();
                _topMostAutoDisableTimer = null;

                // Wyłącz TopMost
                this.Topmost = false;

                // Wymusz przez Windows API
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                SetWindowPos(helper.Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

                // System.Diagnostics.Debug.WriteLine("MainWindow: ? TopMost automatycznie WYŁĄCZONY po 10 sekundach");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"TopMostAutoDisable_Tick error: {ex.Message}");
            }
        }

        /// <summary>
        /// ? ZMODYFIKOWANA: Wymuś TopMost przez API przy starcie
        /// </summary>
        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            try
            {
                // Wymuś TopMost przez Windows API
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                // System.Diagnostics.Debug.WriteLine("MainWindow: Wymuszono TopMost przez Windows API");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"MainWindow_SourceInitialized error: {ex.Message}");
            }
        }

        /// <summary>
        /// ? Handler dla CheckBox (opcjonalny - można usunąć jeśli checkbox jest ukryty)
        /// </summary>
        private void TopMost_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Topmost = true;
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                // System.Diagnostics.Debug.WriteLine("TopMost: WŁĄCZONY ręcznie");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"TopMost_Checked error: {ex.Message}");
            }
        }

        /// <summary>
        /// ? Handler dla CheckBox (opcjonalny - można usunąć jeśli checkbox jest ukryty)
        /// </summary>
        private void TopMost_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Topmost = false;
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                SetWindowPos(helper.Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                // System.Diagnostics.Debug.WriteLine("TopMost: WYŁĄCZONY ręcznie");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"TopMost_Unchecked error: {ex.Message}");
            }
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            if (ClockText != null)
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            }
        }
        private DispatcherTimer _clockTimer;

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Zatrzymaj timery
                _clockTimer?.Stop();
                _topMostAutoDisableTimer?.Stop();

                if (DataContext is IDisposable disposableVm)
                {
                    try { disposableVm.Dispose(); } catch { }
                }

                this.Close();
                Application.Current.Shutdown();

                System.Threading.Tasks.Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(2000);
                    try
                    {
                        var currentProcess = Process.GetCurrentProcess();
                        if (!currentProcess.HasExited)
                        {
                            currentProcess.Kill();
                        }
                    }
                    catch { }
                });
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CloseApp_Click error: {ex}");
                try
                {
                    Process.GetCurrentProcess().Kill();
                }
                catch { }
            }
        }

        private void PacjentSkierowanieView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                return;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _clockTimer?.Stop();
                _topMostAutoDisableTimer?.Stop();

                if (DataContext is IDisposable disposableVm)
                {
                    try { disposableVm.Dispose(); } catch { }
                }

            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"OnClosing cleanup error: {ex}");
            }

            base.OnClosing(e);
        }

        private void Baza_Danych_TabClosed(object sender, Syncfusion.Windows.Tools.Controls.CloseTabEventArgs e)
        {

        }
    }
}
// End of file MainWindow.xaml.cs
