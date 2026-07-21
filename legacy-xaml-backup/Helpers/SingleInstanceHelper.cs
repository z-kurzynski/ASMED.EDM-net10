using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Zapewnia że tylko jedna instancja aplikacji jest uruchomiona.
    /// Jeśli użytkownik uruchomi drugą instancję, aktywuje istniejące okno.
    /// ✅ OBSŁUGUJE "zombie mutex" (pozostały po crash'u)
    /// </summary>
    public static class SingleInstanceHelper
    {
        private static Mutex? _mutex;
        private const string MutexName = "ASMED_WPF_SingleInstance_Mutex_{B7A3F8D1-2E4C-4A9B-8F3D-6E5A9C7B4D2E}";

        /// <summary>
        /// Sprawdza czy aplikacja już działa. Jeśli tak, aktywuje istniejące okno i zwraca false.
        /// ✅ POPRAWIONE: Obsługuje porzucone mutexy (zombie mutex)
        /// </summary>
        /// <param name="allowMultipleInstances">Jeśli true, pozwala na wiele instancji (np. tryb deweloperski)</param>
        /// <returns>True jeśli to pierwsza instancja, False jeśli aplikacja już działa</returns>
        public static bool EnsureSingleInstance(bool allowMultipleInstances = false)
        {
            // ✅ Sprawdź czy użytkownik trzyma Shift (pozwala na drugą instancję)
            bool shiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (allowMultipleInstances || shiftPressed)
            {
                // System.Diagnostics.Debug.WriteLine("⚠️ Tryb wieloinstancyjny aktywny (Shift lub allowMultipleInstances=true)");
                return true; // Pozwól na wiele instancji
            }

            try
            {
                bool createdNew = false;

                try
                {
                    // ✅ POPRAWIONE: Użyj WaitOne z timeout aby obsłużyć porzucone mutexy
                    _mutex = new Mutex(true, MutexName, out createdNew);

                    if (!createdNew)
                    {
                        // Mutex już istnieje - sprawdź czy to zombie czy żywy proces
                        // System.Diagnostics.Debug.WriteLine("⚠️ Mutex już istnieje. Sprawdzam czy proces naprawdę działa...");

                        // ✅ KROK 1: Sprawdź czy proces naprawdę istnieje
                        if (IsAnotherInstanceRunning())
                        {
                            // ✅ Proces działa - aktywuj okno
                            // System.Diagnostics.Debug.WriteLine("✅ Znaleziono działający proces. Aktywuję okno...");
                            ActivateExistingInstance();
                            return false; // Zamknij tę instancję
                        }
                        else
                        {
                            // ❌ ZOMBIE MUTEX: Mutex istnieje, ale proces nie działa!
                            // System.Diagnostics.Debug.WriteLine("⚠️ ZOMBIE MUTEX wykryty! Mutex istnieje, ale brak procesu.");
                            // System.Diagnostics.Debug.WriteLine("🔄 Próbuję przejąć mutex...");

                            // Spróbuj przejąć mutex z timeout
                            bool acquired = false;
                            try
                            {
                                acquired = _mutex.WaitOne(TimeSpan.FromSeconds(5), false);
                            }
                            catch (AbandonedMutexException)
                            {
                                // ✅ Mutex został porzucony - możemy go przejąć!
                                // System.Diagnostics.Debug.WriteLine("✅ Przejęto porzucony mutex (AbandonedMutexException)");
                                acquired = true;
                            }

                            if (acquired)
                            {
                                // System.Diagnostics.Debug.WriteLine("✅ Zombie mutex usunięty. Uruchamiam aplikację.");
                                return true; // To pierwsza ŻYWA instancja
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine("❌ Nie można przejąć mutex. Timeout.");
                                MessageBox.Show(
                                    "Nie można uruchomić aplikacji.\n\n" +
                                    "Mutex jest zablokowany przez inny proces.\n" +
                                    "Spróbuj ponownie za chwilę lub uruchom z klawiszem SHIFT.",
                                    "ASMED - Błąd uruchomienia",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return false;
                            }
                        }
                    }
                }
                catch (AbandonedMutexException)
                {
                    // ✅ Mutex został porzucony (crash/kill) - możemy go przejąć!
                    // System.Diagnostics.Debug.WriteLine("✅ AbandonedMutexException - mutex był porzucony. Przejmujemy kontrolę.");
                    createdNew = true; // Traktuj jako nową instancję
                }

                if (createdNew)
                {
                    // System.Diagnostics.Debug.WriteLine("✅ Pierwsza instancja aplikacji ASMED");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd sprawdzania pojedynczej instancji: {ex.Message}");

                // W razie błędu pozwól uruchomić (lepsze niż całkowite zablokowanie)
                MessageBox.Show(
                    $"Wystąpił błąd podczas sprawdzania instancji aplikacji.\n\n" +
                    $"Błąd: {ex.Message}\n\n" +
                    "Aplikacja zostanie uruchomiona.",
                    "ASMED - Ostrzeżenie",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return true;
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Sprawdza czy inny proces ASMED_3 naprawdę działa
        /// </summary>
        private static bool IsAnotherInstanceRunning()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var process in processes)
                {
                    // Pomiń obecny proces
                    if (process.Id == currentProcess.Id)
                        continue;

                    // ✅ Znaleziono inny proces o tej samej nazwie
                    // System.Diagnostics.Debug.WriteLine($"✅ Znaleziono proces: PID={process.Id}, MainWindowHandle={process.MainWindowHandle}");
                    return true;
                }

                // System.Diagnostics.Debug.WriteLine("❌ Brak innych procesów ASMED");
                return false;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd sprawdzania procesów: {ex.Message}");
                return false; // W razie błędu zakładamy że proces nie działa
            }
        }

        /// <summary>
        /// Zwalnia Mutex przy zamykaniu aplikacji
        /// </summary>
        public static void ReleaseMutex()
        {
            try
            {
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                    _mutex = null;
                    // System.Diagnostics.Debug.WriteLine("✅ Mutex zwolniony");
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"⚠️ Błąd zwalniania Mutex: {ex.Message}");
            }
        }

        /// <summary>
        /// Znajduje i aktywuje istniejące okno aplikacji ASMED
        /// </summary>
        private static void ActivateExistingInstance()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var process in processes)
                {
                    // Pomiń obecny proces
                    if (process.Id == currentProcess.Id)
                        continue;

                    // Znajdź główne okno procesu
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        // System.Diagnostics.Debug.WriteLine($"✅ Znaleziono istniejące okno: PID={process.Id}, Handle={process.MainWindowHandle}");

                        // Przywróć okno jeśli zminimalizowane
                        ShowWindow(process.MainWindowHandle, SW_RESTORE);

                        // Przenieś okno na wierzch
                        SetForegroundWindow(process.MainWindowHandle);

                        // ✅ Pokaż MessageBox ZAWSZE NA WIERZCHU
                        ShowTopMostMessageBox(
                            "Aplikacja ASMED już działa!\n\n" +
                            "Okno zostało przeniesione na wierzch.\n\n" +
                            "💡 Wskazówka: Aby uruchomić drugą instancję,\n" +
                            "trzymaj klawisz SHIFT podczas uruchamiania.",
                            "ASMED - Aplikacja już uruchomiona");

                        return;
                    }
                }

                // Nie znaleziono okna - może być zminimalizowane do tray
                // System.Diagnostics.Debug.WriteLine("⚠️ Znaleziono proces, ale brak MainWindowHandle (może być w tray)");

                ShowTopMostMessageBox(
                    "Aplikacja ASMED już działa w tle.\n\n" +
                    "Sprawdź pasek zadań lub zasobnik systemowy.",
                    "ASMED - Aplikacja już uruchomiona");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd aktywacji okna: {ex.Message}");
            }
        }

        /// <summary>
        /// Wyświetla MessageBox zawsze na wierzchu (topmost)
        /// </summary>
        private static void ShowTopMostMessageBox(string message, string title)
        {
            // Tworzymy niewidoczne okno jako owner dla MessageBox
            var ownerWindow = new Window
            {
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Topmost = true,
                ShowActivated = true,
                Left = -10000,
                Top = -10000
            };

            try
            {
                ownerWindow.Show();
                ownerWindow.Activate();

                MessageBox.Show(
                    ownerWindow,
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            finally
            {
                ownerWindow.Close();
            }
        }

        // ═══════════════════════════════════════════════════════
        // Win32 API dla aktywacji okna
        // ═══════════════════════════════════════════════════════

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;
    }
}
