using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models;
using ASMED.EDM.UI.Models.BadaniaNew;
using MySqlConnector;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Serwis TYLKO dla widoku "Zakończ Badanie" (BadaniaNewView)
/// Obsługuje badania do zamknięcia (bez Bad_Data)
/// </summary>
public class BadaniaNewService
{
    private readonly DbConnectionFactory? _dbFactory;

    public BadaniaNewService(DbConnectionFactory? dbFactory = null)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Pobiera aktywny cennik dla ComboBox select_cennik
    /// MUSI być wywołane PRZED GetBadaniaDoZamkniecia()
    /// </summary>
    public List<CennikDto> GetCennikList()
    {
        var result = new List<CennikDto>();
        if (_dbFactory == null) return result;

        try
        {
            Console.WriteLine("🔍 BadaniaNewService.GetCennikList: Loading active cennik...");

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
                    bn_cennik = reader["bn_cennik"]?.ToString(),  // TEKST - nazwa cennika
                    bn_Cen_activ = reader["bn_Cen_activ"] is DBNull ? false : Convert.ToBoolean(reader["bn_Cen_activ"])
                });
            }

            Console.WriteLine($"✅ BadaniaNewService: Loaded {result.Count} active cennik entries");

            // Debug: logujemy pierwsze 3 cenniki
            foreach (var c in result.Take(3))
            {
                Console.WriteLine($"   📋 Cennik: ID={c.Identyfikator}, Name='{c.bn_cennik}', Active={c.bn_Cen_activ}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BadaniaNewService.GetCennikList error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Pobiera listę badań do zamknięcia (bez Bad_Data) dla BadaniaNewView
    /// </summary>
    public List<BadaniaNewListDto> GetBadaniaDoZamkniecia()
    {
        var result = new List<BadaniaNewListDto>();
        if (_dbFactory == null) return result;

        try
        {
            Console.WriteLine($"🔍 BadaniaNewService.GetBadaniaDoZamkniecia: Connecting to MySQL...");

            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            Console.WriteLine($"✅ Connected to MySQL");

            var sql = @"
SELECT 
    B_Skierowania.B_ID,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_Stanowisko,
    B_Skierowania.B_Zaswiadczenie,
    B_Skierowania.B_ksiazeczka,
    B_Skierowania.B_ankieta,
    B_Skierowania.B_wywiad,
    B_Skierowania.B_orzeczenie,
    B_Skierowania.B_orzeczenie_ID,
    B_Skierowania.B_Activ,
    B_Skierowania.B_uwagi,
    B_Skierowania.B_edm_document_id,
    P_Pacjent.P_ID,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_pesel,
    P_Pacjent.P_zawod,
    P_Pacjent.P_Firma_id AS FirmaId,
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
                var dto = new BadaniaNewListDto
                {
                    B_ID = reader["B_ID"] is DBNull ? 0 : Convert.ToInt32(reader["B_ID"]),
                    B_DataSkierowania = reader["B_DataSkierowania"] is DBNull ? null : Convert.ToDateTime(reader["B_DataSkierowania"]),
                    B_TypBadania = reader["B_TypBadania"]?.ToString(),
                    B_Stanowisko = reader["B_Stanowisko"]?.ToString(),
                    B_Zaswiadczenie = reader["B_Zaswiadczenie"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["B_Zaswiadczenie"]),
                    B_ksiazeczka = reader["B_ksiazeczka"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["B_ksiazeczka"]),
                    B_ankieta = reader["B_ankieta"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["B_ankieta"]),
                    B_wywiad = reader["B_wywiad"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["B_wywiad"]),
                    B_orzeczenie = reader["B_orzeczenie"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["B_orzeczenie"]),
                    B_orzeczenie_ID = reader["B_orzeczenie_ID"] is DBNull ? (int?)null : Convert.ToInt32(reader["B_orzeczenie_ID"]),
                    B_Activ = reader["B_Activ"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["B_Activ"]),
                    B_uwagi = reader["B_uwagi"]?.ToString(),
                    B_edm_document_id = reader["B_edm_document_id"] is DBNull ? (int?)null : Convert.ToInt32(reader["B_edm_document_id"]),
                    P_ID = reader["P_ID"] is DBNull ? 0 : Convert.ToInt32(reader["P_ID"]),
                    P_imie = reader["P_imie"]?.ToString(),
                    P_nazwisko = reader["P_nazwisko"]?.ToString(),
                    P_pesel = reader["P_pesel"]?.ToString(),
                    P_zawod = reader["P_zawod"]?.ToString(),
                    FirmaId = reader["FirmaId"] is DBNull ? null : Convert.ToInt32(reader["FirmaId"]),
                    Firma_Nazwa = reader["Firma_Nazwa"]?.ToString(),
                    Firma_NIP = reader["Firma_NIP"]?.ToString(),
                    Bad_Data = reader["Bad_Data"] is DBNull ? null : Convert.ToDateTime(reader["Bad_Data"])
                };

                result.Add(dto);
            }

            Console.WriteLine($"✅ BadaniaNewService.GetBadaniaDoZamkniecia: Loaded {result.Count} badań from MySQL");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BadaniaNewService.GetBadaniaDoZamkniecia error: {ex.Message}");
            throw;
        }

        return result;
    }

    /// <summary>
    /// Pobiera szczegóły badania dla BadaniaNewView po kliknięciu w listę
    /// SQL dostarczony przez usera dla BadaniaNewView
    /// </summary>
    public BadaniaNewDetailsDto? GetBadanieDetails(int bSkierowaniaId)
    {
        if (_dbFactory == null) return null;

        try
        {
            Console.WriteLine($"🔍 BadaniaNewService.GetBadanieDetails: Loading B_ID={bSkierowaniaId}");

            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            // SQL dostarczony przez usera dla BadaniaNewView (prawa kolumna)
            var sql = @"
SELECT
    B_Skierowania.B_ID,
    P_Pacjent.P_ID,
    Firma.id,
    B_Skierowania.B_Badanie_ID,
    Firma.Nazwa,
    Firma.Ulica,
    Firma.Kod,
    Firma.Miejscowosc,
    Firma.NIP,
    Firma.Cennik,
    P_Pacjent.P_pesel,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_Stanowisko,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_Zaswiadczenie,
    B_Skierowania.B_ksiazeczka,
    B_Skierowania.B_laryngolog,
    B_Skierowania.B_okulista,
    B_Skierowania.B_EKG,
    B_Skierowania.B_Lipidogram,
    B_Skierowania.B_ankieta,
    B_Skierowania.B_wywiad,
    B_Skierowania.B_orzeczenie,
    B_Skierowania.B_orzeczenie_ID,
    B_Skierowania.B_Activ,
    B_Skierowania.B_uwagi,
    B_Skierowania.B_edm_document_id
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

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                var details = new BadaniaNewDetailsDto
                {
                    B_ID = reader["B_ID"] is DBNull ? 0 : Convert.ToInt32(reader["B_ID"]),
                    B_Badanie_ID = reader["B_Badanie_ID"] is DBNull ? 0 : Convert.ToInt32(reader["B_Badanie_ID"]),
                    B_DataSkierowania = reader["B_DataSkierowania"] is DBNull ? null : Convert.ToDateTime(reader["B_DataSkierowania"]),
                    B_TypBadania = reader["B_TypBadania"]?.ToString(),
                    B_Stanowisko = reader["B_Stanowisko"]?.ToString(),
                    B_Zaswiadczenie = reader["B_Zaswiadczenie"] is DBNull ? null : Convert.ToBoolean(reader["B_Zaswiadczenie"]),
                    B_ksiazeczka = reader["B_ksiazeczka"] is DBNull ? null : Convert.ToBoolean(reader["B_ksiazeczka"]),
                    B_laryngolog = reader["B_laryngolog"] is DBNull ? null : Convert.ToBoolean(reader["B_laryngolog"]),
                    B_okulista = reader["B_okulista"] is DBNull ? null : Convert.ToBoolean(reader["B_okulista"]),
                    B_EKG = reader["B_EKG"] is DBNull ? null : Convert.ToBoolean(reader["B_EKG"]),
                    B_Lipidogram = reader["B_Lipidogram"] is DBNull ? null : Convert.ToBoolean(reader["B_Lipidogram"]),
                    B_ankieta = reader["B_ankieta"] is DBNull ? null : Convert.ToBoolean(reader["B_ankieta"]),
                    B_wywiad = reader["B_wywiad"] is DBNull ? null : Convert.ToBoolean(reader["B_wywiad"]),
                    B_orzeczenie = reader["B_orzeczenie"] is DBNull ? null : Convert.ToBoolean(reader["B_orzeczenie"]),
                    B_orzeczenie_ID = reader["B_orzeczenie_ID"] is DBNull ? null : Convert.ToInt32(reader["B_orzeczenie_ID"]),
                    B_Activ = reader["B_Activ"] is DBNull ? null : Convert.ToBoolean(reader["B_Activ"]),
                    B_uwagi = reader["B_uwagi"]?.ToString(),
                    B_edm_document_id = reader["B_edm_document_id"] is DBNull ? null : Convert.ToInt32(reader["B_edm_document_id"]),

                    P_ID = reader["P_ID"] is DBNull ? 0 : Convert.ToInt32(reader["P_ID"]),
                    P_pesel = reader["P_pesel"]?.ToString(),
                    P_imie = reader["P_imie"]?.ToString(),
                    P_nazwisko = reader["P_nazwisko"]?.ToString(),

                    Firma_id = reader["id"] is DBNull ? 0 : Convert.ToInt32(reader["id"]),
                    Firma_Nazwa = reader["Nazwa"]?.ToString(),
                    Firma_Ulica = reader["Ulica"]?.ToString(),
                    Firma_Kod = reader["Kod"]?.ToString(),
                    Firma_Miejscowosc = reader["Miejscowosc"]?.ToString(),
                    Firma_NIP = reader["NIP"]?.ToString(),
                    Firma_Cennik = reader["Cennik"]?.ToString()
                };

                Console.WriteLine($"✅ BadaniaNewService: Loaded details for {details.P_imie} {details.P_nazwisko} from {details.Firma_Nazwa}");
                return details;
            }

            Console.WriteLine($"⚠️ BadaniaNewService: No details found for B_ID={bSkierowaniaId}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BadaniaNewService.GetBadanieDetails error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Pobiera ceny z cennika na podstawie nazwy cennika (b_Cennik)
    /// Zwraca Dictionary z nazwami usług (b_Nazwa) jako klucze i cenami (b_Cena) jako wartości
    /// </summary>
    public Dictionary<string, decimal?>? GetCennikPrices(string bnCennik)
    {
        if (_dbFactory == null || string.IsNullOrEmpty(bnCennik)) return null;

        try
        {
            Console.WriteLine($"🔍 BadaniaNewService.GetCennikPrices: Loading prices for cennik='{bnCennik}'");

            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            // Pobierz WSZYSTKIE aktywne usługi dla tego cennika
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
            cmd.Parameters.Add(new MySqlConnector.MySqlParameter("@b_Cennik", bnCennik));

            using var reader = cmd.ExecuteReader();

            var prices = new Dictionary<string, decimal?>();

            while (reader.Read())
            {
                string? nazwa = reader["b_Nazwa"]?.ToString();
                if (string.IsNullOrEmpty(nazwa)) continue;

                decimal? cena = reader["b_Cena"] is DBNull ? null : Convert.ToDecimal(reader["b_Cena"]);

                prices[nazwa] = cena;
                Console.WriteLine($"  📊 {nazwa} = {cena:N2} zł");
            }

            Console.WriteLine($"✅ BadaniaNewService: Loaded {prices.Count} prices for cennik '{bnCennik}'");
            return prices.Count > 0 ? prices : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ BadaniaNewService.GetCennikPrices error: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    #region === CENNIKI CRUD ===

    /// <summary>
    /// Pobiera wszystkie firmy z przypisanym cennikiem (tabela Firma)
    /// </summary>
    public List<FirmaWithCennikDto> GetAllFirmy()
    {
        var result = new List<FirmaWithCennikDto>();
        if (_dbFactory == null) return result;
        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id, Nazwa, NIP, Cennik FROM Firma ORDER BY Nazwa";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new FirmaWithCennikDto
                {
                    Id      = reader["id"]    is DBNull ? 0 : Convert.ToInt32(reader["id"]),
                    Nazwa   = reader["Nazwa"]?.ToString(),
                    NIP     = reader["NIP"]?.ToString(),
                    Cennik  = reader["Cennik"]?.ToString()
                });
            }
            Console.WriteLine($"✅ GetAllFirmy: Loaded {result.Count} firms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetAllFirmy error: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// Pobiera wszystkie unikalne cenniki ze wszystkich źródeł:
    /// BAD_Lista.bn_cennik  (słownik cenników),
    /// BAD_Cennik.b_Cennik  (pozycje cenowe),
    /// Firma.Cennik         (cenniki już przypisane do firm).
    /// Pojedyncze zapytanie UNION ALL — bezpieczne w MySQL strict mode.
    /// </summary>
    public List<CennikItem> GetAllCennikItems()
    {
        var result = new List<CennikItem>();
        if (_dbFactory == null) return result;
        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();

            // Krok 1 – unikalne nazwy ze WSZYSTKICH źródeł (UNION usuwa duplikaty)
            cmd.CommandText = @"
SELECT DISTINCT nazwa FROM (
    SELECT bn_cennik AS nazwa FROM BAD_Lista  WHERE bn_cennik IS NOT NULL AND bn_cennik <> ''
    UNION
    SELECT b_Cennik  AS nazwa FROM BAD_Cennik WHERE b_Cennik  IS NOT NULL AND b_Cennik  <> ''
    UNION
    SELECT Cennik    AS nazwa FROM Firma      WHERE Cennik    IS NOT NULL AND Cennik    <> ''
) AS all_cenniki
ORDER BY nazwa";

            var nazwy = new List<string>();
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                {
                    string? n = reader["nazwa"]?.ToString();
                    if (!string.IsNullOrEmpty(n)) nazwy.Add(n);
                }

            // Krok 2 – pobierz Identyfikator z BAD_Lista dla tych nazw (MIN żeby uniknąć GROUP BY strict)
            var identMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var activeMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (nazwy.Count > 0)
            {
                cmd.CommandText = @"
SELECT bn_cennik, MIN(Identyfikator) AS id, MAX(bn_Cen_activ) AS activ
FROM BAD_Lista
WHERE bn_cennik IS NOT NULL AND bn_cennik <> ''
GROUP BY bn_cennik";
                using var reader2 = cmd.ExecuteReader();
                while (reader2.Read())
                {
                    string? n = reader2["bn_cennik"]?.ToString();
                    if (string.IsNullOrEmpty(n)) continue;
                    identMap[n]  = reader2["id"]    is DBNull ? 0     : Convert.ToInt32(reader2["id"]);
                    activeMap[n] = reader2["activ"] is not DBNull && Convert.ToBoolean(reader2["activ"]);
                }
            }

            // Krok 3 – złóż wynikową listę
            foreach (string nazwa in nazwy)
            {
                result.Add(new CennikItem
                {
                    Identyfikator = identMap.TryGetValue(nazwa, out int id) ? id : 0,
                    Nazwa         = nazwa,
                    IsActive      = activeMap.TryGetValue(nazwa, out bool a) ? a : true
                });
            }

            Console.WriteLine($"✅ GetAllCennikItems: {result.Count} cenniki (BAD_Lista + BAD_Cennik + Firma)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetAllCennikItems error: {ex.Message}\n{ex.StackTrace}");
        }
        return result;
    }

    /// <summary>
    /// Pobiera wszystkie pozycje cennika z BAD_Cennik (aktywne) jako listę edytowalnych DTO
    /// </summary>
    public List<CennikPozycjaDto> GetCennikPozycje(string cennikName)
    {
        var result = new List<CennikPozycjaDto>();
        if (_dbFactory == null || string.IsNullOrEmpty(cennikName)) return result;
        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT b_Nazwa, b_Cena
FROM BAD_Cennik
WHERE b_Cennik = @cennik AND b_activ = 1
ORDER BY b_Nazwa";
            var p = cmd.CreateParameter(); p.ParameterName = "@cennik"; p.Value = cennikName; cmd.Parameters.Add(p);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new CennikPozycjaDto
                {
                    Nazwa = reader["b_Nazwa"]?.ToString() ?? string.Empty,
                    Cena  = reader["b_Cena"] is DBNull ? null : Convert.ToDecimal(reader["b_Cena"])
                });
            }
            Console.WriteLine($"✅ GetCennikPozycje '{cennikName}': {result.Count} pozycji");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetCennikPozycje error: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// Dodaje nowy cennik do BAD_Lista
    /// </summary>
    public bool AddCennik(string nazwaCennika)
    {
        if (_dbFactory == null || string.IsNullOrWhiteSpace(nazwaCennika)) return false;
        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO BAD_Lista (bn_cennik, bn_Cen_activ) VALUES (@nazwa, 1)";
            var p = cmd.CreateParameter(); p.ParameterName = "@nazwa"; p.Value = nazwaCennika.Trim(); cmd.Parameters.Add(p);
            int rows = cmd.ExecuteNonQuery();
            Console.WriteLine($"✅ AddCennik '{nazwaCennika}': {rows} rows");
            return rows > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AddCennik error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Usuwa cennik z BAD_Lista i wszystkie jego ceny z BAD_Cennik
    /// </summary>
    public bool DeleteCennik(int identyfikator, string nazwaCennika)
    {
        if (_dbFactory == null) return false;
        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            // Usuń ceny z BAD_Cennik
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM BAD_Cennik WHERE b_Cennik = @nazwa";
                var p = cmd.CreateParameter(); p.ParameterName = "@nazwa"; p.Value = nazwaCennika; cmd.Parameters.Add(p);
                cmd.ExecuteNonQuery();
            }

            // Usuń wpis z BAD_Lista
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM BAD_Lista WHERE Identyfikator = @id";
                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = identyfikator; cmd.Parameters.Add(p);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"✅ DeleteCennik '{nazwaCennika}' (id={identyfikator})");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ DeleteCennik error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Zapisuje (DELETE + INSERT) ceny dla cennika w tabeli BAD_Cennik
    /// </summary>
    public bool SaveCennikPrices(string cennikName, IEnumerable<CennikPozycjaDto> pozycje)
    {
        if (_dbFactory == null || string.IsNullOrEmpty(cennikName)) return false;
        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            // Usuń stare pozycje
            using (var delCmd = connection.CreateCommand())
            {
                delCmd.CommandText = "DELETE FROM BAD_Cennik WHERE b_Cennik = @cennik";
                var dp = delCmd.CreateParameter(); dp.ParameterName = "@cennik"; dp.Value = cennikName; delCmd.Parameters.Add(dp);
                delCmd.ExecuteNonQuery();
            }

            // Wstaw nowe pozycje
            foreach (var poz in pozycje)
            {
                if (string.IsNullOrWhiteSpace(poz.Nazwa)) continue;
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
INSERT INTO BAD_Cennik (b_Cennik, b_Nazwa, b_Cena, b_activ)
VALUES (@cennik, @nazwa, @cena, 1)";
                var p = cmd.CreateParameter(); p.ParameterName = "@cennik"; p.Value = cennikName; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@nazwa";  p.Value = poz.Nazwa; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@cena";   p.Value = poz.Cena.HasValue ? (object)poz.Cena.Value : DBNull.Value; cmd.Parameters.Add(p);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"✅ SaveCennikPrices '{cennikName}': OK");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SaveCennikPrices error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Aktualizuje pole Cennik w tabeli Firma dla wskazanych firm
    /// </summary>
    public bool UpdateFirmaCennik(IEnumerable<int> firmaIds, string? cennikName)
    {
        if (_dbFactory == null) return false;
        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();
            foreach (var id in firmaIds)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Firma SET Cennik = @cennik WHERE id = @id";
                var p = cmd.CreateParameter(); p.ParameterName = "@cennik"; p.Value = cennikName ?? (object)DBNull.Value; cmd.Parameters.Add(p);
                p = cmd.CreateParameter(); p.ParameterName = "@id";     p.Value = id; cmd.Parameters.Add(p);
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine($"✅ UpdateFirmaCennik '{cennikName}': updated {firmaIds.Count()} firms");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ UpdateFirmaCennik error: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region === SAVE OPERATIONS ===

    /// <summary>
    /// 1. INSERT Badanie - zwraca nowe Bad_ID z LAST_INSERT_ID()
    /// </summary>
    public async Task<int> InsertBadanieAsync(BadanieInsertDto dto)
    {
        if (_dbFactory == null)
            throw new InvalidOperationException("DbConnectionFactory is null");

        string sql = @"
            INSERT INTO Badanie (
                Bad_S_ID, Bad_P_ID, Bad_F_ID, Bad_Fakt_ID, Bad_bn_cennik, Bad_Typ, 
                Bad_Data, Bad_Data_Do, Bad_Wynik, 
                Bad_Cena1, Bad_Cena2, Bad_Cena3, Bad_Cena4, 
                Bad_Cena5, Bad_Cena6, Bad_Cena7, Bad_Cena8, 
                Bad_Razem, Bad_Nr_KS, Bad_END
            ) VALUES (
                @Bad_S_ID, @Bad_P_ID, NULL, NULL, @Bad_bn_cennik, @Bad_Typ,
                @Bad_Data, @Bad_Data_Do, @Bad_Wynik,
                @Bad_Cena1, @Bad_Cena2, @Bad_Cena3, @Bad_Cena4,
                @Bad_Cena5, @Bad_Cena6, @Bad_Cena7, @Bad_Cena8,
                @Bad_Razem, @Bad_Nr_KS, @Bad_END
            )";

        string selectLastId = "SELECT LAST_INSERT_ID()";

        try
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            var p = cmd.CreateParameter(); p.ParameterName = "@Bad_S_ID"; p.Value = dto.Bad_S_ID; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_P_ID"; p.Value = dto.Bad_P_ID; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_F_ID"; p.Value = dto.Bad_F_ID; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_bn_cennik"; p.Value = dto.Bad_bn_cennik ?? (object)DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Typ"; p.Value = dto.Bad_Typ ?? (object)DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Data"; p.Value = dto.Bad_Data.HasValue ? (object)dto.Bad_Data.Value : DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Data_Do"; p.Value = dto.Bad_Data_Do.HasValue ? (object)dto.Bad_Data_Do.Value : DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Wynik"; p.Value = dto.Bad_Wynik ?? (object)DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena1"; p.Value = dto.Bad_Cena1; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena2"; p.Value = dto.Bad_Cena2; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena3"; p.Value = dto.Bad_Cena3; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena4"; p.Value = dto.Bad_Cena4; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena5"; p.Value = dto.Bad_Cena5; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena6"; p.Value = dto.Bad_Cena6; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena7"; p.Value = dto.Bad_Cena7; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Cena8"; p.Value = dto.Bad_Cena8; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Razem"; p.Value = dto.Bad_Razem; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_Nr_KS"; p.Value = dto.Bad_Nr_KS ?? (object)DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Bad_END"; p.Value = dto.Bad_END; cmd.Parameters.Add(p);

            await cmd.ExecuteNonQueryAsync();

            // Get last inserted ID
            using var cmdId = conn.CreateCommand();
            cmdId.CommandText = selectLastId;
            var result = await cmdId.ExecuteScalarAsync();
            int newBadId = Convert.ToInt32(result);

            Console.WriteLine($"✅ InsertBadanieAsync: Created Badanie Bad_ID={newBadId}");
            return newBadId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ InsertBadanieAsync error: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 2. UPDATE B_Skierowania.B_Badanie_ID - linkuje skierowanie do badania
    /// </summary>
    public async Task<bool> UpdateSkierowanieBadanieIdAsync(int bId, int badId)
    {
        if (_dbFactory == null)
            throw new InvalidOperationException("DbConnectionFactory is null");

        try
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            // Sprawdź czy zły FK (wskazujący na BAD_Lista) nadal istnieje i usuń go
            string dbName = conn.Database;
            string checkFkSql = $@"
                SELECT COUNT(*) FROM information_schema.REFERENTIAL_CONSTRAINTS
                WHERE CONSTRAINT_SCHEMA = '{dbName}'
                  AND TABLE_NAME = 'B_Skierowania'
                  AND REFERENCED_TABLE_NAME = 'BAD_Lista'";

            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = checkFkSql;
                int badFkCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (badFkCount > 0)
                {
                    Console.WriteLine("⚠️ Znaleziono błędny FK na BAD_Lista — naprawiam...");

                    // Pobierz nazwę constrainta
                    checkCmd.CommandText = $@"
                        SELECT CONSTRAINT_NAME FROM information_schema.REFERENTIAL_CONSTRAINTS
                        WHERE CONSTRAINT_SCHEMA = '{dbName}'
                          AND TABLE_NAME = 'B_Skierowania'
                          AND REFERENCED_TABLE_NAME = 'BAD_Lista'
                        LIMIT 1";
                    string? constraintName = (await checkCmd.ExecuteScalarAsync())?.ToString();

                    if (!string.IsNullOrEmpty(constraintName))
                    {
                        // Krok 1: DROP stary FK
                        using var dropCmd = conn.CreateCommand();
                        dropCmd.CommandText = $"ALTER TABLE `B_Skierowania` DROP FOREIGN KEY `{constraintName}`";
                        try { await dropCmd.ExecuteNonQueryAsync(); } catch { }

                        // Krok 2: NULL wiszące wartości
                        using var nullCmd = conn.CreateCommand();
                        nullCmd.CommandText = @"UPDATE `B_Skierowania` SET `B_Badanie_ID` = NULL
                            WHERE `B_Badanie_ID` IS NOT NULL
                              AND `B_Badanie_ID` NOT IN (SELECT `Bad_ID` FROM `Badanie`)";
                        try { await nullCmd.ExecuteNonQueryAsync(); } catch { }

                        // Krok 3: Nowy poprawny FK
                        using var addCmd = conn.CreateCommand();
                        addCmd.CommandText = @"ALTER TABLE `B_Skierowania`
                            ADD CONSTRAINT `B_Skierowania_fk_badanie`
                            FOREIGN KEY (`B_Badanie_ID`) REFERENCES `Badanie` (`Bad_ID`)
                            ON DELETE SET NULL";
                        try { await addCmd.ExecuteNonQueryAsync(); } catch { }

                        Console.WriteLine("✅ FK naprawiony: B_Badanie_ID → Badanie(Bad_ID)");
                    }
                }
            }

            // Teraz wykonaj właściwy UPDATE
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE B_Skierowania SET B_Badanie_ID = @badId WHERE B_ID = @bId";
            var p = cmd.CreateParameter(); p.ParameterName = "@badId"; p.Value = badId; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@bId"; p.Value = bId; cmd.Parameters.Add(p);

            int rows = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ UpdateSkierowanieBadanieIdAsync: Linked B_ID={bId} → Bad_ID={badId} ({rows} rows)");
            return rows > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ UpdateSkierowanieBadanieIdAsync error: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 3. UPDATE B_Skierowania.B_DataSkierowania + inne pola (gdy B_DataSkierowania był NULL)
    /// </summary>
    public async Task<bool> UpdateSkierowanieDataSkierowaniaAsync(int bId, DateTime dataSkierowania, SkierowanieUpdateDto dto)
    {
        if (_dbFactory == null)
            throw new InvalidOperationException("DbConnectionFactory is null");

        string sql = @"
            UPDATE B_Skierowania 
            SET B_DataSkierowania = @dataSkierowania,
                B_Pacjent_ID = @PacjentId,
                B_Firma_ID = @FirmaId,
                B_TypBadania = @TypBadania,
                B_Stanowisko = @Stanowisko,
                B_Zaswiadczenie = @Zaswiadczenie,
                B_ksiazeczka = @Ksiazeczka,
                B_Activ = 1
            WHERE B_ID = @bId";

        try
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var p = cmd.CreateParameter(); p.ParameterName = "@dataSkierowania"; p.Value = dataSkierowania; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@bId"; p.Value = bId; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@PacjentId"; p.Value = dto.PacjentId.HasValue ? (object)dto.PacjentId.Value : DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@FirmaId"; p.Value = dto.FirmaId.HasValue ? (object)dto.FirmaId.Value : DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@TypBadania"; p.Value = dto.TypBadania ?? (object)DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Stanowisko"; p.Value = dto.Stanowisko ?? (object)DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Zaswiadczenie"; p.Value = dto.Zaswiadczenie; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@Ksiazeczka"; p.Value = dto.Ksiazeczka; cmd.Parameters.Add(p);

            int rows = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ UpdateSkierowanieDataSkierowaniaAsync: Updated B_ID={bId} DataSkierowania={dataSkierowania:yyyy-MM-dd} ({rows} rows)");
            return rows > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ UpdateSkierowanieDataSkierowaniaAsync error: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 4. INSERT Rejestracja (gdy nie istnieje dla R_S_ID)
    /// </summary>
    public async Task<bool> InsertRejestracjaAsync(RejestracjaInsertDto dto)
    {
        if (_dbFactory == null)
            throw new InvalidOperationException("DbConnectionFactory is null");

        string sql = @"
            INSERT INTO Rejestracja (R_Data, R_Status, R_S_ID, R_GG_MM, R_Subject, R_Uwagi)
            VALUES (@R_Data, @R_Status, @R_S_ID, @R_GG_MM, @R_Subject, @R_Uwagi)";

        try
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var p = cmd.CreateParameter(); p.ParameterName = "@R_Data"; p.Value = dto.R_Data.HasValue ? (object)dto.R_Data.Value : DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@R_Status"; p.Value = dto.R_Status ?? (object)DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@R_S_ID"; p.Value = dto.R_S_ID; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@R_GG_MM"; p.Value = dto.R_GG_MM.HasValue ? (object)dto.R_GG_MM.Value : DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@R_Subject"; p.Value = dto.R_Subject ?? (object)DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@R_Uwagi"; p.Value = dto.R_Uwagi ?? (object)DBNull.Value; cmd.Parameters.Add(p);

            int rows = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ InsertRejestracjaAsync: Created Rejestracja for R_S_ID={dto.R_S_ID} ({rows} rows)");
            return rows > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ InsertRejestracjaAsync error: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 5. UPDATE Rejestracja.R_Status (TYLKO status, reszta bez zmian)
    /// </summary>
    public async Task<bool> UpdateRejestracjaStatusAsync(int rId, string newStatus)
    {
        if (_dbFactory == null)
            throw new InvalidOperationException("DbConnectionFactory is null");

        string sql = "UPDATE Rejestracja SET R_Status = @newStatus WHERE R_ID = @rId";

        try
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var p = cmd.CreateParameter(); p.ParameterName = "@newStatus"; p.Value = newStatus; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@rId"; p.Value = rId; cmd.Parameters.Add(p);

            int rows = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ UpdateRejestracjaStatusAsync: Set R_ID={rId} status='{newStatus}' ({rows} rows)");
            return rows > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ UpdateRejestracjaStatusAsync error: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 6. Sprawdzenie istnienia Rejestracji dla R_S_ID
    /// </summary>
    public async Task<RejestracjaDto?> GetRejestracjaBySIdAsync(int sId)
    {
        if (_dbFactory == null)
            return null;

        string sql = "SELECT R_ID, R_Data, R_Status, R_S_ID, R_GG_MM, R_Subject, R_Uwagi FROM Rejestracja WHERE R_S_ID = @sId";

        try
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var p = cmd.CreateParameter(); p.ParameterName = "@sId"; p.Value = sId; cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var result = new RejestracjaDto
                {
                    R_ID = reader["R_ID"] is DBNull ? null : Convert.ToInt32(reader["R_ID"]),
                    R_Data = reader["R_Data"] is DBNull ? null : Convert.ToDateTime(reader["R_Data"]),
                    R_Status = reader["R_Status"]?.ToString(),
                    R_S_ID = reader["R_S_ID"] is DBNull ? 0 : Convert.ToInt32(reader["R_S_ID"]),
                    R_GG_MM = reader["R_GG_MM"] is DBNull ? null : Convert.ToDateTime(reader["R_GG_MM"]),
                    R_Subject = reader["R_Subject"]?.ToString(),
                    R_Uwagi = reader["R_Uwagi"]?.ToString()
                };

                Console.WriteLine($"🔍 GetRejestracjaBySIdAsync: R_S_ID={sId} → R_ID={result.R_ID}");
                return result;
            }

            Console.WriteLine($"🔍 GetRejestracjaBySIdAsync: R_S_ID={sId} → NOT FOUND");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetRejestracjaBySIdAsync error: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    #endregion

    /// <summary>
    /// Aktualizuje na bieżąco pojedynczą flagę statusu (np. B_ksiazeczka, B_laryngolog)
    /// w tabeli B_Skierowania dla wskazanego rekordu. Nazwa kolumny musi pochodzić
    /// z bezpiecznej, wewnętrznie kontrolowanej listy (nigdy z danych wejściowych użytkownika).
    /// </summary>
    public bool UpdateBSkierowaniaFlag(int bSkierowaniaId, string columnName, bool value)
    {
        if (_dbFactory == null)
        {
            Console.WriteLine($"⚠️ UpdateBSkierowaniaFlag: DbConnectionFactory not available, cannot update {columnName}");
            return false;
        }

        try
        {
            using var connection = _dbFactory.CreateConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"UPDATE B_Skierowania SET {columnName} = @val WHERE B_ID = @id";

            var valParam = cmd.CreateParameter();
            valParam.ParameterName = "@val";
            valParam.Value = value ? 1 : 0;
            cmd.Parameters.Add(valParam);

            var idParam = cmd.CreateParameter();
            idParam.ParameterName = "@id";
            idParam.Value = bSkierowaniaId;
            cmd.Parameters.Add(idParam);

            var affected = cmd.ExecuteNonQuery();

            if (affected == 0)
            {
                Console.WriteLine($"⚠️ UpdateBSkierowaniaFlag: brak zaktualizowanych wierszy dla B_ID={bSkierowaniaId}, kolumna={columnName}");
            }
            else
            {
                Console.WriteLine($"✅ UpdateBSkierowaniaFlag: {columnName}={value} zapisane dla B_ID={bSkierowaniaId}");
            }

            return affected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ UpdateBSkierowaniaFlag error ({columnName}, B_ID={bSkierowaniaId}): {ex.Message}");
            return false;
        }
    }
}
