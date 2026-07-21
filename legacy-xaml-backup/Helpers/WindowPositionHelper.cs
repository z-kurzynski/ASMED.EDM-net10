using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Pomocnicza klasa do zarz�dzania pozycj� i stanem okien WPF
    /// U�ywa Win32 API do precyzyjnego pozycjonowania na wielu monitorach
    /// </summary>
    public static class WindowPositionHelper
    {
        #region Win32 API Declarations

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const uint MONITORINFOF_PRIMARY = 1;

        #endregion

        #region Monitor Detection

        private static List<MONITORINFO> GetAllMonitors()
        {
            var monitors = new List<MONITORINFO>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
                {
                    var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        monitors.Add(mi);
                    }
                    return true;
                }, IntPtr.Zero);

            // Sortuj monitory od lewej do prawej
            monitors = monitors.OrderBy(m => m.rcWork.Left).ToList();

            // System.Diagnostics.Debug.WriteLine($"??? Wykryto {monitors.Count} monitor(�w):");
            for (int i = 0; i < monitors.Count; i++)
            {
                var m = monitors[i];
                bool isPrimary = (m.dwFlags & MONITORINFOF_PRIMARY) != 0;
                // System.Diagnostics.Debug.WriteLine($"  Monitor {i}: Left={m.rcWork.Left}, Top={m.rcWork.Top}, " +
                    //     $"Width={m.rcWork.Width}, Height={m.rcWork.Height}, Primary={isPrimary}");
            }

            return monitors;
        }

        private static MONITORINFO GetCurrentMonitor(IntPtr hwnd)
        {
            var hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
            GetMonitorInfo(hMonitor, ref mi);
            return mi;
        }

        #endregion

        /// <summary>
        /// Przesuwa okno na lewy monitor (symuluje Win+Shift+?)
        /// </summary>
        public static void MoveToLeftMonitor(Window window)
        {
            if (window == null) return;

            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                // Przywr�� okno je�li jest zmaksymalizowane
                if (window.WindowState == WindowState.Maximized)
                    window.WindowState = WindowState.Normal;

                var monitors = GetAllMonitors();
                if (monitors.Count < 2)
                {
                    // System.Diagnostics.Debug.WriteLine("?? Tylko 1 monitor wykryty - nie mo�na przesun�� na lewy");
                    MessageBox.Show("Wykryto tylko jeden monitor. Funkcja dzia�a z wieloma monitorami.",
                        "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Pobierz aktualny monitor
                var currentMonitor = GetCurrentMonitor(hwnd);
                var currentIndex = monitors.FindIndex(m =>
                    m.rcWork.Left == currentMonitor.rcWork.Left &&
                    m.rcWork.Top == currentMonitor.rcWork.Top);

                // Znajd� monitor po lewej
                int targetIndex = currentIndex - 1;
                if (targetIndex < 0)
                {
                    // Je�li ju� jeste�my na skrajnie lewym, przejd� na skrajnie prawy (wrap around)
                    targetIndex = monitors.Count - 1;
                }

                var targetMonitor = monitors[targetIndex];

                // Pobierz aktualne wymiary okna
                GetWindowRect(hwnd, out RECT windowRect);
                int windowWidth = windowRect.Width;
                int windowHeight = windowRect.Height;

                // Oblicz pozycj� na docelowym monitorze (wycentrowane)
                int targetX = targetMonitor.rcWork.Left + (targetMonitor.rcWork.Width - windowWidth) / 2;
                int targetY = targetMonitor.rcWork.Top + (targetMonitor.rcWork.Height - windowHeight) / 2;

                // Przesu� okno
                SetWindowPos(hwnd, IntPtr.Zero, targetX, targetY, windowWidth, windowHeight,
                    SWP_NOZORDER | SWP_SHOWWINDOW);

                // System.Diagnostics.Debug.WriteLine($"? Okno przeniesione na lewy monitor (index {targetIndex}): X={targetX}, Y={targetY}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"? B��d przesuwania na lewy monitor: {ex.Message}");
                MessageBox.Show($"Nie uda�o si� przenie�� okna na lewy monitor:\n{ex.Message}",
                    "B��d", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Przesuwa okno na prawy monitor (symuluje Win+Shift+?)
        /// </summary>
        public static void MoveToRightMonitor(Window window)
        {
            if (window == null) return;

            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return;

                // Przywr�� okno je�li jest zmaksymalizowane
                if (window.WindowState == WindowState.Maximized)
                    window.WindowState = WindowState.Normal;

                var monitors = GetAllMonitors();
                if (monitors.Count < 2)
                {
                    // System.Diagnostics.Debug.WriteLine("?? Tylko 1 monitor wykryty - nie mo�na przesun�� na prawy");
                    MessageBox.Show("Wykryto tylko jeden monitor. Funkcja dzia�a z wieloma monitorami.",
                        "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Pobierz aktualny monitor
                var currentMonitor = GetCurrentMonitor(hwnd);
                var currentIndex = monitors.FindIndex(m =>
                    m.rcWork.Left == currentMonitor.rcWork.Left &&
                    m.rcWork.Top == currentMonitor.rcWork.Top);

                // Znajd� monitor po prawej
                int targetIndex = currentIndex + 1;
                if (targetIndex >= monitors.Count)
                {
                    // Je�li ju� jeste�my na skrajnie prawym, przejd� na skrajnie lewy (wrap around)
                    targetIndex = 0;
                }

                var targetMonitor = monitors[targetIndex];

                // Pobierz aktualne wymiary okna
                GetWindowRect(hwnd, out RECT windowRect);
                int windowWidth = windowRect.Width;
                int windowHeight = windowRect.Height;

                // Oblicz pozycj� na docelowym monitorze (wycentrowane)
                int targetX = targetMonitor.rcWork.Left + (targetMonitor.rcWork.Width - windowWidth) / 2;
                int targetY = targetMonitor.rcWork.Top + (targetMonitor.rcWork.Height - windowHeight) / 2;

                // Przesu� okno
                SetWindowPos(hwnd, IntPtr.Zero, targetX, targetY, windowWidth, windowHeight,
                    SWP_NOZORDER | SWP_SHOWWINDOW);

                // System.Diagnostics.Debug.WriteLine($"? Okno przeniesione na prawy monitor (index {targetIndex}): X={targetX}, Y={targetY}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"? B��d przesuwania na prawy monitor: {ex.Message}");
                MessageBox.Show($"Nie uda�o si� przenie�� okna na prawy monitor:\n{ex.Message}",
                    "B��d", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Maksymalizuje okno na pe�ny ekran
        /// </summary>
        public static void MaximizeWindow(Window window)
        {
            if (window == null) return;

            try
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    // Je�li ju� zmaksymalizowane, przywr�� normalny rozmiar
                    window.WindowState = WindowState.Normal;
                    // System.Diagnostics.Debug.WriteLine("? Okno przywr�cone do normalnego rozmiaru");
                }
                else
                {
                    // Zmaksymalizuj
                    window.WindowState = WindowState.Maximized;
                    // System.Diagnostics.Debug.WriteLine("? Okno zmaksymalizowane");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"? B��d maksymalizacji okna: {ex.Message}");
                MessageBox.Show($"Nie uda�o si� zmaksymalizowa� okna:\n{ex.Message}",
                    "B��d", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Przywraca normalne rozmiary okna
        /// </summary>
        public static void RestoreWindow(Window window)
        {
            if (window == null) return;

            try
            {
                window.WindowState = WindowState.Normal;
                // System.Diagnostics.Debug.WriteLine("? Okno przywr�cone do normalnego rozmiaru");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"? B��d przywracania okna: {ex.Message}");
            }
        }

        /// <summary>
        /// Centruje okno na ekranie
        /// </summary>
        public static void CenterWindow(Window window)
        {
            if (window == null) return;

            try
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                // System.Diagnostics.Debug.WriteLine("? Okno wycentrowane");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"? B��d centrowania okna: {ex.Message}");
            }
        }
    }
}
