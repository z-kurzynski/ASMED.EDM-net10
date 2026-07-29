using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models.BadaniaEdit;
using ASMED.EDM.UI.Models.BadaniaNew;
using MySqlConnector;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Serwis TYLKO dla widoku "Edycja Badań" (BadaniaEditNewView)
/// Obsługuje zamknięte badania (z Bad_Data)
/// </summary>
public class BadaniaEditService
{
    private readonly DbConnectionFactory? _dbFactory;

    public BadaniaEditService(DbConnectionFactory? dbFactory = null)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Pobiera listę zamkniętych badań (z Bad_Data) dla BadaniaEditNewView
    /// </summary>
    public List<BadaniaEditListDto> GetBadaniaZamkniete()
    {
        var result = new List<BadaniaEditListDto>();
        if (_dbFactory == null) return result;

        try
        {
            Console.WriteLine($"🔍 BadaniaEditService.GetBadaniaZamkniete: Connecting to MySQL...");

            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            var sql = @"
SELECT 
    B_Skierowania.B_ID,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_Stanowisko,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_pesel,
    Firma.Nazwa AS Firma_Nazwa,
    Firma.NIP AS Firma_NIP,
    Badanie.Bad_Data,
    Badanie.Bad_Wynik,
    Badanie.Bad_Fakt
FROM 
    (
        (B_Skierowania 
        INNER JOIN P_Pacjent ON B_Skierowania.B_Pacjent_ID = P_Pacjent.P_ID)
        LEFT JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
    )
    INNER JOIN Badanie ON B_Skierowania.B_ID = Badanie.Bad_S_ID
WHERE 
    B_Skierowania.B_Activ = 1
    AND Badanie.Bad_Data IS NOT NULL
ORDER BY 
    Badanie.Bad_Data DESC";

            Console.WriteLine($"🔍 Executing SQL for BadaniaEditNewView...");

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var dto = new BadaniaEditListDto
                {
                    B_ID = reader["B_ID"] is DBNull ? 0 : Convert.ToInt32(reader["B_ID"]),
                    B_DataSkierowania = reader["B_DataSkierowania"] is DBNull ? null : Convert.ToDateTime(reader["B_DataSkierowania"]),
                    B_TypBadania = reader["B_TypBadania"]?.ToString(),
                    P_imie = reader["P_imie"]?.ToString(),
                    P_nazwisko = reader["P_nazwisko"]?.ToString(),
                    P_pesel = reader["P_pesel"]?.ToString(),
                    Firma_Nazwa = reader["Firma_Nazwa"]?.ToString(),
                    Firma_NIP = reader["Firma_NIP"]?.ToString(),
                    B_Stanowisko = reader["B_Stanowisko"]?.ToString(),
                    Bad_Data = reader["Bad_Data"] is DBNull ? null : Convert.ToDateTime(reader["Bad_Data"]),
                    Bad_Wynik = reader["Bad_Wynik"]?.ToString(),
                    Bad_Fakt = reader["Bad_Fakt"]?.ToString()
                };

                result.Add(dto);
            }

            Console.WriteLine($"✅ BadaniaEditService.GetBadaniaZamkniete: Loaded {result.Count} badań from MySQL");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BadaniaEditService.GetBadaniaZamkniete error: {ex.Message}");
            throw;
        }

