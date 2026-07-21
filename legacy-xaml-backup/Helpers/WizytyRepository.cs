using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;

namespace ASMED.WPF.Helpers
{
    public class WizytaRecord
    {
        public int B_Badanie_ID { get; set; }
        public int B_ID { get; set; }
        public int P_ID { get; set; }
        public int Firma_id { get; set; }
        public string P_imie { get; set; } = string.Empty;
        public string P_nazwisko { get; set; } = string.Empty;
        public string P_pesel { get; set; } = string.Empty;
        public string P_zawod { get; set; } = string.Empty;
        public string Firma_Nazwa { get; set; } = string.Empty;
        public string Firma_Cennik { get; set; } = string.Empty;
        public string Firma_NIP { get; set; } = string.Empty;
        public bool B_ksiazeczka { get; set; }
        public bool B_Zaswiadczenie { get; set; }
        public DateTime? B_DataSkierowania { get; set; }
        public string B_TypBadania { get; set; } = string.Empty;
        // Badanie fields (when a badanie exists)
        public DateTime? Bad_Data { get; set; }
        public DateTime? Bad_Data_Do { get; set; }
        public string? Bad_Wynik { get; set; }
        public decimal? Bad_Razem { get; set; }
        public string? Bad_Nr_KS { get; set; }
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

        // Computed formatted PESEL: xxxxxx xx xxx (groups 6-2-3)
        public string FormattedPesel
        {
            get
            {
                if (string.IsNullOrWhiteSpace(P_pesel)) return string.Empty;
                var digits = new string(P_pesel.Where(char.IsDigit).ToArray());
                if (digits.Length >= 11)
                {
                    // use first 11 digits
                    digits = digits.Substring(0, 11);
                    return digits.Substring(0, 6) + " " + digits.Substring(6, 2) + " " + digits.Substring(8, 3);
                }
                // fallback grouping
                if (digits.Length > 6)
                {
                    var part1 = digits.Substring(0, 6);
                    var rest = digits.Substring(6);
                    if (rest.Length > 2)
                        return part1 + " " + rest.Substring(0, 2) + " " + rest.Substring(2);
                    return part1 + " " + rest;
                }
                return digits;
            }
        }

        // Expose referral date for UI as raw DateTime and formatted string
        public DateTime? DataSkierDate => B_DataSkierowania;

        public string DataSkierDisplay => B_DataSkierowania.HasValue ? B_DataSkierowania.Value.ToString("dd.MM.yyyy") : string.Empty;

        public int Bad_ID { get; internal set; }
        public string Bad_bn_cennik { get; internal set; } = string.Empty;
    }

    public class WizytyRepository
    {

