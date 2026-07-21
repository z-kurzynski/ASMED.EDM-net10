using ASMED.WPF.Models;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Globalna sesja zalogowanego użytkownika (Singleton)
    /// </summary>
    public static class UserSession
    {
        /// <summary>
        /// Aktualnie zalogowany użytkownik
        /// </summary>
        public static User? CurrentUser { get; private set; }

        /// <summary>
        /// Czy użytkownik jest zalogowany
        /// </summary>
        public static bool IsLoggedIn => CurrentUser != null;

        /// <summary>
        /// Loguje użytkownika (ustawia CurrentUser)
        /// </summary>
        public static void Login(User user)
        {
            CurrentUser = user;
            // System.Diagnostics.Debug.WriteLine($"✅ Zalogowano: {user.Username} ({user.Role})");
        }

        /// <summary>
        /// Wylogowuje użytkownika (czyści CurrentUser)
        /// </summary>
        public static void Logout()
        {
            var username = CurrentUser?.Username ?? "nieznany";
            CurrentUser = null;
            // System.Diagnostics.Debug.WriteLine($"🚪 Wylogowano: {username}");
        }

        /// <summary>
        /// Sprawdza czy użytkownik ma wymaganą rolę
        /// </summary>
        public static bool HasRole(UserRole requiredRole)
        {
            if (!IsLoggedIn)
                return false;

            // SuperAdmin ma dostęp do wszystkiego
            if (CurrentUser?.Role == UserRole.SuperAdmin)
                return true;

            return CurrentUser?.Role == requiredRole;
        }

        /// <summary>
        /// Sprawdza czy użytkownik ma jedną z wymaganych ról
        /// </summary>
        public static bool HasAnyRole(params UserRole[] roles)
        {
            if (!IsLoggedIn)
                return false;

            // SuperAdmin ma dostęp do wszystkiego
            if (CurrentUser?.Role == UserRole.SuperAdmin)
                return true;

            foreach (var role in roles)
            {
                if (CurrentUser?.Role == role)
                    return true;
            }

            return false;
        }
    }
}
