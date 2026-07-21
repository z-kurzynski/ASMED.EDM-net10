using ASMED.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Helper do zarz�dzania cennikami firm (ODDZIELNY PLIK - nie modyfikuje AccessDbContext)
    /// </summary>
    public class CennikiHelper
    {
        private readonly AccessDbHelper _db;

        public CennikiHelper()
        {
            _db = new AccessDbHelper();
        }

        // ==============================
        // FIRMA - OPERACJE
        // ==============================

        /// <summary>
        /// Pobiera wszystkie AKTYWNE firmy z bazy danych
        /// </summary>
        public List<FirmaRow> GetAllFirmy()
        {
            var firmy = new List<FirmaRow>();

            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();

                    // ? POPRAWNE SQL - tylko aktywne firmy
                    var cmd = new OdbcCommand(@"
                        SELECT 
                            Firma.id, 
                            Firma.Cennik, 
                            Firma.Nazwa, 
                            Firma.activ
                        FROM Firma
                        WHERE Firma.activ = True
                        ORDER BY Firma.Nazwa",
                        conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            firmy.Add(new FirmaRow
                            {
                                Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                Cennik = reader["Cennik"]?.ToString() ?? string.Empty,
                                Nazwa = reader["Nazwa"]?.ToString() ?? string.Empty,
                                IsSelected = false
                            });
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"CennikiHelper: Za�adowano {firmy.Count} aktywnych firm");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiHelper: B��d �adowania firm: {ex.Message}");
            }

            return firmy;
        }

        /// <summary>
        /// Aktualizuje cennik firmy
        /// </summary>
        public bool UpdateFirmaCennik(int firmaId, string nowyCennik)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();
                    var cmd = new OdbcCommand(
                        "UPDATE Firma SET Cennik = ? WHERE id = ?",
                        conn);

                    cmd.Parameters.AddWithValue("@Cennik", nowyCennik);
                    cmd.Parameters.AddWithValue("@id", firmaId);

                    int affected = cmd.ExecuteNonQuery();
                    // System.Diagnostics.Debug.WriteLine($"CennikiHelper: Zaktualizowano cennik firmy ID={firmaId}");

                    return affected > 0;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiHelper: B��d aktualizacji: {ex.Message}");
                throw;
            }
        }

        // ==============================
        // CENNIK - OPERACJE
        // ==============================

        /// <summary>
        /// Pobiera list� wszystkich AKTYWNYCH cennik�w z BAD_Lista
        /// </summary>
        public List<CennikRow> GetAllCenniki()
        {
            var cenniki = new List<CennikRow>();

            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();

                    // ? POPRAWNE SQL - z tabeli BAD_Lista, tylko aktywne cenniki
                    var cmd = new OdbcCommand(@"
                        SELECT DISTINCT 
                            BAD_Lista.bn_cennik, 
                            BAD_Lista.bn_Cen_activ
                        FROM BAD_Lista
                        WHERE BAD_Lista.bn_Cen_activ = True",
                        conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var nazwa = reader["bn_cennik"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(nazwa))
                            {
                                cenniki.Add(new CennikRow { Nazwa = nazwa });
                            }
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"CennikiHelper: Za�adowano {cenniki.Count} aktywnych cennik�w");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiHelper: B��d �adowania cennik�w: {ex.Message}");
            }

            return cenniki;
        }

        /// <summary>
        /// Pobiera pozycje (ceny) dla konkretnego cennika z BAD_Cennik
        /// </summary>
        public List<CennikPozycjaRow> GetCennikPozycje(string nazwaCennika)
        {
            var pozycje = new List<CennikPozycjaRow>();

            try
            {
                // System.Diagnostics.Debug.WriteLine($"GetCennikPozycje: �adowanie '{nazwaCennika}'");

                using (var conn = _db.GetConnection())
                {
                    conn.Open();

                    // ? POPRAWNE SQL - z tabeli BAD_Cennik
                    var cmd = new OdbcCommand(@"
                        SELECT DISTINCT 
                            BAD_Cennik.Identyfikator,
                            BAD_Cennik.b_Nazwa,
                            BAD_Cennik.b_Cena,
                            BAD_Cennik.b_Vat,
                            BAD_Cennik.b_Cennik,
                            BAD_Cennik.b_activ
                        FROM BAD_Cennik
                        WHERE BAD_Cennik.b_Cennik = ? 
                          AND BAD_Cennik.b_activ = True
                        ORDER BY BAD_Cennik.b_Nazwa",
                        conn);

                    cmd.Parameters.AddWithValue("@b_Cennik", nazwaCennika);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var pozycja = new CennikPozycjaRow
                            {
                                Nazwa = reader["b_Nazwa"]?.ToString() ?? string.Empty,
                                Cena = reader["b_Cena"] != DBNull.Value ? Convert.ToDecimal(reader["b_Cena"]) : 0m
                            };

                            pozycje.Add(pozycja);

                            // System.Diagnostics.Debug.WriteLine($"  - {pozycja.Nazwa}: {pozycja.Cena:N2} z�");
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"GetCennikPozycje: Za�adowano {pozycje.Count} pozycji");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"GetCennikPozycje: B��d: {ex.Message}");
            }

            return pozycje;
        }

        /// <summary>
        /// Tworzy nowy cennik - dodaje nazw� do BAD_Lista i pozycje do BAD_Cennik
        /// </summary>
        public bool CreateCennik(string nazwaCennika)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();

                    // ? KROK 1: Dodaj nazw� cennika do BAD_Lista
                    // System.Diagnostics.Debug.WriteLine($"CreateCennik: Dodaj� cennik '{nazwaCennika}' do BAD_Lista");

                    var insertCennikCmd = new OdbcCommand(@"
                        INSERT INTO BAD_Lista (bn_cennik, bn_Cen_activ, bn_activ, bn_nazwa) 
                        VALUES (?, ?, ?, ?)",
                        conn);

                    insertCennikCmd.Parameters.AddWithValue("@bn_cennik", nazwaCennika);
                    insertCennikCmd.Parameters.AddWithValue("@bn_Cen_activ", true);    // Cennik aktywny
                    insertCennikCmd.Parameters.AddWithValue("@bn_activ", false);       // To nie jest badanie, tylko definicja cennika
                    insertCennikCmd.Parameters.AddWithValue("@bn_nazwa", nazwaCennika); // Nazwa taka sama jak cennik

                    try
                    {
                        insertCennikCmd.ExecuteNonQuery();
                        // System.Diagnostics.Debug.WriteLine($"CreateCennik: ? Dodano cennik '{nazwaCennika}' do BAD_Lista");
                    }
                    catch (OdbcException odbcEx)
                    {
                        // Je�li cennik ju� istnieje, zignoruj b��d
                        if (odbcEx.Message.Contains("duplicate") || odbcEx.Message.Contains("naruszenie"))
                        {
                            // System.Diagnostics.Debug.WriteLine($"CreateCennik: ?? Cennik '{nazwaCennika}' ju� istnieje w BAD_Lista");
                        }
                        else
                        {
                            throw;
                        }
                    }

                    // ? KROK 2: Pobierz struktur� nazw bada� z BAD_Lista (aktywne badania)
                    // System.Diagnostics.Debug.WriteLine("CreateCennik: Pobieram list� aktywnych bada� z BAD_Lista");

                    var strukturaCmd = new OdbcCommand(@"
                        SELECT DISTINCT 
                            BAD_Lista.Identyfikator,
                            BAD_Lista.bn_nazwa,
                            BAD_Lista.bn_activ
                        FROM BAD_Lista
                        WHERE BAD_Lista.bn_activ = True
                          AND BAD_Lista.bn_nazwa IS NOT NULL
                          AND BAD_Lista.bn_nazwa <> ''",
                        conn);

                    var nazwyCen = new List<string>();
                    using (var reader = strukturaCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var nazwa = reader["bn_nazwa"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(nazwa))
                            {
                                nazwyCen.Add(nazwa);
                            }
                        }
                    }

                    // System.Diagnostics.Debug.WriteLine($"CreateCennik: Znaleziono {nazwyCen.Count} aktywnych bada�");

                    // Je�li brak struktury w BAD_Lista, u�yj domy�lnych nazw
                    if (!nazwyCen.Any())
                    {
                        // System.Diagnostics.Debug.WriteLine("CreateCennik: Brak bada� w BAD_Lista - u�ywam domy�lnych nazw");

                        nazwyCen = new List<string>
                        {
                            "Lekarz", "Laryngolog", "Okulista",
                            "Ksi��eczka (Sanepid)", "Lipidogram", "EKG",
                            "Urlop (Zdrowie)", "Inne", "Rezerwa1"
                        };
                    }

                    // ? KROK 3: Dodaj pozycje (ceny) do BAD_Cennik
                    // System.Diagnostics.Debug.WriteLine($"CreateCennik: Dodaj� {nazwyCen.Count} pozycji do BAD_Cennik");

                    var insertCmd = new OdbcCommand(@"
                        INSERT INTO BAD_Cennik (b_Cennik, b_Nazwa, b_Cena, b_Vat, b_activ) 
                        VALUES (?, ?, ?, ?, ?)",
                        conn);

                    int insertedCount = 0;
                    foreach (var nazwaCeny in nazwyCen)
                    {
                        try
                        {
                            insertCmd.Parameters.Clear();
                            insertCmd.Parameters.AddWithValue("@b_Cennik", nazwaCennika);
                            insertCmd.Parameters.AddWithValue("@b_Nazwa", nazwaCeny);
                            insertCmd.Parameters.AddWithValue("@b_Cena", 0);  // Domy�lna cena 0
                            insertCmd.Parameters.AddWithValue("@b_Vat", 0);   // Domy�lny VAT 0
                            insertCmd.Parameters.AddWithValue("@b_activ", true);
                            insertCmd.ExecuteNonQuery();
                            insertedCount++;
                        }
                        catch (OdbcException odbcEx)
                        {
                            // System.Diagnostics.Debug.WriteLine($"CreateCennik: ?? Nie mo�na doda� pozycji '{nazwaCeny}': {odbcEx.Message}");
                        }
                    }

                    // System.Diagnostics.Debug.WriteLine($"CreateCennik: ? Utworzono cennik '{nazwaCennika}' z {insertedCount}/{nazwyCen.Count} pozycjami");
                    return insertedCount > 0;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CreateCennik: ? B��d tworzenia cennika: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"CreateCennik: StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Aktualizuje pozycje cennika w BAD_Cennik
        /// </summary>
        public bool UpdateCennikPozycje(string nazwaCennika, List<CennikPozycjaRow> pozycje)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();

                    // ? POPRAWNE SQL - aktualizacja w BAD_Cennik
                    var cmd = new OdbcCommand(@"
                        UPDATE BAD_Cennik 
                        SET b_Cena = ? 
                        WHERE b_Cennik = ? AND b_Nazwa = ?",
                        conn);

                    int updatedCount = 0;
                    foreach (var pozycja in pozycje)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@b_Cena", Convert.ToInt32(pozycja.Cena));  // Cena jako integer
                        cmd.Parameters.AddWithValue("@b_Cennik", nazwaCennika);
                        cmd.Parameters.AddWithValue("@b_Nazwa", pozycja.Nazwa);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            updatedCount++;
                            // System.Diagnostics.Debug.WriteLine($"  Updated: {pozycja.Nazwa} = {pozycja.Cena:N0} z�");
                        }
                    }

                    // System.Diagnostics.Debug.WriteLine($"CennikiHelper: Zaktualizowano {updatedCount}/{pozycje.Count} pozycji cennika '{nazwaCennika}'");
                    return updatedCount > 0;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"CennikiHelper: B��d aktualizacji: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Usuwa cennik - ustawia b_activ = False w BAD_Cennik i bn_Cen_activ = False w BAD_Lista
        /// (SOFT DELETE - nie usuwa fizycznie rekord�w)
        /// </summary>
        public bool DeleteCennik(string nazwaCennika)
        {
            try
            {
                using (var conn = _db.GetConnection())
                {
                    conn.Open();

                    // ? KROK 1: Dezaktywuj cennik w BAD_Lista
                    // System.Diagnostics.Debug.WriteLine($"DeleteCennik: Dezaktywuj� cennik '{nazwaCennika}' w BAD_Lista");

                    var updateListaCmd = new OdbcCommand(@"
                        UPDATE BAD_Lista 
                        SET bn_Cen_activ = False 
                        WHERE bn_cennik = ?",
                        conn);

                    updateListaCmd.Parameters.AddWithValue("@bn_cennik", nazwaCennika);
                    int affectedLista = updateListaCmd.ExecuteNonQuery();

                    // System.Diagnostics.Debug.WriteLine($"DeleteCennik: Dezaktywowano {affectedLista} rekord�w w BAD_Lista");

                    // ? KROK 2: Dezaktywuj wszystkie pozycje cennika w BAD_Cennik
                    // System.Diagnostics.Debug.WriteLine($"DeleteCennik: Dezaktywuj� pozycje cennika '{nazwaCennika}' w BAD_Cennik");

                    var updateCennikCmd = new OdbcCommand(@"
                        UPDATE BAD_Cennik 
                        SET b_activ = False 
                        WHERE b_Cennik = ?",
                        conn);

                    updateCennikCmd.Parameters.AddWithValue("@b_Cennik", nazwaCennika);
                    int affectedCennik = updateCennikCmd.ExecuteNonQuery();

                    // System.Diagnostics.Debug.WriteLine($"DeleteCennik: ? Dezaktywowano cennik '{nazwaCennika}':");
                    // System.Diagnostics.Debug.WriteLine($"  - BAD_Lista: {affectedLista} rekord�w");
                    // System.Diagnostics.Debug.WriteLine($"  - BAD_Cennik: {affectedCennik} pozycji");

                    return affectedLista > 0 || affectedCennik > 0;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"DeleteCennik: ? B��d usuwania: {ex.Message}");
                throw;
            }
        }
    }
}