        public List<WizytaRecord> GetWizyty()
        {
            var result = new List<WizytaRecord>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    var sql = @"SELECT
    B_Skierowania.B_Badanie_ID,
    B_Skierowania.B_ID,
    P_Pacjent.P_ID,
    Firma.id,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_pesel,
    P_Pacjent.P_zawód,
    Firma.Nazwa,
    Firma.Cennik,
    Firma.NIP,
    B_Skierowania.B_książeczka,
    B_Skierowania.B_Zaswiadczenie,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania
FROM
    Firma
    INNER JOIN (
        B_Skierowania
        INNER JOIN P_Pacjent ON B_Skierowania.B_Pacjent_ID = P_Pacjent.P_ID
    ) ON Firma.id = P_Pacjent.P_Firma_id
WHERE
    (((B_Skierowania.B_Badanie_ID) < 1))
    OR (((B_Skierowania.B_Badanie_ID) IS NULL));";

                    using (var cmd = new OdbcCommand(sql, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rec = new WizytaRecord();
                            rec.B_Badanie_ID = reader["B_Badanie_ID"] is int bb ? bb : (int.TryParse(reader["B_Badanie_ID"]?.ToString(), out var bbi) ? bbi : 0);
                            rec.B_ID = reader["B_ID"] is int bid ? bid : (int.TryParse(reader["B_ID"]?.ToString(), out var bid2) ? bid2 : 0);
                            rec.P_ID = reader["P_ID"] is int pid ? pid : (int.TryParse(reader["P_ID"]?.ToString(), out var pid2) ? pid2 : 0);
                            rec.Firma_id = reader["id"] is int fid ? fid : (int.TryParse(reader["id"]?.ToString(), out var fid2) ? fid2 : 0);
                            rec.P_imie = reader["P_imie"]?.ToString() ?? string.Empty;
                            rec.P_nazwisko = reader["P_nazwisko"]?.ToString() ?? string.Empty;
                            rec.P_pesel = reader["P_pesel"]?.ToString() ?? string.Empty;
                            rec.P_zawod = reader["P_zawód"]?.ToString() ?? string.Empty;
                            rec.Firma_Nazwa = reader["Nazwa"]?.ToString() ?? string.Empty;
                            rec.Firma_Cennik = reader["Cennik"]?.ToString() ?? string.Empty;
                            rec.Firma_NIP = reader["NIP"]?.ToString() ?? string.Empty;
                            rec.B_ksiazeczka = reader["B_książeczka"] != DBNull.Value && Convert.ToBoolean(reader["B_książeczka"]);
                            rec.B_Zaswiadczenie = reader["B_Zaswiadczenie"] != DBNull.Value && Convert.ToBoolean(reader["B_Zaswiadczenie"]);
                            rec.B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["B_DataSkierowania"]) : null;
                            rec.B_TypBadania = reader["B_TypBadania"]?.ToString() ?? string.Empty;

                            result.Add(rec);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd pobierania wizyt: {ex.Message}", "Błąd bazy danych", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return result;
        }

        public List<string> GetCennikOptions()
        {
            var result = new List<string>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    var sql = @"SELECT BAD_Lista.Identyfikator, BAD_Lista.bn_cennik FROM BAD_Lista WHERE (((BAD_Lista.bn_Cen_activ)=True));";
                    using (var cmd = new OdbcCommand(sql, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var val = reader["bn_cennik"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(val) && !result.Contains(val))
                                result.Add(val);
                        }
                        // Debug.WriteLine($"GetCennikOptions: found {result.Count} cennik(s): {string.Join(", ", result)}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd pobierania cenników: {ex.Message}", "Błąd bazy danych", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return result;
        }

        public Dictionary<string, decimal> GetCennikPrices(string bnCennik)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            // Debug.WriteLine($"GetCennikPrices: bnCennik='{bnCennik}'");
            if (string.IsNullOrWhiteSpace(bnCennik))
                return result;
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    var sql = @"SELECT BAD_Lista.bn_cennik, BAD_Cennik.b_Nazwa, BAD_Cennik.b_Cena FROM BAD_Lista INNER JOIN BAD_Cennik ON BAD_Lista.bn_cennik = BAD_Cennik.b_Cennik WHERE ((BAD_Cennik.b_activ)=True) AND (BAD_Lista.bn_cennik = ?);";
                    using (var cmd = new OdbcCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@bn", bnCennik);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var name = reader["b_Nazwa"]?.ToString();
                                decimal price = 0m;
                                var obj = reader["b_Cena"];
                                if (obj != null && decimal.TryParse(obj.ToString(), out var p))
                                    price = p;
                                if (!string.IsNullOrEmpty(name))
                                {
                                    if (!result.ContainsKey(name))
                                        result[name] = price;
                                }
                            }
                            // Debug.WriteLine($"GetCennikPrices: loaded {result.Count} price(s) for '{bnCennik}'.");
                            foreach (var kv in result)
                            {
                                // Debug.WriteLine($" Price entry: '{kv.Key}' = {kv.Value}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd pobierania cen cennika: {ex.Message}", "Błąd bazy danych", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return result;
        }

        public List<WizytaRecord> GetBadaniaList()
        {
            var result = new List<WizytaRecord>();
            try
            {
                var dbHelper = new AccessDbHelper();
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    var sql = @"SELECT
 [B_Skierowania].[B_Badanie_ID],
 [B_Skierowania].[B_ID],
 [P_Pacjent].[P_ID],
 [Firma].[id],
 [P_Pacjent].[P_imie],
 [P_Pacjent].[P_nazwisko],
 [P_Pacjent].[P_pesel],
 [P_Pacjent].[P_zawód],
 [Firma].[Nazwa],
 [Firma].[Cennik],
 [Firma].[NIP],
 [B_Skierowania].[B_książeczka],
 [B_Skierowania].[B_Zaswiadczenie],
 [B_Skierowania].[B_DataSkierowania],
 [B_Skierowania].[B_TypBadania],
 [Badanie].[Bad_Data],
 [Badanie].[Bad_Data_Do],
 [Badanie].[Bad_Wynik],
 [Badanie].[Bad_Razem],
 [Badanie].[Bad_Nr_KS],
 [Badanie].[Bad_Cena1],
 [Badanie].[Bad_Cena2],
 [Badanie].[Bad_Cena3],
 [Badanie].[Bad_Cena4],
 [Badanie].[Bad_Cena5],
 [Badanie].[Bad_Cena6],
 [Badanie].[Bad_Cena7],
 [Badanie].[Bad_Cena8],
 [Badanie].[Bad_Cena9],
 [Badanie].[Bad_Cena10],
 [Badanie].[Bad_bn_cennik]
 FROM
 ([Firma]
 INNER JOIN ([B_Skierowania]
 INNER JOIN [P_Pacjent] ON [B_Skierowania].[B_Pacjent_ID] = [P_Pacjent].[P_ID]) ON [Firma].[id] = [P_Pacjent].[P_Firma_id])
 INNER JOIN [Badanie] ON [B_Skierowania].[B_Badanie_ID] = [Badanie].[Bad_ID]
 WHERE
 ([B_Skierowania].[B_Badanie_ID] >0);";

                    using (var cmd = new OdbcCommand(sql, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var rec = new WizytaRecord();
                            rec.B_Badanie_ID = reader["B_Badanie_ID"] is int bb ? bb : (int.TryParse(reader["B_Badanie_ID"]?.ToString(), out var bbi) ? bbi : 0);
                            rec.B_ID = reader["B_ID"] is int bid ? bid : (int.TryParse(reader["B_ID"]?.ToString(), out var bid2) ? bid2 : 0);
                            rec.P_ID = reader["P_ID"] is int pid ? pid : (int.TryParse(reader["P_ID"]?.ToString(), out var pid2) ? pid2 : 0);
                            rec.Firma_id = reader["id"] is int fid ? fid : (int.TryParse(reader["id"]?.ToString(), out var fid2) ? fid2 : 0);
                            rec.P_imie = reader["P_imie"]?.ToString() ?? string.Empty;
                            rec.P_nazwisko = reader["P_nazwisko"]?.ToString() ?? string.Empty;
                            rec.P_pesel = reader["P_pesel"]?.ToString() ?? string.Empty;
                            rec.P_zawod = reader["P_zawód"]?.ToString() ?? string.Empty;
                            rec.Firma_Nazwa = reader["Nazwa"]?.ToString() ?? string.Empty;

                            // Najpierw sprawdź czy w tabeli Badanie jest zapisany cennik (Bad_bn_cennik).
                            // Jeśli jest pusta wartość, fallback do Firma.Cennik.
                            var bad_bn_cennik = reader["Bad_bn_cennik"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(bad_bn_cennik))
                                rec.Firma_Cennik = bad_bn_cennik;
                            else
                                rec.Firma_Cennik = reader["Cennik"]?.ToString() ?? string.Empty;

                            rec.Firma_NIP = reader["NIP"]?.ToString() ?? string.Empty;
                            rec.B_ksiazeczka = reader["B_książeczka"] != DBNull.Value && Convert.ToBoolean(reader["B_książeczka"]);
                            rec.B_Zaswiadczenie = reader["B_Zaswiadczenie"] != DBNull.Value && Convert.ToBoolean(reader["B_Zaswiadczenie"]);
                            rec.B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["B_DataSkierowania"]) : null;
                            rec.B_TypBadania = reader["B_TypBadania"]?.ToString() ?? string.Empty;
                            rec.Bad_Data = reader["Bad_Data"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["Bad_Data"]) : null;
                            rec.Bad_Data_Do = reader["Bad_Data_Do"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["Bad_Data_Do"]) : null;
                            rec.Bad_Wynik = reader["Bad_Wynik"]?.ToString();
                            if (reader["Bad_Razem"] != DBNull.Value && decimal.TryParse(reader["Bad_Razem"].ToString(), out var br)) rec.Bad_Razem = br; else rec.Bad_Razem = null;
                            rec.Bad_Nr_KS = reader["Bad_Nr_KS"]?.ToString();

                            decimal parseDecimal(object obj)
                            {
                                if (obj == null || obj == DBNull.Value) return decimal.MinValue;
                                if (decimal.TryParse(obj.ToString(), out var d)) return d;
                                return decimal.MinValue;
                            }

                            var d1 = parseDecimal(reader["Bad_Cena1"]);
                            rec.Bad_Cena1 = d1 != decimal.MinValue ? d1 : (decimal?)null;
                            var d2 = parseDecimal(reader["Bad_Cena2"]);
                            rec.Bad_Cena2 = d2 != decimal.MinValue ? d2 : (decimal?)null;
                            var d3 = parseDecimal(reader["Bad_Cena3"]);
                            rec.Bad_Cena3 = d3 != decimal.MinValue ? d3 : (decimal?)null;
                            var d4 = parseDecimal(reader["Bad_Cena4"]);
                            rec.Bad_Cena4 = d4 != decimal.MinValue ? d4 : (decimal?)null;
                            var d5 = parseDecimal(reader["Bad_Cena5"]);
                            rec.Bad_Cena5 = d5 != decimal.MinValue ? d5 : (decimal?)null;
                            var d6 = parseDecimal(reader["Bad_Cena6"]);
                            rec.Bad_Cena6 = d6 != decimal.MinValue ? d6 : (decimal?)null;
                            var d7 = parseDecimal(reader["Bad_Cena7"]);
                            rec.Bad_Cena7 = d7 != decimal.MinValue ? d7 : (decimal?)null;
                            var d8 = parseDecimal(reader["Bad_Cena8"]);
                            rec.Bad_Cena8 = d8 != decimal.MinValue ? d8 : (decimal?)null;
                            var d9 = parseDecimal(reader["Bad_Cena9"]);
                            rec.Bad_Cena9 = d9 != decimal.MinValue ? d9 : (decimal?)null;
                            var d10 = parseDecimal(reader["Bad_Cena10"]);
                            rec.Bad_Cena10 = d10 != decimal.MinValue ? d10 : (decimal?)null;

                            result.Add(rec);
                        }
                    }
                }
            }
            catch (System.Data.Odbc.OdbcException odex)
            {
                // Show detailed ODBC error information to help diagnose Access SQL issues
                try
                {
                    var details = new System.Text.StringBuilder();
                    details.AppendLine(odex.Message);
                    foreach (System.Data.Odbc.OdbcError err in odex.Errors)
                    {
                        details.AppendLine($"SQLState: {err.SQLState} NativeError: {err.NativeError} Message: {err.Message}");
                    }
                    details.AppendLine("-- SQL -- (niedostępne w tym kontekście)");
                    System.Windows.MessageBox.Show(details.ToString(), "Błąd bazy danych (ODBC)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                catch
                {
                    System.Windows.MessageBox.Show($"Błąd pobierania listy badań: {odex.Message}", "Błąd bazy danych", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Błąd pobierania listy badań: {ex.Message}", "Błąd bazy danych", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return result;
        }
    }
}
