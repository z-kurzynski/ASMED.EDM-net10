using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.Data.Services.Domain;

/// <summary>
/// Serwis fakturowania — transakcyjny zapis list badań i faktur.
/// Migracja z Access SQL (AccessDbContext.AddFaktura / SaveListOfInvoiceAsync).
/// </summary>
public class FakturaService : IFakturaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FakturaService> _logger;

    public FakturaService(IUnitOfWork unitOfWork, ILogger<FakturaService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Firmy ──────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<Firma>> GetActiveFirmyAsync(string? searchQuery = null, CancellationToken ct = default)
    {
        return await _unitOfWork.Firmy.GetActiveFirmyAsync(searchQuery, ct);
    }

    // ── Cenniki ────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<string>> GetActiveCennikNazwyAsync(CancellationToken ct = default)
    {
        var cenniki = await _unitOfWork.Cenniki.GetActiveCennikiAsync(ct);
        return cenniki.Select(c => c.Nazwa);
    }

    public async Task<Cennik?> GetCennikByNazwaAsync(string nazwa, CancellationToken ct = default)
    {
        return await _unitOfWork.Cenniki.GetByNazwaAsync(nazwa, ct);
    }

    // ── Badania ────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<Badanie>> GetAvailableBadaniaAsync(int? firmaId = null, CancellationToken ct = default)
    {
        return await _unitOfWork.Badania.GetAvailableBadaniaAsync(firmaId, ct);
    }

    // ── Listy badań ───────────────────────────────────────────────────────────

    public async Task<ListaBadan?> GetListaBadanAsync(int id, CancellationToken ct = default)
    {
        return await _unitOfWork.ListyBadan.GetWithBadaniaAsync(id, ct);
    }

    // ── Zapis transakcyjny ────────────────────────────────────────────────────

    /// <summary>
    /// Transakcyjny zapis listy badań + faktury.
    /// Odpowiednik: SaveListOfInvoiceAsync z legacy ListaFaktAddViewModel.
    ///
    /// Algorytm (zgodny z legacy Access SQL):
    ///  1. Walidacja danych wejściowych
    ///  2. Znajdź lub utwórz Faktura (po NumerFaktury — jak w Access: SELECT FK_ID WHERE TRIM(FK_Numer) = ?)
    ///  3. Utwórz lub zaktualizuj ListaBadan (powiąż z firmą i fakturą)
    ///  4. Zaktualizuj Bad_L_ID i Bad_F_ID dla badań (AssignToLista + SetFakturaId)
    ///  5. Zapisz — jeśli błąd → rollback
    /// </summary>
    public async Task<SaveListaBadanResult> SaveListaBadanAsync(SaveListaBadanRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Zapis listy badań — FirmaId={FirmaId}, NumerFaktury={Numer}, Badania={Count}",
            request.FirmaId, request.NumerFaktury, request.BadanieIds.Count());

        // ── Walidacja ──────────────────────────────────────────────────────────
        if (!request.BadanieIds.Any())
        {
            return new SaveListaBadanResult(false, null, 0, "Brak pozycji na liście — nic do zapisania.");
        }

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // ── 1. Utwórz lub znajdź Faktura ───────────────────────────────────
            int? fakturaId = null;

            if (!string.IsNullOrWhiteSpace(request.NumerFaktury))
            {
                var existingFaktura = await _unitOfWork.Faktury.GetByNumerAsync(request.NumerFaktury, ct);

                if (existingFaktura != null)
                {
                    fakturaId = existingFaktura.Id;
                    _logger.LogDebug("Znaleziono istniejącą fakturę: Id={FakturaId}, Numer={Numer}", fakturaId, request.NumerFaktury);
                }
                else
                {
                    var nowaFaktura = new Faktura
                    {
                        FirmaId = request.FirmaId,
                        NumerFaktury = request.NumerFaktury.Trim(),
                        DataWystawienia = request.DataWystawienia,
                        KwotaBrutto = request.KwotaBrutto,
                        Uwagi = request.Uwagi,
                        NazwaCennika = request.NazwaCennika,
                        Status = FakturaStatus.Wystawiona,
                        SumaBadan = request.KwotaBrutto,
                        Saldo = request.KwotaBrutto
                    };

                    await _unitOfWork.Faktury.AddAsync(nowaFaktura, ct);
                    await _unitOfWork.SaveChangesAsync(ct);

                    fakturaId = nowaFaktura.Id;
                    _logger.LogInformation("Utworzono nową fakturę: Id={FakturaId}, Numer={Numer}", fakturaId, request.NumerFaktury);
                }
            }

            // ── 2. Utwórz lub zaktualizuj ListaBadan ───────────────────────────
            ListaBadan listaBadan;

            if (request.IstniejacaListaBadanId.HasValue)
            {
                var existing = await _unitOfWork.ListyBadan.GetByIdAsync(request.IstniejacaListaBadanId.Value, ct);
                if (existing == null)
                    throw new InvalidOperationException($"ListaBadan Id={request.IstniejacaListaBadanId} nie istnieje.");

                // Odepnij stare badania przed przypisaniem nowych
                var stareBadania = await _unitOfWork.Badania.GetByListaBadanAsync(existing.Id, ct);
                var stareBadanieIds = stareBadania.Select(b => b.Id).ToList();
                if (stareBadanieIds.Any())
                    await _unitOfWork.Badania.UnassignFromListaAsync(stareBadanieIds, ct);

                existing.FirmaId = request.FirmaId;
                existing.FakturaId = fakturaId;
                existing.SumaRazem = request.KwotaBrutto;
                existing.Uwagi = request.Uwagi;
                existing.ModifiedAt = DateTime.UtcNow;

                _unitOfWork.ListyBadan.Update(existing);
                listaBadan = existing;
                _logger.LogDebug("Zaktualizowano ListaBadan Id={Id}", existing.Id);
            }
            else
            {
                listaBadan = new ListaBadan
                {
                    FirmaId = request.FirmaId,
                    FakturaId = fakturaId,
                    DataUtworzenia = request.DataWystawienia,
                    SumaRazem = request.KwotaBrutto,
                    Uwagi = request.Uwagi
                };

                await _unitOfWork.ListyBadan.AddAsync(listaBadan, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("Utworzono nową ListaBadan Id={Id}", listaBadan.Id);
            }

            // ── 3. Przypisz badania do listy (Bad_L_ID) ────────────────────────
            var badanieIds = request.BadanieIds.ToList();
            await _unitOfWork.Badania.AssignToListaAsync(badanieIds, listaBadan.Id, ct);

            // ── 4. Ustaw Bad_F_ID jeśli faktura istnieje ───────────────────────
            if (fakturaId.HasValue)
                await _unitOfWork.Badania.SetFakturaIdAsync(listaBadan.Id, fakturaId.Value, ct);

            // ── 5. Zapisz i zatwierdź transakcję ──────────────────────────────
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation("Zapis listy badań zakończony sukcesem — ListaBadanId={Id}, FakturaId={FId}",
                listaBadan.Id, fakturaId);

            return new SaveListaBadanResult(true, fakturaId, listaBadan.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd zapisu listy badań — rollback transakcji");
            await _unitOfWork.RollbackTransactionAsync(ct);
            return new SaveListaBadanResult(false, null, 0, ex.Message);
        }
    }

    public async Task UnassignBadaniaAsync(IEnumerable<int> badanieIds, CancellationToken ct = default)
    {
        var ids = badanieIds.ToList();
        _logger.LogInformation("Odpinanie {Count} badań od listy/faktury", ids.Count);
        await _unitOfWork.Badania.UnassignFromListaAsync(ids, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
