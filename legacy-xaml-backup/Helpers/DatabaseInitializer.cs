using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Text;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Inicjalizuje i weryfikuje strukturę bazy danych Access.
    /// Sprawdza dostępność pliku .accdb, połączenie ODBC oraz obecność kluczowych tabel.
    /// </summary>
    public class DatabaseInitializer
    {
        // ── Wyniki inicjalizacji ─────────────────────────────────────────────
        public bool Success { get; private set; }
        public string StatusMessage { get; private set; } = string.Empty;
        public string Details { get; private set; } = string.Empty;

        // Tabele wymagane do poprawnego działania aplikacji
        private static readonly string[] WymaganeTabelе =
        {
            "Badania",
            "Pacjenci",
            "Skierowania",
            "Firmy",
            "Faktura",
            "BAD_Lista",
            "BAD_Cennik",
            "Rejestracja",
            "Users",
        };

        // ── Publiczne wejście ────────────────────────────────────────────────
        public void Initialize()
        {
            Success = false;
            StatusMessage = string.Empty;
            Details = string.Empty;

            var log = new StringBuilder();
            var brakujace = new List<string>();

            try
            {
                var dbPath = DatabaseConfiguration.UzywanaDbPath;
                log.AppendLine($"Ścieżka bazy: {dbPath}");
                log.AppendLine($"Typ bazy:     {DatabaseConfiguration.AktywnaDbTyp}");
                log.AppendLine();

                // 1. Sprawdź czy plik istnieje
                if (!File.Exists(dbPath))
                {
                    StatusMessage = $"❌ Plik bazy nie istnieje: {dbPath}";
                    log.AppendLine(StatusMessage);
                    Details = log.ToString();
                    return;
                }
                log.AppendLine($"✅ Plik istnieje ({new FileInfo(dbPath).Length / 1024:N0} KB)");

                // 2. Sprawdź połączenie ODBC
                try
                {
                    var helper = new AccessDbHelper();
                    helper.TestConnection();
                    log.AppendLine("✅ Połączenie ODBC — OK");
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ Błąd połączenia ODBC: {ex.Message}";
                    log.AppendLine(StatusMessage);
                    Details = log.ToString();
                    return;
                }

                // 3. Weryfikuj obecność tabel
                log.AppendLine();
                log.AppendLine("── Weryfikacja tabel ──────────────────");
                try
                {
                    var helper = new AccessDbHelper();
                    using var conn = helper.GetConnection();
                    conn.Open();

                    var schema = conn.GetSchema("Tables");

                    var istniejace = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (System.Data.DataRow row in schema.Rows)
                    {
                        var tblType = row["TABLE_TYPE"]?.ToString();
                        if (tblType == "TABLE" || tblType == "SYSTEM TABLE" || tblType == "VIEW")
                            istniejace.Add(row["TABLE_NAME"]?.ToString() ?? "");
                    }

                    foreach (var tabela in WymaganeTabelе)
                    {
                        if (istniejace.Contains(tabela))
                            log.AppendLine($"  ✅ {tabela}");
                        else
                        {
                            log.AppendLine($"  ⚠️ Brak tabeli: {tabela}");
                            brakujace.Add(tabela);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.AppendLine($"⚠️ Nie można odczytać schematu tabel: {ex.Message}");
                }

                // 4. Podsumowanie
                log.AppendLine();
                log.AppendLine("── Podsumowanie ───────────────────────");
                if (brakujace.Count == 0)
                {
                    log.AppendLine("✅ Wszystkie wymagane tabele są obecne.");
                    StatusMessage = "✅ Baza danych gotowa do pracy.";
                    Success = true;
                }
                else
                {
                    log.AppendLine($"⚠️ Brakujące tabele ({brakujace.Count}): {string.Join(", ", brakujace)}");
                    StatusMessage = $"⚠️ Baza połączona, brakuje {brakujace.Count} tabeli — sprawdź szczegóły.";
                    Success = true;
                }

                log.AppendLine($"Czas sprawdzenia: {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Błąd inicjalizacji: {ex.Message}";
                log.AppendLine(StatusMessage);
                log.AppendLine(ex.ToString());
                Success = false;
            }
            finally
            {
                Details = log.ToString();
            }
        }
    }
}
