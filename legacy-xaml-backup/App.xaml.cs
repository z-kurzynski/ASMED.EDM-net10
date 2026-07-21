using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ASMED.WPF.Views;
using ASMED.WPF.Services;
using Syncfusion.Licensing;
using ASMED.WPF.Helpers;

namespace ASMED.WPF
{
    public partial class App : Application
    {
        public App()
        {
            //Register Syncfusion license 34.x.x
            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JAaF5cX2pCd1p/TH5YfUNzdUVEY1ZUTXxaS1ZhSXxVdkJhWH5bdXBRRGBUU0J9XEY=");


            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("pl-PL");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new System.Globalization.CultureInfo("pl-PL");
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("pl-PL");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("pl-PL");

            // ⚠️ TYMCZASOWE: InitializeComponent() jest zakomentowany, ponieważ kompilator nie generuje App.g.cs
            // Zasoby z App.xaml nie będą załadowane, ale aplikacja powinna się uruchomić
            // Rozwiązanie: Zamknij Visual Studio, usuń foldery bin i obj, otwórz ponownie i zrób Rebuild
            // InitializeComponent();

            // ✅ FIX: Obsługa wyjątku LayoutTransform.ScaleX z Notifications.Wpf
            // Problem: Biblioteka próbuje animować LayoutTransform.ScaleX, ale kontrolka ma MatrixTransform
            // Rozwiązanie: Złap wyjątek i pozwól aplikacji działać dalej (powiadomienie zamknie się bez animacji)
            this.DispatcherUnhandledException += (sender, e) =>
            {
                // Sprawdź czy to jest znany błąd z Notifications.Wpf
                if (e.Exception is InvalidOperationException &&
                    e.Exception.Message.Contains("LayoutTransform.ScaleX") &&
                    e.Exception.StackTrace?.Contains("Notifications.Wpf") == true)
                {
                    // Zaloguj błąd do debugowania
                    // System.Diagnostics.Debug.WriteLine($"⚠️ Złapano znany błąd Notifications.Wpf: {e.Exception.Message}");
                    // System.Diagnostics.Debug.WriteLine("   Powiadomienie zamknie się bez animacji.");

                    // Oznacz wyjątek jako obsłużony - aplikacja będzie działać dalej
                    e.Handled = true;
                }
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // ✅ DODANE: Sprawdź czy aplikacja już działa
            bool isFirstInstance = SingleInstanceHelper.EnsureSingleInstance(
                allowMultipleInstances: false // Zmień na true podczas deweloperki jeśli potrzebujesz
            );

            if (!isFirstInstance)
            {
                // Aplikacja już działa - zamknij tę instancję
                // System.Diagnostics.Debug.WriteLine("⛔ Aplikacja już działa. Zamykam duplikat.");
                Shutdown();
                return;
            }

            // ✅ Kontynuuj normalne uruchomienie
            base.OnStartup(e);

            // ✅ KLUCZOWE: Ustaw ShutdownMode aby aplikacja nie zamykała się po zamknięciu LoginWindow
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // ═══════════════════════════════════════════════════════
            // ✅ NOWE: Inicjalizacja systemu użytkowników
            // ═══════════════════════════════════════════════════════
            // System.Diagnostics.Debug.WriteLine("👤 Inicjalizacja systemu użytkowników...");
            try
            {
                var db = new AccessDbContext();

                // Utwórz tabelę Users jeśli nie istnieje
                // System.Diagnostics.Debug.WriteLine("👤 Tworzę tabelę Users...");
                db.CreateUsersTableIfNotExists();

                // Utwórz tabelę LoginHistory jeśli nie istnieje
                // System.Diagnostics.Debug.WriteLine("👤 Tworzę tabelę LoginHistory...");
                db.CreateLoginHistoryTableIfNotExists();

                // Zainicjalizuj super admina (tesla/2025) jeśli baza pusta
                // System.Diagnostics.Debug.WriteLine("👤 Inicjalizuję super admina...");
                db.InitializeSuperAdmin();

                // System.Diagnostics.Debug.WriteLine("✅ System użytkowników zainicjalizowany");
            }
            catch (System.Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd inicjalizacji Users: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                MessageBox.Show(
                    $"Błąd inicjalizacji systemu użytkowników:\n{ex.Message}",
                    "Błąd krytyczny",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // ═══════════════════════════════════════════════════════
            // ✅ NOWE: Okno logowania
            // ═══════════════════════════════════════════════════════
            // System.Diagnostics.Debug.WriteLine("🔐 Pokazuję okno logowania...");

            var loginWindow = new LoginWindow();
            var loginResult = loginWindow.ShowDialog();

            // System.Diagnostics.Debug.WriteLine($"🔐 LoginResult: {loginResult}, LoginSuccessful: {loginWindow.LoginSuccessful}");

            if (loginResult != true || !loginWindow.LoginSuccessful)
            {
                // Anulowano logowanie - zamknij aplikację
                // System.Diagnostics.Debug.WriteLine("🚪 Anulowano logowanie - zamykam aplikację");
                Shutdown();
                return;
            }

            // Sprawdź czy użytkownik się zalogował
            if (!UserSession.IsLoggedIn)
            {
                // System.Diagnostics.Debug.WriteLine("❌ Brak zalogowanego użytkownika");
                MessageBox.Show(
                    "Nie udało się zalogować. Aplikacja zostanie zamknięta.",
                    "Błąd logowania",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // System.Diagnostics.Debug.WriteLine($"✅ Zalogowano: {UserSession.CurrentUser.Username} ({UserSession.CurrentUser.Role})");
            // System.Diagnostics.Debug.WriteLine("🚀 Ładuję MainWindow...");

            // ═══════════════════════════════════════════════════════
            // ✅ Pokaż splash screen
            // ═══════════════════════════════════════════════════════

            // uruchom splash w osobnym wątku STA aby nie blokować głównego UI
            var splashReady = new ManualResetEventSlim(false);
            Thread splashThread = new Thread(() =>
            {
                var splash = new SplashWindow();
                // zarejestruj splash w serwisie razem z wątkiem
                SplashService.Instance.Register(splash, Thread.CurrentThread);
                splash.Show();
                // sygnalizuj, że splash został pokazany
                splashReady.Set();
                // uruchom pętlę Dispatcher dla tego wątku
                System.Windows.Threading.Dispatcher.Run();
            })
            {
                IsBackground = true
            };
            splashThread.SetApartmentState(ApartmentState.STA);
            splashThread.Start();

            // Poczekaj aż splash pojawi się
            splashReady.Wait();

            // Krótkie opóźnienie aby splash mógł wyrenderować
            await Task.Delay(80);

            // Teraz utwórz i pokaż główne okno (może wykonywać ciężkie operacje)
            // System.Diagnostics.Debug.WriteLine("🏠 Tworzę MainWindow...");
            var main = new MainWindow();

            // System.Diagnostics.Debug.WriteLine("🏠 MainWindow utworzone, ustawiam MainWindow property...");

            // Zamknij splash po załadowaniu MainWindow
            main.Loaded += (s, args) =>
            {
                // System.Diagnostics.Debug.WriteLine("🏠 MainWindow.Loaded event - zamykam splash...");
                try { SplashService.Instance.Close(); } catch { }
            };

            MainWindow = main;
            // System.Diagnostics.Debug.WriteLine("🏠 Pokazuję MainWindow...");
            main.Show();
            // System.Diagnostics.Debug.WriteLine("🏠 MainWindow.Show() wywołane");

            // ✅ Przywróć normalny ShutdownMode (zamknięcie MainWindow kończy aplikację)
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // ✅ Zapisz czas wylogowania przed zamknięciem
            try
            {
                if (UserSession.IsLoggedIn && UserSession.CurrentUser != null)
                {
                    var db = new AccessDbContext();
                    var endLoginTime = DateTime.Now;

                    // Zapisz EndLogin w tabeli Users
                    bool success = db.UpdateEndLogin(UserSession.CurrentUser.Id, endLoginTime);

                    if (success)
                    {
                        // System.Diagnostics.Debug.WriteLine($"🚪 EndLogin zapisany: {UserSession.CurrentUser.Username} → {endLoginTime:yyyy-MM-dd HH:mm:ss}");
                    }

                    // ✅ Zapisz wylogowanie w historii
                    db.LogLogout(UserSession.CurrentUser.Id, endLoginTime);
                    // System.Diagnostics.Debug.WriteLine($"📝 Logout zapisany w historii: UserId={UserSession.CurrentUser.Id}");
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd zapisu EndLogin/Logout: {ex.Message}");
            }

            // ✅ DODANE: Zwolnij Mutex przy zamykaniu
            SingleInstanceHelper.ReleaseMutex();

            base.OnExit(e);
        }
    }
}

// ENC0023 oznacza, że nie można dynamicznie dodać lub zmienić metod abstrakcyjnych/zastępowanych podczas debugowania z funkcją "Edytuj i kontynuuj".
// Rozwiązanie: Zatrzymaj debugowanie, wprowadź zmiany, a następnie ponownie uruchom aplikację.
// Kod nie wymaga zmian – to ograniczenie środowiska Visual Studio, nie błąd w kodzie.
// Jeśli zmieniłeś sygnaturę lub dodałeś/zastąpiłeś metodę (np. OnStartup), musisz ponownie uruchomić aplikację, aby zmiany zostały uwzględnione.
