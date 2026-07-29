using System;
using System.Collections.Generic;
using System.Linq;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Serwis do zarządzania badaniami medycyny pracy
/// </summary>
public class BadaniaService
{
    private readonly DbConnectionFactory? _dbFactory;

    public BadaniaService(DbConnectionFactory? dbFactory = null)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Pobiera listę wszystkich badań z danymi skierowania, pacjenta i firmy
    /// </summary>
    public List<BadanieDto> GetBadaniaWithSkierowania()
    {
        var result = new List<BadanieDto>();
        if (_dbFactory == null) return result;

        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            var sql = @"
SELECT 
    B_Skierowania.B_ID,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    Firma.Nazwa AS Firma_Nazwa,
    Badanie.Bad_Data
FROM 
    (
        (B_Skierowania 
        INNER JOIN P_Pacjent ON B_Skierowania.B_Pacjent_ID = P_Pacjent.P_ID)
        LEFT JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
    )
    LEFT JOIN Badanie ON B_Skierowania.B_ID = Badanie.Bad_S_ID
WHERE 
    B_Skierowania.B_Activ = 1
ORDER BY 
    B_Skierowania.B_ID DESC";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var dto = new BadanieDto
                {
                    B_ID = reader["B_ID"] is DBNull ? 0 : Convert.ToInt32(reader["B_ID"]),
                    B_DataSkierowania = reader["B_DataSkierowania"] is DBNull ? null : Convert.ToDateTime(reader["B_DataSkierowania"]),
                    B_TypBadania = reader["B_TypBadania"]?.ToString(),
                    P_imie = reader["P_imie"]?.ToString(),
                    P_nazwisko = reader["P_nazwisko"]?.ToString(),
                    Firma_Nazwa = reader["Firma_Nazwa"]?.ToString(),
                    Bad_Data = reader["Bad_Data"] is DBNull ? null : Convert.ToDateTime(reader["Bad_Data"])
                };

                result.Add(dto);
            }

