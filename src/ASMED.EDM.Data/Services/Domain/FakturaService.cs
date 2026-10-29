using ASMED.EDM.Core.DTOs;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.Data.Services;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Windows;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Serwis fakturowania â€” transakcyjny zapis list badaĹ„ i faktur.
/// Migracja z Access SQL (AccessDbContext.AddFaktura / SaveListOfInvoiceAsync).
/// </summary>
public class FakturaService : IFakturaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FakturaService> _logger;
    private readonly DbConnectionFactory _connectionFactory;
    private readonly AccessAutoSyncService? _autoSync;

    public FakturaService(
        IUnitOfWork unitOfWork,
        ILogger<FakturaService> logger,
        DbConnectionFactory connectionFactory,
        AccessAutoSyncService? autoSync = null)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _autoSync = autoSync;
    }

    // â”€â”€ Firmy â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<IEnumerable<Firma>> GetActiveFirmyAsync(string? searchQuery = null, CancellationToken ct = default)
    {
        return await _unitOfWork.Firmy.GetActiveFirmyAsync(searchQuery, ct);
    }

    // â”€â”€ Cenniki â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<IEnumerable<string>> GetActiveCennikNazwyAsync(CancellationToken ct = default)
    {
        var cenniki = await _unitOfWork.Cenniki.GetActiveCennikiAsync(ct);
        return cenniki.Select(c => c.Nazwa);
    }

    public async Task<Cennik?> GetCennikByNazwaAsync(string nazwa, CancellationToken ct = default)
    {
        return await _unitOfWork.Cenniki.GetByNazwaAsync(nazwa, ct);
    }

    // â”€â”€ Badania â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<IEnumerable<Badanie>> GetAvailableBadaniaAsync(int? firmaId = null, CancellationToken ct = default)
    {
        return await _unitOfWork.Badania.GetAvailableBadaniaAsync(firmaId, ct);
    }

    public async Task<IEnumerable<BadanieRowDto>> GetAvailableBadaniaRowsAsync(CancellationToken ct = default)
    {
        return await _unitOfWork.Badania.GetAvailableBadaniaRowsAsync(null, ct);
    }

    // â”€â”€ Listy badaĹ„ â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task<ListaBadan?> GetListaBadanAsync(int id, CancellationToken ct = default)
    {
        return await _unitOfWork.ListyBadan.GetWithBadaniaAsync(id, ct);
    }

    // â”€â”€ Zapis transakcyjny â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Transakcyjny zapis listy badań + faktury (Dapper + transakcja MySQL).
    /// Odpowiednik: SaveListOfInvoiceAsync z legacy ListaFaktAddViewModel.
    ///
    /// Algorytm (zgodny z legacy):
    ///  1. Walidacja (firma, pozycje, pacjent dla pozycji dodanych ręcznie)
    ///  2. Znajdź (TRIM(FK_Numer)) lub utwórz Faktura
    ///  3. Utwórz lub zaktualizuj ListyBadan — przy modyfikacji odepnij poprzednie badania
    ///  4. Każda pozycja: znajdź/utwórz pacjenta, znajdź/utwórz skierowanie, INSERT/UPDATE Badanie
    ///  5. Faktura: FK_Suma_Bad = suma Bad_Razem z listy, FK_Num_Listy = Id listy
    ///  6. Commit — przy błędzie rollback
    ///
    /// Semantyka kolumn Badanie: Bad_F_ID = Bad_Fakt_ID = Id faktury, Bad_Fakt = numer faktury, Bad_L_ID = Id listy.
    /// </summary>
    public async Task<SaveListaBadanResult> SaveListaBadanAsync(SaveListaBadanRequest request, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var pozycje = request.Pozycje?.ToList();
        var badanieIds = pozycje == null ? request.BadanieIds.Distinct().ToList() : new List<int>();
        int liczbaPozycji = pozycje?.Count ?? badanieIds.Count;

        _logger.LogInformation("Zapis listy badań — FirmaId={FirmaId}, NumerFaktury={Numer}, Pozycje={Count}, ListaId={ListaId}",
            request.FirmaId, request.NumerFaktury, liczbaPozycji, request.IstniejacaListaBadanId);

        // ── Walidacja ──────────────────────────────────────────────────────────
        if (liczbaPozycji == 0)
            return new SaveListaBadanResult(false, null, 0, "Brak pozycji na liście — nic do zapisania.");

        if (request.FirmaId <= 0)
            return new SaveListaBadanResult(false, null, 0, "Nie wybrano firmy.");

        if (pozycje != null)
        {
            var bezPacjenta = pozycje.FirstOrDefault(p => !p.Bad_P_ID.HasValue
                && (string.IsNullOrWhiteSpace(p.P_imie) || string.IsNullOrWhiteSpace(p.P_nazwisko)));
            if (bezPacjenta != null)
                return new SaveListaBadanResult(false, null, 0,
                    $"Pozycja Lp {bezPacjenta.Lp}: brak pacjenta — podaj imię i nazwisko.");
        }

        var numer = string.IsNullOrWhiteSpace(request.NumerFaktury) ? null : request.NumerFaktury.Trim();
        var cennik = string.IsNullOrWhiteSpace(request.NazwaCennika) ? null : request.NazwaCennika.Trim();

        // Kopie robocze — Id trafiają do DTO dopiero po commit, żeby rollback nie zostawił Id nieistniejących rekordów
        var robocze = pozycje?.Select(p => (Zrodlo: p, Bad_ID: p.Bad_ID, Bad_P_ID: p.Bad_P_ID, Bad_S_ID: p.Bad_S_ID)).ToList();

        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            // ── Krok 1/5: firma i faktura ─────────────────────────────────────────
            progress?.Report("Krok 1/5 — weryfikacja firmy i faktury...");

            var firma = await conn.QueryFirstOrDefaultAsync<FirmaRow>(new CommandDefinition(
                "SELECT id AS Id, Nazwa FROM Firma WHERE id = @Id",
                new { Id = request.FirmaId }, tx, cancellationToken: ct))
                ?? throw new InvalidOperationException($"Firma Id={request.FirmaId} nie istnieje.");

            int? fakturaId = null;
            if (numer != null)
            {
                var istniejaca = await conn.QueryFirstOrDefaultAsync<FakturaRow>(new CommandDefinition(
                    "SELECT FK_ID AS Id, FK_Firma_ID AS FirmaId FROM Faktura WHERE TRIM(FK_Numer) = @Numer ORDER BY FK_ID LIMIT 1",
                    new { Numer = numer }, tx, cancellationToken: ct));

                if (istniejaca != null)
                {
                    if (istniejaca.FirmaId is int fFirma && fFirma != 0 && fFirma != request.FirmaId)
                        throw new InvalidOperationException($"Faktura nr {numer} jest już wystawiona dla innej firmy.");

                    fakturaId = istniejaca.Id;
                    progress?.Report($"Krok 1/5 — znaleziono fakturę nr {numer} (Id={fakturaId}).");
                }
                else
                {
                    fakturaId = await InsertRowAsync(conn, tx, "Faktura", new Dictionary<string, object?>
                    {
                        ["FK_Firma_ID"]  = request.FirmaId,
                        ["FK_Numer"]     = numer,
                        ["FK_Data"]      = request.DataWystawienia,
                        ["FK_Kwota"]     = request.KwotaBrutto,
                        ["FK_Uwagi"]     = request.Uwagi,
                        ["FK_Cennik"]    = cennik,
                        ["FK_Saldo"]     = request.KwotaBrutto,
                        ["FK_Status"]    = FakturaStatus.Wystawiona.ToString(),
                        ["FK_PDF"]       = false,
                        ["FK_Num_Listy"] = request.IstniejacaListaBadanId
                    }, ct);
                    _logger.LogInformation("Utworzono fakturę Id={FakturaId}, Numer={Numer}", fakturaId, numer);
                    progress?.Report($"Krok 1/5 — utworzono fakturę nr {numer} (Id={fakturaId}).");
                }
            }
            else
            {
                progress?.Report("Krok 1/5 — brak numeru faktury, lista bez faktury.");
            }

            // ── Krok 2/5: lista badań ─────────────────────────────────────────────
            progress?.Report("Krok 2/5 — zapis listy badań...");
            int listaId;
            if (request.IstniejacaListaBadanId is int istniejacaListaId)
            {
                var istnieje = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
                    "SELECT COUNT(*) FROM ListyBadan WHERE Identyfikator = @Id",
                    new { Id = istniejacaListaId }, tx, cancellationToken: ct));
                if (istnieje == 0)
                    throw new InvalidOperationException($"Lista badań Id={istniejacaListaId} nie istnieje.");

                // Odepnij poprzedni skład listy — pozycje pozostające na liście zostaną powiązane ponownie w kroku 3
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE Badanie SET Bad_L_ID = NULL, Bad_F_ID = NULL, Bad_Fakt_ID = NULL, Bad_Fakt = NULL
                    WHERE Bad_L_ID = @Id",
                    new { Id = istniejacaListaId }, tx, cancellationToken: ct));

                // Faktura, która wskazywała na tę listę, a nie jest już z nią powiązana
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE Faktura SET FK_Num_Listy = NULL
                    WHERE FK_Num_Listy = @Id AND (@FakturaId IS NULL OR FK_ID <> @FakturaId)",
                    new { Id = istniejacaListaId, FakturaId = fakturaId }, tx, cancellationToken: ct));

                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE ListyBadan SET L_FK_ID = @FakturaId, L_Firma_ID = @FirmaId, L_Data = @Data, L_Uwagi = @Uwagi
                    WHERE Identyfikator = @Id",
                    new { Id = istniejacaListaId, FakturaId = fakturaId, FirmaId = request.FirmaId, Data = DateTime.Now, Uwagi = request.Uwagi },
                    tx, cancellationToken: ct));

                listaId = istniejacaListaId;
                progress?.Report($"Krok 2/5 — zaktualizowano listę badań (Id={listaId}).");
            }
            else
            {
                listaId = await InsertRowAsync(conn, tx, "ListyBadan", new Dictionary<string, object?>
                {
                    ["L_FK_ID"]    = fakturaId,
                    ["L_Firma_ID"] = request.FirmaId,
                    ["L_Data"]     = DateTime.Now,
                    ["L_Uwagi"]    = request.Uwagi
                }, ct);
                progress?.Report($"Krok 2/5 — utworzono listę badań (Id={listaId}).");
            }

            // ── Krok 3/5: pozycje ─────────────────────────────────────────────────
            if (robocze != null)
            {
                for (int i = 0; i < robocze.Count; i++)
                {
                    var r = robocze[i];
                    var p = r.Zrodlo;
                    progress?.Report($"Krok 3/5 — pozycja {i + 1}/{robocze.Count}: {p.PacjentDisplay}");

                    // Pacjent — legacy: szukaj po imieniu, nazwisku i firmie; brak → nowy pacjent
                    r.Bad_P_ID ??= await FindOrCreatePacjentAsync(conn, tx, p.P_imie!, p.P_nazwisko!, firma, ct);

                    // Skierowanie — istniejące badanie mogło mieć skierowanie powiązane tylko przez B_Badanie_ID
                    if (!r.Bad_S_ID.HasValue && r.Bad_ID.HasValue)
                    {
                        r.Bad_S_ID = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(
                            "SELECT B_ID FROM B_Skierowania WHERE B_Badanie_ID = @BadId ORDER BY B_ID DESC LIMIT 1",
                            new { BadId = r.Bad_ID }, tx, cancellationToken: ct));
                    }
                    if (!r.Bad_S_ID.HasValue)
                    {
                        r.Bad_S_ID = await InsertRowAsync(conn, tx, "B_Skierowania", new Dictionary<string, object?>
                        {
                            ["B_Pacjent_ID"]          = r.Bad_P_ID,
                            ["B_Firma_ID"]            = request.FirmaId,
                            ["B_Faktura_ID"]          = fakturaId,
                            ["B_TypBadania"]          = p.Bad_Typ,
                            ["B_DataSkierowania"]     = p.Bad_Data,
                            ["B_RegistrationDate"]    = DateTime.Now,
                            ["B_ksiazeczka"]          = (p.Bad_Cena4 ?? 0m) != 0m,
                            ["B_Zaswiadczenie"]       = (p.Bad_Cena7 ?? 0m) != 0m,
                            ["B_Urlop"]               = false,
                            ["B_Ankieta"]             = false,
                            ["B_czynnik_fizyczny"]    = false,
                            ["B_czynnik_pylowy"]      = false,
                            ["B_czynnik_chemiczny"]   = false,
                            ["B_czynnik_biologiczny"] = false,
                            ["B_czynnik_inny"]        = false,
                            ["B_Activ"]               = true
                        }, ct, jesliKolumnaIstnieje: new Dictionary<string, object?> { ["B_Nowe"] = true });
                    }

                    // Kolumny wspólne dla INSERT i UPDATE Badanie
                    var badanie = new Dictionary<string, object?>
                    {
                        ["Bad_L_ID"]    = listaId,
                        ["Bad_F_ID"]    = fakturaId,
                        ["Bad_Fakt_ID"] = fakturaId,
                        ["Bad_Fakt"]    = numer,
                        ["Bad_Typ"]     = p.Bad_Typ,
                        ["Bad_Data"]    = p.Bad_Data,
                        ["Bad_Data_Do"] = p.Bad_Data_Do,
                        ["Bad_Wynik"]   = p.Bad_Wynik,
                        ["Bad_Cena1"]   = p.Bad_Cena1, ["Bad_Cena2"] = p.Bad_Cena2, ["Bad_Cena3"] = p.Bad_Cena3,
                        ["Bad_Cena4"]   = p.Bad_Cena4, ["Bad_Cena5"] = p.Bad_Cena5, ["Bad_Cena6"] = p.Bad_Cena6,
                        ["Bad_Cena7"]   = p.Bad_Cena7, ["Bad_Cena8"] = p.Bad_Cena8, ["Bad_Cena9"] = p.Bad_Cena9,
                        ["Bad_Cena10"]  = p.Bad_Cena10,
                        ["Bad_Razem"]   = p.Bad_Razem,
                        ["Bad_Nr_KS"]   = p.Bad_Nr_KS
                    };

                    if (!r.Bad_ID.HasValue)
                    {
                        badanie["Bad_S_ID"]      = r.Bad_S_ID;
                        badanie["Bad_P_ID"]      = r.Bad_P_ID;
                        badanie["Bad_bn_cennik"] = cennik ?? p.Bad_bn_cennik;
                        badanie["Bad_END"]       = p.Bad_END ?? false;
                        r.Bad_ID = await InsertRowAsync(conn, tx, "Badanie", badanie, ct);
                    }
                    else
                    {
                        var wymagane = await GetKolumnyAsync(conn, tx, "Badanie", ct);
                        UzupelnijWymagane(wymagane, badanie, tylkoPodane: true);

                        var prm = new DynamicParameters(badanie);
                        prm.Add("Id", r.Bad_ID);
                        prm.Add("SId", r.Bad_S_ID);
                        prm.Add("PId", r.Bad_P_ID);
                        prm.Add("Cennik", cennik ?? p.Bad_bn_cennik);

                        var setPola = string.Join(",\n                                ", badanie.Keys.Select(k => $"`{k}` = @{k}"));
                        var zmienione = await conn.ExecuteAsync(new CommandDefinition($@"
                            UPDATE Badanie SET
                                Bad_S_ID      = COALESCE(@SId, Bad_S_ID),
                                Bad_P_ID      = COALESCE(@PId, Bad_P_ID),
                                Bad_bn_cennik = COALESCE(NULLIF(@Cennik, ''), Bad_bn_cennik),
                                {setPola}
                            WHERE Bad_ID = @Id",
                            prm, tx, cancellationToken: ct));
                        if (zmienione == 0)
                            throw new InvalidOperationException($"Badanie Id={r.Bad_ID} nie istnieje w bazie (pozycja Lp {p.Lp}).");
                    }

                    // Powiąż skierowanie z badaniem (tylko jeśli jeszcze niepowiązane)
                    await conn.ExecuteAsync(new CommandDefinition(
                        "UPDATE B_Skierowania SET B_Badanie_ID = @BadId WHERE B_ID = @SId AND B_Badanie_ID IS NULL",
                        new { BadId = r.Bad_ID, SId = r.Bad_S_ID }, tx, cancellationToken: ct));

                    robocze[i] = r;
                }

                // Legacy krok 6: jawne powiązanie wszystkich pozycji z listą i fakturą
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE Badanie SET Bad_L_ID = @LId, Bad_F_ID = @FakturaId, Bad_Fakt_ID = @FakturaId, Bad_Fakt = @Numer
                    WHERE Bad_ID IN @Ids",
                    new { LId = listaId, FakturaId = fakturaId, Numer = numer, Ids = robocze.Select(x => x.Bad_ID!.Value).ToList() },
                    tx, cancellationToken: ct));
            }
            else
            {
                progress?.Report($"Krok 3/5 — przypisywanie {badanieIds.Count} badań do listy...");
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE Badanie SET Bad_L_ID = @LId, Bad_F_ID = @FakturaId, Bad_Fakt_ID = @FakturaId, Bad_Fakt = @Numer
                    WHERE Bad_ID IN @Ids",
                    new { LId = listaId, FakturaId = fakturaId, Numer = numer, Ids = badanieIds },
                    tx, cancellationToken: ct));
            }

            // ── Krok 4/5: suma faktury ────────────────────────────────────────────
            if (fakturaId.HasValue)
            {
                progress?.Report("Krok 4/5 — aktualizacja sumy faktury...");
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE Faktura SET
                        FK_Suma_Bad  = (SELECT COALESCE(SUM(Bad_Razem), 0) FROM Badanie WHERE Bad_L_ID = @LId),
                        FK_Num_Listy = @LId
                    WHERE FK_ID = @FakturaId",
                    new { LId = listaId, FakturaId = fakturaId }, tx, cancellationToken: ct));
            }

            // ── Krok 5/5: commit ──────────────────────────────────────────────────
            progress?.Report("Krok 5/5 — zatwierdzanie zmian...");
            await tx.CommitAsync(ct);

            if (robocze != null)
            {
                foreach (var r in robocze)
                {
                    r.Zrodlo.Bad_ID   = r.Bad_ID;
                    r.Zrodlo.Bad_P_ID = r.Bad_P_ID;
                    r.Zrodlo.Bad_S_ID = r.Bad_S_ID;
                    r.Zrodlo.Bad_L_ID = listaId;
                    r.Zrodlo.Bad_F_ID = fakturaId;
                    if (cennik != null) r.Zrodlo.Bad_bn_cennik = cennik;
                }
            }

            _autoSync?.NotifyTablesChanged(new[] { "Badanie", "Faktura", "ListyBadan", "P_Pacjent", "B_Skierowania" });

            _logger.LogInformation("Zapis listy badań zakończony — ListaId={ListaId}, FakturaId={FakturaId}", listaId, fakturaId);
            return new SaveListaBadanResult(true, fakturaId, listaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd zapisu listy badań — rollback transakcji");
            progress?.Report($"Błąd: {ex.Message}");
            return new SaveListaBadanResult(false, null, 0, ex.Message);
        }
    }

    private static readonly ConcurrentDictionary<string, IReadOnlyList<KolumnaInfo>> _schematCache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// INSERT odporny na różnice schematu (baza produkcyjna z migracji Access ma kolumny NOT NULL bez DEFAULT).
    /// Kolumny wymagane, których nie podano (lub podano NULL), dostają wartość neutralną: 0 / '' / bieżąca data.
    /// Podane kolumny, których nie ma w tabeli, są pomijane. Zwraca LAST_INSERT_ID().
    /// </summary>
    private static async Task<int> InsertRowAsync(
        DbConnection conn, DbTransaction tx, string tabela, IDictionary<string, object?> wartosci,
        CancellationToken ct, IDictionary<string, object?>? jesliKolumnaIstnieje = null)
    {
        var kolumny = await GetKolumnyAsync(conn, tx, tabela, ct);
        var dane = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (kolumny.Count == 0)
        {
            foreach (var kv in wartosci) dane[kv.Key] = kv.Value;
        }
        else
        {
            var istniejace = kolumny.ToDictionary(k => k.Nazwa, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in wartosci)
                if (istniejace.TryGetValue(kv.Key, out var k)) dane[k.Nazwa] = kv.Value;
            if (jesliKolumnaIstnieje != null)
                foreach (var kv in jesliKolumnaIstnieje)
                    if (istniejace.TryGetValue(kv.Key, out var k) && !dane.ContainsKey(k.Nazwa)) dane[k.Nazwa] = kv.Value;

            UzupelnijWymagane(kolumny, dane, tylkoPodane: false);
        }

        var nazwy = dane.Keys.ToList();
        var prm = new DynamicParameters();
        for (int i = 0; i < nazwy.Count; i++) prm.Add($"p{i}", dane[nazwy[i]]);

        var sql = $"INSERT INTO `{tabela}` ({string.Join(", ", nazwy.Select(n => $"`{n}`"))}) " +
                  $"VALUES ({string.Join(", ", nazwy.Select((_, i) => $"@p{i}"))})";

        await conn.ExecuteAsync(new CommandDefinition(sql, prm, tx, cancellationToken: ct));
        var id = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT LAST_INSERT_ID()", transaction: tx, cancellationToken: ct));
        return checked((int)id);
    }

    /// <summary>
    /// Zastępuje NULL w kolumnach NOT NULL wartością neutralną. Gdy <paramref name="tylkoPodane"/> = false,
    /// dodaje też brakujące kolumny NOT NULL bez DEFAULT.
    /// </summary>
    private static void UzupelnijWymagane(IReadOnlyList<KolumnaInfo> kolumny, IDictionary<string, object?> dane, bool tylkoPodane)
    {
        foreach (var k in kolumny.Where(k => k.Wymagana))
        {
            var klucz = dane.Keys.FirstOrDefault(n => string.Equals(n, k.Nazwa, StringComparison.OrdinalIgnoreCase));
            if (klucz != null)
            {
                if (dane[klucz] is null && WartoscNeutralna(k.TypDanych) is { } v) dane[klucz] = v;
            }
            else if (!tylkoPodane && !k.MaDomyslna && WartoscNeutralna(k.TypDanych) is { } v)
            {
                dane[k.Nazwa] = v;
            }
        }
    }

    private static object? WartoscNeutralna(string typ) => typ.ToLowerInvariant() switch
    {
        "tinyint" or "smallint" or "mediumint" or "int" or "integer" or "bigint" or "bit" => 0,
        "decimal" or "numeric" or "float" or "double" or "real" => 0m,
        "char" or "varchar" or "tinytext" or "text" or "mediumtext" or "longtext" => string.Empty,
        "date" => DateTime.Today,
        "datetime" or "timestamp" => DateTime.Now,
        "time" => TimeSpan.Zero,
        "year" => DateTime.Today.Year,
        _ => null
    };

    private static async Task<IReadOnlyList<KolumnaInfo>> GetKolumnyAsync(DbConnection conn, DbTransaction tx, string tabela, CancellationToken ct)
    {
        var klucz = $"{conn.DataSource}|{conn.Database}|{tabela}";
        if (_schematCache.TryGetValue(klucz, out var cached)) return cached;

        var wiersze = await conn.QueryAsync<KolumnaInfo>(new CommandDefinition(@"
            SELECT COLUMN_NAME AS Nazwa,
                   DATA_TYPE   AS TypDanych,
                   (IS_NULLABLE = 'NO'
                    AND EXTRA NOT LIKE '%auto_increment%'
                    AND EXTRA NOT LIKE '%GENERATED%')        AS Wymagana,
                   (COLUMN_DEFAULT IS NOT NULL)               AS MaDomyslna
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@Tabela)
            ORDER BY ORDINAL_POSITION",
            new { Tabela = tabela }, tx, cancellationToken: ct));

        var lista = wiersze.ToList();
        if (lista.Count > 0) _schematCache[klucz] = lista;
        return lista;
    }

    private sealed class KolumnaInfo
    {
        public string Nazwa { get; set; } = string.Empty;
        public string TypDanych { get; set; } = string.Empty;
        public bool Wymagana { get; set; }
        public bool MaDomyslna { get; set; }
    }

    /// <summary>
    /// Legacy: SELECT P_ID FROM P_Pacjent WHERE TRIM(P_imie)=? AND TRIM(P_nazwisko)=? AND TRIM(P_firma)=?
    /// — rozszerzone o dopasowanie po P_Firma_id. Brak → INSERT nowego pacjenta przypisanego do firmy.
    /// </summary>
    private static async Task<int> FindOrCreatePacjentAsync(DbConnection conn, DbTransaction tx, string imie, string nazwisko, FirmaRow firma, CancellationToken ct)
    {
        imie = imie.Trim();
        nazwisko = nazwisko.Trim();

        var pid = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(@"
            SELECT P_ID FROM P_Pacjent
            WHERE TRIM(P_imie) = @Imie AND TRIM(P_nazwisko) = @Nazwisko
              AND (P_Firma_id = @FirmaId OR (@FirmaNazwa IS NOT NULL AND TRIM(P_firma) = @FirmaNazwa))
            ORDER BY (P_Firma_id = @FirmaId) DESC, P_ID DESC
            LIMIT 1",
            new { Imie = imie, Nazwisko = nazwisko, FirmaId = firma.Id, FirmaNazwa = firma.Nazwa?.Trim() }, tx, cancellationToken: ct));

        if (pid.HasValue) return pid.Value;

        return await InsertRowAsync(conn, tx, "P_Pacjent", new Dictionary<string, object?>
        {
            ["P_imie"]     = imie,
            ["P_nazwisko"] = nazwisko,
            ["P_firma"]    = firma.Nazwa?.Trim(),
            ["P_Firma_id"] = firma.Id
        }, ct, jesliKolumnaIstnieje: new Dictionary<string, object?>
        {
            ["P_activ"] = true,
            ["P_Nowy"]  = true,
            ["P_brak"]  = true
        });
    }

    private sealed class FirmaRow
    {
        public int Id { get; set; }
        public string? Nazwa { get; set; }
    }

    private sealed class FakturaRow
    {
        public int Id { get; set; }
        public int? FirmaId { get; set; }
    }
    public async Task UnassignBadaniaAsync(IEnumerable<int> badanieIds, CancellationToken ct = default)
    {
        var ids = badanieIds.ToList();
        _logger.LogInformation("Odpinanie {Count} badań od listy/faktury", ids.Count);
        await _unitOfWork.Badania.UnassignFromListaAsync(ids, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