        return result;
    }

    /// <summary>
    /// Pobiera szczegóły badania dla BadaniaEditNewView
    /// UWAGA: SQL różni się od BadaniaNewView - zawiera wszystkie pola z tabeli Badanie
    /// </summary>
    public BadaniaEditDetailsDto? GetBadanieDetailsEdt(int bSkierowaniaId)
    {
        if (_dbFactory == null) return null;

        try
        {
            Console.WriteLine($"🔍 BadaniaEditService.GetBadanieDetailsEdt: Loading B_ID={bSkierowaniaId}");

            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            // JOIN przez Bad_S_ID (taki sam jak GetBadaniaZamkniete)
            var sql = @"
SELECT
    B_Skierowania.B_ID,
    P_Pacjent.P_ID,
    Firma.id,
    Badanie.Bad_bn_cennik,
    Badanie.Bad_Fakt,
    Badanie.Bad_Typ,
    Badanie.Bad_Data,
    Badanie.Bad_Data_Do,
    Badanie.Bad_Wynik,
    Badanie.Bad_Cena1,
    Badanie.Bad_Cena2,
    Badanie.Bad_Cena3,
    Badanie.Bad_Cena4,
    Badanie.Bad_Cena6,
    Badanie.Bad_Cena7,
    Badanie.Bad_Cena8,
    Badanie.Bad_Cena9,
    Badanie.Bad_Cena10,
    Badanie.Bad_Razem,
    Badanie.Bad_Nr_KS,
    Firma.Nazwa,
    Firma.Ulica,
    Firma.Kod,
    Firma.Miejscowosc,
    Firma.NIP,
    P_Pacjent.P_pesel,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_zawod,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_Stanowisko,
    B_Skierowania.B_Zaswiadczenie,
    B_Skierowania.B_ksiazeczka
FROM
    (
        (
            P_Pacjent
            INNER JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
        )
        INNER JOIN B_Skierowania ON P_Pacjent.P_ID = B_Skierowania.B_Pacjent_ID
    )
    INNER JOIN Badanie ON B_Skierowania.B_ID = Badanie.Bad_S_ID
WHERE
    B_Skierowania.B_ID = @BID";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;

            var param = cmd.CreateParameter();
            param.ParameterName = "@BID";
            param.Value = bSkierowaniaId;
            cmd.Parameters.Add(param);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                var details = new BadaniaEditDetailsDto
                {
                    B_ID = reader["B_ID"] is DBNull ? 0 : Convert.ToInt32(reader["B_ID"]),
                    B_DataSkierowania = reader["B_DataSkierowania"] is DBNull ? null : Convert.ToDateTime(reader["B_DataSkierowania"]),
                    B_TypBadania = reader["B_TypBadania"]?.ToString(),
                    B_Stanowisko = reader["B_Stanowisko"]?.ToString(),
                    B_Zaswiadczenie = reader["B_Zaswiadczenie"] is DBNull ? null : Convert.ToBoolean(reader["B_Zaswiadczenie"]),
                    B_ksiazeczka = reader["B_ksiazeczka"] is DBNull ? null : Convert.ToBoolean(reader["B_ksiazeczka"]),

                    P_ID = reader["P_ID"] is DBNull ? 0 : Convert.ToInt32(reader["P_ID"]),
                    P_pesel = reader["P_pesel"]?.ToString(),
                    P_imie = reader["P_imie"]?.ToString(),
                    P_nazwisko = reader["P_nazwisko"]?.ToString(),
                    P_zawod = reader["P_zawod"]?.ToString(),

                    Firma_id = reader["id"] is DBNull ? 0 : Convert.ToInt32(reader["id"]),
                    Firma_Nazwa = reader["Nazwa"]?.ToString(),
                    Firma_Ulica = reader["Ulica"]?.ToString(),
                    Firma_Kod = reader["Kod"]?.ToString(),
                    Firma_Miejscowosc = reader["Miejscowosc"]?.ToString(),
                    Firma_NIP = reader["NIP"]?.ToString(),

                    Bad_bn_cennik = reader["Bad_bn_cennik"]?.ToString(),
                    Bad_Fakt = reader["Bad_Fakt"]?.ToString(),
                    Bad_Typ = reader["Bad_Typ"]?.ToString(),
                    Bad_Data = reader["Bad_Data"] is DBNull ? null : Convert.ToDateTime(reader["Bad_Data"]),
                    Bad_Data_Do = reader["Bad_Data_Do"] is DBNull ? null : Convert.ToDateTime(reader["Bad_Data_Do"]),
                    Bad_Wynik = reader["Bad_Wynik"]?.ToString(),
                    Bad_Cena1 = ParseDecimal(reader["Bad_Cena1"]),
                    Bad_Cena2 = ParseDecimal(reader["Bad_Cena2"]),
                    Bad_Cena3 = ParseDecimal(reader["Bad_Cena3"]),
                    Bad_Cena4 = ParseDecimal(reader["Bad_Cena4"]),
                    Bad_Cena6 = ParseDecimal(reader["Bad_Cena6"]),
                    Bad_Cena7 = ParseDecimal(reader["Bad_Cena7"]),
                    Bad_Cena8 = ParseDecimal(reader["Bad_Cena8"]),
                    Bad_Cena9 = ParseDecimal(reader["Bad_Cena9"]),
                    Bad_Cena10 = ParseDecimal(reader["Bad_Cena10"]),
                    Bad_Razem = ParseDecimal(reader["Bad_Razem"]),
                    Bad_Nr_KS = reader["Bad_Nr_KS"]?.ToString()
                };

                Console.WriteLine($"✅ BadaniaEditService: Loaded details for {details.P_imie} {details.P_nazwisko} from {details.Firma_Nazwa}");
                
                return details;
            }

            Console.WriteLine($"⚠️ BadaniaEditService: No details found for B_ID={bSkierowaniaId}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BadaniaEditService.GetBadanieDetailsEdt error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Pobiera listę aktywnych cenników (ComboBox select_cennik)
    /// </summary>
    public List<CennikDto> GetCennikList()
    {
        var result = new List<CennikDto>();
        if (_dbFactory == null) return result;

        try
        {
            Console.WriteLine("🔍 BadaniaEditService.GetCennikList: Loading active cennik...");
            Debug.WriteLine("🔍 BadaniaEditService.GetCennikList: Loading active cennik...");

            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            var sql = @"
SELECT
    BAD_Lista.Identyfikator,
    BAD_Lista.bn_cennik,
    BAD_Lista.bn_Cen_activ
FROM
    BAD_Lista
GROUP BY
    BAD_Lista.Identyfikator,
    BAD_Lista.bn_cennik,
    BAD_Lista.bn_Cen_activ
HAVING
    BAD_Lista.bn_Cen_activ = 1";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new CennikDto
                {
                    Identyfikator = reader["Identyfikator"] is DBNull ? 0 : Convert.ToInt32(reader["Identyfikator"]),
                    bn_cennik = reader["bn_cennik"]?.ToString(),
                    bn_Cen_activ = reader["bn_Cen_activ"] is DBNull ? false : Convert.ToBoolean(reader["bn_Cen_activ"])
                });
            }

            Console.WriteLine($"✅ BadaniaEditService: Loaded {result.Count} active cennik entries");
            Debug.WriteLine($"✅ BadaniaEditService: Loaded {result.Count} active cennik entries");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BadaniaEditService.GetCennikList error: {ex.Message}");
            Debug.WriteLine($"❌ BadaniaEditService.GetCennikList error: {ex.Message}\n{ex.StackTrace}");
        }

        return result;
    }

    /// <summary>
    /// Pobiera ceny z cennika po nazwie cennika
    /// </summary>
    public Dictionary<string, decimal?>? GetCennikPrices(string bnCennik)
    {
        if (_dbFactory == null || string.IsNullOrEmpty(bnCennik)) return null;

        try
        {
            Console.WriteLine($"🔍 BadaniaEditService.GetCennikPrices: Loading prices for cennik='{bnCennik}'");

            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            var sql = @"
SELECT
    BAD_Cennik.b_Nazwa,
    BAD_Cennik.b_Cena
FROM
    BAD_Cennik
WHERE
    BAD_Cennik.b_Cennik = @b_Cennik
    AND BAD_Cennik.b_activ = 1";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new MySqlParameter("@b_Cennik", bnCennik));

            using var reader = cmd.ExecuteReader();
            var prices = new Dictionary<string, decimal?>();

            while (reader.Read())
            {
                string? nazwa = reader["b_Nazwa"]?.ToString();
                if (string.IsNullOrEmpty(nazwa)) continue;

                decimal? cena = null;
                var rawCena = reader["b_Cena"];
                if (rawCena is not DBNull && rawCena != null)
                {
                    var rawStr = rawCena.ToString()?.Trim().Replace(",", ".");
                    if (decimal.TryParse(rawStr,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out decimal parsed))
                        cena = parsed;
                    else
                        Console.WriteLine($"[EDIT SVC] GetCennikPrices: nie mozna sparsowac ceny '{rawStr}' dla '{nazwa}'");
                }
                prices[nazwa] = cena;
            }

            Console.WriteLine($"✅ BadaniaEditService: Loaded {prices.Count} prices for cennik '{bnCennik}'");
            return prices.Count > 0 ? prices : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BadaniaEditService.GetCennikPrices error: {ex.Message}");
            return null;
        }
    }

    // -----------------------------------------------------------------------
    // Helper: bezpieczny parse decimal niezależny od kultury (obsługuje , i .)
    // -----------------------------------------------------------------------
    private static decimal? ParseDecimal(object? raw)
    {
        if (raw is null || raw is DBNull) return null;
        if (raw is decimal d) return d;
        if (raw is double db) return (decimal)db;
        if (raw is float fl) return (decimal)fl;
        var str = raw.ToString()?.Trim().Replace(",", ".");
        if (string.IsNullOrEmpty(str)) return null;
        return decimal.TryParse(str,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result) ? result : null;
    }
}
