using System;
using System.Data.Odbc;
using System.IO;

namespace ASMED.WPF.Helpers
{
    public class AccessDbHelper
    {
        private readonly string _connectionString;

        // Sterowniki w kolejności priorytetu (64-bit najpierw, potem starsze)
        private static readonly string[] KandydaciSterownikow =
        {
            "Microsoft Access Driver (*.mdb, *.accdb)",
            "Driver do Microsoft Access (*.mdb)",
            "Microsoft Access-Treiber (*.mdb)",
            "Microsoft Access Driver (*.mdb)",
        };

        public AccessDbHelper()
        {
            string dbPath = DatabaseConfiguration.UzywanaDbPath;
            _connectionString = BudujConnectionString(dbPath);
        }

        /// <summary>
        /// Próbuje każdego kandydata sterownika ODBC i zwraca pierwszy działający connection string.
        /// Rzuca wyjątek z pomocnym komunikatem jeśli żaden nie działa.
        /// </summary>
        private static string BudujConnectionString(string dbPath)
        {
            if (!File.Exists(dbPath))
                throw new FileNotFoundException(
                    $"Plik bazy danych nie istnieje:\n{dbPath}", dbPath);

            var errors = new System.Text.StringBuilder();

            foreach (var driver in KandydaciSterownikow)
            {
                var cs = $"Driver={{{driver}}};Dbq={dbPath};";
                try
                {
                    using var test = new OdbcConnection(cs);
                    test.Open();
                    // System.Diagnostics.Debug.WriteLine($"[AccessDbHelper] Używam sterownika: {driver}");
                    return cs;
                }
                catch (OdbcException ex)
                {
                    errors.AppendLine($"  • {driver} → {ex.Message.Split('\n')[0]}");
                }
            }

            throw new InvalidOperationException(
                $"Nie znaleziono działającego sterownika ODBC dla Microsoft Access.\n\n" +
                $"Próbowano:\n{errors}\n" +
                $"Zainstaluj: https://www.microsoft.com/en-us/download/details.aspx?id=54920\n" +
                $"(Microsoft Access Database Engine 2016 Redistributable x64)");
        }

        public OdbcConnection GetConnection()
        {
            return new OdbcConnection(_connectionString);
        }

        public void TestConnection()
        {
            using var conn = GetConnection();
            conn.Open();
        }

        internal IEnumerable<string> GetImiona()
        {
            throw new NotImplementedException();
        }
    }
}
