using ASMED.WPF.Helpers;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.Views
{
    public partial class LoginWindow : Window
    {
        public bool LoginSuccessful { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();

            // ? Focus na Username przy starcie
            Loaded += (s, e) => UsernameTextBox.Focus();
        }

        /// <summary>
        /// Loguje użytkownika
        /// </summary>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            TryLogin();
        }

        /// <summary>
        /// Anuluje logowanie i zamyka aplikację
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoginSuccessful = false;
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Enter w polu Username przenosi do Password
        /// </summary>
        private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Enter w polu Password próbuje zalogować
        /// </summary>
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryLogin();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Próba logowania
        /// </summary>
        private void TryLogin()
        {
            // Ukryj poprzedni błąd
            ErrorMessage.Visibility = Visibility.Collapsed;

            var username = UsernameTextBox.Text?.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Podaj nazwę użytkownika");
                UsernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Podaj hasło");
                PasswordBox.Focus();
                return;
            }

            try
            {
                var db = new AccessDbContext();
                var user = db.AuthenticateUser(username, password);

                if (user != null)
                {
                    // Logowanie udane
                    UserSession.Login(user);
                    LoginSuccessful = true;

                    // System.Diagnostics.Debug.WriteLine($"✅ Zalogowano: {user.Username} ({user.Role})");
                    // System.Diagnostics.Debug.WriteLine($"🔐 Ustawiam DialogResult = true...");

                    // ✅ Zapisz logowanie w historii
                    db.LogLoginAttempt(user.Id, user.Username, success: true);

                    DialogResult = true;

                    // System.Diagnostics.Debug.WriteLine($"🔐 Zamykam LoginWindow...");
                    Close();
                    // System.Diagnostics.Debug.WriteLine($"🔐 LoginWindow zamknięte");
                }
                else
                {
                    // ? Nieprawidłowe dane
                    ShowError("? Nieprawidłowa nazwa użytkownika lub hasło");
                    PasswordBox.Clear();
                    PasswordBox.Focus();

                    // System.Diagnostics.Debug.WriteLine($"? Nieudana próba logowania: {username}");

                    // ✅ Zapisz nieudaną próbę logowania w historii
                    db.LogLoginAttempt(null, username, success: false, failureReason: "Nieprawidłowe hasło");
                }
            }
            catch (System.Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"? Błąd logowania: {ex.Message}");
                ShowError($"Błąd systemu: {ex.Message}");

                // ✅ Zapisz błąd logowania w historii
                try
                {
                    var db2 = new AccessDbContext();
                    db2.LogLoginAttempt(null, username, success: false, failureReason: $"Błąd: {ex.Message}");
                }
                catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Pokazuje komunikat błędu
        /// </summary>
        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }
    }
}
