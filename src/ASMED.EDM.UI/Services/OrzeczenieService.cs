using System;
using System.Data.Common;
using System.Threading.Tasks;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models.Orzeczenie;
using MySqlConnector;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Odczyt i zapis orzeczenia lekarskiego (OrzeczenieView).
/// Orzeczenie jest zapisywane w całości dopiero przy zatwierdzeniu (po podpisie lekarza):
/// tabela Orzeczenie + badania specjalistyczne + flagi w B_Skierowania + status wizyty.
/// Tabela Badanie NIE jest tu używana — wypełnia ją dopiero moduł zakończenia/wyceny.
/// </summary>
public sealed class OrzeczenieService
{
    public const string StatusZatwierdzone = "Zatwierdzone";
    public const string StatusWizytyOrzeczenie = "Orzeczenie";

    private readonly DbConnectionFactory _dbFactory;

    public OrzeczenieService(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
    }

    /// <summary>
    /// Wczytuje istniejące orzeczenie dla skierowania (wraz z badaniami specjalistycznymi).
    /// Zwraca null, gdy orzeczenie dla skierowania jeszcze nie istnieje.
    /// </summary>
    public async Task<OrzeczenieZapisDto?> LoadAsync(int bId)
    {
        await using var conn = (MySqlConnection)_dbFactory.CreateConnection();
        await conn.OpenAsync();

        OrzeczenieZapisDto dto;
        await using (var cmd = new MySqlCommand("SELECT * FROM Orzeczenie WHERE O_B_ID = @b", conn))
        {
            cmd.Parameters.AddWithValue("@b", bId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
            {
                return null;
            }

            dto = new OrzeczenieZapisDto
            {
                OId = GetInt(r, "O_ID"),
                BId = bId,
                RId = GetInt(r, "O_R_ID"),
                PId = GetInt(r, "O_P_ID"),
                Numer = GetString(r, "O_Numer"),
                Status = GetString(r, "O_Status"),
                ZatwierdzonoAt = GetDate(r, "O_ZatwierdzonoAt"),
                TypBadania = GetString(r, "O_TypBadania"),
                Stanowisko = GetString(r, "O_Stanowisko"),
                DataBadania = GetDate(r, "O_DataBadania"),
                DataSkierowania = GetDate(r, "O_DataSkierowania"),
                DataNastepnegoBadania = GetDate(r, "O_DataNastepnegoBadania"),
                Werdykt = GetString(r, "O_Werdykt") ?? "21",
                DataUtratyZdolnosci = GetDate(r, "O_DataUtratyZdolnosci"),
                Uzasadnienie = GetString(r, "O_Uzasadnienie"),
                DoctorId = GetInt(r, "O_Doctor_ID"),
                Lekarz = GetString(r, "O_Lekarz"),
                LekarzPwz = GetString(r, "O_Lekarz_PWZ"),
                PodpisanoAt = GetDate(r, "O_PodpisanoAt"),
                Laryngolog = GetBool(r, "O_Laryngolog", false),
                Okulista = GetBool(r, "O_Okulista", false),
                Sanitarne = GetBool(r, "O_Sanitarne", false),
                Urlop = GetBool(r, "O_Urlop", false),
            };
        }

        await using (var cmd = new MySqlCommand("SELECT * FROM Badanie_Laryngologiczne WHERE BL_B_ID = @b", conn))
        {
            cmd.Parameters.AddWithValue("@b", bId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                dto.LarData = GetDate(r, "BL_Data");
                dto.LarWykonano = GetBool(r, "BL_Status_Wykonano", true);
                dto.LarVhiWypelniona = GetBool(r, "BL_VHI_Wypelniona", false);
                dto.LarVhiOpis = GetString(r, "BL_VHI_Opis");
                dto.LarKrtanPrawidlowa = GetBool(r, "BL_Krtan_Prawidlowa", true);
                dto.LarNosogardloPrawidlowe = GetBool(r, "BL_Nosogardlo_Prawidlowe", true);
                dto.LarSluchPrawidlowy = GetBool(r, "BL_Sluch_Prawidlowy", true);
                dto.LarBlonyPrawidlowe = GetBool(r, "BL_BlonyBebenkowe_Prawidlowe", true);
                dto.LarRownowagaPrawidlowa = GetBool(r, "BL_Rownowaga_Prawidlowa", true);
                dto.LarPrzeciwwskazaniaGlos = GetBool(r, "BL_PrzeciwwskazaniaGlos", false);
                dto.LarWynikPrawidlowy = GetBool(r, "BL_WynikOgolny_Prawidlowy", true);
                dto.LarUwagi = GetString(r, "BL_Uwagi");
            }
        }

        await using (var cmd = new MySqlCommand("SELECT * FROM Badanie_Okulistyczne WHERE BO_B_ID = @b", conn))
        {
            cmd.Parameters.AddWithValue("@b", bId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                dto.OkuData = GetDate(r, "BO_Data");
                dto.OkuWykonano = GetBool(r, "BO_Status_Wykonano", true);
                dto.OkuBarwyPrawidlowe = GetBool(r, "BO_WidzenieBarw_Prawidlowe", true);
                dto.OkuPolePrawidlowe = GetBool(r, "BO_PoleWidzenia_Prawidlowe", true);
                dto.OkuObuocznePrawidlowe = GetBool(r, "BO_WidzenieObuoczne_Prawidlowe", true);
                dto.OkuWynikPrawidlowy = GetBool(r, "BO_WynikOgolny_Prawidlowy", true);
                dto.OkuPrzeciwwskazaniaMonitor = GetBool(r, "BO_PrzeciwwskazaniaMonitor", false);
                dto.OkuUzywaOkularow = GetBool(r, "BO_UzywaOkularow", false);
                dto.OkuUwagi = GetString(r, "BO_Uwagi");
            }
        }

        await using (var cmd = new MySqlCommand("SELECT * FROM Badanie_Sanitarne WHERE BS_B_ID = @b", conn))
        {
            cmd.Parameters.AddWithValue("@b", bId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                dto.SanData = GetDate(r, "BS_Data");
                dto.SanWynik = GetString(r, "BS_Wynik") ?? "Pozytywny";
                dto.SanUwagi = GetString(r, "BS_Uwagi");
            }
        }

        await using (var cmd = new MySqlCommand("SELECT * FROM Badanie_Urlop WHERE BU_B_ID = @b", conn))
        {
            cmd.Parameters.AddWithValue("@b", bId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                dto.UrlopOd = GetDate(r, "BU_DataOd");
                dto.UrlopDo = GetDate(r, "BU_DataDo");
                dto.UrlopUzasadnienie = GetString(r, "BU_Uzasadnienie");
            }
        }

        return dto;
    }

    /// <summary>
    /// Zatwierdza (zapisuje w całości) podpisane orzeczenie w jednej transakcji.
    /// Przy pierwszym zatwierdzeniu nadaje numer "nr/rok"; ponowne zatwierdzenie
    /// tego samego skierowania aktualizuje orzeczenie z zachowaniem numeru.
    /// </summary>
    public async Task<(int OId, string Numer)> ZatwierdzAsync(OrzeczenieZapisDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await using var conn = (MySqlConnection)_dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var now = DateTime.Now;

            // 1. Skierowanie (pacjent, firma) — blokada wiersza na czas zapisu.
            int? pId;
            int? firmaId;
            await using (var cmd = new MySqlCommand(
                "SELECT B_Pacjent_ID, B_Firma_ID FROM B_Skierowania WHERE B_ID = @b FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("@b", dto.BId);
                await using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync())
                {
                    throw new InvalidOperationException($"Nie znaleziono skierowania B_ID={dto.BId}.");
                }

                pId = GetInt(r, "B_Pacjent_ID") ?? dto.PId;
                firmaId = GetInt(r, "B_Firma_ID");
            }

            // 2. Istniejące orzeczenie dla skierowania?
            int? oId = null;
            string? numer = null;
            await using (var cmd = new MySqlCommand(
                "SELECT O_ID, O_Numer FROM Orzeczenie WHERE O_B_ID = @b FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("@b", dto.BId);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    oId = GetInt(r, "O_ID");
                    numer = GetString(r, "O_Numer");
                }
            }

            // 3. Numeracja — tylko dla nowego orzeczenia (lub gdy numer nie został nadany).
            int? numerKolejny = null;
            int? rok = null;
            if (string.IsNullOrWhiteSpace(numer))
            {
                rok = (dto.DataBadania ?? now).Year;
                numerKolejny = await NastepnyNumerAsync(conn, tx, rok.Value);
                numer = $"{numerKolejny}/{rok}";
            }

            // 4. Orzeczenie
            if (oId == null)
            {
                await using var cmd = new MySqlCommand(@"
                    INSERT INTO Orzeczenie (
                        O_B_ID, O_R_ID, O_P_ID, O_Firma_ID,
                        O_Numer, O_NumerKolejny, O_Rok,
                        O_TypBadania, O_Stanowisko,
                        O_DataBadania, O_DataSkierowania, O_DataNastepnegoBadania,
                        O_Werdykt, O_DataUtratyZdolnosci, O_Uzasadnienie,
                        O_Laryngolog, O_Okulista, O_Sanitarne, O_Urlop,
                        O_Doctor_ID, O_Lekarz, O_Lekarz_PWZ,
                        O_Status, O_PodpisanoAt, O_ZatwierdzonoAt,
                        O_P1_Status, O_CreatedAt, O_CreatedBy_User_ID, O_UpdatedAt)
                    VALUES (
                        @b, @r, @p, @firma,
                        @numer, @nrKolejny, @rok,
                        @typ, @stanowisko,
                        @dataBad, @dataSkier, @dataNast,
                        @werdykt, @dataUtraty, @uzasadnienie,
                        @lar, @oku, @san, @urlop,
                        @doctorId, @lekarz, @pwz,
                        @status, @podpisano, @now,
                        0, @now, @userId, @now);
                    SELECT LAST_INSERT_ID();", conn, tx);
                AddOrzeczenieParams(cmd, dto, pId, firmaId, numer!, numerKolejny, rok, now);
                oId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            else
            {
                await using var cmd = new MySqlCommand(@"
                    UPDATE Orzeczenie SET
                        O_R_ID = @r, O_P_ID = @p, O_Firma_ID = @firma,
                        O_Numer = @numer,
                        O_NumerKolejny = COALESCE(@nrKolejny, O_NumerKolejny),
                        O_Rok = COALESCE(@rok, O_Rok),
                        O_TypBadania = @typ, O_Stanowisko = @stanowisko,
                        O_DataBadania = @dataBad, O_DataSkierowania = @dataSkier, O_DataNastepnegoBadania = @dataNast,
                        O_Werdykt = @werdykt, O_DataUtratyZdolnosci = @dataUtraty, O_Uzasadnienie = @uzasadnienie,
                        O_Laryngolog = @lar, O_Okulista = @oku, O_Sanitarne = @san, O_Urlop = @urlop,
                        O_Doctor_ID = @doctorId, O_Lekarz = @lekarz, O_Lekarz_PWZ = @pwz,
                        O_Status = @status, O_PodpisanoAt = @podpisano, O_ZatwierdzonoAt = @now,
                        O_UpdatedAt = @now
                    WHERE O_ID = @oId", conn, tx);
                AddOrzeczenieParams(cmd, dto, pId, firmaId, numer!, numerKolejny, rok, now);
                cmd.Parameters.AddWithValue("@oId", oId.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            // 5. Badania specjalistyczne (1:1 ze skierowaniem) — zapis lub usunięcie wg flag.
            await ZapiszLaryngologAsync(conn, tx, dto, pId, now);
            await ZapiszOkulistaAsync(conn, tx, dto, pId, now);
            await ZapiszSanitarneAsync(conn, tx, dto, pId, now);
            await ZapiszUrlopAsync(conn, tx, dto, pId, now);

            // 6. Skierowanie — powiązanie z orzeczeniem i flagi badań.
            await using (var cmd = new MySqlCommand(@"
                UPDATE B_Skierowania SET
                    B_orzeczenie = 1,
                    B_orzeczenie_ID = @oId,
                    B_DataSkierowania = @dataSkier,
                    B_laryngolog = @lar,
                    B_okulista = @oku,
                    B_ksiazeczka = @san,
                    B_Urlop = @urlop
                WHERE B_ID = @b", conn, tx))
            {
                cmd.Parameters.AddWithValue("@oId", oId.Value);
                cmd.Parameters.AddWithValue("@dataSkier", DbDate(dto.DataSkierowania));
                cmd.Parameters.AddWithValue("@lar", dto.Laryngolog ? 1 : 0);
                cmd.Parameters.AddWithValue("@oku", dto.Okulista ? 1 : 0);
                cmd.Parameters.AddWithValue("@san", dto.Sanitarne ? 1 : 0);
                cmd.Parameters.AddWithValue("@urlop", dto.Urlop ? 1 : 0);
                cmd.Parameters.AddWithValue("@b", dto.BId);
                await cmd.ExecuteNonQueryAsync();
            }

            // 7. Status wizyty
            if (dto.RId.HasValue)
            {
                await using var cmd = new MySqlCommand(
                    "UPDATE Rejestracja SET R_Status = @status WHERE R_ID = @r", conn, tx);
                cmd.Parameters.AddWithValue("@status", StatusWizytyOrzeczenie);
                cmd.Parameters.AddWithValue("@r", dto.RId.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return (oId.Value, numer!);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Ustawia licznik numeracji orzeczeń dla roku (np. reset przy przejściu na produkcję).
    /// Kolejne zatwierdzone orzeczenie otrzyma numer <paramref name="ostatniNumer"/> + 1.
    /// </summary>
    public async Task UstawNumeracjeAsync(int rok, int ostatniNumer)
    {
        await using var conn = (MySqlConnection)_dbFactory.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(@"
            INSERT INTO Orzeczenie_Numeracja (ON_Rok, ON_Ostatni, ON_UpdatedAt)
            VALUES (@rok, @ostatni, NOW())
            ON DUPLICATE KEY UPDATE ON_Ostatni = @ostatni, ON_UpdatedAt = NOW()", conn);
        cmd.Parameters.AddWithValue("@rok", rok);
        cmd.Parameters.AddWithValue("@ostatni", Math.Max(0, ostatniNumer));
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> NastepnyNumerAsync(MySqlConnection conn, MySqlTransaction tx, int rok)
    {
        // Upsert blokuje wiersz licznika do końca transakcji — numeracja bez luk i duplikatów
        // przy równoczesnym zatwierdzaniu z kilku stanowisk (rollback cofa również licznik).
        await using (var cmd = new MySqlCommand(@"
            INSERT INTO Orzeczenie_Numeracja (ON_Rok, ON_Ostatni, ON_UpdatedAt)
            VALUES (@rok, 1, NOW())
            ON DUPLICATE KEY UPDATE ON_Ostatni = ON_Ostatni + 1, ON_UpdatedAt = NOW()", conn, tx))
        {
            cmd.Parameters.AddWithValue("@rok", rok);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new MySqlCommand(
            "SELECT ON_Ostatni FROM Orzeczenie_Numeracja WHERE ON_Rok = @rok", conn, tx))
        {
            cmd.Parameters.AddWithValue("@rok", rok);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
    }

    private static void AddOrzeczenieParams(
        MySqlCommand cmd, OrzeczenieZapisDto dto, int? pId, int? firmaId,
        string numer, int? numerKolejny, int? rok, DateTime now)
    {
        cmd.Parameters.AddWithValue("@b", dto.BId);
        cmd.Parameters.AddWithValue("@r", (object?)dto.RId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p", (object?)pId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@firma", (object?)firmaId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@numer", numer);
        cmd.Parameters.AddWithValue("@nrKolejny", (object?)numerKolejny ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@rok", (object?)rok ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@typ", DbString(dto.TypBadania));
        cmd.Parameters.AddWithValue("@stanowisko", DbString(dto.Stanowisko));
        cmd.Parameters.AddWithValue("@dataBad", DbDate(dto.DataBadania));
        cmd.Parameters.AddWithValue("@dataSkier", DbDate(dto.DataSkierowania));
        cmd.Parameters.AddWithValue("@dataNast", DbDate(dto.DataNastepnegoBadania));
        cmd.Parameters.AddWithValue("@werdykt", dto.Werdykt);
        cmd.Parameters.AddWithValue("@dataUtraty", DbDate(dto.DataUtratyZdolnosci));
        cmd.Parameters.AddWithValue("@uzasadnienie", DbString(dto.Uzasadnienie));
        cmd.Parameters.AddWithValue("@lar", dto.Laryngolog ? 1 : 0);
        cmd.Parameters.AddWithValue("@oku", dto.Okulista ? 1 : 0);
        cmd.Parameters.AddWithValue("@san", dto.Sanitarne ? 1 : 0);
        cmd.Parameters.AddWithValue("@urlop", dto.Urlop ? 1 : 0);
        cmd.Parameters.AddWithValue("@doctorId", (object?)dto.DoctorId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lekarz", DbString(dto.Lekarz));
        cmd.Parameters.AddWithValue("@pwz", DbString(dto.LekarzPwz));
        cmd.Parameters.AddWithValue("@status", StatusZatwierdzone);
        cmd.Parameters.AddWithValue("@podpisano", (object?)dto.PodpisanoAt ?? now);
        cmd.Parameters.AddWithValue("@now", now);
        cmd.Parameters.AddWithValue("@userId", (object?)dto.CreatedByUserId ?? DBNull.Value);
    }

    private static async Task ZapiszLaryngologAsync(MySqlConnection conn, MySqlTransaction tx, OrzeczenieZapisDto dto, int? pId, DateTime now)
    {
        if (!dto.Laryngolog)
        {
            await DeleteByBIdAsync(conn, tx, "Badanie_Laryngologiczne", "BL_B_ID", dto.BId);
            return;
        }

        var exists = await ExistsByBIdAsync(conn, tx, "Badanie_Laryngologiczne", "BL_B_ID", dto.BId);
        await using var cmd = new MySqlCommand(exists
            ? @"UPDATE Badanie_Laryngologiczne SET
                    BL_P_ID = @p, BL_Data = @data, BL_Status_Wykonano = @wyk,
                    BL_VHI_Wypelniona = @vhi, BL_VHI_Opis = @vhiOpis,
                    BL_Krtan_Prawidlowa = @krtan, BL_Nosogardlo_Prawidlowe = @noso,
                    BL_Sluch_Prawidlowy = @sluch, BL_BlonyBebenkowe_Prawidlowe = @blony,
                    BL_Rownowaga_Prawidlowa = @rown, BL_PrzeciwwskazaniaGlos = @glos,
                    BL_WynikOgolny_Prawidlowy = @wynik, BL_Uwagi = @uwagi, BL_UpdatedAt = @now
                WHERE BL_B_ID = @b"
            : @"INSERT INTO Badanie_Laryngologiczne (
                    BL_B_ID, BL_P_ID, BL_Data, BL_Status_Wykonano, BL_VHI_Wypelniona, BL_VHI_Opis,
                    BL_Krtan_Prawidlowa, BL_Nosogardlo_Prawidlowe, BL_Sluch_Prawidlowy,
                    BL_BlonyBebenkowe_Prawidlowe, BL_Rownowaga_Prawidlowa, BL_PrzeciwwskazaniaGlos,
                    BL_WynikOgolny_Prawidlowy, BL_Uwagi, BL_CreatedAt, BL_UpdatedAt)
                VALUES (
                    @b, @p, @data, @wyk, @vhi, @vhiOpis,
                    @krtan, @noso, @sluch,
                    @blony, @rown, @glos,
                    @wynik, @uwagi, @now, @now)", conn, tx);

        cmd.Parameters.AddWithValue("@b", dto.BId);
        cmd.Parameters.AddWithValue("@p", (object?)pId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@data", DbDate(dto.LarData));
        cmd.Parameters.AddWithValue("@wyk", dto.LarWykonano ? 1 : 0);
        cmd.Parameters.AddWithValue("@vhi", dto.LarVhiWypelniona ? 1 : 0);
        cmd.Parameters.AddWithValue("@vhiOpis", DbString(Truncate(dto.LarVhiOpis, 100)));
        cmd.Parameters.AddWithValue("@krtan", dto.LarKrtanPrawidlowa ? 1 : 0);
        cmd.Parameters.AddWithValue("@noso", dto.LarNosogardloPrawidlowe ? 1 : 0);
        cmd.Parameters.AddWithValue("@sluch", dto.LarSluchPrawidlowy ? 1 : 0);
        cmd.Parameters.AddWithValue("@blony", dto.LarBlonyPrawidlowe ? 1 : 0);
        cmd.Parameters.AddWithValue("@rown", dto.LarRownowagaPrawidlowa ? 1 : 0);
        cmd.Parameters.AddWithValue("@glos", dto.LarPrzeciwwskazaniaGlos ? 1 : 0);
        cmd.Parameters.AddWithValue("@wynik", dto.LarWynikPrawidlowy ? 1 : 0);
        cmd.Parameters.AddWithValue("@uwagi", DbString(dto.LarUwagi));
        cmd.Parameters.AddWithValue("@now", now);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ZapiszOkulistaAsync(MySqlConnection conn, MySqlTransaction tx, OrzeczenieZapisDto dto, int? pId, DateTime now)
    {
        if (!dto.Okulista)
        {
            await DeleteByBIdAsync(conn, tx, "Badanie_Okulistyczne", "BO_B_ID", dto.BId);
            return;
        }

        var exists = await ExistsByBIdAsync(conn, tx, "Badanie_Okulistyczne", "BO_B_ID", dto.BId);
        await using var cmd = new MySqlCommand(exists
            ? @"UPDATE Badanie_Okulistyczne SET
                    BO_P_ID = @p, BO_Data = @data, BO_Status_Wykonano = @wyk,
                    BO_WidzenieBarw_Prawidlowe = @barwy, BO_PoleWidzenia_Prawidlowe = @pole,
                    BO_WidzenieObuoczne_Prawidlowe = @obu, BO_WynikOgolny_Prawidlowy = @wynik,
                    BO_PrzeciwwskazaniaMonitor = @monitor, BO_UzywaOkularow = @okulary,
                    BO_Uwagi = @uwagi, BO_UpdatedAt = @now
                WHERE BO_B_ID = @b"
            : @"INSERT INTO Badanie_Okulistyczne (
                    BO_B_ID, BO_P_ID, BO_Data, BO_Status_Wykonano,
                    BO_WidzenieBarw_Prawidlowe, BO_PoleWidzenia_Prawidlowe,
                    BO_WidzenieObuoczne_Prawidlowe, BO_WynikOgolny_Prawidlowy,
                    BO_PrzeciwwskazaniaMonitor, BO_UzywaOkularow, BO_Uwagi, BO_CreatedAt, BO_UpdatedAt)
                VALUES (
                    @b, @p, @data, @wyk,
                    @barwy, @pole,
                    @obu, @wynik,
                    @monitor, @okulary, @uwagi, @now, @now)", conn, tx);

        cmd.Parameters.AddWithValue("@b", dto.BId);
        cmd.Parameters.AddWithValue("@p", (object?)pId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@data", DbDate(dto.OkuData));
        cmd.Parameters.AddWithValue("@wyk", dto.OkuWykonano ? 1 : 0);
        cmd.Parameters.AddWithValue("@barwy", dto.OkuBarwyPrawidlowe ? 1 : 0);
        cmd.Parameters.AddWithValue("@pole", dto.OkuPolePrawidlowe ? 1 : 0);
        cmd.Parameters.AddWithValue("@obu", dto.OkuObuocznePrawidlowe ? 1 : 0);
        cmd.Parameters.AddWithValue("@wynik", dto.OkuWynikPrawidlowy ? 1 : 0);
        cmd.Parameters.AddWithValue("@monitor", dto.OkuPrzeciwwskazaniaMonitor ? 1 : 0);
        cmd.Parameters.AddWithValue("@okulary", dto.OkuUzywaOkularow ? 1 : 0);
        cmd.Parameters.AddWithValue("@uwagi", DbString(dto.OkuUwagi));
        cmd.Parameters.AddWithValue("@now", now);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ZapiszSanitarneAsync(MySqlConnection conn, MySqlTransaction tx, OrzeczenieZapisDto dto, int? pId, DateTime now)
    {
        if (!dto.Sanitarne)
        {
            await DeleteByBIdAsync(conn, tx, "Badanie_Sanitarne", "BS_B_ID", dto.BId);
            return;
        }

        var exists = await ExistsByBIdAsync(conn, tx, "Badanie_Sanitarne", "BS_B_ID", dto.BId);
        await using var cmd = new MySqlCommand(exists
            ? @"UPDATE Badanie_Sanitarne SET
                    BS_P_ID = @p, BS_Data = @data, BS_Wynik = @wynik, BS_Uwagi = @uwagi, BS_UpdatedAt = @now
                WHERE BS_B_ID = @b"
            : @"INSERT INTO Badanie_Sanitarne (BS_B_ID, BS_P_ID, BS_Data, BS_Wynik, BS_Uwagi, BS_CreatedAt, BS_UpdatedAt)
                VALUES (@b, @p, @data, @wynik, @uwagi, @now, @now)", conn, tx);

        cmd.Parameters.AddWithValue("@b", dto.BId);
        cmd.Parameters.AddWithValue("@p", (object?)pId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@data", DbDate(dto.SanData));
        cmd.Parameters.AddWithValue("@wynik", Truncate(dto.SanWynik, 30) ?? "Pozytywny");
        cmd.Parameters.AddWithValue("@uwagi", DbString(dto.SanUwagi));
        cmd.Parameters.AddWithValue("@now", now);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ZapiszUrlopAsync(MySqlConnection conn, MySqlTransaction tx, OrzeczenieZapisDto dto, int? pId, DateTime now)
    {
        if (!dto.Urlop)
        {
            await DeleteByBIdAsync(conn, tx, "Badanie_Urlop", "BU_B_ID", dto.BId);
            return;
        }

        var exists = await ExistsByBIdAsync(conn, tx, "Badanie_Urlop", "BU_B_ID", dto.BId);
        await using var cmd = new MySqlCommand(exists
            ? @"UPDATE Badanie_Urlop SET
                    BU_P_ID = @p, BU_DataOd = @od, BU_DataDo = @do, BU_Uzasadnienie = @uzas, BU_UpdatedAt = @now
                WHERE BU_B_ID = @b"
            : @"INSERT INTO Badanie_Urlop (BU_B_ID, BU_P_ID, BU_DataOd, BU_DataDo, BU_Uzasadnienie, BU_CreatedAt, BU_UpdatedAt)
                VALUES (@b, @p, @od, @do, @uzas, @now, @now)", conn, tx);

        cmd.Parameters.AddWithValue("@b", dto.BId);
        cmd.Parameters.AddWithValue("@p", (object?)pId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@od", DbDate(dto.UrlopOd));
        cmd.Parameters.AddWithValue("@do", DbDate(dto.UrlopDo));
        cmd.Parameters.AddWithValue("@uzas", DbString(dto.UrlopUzasadnienie));
        cmd.Parameters.AddWithValue("@now", now);
        await cmd.ExecuteNonQueryAsync();
    }

    // Nazwy tabel/kolumn pochodzą wyłącznie ze stałych w tej klasie (nigdy z danych użytkownika).
    private static async Task<bool> ExistsByBIdAsync(MySqlConnection conn, MySqlTransaction tx, string table, string bIdColumn, int bId)
    {
        await using var cmd = new MySqlCommand($"SELECT 1 FROM `{table}` WHERE {bIdColumn} = @b LIMIT 1 FOR UPDATE", conn, tx);
        cmd.Parameters.AddWithValue("@b", bId);
        return await cmd.ExecuteScalarAsync() != null;
    }

    private static async Task DeleteByBIdAsync(MySqlConnection conn, MySqlTransaction tx, string table, string bIdColumn, int bId)
    {
        await using var cmd = new MySqlCommand($"DELETE FROM `{table}` WHERE {bIdColumn} = @b", conn, tx);
        cmd.Parameters.AddWithValue("@b", bId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static object DbDate(DateTime? value) => value.HasValue ? value.Value : DBNull.Value;

    private static object DbString(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static string? Truncate(string? value, int max)
    {
        if (value == null) return null;
        value = value.Trim();
        return value.Length <= max ? value : value[..max];
    }

    private static int? GetInt(DbDataReader r, string column)
    {
        var v = r[column];
        return v == DBNull.Value ? null : Convert.ToInt32(v);
    }

    private static string? GetString(DbDataReader r, string column)
    {
        var v = r[column];
        return v == DBNull.Value ? null : v.ToString();
    }

    private static DateTime? GetDate(DbDataReader r, string column)
    {
        var v = r[column];
        return v == DBNull.Value ? null : Convert.ToDateTime(v);
    }

    private static bool GetBool(DbDataReader r, string column, bool defaultValue)
    {
        var v = r[column];
        return v == DBNull.Value ? defaultValue : Convert.ToBoolean(v);
    }
}
