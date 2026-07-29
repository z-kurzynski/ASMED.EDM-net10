using System;
using System.Threading;
using System.Windows;
using ASMED.WPF.Views;

namespace ASMED.WPF.Services
{
    public sealed class SplashService
    {
        public static SplashService Instance { get; } = new SplashService();

        private SplashWindow? _splash;
        private Thread? _splashThread;
        private readonly object _sync = new();

        private SplashService() { }

        // Rejestruj splash wraz z w¹tkiem, na którym zosta³ utworzony.
        public void Register(SplashWindow splash, Thread splashThread)
        {
            if (splash == null) return;
            lock (_sync)
            {
                _splash = splash;
                _splashThread = splashThread;
            }
        }

        public void SetStatus(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            SplashWindow? splash;
            lock (_sync) { splash = _splash; }
            if (splash == null) return;

            var disp = splash.Dispatcher;
            if (disp == null) return;

            try
            {
                if (disp.CheckAccess())
                    splash.SetStatus(message);
                else
                    disp.Invoke(() => splash.SetStatus(message));
            }
            catch { /* ignore */ }
        }

        public void Close()
        {
            SplashWindow? splash;
            Thread? thread;
            lock (_sync)
            {
                splash = _splash;
                thread = _splashThread;
                _splash = null;
                _splashThread = null;
            }

            if (splash == null) return;

            try
            {
                var disp = splash.Dispatcher;
                if (disp != null)
                {
                    // zamknij okno na jego Dispatcherze
                    disp.BeginInvoke(new Action(() =>
                    {
                        try { splash.Close(); } catch { }
                    }));

                    // poproœ Dispatcher o zakoñczenie pêtli
                    try
                    {
                        disp.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Normal);
                    }
                    catch { }
                }

                // opcjonalne: poczekaj krótko a¿ w¹tek splash siê zakoñczy
                if (thread != null && thread.IsAlive)
                {
                    try { thread.Join(5000); } catch { }
                }
            }
            catch { /* ignore */ }
        }
    }
}