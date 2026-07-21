using System.Data.Odbc;
using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using ASMED.WPF.ViewModels.Skierowania;
using System.Runtime.CompilerServices;
using ASMED.WPF.ViewModels.lista_do_faktur; // <-- dodane

namespace ASMED.WPF.Helpers
{
    public class AccessDbContext
    {
        public AccessDbContext() { }

        /// <summary>
        /// Aktualizuje pole FK_Saldo dla podanego rekordu Faktura (FK_ID).
        /// </summary>
        public bool UpdateFakturaSaldo(int fkId, decimal saldo)
        {
            try
            {
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Faktura SET FK_Saldo = ? WHERE FK_ID = ?";
                var p1 = cmd.CreateParameter(); p1.Value = saldo; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = fkId; cmd.Parameters.Add(p2);
                var rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateFakturaSaldo error: {ex}");
                return false;
            }
        }


        /// <summary>
        /// Znajduje firmę po dokładnej nazwie (trim) i zwraca Id oraz pole Cennik (może być null).
        /// </summary>
        public (int? Id, string? Cennik) GetFirmaIdAndCennikByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return (null, null);
            try
            {
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, cennik FROM Firma WHERE TRIM(Nazwa) = TRIM(?)";
                var p = cmd.CreateParameter(); p.Value = name; cmd.Parameters.Add(p);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int? id = null;
                    if (reader["id"] != DBNull.Value && int.TryParse(reader["id"].ToString(), out var tmp)) id = tmp;
                    var cennik = reader["cennik"] != DBNull.Value ? reader["cennik"].ToString() : null;
                    return (id, cennik);
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"GetFirmaIdAndCennikByName error: {ex}");
            }
            return (null, null);
        }

        /// <summary>
        /// Szuka istniejącej faktury po FK_Firma_ID i FK_Numer (porównanie trimowane)
        /// Zwraca FK_ID jeśli znaleziono, w przeciwnym razie null.
        /// </summary>
        public int? FindFakturaByFirmaAndNumer(int firmaId, string numer)
        {
            try
            {
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT FK_ID FROM Faktura WHERE FK_Firma_ID = ? AND TRIM(FK_Numer) = TRIM(?)";
                var p1 = cmd.CreateParameter(); p1.Value = firmaId; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = numer ?? string.Empty; cmd.Parameters.Add(p2);
                var obj = cmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var id)) return id;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"FindFakturaByFirmaAndNumer error: {ex}");
            }
            return null;
        }

        /// <summary>
        /// Aktualizuje pola faktury (proste pola używane przy imporcie).
        /// </summary>
        public bool UpdateFakturaFields(int fkId, int? firmaId, string? numer, DateTime? data, decimal? kwota, string? cennik)
        {
            try
            {
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Faktura SET FK_Firma_ID = ?, FK_Numer = ?, FK_Data = ?, FK_Kwota = ?, FK_Cennik = ? WHERE FK_ID = ?";
                var p1 = cmd.CreateParameter(); p1.Value = firmaId.HasValue ? (object)firmaId.Value : DBNull.Value; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = string.IsNullOrWhiteSpace(numer) ? (object)DBNull.Value : numer; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = data.HasValue ? (object)data.Value : DBNull.Value; cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.Value = kwota.HasValue ? (object)kwota.Value : DBNull.Value; cmd.Parameters.Add(p4);
                var p5 = cmd.CreateParameter(); p5.Value = string.IsNullOrWhiteSpace(cennik) ? (object)DBNull.Value : cennik; cmd.Parameters.Add(p5);
                var p6 = cmd.CreateParameter(); p6.Value = fkId; cmd.Parameters.Add(p6);
                var rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateFakturaFields error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Wstawia prosty rekord Faktura i zwraca nowo utworzony FK_ID.
        /// </summary>
        public int InsertFakturaSimple(int? firmaId, string? numer, DateTime? data, decimal? kwota, string? cennik)
        {
            int newId = 0;
            try
            {
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Faktura (FK_Firma_ID, FK_Numer, FK_Data, FK_Kwota, FK_Cennik) VALUES (?, ?, ?, ?, ?)";
                var p1 = cmd.CreateParameter(); p1.Value = firmaId.HasValue ? (object)firmaId.Value : DBNull.Value; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = string.IsNullOrWhiteSpace(numer) ? (object)DBNull.Value : numer; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = data.HasValue ? (object)data.Value : DBNull.Value; cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.Value = kwota.HasValue ? (object)kwota.Value : DBNull.Value; cmd.Parameters.Add(p4);
                var p5 = cmd.CreateParameter(); p5.Value = string.IsNullOrWhiteSpace(cennik) ? (object)DBNull.Value : cennik; cmd.Parameters.Add(p5);

                cmd.ExecuteNonQuery();

                using var idCmd = conn.CreateCommand();
                idCmd.CommandText = "SELECT @@IDENTITY";
                var obj = idCmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var id)) newId = id;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"InsertFakturaSimple error: {ex}");
            }
            return newId;
        }
        // Dodano metodę AddFaktura na końcu klasy AccessDbContext (przed zamknięciem klasy)
        public int AddFaktura(int? firmaId, string numer, DateTime? data, decimal? kwota, int status, string? pdfPath, decimal? badSuma, string? fkNumListy = null)
        {
            int newId = 0;
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();

                // Zakładamy kolumny zgodne z dotychczasowym schematem:
                // FK_Firma_ID, FK_Numer, FK_Data, FK_Kwota, FK_Status, FK_PDF, FK_Num_Listy, FK_Suma_Bad, FK_Saldo
                cmd.CommandText = @"INSERT INTO Faktura 
            (FK_Firma_ID, FK_Numer, FK_Data, FK_Kwota, FK_Status, FK_PDF, FK_Num_Listy, FK_Suma_Bad, FK_Saldo)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                var p = cmd.CreateParameter(); p.ParameterName = "@p1"; p.Value = firmaId.HasValue ? (object)firmaId.Value : DBNull.Value; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@p2"; p.Value = string.IsNullOrWhiteSpace(numer) ? (object)DBNull.Value : numer; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@p3"; p.Value = data.HasValue ? (object)data.Value : DBNull.Value; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@p4"; p.Value = kwota.HasValue ? (object)kwota.Value : DBNull.Value; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@p5"; p.Value = status; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@p6"; p.Value = string.IsNullOrWhiteSpace(pdfPath) ? (object)DBNull.Value : pdfPath; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@p7"; p.Value = string.IsNullOrWhiteSpace(fkNumListy) ? (object)DBNull.Value : fkNumListy; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@p8"; p.Value = badSuma.HasValue ? (object)badSuma.Value : 0m; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@p9"; p.Value = 0m; cmd.Parameters.Add(p);

                cmd.ExecuteNonQuery();

                using var idCmd = connection.CreateCommand();
                idCmd.CommandText = "SELECT @@IDENTITY";
                var result = idCmd.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out var id))
                    newId = id;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu faktury do bazy:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return newId;
        }

        // ---- DODANE METODY POMOCNICZE DLA USUWANIA LISTY ----

        /// <summary>
        /// Zwraca FK_ID (identyfikator faktury) powiązanej z listą (pole L_FK_ID w ListyBadan).
        /// </summary>
        public int? GetFakturaIdForList(int listId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT L_FK_ID FROM ListyBadan WHERE Identyfikator = ?";
                var p = cmd.CreateParameter(); p.Value = listId; cmd.Parameters.Add(p);
                var obj = cmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var id)) return id;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"GetFakturaIdForList error: {ex}");
            }
            return null;
        }

        /// <summary>
        /// Ustawia FK_Num_Listy = 0 w tabeli Faktura dla podanego FK_ID.
        /// </summary>
        public bool ClearFakturaNumListByFakturaId(int fkId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                // ustawiamy numer listy na 0 oraz zerujemy sumę badań i saldo
                cmd.CommandText = "UPDATE Faktura SET FK_Num_Listy = 0, FK_Suma_Bad = 0, FK_Saldo = 0 WHERE FK_ID = ?";
                var p = cmd.CreateParameter(); p.Value = fkId; cmd.Parameters.Add(p);
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ClearFakturaNumListByFakturaId error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Usuwa rekord z tabeli ListyBadan o danym Identyfikatorze.
        /// </summary>
        public bool DeleteListyBadan(int listId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM ListyBadan WHERE Identyfikator = ?";
                var p = cmd.CreateParameter(); p.Value = listId; cmd.Parameters.Add(p);
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"DeleteListyBadan error: {ex}");
                return false;
            }
        }

        // -----------------------------------------------------

        // DTO dla listy faktur (używane przez FakturaViewModel)
        public class FakturaDto
        {
            public int Id { get; set; }
            public string? Lista { get; set; }            // FK_Num_Listy
            public string? Numer_Faktury { get; set; }   // FK_Numer
            public DateTime? Data { get; set; }          // FK_Data
            public string? Firma { get; set; }           // Firma.Nazwa
            public string? NIP { get; set; }             // Firma.NIP
            public decimal? Kwota { get; set; }          // FK_Kwota
            public decimal? Kwota_B { get; set; }        // FK_Suma_Bad
            public decimal? Saldo { get; set; }          // FK_Saldo
            public string? Status { get; set; }          // FK_Status
            public string? PDF { get; set; }             // FK_PDF
            public object? Firma_ID { get; internal set; }
        }

        /// <summary>
        /// Pobiera listę faktur (join Firma + Faktura).
        /// SQL zgodny z zapytaniem przekazanym przez użytkownika.
        /// </summary>
        public List<FakturaDto> GetFaktury(int max = 800)
        {
            var result = new List<FakturaDto>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = $@"
SELECT TOP {max}
    Faktura.FK_ID,
    Firma.id,
    Firma.Nazwa,
    Firma.NIP,
    Faktura.FK_Numer,
    Faktura.FK_Data,
    Faktura.FK_Kwota,
    Faktura.FK_Suma_Bad,
    Faktura.FK_Saldo,
    Faktura.FK_Status,
    Faktura.FK_PDF,
    Faktura.FK_Num_Listy
FROM
    Firma
    INNER JOIN Faktura ON Firma.ID = Faktura.FK_Firma_ID
ORDER BY
    Faktura.FK_Data DESC;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    decimal? ParseDecimal(object obj)
                    {
                        if (obj == null || obj == DBNull.Value) return null;
                        if (decimal.TryParse(obj.ToString(), out var d)) return d;
                        try { return Convert.ToDecimal(obj); } catch { return null; }
                    }

                    var dto = new FakturaDto
                    {
                        Id = reader["FK_ID"] is int fkId ? fkId : (int.TryParse(reader["FK_ID"]?.ToString(), out var fkId2) ? fkId2 : 0),
                        Firma = reader["Nazwa"]?.ToString(),
                        NIP = reader["NIP"]?.ToString(),
                        Numer_Faktury = reader["FK_Numer"]?.ToString(),
                        Data = reader["FK_Data"] != DBNull.Value ? Convert.ToDateTime(reader["FK_Data"]) : (DateTime?)null,
                        Kwota = ParseDecimal(reader["FK_Kwota"]),
                        Kwota_B = ParseDecimal(reader["FK_Suma_Bad"]),
                        Saldo = ParseDecimal(reader["FK_Saldo"]),
                        Status = reader["FK_Status"]?.ToString(),
                        PDF = reader["FK_PDF"]?.ToString(),
                        Lista = reader["FK_Num_Listy"]?.ToString()
                    };

                    result.Add(dto);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania faktur z bazy:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        /// <summary>
        /// Pobiera email firmy na podstawie ID. Zwraca "info@adres.pl" jeśli brak emaila.
        /// </summary>
        public string GetFirmaEmailById(int? firmaId)
        {
            if (!firmaId.HasValue)
                return "info@adres.pl";

            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT FKemail FROM Firma WHERE id = ?";
                var p = cmd.CreateParameter();
                p.Value = firmaId.Value;
                cmd.Parameters.Add(p);

                var result = cmd.ExecuteScalar();
                var email = result?.ToString();

                return string.IsNullOrWhiteSpace(email) ? "info@adres.pl" : email;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"GetFirmaEmailById error: {ex}");
                return "info@adres.pl";
            }
        }

        public System.Collections.Generic.List<FirmaDto> GetFirmy(string query, int max = 800)
        {
            var list = new System.Collections.Generic.List<FirmaDto>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                var baseSql = $"SELECT TOP {max} id, activ, Nazwa,Cennik,FKemail, NIP FROM Firma WHERE activ = TRUE";

                if (string.IsNullOrWhiteSpace(query))
                {
                    cmd.CommandText = baseSql + " ORDER BY Nazwa";
                }
                else
                {
                    // Rozbij zapytanie na fragmenty (max 5), wymagamy aby każdy fragment występował (AND),
                    // ale fragment może wystąpić w Nazwa LUB w NIP (OR). Dzięki temu "szko" znajdzie "szkoła".
                    var parts = query
                        .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Take(5)
                        .ToArray();

                    if (parts.Length == 0)
                    {
                        cmd.CommandText = baseSql + " ORDER BY Nazwa";
                    }
                    else
                    {
                        var cond = new System.Text.StringBuilder();
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (i > 0) cond.Append(" AND ");
                            cond.Append("(Nazwa LIKE ? OR NIP LIKE ?)");
                        }

                        cmd.CommandText = baseSql + " AND (" + cond.ToString() + ") ORDER BY Nazwa";

                        // Dodaj parametry (po dwa na fragment: Nazwa i NIP), używamy '*' jako wildcard dla Access
                        foreach (var part in parts)
                        {
                            var pattern = "*" + (part ?? string.Empty) + "*";

                            var p1 = cmd.CreateParameter();
                            p1.OdbcType = System.Data.Odbc.OdbcType.VarChar;
                            p1.Size = Math.Max(255, pattern.Length);
                            p1.Value = pattern;
                            cmd.Parameters.Add(p1);

                            var p2 = cmd.CreateParameter();
                            p2.OdbcType = System.Data.Odbc.OdbcType.VarChar;
                            p2.Size = Math.Max(255, pattern.Length);
                            p2.Value = pattern;
                            cmd.Parameters.Add(p2);
                        }

                        try { cmd.CommandTimeout = 10; } catch { }
                    }
                }

                // Debug — Access-ready SQL do kopiowania
                try
                {
                    string displaySql = cmd.CommandText ?? "";
                    static string ReplaceFirst(string text, string search, string replace)
                    {
                        int pos = text.IndexOf(search, StringComparison.Ordinal);
                        if (pos < 0) return text;
                        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
                    }

                    for (int i = 0; i < cmd.Parameters.Count; i++)
                    {
                        var p = cmd.Parameters[i];
                        var raw = (p?.Value?.ToString() ?? "").Replace("'", "''");
                        var repl = "'*" + raw + "*'";
                        displaySql = ReplaceFirst(displaySql, "?", repl);
                    }

                    //System.Diagnostics.Debug.WriteLine("GetFirmy SQL: " + cmd.CommandText);
                    //System.Diagnostics.Debug.WriteLine("GetFirmy DEBUG_SQL: " + displaySql);
                    //System.Diagnostics.Debug.WriteLine("GetFirmy ConnectionString: " + (conn?.ConnectionString ?? "<null>"));
                }
                catch { /* ignore debug errors */ }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var f = new FirmaDto();
                    if (reader["id"] != DBNull.Value) f.Id = Convert.ToInt32(reader["id"]);
                    if (reader["activ"] != DBNull.Value)
                    {
                        try { f.Activ = Convert.ToBoolean(reader["activ"]); } catch { f.Activ = reader["activ"]?.ToString() == "True"; }
                    }
                    if (reader["Nazwa"] != DBNull.Value) f.Nazwa = reader["Nazwa"].ToString();
                    if (reader["NIP"] != DBNull.Value) f.NIP = reader["NIP"].ToString();
                    list.Add(f);
                }
            }
            catch (System.Data.Odbc.OdbcException)
            {
                //System.Diagnostics.Debug.WriteLine($"GetFirmy OdbcException: {ex.Message}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"GetFirmy error: {ex}");
            }
            return list;
        }
        //--
        // Dodaje pacjenta i zwraca nowo utworzony ID
        public int AddPatientAndGetId(string pesel, bool brakPesel, string plec, string imie, string nazwisko, string adresKod, string adresUlica, string adresMiasto, string zawod, int? firmaId, string kraj, DateTime? dataUrodzenia, string obywatelstwo, string telefon, string email, string firma)
        {
            int newId = 0;
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    using var cmd = new OdbcCommand(@"INSERT INTO P_Pacjent 
                        (P_pesel, P_brak, P_płeć, P_imie, P_nazwisko, P_Ades_kod, P_Adres_ulica_numer, P_Ades_miasto, P_zawód, P_Firma_id, P_Adres_kraj, P_data_urodzenia, P_obywatelstwo, P_telefon, P_email,P_firma, P_activ)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, True)", connection);

                    cmd.Parameters.AddWithValue("@pesel", pesel ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@brak", brakPesel);
                    cmd.Parameters.AddWithValue("@plec", plec ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@imie", imie ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@nazwisko", nazwisko ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@adresKod", adresKod ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@adresUlica", adresUlica ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@adresMiasto", adresMiasto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@zawod", zawod ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@firmaId", firmaId.HasValue ? (object)firmaId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@kraj", kraj ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@dataUrodzenia", dataUrodzenia.HasValue ? (object)dataUrodzenia.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@obywatelstwo", obywatelstwo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@telefon", telefon ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", email ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@firma", firma ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                    NotificationHelper.ShowPatientSaved();

                    using var idCmd = new OdbcCommand("SELECT @@IDENTITY", connection);
                    var result = idCmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int id))
                        newId = id;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu pacjenta do bazy:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return newId;
        }

        public void UpdatePatient(int pId, string pesel, bool brakPesel, string plec, string imie, string nazwisko, string adresKod, string adresUlica, string adresMiasto, string zawod, int? firmaId, string kraj, DateTime? dataUrodzenia, string obywatelstwo, string telefon, string email, string firma)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    using var cmd = new OdbcCommand(@"UPDATE P_Pacjent SET 
                        P_pesel = ?,
                        P_brak = ?,
                        P_płeć = ?,
                        P_imie = ?,
                        P_nazwisko = ?,
                        P_Ades_kod = ?,
                        P_Adres_ulica_numer = ?,
                        P_Ades_miasto = ?,
                        P_zawód = ?,
                        P_Firma_id = ?,
                        P_Adres_kraj = ?,
                        P_data_urodzenia = ?,
                        P_obywatelstwo = ?,
                        P_telefon = ?,
                        P_email = ?,
                        P_firma = ?,
                        P_activ = True
                    WHERE P_ID = ?", connection);

                    cmd.Parameters.AddWithValue("@pesel", pesel ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@brak", brakPesel);
                    cmd.Parameters.AddWithValue("@plec", plec ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@imie", imie ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@nazwisko", nazwisko ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@adresKod", adresKod ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@adresUlica", adresUlica ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@adresMiasto", adresMiasto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@zawod", zawod ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@firmaId", firmaId.HasValue ? (object)firmaId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@kraj", kraj ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@dataUrodzenia", dataUrodzenia.HasValue ? (object)dataUrodzenia.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@obywatelstwo", obywatelstwo ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@telefon", telefon ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", email ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@firma", firma ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", pId);

                    cmd.ExecuteNonQuery();
                }
                NotificationHelper.ShowPatientUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd aktualizacji pacjenta w bazie:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public List<SkierowanieDto> GetSkierowania()
        {
            var result = new List<SkierowanieDto>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    using var cmd = new OdbcCommand(@"SELECT
    B_Skierowania.B_ID,    
    B_Skierowania.B_Pacjent_ID,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_pesel,
    P_Pacjent.P_zawód,
    Firma.Nazwa,
    Firma.NIP,
    Firma.Cennik,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_książeczka,
    B_Skierowania.B_Zaswiadczenie,
    Badanie.Bad_Data,
    Rejestracja.R_Data,
    Faktura.FK_Numer,
    B_Skierowania.B_Activ
FROM
    Faktura
    RIGHT JOIN (
        Firma
        INNER JOIN (
            (
                (
                    P_Pacjent
                    INNER JOIN B_Skierowania ON P_Pacjent.P_ID = B_Skierowania.B_Pacjent_ID
                )
                LEFT JOIN Badanie ON B_Skierowania.B_Badanie_ID = Badanie.Bad_ID
            )
            LEFT JOIN Rejestracja ON B_Skierowania.B_ID = Rejestracja.R_S_ID
        ) ON Firma.id = P_Pacjent.P_Firma_id
    ) ON Faktura.FK_ID = Badanie.Bad_F_ID
WHERE
    (((B_Skierowania.B_Activ) = True))
ORDER BY
    B_Skierowania.B_ID DESC;", connection);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        result.Add(new SkierowanieDto
                        {
                            B_ID = reader["B_ID"] is int bid ? bid : int.TryParse(reader["B_ID"].ToString(), out var bid2) ? bid2 : 0,
                            B_Pacjent_ID = reader["B_Pacjent_ID"] is int id ? id : int.TryParse(reader["B_Pacjent_ID"].ToString(), out var id2) ? id2 : 0,
                            P_imie = reader["P_imie"]?.ToString(),
                            P_nazwisko = reader["P_nazwisko"]?.ToString(),
                            P_pesel = reader["P_pesel"]?.ToString(),
                            P_zawod = reader["P_zawód"]?.ToString(),
                            Nazwa = reader["Nazwa"]?.ToString(),
                            Firma_NIP = reader["NIP"]?.ToString(),
                            Firma_Cennik = reader["Cennik"]?.ToString(),
                            B_DataSkierowania = reader["B_DataSkierowania"] as DateTime? ?? (reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : (DateTime?)null),
                            B_TypBadania = reader["B_TypBadania"]?.ToString(),
                            B_książeczka_sanepid = reader["B_książeczka"] as bool? ?? (reader["B_książeczka"] != DBNull.Value ? Convert.ToBoolean(reader["B_książeczka"]) : (bool?)null),
                            B_Zaswiadczenie = reader["B_Zaswiadczenie"] as bool? ?? (reader["B_Zaswiadczenie"] != DBNull.Value ? Convert.ToBoolean(reader["B_Zaswiadczenie"]) : (bool?)null),
                            Bad_Data = reader["Bad_Data"] as DateTime? ?? (reader["Bad_Data"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data"]) : (DateTime?)null),
                            R_Data = reader["R_Data"] as DateTime? ?? (reader["R_Data"] != DBNull.Value ? Convert.ToDateTime(reader["R_Data"]) : (DateTime?)null),
                            FK_Numer = reader["FK_Numer"]?.ToString(),
                            B_Activ = reader["B_Activ"] as bool? ?? (reader["B_Activ"] != DBNull.Value ? Convert.ToBoolean(reader["B_Activ"]) : (bool?)null)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania skierowań z bazy:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        public int AddSkierowanie(SkierowanieRecord rec)
        {
            int newId = 0;
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    using var cmd = new OdbcCommand(@"INSERT INTO B_Skierowania (
        B_Pacjent_ID,
        B_Firma_ID,
        B_Badanie_ID,
        B_DataSkierowania,
        B_TypBadania,
        B_Stanowisko,
        B_RegistrationDate,
        B_czynnik_fizyczny,
        B_czynnik_fizyczny_opis,
                B_czynnik_pyłowy,
                B_czynnik_pyłowy_opis,
                B_czynnik_chemiczny,
                B_czynnik_chemiczny_opis,
                B_czynnik_biologiczny,
                B_czynnik_biologiczny_opis,
                B_czynnik_inny,
                B_czynnik_inny_opis,
                B_czynnik_sanepid,
                B_czynnik_sanepid_opis,
        B_Zaswiadczenie,
        B_książeczka,
        B_Ankieta,
        B_Nowe,
        B_Activ
    ) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", connection);
                    //  ) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", connection);

                    cmd.Parameters.AddWithValue("@B_Pacjent_ID", rec.PacjentId.HasValue ? (object)rec.PacjentId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_Firma_ID", rec.FirmaId.HasValue ? (object)rec.FirmaId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_Badanie_ID", rec.BadanieId.HasValue ? (object)rec.BadanieId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_DataSkierowania", rec.DataSkierowania.HasValue ? (object)rec.DataSkierowania.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_TypBadania", rec.TypBadania ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_Stanowisko", rec.Stanowisko ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_RegistrationDate", rec.RegistrationDate.HasValue ? (object)rec.RegistrationDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_czynnik_fizyczny", rec.CzynnikFizyczny);
                    cmd.Parameters.AddWithValue("@B_czynnik_fizyczny_opis", rec.CzynnikFizycznyOpis ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_czynnik_pyłowy", rec.CzynnikPylowy);
                    cmd.Parameters.AddWithValue("@B_czynnik_pyłowy_opis", rec.CzynnikPylowyOpis ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_czynnik_chemiczny", rec.CzynnikChemiczny);
                    cmd.Parameters.AddWithValue("@B_czynnik_chemiczny_opis", rec.CzynnikChemicznyOpis ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_czynnik_biologiczny", rec.CzynnikBiologiczny);
                    cmd.Parameters.AddWithValue("@B_czynnik_biologiczny_opis", rec.CzynnikBiologicznyOpis ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_czynnik_inny", rec.CzynnikInny);
                    cmd.Parameters.AddWithValue("@B_czynnik_inny_opis", rec.CzynnikInnyOpis ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_czynnik_sanepid", rec.CzynnikSanepid);
                    cmd.Parameters.AddWithValue("@B_czynnik_sanepid_opis", rec.CzynnikSanepidOpis ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@B_Zaswiadczenie", rec.Zaswiadczenie);
                    cmd.Parameters.AddWithValue("@B_książeczka", rec.Ksiazeczka);
                    cmd.Parameters.AddWithValue("@B_Ankieta", rec.Ankieta);
                    cmd.Parameters.AddWithValue("@B_Nowe", rec.Nowe);
                    cmd.Parameters.AddWithValue("@B_Activ", rec.Activ);

                    cmd.ExecuteNonQuery();

                    using var idCmd = new OdbcCommand("SELECT @@IDENTITY", connection);
                    var result = idCmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int id))
                        newId = id;
                }
                NotificationHelper.ShowRefferalSaved();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu skierowania do bazy:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return newId;
        }

        public PacjentRecord? GetPacjentById(int pId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_pesel,
    P_Pacjent.P_brak,
    P_Pacjent.P_płeć,
    P_Pacjent.P_zawód,
    P_Pacjent.P_Uwagi,
    P_Pacjent.P_Adres_ulica_numer,
    P_Pacjent.P_Ades_kod,
    P_Pacjent.P_email,
    P_Pacjent.P_telefon,
    P_Pacjent.P_Firma_id,
    Firma.Nazwa,
    P_Pacjent.P_ID
FROM
    Firma
    INNER JOIN P_Pacjent ON Firma.id = P_Pacjent.P_Firma_id
WHERE
    P_Pacjent.P_ID = ?";

                var param = cmd.CreateParameter();
                param.ParameterName = "@id";
                param.Value = pId;
                cmd.Parameters.Add(param);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new PacjentRecord
                    {
                        Imie = reader["P_imie"]?.ToString(),
                        Nazwisko = reader["P_nazwisko"]?.ToString(),
                        PESEL = reader["P_pesel"]?.ToString(),
                        BrakPESEL = reader["P_brak"] is bool b ? b : (reader["P_brak"]?.ToString() == "True"),
                        Plec = reader["P_płeć"]?.ToString(),
                        Zawod = reader["P_zawód"]?.ToString(),
                        Uwagi = reader["P_Uwagi"]?.ToString(),
                        UlicaNumerDomu = reader["P_Adres_ulica_numer"]?.ToString(),
                        Kod = reader["P_Ades_kod"]?.ToString(),
                        Email = reader["P_email"]?.ToString(),
                        Telefon = reader["P_telefon"]?.ToString(),
                        FirmaId = reader["P_Firma_id"] is int fid ? fid : int.TryParse(reader["P_Firma_id"]?.ToString(), out var fid2) ? fid2 : (int?)null,
                        FirmaNazwa = reader["Nazwa"]?.ToString(),
                        ID = reader["P_ID"] is int id ? id : int.TryParse(reader["P_ID"]?.ToString(), out var id2) ? id2 : (int?)null
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania danych pacjenta:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        public class PacjentRecord
        {
            public int? ID { get; set; }
            public string? Imie { get; set; }
            public string? Nazwisko { get; set; }
            public string? PESEL { get; set; }
            public bool BrakPESEL { get; set; }
            public string? Plec { get; set; }
            public string? Zawod { get; set; }
            public string? Uwagi { get; set; }
            public string? UlicaNumerDomu { get; set; }
            public string? Kod { get; set; }
            public string? Email { get; set; }
            public string? Telefon { get; set; }
            public int? FirmaId { get; set; }
            public string? FirmaNazwa { get; set; }
        }

        public class SkierowanieRecord
        {
            public int? PacjentId { get; set; }
            public int? FirmaId { get; set; }
            public int? BadanieId { get; set; }
            public DateTime? DataSkierowania { get; set; }
            public string? TypBadania { get; set; }
            public string? Stanowisko { get; set; }
            public DateTime? RegistrationDate { get; set; }
            public bool CzynnikFizyczny { get; set; }
            public string? CzynnikFizycznyOpis { get; set; }
            public bool CzynnikPylowy { get; set; }
            public string? CzynnikPylowyOpis { get; set; }
            public bool CzynnikChemiczny { get; set; }
            public string? CzynnikChemicznyOpis { get; set; }
            public bool CzynnikBiologiczny { get; set; }
            public string? CzynnikBiologicznyOpis { get; set; }
            public bool CzynnikInny { get; set; }
            public string? CzynnikInnyOpis { get; set; }
            public bool CzynnikSanepid { get; set; }
            public string? CzynnikSanepidOpis { get; set; }
            public bool Zaswiadczenie { get; set; }
            public bool Ksiazeczka { get; set; }
            public bool Ankieta { get; set; }
            public bool Nowe { get; set; }
            public bool Activ { get; set; } = true;
            public DateTime? Bad_Data { get; set; }
            public DateTime? R_Data { get; set; }
            public string? FK_Numer { get; set; }
            public int? B_ID { get; set; }
        }

        public class SkierowanieFullRecord
        {
            public int? B_ID { get; set; }
            public int? B_Pacjent_ID { get; set; }
            public int? B_Firma_ID { get; set; }
            public int? B_Badanie_ID { get; set; }
            public DateTime? B_DataSkierowania { get; set; }
            public string? B_TypBadania { get; set; }
            public string? B_Stanowisko { get; set; }
            public bool? B_czynnik_fizyczny { get; set; }
            public string? B_czynnik_fizyczny_opis { get; set; }
            public bool? B_czynnik_pyłowy { get; set; }
            public string? B_czynnik_pyłowy_opis { get; set; }
            public bool? B_czynnik_chemiczny { get; set; }
            public string? B_czynnik_chemiczny_opis { get; set; }
            public bool? B_czynnik_biologiczny { get; set; }
            public string? B_czynnik_biologiczny_opis { get; set; }
            public bool? B_czynnik_inny { get; set; }
            public string? B_czynnik_inny_opis { get; set; }
            public bool? B_czynnik_sanepid { get; set; }
            public string? B_czynnik_sanepid_opis { get; set; }
            public bool? B_Zaswiadczenie { get; set; }
            public bool? B_książeczka { get; set; }
            public bool? B_Ankieta { get; set; }
            public bool? B_Nowe { get; set; }
            public bool? B_Activ { get; set; }

            // Patient
            public string? P_imie { get; set; }
            public string? P_nazwisko { get; set; }
            public string? P_pesel { get; set; }
            public bool? P_brak { get; set; }
            public string? P_plec { get; set; }
            public DateTime? P_data_urodzenia { get; set; }
            public string? P_zawod { get; set; }
            public string? P_Uwagi { get; set; }
            public string? P_Adres_ulica_numer { get; set; }
            public string? P_Ades_kod { get; set; }
            public string? P_Ades_miasto { get; set; }
            public string? P_telefon { get; set; }
            public string? P_email { get; set; }
            public int? P_ID { get; set; }
            public int? P_Firma_id { get; set; }

            // Firma
            public string? Firma_Nazwa { get; set; }
            public string? Firma_Kod { get; set; }
            public string? Firma_Miejscowosc { get; set; }
            public string? Firma_Ulica { get; set; }
            public int? Firma_id { get; set; }

            // Extra
            public DateTime? Bad_Data { get; set; }
            public DateTime? R_Data { get; set; }
            public string? FK_Numer { get; set; }

            // ASCII aliases for properties that contain diacritics in their names
            // Some parts of code reference non-diacritic names (B_czynnik_pylowy etc.)
            public bool? B_czynnik_pylowy
            {
                get => B_czynnik_pyłowy;
                set => B_czynnik_pyłowy = value;
            }

            public string? B_czynnik_pylowy_opis
            {
                get => B_czynnik_pyłowy_opis;
                set => B_czynnik_pyłowy_opis = value;
            }

            // Alias for B_książeczka -> B_ksiazeczka (ASCII)
            public bool? B_ksiazeczka
            {
                get => B_książeczka;
                set => B_książeczka = value;
            }
            public DateTime? B_RegistrationDate { get; internal set; }
        }

        public class RejestracjaRecord
        {
            // ═══════════════════════════════════════════════════════
            // ✅ REJESTRACJA (istniejące pola)
            // ═══════════════════════════════════════════════════════
            public int? R_ID { get; set; }
            public int? R_B_ID { get; set; }
            public DateTime? R_Data { get; set; }
            public string? RStatus { get; set; }
            public int? R_Employee_ID { get; set; }
            public int R_S_ID { get; set; }
            public int R_P_ID { get; set; }
            public DateTime? R_GG_MM { get; set; }
            public string? R_Subject { get; set; }
            public string? R_Uwagi { get; set; }

            // ═══════════════════════════════════════════════════════
            // ✅ PACJENT (nowe pola z JOIN P_Pacjent)
            // ═══════════════════════════════════════════════════════
            public string? P_Imie { get; set; }
            public string? P_Nazwisko { get; set; }
            public string? P_Pesel { get; set; }
            public string? P_Telefon { get; set; }
            public string? P_Email { get; set; }
            public string? P_Plec { get; set; }
            public DateTime? P_DataUrodzenia { get; set; }
            public string? P_Zawod { get; set; }
            public string? P_AdresUlica { get; set; }
            public string? P_AdresKod { get; set; }
            public string? P_AdresMiasto { get; set; }
            public int? P_FirmaId { get; set; }

            // ═══════════════════════════════════════════════════════
            // ✅ FIRMA (nowe pola z JOIN Firma)
            // ═══════════════════════════════════════════════════════
            public string? Firma_Nazwa { get; set; }
            public string? Firma_Kod { get; set; }
            public string? Firma_Miejscowosc { get; set; }
            public string? Firma_Ulica { get; set; }
            public string? Firma_Email { get; set; }

            // ═══════════════════════════════════════════════════════
            // ✅ SKIEROWANIE (nowe pola z JOIN B_Skierowania)
            // ═══════════════════════════════════════════════════════
            public int? B_ID { get; set; }
            public DateTime? B_DataSkierowania { get; set; }
            public string? B_TypBadania { get; set; }
            public string? B_Stanowisko { get; set; }
            public DateTime? B_RegistrationDate { get; set; }

            // Czynniki szkodliwe
            public bool? B_CzynnikFizyczny { get; set; }
            public string? B_CzynnikFizycznyOpis { get; set; }
            public bool? B_CzynnikPylowy { get; set; }
            public string? B_CzynnikPylowyOpis { get; set; }
            public bool? B_CzynnikChemiczny { get; set; }
            public string? B_CzynnikChemicznyOpis { get; set; }
            public bool? B_CzynnikBiologiczny { get; set; }
            public string? B_CzynnikBiologicznyOpis { get; set; }
            public bool? B_CzynnikInny { get; set; }
            public string? B_CzynnikInnyOpis { get; set; }

            // Dokumenty
            public bool? B_Zaswiadczenie { get; set; }
            public bool? B_Ksiazeczka { get; set; }
            public bool? BrakPESEL { get; internal set; }
            public int? P_ID { get; internal set; }
        }

        public void AddRejestracja(RejestracjaRecord rec)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = new OdbcCommand(@"INSERT INTO Rejestracja 
                (R_Data, R_Status, R_S_ID, R_GG_MM, R_Subject, R_Uwagi) 
                VALUES (?, ?, ?, ?,?, ?)", connection);

                cmd.Parameters.AddWithValue("@R_Data", rec.R_Data.HasValue ? rec.R_Data.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_Status", rec.RStatus ?? (object)DBNull.Value);
                int r_s_id_param = rec.R_S_ID != 0 ? rec.R_S_ID : 0;
                cmd.Parameters.AddWithValue("@R_S_ID", r_s_id_param);
                cmd.Parameters.AddWithValue("@R_GG_MM", rec.R_GG_MM.HasValue ? rec.R_GG_MM.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_Subject", rec.R_Subject ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_Uwagi", rec.R_Uwagi ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
                NotificationHelper.ShowRegistrationSaved();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu rejestracji do bazy:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public int AddRejestracjaReturnId(RejestracjaRecord rec)
        {
            int newId = 0;
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = new OdbcCommand(@"INSERT INTO Rejestracja 
                (R_Data, R_Status, R_S_ID, R_GG_MM, R_Subject, R_Uwagi) 
                VALUES (?, ?, ?, ?,?, ?)", connection);

                DateTime? rDataParam = null;
                if (rec.R_GG_MM.HasValue)
                {
                    var dt = rec.R_GG_MM.Value;
                    if (dt.TimeOfDay == TimeSpan.Zero)
                        dt = new DateTime(dt.Year, dt.Month, dt.Day, 11, 00, 00);
                    rDataParam = dt;
                }
                cmd.Parameters.AddWithValue("@R_Data", rDataParam.HasValue ? (object)rDataParam.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@R_Status", rec.RStatus ?? (object)DBNull.Value);
                int r_s_id_param = rec.R_S_ID != 0 ? rec.R_S_ID : 50;
                cmd.Parameters.AddWithValue("@R_S_ID", r_s_id_param);
                cmd.Parameters.AddWithValue("@R_GG_MM", rec.R_GG_MM.HasValue ? rec.R_GG_MM.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_Subject", rec.R_Subject ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_Uwagi", rec.R_Uwagi ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();

                using var idCmd = new OdbcCommand("SELECT @@IDENTITY", connection);
                var result = idCmd.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                    newId = id;
                NotificationHelper.ShowRegistrationSaved();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu rejestracji do bazy:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return newId;
        }

        // nowe GetRejestracje

        public List<RejestracjaRecord> GetRejestracje()
        {
            var result = new List<RejestracjaRecord>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();

                using var cmd = new OdbcCommand(@"
SELECT
    Rejestracja.R_ID,
    Rejestracja.R_Data,
    Rejestracja.R_GG_MM,
    Rejestracja.R_Status,
    Rejestracja.R_Subject,
    Rejestracja.R_Uwagi,
    Rejestracja.R_S_ID,
    Rejestracja.R_P_ID,
    P_Pacjent.P_ID,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_brak,
    P_Pacjent.P_pesel,
    P_Pacjent.P_telefon,
    P_Pacjent.P_email,
    P_Pacjent.P_płeć,
    P_Pacjent.P_data_urodzenia,
    P_Pacjent.P_zawód,
    P_Pacjent.P_Adres_ulica_numer,
    P_Pacjent.P_Ades_kod,
    P_Pacjent.P_Ades_miasto,
    P_Pacjent.P_Firma_id,
    Firma.Nazwa AS Firma_Nazwa,
    Firma.Kod,
    Firma.Miejscowosc,
    Firma.Ulica,
    Firma.FKemail AS Firma_Email,
    B_Skierowania.B_ID,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_Stanowisko,
    B_Skierowania.B_RegistrationDate,
    B_Skierowania.B_czynnik_fizyczny,
    B_Skierowania.B_czynnik_fizyczny_opis,
    B_Skierowania.B_czynnik_pyłowy,
    B_Skierowania.B_czynnik_pyłowy_opis,
    B_Skierowania.B_czynnik_chemiczny,
    B_Skierowania.B_czynnik_chemiczny_opis,
    B_Skierowania.B_czynnik_biologiczny,
    B_Skierowania.B_czynnik_biologiczny_opis,
    B_Skierowania.B_czynnik_inny,
    B_Skierowania.B_czynnik_inny_opis,
    B_Skierowania.B_Zaswiadczenie,
    B_Skierowania.B_książeczka
FROM
    (
        (
            Rejestracja
            INNER JOIN B_Skierowania ON Rejestracja.R_S_ID = B_Skierowania.B_ID
        )
        INNER JOIN P_Pacjent ON B_Skierowania.B_Pacjent_ID = P_Pacjent.P_ID
    )
    INNER JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
ORDER BY
    Rejestracja.R_Data DESC,
    Rejestracja.R_GG_MM DESC", connection);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new RejestracjaRecord
                    {
                        // ═══════════════════════════════════════════════════════
                        // ✅ REJESTRACJA
                        // ═══════════════════════════════════════════════════════
                        R_ID = reader["R_ID"] as int? ?? (reader["R_ID"] != DBNull.Value ? Convert.ToInt32(reader["R_ID"]) : (int?)null),
                        R_Data = reader["R_Data"] != DBNull.Value ? Convert.ToDateTime(reader["R_Data"]) : (DateTime?)null,
                        R_GG_MM = reader["R_GG_MM"] != DBNull.Value ? Convert.ToDateTime(reader["R_GG_MM"]) : (DateTime?)null,
                        RStatus = reader["R_Status"]?.ToString(),
                        R_Subject = reader["R_Subject"]?.ToString(),
                        R_Uwagi = reader["R_Uwagi"]?.ToString(),
                        R_S_ID = reader["R_S_ID"] is int sid ? sid : int.TryParse(reader["R_S_ID"]?.ToString(), out var sid2) ? sid2 : 0,
                        R_P_ID = reader["R_P_ID"] is int pid ? pid : int.TryParse(reader["R_P_ID"]?.ToString(), out var pid2) ? pid2 : 0,

                        // ═══════════════════════════════════════════════════════
                        // ✅ PACJENT (z JOIN P_Pacjent)
                        // ═══════════════════════════════════════════════════════
                        P_ID = reader["P_ID"] is int p_id ? p_id : int.TryParse(reader["P_ID"]?.ToString(), out var p_id2) ? p_id2 : (int?)null,
                        P_Imie = reader["P_imie"]?.ToString(),
                        P_Nazwisko = reader["P_nazwisko"]?.ToString(),
                        BrakPESEL = reader["P_brak"] is bool b ? b : (reader["P_brak"]?.ToString() == "True"),
                        P_Pesel = reader["P_pesel"]?.ToString(),
                        P_Telefon = reader["P_telefon"]?.ToString(),
                        P_Email = reader["P_email"]?.ToString(),
                        P_Plec = reader["P_płeć"]?.ToString(),
                        P_DataUrodzenia = reader["P_data_urodzenia"] != DBNull.Value ? Convert.ToDateTime(reader["P_data_urodzenia"]) : (DateTime?)null,
                        P_Zawod = reader["P_zawód"]?.ToString(),
                        P_AdresUlica = reader["P_Adres_ulica_numer"]?.ToString(),
                        P_AdresKod = reader["P_Ades_kod"]?.ToString(),
                        P_AdresMiasto = reader["P_Ades_miasto"]?.ToString(),
                        P_FirmaId = reader["P_Firma_id"] is int fid ? fid : int.TryParse(reader["P_Firma_id"]?.ToString(), out var fid2) ? fid2 : (int?)null,

                        // ═══════════════════════════════════════════════════════
                        // ✅ FIRMA (z JOIN Firma)
                        // ═══════════════════════════════════════════════════════
                        Firma_Nazwa = reader["Firma_Nazwa"]?.ToString(),
                        Firma_Kod = reader["Kod"]?.ToString(),
                        Firma_Miejscowosc = reader["Miejscowosc"]?.ToString(),
                        Firma_Ulica = reader["Ulica"]?.ToString(),
                        //Firma_Email = reader["Firma_Email"]?.ToString(),
                        Firma_Email = reader["Firma_Email"]?.ToString() ?? "info@adres.pl",

                        // ═══════════════════════════════════════════════════════
                        // ✅ SKIEROWANIE (z JOIN B_Skierowania)
                        // ═══════════════════════════════════════════════════════
                        B_ID = reader["B_ID"] is int bid ? bid : int.TryParse(reader["B_ID"]?.ToString(), out var bid2) ? bid2 : (int?)null,
                        B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : (DateTime?)null,
                        B_TypBadania = reader["B_TypBadania"]?.ToString(),
                        B_Stanowisko = reader["B_Stanowisko"]?.ToString(),
                        B_RegistrationDate = reader["B_RegistrationDate"] != DBNull.Value ? Convert.ToDateTime(reader["B_RegistrationDate"]) : (DateTime?)null,

                        // Czynniki szkodliwe
                        B_CzynnikFizyczny = reader["B_czynnik_fizyczny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_fizyczny"]) : (bool?)null,
                        B_CzynnikFizycznyOpis = reader["B_czynnik_fizyczny_opis"]?.ToString(),
                        B_CzynnikPylowy = reader["B_czynnik_pyłowy"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_pyłowy"]) : (bool?)null,
                        B_CzynnikPylowyOpis = reader["B_czynnik_pyłowy_opis"]?.ToString(),
                        B_CzynnikChemiczny = reader["B_czynnik_chemiczny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_chemiczny"]) : (bool?)null,
                        B_CzynnikChemicznyOpis = reader["B_czynnik_chemiczny_opis"]?.ToString(),
                        B_CzynnikBiologiczny = reader["B_czynnik_biologiczny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_biologiczny"]) : (bool?)null,
                        B_CzynnikBiologicznyOpis = reader["B_czynnik_biologiczny_opis"]?.ToString(),
                        B_CzynnikInny = reader["B_czynnik_inny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_inny"]) : (bool?)null,
                        B_CzynnikInnyOpis = reader["B_czynnik_inny_opis"]?.ToString(),

                        // Dokumenty
                        B_Zaswiadczenie = reader["B_Zaswiadczenie"] != DBNull.Value ? Convert.ToBoolean(reader["B_Zaswiadczenie"]) : (bool?)null,
                        B_Ksiazeczka = reader["B_książeczka"] != DBNull.Value ? Convert.ToBoolean(reader["B_książeczka"]) : (bool?)null
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania rejestracji:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        // end nowe GetRejestracje
        public bool UpdateRejestracja(int rId, RejestracjaRecord rec)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE Rejestracja SET
                R_Data = ?,
                R_Status = ?,
                R_S_ID = ?,
                R_GG_MM = ?,
                R_Subject = ?,
                R_Uwagi = ?
            WHERE R_ID = ?";

                cmd.Parameters.AddWithValue("@R_Data", rec.R_Data.HasValue ? rec.R_Data.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_Status", rec.RStatus ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_S_ID", rec.R_S_ID);
                cmd.Parameters.AddWithValue("@R_GG_MM", rec.R_GG_MM.HasValue ? rec.R_GG_MM.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_Subject", rec.R_Subject ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_Uwagi", rec.R_Uwagi ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@R_ID", rId);
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0) NotificationHelper.ShowRegistrationUpdate();
                return rows > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd aktualizacji rejestracji:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Usuwa skierowanie (kartę badań) jeśli nie ma powiązanego badania (B_Badanie_ID == 0 lub null)
        /// </summary>
        /// <param name="skierowanieId">ID skierowania (B_ID)</param>
        /// <returns>True jeśli usunięto, False jeśli istnieje powiązane badanie lub błąd</returns>
        public bool DeleteSkierowanie(int skierowanieId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();

                // ✅ KROK 1: Sprawdź czy nie ma powiązanego badania
                using (var checkCmd = new OdbcCommand(
                    "SELECT B_Badanie_ID FROM B_Skierowania WHERE B_ID = ?", connection))
                {
                    checkCmd.Parameters.AddWithValue("@B_ID", skierowanieId);
                    var result = checkCmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        if (int.TryParse(result.ToString(), out var badanieId) && badanieId > 0)
                        {
                            // ❌ Istnieje powiązane badanie - nie można usunąć
                            MessageBox.Show(
                                $"Nie można usunąć karty badań.\nIstnieje powiązane badanie (ID: {badanieId}).\n\nNajpierw usuń badanie.",
                                "Błąd usuwania",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return false;
                        }
                    }
                }

                // ✅ KROK 2: Usuń skierowanie (nie ma powiązanego badania)
                using var deleteCmd = new OdbcCommand(
                    "DELETE FROM B_Skierowania WHERE B_ID = ?", connection);
                deleteCmd.Parameters.AddWithValue("@B_ID", skierowanieId);

                int rows = deleteCmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    NotificationHelper.ShowInfo("Karta badań usunięta", $"ID: {skierowanieId}");
                    // System.Diagnostics.Debug.WriteLine($"✅ DeleteSkierowanie: Usunięto B_ID={skierowanieId}");
                    return true;
                }

                MessageBox.Show(
                    "Nie znaleziono karty badań do usunięcia.",
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"DeleteSkierowanie ERROR: {ex.Message}");
                MessageBox.Show(
                    $"Błąd usuwania karty badań:\n{ex.Message}",
                    "Błąd bazy danych",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Sprawdza czy skierowanie ma powiązane badanie (B_Badanie_ID > 0)
        /// </summary>
        public bool HasSkierowanieBadanie(int skierowanieId)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"🔍 HasSkierowanieBadanie: Sprawdzam B_ID={skierowanieId}...");

                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = new OdbcCommand(
                    "SELECT B_Badanie_ID FROM B_Skierowania WHERE B_ID = ?", connection);
                cmd.Parameters.AddWithValue("@B_ID", skierowanieId);

                var result = cmd.ExecuteScalar();

                // ✅ LOGI DEBUG
                if (result == null)
                {
                    // System.Diagnostics.Debug.WriteLine($"  → result == null (brak rekordu lub błąd SQL)");
                    return false;
                }

                if (result == DBNull.Value)
                {
                    // System.Diagnostics.Debug.WriteLine($"  → result == DBNull.Value (pole B_Badanie_ID jest NULL) → CanDelete=TRUE");
                    return false; // NULL = brak badania = można usunąć
                }

                // Spróbuj sparsować jako int
                if (int.TryParse(result.ToString(), out var badanieId))
                {
                    // System.Diagnostics.Debug.WriteLine($"  → B_Badanie_ID = {badanieId}");

                    if (badanieId > 0)
                    {
                        // System.Diagnostics.Debug.WriteLine($"  → HAS badanie (Bad_ID={badanieId}) → CanDelete=FALSE");
                        return true; // Istnieje badanie = NIE można usunąć
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"  → B_Badanie_ID = 0 (brak badania) → CanDelete=TRUE");
                        return false; // 0 = brak badania = można usunąć
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"  → Nie udało się sparsować result='{result}' → domyślnie FALSE");
                return false;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ HasSkierowanieBadanie ERROR: {ex.Message}");
                return true; // W razie błędu zakładamy że ma badanie (bezpieczniej)
            }
        }


        public void DeleteRejestracja(int rId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = new OdbcCommand("DELETE FROM Rejestracja WHERE R_ID = ?", connection);
                cmd.Parameters.AddWithValue("@R_ID", rId);
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0) NotificationHelper.ShowRegistrationDeleted();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd usuwania rejestracji:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class BadanieRecord
        {
            public int? Bad_ID { get; set; }
            public int? Bad_R_ID { get; set; }
            public int? Bad_S_ID { get; set; }
            public int? Bad_P_ID { get; set; }
            public int? Bad_L_ID { get; set; }
            public int? Bad_F_ID { get; set; }
            public string? Bad_bn_cennik { get; set; }
            public string? Bad_Typ { get; set; }
            public DateTime? Bad_Data { get; set; }
            public DateTime? Bad_Data_Do { get; set; }
            public string? Bad_Wynik { get; set; }
            public decimal? Bad_Cena1 { get; set; }
            public decimal? Bad_Cena2 { get; set; }
            public decimal? Bad_Cena3 { get; set; }
            public decimal? Bad_Cena4 { get; set; }
            public decimal? Bad_Cena5 { get; set; }
            public decimal? Bad_Cena6 { get; set; }
            public decimal? Bad_Cena7 { get; set; }
            public decimal? Bad_Cena8 { get; set; }
            public decimal? Bad_Cena9 { get; set; }
            public decimal? Bad_Cena10 { get; set; }
            public decimal? Bad_Razem { get; set; }
            public string? Bad_Nr_KS { get; set; }
            public bool Bad_END { get; set; }
        }

        public int AddBadanie(BadanieRecord rec)
        {
            int newId = 0;
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    // build column list and placeholders dynamically to avoid mismatch
                    var columns = new[] {
    "Bad_R_ID", "Bad_S_ID", "Bad_P_ID", "Bad_bn_cennik", "Bad_Typ", "Bad_Data", "Bad_Data_Do", "Bad_Wynik",
    "Bad_Cena1", "Bad_Cena2", "Bad_Cena3", "Bad_Cena4", "Bad_Cena5", "Bad_Cena6", "Bad_Cena7", "Bad_Cena8", "Bad_Cena9", "Bad_Cena10",
    "Bad_Razem", "Bad_Nr_KS", "Bad_END"
};
                    var placeholders = string.Join(",", Enumerable.Repeat("?", columns.Length));
                    cmd.CommandText = $"INSERT INTO Badanie ({string.Join(", ", columns)}) VALUES ({placeholders})";

                    // add parameters in same order as columns
                    OdbcParameter p;
                    p = cmd.CreateParameter(); p.ParameterName = "@p1"; p.Value = rec.Bad_R_ID.HasValue ? (object)rec.Bad_R_ID.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p2"; p.Value = rec.Bad_S_ID.HasValue ? (object)rec.Bad_S_ID.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p3"; p.Value = rec.Bad_P_ID.HasValue ? (object)rec.Bad_P_ID.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p4"; p.Value = rec.Bad_bn_cennik ?? (object)DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p5"; p.Value = rec.Bad_Typ ?? (object)DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p6"; p.Value = rec.Bad_Data.HasValue ? (object)rec.Bad_Data.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p7"; p.Value = rec.Bad_Data_Do.HasValue ? (object)rec.Bad_Data_Do.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p8"; p.Value = rec.Bad_Wynik ?? (object)DBNull.Value; cmd.Parameters.Add(p);

                    p = cmd.CreateParameter(); p.ParameterName = "@p9"; p.Value = rec.Bad_Cena1.HasValue ? (object)rec.Bad_Cena1.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p10"; p.Value = rec.Bad_Cena2.HasValue ? (object)rec.Bad_Cena2.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p11"; p.Value = rec.Bad_Cena3.HasValue ? (object)rec.Bad_Cena3.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p12"; p.Value = rec.Bad_Cena4.HasValue ? (object)rec.Bad_Cena4.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p13"; p.Value = rec.Bad_Cena5.HasValue ? (object)rec.Bad_Cena5.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p14"; p.Value = rec.Bad_Cena6.HasValue ? (object)rec.Bad_Cena6.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p15"; p.Value = rec.Bad_Cena7.HasValue ? (object)rec.Bad_Cena7.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p16"; p.Value = rec.Bad_Cena8.HasValue ? (object)rec.Bad_Cena8.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p17"; p.Value = rec.Bad_Cena9.HasValue ? (object)rec.Bad_Cena9.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p18"; p.Value = rec.Bad_Cena10.HasValue ? (object)rec.Bad_Cena10.Value : DBNull.Value; cmd.Parameters.Add(p);

                    p = cmd.CreateParameter(); p.ParameterName = "@p19"; p.Value = rec.Bad_Razem.HasValue ? (object)rec.Bad_Razem.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p20"; p.Value = rec.Bad_Nr_KS ?? (object)DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@p21"; p.Value = rec.Bad_END; cmd.Parameters.Add(p);

                    cmd.ExecuteNonQuery();

                    using var idCmd = new OdbcCommand("SELECT @@IDENTITY", connection);
                    var result = idCmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int id))
                        newId = id;

                    NotificationHelper.ShowInfo("Badanie zapisane", $"ID = {newId}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu badania do bazy:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return newId;
        }

        public bool UpdateBadanie(int badId, BadanieRecord rec)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"UPDATE Badanie SET
                        Bad_R_ID = ?, Bad_S_ID = ?, Bad_P_ID = ?, Bad_bn_cennik = ?, Bad_Typ = ?, Bad_Data = ?, Bad_Data_Do = ?, Bad_Wynik = ?,
                        Bad_Cena1 = ?, Bad_Cena2 = ?, Bad_Cena3 = ?, Bad_Cena4 = ?, Bad_Cena5 = ?, Bad_Cena6 = ?, Bad_Cena7 = ?, Bad_Cena8 = ?, Bad_Cena9 = ?, Bad_Cena10 = ?,
                        Bad_Razem = ?, Bad_Nr_KS = ?, Bad_END = ?
                    WHERE Bad_ID = ?";

                    var p = cmd.CreateParameter(); p.ParameterName = "@Bad_R_ID"; p.Value = rec.Bad_R_ID.HasValue ? (object)rec.Bad_R_ID.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_S_ID"; p.Value = rec.Bad_S_ID.HasValue ? (object)rec.Bad_S_ID.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_P_ID"; p.Value = rec.Bad_P_ID.HasValue ? (object)rec.Bad_P_ID.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_bn_cennik"; p.Value = rec.Bad_bn_cennik ?? (object)DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Typ"; p.Value = rec.Bad_Typ ?? (object)DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Data"; p.Value = rec.Bad_Data.HasValue ? (object)rec.Bad_Data.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Data_Do"; p.Value = rec.Bad_Data_Do.HasValue ? (object)rec.Bad_Data_Do.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Wynik"; p.Value = rec.Bad_Wynik ?? (object)DBNull.Value; cmd.Parameters.Add(p);

                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena1"; p.Value = rec.Bad_Cena1.HasValue ? (object)rec.Bad_Cena1.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena2"; p.Value = rec.Bad_Cena2.HasValue ? (object)rec.Bad_Cena2.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena3"; p.Value = rec.Bad_Cena3.HasValue ? (object)rec.Bad_Cena3.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena4"; p.Value = rec.Bad_Cena4.HasValue ? (object)rec.Bad_Cena4.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena5"; p.Value = rec.Bad_Cena5.HasValue ? (object)rec.Bad_Cena5.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena6"; p.Value = rec.Bad_Cena6.HasValue ? (object)rec.Bad_Cena6.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena7"; p.Value = rec.Bad_Cena7.HasValue ? (object)rec.Bad_Cena7.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena8"; p.Value = rec.Bad_Cena8.HasValue ? (object)rec.Bad_Cena8.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena9"; p.Value = rec.Bad_Cena9.HasValue ? (object)rec.Bad_Cena9.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena10"; p.Value = rec.Bad_Cena10.HasValue ? (object)rec.Bad_Cena10.Value : DBNull.Value; cmd.Parameters.Add(p);

                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Razem"; p.Value = rec.Bad_Razem.HasValue ? (object)rec.Bad_Razem.Value : DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_Nr_KS"; p.Value = rec.Bad_Nr_KS ?? (object)DBNull.Value; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_END"; p.Value = rec.Bad_END; cmd.Parameters.Add(p);

                    p = cmd.CreateParameter(); p.ParameterName = "@Bad_ID"; p.Value = badId; cmd.Parameters.Add(p);

                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd aktualizacji badania:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Ustawia pole B_Badanie_ID w tabeli B_Skierowania (linkuje skierowanie do badania)
        /// </summary>
        public bool UpdateSkierowanieBadanieId(int bId, int badId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "UPDATE B_Skierowania SET B_Badanie_ID = ? WHERE B_ID = ?";
                    var p = cmd.CreateParameter(); p.ParameterName = "@B_Badanie_ID"; p.Value = badId; cmd.Parameters.Add(p);
                    p = cmd.CreateParameter(); p.ParameterName = "@B_ID"; p.Value = bId; cmd.Parameters.Add(p);
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd aktualizacji skierowania (ustawienie Badanie_ID):\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public BadanieRecord? GetBadanieById(int badId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT Bad_ID, Bad_R_ID, Bad_S_ID, Bad_P_ID, Bad_L_ID, Bad_F_ID, Bad_bn_cennik, Bad_Typ, Bad_Data, Bad_Data_Do, Bad_Wynik,
                            Bad_Cena1, Bad_Cena2, Bad_Cena3, Bad_Cena4, Bad_Cena5, Bad_Cena6, Bad_Cena7, Bad_Cena8, Bad_Cena9, Bad_Cena10,
                            Bad_Razem, Bad_Nr_KS, Bad_END
                            FROM Badanie WHERE Bad_ID = ?";

                var param = cmd.CreateParameter();
                param.ParameterName = "@Bad_ID";
                param.Value = badId;
                cmd.Parameters.Add(param);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var rec = new BadanieRecord();
                    rec.Bad_ID = reader["Bad_ID"] is int id ? id : (int.TryParse(reader["Bad_ID"]?.ToString(), out var id2) ? id2 : (int?)null);
                    rec.Bad_R_ID = reader["Bad_R_ID"] is int rr ? rr : (int.TryParse(reader["Bad_R_ID"]?.ToString(), out var rr2) ? rr2 : (int?)null);
                    rec.Bad_S_ID = reader["Bad_S_ID"] is int s ? s : (int.TryParse(reader["Bad_S_ID"]?.ToString(), out var s2) ? s2 : (int?)null);
                    rec.Bad_P_ID = reader["Bad_P_ID"] is int ppid ? ppid : (int.TryParse(reader["Bad_P_ID"]?.ToString(), out var ppid2) ? ppid2 : (int?)null);
                    rec.Bad_L_ID = reader["Bad_L_ID"] is int ll ? ll : (int.TryParse(reader["Bad_L_ID"]?.ToString(), out var ll2) ? ll2 : (int?)null);
                    rec.Bad_F_ID = reader["Bad_F_ID"] is int ff ? ff : (int.TryParse(reader["Bad_F_ID"]?.ToString(), out var ff2) ? ff2 : (int?)null);
                    rec.Bad_bn_cennik = reader["Bad_bn_cennik"]?.ToString();
                    rec.Bad_Typ = reader["Bad_Typ"]?.ToString();
                    rec.Bad_Data = reader["Bad_Data"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["Bad_Data"]) : null;
                    rec.Bad_Data_Do = reader["Bad_Data_Do"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["Bad_Data_Do"]) : null;
                    rec.Bad_Wynik = reader["Bad_Wynik"]?.ToString();

                    decimal? parseDec(object obj)
                    {
                        if (obj == null || obj == DBNull.Value) return null;
                        if (decimal.TryParse(obj.ToString(), out var d)) return d;
                        return null;
                    }

                    rec.Bad_Cena1 = parseDec(reader["Bad_Cena1"]);
                    rec.Bad_Cena2 = parseDec(reader["Bad_Cena2"]);
                    rec.Bad_Cena3 = parseDec(reader["Bad_Cena3"]);
                    rec.Bad_Cena4 = parseDec(reader["Bad_Cena4"]);
                    rec.Bad_Cena5 = parseDec(reader["Bad_Cena5"]);
                    rec.Bad_Cena6 = parseDec(reader["Bad_Cena6"]);
                    rec.Bad_Cena7 = parseDec(reader["Bad_Cena7"]);
                    rec.Bad_Cena8 = parseDec(reader["Bad_Cena8"]);
                    rec.Bad_Cena9 = parseDec(reader["Bad_Cena9"]);
                    rec.Bad_Cena10 = parseDec(reader["Bad_Cena10"]);
                    rec.Bad_Razem = parseDec(reader["Bad_Razem"]);
                    rec.Bad_Nr_KS = reader["Bad_Nr_KS"]?.ToString();
                    rec.Bad_END = reader["Bad_END"] != DBNull.Value && Convert.ToBoolean(reader["Bad_END"]);

                    return rec;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania badania: {ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        public bool DeleteBadanie(int badId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Badanie WHERE Bad_ID = ?";
                var param = cmd.CreateParameter(); param.ParameterName = "@Bad_ID"; param.Value = badId; cmd.Parameters.Add(param);
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0)
                {
                    NotificationHelper.ShowInfo("Usunięto badanie", $"Bad_ID = {badId}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd usuwania badania:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        internal SkierowanieFullRecord? GetSkierowanieById(int value)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT
    B_Skierowania.B_ID,
    B_Skierowania.B_Pacjent_ID,
    B_Skierowania.B_Firma_ID,
    B_Skierowania.B_Badanie_ID,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_RegistrationDate,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_Stanowisko,
    B_Skierowania.B_czynnik_fizyczny,
    B_Skierowania.B_czynnik_fizyczny_opis,
    B_Skierowania.B_czynnik_pyłowy,
    B_Skierowania.B_czynnik_pyłowy_opis,
    B_Skierowania.B_czynnik_chemiczny,
    B_Skierowania.B_czynnik_chemiczny_opis,
    B_Skierowania.B_czynnik_biologiczny,
    B_Skierowania.B_czynnik_biologiczny_opis,
    B_Skierowania.B_czynnik_inny,
    B_Skierowania.B_czynnik_inny_opis,
    B_Skierowania.B_czynnik_sanepid,
    B_Skierowania.B_czynnik_sanepid_opis,
    B_Skierowania.B_Zaswiadczenie,
    B_Skierowania.B_książeczka,
    B_Skierowania.B_Ankieta,
    B_Skierowania.B_Nowe,
    B_Skierowania.B_Activ,

    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_pesel,
    P_Pacjent.P_brak,
    P_Pacjent.P_płeć,
    P_Pacjent.P_data_urodzenia,
    P_Pacjent.P_zawód,
    P_Pacjent.P_Uwagi,
    P_Pacjent.P_Adres_ulica_numer,
    P_Pacjent.P_Ades_kod,
    P_Pacjent.P_Ades_miasto,
    P_Pacjent.P_telefon,
    P_Pacjent.P_email,
    P_Pacjent.P_ID,
    P_Pacjent.P_Firma_id,

    Firma.Nazwa AS Firma_Nazwa,
    Firma.Kod AS Firma_Kod,
    Firma.Miejscowosc AS Firma_Miejscowosc,
    Firma.Ulica AS Firma_Ulica,

    Badanie.Bad_Data AS Bad_Data,
    Rejestracja.R_Data AS R_Data,
    Faktura.FK_Numer AS FK_Numer

FROM
    (
        Firma
        INNER JOIN (
            (
                (
                    P_Pacjent
                    INNER JOIN B_Skierowania ON P_Pacjent.P_ID = B_Skierowania.B_Pacjent_ID
                )
                LEFT JOIN Badanie ON B_Skierowania.B_Badanie_ID = Badanie.Bad_ID
            )
            LEFT JOIN Rejestracja ON B_Skierowania.B_ID = Rejestracja.R_S_ID
        ) ON Firma.id = P_Pacjent.P_Firma_id
    )
    LEFT JOIN Faktura ON B_Skierowania.B_Faktura_ID = Faktura.FK_ID
WHERE
    B_Skierowania.B_ID = ?";

                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = value; cmd.Parameters.Add(p);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var rec = new SkierowanieFullRecord();
                    rec.B_ID = reader["B_ID"] is int bid ? bid : (int.TryParse(reader["B_ID"]?.ToString(), out var bid2) ? bid2 : (int?)null);
                    rec.B_Pacjent_ID = reader["B_Pacjent_ID"] is int pid ? pid : (int.TryParse(reader["B_Pacjent_ID"]?.ToString(), out var pid2) ? pid2 : (int?)null);
                    rec.B_Firma_ID = reader["B_Firma_ID"] is int fid ? fid : (int.TryParse(reader["B_Firma_ID"]?.ToString(), out var fid2) ? fid2 : (int?)null);
                    rec.B_Badanie_ID = reader["B_Badanie_ID"] is int bid3 ? bid3 : (int.TryParse(reader["B_Badanie_ID"]?.ToString(), out var bid32) ? bid32 : (int?)null);
                    rec.B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : (DateTime?)null;
                    rec.B_RegistrationDate = reader["B_RegistrationDate"] != DBNull.Value ? Convert.ToDateTime(reader["B_RegistrationDate"]) : (DateTime?)null;
                    rec.B_TypBadania = reader["B_TypBadania"]?.ToString();
                    rec.B_Stanowisko = reader["B_Stanowisko"]?.ToString();
                    rec.B_czynnik_fizyczny = reader["B_czynnik_fizyczny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_fizyczny"]) : (bool?)null;
                    rec.B_czynnik_fizyczny_opis = reader["B_czynnik_fizyczny_opis"]?.ToString();
                    rec.B_czynnik_pyłowy = reader["B_czynnik_pyłowy"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_pyłowy"]) : (bool?)null;
                    rec.B_czynnik_pyłowy_opis = reader["B_czynnik_pyłowy_opis"]?.ToString();
                    rec.B_czynnik_chemiczny = reader["B_czynnik_chemiczny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_chemiczny"]) : (bool?)null;
                    rec.B_czynnik_chemiczny_opis = reader["B_czynnik_chemiczny_opis"]?.ToString();
                    rec.B_czynnik_biologiczny = reader["B_czynnik_biologiczny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_biologiczny"]) : (bool?)null;
                    rec.B_czynnik_biologiczny_opis = reader["B_czynnik_biologiczny_opis"]?.ToString();
                    rec.B_czynnik_inny = reader["B_czynnik_inny"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_inny"]) : (bool?)null;
                    rec.B_czynnik_inny_opis = reader["B_czynnik_inny_opis"]?.ToString();
                    rec.B_czynnik_sanepid = reader["B_czynnik_sanepid"] != DBNull.Value ? Convert.ToBoolean(reader["B_czynnik_sanepid"]) : (bool?)null;
                    rec.B_czynnik_sanepid_opis = reader["B_czynnik_sanepid_opis"]?.ToString();
                    rec.B_Zaswiadczenie = reader["B_Zaswiadczenie"] != DBNull.Value ? Convert.ToBoolean(reader["B_Zaswiadczenie"]) : (bool?)null;
                    rec.B_książeczka = reader["B_książeczka"] != DBNull.Value ? Convert.ToBoolean(reader["B_książeczka"]) : (bool?)null;
                    rec.B_Ankieta = reader["B_Ankieta"] != DBNull.Value ? Convert.ToBoolean(reader["B_Ankieta"]) : (bool?)null;
                    rec.B_Nowe = reader["B_Nowe"] != DBNull.Value ? Convert.ToBoolean(reader["B_Nowe"]) : (bool?)null;
                    rec.B_Activ = reader["B_Activ"] != DBNull.Value ? Convert.ToBoolean(reader["B_Activ"]) : (bool?)null;

                    rec.P_imie = reader["P_imie"]?.ToString();
                    rec.P_nazwisko = reader["P_nazwisko"]?.ToString();
                    rec.P_pesel = reader["P_pesel"]?.ToString();
                    rec.P_brak = reader["P_brak"] != DBNull.Value ? Convert.ToBoolean(reader["P_brak"]) : (bool?)null;
                    rec.P_plec = reader["P_płeć"]?.ToString();
                    rec.P_data_urodzenia = reader["P_data_urodzenia"] != DBNull.Value ? Convert.ToDateTime(reader["P_data_urodzenia"]) : (DateTime?)null;
                    rec.P_zawod = reader["P_zawód"]?.ToString();
                    rec.P_Uwagi = reader["P_Uwagi"]?.ToString();
                    rec.P_Adres_ulica_numer = reader["P_Adres_ulica_numer"]?.ToString();
                    rec.P_Ades_kod = reader["P_Ades_kod"]?.ToString();
                    rec.P_Ades_miasto = reader["P_Ades_miasto"]?.ToString();
                    rec.P_telefon = reader["P_telefon"]?.ToString();
                    rec.P_email = reader["P_email"]?.ToString();
                    rec.P_ID = reader["P_ID"] is int pId ? pId : (int.TryParse(reader["P_ID"]?.ToString(), out var pId2) ? pId2 : (int?)null);
                    rec.P_Firma_id = reader["P_Firma_id"] is int pfid ? pfid : (int.TryParse(reader["P_Firma_id"]?.ToString(), out var pfid2) ? pfid2 : (int?)null);

                    rec.Firma_Nazwa = reader["Firma_Nazwa"]?.ToString();
                    rec.Firma_Kod = reader["Firma_Kod"]?.ToString();
                    rec.Firma_Miejscowosc = reader["Firma_Miejscowosc"]?.ToString();
                    rec.Firma_Ulica = reader["Firma_Ulica"]?.ToString();
                    rec.Firma_id = rec.P_Firma_id;

                    // extra fields from joins
                    rec.Bad_Data = reader["Bad_Data"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data"]) : (DateTime?)null;
                    rec.R_Data = reader["R_Data"] != DBNull.Value ? Convert.ToDateTime(reader["R_Data"]) : (DateTime?)null;
                    rec.FK_Numer = reader["FK_Numer"]?.ToString();

                    return rec;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania skierowania: {ex.Message}", "Błędy bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        /// <summary>
        /// DTO for ListyBadan
        /// </summary>
        public class ListyBadanDto
        {
            public int? Identyfikator { get; set; }
            public int? L_Firma_ID { get; set; } // ✅ NOWE
            public string? Nazwa { get; set; }
            public DateTime? FK_Data { get; set; }
            public string? FK_Numer { get; set; }
            public decimal? FK_Kwota { get; set; }

            // helper for display in list template
            public string FakturaInfo =>
                (FK_Data.HasValue ? FK_Data.Value.ToString("dd.MM.yyyy") : "") +
                (string.IsNullOrWhiteSpace(FK_Numer) ? "" : (" | " + FK_Numer)) +
                (FK_Kwota.HasValue ? (" | " + FK_Kwota.Value.ToString("N2") + " zł") : "");

            // assigned badania (loaded on selection) - changed to ObservableCollection so UI odświeża się automatycznie
            public System.Collections.ObjectModel.ObservableCollection<AssignedBadanieDto> Badania { get; set; } = new System.Collections.ObjectModel.ObservableCollection<AssignedBadanieDto>();
            public string? Numer { get; internal set; }
        }

        // DTO for assigned Badanie rows
        public class AssignedBadanieDto : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            // database PK of Badanie row
            private int? _badId;
            public int? Bad_ID { get => _badId; set { if (_badId != value) { _badId = value; OnPropertyChanged(); } } }

            private int? _badLId;
            public int? Bad_L_ID { get => _badLId; set { if (_badLId != value) { _badLId = value; OnPropertyChanged(); } } }

            private DateTime? _badData;
            public DateTime? Bad_Data { get => _badData; set { if (_badData != value) { _badData = value; OnPropertyChanged(); OnPropertyChanged(nameof(DataSkierDate)); OnPropertyChanged(nameof(DataSkierDisplay)); } } }

            private DateTime? _badDataDo;
            public DateTime? Bad_Data_Do { get => _badDataDo; set { if (_badDataDo != value) { _badDataDo = value; OnPropertyChanged(); } } }

            private string? _badTyp;
            public string? Bad_Typ { get => _badTyp; set { if (_badTyp != value) { _badTyp = value; OnPropertyChanged(); } } }

            private string? _pImie;
            public string? P_imie { get => _pImie; set { if (_pImie != value) { _pImie = value; OnPropertyChanged(); OnPropertyChanged(nameof(PacjentDisplay)); } } }

            private string? _pNazwisko;
            public string? P_nazwisko { get => _pNazwisko; set { if (_pNazwisko != value) { _pNazwisko = value; OnPropertyChanged(); OnPropertyChanged(nameof(PacjentDisplay)); } } }

            private string? _pZawod;
            public string? P_zawod { get => _pZawod; set { if (_pZawod != value) { _pZawod = value; OnPropertyChanged(); } } }

            private string? _firmaNazwa;
            public string? FirmaNazwa { get => _firmaNazwa; set { if (_firmaNazwa != value) { _firmaNazwa = value; OnPropertyChanged(); } } }

            private decimal? _badRazem;
            public decimal? Bad_Razem { get => _badRazem; set { if (_badRazem != value) { _badRazem = value; OnPropertyChanged(); } } }

            private decimal? _badCena1;
            public decimal? Bad_Cena1 { get => _badCena1; set { if (_badCena1 != value) { _badCena1 = value; OnPropertyChanged(); } } }

            private decimal? _badCena2;
            public decimal? Bad_Cena2 { get => _badCena2; set { if (_badCena2 != value) { _badCena2 = value; OnPropertyChanged(); } } }

            private decimal? _badCena3;
            public decimal? Bad_Cena3 { get => _badCena3; set { if (_badCena3 != value) { _badCena3 = value; OnPropertyChanged(); } } }

            private decimal? _badCena4;
            public decimal? Bad_Cena4 { get => _badCena4; set { if (_badCena4 != value) { _badCena4 = value; OnPropertyChanged(); } } }

            private decimal? _badCena5;
            public decimal? Bad_Cena5 { get => _badCena5; set { if (_badCena5 != value) { _badCena5 = value; OnPropertyChanged(); } } }

            private decimal? _badCena6;
            public decimal? Bad_Cena6 { get => _badCena6; set { if (_badCena6 != value) { _badCena6 = value; OnPropertyChanged(); } } }

            private decimal? _badCena7;
            public decimal? Bad_Cena7 { get => _badCena7; set { if (_badCena7 != value) { _badCena7 = value; OnPropertyChanged(); } } }

            private decimal? _badCena8;
            public decimal? Bad_Cena8 { get => _badCena8; set { if (_badCena8 != value) { _badCena8 = value; OnPropertyChanged(); } } }

            private string? _badNrKs;
            public string? Bad_Nr_KS { get => _badNrKs; set { if (_badNrKs != value) { _badNrKs = value; OnPropertyChanged(); } } }

            private bool? _badEnd;
            public bool? Bad_END { get => _badEnd; set { if (_badEnd != value) { _badEnd = value; OnPropertyChanged(); } } }

            // one-based row number for UI
            private int _lp;
            public int Lp { get => _lp; set { if (_lp != value) { _lp = value; OnPropertyChanged(); } } }

            public string PacjentDisplay => (P_imie ?? "") + " " + (P_nazwisko ?? "");

            // New: formatted display for referral/test date (used by ListaDoFaktur_EditView)
            public DateTime? DataSkierDate => Bad_Data;

            public string DataSkierDisplay => Bad_Data.HasValue ? Bad_Data.Value.ToString("dd.MM.yyyy") : string.Empty;

            private string? _badWynik;
            public string? Bad_Wynik { get => _badWynik; set { if (_badWynik != value) { _badWynik = value; OnPropertyChanged(); } } }

            public string? FirmaCennik { get; internal set; }
            public decimal? Bad_Cena9 { get; internal set; }
            public decimal? Bad_Cena10 { get; internal set; }
            public int? Bad_S_ID { get; internal set; }
            public int? Bad_P_ID { get; internal set; }
            public int? Bad_F_ID { get; internal set; }
            public string? Bad_bn_cennik { get; internal set; }
            public string? Bad_typ { get; internal set; }
        }

        /// <summary>
        /// Pobiera listę list badań (ListyBadan) z opcjonalnym filtrem po nazwie firmy lub numerze faktury.
        /// </summary>
        public List<ListyBadanDto> GetListyBadan(string? filter = null)
        {
            var result = new List<ListyBadanDto>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();

                var sql = @"SELECT
    ListyBadan.Identyfikator,
    ListyBadan.L_Firma_ID,
    Firma.Nazwa,
    Faktura.FK_Data,
    Faktura.FK_Numer,
    Faktura.FK_Kwota
FROM
    (
        ListyBadan
        LEFT JOIN Faktura ON ListyBadan.L_FK_ID = Faktura.FK_ID
    )
    INNER JOIN Firma ON ListyBadan.L_Firma_ID = Firma.id";

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    sql += " WHERE (Firma.Nazwa LIKE ?) OR (Faktura.FK_Numer LIKE ?)";
                }

                using var cmd = new OdbcCommand(sql, connection);
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    var pattern = "%" + filter + "%";
                    cmd.Parameters.AddWithValue("@p1", pattern);
                    cmd.Parameters.AddWithValue("@p2", pattern);
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new ListyBadanDto
                    {
                        Identyfikator = reader["Identyfikator"] is int id ? id : int.TryParse(reader["Identyfikator"]?.ToString(), out var id2) ? id2 : (int?)null,
                        Nazwa = reader["Nazwa"]?.ToString(),
                        FK_Data = reader["FK_Data"] != DBNull.Value ? Convert.ToDateTime(reader["FK_Data"]) : (DateTime?)null,
                        FK_Numer = reader["FK_Numer"]?.ToString(),
                        FK_Kwota = reader["FK_Kwota"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["FK_Kwota"]) : null,
                        L_Firma_ID = reader["L_Firma_ID"] is int fid ? fid : int.TryParse(reader["L_Firma_ID"]?.ToString(), out var fid2) ? fid2 : (int?)null
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania ListyBadan: {ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        /// <summary>
        /// Pobiera badania przypisane do danej listy (parametr Badanie.Bad_L_ID)
        /// ON Firma.id = Badanie.Bad_F_ID
        /// </summary>
        public List<AssignedBadanieDto> GetBadaniaForLista(int listaId)
        {
            var result = new List<AssignedBadanieDto>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();

                var sql = @"SELECT
    Badanie.Bad_ID,
    Badanie.Bad_L_ID,
    Badanie.Bad_Data,
    Badanie.Bad_Typ,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_zawód,
    Firma.Nazwa,
    Firma.Cennik,
    Badanie.Bad_S_ID,
    Badanie.Bad_P_ID,
    Badanie.Bad_F_ID,
    Badanie.Bad_bn_cennik,
    Badanie.Bad_Razem,
    Badanie.Bad_Cena1,
    Badanie.Bad_Cena2,
    Badanie.Bad_Cena3,
    Badanie.Bad_Cena4,
    Badanie.Bad_Cena5,
    Badanie.Bad_Cena6,
    Badanie.Bad_Cena7,
    Badanie.Bad_Cena8,
    Badanie.Bad_Cena9,
    Badanie.Bad_Cena10,
    Badanie.Bad_END,
    Badanie.Bad_Data_Do,
    Badanie.Bad_Wynik,
    Badanie.Bad_Nr_KS
FROM
    (
        (
            Badanie
            INNER JOIN P_Pacjent ON Badanie.Bad_P_ID = P_Pacjent.P_ID
        )
        INNER JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
    )
    LEFT JOIN ListyBadan ON Badanie.Bad_L_ID = ListyBadan.Identyfikator
WHERE
    (((Badanie.Bad_L_ID) = [?]));";

                using var cmd = new OdbcCommand(sql, connection);
                cmd.Parameters.AddWithValue("@listaId", listaId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    decimal? parseDec(object obj)
                    {
                        if (obj == null || obj == DBNull.Value) return null;
                        if (decimal.TryParse(obj.ToString(), out var d)) return d;
                        return null;
                    }

                    result.Add(new AssignedBadanieDto
                    {
                        Bad_ID = reader["Bad_ID"] is int idb ? idb : int.TryParse(reader["Bad_ID"]?.ToString(), out var idb2) ? idb2 : (int?)null,
                        Bad_L_ID = reader["Bad_L_ID"] is int id ? id : int.TryParse(reader["Bad_L_ID"]?.ToString(), out var id2) ? id2 : (int?)null,
                        Bad_Data = reader["Bad_Data"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data"]) : (DateTime?)null,
                        Bad_Typ = reader["Bad_Typ"]?.ToString(),
                        P_imie = reader["P_imie"]?.ToString(),
                        P_nazwisko = reader["P_nazwisko"]?.ToString(),
                        P_zawod = reader["P_zawód"]?.ToString(),
                        FirmaNazwa = reader["Nazwa"]?.ToString(),
                        FirmaCennik = reader["Cennik"]?.ToString(),
                        Bad_S_ID = reader["Bad_S_ID"] is int sid ? sid : int.TryParse(reader["Bad_S_ID"]?.ToString(), out var sid2) ? sid2 : (int?)null,
                        Bad_P_ID = reader["Bad_P_ID"] is int pid ? pid : int.TryParse(reader["Bad_P_ID"]?.ToString(), out var pid2) ? pid2 : (int?)null,
                        Bad_F_ID = reader["Bad_F_ID"] is int fid ? fid : int.TryParse(reader["Bad_F_ID"]?.ToString(), out var fid2) ? fid2 : (int?)null,
                        Bad_bn_cennik = reader["Bad_bn_cennik"]?.ToString(),
                        Bad_Razem = parseDec(reader["Bad_Razem"]),
                        Bad_Cena1 = parseDec(reader["Bad_Cena1"]),
                        Bad_Cena2 = parseDec(reader["Bad_Cena2"]),
                        Bad_Cena3 = parseDec(reader["Bad_Cena3"]),
                        Bad_Cena4 = parseDec(reader["Bad_Cena4"]),
                        Bad_Cena5 = parseDec(reader["Bad_Cena5"]),
                        Bad_Cena6 = parseDec(reader["Bad_Cena6"]),
                        Bad_Cena7 = parseDec(reader["Bad_Cena7"]),
                        Bad_Cena8 = parseDec(reader["Bad_Cena8"]),
                        Bad_Cena9 = parseDec(reader["Bad_Cena9"]),
                        Bad_Cena10 = parseDec(reader["Bad_Cena10"]),
                        Bad_END = reader["Bad_END"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["Bad_END"]) : null,
                        Bad_Data_Do = reader["Bad_Data_Do"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data_Do"]) : (DateTime?)null,
                        Bad_Wynik = reader["Bad_Wynik"]?.ToString(),
                        Bad_Nr_KS = reader["Bad_Nr_KS"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania badań dla listy: {ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        /// <summary>
        /// Ustawia pole Bad_L_ID = NULL dla rekordu Badanie (usuwa powiązanie z listą badań)
        /// </summary>
        public bool UnassignBadanieFromLista(int badId, string v)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = new System.Data.Odbc.OdbcCommand(
                    "UPDATE Badanie SET Bad_L_ID = 0, Bad_F_ID = 0, Bad_Fakt = NULL WHERE Bad_ID = ?",
                    connection);

                // ODBC używa pozycyjnych parametrów '?'. Nazwa parametru nie jest wymagana.
                cmd.Parameters.AddWithValue("@p1", badId);

                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odłączenia badania od listy: {ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Aktualizuje rekord skierowania (B_Skierowania) na podstawie przekazanego rekordu
        /// </summary>
        public bool UpdateSkierowanie(int patientSkierowanieId, SkierowanieRecord rec)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE B_Skierowania SET
                B_Pacjent_ID = ?,
                B_Firma_ID = ?,
                B_DataSkierowania = ?,
                B_TypBadania = ?,
                B_Stanowisko = ?,
                B_RegistrationDate = ?,
                B_czynnik_fizyczny = ?,
                B_czynnik_fizyczny_opis = ?,
                B_czynnik_pyłowy = ?,
                B_czynnik_pyłowy_opis = ?,
                B_czynnik_chemiczny = ?,
                B_czynnik_chemiczny_opis = ?,
                B_czynnik_biologiczny = ?,
                B_czynnik_biologiczny_opis = ?,
                B_czynnik_inny = ?,
                B_czynnik_inny_opis = ?,
                B_czynnik_sanepid = ?,
                B_czynnik_sanepid_opis = ?,
                B_Zaswiadczenie = ?,
                B_książeczka = ?,
                B_Ankieta = ?,
                B_Nowe = ?,
                B_Activ = ?
            WHERE B_ID = ?";

                cmd.Parameters.AddWithValue("@B_Pacjent_ID", rec.PacjentId.HasValue ? (object)rec.PacjentId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@B_Firma_ID", rec.FirmaId.HasValue ? (object)rec.FirmaId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@B_DataSkierowania", rec.DataSkierowania.HasValue ? (object)rec.DataSkierowania.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@B_TypBadania", rec.TypBadania ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@B_Stanowisko", rec.Stanowisko ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@B_RegistrationDate", rec.RegistrationDate.HasValue ? (object)rec.RegistrationDate.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@B_czynnik_fizyczny", rec.CzynnikFizyczny);
                cmd.Parameters.AddWithValue("@B_czynnik_fizyczny_opis", rec.CzynnikFizycznyOpis ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@B_czynnik_pyłowy", rec.CzynnikPylowy);
                cmd.Parameters.AddWithValue("@B_czynnik_pyłowy_opis", rec.CzynnikPylowyOpis ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@B_czynnik_chemiczny", rec.CzynnikChemiczny);
                cmd.Parameters.AddWithValue("@B_czynnik_chemiczny_opis", rec.CzynnikChemicznyOpis ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@B_czynnik_biologiczny", rec.CzynnikBiologiczny);
                cmd.Parameters.AddWithValue("@B_czynnik_biologiczny_opis", rec.CzynnikBiologicznyOpis ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@B_czynnik_inny", rec.CzynnikInny);
                cmd.Parameters.AddWithValue("@B_czynnik_inny_opis", rec.CzynnikInnyOpis ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@B_czynnik_sanepid", rec.CzynnikSanepid);
                cmd.Parameters.AddWithValue("@B_czynnik_sanepid_opis", rec.CzynnikSanepidOpis ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@B_Zaswiadczenie", rec.Zaswiadczenie);
                cmd.Parameters.AddWithValue("@B_książeczka", rec.Ksiazeczka);
                cmd.Parameters.AddWithValue("@B_Ankieta", rec.Ankieta);
                cmd.Parameters.AddWithValue("@B_Nowe", rec.Nowe);
                cmd.Parameters.AddWithValue("@B_Activ", rec.Activ);
                cmd.Parameters.AddWithValue("@B_ID", patientSkierowanieId);

                int rows = cmd.ExecuteNonQuery();
                if (rows > 0) NotificationHelper.ShowInfo("Skierowanie zaktualizowane", $"B_ID = {patientSkierowanieId}");
                return rows > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd aktualizacji skierowania:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Simple helper result for cennik mapping used by some views
        public class CennikPrices
        {
            public decimal? CenaPodstawowa { get; set; }
            public decimal? CenaLaryngolog { get; set; }
            public decimal? CenaOkulista { get; set; }
            public decimal? CenaSanitariusz { get; set; }
            public decimal? CenaLipidogram { get; set; }
            public decimal? CenaEKG { get; set; }
            public decimal? CenaPoradnia { get; set; }
            public decimal? CenaInne { get; set; }
        }

        // Map cennik name to price structure via WizytyRepository
        public CennikPrices? GetCennikByName(string bnCennik)
        {
            try
            {
                var repo = new WizytyRepository();
                var prices = repo.GetCennikPrices(bnCennik);
                // System.Diagnostics.Debug.WriteLine($"AccessDbContext.GetCennikByName: bnCennik='{bnCennik}', prices.Count={prices?.Count ?? 0}");
                if (prices == null || prices.Count == 0) return null;
                decimal? get(string[] keys)
                {
                    foreach (var k in keys)
                    {
                        var matchingKey = prices.Keys.FirstOrDefault(dbKey =>
                            dbKey.ToLower().Contains(k.ToLower()) ||
                            dbKey.ToLower().Replace(".", "").Contains(k.ToLower()));

                        if (matchingKey != null && prices.TryGetValue(matchingKey, out var v))
                        {
                            // System.Diagnostics.Debug.WriteLine($"  Matched '{k}' -> '{matchingKey}' = {v}");
                            return v;
                        }
                    }
                    // System.Diagnostics.Debug.WriteLine($"  No match for keys: {string.Join(", ", keys)}");
                    return null;
                }

                return new CennikPrices
                {
                    CenaPodstawowa = get(new[] { "lekarz", "lekasz", "MP", "basic" }),
                    CenaLaryngolog = get(new[] { "laryngolog" }),
                    CenaOkulista = get(new[] { "okulista", "okulist" }),
                    CenaSanitariusz = get(new[] { "ksi", "książeczka", "ksiazeczka" }),
                    CenaLipidogram = get(new[] { "lipidogram" }),
                    CenaEKG = get(new[] { "ekg" }),
                    CenaPoradnia = get(new[] { "urlop", "zdrowie", "healthclinic" }),
                    CenaInne = get(new[] { "inne", "other" })
                };
            }
            catch { return null; }
        }

        internal void UpdateSkierowanieBadanieId(int value1, object value2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// wczytanie rekordów z tabeli archiwum Lx_Listy_do_faktur--old-baza
        /// 

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

            // Dodatkowe pola cenowe z archiwum (jeśli będą potrzebne do importu)
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

        public List<ArchiveListRecord> GetArchiveListRecords(string? filter)
        {
            // System.Diagnostics.Debug.WriteLine("=== GetArchiveListRecords WYWOŁANA ===");
            var result = new List<ArchiveListRecord>();

            try
            {
                // System.Diagnostics.Debug.WriteLine("GetArchiveListRecords: Tworzenie AccessDbHelper...");
                var dbHelper = new AccessDbHelper();

                // System.Diagnostics.Debug.WriteLine("GetArchiveListRecords: Pobieranie połączenia...");
                using var connection = dbHelper.GetConnection();

                // System.Diagnostics.Debug.WriteLine($"GetArchiveListRecords: ConnectionString = {connection.ConnectionString}");

                // System.Diagnostics.Debug.WriteLine("GetArchiveListRecords: Otwieranie połączenia...");
                connection.Open();

                // System.Diagnostics.Debug.WriteLine("GetArchiveListRecords: ✅ Połączenie otwarte!");

                // ✅ UPROSZCZONE zapytanie SQL (zgodne z działającym zapytaniem z Access)
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

                // Dodaj filtr jeśli podano
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

                // System.Diagnostics.Debug.WriteLine($"GetArchiveListRecords SQL:\n{sql}");

                using var cmd = new OdbcCommand(sql, connection);

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    var pattern = "%" + filter + "%";
                    cmd.Parameters.AddWithValue("@p1", pattern);
                    cmd.Parameters.AddWithValue("@p2", pattern);
                    cmd.Parameters.AddWithValue("@p3", pattern);
                    cmd.Parameters.AddWithValue("@p4", pattern);
                    // System.Diagnostics.Debug.WriteLine($"GetArchiveListRecords: Filter pattern = '{pattern}'");
                }

                // System.Diagnostics.Debug.WriteLine("GetArchiveListRecords: Wykonywanie zapytania...");
                using var reader = cmd.ExecuteReader();

                int rowCount = 0;
                while (reader.Read())
                {
                    rowCount++;

                    decimal? ParseDecimal(object obj)
                    {
                        if (obj == null || obj == DBNull.Value) return null;
                        if (decimal.TryParse(obj.ToString(), out var d)) return d;
                        return null;
                    }

                    int? ParseInt(object obj)
                    {
                        if (obj == null || obj == DBNull.Value) return null;
                        if (int.TryParse(obj.ToString(), out var i)) return i;
                        return null;
                    }

                    var record = new ArchiveListRecord
                    {
                        Identyfikator = ParseInt(reader["Identyfikator"]) ?? 0,
                        Lx_ID_Faktura = ParseInt(reader["Lx_ID_Faktura"]),
                        Lx_ID_Firma = ParseInt(reader["Lx_ID_Firma"]),
                        Lx_ID_pacjent = ParseInt(reader["Lx_ID_pacjent"]),
                        Lx_ID_Skierowania = ParseInt(reader["Lx_ID_Skierowania"]),
                        Lx_ID_Badania = ParseInt(reader["Lx_ID_Badania"]) ?? 0,

                        Lx_Data = reader["Lx_Data"] != DBNull.Value ? Convert.ToDateTime(reader["Lx_Data"]) : (DateTime?)null,

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

                    // Debug pierwszych 3 rekordów
                    if (rowCount <= 3)
                    {
                        // System.Diagnostics.Debug.WriteLine($"Rekord #{rowCount}: ID={record.Identyfikator}, Pacjent={record.PacjentDisplay}, Kwota={record.Lx_Razem}");
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"GetArchiveListRecords: ✅ Znaleziono {result.Count} rekordów z archiwum");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"GetArchiveListRecords ERROR: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // ✅ RZUĆ WYJĄTEK DALEJ aby ViewModel zobaczył szczegóły
                throw new Exception($"Błąd pobierania archiwum: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// ✅ Wstawia badanie z rekordu archiwum i zwraca nowy Bad_ID
        /// WALIDACJA HIERARCHICZNA (4 poziomy):
        /// 1. Bezpośrednie ID z archiwum
        /// 2. Imię + Nazwisko + ID Firmy
        /// 3. Imię + Nazwisko (bez firmy)
        /// 4. Nazwisko + ID Firmy (fallback)
        /// </summary>
        public int InsertBadanieFromArchive(ArchiveListRecord record)
        {
            int newId = 0;
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                // ✅ WALIDACJA ID PACJENTA (4-poziomowa hierarchia)
                int? validatedPacjentId = ValidatePacjentId(record, conn);

                if (!validatedPacjentId.HasValue)
                {
                    // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: ❌ BRAK WALIDACJI dla archiwum #{record.Identyfikator} - {record.Lx_Imie} {record.Lx_Nazwisko} (Lx_ID_pacjent={record.Lx_ID_pacjent})");
                    return 0; // Nie dodajemy badania bez pacjenta
                }

                // ✅ WALIDACJA ID SKIEROWANIA (opcjonalne)
                int? validatedSkierowanieId = ValidateSkierowanieId(record, conn, validatedPacjentId.Value);

                // ✅ INSERT do tabeli Badanie
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO Badanie (
                    Bad_S_ID, Bad_P_ID, Bad_Cena1, Bad_Cena2, Bad_Cena3, Bad_Cena4, 
                    Bad_Cena5, Bad_Cena6, Bad_Cena7, Bad_Cena8, Bad_Cena9, 
                    Bad_Razem, Bad_Data, Bad_Data_Do
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                var p1 = cmd.CreateParameter(); p1.Value = validatedSkierowanieId.HasValue ? (object)validatedSkierowanieId.Value : DBNull.Value; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = validatedPacjentId.Value; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = record.Lx_Cena1.HasValue ? (object)record.Lx_Cena1.Value : DBNull.Value; cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.Value = record.Lx_Cena2.HasValue ? (object)record.Lx_Cena2.Value : DBNull.Value; cmd.Parameters.Add(p4);
                var p5 = cmd.CreateParameter(); p5.Value = record.Lx_Cena3.HasValue ? (object)record.Lx_Cena3.Value : DBNull.Value; cmd.Parameters.Add(p5);
                var p6 = cmd.CreateParameter(); p6.Value = record.Lx_Cena4.HasValue ? (object)record.Lx_Cena4.Value : DBNull.Value; cmd.Parameters.Add(p6);
                var p7 = cmd.CreateParameter(); p7.Value = record.Lx_Cena5.HasValue ? (object)record.Lx_Cena5.Value : DBNull.Value; cmd.Parameters.Add(p7);
                var p8 = cmd.CreateParameter(); p8.Value = record.Lx_Cena6.HasValue ? (object)record.Lx_Cena6.Value : DBNull.Value; cmd.Parameters.Add(p8);
                var p9 = cmd.CreateParameter(); p9.Value = record.Lx_Cena7.HasValue ? (object)record.Lx_Cena7.Value : DBNull.Value; cmd.Parameters.Add(p9);
                var p10 = cmd.CreateParameter(); p10.Value = 0m; cmd.Parameters.Add(p10); // Bad_Cena8
                var p11 = cmd.CreateParameter(); p11.Value = record.Lx_Cena9.HasValue ? (object)record.Lx_Cena9.Value : DBNull.Value; cmd.Parameters.Add(p11);
                var p12 = cmd.CreateParameter(); p12.Value = record.Lx_Razem.HasValue ? (object)record.Lx_Razem.Value : DBNull.Value; cmd.Parameters.Add(p12);
                var p13 = cmd.CreateParameter(); p13.Value = record.Lx_Data.HasValue ? (object)record.Lx_Data.Value : DBNull.Value; cmd.Parameters.Add(p13);
                var p14 = cmd.CreateParameter(); p14.Value = record.Lx_Data.HasValue ? (object)record.Lx_Data.Value : DBNull.Value; cmd.Parameters.Add(p14);

                cmd.ExecuteNonQuery();

                // Pobierz nowo utworzony Bad_ID
                using var idCmd = conn.CreateCommand();
                idCmd.CommandText = "SELECT @@IDENTITY";
                var obj = idCmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var id))
                    newId = id;

                // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive: ✅ Dodano Bad_ID={newId} dla P_ID={validatedPacjentId} (archiwum #{record.Identyfikator})");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"InsertBadanieFromArchive ERROR: {ex.Message}");
            }
            return newId;
        }

        /// <summary>
        /// ✅ HIERARCHICZNA WALIDACJA ID PACJENTA (4 poziomy):
        /// 1. Sprawdź czy Lx_ID_pacjent ISTNIEJE w P_Pacjent (P_activ = True)
        /// 2. Dopasuj po Imię + Nazwisko + ID Firmy (najbardziej precyzyjne)
        /// 3. Dopasuj po Imię + Nazwisko (bez weryfikacji firmy)
        /// 4. Dopasuj po Nazwisko + ID Firmy (fallback dla błędów w imieniu)
        /// </summary>
        private int? ValidatePacjentId(ArchiveListRecord record, OdbcConnection conn)
        {
            // ═══════════════════════════════════════════════════════════
            // 1️⃣ POZIOM 1: Sprawdź czy Lx_ID_pacjent ISTNIEJE w P_Pacjent
            // ═══════════════════════════════════════════════════════════
            if (record.Lx_ID_pacjent.HasValue && record.Lx_ID_pacjent.Value > 0)
            {
                try
                {
                    using var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = "SELECT P_ID FROM P_Pacjent WHERE P_ID = ? AND P_activ = True";
                    var p = checkCmd.CreateParameter();
                    p.Value = record.Lx_ID_pacjent.Value;
                    checkCmd.Parameters.Add(p);

                    var obj = checkCmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var existingId))
                    {
                        // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ✅ POZIOM 1 - Lx_ID_pacjent={existingId} ISTNIEJE w P_Pacjent");
                        return existingId;
                    }

                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ⚠️ POZIOM 1 - Lx_ID_pacjent={record.Lx_ID_pacjent} NIE ISTNIEJE lub nieaktywny, przechodzę do dopasowania...");
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: POZIOM 1 ERROR - {ex.Message}");
                }
            }

            // ═══════════════════════════════════════════════════════════
            // 2️⃣ POZIOM 2: Dopasuj po Imię + Nazwisko + ID Firmy
            // ═══════════════════════════════════════════════════════════
            if (!string.IsNullOrWhiteSpace(record.Lx_Imie) &&
                !string.IsNullOrWhiteSpace(record.Lx_Nazwisko) &&
                record.Lx_ID_Firma.HasValue && record.Lx_ID_Firma.Value > 0)
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

                    var p1 = searchCmd.CreateParameter(); p1.Value = record.Lx_Imie; searchCmd.Parameters.Add(p1);
                    var p2 = searchCmd.CreateParameter(); p2.Value = record.Lx_Nazwisko; searchCmd.Parameters.Add(p2);
                    var p3 = searchCmd.CreateParameter(); p3.Value = record.Lx_ID_Firma.Value; searchCmd.Parameters.Add(p3);

                    var obj = searchCmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var foundId))
                    {
                        // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ✅ POZIOM 2 - Znaleziono P_ID={foundId} po Imię+Nazwisko+Firma: '{record.Lx_Imie}' '{record.Lx_Nazwisko}' FirmaID={record.Lx_ID_Firma}");
                        return foundId;
                    }

                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ⚠️ POZIOM 2 - Brak dopasowania po Imię+Nazwisko+Firma");
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: POZIOM 2 ERROR - {ex.Message}");
                }
            }

            // ═══════════════════════════════════════════════════════════
            // 3️⃣ POZIOM 3: Dopasuj po Imię + Nazwisko (bez firmy)
            // ═══════════════════════════════════════════════════════════
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

                    var p1 = searchCmd.CreateParameter(); p1.Value = record.Lx_Imie; searchCmd.Parameters.Add(p1);
                    var p2 = searchCmd.CreateParameter(); p2.Value = record.Lx_Nazwisko; searchCmd.Parameters.Add(p2);

                    var obj = searchCmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var foundId))
                    {
                        // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ✅ POZIOM 3 - Znaleziono P_ID={foundId} po Imię+Nazwisko: '{record.Lx_Imie}' '{record.Lx_Nazwisko}' (bez weryfikacji firmy)");
                        return foundId;
                    }

                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ⚠️ POZIOM 3 - Brak dopasowania po Imię+Nazwisko");
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: POZIOM 3 ERROR - {ex.Message}");
                }
            }

            // ═══════════════════════════════════════════════════════════
            // 4️⃣ POZIOM 4: Dopasuj po Nazwisko + ID Firmy (fallback)
            // ═══════════════════════════════════════════════════════════
            if (!string.IsNullOrWhiteSpace(record.Lx_Nazwisko) &&
                record.Lx_ID_Firma.HasValue && record.Lx_ID_Firma.Value > 0)
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

                    var p1 = searchCmd.CreateParameter(); p1.Value = record.Lx_Nazwisko; searchCmd.Parameters.Add(p1);
                    var p2 = searchCmd.CreateParameter(); p2.Value = record.Lx_ID_Firma.Value; searchCmd.Parameters.Add(p2);

                    var obj = searchCmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var foundId))
                    {
                        // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ⚠️ POZIOM 4 - Znaleziono P_ID={foundId} po Nazwisko+Firma: '{record.Lx_Nazwisko}' FirmaID={record.Lx_ID_Firma} (UWAGA: bez weryfikacji imienia!)");
                        return foundId;
                    }

                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ⚠️ POZIOM 4 - Brak dopasowania po Nazwisko+Firma");
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: POZIOM 4 ERROR - {ex.Message}");
                }
            }

            // ═══════════════════════════════════════════════════════════
            // ❌ BRAK DOPASOWANIA - Zaloguj szczegóły
            // ═══════════════════════════════════════════════════════════
            // System.Diagnostics.Debug.WriteLine($"ValidatePacjentId: ❌ BRAK DOPASOWANIA dla:");
            // System.Diagnostics.Debug.WriteLine($"  - Lx_ID_pacjent: {record.Lx_ID_pacjent}");
            // System.Diagnostics.Debug.WriteLine($"  - Lx_Imie: '{record.Lx_Imie}'");
            // System.Diagnostics.Debug.WriteLine($"  - Lx_Nazwisko: '{record.Lx_Nazwisko}'");
            // System.Diagnostics.Debug.WriteLine($"  - Lx_ID_Firma: {record.Lx_ID_Firma}");
            // System.Diagnostics.Debug.WriteLine($"  - Lx_Firma: '{record.Lx_Firma}'");

            return null;
        }

        /// <summary>
        /// ✅ Waliduje ID skierowania z archiwum (opcjonalne - może być null)
        /// </summary>
        private int? ValidateSkierowanieId(ArchiveListRecord record, OdbcConnection conn, int pacjentId)
        {
            if (!record.Lx_ID_Skierowania.HasValue)
                return null;

            try
            {
                // Sprawdź czy ID skierowania istnieje dla danego pacjenta
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT B_ID FROM B_Skierowania WHERE B_ID = ? AND B_Pacjent_ID = ?";
                var p1 = checkCmd.CreateParameter(); p1.Value = record.Lx_ID_Skierowania.Value; checkCmd.Parameters.Add(p1);
                var p2 = checkCmd.CreateParameter(); p2.Value = pacjentId; checkCmd.Parameters.Add(p2);

                var obj = checkCmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var validId))
                {
                    // System.Diagnostics.Debug.WriteLine($"ValidateSkierowanieId: ✅ Lx_ID_Skierowania={validId} ISTNIEJE dla P_ID={pacjentId}");
                    return validId;
                }

                // System.Diagnostics.Debug.WriteLine($"ValidateSkierowanieId: ⚠️ Lx_ID_Skierowania={record.Lx_ID_Skierowania} NIE ISTNIEJE dla P_ID={pacjentId}, ustawiam NULL");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ValidateSkierowanieId ERROR: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Aktualizuje Lx_ID_Badania w archiwum
        /// </summary>
        public bool UpdateArchiveBadanieId(int archiveId, int badId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE [Lx_Listy_do_faktur--old-baza] SET Lx_ID_Badania = ? WHERE Identyfikator = ?";
                var p1 = cmd.CreateParameter(); p1.Value = badId; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = archiveId; cmd.Parameters.Add(p2);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateArchiveBadanieId error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Tworzy rekord ListyBadan i zwraca nowy Identyfikator
        /// </summary>
        public int CreateListaBadan(int fakturaId, int? firmaId, DateTime? data)
        {
            int newId = 0;
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = "INSERT INTO ListyBadan (L_Firma_ID, L_FK_ID, L_Data) VALUES (?, ?, ?)";
                var p1 = cmd.CreateParameter(); p1.Value = firmaId.HasValue ? (object)firmaId.Value : DBNull.Value; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = fakturaId; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = data.HasValue ? (object)data.Value : DBNull.Value; cmd.Parameters.Add(p3);

                cmd.ExecuteNonQuery();

                using var idCmd = conn.CreateCommand();
                idCmd.CommandText = "SELECT @@IDENTITY";
                var obj = idCmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var id)) newId = id;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CreateListaBadan error: {ex}");
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
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE [Lx_Listy_do_faktur--old-baza] SET Lx_ID_listy = ? WHERE Lx_ID_Faktura = ?";
                var p1 = cmd.CreateParameter(); p1.Value = listaId; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = fakturaId; cmd.Parameters.Add(p2);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateArchiveWithListaId error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Aktualizuje Bad_L_ID i Bad_F_ID w tabeli Badanie
        /// </summary>
        public bool UpdateBadanieWithListaAndFaktura(int badId, int listaId, int fakturaId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Badanie SET Bad_L_ID = ?, Bad_F_ID = ? WHERE Bad_ID = ?";
                var p1 = cmd.CreateParameter(); p1.Value = listaId; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = fakturaId; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = badId; cmd.Parameters.Add(p3);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateBadanieWithListaAndFaktura error: {ex}");
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
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Faktura SET FK_Num_Listy = ?, FK_Suma_Bad = ?, FK_Status = ? WHERE FK_ID = ?";
                var p1 = cmd.CreateParameter(); p1.Value = listaId; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = suma; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = 2; cmd.Parameters.Add(p3); // Status = 2 (Lista)
                var p4 = cmd.CreateParameter(); p4.Value = fakturaId; cmd.Parameters.Add(p4);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateFakturaWithListaSummary error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Oznacza rekordy w archiwum jako przetworzone (Lx_End = True)
        /// </summary>
        public bool MarkArchiveRecordsAsProcessed(List<int> identifiers)
        {
            if (identifiers == null || identifiers.Count == 0) return false;

            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                foreach (var id in identifiers)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE [Lx_Listy_do_faktur--old-baza] SET Lx_End = ? WHERE Identyfikator = ?";
                    var p1 = cmd.CreateParameter(); p1.Value = true; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.Value = id; cmd.Parameters.Add(p2);
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"MarkArchiveRecordsAsProcessed error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Pobiera listę badań ze skierowaniami (JOIN B_Skierowania + Badanie + P_Pacjent + Firma)
        /// WHERE B_Badanie_ID > 0 (tylko badania z przypisanym Bad_ID)
        /// </summary>
        public List<BadanieWithSkierowanieDto> GetBadaniaWithSkierowania()
        {
            var result = new List<BadanieWithSkierowanieDto>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();

                var sql = @"
SELECT
    Badanie.Bad_ID,
    Badanie.Bad_Data,
    Badanie.Bad_Data_Do,
    Badanie.Bad_Wynik,
    Badanie.Bad_Nr_KS,
    Badanie.Bad_bn_cennik,
    Badanie.Bad_Fakt,
    Badanie.Bad_Razem,
    Badanie.Bad_Cena1,
    Badanie.Bad_Cena2,
    Badanie.Bad_Cena3,
    Badanie.Bad_Cena4,
    Badanie.Bad_Cena5,
    Badanie.Bad_Cena6,
    Badanie.Bad_Cena7,
    Badanie.Bad_Cena8,
    Badanie.Bad_Cena9,
    Badanie.Bad_Cena10,
    B_Skierowania.B_ID,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_książeczka,
    B_Zaswiadczenie,
    P_Pacjent.P_ID,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_pesel,
    P_Pacjent.P_zawód,
    P_Pacjent.P_Firma_id,
    Firma.Nazwa AS Firma_Nazwa,
    Firma.NIP AS Firma_NIP,
    Firma.Cennik AS Firma_Cennik
FROM
    (
        (
            Badanie
            INNER JOIN B_Skierowania ON Badanie.Bad_S_ID = B_Skierowania.B_ID
        )
        INNER JOIN P_Pacjent ON B_Skierowania.B_Pacjent_ID = P_Pacjent.P_ID
    )
    INNER JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
WHERE
    Badanie.Bad_ID > 0
ORDER BY
    Badanie.Bad_Data DESC;";

                using var cmd = new OdbcCommand(sql, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    decimal? ParseDecimal(object obj)
                    {
                        if (obj == null || obj == DBNull.Value) return null;
                        if (decimal.TryParse(obj.ToString(), out var d)) return d;
                        return null;
                    }

                    var dto = new BadanieWithSkierowanieDto
                    {
                        // Badanie
                        Bad_ID = reader["Bad_ID"] is int badId ? badId : (int.TryParse(reader["Bad_ID"]?.ToString(), out var badId2) ? badId2 : 0),
                        Bad_Data = reader["Bad_Data"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data"]) : (DateTime?)null,
                        Bad_Data_Do = reader["Bad_Data_Do"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data_Do"]) : (DateTime?)null,
                        Bad_Wynik = reader["Bad_Wynik"]?.ToString(),
                        Bad_Nr_KS = reader["Bad_Nr_KS"]?.ToString(),
                        Bad_bn_cennik = reader["Bad_bn_cennik"]?.ToString(),
                        Bad_Fakt = reader["Bad_Fakt"]?.ToString(),
                        Bad_Razem = ParseDecimal(reader["Bad_Razem"]),
                        Bad_Cena1 = ParseDecimal(reader["Bad_Cena1"]),
                        Bad_Cena2 = ParseDecimal(reader["Bad_Cena2"]),
                        Bad_Cena3 = ParseDecimal(reader["Bad_Cena3"]),
                        Bad_Cena4 = ParseDecimal(reader["Bad_Cena4"]),
                        Bad_Cena5 = ParseDecimal(reader["Bad_Cena5"]),
                        Bad_Cena6 = ParseDecimal(reader["Bad_Cena6"]),
                        Bad_Cena7 = ParseDecimal(reader["Bad_Cena7"]),
                        Bad_Cena8 = ParseDecimal(reader["Bad_Cena8"]),
                        Bad_Cena9 = ParseDecimal(reader["Bad_Cena9"]),
                        Bad_Cena10 = ParseDecimal(reader["Bad_Cena10"]),

                        // Skierowanie
                        B_ID = reader["B_ID"] is int bId ? bId : (int.TryParse(reader["B_ID"]?.ToString(), out var bId2) ? bId2 : 0),
                        B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : (DateTime?)null,
                        B_TypBadania = reader["B_TypBadania"]?.ToString(),
                        B_ksiazeczka = reader["B_książeczka"] != DBNull.Value ? Convert.ToBoolean(reader["B_książeczka"]) : (bool?)null,
                        B_Zaswiadczenie = reader["B_Zaswiadczenie"] != DBNull.Value ? Convert.ToBoolean(reader["B_Zaswiadczenie"]) : (bool?)null,

                        // Pacjent
                        P_ID = reader["P_ID"] is int pId ? pId : (int.TryParse(reader["P_ID"]?.ToString(), out var pId2) ? pId2 : 0),
                        P_imie = reader["P_imie"]?.ToString(),
                        P_nazwisko = reader["P_nazwisko"]?.ToString(),
                        P_pesel = reader["P_pesel"]?.ToString(),
                        P_zawod = reader["P_zawód"]?.ToString(),

                        // Firma
                        Firma_id = reader["P_Firma_id"] is int fId ? fId : (int.TryParse(reader["P_Firma_id"]?.ToString(), out var fId2) ? fId2 : 0),
                        Firma_Nazwa = reader["Firma_Nazwa"]?.ToString(),
                        Firma_NIP = reader["Firma_NIP"]?.ToString(),
                        Firma_Cennik = reader["Firma_Cennik"]?.ToString()
                    };

                    result.Add(dto);
                }

                // System.Diagnostics.Debug.WriteLine($"GetBadaniaWithSkierowania: Loaded {result.Count} badań");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"GetBadaniaWithSkierowania error: {ex}");
                MessageBox.Show($"Błąd pobierania badań: {ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        /// <summary>
        /// DTO dla badania z danymi skierowania, pacjenta i firmy
        /// </summary>
        public class BadanieWithSkierowanieDto
        {
            // Badanie
            public int Bad_ID { get; set; }
            public DateTime? Bad_Data { get; set; }
            public DateTime? Bad_Data_Do { get; set; }
            public string? Bad_Wynik { get; set; }
            public string? Bad_Nr_KS { get; set; }
            public string? Bad_bn_cennik { get; set; }
            public decimal? Bad_Razem { get; set; }
            public decimal? Bad_Cena1 { get; set; }
            public decimal? Bad_Cena2 { get; set; }
            public decimal? Bad_Cena3 { get; set; }
            public decimal? Bad_Cena4 { get; set; }
            public decimal? Bad_Cena5 { get; set; }
            public decimal? Bad_Cena6 { get; set; }
            public decimal? Bad_Cena7 { get; set; }
            public decimal? Bad_Cena8 { get; set; }
            public decimal? Bad_Cena9 { get; set; }
            public decimal? Bad_Cena10 { get; set; }

            // Skierowanie
            public int B_ID { get; set; }
            public DateTime? B_DataSkierowania { get; set; }
            public string? B_TypBadania { get; set; }
            public bool? B_ksiazeczka { get; set; }
            public bool? B_Zaswiadczenie { get; set; }

            // Pacjent
            public int P_ID { get; set; }
            public string? P_imie { get; set; }
            public string? P_nazwisko { get; set; }
            public string? P_pesel { get; set; }
            public string? P_zawod { get; set; }

            // Firma
            public int Firma_id { get; set; }
            public string? Firma_Nazwa { get; set; }
            public string? Firma_NIP { get; set; }
            public string? Firma_Cennik { get; set; }
            public string? Bad_Fakt { get; internal set; }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ USERS - System logowania użytkowników
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Tworzy tabelę Users jeśli nie istnieje
        /// </summary>
        public void CreateUsersTableIfNotExists()
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                // Sprawdź czy tabela istnieje
                var schema = conn.GetSchema("Tables");
                bool tableExists = false;
                foreach (System.Data.DataRow row in schema.Rows)
                {
                    if (row["TABLE_NAME"]?.ToString()?.Equals("Users", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        tableExists = true;
                        break;
                    }
                }

                if (!tableExists)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        CREATE TABLE Users (
                            Id AUTOINCREMENT PRIMARY KEY,
                            Username VARCHAR(50) NOT NULL,
                            PasswordHash VARCHAR(255) NOT NULL,
                            Email VARCHAR(100),
                            FullName VARCHAR(100),
                            Role INTEGER NOT NULL,
                            IsActive YESNO NOT NULL,
                            CreatedDate DATETIME,
                            LastLogin DATETIME
                        )";
                    cmd.ExecuteNonQuery();

                    // System.Diagnostics.Debug.WriteLine("✅ Tabela Users utworzona");
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CreateUsersTableIfNotExists error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Tworzy tabelę LoginHistory jeśli nie istnieje
        /// </summary>
        public void CreateLoginHistoryTableIfNotExists()
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                // Sprawdź czy tabela istnieje
                var schema = conn.GetSchema("Tables");
                bool tableExists = false;
                foreach (System.Data.DataRow row in schema.Rows)
                {
                    if (row["TABLE_NAME"]?.ToString()?.Equals("LoginHistory", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        tableExists = true;
                        break;
                    }
                }

                if (!tableExists)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        CREATE TABLE LoginHistory (
                            Id AUTOINCREMENT PRIMARY KEY,
                            UserId INTEGER,
                            Username VARCHAR(50),
                            LoginTime DATETIME NOT NULL,
                            LogoutTime DATETIME,
                            ComputerName VARCHAR(100),
                            IpAddress VARCHAR(50),
                            Success YESNO NOT NULL,
                            FailureReason VARCHAR(255)
                        )";
                    cmd.ExecuteNonQuery();

                    // System.Diagnostics.Debug.WriteLine("✅ Tabela LoginHistory utworzona");
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CreateLoginHistoryTableIfNotExists error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Dodaje kolumnę EndLogin do tabeli Users jeśli nie istnieje
        /// </summary>
        private void AddEndLoginColumnIfNotExists()
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                // Sprawdź czy kolumna EndLogin istnieje
                try
                {
                    using var testCmd = conn.CreateCommand();
                    testCmd.CommandText = "SELECT TOP 1 EndLogin FROM Users";
                    testCmd.ExecuteScalar();
                    // System.Diagnostics.Debug.WriteLine("✅ Kolumna EndLogin już istnieje");
                    return; // Kolumna istnieje
                }
                catch
                {
                    // Kolumna nie istnieje - dodajemy ją
                    // System.Diagnostics.Debug.WriteLine("⚠️ Kolumna EndLogin nie istnieje - dodaję...");
                }

                // Dodaj kolumnę EndLogin
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "ALTER TABLE Users ADD COLUMN EndLogin DATETIME";
                cmd.ExecuteNonQuery();

                // System.Diagnostics.Debug.WriteLine("✅ Kolumna EndLogin dodana do tabeli Users");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"AddEndLoginColumnIfNotExists error: {ex.Message}");
                // Nie rzucamy wyjątku - aplikacja może działać bez EndLogin
            }
        }

        /// <summary>
        /// Inicjalizuje super admina (tesla/2025) jeśli baza jest pusta
        /// </summary>
        public void InitializeSuperAdmin()
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                // ✅ KLUCZOWE: Dodaj kolumnę EndLogin jeśli nie istnieje
                AddEndLoginColumnIfNotExists();

                // ✅ TYMCZASOWE: Generuj hash dla hasła "1221"
                var testHash = PasswordHelper.HashPassword("1221");
                // System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════");
                // System.Diagnostics.Debug.WriteLine($"🔑 HASH dla hasła '1221':");
                // System.Diagnostics.Debug.WriteLine($"   {testHash}");
                // System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════════════");

                // Sprawdź czy są użytkownicy
                using var cmdCheck = conn.CreateCommand();
                cmdCheck.CommandText = "SELECT COUNT(*) FROM Users";
                var count = Convert.ToInt32(cmdCheck.ExecuteScalar());

                if (count == 0)
                {
                    // Utwórz super admina
                    var passwordHash = PasswordHelper.HashPassword("2025");

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO Users (Username, PasswordHash, Email, FullName, Role, IsActive, CreatedDate)
                        VALUES (?, ?, ?, ?, ?, ?, ?)";

                    var p1 = cmd.CreateParameter(); p1.Value = "tesla"; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.Value = passwordHash; cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter(); p3.Value = "admin@asmed.pl"; cmd.Parameters.Add(p3);
                    var p4 = cmd.CreateParameter(); p4.Value = "Super Administrator"; cmd.Parameters.Add(p4);
                    var p5 = cmd.CreateParameter(); p5.Value = (int)Models.UserRole.SuperAdmin; cmd.Parameters.Add(p5);
                    var p6 = cmd.CreateParameter(); p6.Value = true; cmd.Parameters.Add(p6);
                    var p7 = cmd.CreateParameter(); p7.Value = DateTime.Now; cmd.Parameters.Add(p7);

                    cmd.ExecuteNonQuery();

                    // System.Diagnostics.Debug.WriteLine("✅ Super Admin utworzony: tesla/2025");
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"InitializeSuperAdmin error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Autoryzacja użytkownika (zwraca User jeśli sukces, null jeśli błąd)
        /// </summary>
        public Models.User AuthenticateUser(string username, string password)
        {
            // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: username='{username}', password.Length={password?.Length ?? 0}");

            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                //System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: Połączenie z bazą otwarte");
                //MessageBox.Show("Debug: Połączenie z bazą otwarte", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Username, PasswordHash, Email, FullName, Role, IsActive, CreatedDate, LastLogin, EndLogin
                    FROM Users
                    WHERE Username = ? AND IsActive = TRUE";

                var p = cmd.CreateParameter();
                p.Value = username;
                cmd.Parameters.Add(p);

                //System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: Wykonuję zapytanie SQL...");
                //MessageBox.Show("Debug: Wykonuję zapytanie SQL...", "Debug", MessageBoxButton.OK, MessageBoxImage.Information); 

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    //System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: Znaleziono użytkownika w bazie");
                    //MessageBox.Show("Debug: Znaleziono użytkownika w bazie", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);

                    var storedHash = reader["PasswordHash"]?.ToString();
                    // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: storedHash.Length={storedHash?.Length ?? 0}");

                    // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: Weryfikuję hasło...");
                    bool passwordValid = PasswordHelper.VerifyPassword(password, storedHash);
                    // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: Hasło {(passwordValid ? "✅ POPRAWNE" : "❌ NIEPOPRAWNE")}");

                    if (passwordValid)
                    {
                        // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: Tworzę obiekt User...");

                        var user = new Models.User
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Username = reader["Username"]?.ToString(),
                            PasswordHash = storedHash,
                            Email = reader["Email"]?.ToString(),
                            FullName = reader["FullName"]?.ToString(),
                            Role = (Models.UserRole)Convert.ToInt32(reader["Role"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            CreatedDate = reader["CreatedDate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["CreatedDate"])
                                : (DateTime?)null,
                            LastLogin = reader["LastLogin"] != DBNull.Value
                                ? Convert.ToDateTime(reader["LastLogin"])
                                : (DateTime?)null,
                            EndLogin = reader["EndLogin"] != DBNull.Value
                                ? Convert.ToDateTime(reader["EndLogin"])
                                : (DateTime?)null
                        };

                        // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: User utworzony - ID={user.Id}, Username={user.Username}, Role={user.Role}");

                        // Zaktualizuj LastLogin
                        reader.Close();
                        // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: Aktualizuję LastLogin...");
                        UpdateLastLogin(user.Id, conn);

                        // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: ✅ SUKCES - zwracam użytkownika");
                        return user;
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: ❌ Hasło niepoprawne dla username='{username}'");
                    }
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser: ❌ NIE ZNALEZIONO użytkownika '{username}' w bazie (lub IsActive=False)");
                }

                return null;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser ERROR: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"🔐 AuthenticateUser StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        private void UpdateLastLogin(int userId, System.Data.Odbc.OdbcConnection conn)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Users SET LastLogin = ? WHERE Id = ?";
                var p1 = cmd.CreateParameter(); p1.Value = DateTime.Now; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = userId; cmd.Parameters.Add(p2);
                cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateLastLogin error: {ex}");
            }
        }

        /// <summary>
        /// Pobiera wszystkich użytkowników z bazy
        /// </summary>
        public System.Collections.Generic.List<Models.User> GetAllUsers()
        {
            var list = new System.Collections.Generic.List<Models.User>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Username, PasswordHash, Email, FullName, Role, IsActive, CreatedDate, LastLogin, EndLogin
                    FROM Users
                    ORDER BY Role, Username";

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Models.User
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Username = reader["Username"]?.ToString(),
                        PasswordHash = reader["PasswordHash"]?.ToString(),
                        Email = reader["Email"]?.ToString(),
                        FullName = reader["FullName"]?.ToString(),
                        Role = (Models.UserRole)Convert.ToInt32(reader["Role"]),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        CreatedDate = reader["CreatedDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["CreatedDate"])
                            : (DateTime?)null,
                        LastLogin = reader["LastLogin"] != DBNull.Value
                            ? Convert.ToDateTime(reader["LastLogin"])
                            : (DateTime?)null,
                        EndLogin = reader["EndLogin"] != DBNull.Value
                            ? Convert.ToDateTime(reader["EndLogin"])
                            : (DateTime?)null
                    });
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"GetAllUsers error: {ex}");
            }
            return list;
        }

        /// <summary>
        /// Dodaje nowego użytkownika
        /// </summary>
        public bool AddUser(string? username, string? password, string? email, string? fullName, Models.UserRole role)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                // Sprawdź czy username już istnieje
                using var cmdCheck = conn.CreateCommand();
                cmdCheck.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = ?";
                var pCheck = cmdCheck.CreateParameter();
                pCheck.Value = username;
                cmdCheck.Parameters.Add(pCheck);

                var count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                if (count > 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ Username już istnieje: {username}");
                    return false;
                }

                // Dodaj użytkownika
                var passwordHash = PasswordHelper.HashPassword(password);

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Users (Username, PasswordHash, Email, FullName, Role, IsActive, CreatedDate)
                    VALUES (?, ?, ?, ?, ?, ?, ?)";

                var p1 = cmd.CreateParameter(); p1.Value = username; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = passwordHash; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = email ?? (object)DBNull.Value; cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.Value = fullName ?? (object)DBNull.Value; cmd.Parameters.Add(p4);
                var p5 = cmd.CreateParameter(); p5.Value = (int)role; cmd.Parameters.Add(p5);
                var p6 = cmd.CreateParameter(); p6.Value = true; cmd.Parameters.Add(p6);
                var p7 = cmd.CreateParameter(); p7.Value = DateTime.Now; cmd.Parameters.Add(p7);

                cmd.ExecuteNonQuery();

                // System.Diagnostics.Debug.WriteLine($"✅ Użytkownik dodany: {username}");
                return true;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"AddUser error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Aktualizuje dane użytkownika
        /// </summary>
        public bool UpdateUser(int userId, string? email, string? fullName, Models.UserRole role, bool isActive)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Users
                    SET Email = ?, FullName = ?, Role = ?, IsActive = ?
                    WHERE Id = ?";

                var p1 = cmd.CreateParameter(); p1.Value = email ?? (object)DBNull.Value; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = fullName ?? (object)DBNull.Value; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = (int)role; cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.Value = isActive; cmd.Parameters.Add(p4);
                var p5 = cmd.CreateParameter(); p5.Value = userId; cmd.Parameters.Add(p5);

                cmd.ExecuteNonQuery();

                // System.Diagnostics.Debug.WriteLine($"✅ Użytkownik zaktualizowany: ID={userId}");
                return true;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateUser error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Zmienia hasło użytkownika
        /// </summary>
        public bool ChangePassword(int userId, string newPassword)
        {
            try
            {
                var passwordHash = PasswordHelper.HashPassword(newPassword);

                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Users SET PasswordHash = ? WHERE Id = ?";

                var p1 = cmd.CreateParameter(); p1.Value = passwordHash; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = userId; cmd.Parameters.Add(p2);

                cmd.ExecuteNonQuery();

                // System.Diagnostics.Debug.WriteLine($"✅ Hasło zmienione: ID={userId}");
                return true;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"ChangePassword error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Usuwa użytkownika (soft delete - ustawia IsActive = false)
        /// </summary>
        public bool DeleteUser(int userId)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Users SET IsActive = ? WHERE Id = ?";

                var p1 = cmd.CreateParameter(); p1.Value = false; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = userId; cmd.Parameters.Add(p2);

                cmd.ExecuteNonQuery();

                // System.Diagnostics.Debug.WriteLine($"✅ Użytkownik dezaktywowany: ID={userId}");
                return true;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"DeleteUser error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Aktualizuje EndLogin (czas wylogowania) dla użytkownika
        /// </summary>
        public bool UpdateEndLogin(int userId, DateTime endLoginTime)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Users SET EndLogin = ? WHERE Id = ?";

                var p1 = cmd.CreateParameter(); p1.Value = endLoginTime; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = userId; cmd.Parameters.Add(p2);

                cmd.ExecuteNonQuery();

                // System.Diagnostics.Debug.WriteLine($"✅ EndLogin zaktualizowane: ID={userId}, EndLogin={endLoginTime:yyyy-MM-dd HH:mm:ss}");
                return true;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"UpdateEndLogin error: {ex}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ LOGIN HISTORY - Historia logowań
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Zapisuje wpis historii logowania (sukces lub błąd)
        /// Zwraca ID nowego wpisu
        /// </summary>
        public int LogLoginAttempt(int? userId, string username, bool success, string? failureReason = null)
        {
            int newId = 0;
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO LoginHistory 
                    (UserId, Username, LoginTime, ComputerName, IpAddress, Success, FailureReason)
                    VALUES (?, ?, ?, ?, ?, ?, ?)";

                var p1 = cmd.CreateParameter(); p1.Value = userId.HasValue ? (object)userId.Value : DBNull.Value; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = username ?? (object)DBNull.Value; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = DateTime.Now; cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.Value = Environment.MachineName; cmd.Parameters.Add(p4);
                var p5 = cmd.CreateParameter(); p5.Value = GetLocalIPAddress(); cmd.Parameters.Add(p5);
                var p6 = cmd.CreateParameter(); p6.Value = success; cmd.Parameters.Add(p6);
                var p7 = cmd.CreateParameter(); p7.Value = failureReason ?? (object)DBNull.Value; cmd.Parameters.Add(p7);

                cmd.ExecuteNonQuery();

                // Pobierz ID nowo utworzonego wpisu
                using var idCmd = conn.CreateCommand();
                idCmd.CommandText = "SELECT @@IDENTITY";
                var obj = idCmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value && int.TryParse(obj.ToString(), out var id))
                    newId = id;

                // System.Diagnostics.Debug.WriteLine($"📝 Login History: ID={newId}, User={username}, Success={success}");
                return newId;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"LogLoginAttempt error: {ex}");
                return 0;
            }
        }

        /// <summary>
        /// Aktualizuje LogoutTime dla aktywnej sesji użytkownika
        /// </summary>
        public bool LogLogout(int userId, DateTime logoutTime)
        {
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                // ✅ KROK 1: Znajdź ID ostatniej sesji użytkownika bez LogoutTime
                int? lastSessionId = null;
                using (var selectCmd = conn.CreateCommand())
                {
                    selectCmd.CommandText = @"
                        SELECT TOP 1 Id 
                        FROM LoginHistory 
                        WHERE UserId = ? AND LogoutTime IS NULL AND Success = TRUE
                        ORDER BY LoginTime DESC";

                    var p = selectCmd.CreateParameter();
                    p.Value = userId;
                    selectCmd.Parameters.Add(p);

                    var result = selectCmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out var id))
                    {
                        lastSessionId = id;
                        // System.Diagnostics.Debug.WriteLine($"📝 Znaleziono aktywną sesję: LoginHistory.Id={id}");
                    }
                }

                // ✅ KROK 2: Zaktualizuj LogoutTime dla tej sesji
                if (lastSessionId.HasValue)
                {
                    using var updateCmd = conn.CreateCommand();
                    updateCmd.CommandText = "UPDATE LoginHistory SET LogoutTime = ? WHERE Id = ?";

                    var p1 = updateCmd.CreateParameter(); p1.Value = logoutTime; updateCmd.Parameters.Add(p1);
                    var p2 = updateCmd.CreateParameter(); p2.Value = lastSessionId.Value; updateCmd.Parameters.Add(p2);

                    int rows = updateCmd.ExecuteNonQuery();

                    // System.Diagnostics.Debug.WriteLine($"📝 Logout History: UserId={userId}, SessionId={lastSessionId}, Rows updated={rows}, LogoutTime={logoutTime:yyyy-MM-dd HH:mm:ss}");
                    return rows > 0;
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"⚠️ Nie znaleziono aktywnej sesji dla UserId={userId}");
                    return false;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LogLogout error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pobiera historię logowań dla użytkownika
        /// </summary>
        public System.Collections.Generic.List<Models.LoginHistory> GetLoginHistory(int? userId = null, int maxRecords = 100)
        {
            var list = new System.Collections.Generic.List<Models.LoginHistory>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();

                if (userId.HasValue)
                {
                    cmd.CommandText = $@"
                        SELECT TOP {maxRecords} 
                            Id, UserId, Username, LoginTime, LogoutTime, 
                            ComputerName, IpAddress, Success, FailureReason
                        FROM LoginHistory
                        WHERE UserId = ?
                        ORDER BY LoginTime DESC";

                    var p = cmd.CreateParameter(); p.Value = userId.Value; cmd.Parameters.Add(p);
                }
                else
                {
                    cmd.CommandText = $@"
                        SELECT TOP {maxRecords} 
                            Id, UserId, Username, LoginTime, LogoutTime, 
                            ComputerName, IpAddress, Success, FailureReason
                        FROM LoginHistory
                        ORDER BY LoginTime DESC";
                }

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Models.LoginHistory
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : 0,
                        Username = reader["Username"]?.ToString(),
                        LoginTime = Convert.ToDateTime(reader["LoginTime"]),
                        LogoutTime = reader["LogoutTime"] != DBNull.Value
                            ? Convert.ToDateTime(reader["LogoutTime"])
                            : (DateTime?)null,
                        ComputerName = reader["ComputerName"]?.ToString(),
                        IpAddress = reader["IpAddress"]?.ToString(),
                        Success = Convert.ToBoolean(reader["Success"]),
                        FailureReason = reader["FailureReason"]?.ToString()
                    });
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"GetLoginHistory error: {ex}");
            }
            return list;
        }

        /// <summary>
        /// Pomocnicza metoda do pobierania lokalnego IP
        /// </summary>
        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "Unknown";
            }
        }

        // ═══════════════════════════════════════════════════════
        // ✅ RAPORTY - Niezafakturowane badania po firmie
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// DTO dla raportu niezafakturowanych badań pogrupowanych po firmie
        /// </summary>
        public class NiezafakturowaneBadaniaDto
        {
            public string? NazwaFirmy { get; set; }
            public decimal? SumaWartosci { get; set; }
            public int LiczbaBadan { get; set; }

            // Formatowane właściwości dla UI
            public string SumaWartosciFormatted => SumaWartosci.HasValue 
                ? $"{SumaWartosci.Value:N2} zł" 
                : "0,00 zł";
        }

        /// <summary>
        /// Pobiera listę firm z niezafakturowanymi badaniami (Bad_L_ID <= 0)
        /// Grupuje po firmie, sumuje wartości i liczy badania
        /// ✅ POPRAWIONY SQL: HAVING zamiast WHERE (po GROUP BY!)
        /// </summary>
        public List<NiezafakturowaneBadaniaDto> GetNiezafakturowaneBadaniaPoFirmie(string? filterFirmaNazwa = null)
        {
            var result = new List<NiezafakturowaneBadaniaDto>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();

                // ✅ SQL zgodny z działającym zapytaniem z Access
                // KLUCZOWE ZMIANY:
                // 1. GROUP BY zawiera również Badanie.Bad_L_ID
                // 2. HAVING zamiast WHERE (filtracja PO grupowaniu)
                // 3. ORDER BY Sum(Badanie.Bad_Razem) DESC (po wartości, nie nazwie)
                var sql = @"
SELECT
    Firma.Nazwa,
    Sum(Badanie.Bad_Razem) AS SumaOfBad_Razem,
    Count(Firma.Nazwa) AS ilosc
FROM
    (
        Badanie
        INNER JOIN P_Pacjent ON Badanie.Bad_P_ID = P_Pacjent.P_ID
    )
    INNER JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
GROUP BY
    Firma.Nazwa,
    Badanie.Bad_L_ID
HAVING
    (Badanie.Bad_L_ID <= 0 OR Badanie.Bad_L_ID IS NULL)";

                // ✅ Dodaj filtr po nazwie firmy w HAVING (jeśli podano)
                if (!string.IsNullOrWhiteSpace(filterFirmaNazwa))
                {
                    sql += " AND (Firma.Nazwa LIKE ?)";
                }

                // ✅ Sortowanie po wartości (od największej do najmniejszej)
                sql += @"
ORDER BY
    Sum(Badanie.Bad_Razem) DESC";

                using var cmd = new OdbcCommand(sql, connection);

                if (!string.IsNullOrWhiteSpace(filterFirmaNazwa))
                {
                    // ✅ ODBC wildcard: % (SQL standard)
                    // ✅ ODBC pozycyjne parametry (bez nazw)
                    var pattern = "%" + filterFirmaNazwa + "%";

                    var p = cmd.CreateParameter();
                    p.OdbcType = System.Data.Odbc.OdbcType.VarChar;
                    p.Size = Math.Max(255, pattern.Length);
                    p.Value = pattern;
                    cmd.Parameters.Add(p);

                    // System.Diagnostics.Debug.WriteLine($"📊 GetNiezafakturowaneBadaniaPoFirmie: Filtr pattern='{pattern}'");
                }

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    decimal? ParseDecimal(object obj)
                    {
                        if (obj == null || obj == DBNull.Value) return null;
                        if (decimal.TryParse(obj.ToString(), out var d)) return d;
                        return null;
                    }

                    result.Add(new NiezafakturowaneBadaniaDto
                    {
                        NazwaFirmy = reader["Nazwa"]?.ToString(),
                        SumaWartosci = ParseDecimal(reader["SumaOfBad_Razem"]),
                        LiczbaBadan = reader["ilosc"] is int count ? count : 
                                      (int.TryParse(reader["ilosc"]?.ToString(), out var count2) ? count2 : 0)
                    });
                }

                // System.Diagnostics.Debug.WriteLine($"✅ GetNiezafakturowaneBadaniaPoFirmie: Znaleziono {result.Count} firm z niezafakturowanymi badaniami");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ GetNiezafakturowaneBadaniaPoFirmie ERROR: {ex.Message}");
                MessageBox.Show($"Błąd pobierania raportu:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
        }

        // ═══════════════════════════════════════════════════════
        // ✅ STATYSTYKI - Skierowania / Wizyty / Badania
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// DTO dla statystyk miesięcznych skierowań/wizyt/badań
        /// </summary>
        public class StatystykaMiesiecznaDto
        {
            public int Rok { get; set; }
            public int Miesiac { get; set; }
            public string MiesiacNazwa { get; set; } = string.Empty;

            // Skierowania (B_RegistrationDate)
            public int LiczbaSkierowan { get; set; }

            // Badania wg typu (B_TypBadania)
            public int BadaniaOkresowe { get; set; }      // O
            public int BadaniaWstepne { get; set; }       // W
            public int BadaniaKontrolne { get; set; }     // K
            public int BadaniaInne { get; set; }          // inne

            // Książeczki (B_książeczka = true)
            public int LiczbaKsiazeczek { get; set; }

            // Badania odbyte (Bad_Data)
            public int BadaniaOdbyte { get; set; }

            // Wizyty zarejestrowane (R_Data)
            public int WizytyZarejestrowane { get; set; }

            // ✅ NOWE: Wartość badań w miesiącu (suma Bad_Razem)
            public decimal WartoscBadan { get; set; }

            // Formatowane właściwości
            public string MiesiacRokDisplay => $"{MiesiacNazwa} {Rok}";
        }

        /// <summary>
        /// Pobiera statystyki miesięczne dla wybranego roku
        /// </summary>
        public List<StatystykaMiesiecznaDto> GetStatystykiMiesieczne(int rok)
        {
            var result = new List<StatystykaMiesiecznaDto>();

            try
            {
                var dbHelper = new AccessDbHelper();
                using var connection = dbHelper.GetConnection();
                connection.Open();

                // ✅ SQL zgodny z zapytaniem użytkownika (wszystkie dane dla roku)
                var sql = @"
SELECT
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_książeczka,
    B_Skierowania.B_RegistrationDate,
    Badanie.Bad_Data,
    Badanie.Bad_Razem,
    Rejestracja.R_Data
FROM
    Rejestracja
    RIGHT JOIN (
        B_Skierowania
        LEFT JOIN Badanie ON B_Skierowania.B_ID = Badanie.Bad_S_ID
    ) ON Rejestracja.R_S_ID = B_Skierowania.B_ID
WHERE
    (YEAR(B_Skierowania.B_RegistrationDate) = ?)
    OR (YEAR(Badanie.Bad_Data) = ?)
    OR (YEAR(Rejestracja.R_Data) = ?)";

                using var cmd = new OdbcCommand(sql, connection);
                var p1 = cmd.CreateParameter(); p1.Value = rok; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.Value = rok; cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.Value = rok; cmd.Parameters.Add(p3);

                using var reader = cmd.ExecuteReader();

                // ✅ Inicjalizuj statystyki dla każdego miesiąca (1-12)
                var miesiacNazwy = new[] { "", "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec", 
                                           "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień" };

                for (int m = 1; m <= 12; m++)
                {
                    result.Add(new StatystykaMiesiecznaDto
                    {
                        Rok = rok,
                        Miesiac = m,
                        MiesiacNazwa = miesiacNazwy[m]
                    });
                }

                // ✅ Przetworz dane z bazy (agregacja po miesiącach)
                while (reader.Read())
                {
                    var typBadania = reader["B_TypBadania"]?.ToString()?.ToUpper();
                    var ksiazeczka = reader["B_książeczka"] != DBNull.Value && Convert.ToBoolean(reader["B_książeczka"]);

                    // Data skierowania
                    if (reader["B_RegistrationDate"] != DBNull.Value)
                    {
                        var dataSkierowania = Convert.ToDateTime(reader["B_RegistrationDate"]);
                        if (dataSkierowania.Year == rok)
                        {
                            var stat = result[dataSkierowania.Month - 1];
                            stat.LiczbaSkierowan++;

                            // Typ badania
                            switch (typBadania)
                            {
                                case "O": stat.BadaniaOkresowe++; break;
                                case "W": stat.BadaniaWstepne++; break;
                                case "K": stat.BadaniaKontrolne++; break;
                                default: stat.BadaniaInne++; break;
                            }

                            // Książeczka
                            if (ksiazeczka) stat.LiczbaKsiazeczek++;
                        }
                    }

                    // Data badania
                    if (reader["Bad_Data"] != DBNull.Value)
                    {
                        var dataBadania = Convert.ToDateTime(reader["Bad_Data"]);
                        if (dataBadania.Year == rok)
                        {
                            var stat = result[dataBadania.Month - 1];
                            stat.BadaniaOdbyte++;

                            // ✅ NOWE: Wartość badania (Bad_Razem)
                            if (reader["Bad_Razem"] != DBNull.Value)
                            {
                                var wartoscBadania = Convert.ToDecimal(reader["Bad_Razem"]);
                                stat.WartoscBadan += wartoscBadania;
                            }
                        }
                    }

                    // Data wizyty
                    if (reader["R_Data"] != DBNull.Value)
                    {
                        var dataWizyty = Convert.ToDateTime(reader["R_Data"]);
                        if (dataWizyty.Year == rok)
                        {
                            var stat = result[dataWizyty.Month - 1];
                            stat.WizytyZarejestrowane++;
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"✅ GetStatystykiMiesieczne: Wygenerowano statystyki dla {rok}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ GetStatystykiMiesieczne ERROR: {ex.Message}");
                MessageBox.Show($"Błąd pobierania statystyk:\n{ex.Message}", "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }
    }
}
// end of file