            System.Diagnostics.Debug.WriteLine($"✅ GetBadaniaWithSkierowania: Loaded {result.Count} badań from MySQL");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ GetBadaniaWithSkierowania error: {ex.Message}");
            throw;
        }

        return result;
    }

    /// <summary>
    /// Pobiera badania do zamknięcia (bez wypełnionego Bad_Data)
    /// </summary>
    public List<BadanieDto> GetBadaniaDoZamkniecia()
    {
        var result = new List<BadanieDto>();
        if (_dbFactory == null) return result;

        try
        {
            Console.WriteLine($"🔍 GetBadaniaDoZamkniecia: Connecting to MySQL...");

            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            Console.WriteLine($"✅ Connected to MySQL");

            // Pełne zapytanie badań do zamknięcia (bez Bad_Data)
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
    Badanie.Bad_Data
FROM 
    (
        (B_Skierowania 
        INNER JOIN P_Pacjent ON B_Skierowania.B_Pacjent_ID = P_Pacjent.P_ID)
        LEFT JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
    )
    LEFT JOIN Badanie ON B_Skierowania.B_ID = Badanie.Bad_S_ID
WHERE 
    B_Skierowania.B_Activ = 1
    AND Badanie.Bad_Data IS NULL
ORDER BY 
    B_Skierowania.B_ID DESC";

            Console.WriteLine($"🔍 Executing SQL: {sql}");

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            Console.WriteLine($"✅ Query executed, reading results...");

            while (reader.Read())
            {
                var dto = new BadanieDto
                {
                    B_ID = reader["B_ID"] is DBNull ? 0 : Convert.ToInt32(reader["B_ID"]),
                    B_DataSkierowania = reader["B_DataSkierowania"] is DBNull ? null : Convert.ToDateTime(reader["B_DataSkierowania"]),
                    B_TypBadania = reader["B_TypBadania"]?.ToString(),
                    B_Stanowisko = reader["B_Stanowisko"]?.ToString(),
                    P_imie = reader["P_imie"]?.ToString(),
                    P_nazwisko = reader["P_nazwisko"]?.ToString(),
                    P_pesel = reader["P_pesel"]?.ToString(),
                    Firma_Nazwa = reader["Firma_Nazwa"]?.ToString(),
                    Firma_NIP = reader["Firma_NIP"]?.ToString(),
                    Bad_Data = reader["Bad_Data"] is DBNull ? null : Convert.ToDateTime(reader["Bad_Data"])
                };

                result.Add(dto);
            }

            Console.WriteLine($"✅ GetBadaniaDoZamkniecia: Loaded {result.Count} badań from MySQL");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetBadaniaDoZamkniecia error: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            throw;
        }

        return result;
    }

    /// <summary>
    /// Pobiera badania z wypełnionym wynikiem (mają już datę wykonania Bad_Data)
    /// </summary>
    public List<BadanieDto> GetBadaniaZamkniete()
    {
        var allBadania = GetBadaniaWithSkierowania();
        return allBadania.Where(b => b.Bad_Data != null).ToList();
    }

    /// <summary>
    /// Pobiera szczegółowe dane badania dla widoku "Zakończ Badanie" (BadaniaNewView)
    /// </summary>
    public BadanieDetailsDto? GetBadanieDetailsForNew(int bSkierowaniaId)
    {
        if (_dbFactory == null) return null;

        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            var sql = @"
SELECT
    B_Skierowania.B_ID,
    P_Pacjent.P_ID,
    Firma.id,
    Firma.Cennik,
    Firma.Nazwa,
    Firma.Ulica,
    Firma.Kod,
    Firma.Miejscowosc,
    Firma.NIP,
    P_Pacjent.P_pesel,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_zawód,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_Stanowisko,
    B_Skierowania.B_Zaswiadczenie,
    B_Skierowania.B_książeczka
FROM
    (
        P_Pacjent
        INNER JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
    )
    INNER JOIN B_Skierowania ON P_Pacjent.P_ID = B_Skierowania.B_Pacjent_ID
WHERE
    B_Skierowania.B_ID = @BID";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            var param = cmd.CreateParameter();
            param.ParameterName = "@BID";
            param.Value = bSkierowaniaId;
            cmd.Parameters.Add(param);

            Console.WriteLine($"🔍 GetBadanieDetailsForNew: Executing for B_ID={bSkierowaniaId}");

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var details = new BadanieDetailsDto
                {
                    B_ID = reader.GetInt32(reader.GetOrdinal("B_ID")),
                    P_ID = reader.GetInt32(reader.GetOrdinal("P_ID")),
                    Firma_id = reader.GetInt32(reader.GetOrdinal("id")),
                    Firma_Cennik = reader["Cennik"] as string,
                    Firma_Nazwa = reader["Nazwa"] as string,
                    Firma_Ulica = reader["Ulica"] as string,
                    Firma_Kod = reader["Kod"] as string,
                    Firma_Miejscowosc = reader["Miejscowosc"] as string,
                    Firma_NIP = reader["NIP"] as string,
                    P_pesel = reader["P_pesel"] as string,
                    P_imie = reader["P_imie"] as string,
                    P_nazwisko = reader["P_nazwisko"] as string,
                    P_zawod = reader["P_zawód"] as string,
                    B_DataSkierowania = reader["B_DataSkierowania"] as DateTime?,
                    B_TypBadania = reader["B_TypBadania"] as string,
                    B_Stanowisko = reader["B_Stanowisko"] as string,
                    B_Zaswiadczenie = reader["B_Zaswiadczenie"] as string,
                    B_ksiazeczka = reader["B_książeczka"] as string
                };

                Console.WriteLine($"✅ GetBadanieDetailsForNew: Loaded details for {details.P_imie} {details.P_nazwisko}");
                return details;
            }

            Console.WriteLine($"⚠️ GetBadanieDetailsForNew: No data found for B_ID={bSkierowaniaId}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetBadanieDetailsForNew error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Aktualizuje badanie
    /// </summary>
    public bool UpdateBadanie(BadanieDto badanie)
    {
        // TODO: Implement database update
        return true;
    }
}

