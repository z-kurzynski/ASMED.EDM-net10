using ASMED.EDM.Core.DTOs;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models.ListaDoFaktur;
using MySqlConnector;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Serwis TYLKO dla widoku "Lista do Faktur" (ListaFaktAddView).
/// Własne połączenie MySQL na każde zapytanie — niezależny od innych zakładek.
/// </summary>
public class ListaFaktAddService
{
    private readonly DbConnectionFactory? _dbFactory;

    public ListaFaktAddService(DbConnectionFactory? dbFactory = null)
    {
        _dbFactory = dbFactory;
    }

    // ===== FIRMY =====
    public List<FirmaDto> GetFirmy()
    {
        var result = new List<FirmaDto>();
        if (_dbFactory == null) return result;

        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            const string sql = @"
                SELECT id, Nazwa, NIP, Activ, Cennik, FkEmail
                FROM Firma
                WHERE Activ = 1
                ORDER BY Nazwa";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new FirmaDto
                {
                    Id     = reader.IsDBNull(0) ? 0    : reader.GetInt32(0),
                    Nazwa  = reader.IsDBNull(1) ? null : reader.GetString(1),
                    NIP    = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Activ  = reader.IsDBNull(3) ? null : reader.GetInt32(3) == 1,
                    Cennik = reader.IsDBNull(4) ? null : reader.GetString(4),
                    FkEmail= reader.IsDBNull(5) ? null : reader.GetString(5),
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ListaFaktAddService] GetFirmy ERROR: {ex.Message}");
        }
        return result;
    }

    // ===== CENNIKI (nazwy) =====
    public List<string> GetCennikiNazwy()
    {
        var result = new List<string>();
        if (_dbFactory == null) return result;

        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            const string sql = @"
                SELECT DISTINCT Nazwa
                FROM Cennik
                WHERE Activ = 1
                ORDER BY Nazwa";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                if (!reader.IsDBNull(0))
                    result.Add(reader.GetString(0));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ListaFaktAddService] GetCennikiNazwy ERROR: {ex.Message}");
        }
        return result;
    }

    // ===== IMIONA =====
    public List<string> GetImiona()
    {
        var result = new List<string>();
        if (_dbFactory == null) return result;

        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            const string sql = @"
                SELECT DISTINCT P_Pacjent.P_imie
                FROM Badanie
                LEFT JOIN P_Pacjent ON P_Pacjent.P_ID = Badanie.Bad_P_ID
                WHERE (Badanie.Bad_L_ID    IS NULL OR Badanie.Bad_L_ID    = 0)
                  AND (Badanie.Bad_Fakt_ID IS NULL OR Badanie.Bad_Fakt_ID = 0)
                  AND P_Pacjent.P_imie IS NOT NULL
                ORDER BY P_Pacjent.P_imie";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                if (!reader.IsDBNull(0))
                    result.Add(reader.GetString(0));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ListaFaktAddService] GetImiona ERROR: {ex.Message}");
        }
        return result;
    }

    // ===== NAZWISKA =====
    public List<string> GetNazwiska()
    {
        var result = new List<string>();
        if (_dbFactory == null) return result;

        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            const string sql = @"
                SELECT DISTINCT P_Pacjent.P_nazwisko
                FROM Badanie
                LEFT JOIN P_Pacjent ON P_Pacjent.P_ID = Badanie.Bad_P_ID
                WHERE (Badanie.Bad_L_ID    IS NULL OR Badanie.Bad_L_ID    = 0)
                  AND (Badanie.Bad_Fakt_ID IS NULL OR Badanie.Bad_Fakt_ID = 0)
                  AND P_Pacjent.P_nazwisko IS NOT NULL
                ORDER BY P_Pacjent.P_nazwisko";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                if (!reader.IsDBNull(0))
                    result.Add(reader.GetString(0));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ListaFaktAddService] GetNazwiska ERROR: {ex.Message}");
        }
        return result;
    }

    // ===== BADANIA DO FAKTUROWANIA =====
    public List<BadanieRowDto> GetAvailableBadania(int? firmaId = null)
    {
        var result = new List<BadanieRowDto>();
        if (_dbFactory == null) return result;

        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            const string sql = @"
                SELECT
                    Badanie.Bad_ID,
                    Badanie.Bad_L_ID,
                    Badanie.Bad_F_ID,
                    Badanie.Bad_P_ID,
                    Badanie.Bad_Data,
                    Badanie.Bad_Data_Do,
                    Badanie.Bad_Typ,
                    Badanie.Bad_Wynik,
                    Badanie.Bad_Nr_KS,
                    Badanie.Bad_END,
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
                    P_Pacjent.P_imie,
                    P_Pacjent.P_nazwisko,
                    P_Pacjent.P_zawod,
                    Firma.Nazwa   AS FirmaNazwa,
                    Firma.Cennik  AS FirmaCennik,
                    Badanie.Bad_S_ID
                FROM Badanie
                LEFT JOIN P_Pacjent ON P_Pacjent.P_ID = Badanie.Bad_P_ID
                LEFT JOIN Firma     ON Firma.id        = P_Pacjent.P_Firma_id
                WHERE (Badanie.Bad_L_ID    IS NULL OR Badanie.Bad_L_ID    = 0)
                  AND (Badanie.Bad_Fakt_ID IS NULL OR Badanie.Bad_Fakt_ID = 0)
                ORDER BY Badanie.Bad_Data DESC";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new BadanieRowDto
                {
                    Bad_ID        = reader.IsDBNull(0)  ? null : reader.GetInt32(0),
                    Bad_L_ID      = reader.IsDBNull(1)  ? null : reader.GetInt32(1),
                    Bad_F_ID      = reader.IsDBNull(2)  ? null : reader.GetInt32(2),
                    Bad_P_ID      = reader.IsDBNull(3)  ? null : reader.GetInt32(3),
                    Bad_Data      = reader.IsDBNull(4)  ? null : reader.GetDateTime(4),
                    Bad_Data_Do   = reader.IsDBNull(5)  ? null : reader.GetDateTime(5),
                    Bad_Typ       = reader.IsDBNull(6)  ? null : reader.GetString(6),
                    Bad_Wynik     = reader.IsDBNull(7)  ? null : reader.GetString(7),
                    Bad_Nr_KS     = reader.IsDBNull(8)  ? null : reader.GetString(8),
                    Bad_END       = reader.IsDBNull(9)  ? null : (bool?)Convert.ToBoolean(reader.GetInt32(9)),
                    Bad_bn_cennik = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Bad_Razem     = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
                    Bad_Cena1     = reader.IsDBNull(12) ? null : reader.GetDecimal(12),
                    Bad_Cena2     = reader.IsDBNull(13) ? null : reader.GetDecimal(13),
                    Bad_Cena3     = reader.IsDBNull(14) ? null : reader.GetDecimal(14),
                    Bad_Cena4     = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                    Bad_Cena5     = reader.IsDBNull(16) ? null : reader.GetDecimal(16),
                    Bad_Cena6     = reader.IsDBNull(17) ? null : reader.GetDecimal(17),
                    Bad_Cena7     = reader.IsDBNull(18) ? null : reader.GetDecimal(18),
                    Bad_Cena8     = reader.IsDBNull(19) ? null : reader.GetDecimal(19),
                    Bad_Cena9     = reader.IsDBNull(20) ? null : reader.GetDecimal(20),
                    Bad_Cena10    = reader.IsDBNull(21) ? null : reader.GetDecimal(21),
                    P_imie        = reader.IsDBNull(22) ? null : reader.GetString(22),
                    P_nazwisko    = reader.IsDBNull(23) ? null : reader.GetString(23),
                    P_zawod       = reader.IsDBNull(24) ? null : reader.GetString(24),
                    FirmaNazwa    = reader.IsDBNull(25) ? null : reader.GetString(25),
                    FirmaCennik   = reader.IsDBNull(26) ? null : reader.GetString(26),
                    Bad_S_ID      = reader.IsDBNull(27) ? null : reader.GetInt32(27),
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ListaFaktAddService] GetAvailableBadania ERROR: {ex.Message}");
        }
        return result;
    }
}
