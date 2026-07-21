using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Odbc;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Repository dla operacji importu danych z archiwum (Lx_Listy_do_faktur--old-baza)
    /// </summary>
    public class ArchiveImportRepository
    {
        private readonly AccessDbHelper _dbHelper;

        public ArchiveImportRepository()
        {
            _dbHelper = new AccessDbHelper();
        }

        /// <summary>
        /// Rekord z tabeli archiwum Lx_Listy_do_faktur--old-baza
        /// </summary>
        public class ArchiveListRecord : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
            }

            public int Identyfikator { get; set; }
            public int? Lx_ID_Faktura { get; set; }
            public int? Lx_ID_Firma { get; set; }
            public DateTime? Lx_Data { get; set; }
            public int Lx_ID_Badania { get; set; }
            public string? PacjentDisplay { get; set; }

            // Dodatkowe właściwości dla widoku
            public string? Lx_Faktura { get; set; }
            public string? Lx_Firma { get; set; }
            public string? Lx_Imie { get; set; }
            public string? Lx_Nazwisko { get; set; }
            public decimal? Lx_Razem { get; set; }
            public string? Lx_Uwagi { get; set; }

            // Dodatkowe pola cenowe z archiwum
            public decimal? Lx_Cena1 { get; set; }
            public decimal? Lx_Cena2 { get; set; }
            public decimal? Lx_Cena3 { get; set; }
            public decimal? Lx_Cena4 { get; set; }
            public decimal? Lx_Cena5 { get; set; }
            public decimal? Lx_Cena6 { get; set; }
            public decimal? Lx_Cena7 { get; set; }
            public decimal? Lx_Cena9 { get; set; }
            public int? Lx_ID_pacjent { get; set; }
            public int? Lx_ID_Skierowania { get; set; }
        }

        /// <summary>
        /// Pobiera rekordy z archiwum z opcjonalnym filtrem
        /// </summary>
        public List<ArchiveListRecord> GetArchiveListRecords(string? filter)
        {
            // System.Diagnostics.Debug.WriteLine("=== GetArchiveListRecords WYWOŁANA ===");
            var result = new List<ArchiveListRecord>();

            try
            {
                using var connection = _dbHelper.GetConnection();
                connection.Open();

                var sql = @"
SELECT 
    Identyfikator,
    Lx_ID_Faktura,
    Lx_ID_Firma,
    Lx_Firma,
    Lx_Faktura,
    Lx_ID_pacjent,
    Lx_Imie,
    Lx_Nazwisko,
    Lx_ID_Skierowania,
    Lx_ID_Badania,
    Lx_Data,
    Lx_Razem,
    Lx_Uwagi,
    Lx_Cena1,
    Lx_Cena2,
    Lx_Cena3,
    Lx_Cena4,
    Lx_Cena5,
    Lx_Cena6,
    Lx_Cena7,
    Lx_Cena9,
    Lx_End
FROM 
    [Lx_Listy_do_faktur--old-baza]
WHERE 
    Lx_ID_Faktura > 0 AND Lx_End = False";

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    sql += @" AND (
                Lx_Firma LIKE ? OR 
                Lx_Faktura LIKE ? OR
                Lx_Imie LIKE ? OR 
                Lx_Nazwisko LIKE ?
            )";
                }

                sql += " ORDER BY Lx_Data DESC, Lx_ID_Faktura, Lx_Nazwisko";

                using var cmd = new OdbcCommand(sql, connection);

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    var pattern = "%" + filter + "%";
                    cmd.Parameters.AddWithValue("@p1", pattern);
                    cmd.Parameters.AddWithValue("@p2", pattern);
                    cmd.Parameters.AddWithValue("@p3", pattern);
                    cmd.Parameters.AddWithValue("@p4", pattern);
                }

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var record = new ArchiveListRecord
                    {
                        Identyfikator = ParseInt(reader["Identyfikator"]) ?? 0,
                        Lx_ID_Faktura = ParseInt(reader["Lx_ID_Faktura"]),
                        Lx_ID_Firma = ParseInt(reader["Lx_ID_Firma"]),
                        Lx_ID_pacjent = ParseInt(reader["Lx_ID_pacjent"]),
                        Lx_ID_Skierowania = ParseInt(reader["Lx_ID_Skierowania"]),
                        Lx_ID_Badania = ParseInt(reader["Lx_ID_Badania"]) ?? 0,

                        Lx_Data = reader["Lx_Data"] != DBNull.Value ? Convert.ToDateTime(reader["Lx_Data"]) : null,

                        Lx_Faktura = reader["Lx_Faktura"]?.ToString(),
                        Lx_Firma = reader["Lx_Firma"]?.ToString(),
                        Lx_Imie = reader["Lx_Imie"]?.ToString(),
                        Lx_Nazwisko = reader["Lx_Nazwisko"]?.ToString(),
                        Lx_Uwagi = reader["Lx_Uwagi"]?.ToString(),

                        Lx_Razem = ParseDecimal(reader["Lx_Razem"]),
                        Lx_Cena1 = ParseDecimal(reader["Lx_Cena1"]),
                        Lx_Cena2 = ParseDecimal(reader["Lx_Cena2"]),
                        Lx_Cena3 = ParseDecimal(reader["Lx_Cena3"]),
                        Lx_Cena4 = ParseDecimal(reader["Lx_Cena4"]),
                        Lx_Cena5 = ParseDecimal(reader["Lx_Cena5"]),
                        Lx_Cena6 = ParseDecimal(reader["Lx_Cena6"]),
                        Lx_Cena7 = ParseDecimal(reader["Lx_Cena7"]),
                        Lx_Cena9 = ParseDecimal(reader["Lx_Cena9"]),

                        PacjentDisplay = string.IsNullOrEmpty(reader["Lx_Nazwisko"]?.ToString()) &&
                                        string.IsNullOrEmpty(reader["Lx_Imie"]?.ToString())
                            ? "<brak danych pacjenta>"
                            : $"{reader["Lx_Nazwisko"]?.ToString()} {reader["Lx_Imie"]?.ToString()} - " +
                              $"{reader["Lx_Firma"]?.ToString() ?? "brak firmy"}",

                        IsSelected = false
                    };

                    result.Add(record);
                }

                // System.Diagnostics.Debug.WriteLine($"GetArchiveListRecords: ✅ Znaleziono {result.Count} rekordów z archiwum");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"GetArchiveListRecords ERROR: {ex.Message}");
                throw new Exception($"Błąd pobierania archiwum: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Wstawia badanie z rekordu archiwum i zwraca nowy Bad_ID
        /// </summary>
        public int InsertBadanieFromArchive(ArchiveListRecord record)
        {
            int newId = 0;
            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();

                // Walidacja ID pacjenta
                int? validatedPacjentId = ValidatePacjentId(record, conn);

                if (!validatedPacjentId.HasValue)
                {
                    // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: ❌ BRAK WALIDACJI dla archiwum #{record.Identyfikator}");
                    return 0;
                }

                // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: ✅ Znaleziono P_ID={validatedPacjentId.Value}");

                // Walidacja ID skierowania lub utworzenie nowego
                int? validatedSkierowanieId = ValidateSkierowanieId(record, conn, validatedPacjentId.Value);

                // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: ValidateSkierowanieId zwróciło: {validatedSkierowanieId?.ToString() ?? "NULL"}");

                // Jeśli brak skierowania, utwórz nowe
                if (!validatedSkierowanieId.HasValue)
                {
                    // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: 🔸 Brak istniejącego skierowania, wywołuję CreateSkierowanieFromArchive...");
                    // System.Diagnostics.Debug.WriteLine($"  - record.Lx_ID_Skierowania = {record.Lx_ID_Skierowania}");

                    validatedSkierowanieId = CreateSkierowanieFromArchive(record, conn, validatedPacjentId.Value);

                    if (validatedSkierowanieId.HasValue)
                    {
                        // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: ✅ CreateSkierowanieFromArchive zwróciło B_ID={validatedSkierowanieId.Value}");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: ⚠️ CreateSkierowanieFromArchive zwróciło NULL!");
                    }
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: ℹ️ Używam istniejącego skierowania B_ID={validatedSkierowanieId.Value}");
                }

                // INSERT do tabeli Badanie
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO Badanie (
                    Bad_S_ID, Bad_P_ID, Bad_Cena1, Bad_Cena2, Bad_Cena3, Bad_Cena4, 
                    Bad_Cena5, Bad_Cena6, Bad_Cena7, Bad_Cena8, Bad_Cena9, 
                    Bad_Razem, Bad_Data, Bad_Data_Do
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                cmd.Parameters.AddWithValue("@p1", validatedSkierowanieId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p2", validatedPacjentId.Value);
                cmd.Parameters.AddWithValue("@p3", record.Lx_Cena1 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p4", record.Lx_Cena2 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p5", record.Lx_Cena3 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p6", record.Lx_Cena4 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p7", record.Lx_Cena5 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p8", record.Lx_Cena6 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p9", record.Lx_Cena7 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p10", 0m);
                cmd.Parameters.AddWithValue("@p11", record.Lx_Cena9 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p12", record.Lx_Razem ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p13", record.Lx_Data ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p14", record.Lx_Data ?? (object)DBNull.Value);

                // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: 🔹 Wykonywanie INSERT do Badanie...");
                cmd.ExecuteNonQuery();

                using var idCmd = conn.CreateCommand();
                idCmd.CommandText = "SELECT @@IDENTITY";
                var obj = idCmd.ExecuteScalar();
                if (obj != null && int.TryParse(obj.ToString(), out var id))
                    newId = id;

                // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: ✅ Dodano Bad_ID={newId} (Bad_S_ID={validatedSkierowanieId?.ToString() ?? "NULL"})");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive ERROR: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"  StackTrace: {ex.StackTrace}");
            }
            return newId;
        }

        /// <summary>
        /// GŁÓWNA METODA IMPORTU - wykonuje pełny import rekordu z archiwum
        /// Zwraca ID utworzonego badania lub 0 w przypadku błędu
        /// </summary>
        public int ImportArchiveRecord(ArchiveListRecord record)
        {
            // System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════════");
            // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord: START dla archiwum #{record.Identyfikator}");

            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();

                // ═════════════════════════════════════════════════════════
                // KROK 1: Walidacja/Utworzenie PACJENTA
                // ═════════════════════════════════════════════════════════
                int? pacjentId = ValidatePacjentId(record, conn);
                if (!pacjentId.HasValue)
                {
                    // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord: ❌ Brak pacjenta dla archiwum #{record.Identyfikator}");
                    return 0;
                }
                // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord: ✅ Pacjent P_ID={pacjentId.Value}");

                // ═════════════════════════════════════════════════════════
                // KROK 2: Walidacja/Utworzenie SKIEROWANIA
                // ═════════════════════════════════════════════════════════
                int? skierowanieId = ValidateOrCreateSkierowanie(record, conn, pacjentId.Value);
                if (!skierowanieId.HasValue)
                {
                    // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord: ❌ Nie udało się utworzyć skierowania");
                    return 0;
                }
                // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord: ✅ Skierowanie B_ID={skierowanieId.Value}");

                // ═════════════════════════════════════════════════════════
                // KROK 3: Pobranie CENNIKA z Firma
                // ═════════════════════════════════════════════════════════
                string? cennik = GetFirmaCennik(record.Lx_ID_Firma, conn);
                // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord: ℹ️ Cennik firmy: '{cennik ?? "BRAK"}'");

                // ═════════════════════════════════════════════════════════
                // KROK 4: Utworzenie BADANIA
                // ═════════════════════════════════════════════════════════
                int badanieId = CreateBadanieFromArchive(record, conn, pacjentId.Value, skierowanieId.Value, cennik);
                if (badanieId == 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord: ❌ Nie udało się utworzyć badania");
                    return 0;
                }
                // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord: ✅ Badanie Bad_ID={badanieId}");

                // ═════════════════════════════════════════════════════════
                // KROK 5: Aktualizacja B_Badanie_ID w skierowaniu
                // ═════════════════════════════════════════════════════════
                UpdateSkierowanieBadanieId(skierowanieId.Value, badanieId, conn);

                // ═════════════════════════════════════════════════════════
                // KROK 6: Aktualizacja Lx_ID_Badania w archiwum
                // ═════════════════════════════════════════════════════════
                this.UpdateArchiveBadanieId(record.Identyfikator, badanieId);

                // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord: ✅ SUKCES! Bad_ID={badanieId}");
                // System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════════");
                return badanieId;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ImportArchiveRecord ERROR: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"  StackTrace: {ex.StackTrace}");
                // System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════════════════");
                return 0;
            }
        }

        /// <summary>
        /// Waliduje lub tworzy skierowanie (KROK 2)
        /// </summary>
        private int? ValidateOrCreateSkierowanie(ArchiveListRecord record, OdbcConnection conn, int pacjentId)
        {
            // Najpierw sprawdź czy istnieje skierowanie z archiwum
            int? existingId = ValidateSkierowanieId(record, conn, pacjentId);

            if (existingId.HasValue)
            {
                // System.Diagnostics.Debug.WriteLine($"ValidateOrCreateSkierowanie: ✅ Użycie istniejącego B_ID={existingId.Value}");
                return existingId.Value;
            }

            // Brak - utwórz nowe
            // System.Diagnostics.Debug.WriteLine($"ValidateOrCreateSkierowanie: 🔸 Tworzenie nowego skierowania...");
            return CreateSkierowanieFromArchive(record, conn, pacjentId);
        }

        /// <summary>
        /// Tworzy rekord Badanie z pełnymi danymi (KROK 4)
        /// </summary>
        private int CreateBadanieFromArchive(ArchiveListRecord record, OdbcConnection conn, int pacjentId, int skierowanieId, string? cennik)
        {
            // System.Diagnostics.Debug.WriteLine($"CreateBadanieFromArchive: START");

            try
            {
                using var cmd = conn.CreateCommand();

                // ✅ KOMPLETNY INSERT z wszystkimi polami
                cmd.CommandText = @"INSERT INTO Badanie (
                    Bad_S_ID, 
                    Bad_P_ID, 
                    Bad_bn_cennik,
                    Bad_Typ,
                    Bad_Data, 
                    Bad_Data_Do,
                    Bad_Cena1, 
                    Bad_Cena2, 
                    Bad_Cena3, 
                    Bad_Cena4, 
                    Bad_Cena5, 
                    Bad_Cena6, 
                    Bad_Cena7, 
                    Bad_Cena8, 
                    Bad_Cena9,
                    Bad_Cena10,
                    Bad_Razem
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                cmd.Parameters.AddWithValue("@p1", skierowanieId);
                cmd.Parameters.AddWithValue("@p2", pacjentId);
                cmd.Parameters.AddWithValue("@p3", cennik ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p4", "F"); // Bad_Typ = "F" (z faktury/archiwum)
                cmd.Parameters.AddWithValue("@p5", record.Lx_Data ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p6", record.Lx_Data ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p7", record.Lx_Cena1 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p8", record.Lx_Cena2 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p9", record.Lx_Cena3 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p10", record.Lx_Cena4 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p11", record.Lx_Cena5 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p12", record.Lx_Cena6 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p13", record.Lx_Cena7 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p14", 0m); // Bad_Cena8
                cmd.Parameters.AddWithValue("@p15", record.Lx_Cena9 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@p16", 0m); // Bad_Cena10
                cmd.Parameters.AddWithValue("@p17", record.Lx_Razem ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();

                using var idCmd = conn.CreateCommand();
                idCmd.CommandText = "SELECT @@IDENTITY";
                var obj = idCmd.ExecuteScalar();

                if (obj != null && int.TryParse(obj.ToString(), out var newId))
                {
                    // System.Diagnostics.Debug.WriteLine($"CreateBadanieFromArchive: ✅ Utworzono Bad_ID={newId}");
                    return newId;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CreateBadanieFromArchive ERROR: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Pobiera cennik firmy
        /// </summary>
        private string? GetFirmaCennik(int? firmaId, OdbcConnection conn)
        {
            if (!firmaId.HasValue || firmaId.Value == 0)
                return null;

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Cennik FROM Firma WHERE id = ?";
                cmd.Parameters.AddWithValue("@p1", firmaId.Value);

                var obj = cmd.ExecuteScalar();
                return obj != DBNull.Value ? obj?.ToString() : null;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"GetFirmaCennik ERROR: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Aktualizuje B_Badanie_ID w skierowaniu
        /// </summary>
        private bool UpdateSkierowanieBadanieId(int skierowanieId, int badanieId, OdbcConnection conn)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE B_Skierowania SET B_Badanie_ID = ? WHERE B_ID = ?";
                cmd.Parameters.AddWithValue("@p1", badanieId);
                cmd.Parameters.AddWithValue("@p2", skierowanieId);

                int rows = cmd.ExecuteNonQuery();
                string status = rows > 0 ? "✅" : "⚠️";
                // System.Diagnostics.Debug.WriteLine($"UpdateSkierowanieBadanieId: {status} B_ID={skierowanieId} -> B_Badanie_ID={badanieId}");
                return rows > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateSkierowanieBadanieId ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tworzy nowe skierowanie z rekordu archiwum
        /// </summary>
        private int? CreateSkierowanieFromArchive(ArchiveListRecord record, OdbcConnection conn, int pacjentId)
        {
            // System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
            // System.Diagnostics.Debug.WriteLine($"CreateSkierowanieFromArchive: ▶️ START dla P_ID={pacjentId}");
            // System.Diagnostics.Debug.WriteLine($"  - Record.Identyfikator: {record.Identyfikator}");
            // System.Diagnostics.Debug.WriteLine($"  - Record.Lx_Data: {record.Lx_Data}");
            // System.Diagnostics.Debug.WriteLine($"  - Record.Lx_Imie: '{record.Lx_Imie}'");
            // System.Diagnostics.Debug.WriteLine($"  - Record.Lx_Nazwisko: '{record.Lx_Nazwisko}'");

            try
            {
                // System.Diagnostics.Debug.WriteLine("CreateSkierowanieFromArchive: 🔹 Tworzenie komendy SQL...");

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"INSERT INTO B_Skierowania (
                    B_Pacjent_ID, 
                    B_ID_pacjenta, 
                    B_DataSkierowania, 
                    B_RegistrationDate, 
                    B_Activ, 
                    B_Nowe, 
                    B_TypBadania, 
                    B_Scan
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                // System.Diagnostics.Debug.WriteLine($"CreateSkierowanieFromArchive: 🔹 SQL Command: {cmd.CommandText}");
                // System.Diagnostics.Debug.WriteLine("CreateSkierowanieFromArchive: 🔹 Dodawanie parametrów...");

                cmd.Parameters.AddWithValue("@p1", pacjentId);
                // System.Diagnostics.Debug.WriteLine($"  ✓ @p1 (B_Pacjent_ID) = {pacjentId}");

                cmd.Parameters.AddWithValue("@p2", pacjentId);
                // System.Diagnostics.Debug.WriteLine($"  ✓ @p2 (B_ID_pacjenta) = {pacjentId}");

                cmd.Parameters.AddWithValue("@p3", record.Lx_Data ?? (object)DBNull.Value);
                // System.Diagnostics.Debug.WriteLine($"  ✓ @p3 (B_DataSkierowania) = {record.Lx_Data}");

                cmd.Parameters.AddWithValue("@p4", record.Lx_Data ?? (object)DBNull.Value);
                // System.Diagnostics.Debug.WriteLine($"  ✓ @p4 (B_RegistrationDate) = {record.Lx_Data}");

                cmd.Parameters.AddWithValue("@p5", true);
                // System.Diagnostics.Debug.WriteLine("  ✓ @p5 (B_Activ) = True");

                cmd.Parameters.AddWithValue("@p6", true);
                // System.Diagnostics.Debug.WriteLine("  ✓ @p6 (B_Nowe) = True");

                cmd.Parameters.AddWithValue("@p7", "F");
                // System.Diagnostics.Debug.WriteLine("  ✓ @p7 (B_TypBadania) = 'F'");

                cmd.Parameters.AddWithValue("@p8", true);
                // System.Diagnostics.Debug.WriteLine("  ✓ @p8 (B_Scan) = True");

                // System.Diagnostics.Debug.WriteLine("CreateSkierowanieFromArchive: 🔹 Wykonywanie INSERT...");
                int affectedRows = cmd.ExecuteNonQuery();
                // System.Diagnostics.Debug.WriteLine($"CreateSkierowanieFromArchive: ✅ INSERT wykonany! Affected rows = {affectedRows}");

                // System.Diagnostics.Debug.WriteLine("CreateSkierowanieFromArchive: 🔹 Pobieranie nowego ID (SELECT @@IDENTITY)...");
                using var idCmd = conn.CreateCommand();
                idCmd.CommandText = "SELECT @@IDENTITY";
                var obj = idCmd.ExecuteScalar();

                // System.Diagnostics.Debug.WriteLine($"CreateSkierowanieFromArchive: 🔹 @@IDENTITY zwróciło: {obj} (typ: {obj?.GetType().Name ?? "null"})");

                if (obj != null && int.TryParse(obj.ToString(), out var newSkierowanieId))
                {
                    // System.Diagnostics.Debug.WriteLine($"CreateSkierowanieFromArchive: ✅ SUCCESS! Utworzono B_ID={newSkierowanieId} dla P_ID={pacjentId}");
                    // System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
                    return newSkierowanieId;
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("CreateSkierowanieFromArchive: ⚠️ @@IDENTITY nie zwróciło poprawnego ID!");
                    // System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"CreateSkierowanieFromArchive: ❌ EXCEPTION!");
                // System.Diagnostics.Debug.WriteLine($"  Message: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"  StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    // System.Diagnostics.Debug.WriteLine($"  InnerException: {ex.InnerException.Message}");
                }

                // System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════════════");
            }

            return null;
        }

        /// <summary>
        /// Hierarchiczna walidacja ID pacjenta (4 poziomy)
        /// </summary>
        private int? ValidatePacjentId(ArchiveListRecord record, OdbcConnection conn)
        {
            // POZIOM 1: Sprawdź bezpośrednie ID
            if (record.Lx_ID_pacjent.HasValue && record.Lx_ID_pacjent.Value > 0)
            {
                try
                {
                    using var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = "SELECT P_ID FROM P_Pacjent WHERE P_ID = ? AND P_activ = True";
                    checkCmd.Parameters.AddWithValue("@p1", record.Lx_ID_pacjent.Value);

                    var obj = checkCmd.ExecuteScalar();
                    if (obj != null && int.TryParse(obj.ToString(), out var existingId))
                    {
                        // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ✅ POZIOM 1 - ID={existingId}");
                        return existingId;
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: POZIOM 1 ERROR - {ex.Message}");
                }
            }

            // POZIOM 2: Imię + Nazwisko + ID Firmy
            if (!string.IsNullOrWhiteSpace(record.Lx_Imie) &&
                !string.IsNullOrWhiteSpace(record.Lx_Nazwisko) &&
                record.Lx_ID_Firma.HasValue)
            {
                try
                {
                    using var searchCmd = conn.CreateCommand();
                    searchCmd.CommandText = @"
                        SELECT TOP 1 P_ID 
                        FROM P_Pacjent 
                        WHERE TRIM(UCASE(P_imie)) = TRIM(UCASE(?)) 
                          AND TRIM(UCASE(P_nazwisko)) = TRIM(UCASE(?))
                          AND P_Firma_id = ?
                          AND P_activ = True
                        ORDER BY P_ID DESC";

                    searchCmd.Parameters.AddWithValue("@p1", record.Lx_Imie);
                    searchCmd.Parameters.AddWithValue("@p2", record.Lx_Nazwisko);
                    searchCmd.Parameters.AddWithValue("@p3", record.Lx_ID_Firma.Value);

                    var obj = searchCmd.ExecuteScalar();
                    if (obj != null && int.TryParse(obj.ToString(), out var foundId))
                    {
                        // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ✅ POZIOM 2 - ID={foundId}");
                        return foundId;
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: POZIOM 2 ERROR - {ex.Message}");
                }
            }

            // POZIOM 3: Imię + Nazwisko
            if (!string.IsNullOrWhiteSpace(record.Lx_Imie) &&
                !string.IsNullOrWhiteSpace(record.Lx_Nazwisko))
            {
                try
                {
                    using var searchCmd = conn.CreateCommand();
                    searchCmd.CommandText = @"
                        SELECT TOP 1 P_ID 
                        FROM P_Pacjent 
                        WHERE TRIM(UCASE(P_imie)) = TRIM(UCASE(?)) 
                          AND TRIM(UCASE(P_nazwisko)) = TRIM(UCASE(?))
                          AND P_activ = True
                        ORDER BY P_ID DESC";

                    searchCmd.Parameters.AddWithValue("@p1", record.Lx_Imie);
                    searchCmd.Parameters.AddWithValue("@p2", record.Lx_Nazwisko);

                    var obj = searchCmd.ExecuteScalar();
                    if (obj != null && int.TryParse(obj.ToString(), out var foundId))
                    {
                        // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ✅ POZIOM 3 - ID={foundId}");
                        return foundId;
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: POZIOM 3 ERROR - {ex.Message}");
                }
            }

            // POZIOM 4: Nazwisko + ID Firmy
            if (!string.IsNullOrWhiteSpace(record.Lx_Nazwisko) && record.Lx_ID_Firma.HasValue)
            {
                try
                {
                    using var searchCmd = conn.CreateCommand();
                    searchCmd.CommandText = @"
                        SELECT TOP 1 P_ID 
                        FROM P_Pacjent 
                        WHERE TRIM(UCASE(P_nazwisko)) = TRIM(UCASE(?))
                          AND P_Firma_id = ?
                          AND P_activ = True
                        ORDER BY P_ID DESC";

                    searchCmd.Parameters.AddWithValue("@p1", record.Lx_Nazwisko);
                    searchCmd.Parameters.AddWithValue("@p2", record.Lx_ID_Firma.Value);

                    var obj = searchCmd.ExecuteScalar();
                    if (obj != null && int.TryParse(obj.ToString(), out var foundId))
                    {
                        // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ⚠️ POZIOM 4 - ID={foundId}");
                        return foundId;
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: POZIOM 4 ERROR - {ex.Message}");
                }
            }

            // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ❌ BRAK DOPASOWANIA");
            return null;
        }

        /// <summary>
        /// Waliduje ID skierowania
        /// </summary>
        private int? ValidateSkierowanieId(ArchiveListRecord record, OdbcConnection conn, int pacjentId)
        {
            if (!record.Lx_ID_Skierowania.HasValue)
                return null;

            try
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT B_ID FROM B_Skierowania WHERE B_ID = ? AND B_Pacjent_ID = ?";
                checkCmd.Parameters.AddWithValue("@p1", record.Lx_ID_Skierowania.Value);
                checkCmd.Parameters.AddWithValue("@p2", pacjentId);

                var obj = checkCmd.ExecuteScalar();
                if (obj != null && int.TryParse(obj.ToString(), out var validId))
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidateSkierowanieId: ✅ ID={validId}");
                    return validId;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ValidateSkierowanieId ERROR: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Sprawdza czy lista już istnieje dla danej faktury
        /// Zwraca ID listy jeśli istnieje, null jeśli nie
        /// </summary>
        public int? CheckIfListaExists(int fakturaId)
        {
            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT TOP 1 Identyfikator FROM ListyBadan WHERE L_FK_ID = ?";
                cmd.Parameters.AddWithValue("@p1", fakturaId);

                var obj = cmd.ExecuteScalar();
                if (obj != null && int.TryParse(obj.ToString(), out var listaId))
                {
                    // System.Diagnostics.Debug.WriteLine($"CheckIfListaExists: ⚠️ Lista L_ID={listaId} już istnieje dla FK_ID={fakturaId}");
                    return listaId;
                }

                return null;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CheckIfListaExists ERROR: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Aktualizuje Bad_L_ID, Bad_F_ID i Bad_Fakt w tabeli Badanie
        /// </summary>
        public bool UpdateBadanieWithListaFakturaAndNumer(int badId, int listaId, int fakturaId, string numerFaktury)
        {
            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE Badanie SET Bad_L_ID = ?, Bad_F_ID = ?, Bad_Fakt = ? WHERE Bad_ID = ?";
                cmd.Parameters.AddWithValue("@p1", listaId);
                cmd.Parameters.AddWithValue("@p2", fakturaId);
                cmd.Parameters.AddWithValue("@p3", string.IsNullOrWhiteSpace(numerFaktury) ? (object)DBNull.Value : numerFaktury);
                cmd.Parameters.AddWithValue("@p4", badId);

                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"UpdateBadanieWithListaFakturaAndNumer: ✅ Bad_ID={badId} -> L_ID={listaId}, F_ID={fakturaId}, Fakt='{numerFaktury}'");
                }
                return rows > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateBadanieWithListaFakturaAndNumer ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aktualizuje pole Lx_ID_Badania w tabeli archiwum Lx_Listy_do_faktur--old-baza
        /// </summary>
        public bool UpdateArchiveBadanieId(int identyfikator, int badanieId)
        {
            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE [Lx_Listy_do_faktur--old-baza] SET Lx_ID_Badania = ? WHERE Identyfikator = ?";
                cmd.Parameters.AddWithValue("@p1", badanieId);
                cmd.Parameters.AddWithValue("@p2", identyfikator);

                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"UpdateArchiveBadanieId: ✅ Zaktualizowano Lx_ID_Badania={badanieId} dla Identyfikator={identyfikator}");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"UpdateArchiveBadanieId: ⚠️ Nie znaleziono rekordu do aktualizacji dla Identyfikator={identyfikator}");
                }
                return rows > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateArchiveBadanieId ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tworzy nową listę badań i zwraca jej ID
        /// </summary>
        public int CreateListaBadan(int fakturaId, int? firmaId, DateTime? data)
        {
            int newId = 0;
            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO ListyBadan (L_FK_ID, L_Firma_ID, L_Data) VALUES (?, ?, ?)";
                cmd.Parameters.AddWithValue("@p1", fakturaId);
                cmd.Parameters.AddWithValue("@p2", firmaId.HasValue ? (object)firmaId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@p3", data.HasValue ? (object)data.Value : DBNull.Value);
                cmd.ExecuteNonQuery();

                using var idCmd = conn.CreateCommand();
                idCmd.CommandText = "SELECT @@IDENTITY";
                var obj = idCmd.ExecuteScalar();
                if (obj != null && int.TryParse(obj.ToString(), out var id))
                    newId = id;

                // System.Diagnostics.Debug.WriteLine($"CreateListaBadan: ✅ Utworzono L_ID={newId} dla FK_ID={fakturaId}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CreateListaBadan ERROR: {ex.Message}");
            }
            return newId;
        }

        /// <summary>
        /// Aktualizuje Lx_ID_listy w archiwum dla wszystkich rekordów danej faktury
        /// </summary>
        public bool UpdateArchiveWithListaId(int fakturaId, int listaId)
        {
            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE [Lx_Listy_do_faktur--old-baza] SET Lx_ID_listy = ? WHERE Lx_ID_Faktura = ?";
                cmd.Parameters.AddWithValue("@p1", listaId);
                cmd.Parameters.AddWithValue("@p2", fakturaId);

                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"UpdateArchiveWithListaId: ✅ Zaktualizowano {rows} rekordów archiwum (FK_ID={fakturaId} -> L_ID={listaId})");
                }
                return rows > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateArchiveWithListaId ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aktualizuje fakturę: FK_Num_Listy, FK_Suma_Bad i FK_Status
        /// </summary>
        public bool UpdateFakturaWithListaSummary(int fakturaId, int listaId, decimal suma)
        {
            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Faktura SET FK_Num_Listy = ?, FK_Suma_Bad = ?, FK_Status = ? WHERE FK_ID = ?";
                cmd.Parameters.AddWithValue("@p1", listaId);
                cmd.Parameters.AddWithValue("@p2", suma);
                cmd.Parameters.AddWithValue("@p3", 2); // Status = 2 (Lista utworzona)
                cmd.Parameters.AddWithValue("@p4", fakturaId);

                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"UpdateFakturaWithListaSummary: ✅ Zaktualizowano FK_ID={fakturaId} -> NumListy={listaId}, Suma={suma:N2}");
                }
                return rows > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateFakturaWithListaSummary ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Oznacza rekordy w archiwum jako przetworzone (Lx_End = True)
        /// </summary>
        public bool MarkArchiveRecordsAsProcessed(List<int> identifiers)
        {
            if (identifiers == null || identifiers.Count == 0)
                return false;

            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();

                int updated = 0;
                foreach (var id in identifiers)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE [Lx_Listy_do_faktur--old-baza] SET Lx_End = ? WHERE Identyfikator = ?";
                    cmd.Parameters.AddWithValue("@p1", true);
                    cmd.Parameters.AddWithValue("@p2", id);
                    updated += cmd.ExecuteNonQuery();
                }

                // System.Diagnostics.Debug.WriteLine($"MarkArchiveRecordsAsProcessed: ✅ Oznaczono {updated}/{identifiers.Count} rekordów jako przetworzone");
                return updated > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"MarkArchiveRecordsAsProcessed ERROR: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper methods
        /// </summary>
        private static decimal? ParseDecimal(object obj)
        {
            if (obj == null || obj == DBNull.Value) return null;
            if (decimal.TryParse(obj.ToString(), out var d)) return d;
            return null;
        }

        private static int? ParseInt(object obj)
        {
            if (obj == null || obj == DBNull.Value) return null;
            if (int.TryParse(obj.ToString(), out var i)) return i;
            return null;
        }
    }
}
