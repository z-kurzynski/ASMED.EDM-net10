using System;
using System.Security.Cryptography;
using System.Text;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Helper do hashowania i weryfikacji hase³ (SHA256)
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Hashuje has³o u¿ywaj¹c SHA256 + salt
        /// </summary>
        /// <param name="password">Has³o do zahashowania</param>
        /// <returns>Hash has³a w formacie Base64</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Has³o nie mo¿e byæ puste", nameof(password));

            // ? Dodaj sta³y salt (dla prostoty - w produkcji u¿yj losowego salt per u¿ytkownik)
            const string salt = "ASMED_2025_SALT";
            var saltedPassword = password + salt;

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(saltedPassword);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Weryfikuje has³o porównuj¹c z zapisanym hashem
        /// </summary>
        /// <param name="password">Has³o do sprawdzenia</param>
        /// <param name="storedHash">Zapisany hash</param>
        /// <returns>True jeœli has³o jest poprawne</returns>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                var newHash = HashPassword(password);
                return newHash == storedHash;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generuje losowe has³o (8 znaków: litery + cyfry)
        /// </summary>
        public static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            var password = new char[8];

            for (int i = 0; i < 8; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }

            return new string(password);
        }
    }
}
