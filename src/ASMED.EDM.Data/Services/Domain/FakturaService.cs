using ASMED.EDM.Core.DTOs;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
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

    public FakturaService(IUnitOfWork unitOfWork, ILogger<FakturaService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    /// Transakcyjny zapis listy badaĹ„ + faktury.
    /// Odpowiednik: SaveListOfInvoiceAsync z legacy ListaFaktAddViewModel.
    ///
    /// Algorytm (zgodny z legacy Access SQL):
    ///  1. Walidacja danych wejĹ›ciowych
    ///  2. ZnajdĹş lub utwĂłrz Faktura (po NumerFaktury â€” jak w Access: SELECT FK_ID WHERE TRIM(FK_Numer) = ?)
    ///  3. UtwĂłrz lub zaktualizuj ListaBadan (powiÄ…ĹĽ z firmÄ… i fakturÄ…)
    ///  4. Zaktualizuj Bad_L_ID i Bad_F_ID dla badaĹ„ (AssignToLista + SetFakturaId)
    ///  5. Zapisz â€” jeĹ›li bĹ‚Ä…d â†’ rollback
    /// </summary>
    public async Task<SaveListaBadanResult> SaveListaBadanAsync(SaveListaBadanRequest request, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Zapis listy badaĹ„ â€” FirmaId={FirmaId}, NumerFaktury={Numer}, Badania={Count}",
            request.FirmaId, request.NumerFaktury, request.BadanieIds.Count());

        // â”€â”€ Walidacja â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (!request.BadanieIds.Any())
        {
            return new SaveListaBadanResult(false, null, 0, "Brak pozycji na liĹ›cie â€” nic do zapisania.");
        }

        SaveListaBadanResult? result = null;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // krok 1/4 weryfikacja faktury

                progress?.Report("Krok 1/4  weryfikacja faktury...");
                int? fakturaId = null;

                if (!string.IsNullOrWhiteSpace(request.NumerFaktury))
                {
                    var existingFaktura = await _unitOfWork.Faktury.GetByNumerAsync(request.NumerFaktury, ct);

                    if (existingFaktura != null)
                    {
                        fakturaId = existingFaktura.Id;
                        _logger.LogDebug("Znaleziono istniejÄ…cÄ… fakturÄ™: Id={FakturaId}, Numer={Numer}", fakturaId, request.NumerFaktury);
                        progress?.Report($"Krok 1/4 â€” znaleziono fakturÄ™ nr {request.NumerFaktury} (Id={fakturaId}).");
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
                            Status = FakturaStatus.Wystawiona.ToString(),
                            SumaBadan = request.KwotaBrutto,
                            Saldo = request.KwotaBrutto
                        };

                     //   await _unitOfWork.Faktury.AddAsync(nowaFaktura, ct);
                     //   await _unitOfWork.SaveChangesAsync(ct);

                        fakturaId = nowaFaktura.Id;
                        _logger.LogInformation("Utworzono nową fakturę: Id={FakturaId}, Numer={Numer}", fakturaId, request.NumerFaktury);
                        progress?.Report($"Krok 1/4 — utworzono nową fakturę nr {request.NumerFaktury} (Id={fakturaId}).");
                    }
                }
                else
                {
                    progress?.Report("Krok 1/4 â€” brak numeru faktury, pominiÄ™to.");
                }

                // krok 2/4 zapis listy badań (ListaBadan)

                progress?.Report("Krok 2/4 — zapis listy badań...");
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
                    progress?.Report($"Krok 2/4 â€” zaktualizowano listÄ™ badaĹ„ (Id={existing.Id}).");
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
                    _logger.LogInformation("Utworzono nowÄ… ListaBadan Id={Id}", listaBadan.Id);
                    progress?.Report($"Krok 2/4 â€” utworzono nowÄ… listÄ™ badaĹ„ (Id={listaBadan.Id}).");
                }

                // krok 3/4 przypisywanie badań do listy

                progress?.Report($"Krok 3/4 — przypisywanie {request.BadanieIds.Count()} badań do listy...");
                var badanieIds = request.BadanieIds.ToList();
                await _unitOfWork.Badania.AssignToListaAsync(badanieIds, listaBadan.Id, ct);
                progress?.Report($"Krok 3/4 — przypisano {badanieIds.Count} badań do listy (Id={listaBadan.Id}).");

                // krok 4/4 ustawienie Bad_F_ID jeśli faktura istnieje
                if (fakturaId.HasValue)
                {
                    progress?.Report("Krok 4/4 — aktualizacja powiązania z fakturą...");
                    await _unitOfWork.Badania.SetFakturaIdAsync(listaBadan.Id, fakturaId.Value, ct);
                    progress?.Report($"Krok 4/4 — powiązano badania z fakturą (Id={fakturaId}).");
                }
                else
                {
                    progress?.Report("Krok 4/4 — brak faktury, pominięto powiązanie.");
                }

                _logger.LogInformation("Zapis listy badań zakończony sukcesem — ListaBadanId={Id}, FakturaId={FId}",
                    listaBadan.Id, fakturaId);

                result = new SaveListaBadanResult(true, fakturaId, listaBadan.Id);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd zapisu listy badań — rollback transakcji");
            progress?.Report($"Błąd: {ex.Message}");
            result = new SaveListaBadanResult(false, null, 0, ex.Message);
        }

        return result ?? new SaveListaBadanResult(false, null, 0, "Nieznany błąd.");
    }
    public async Task UnassignBadaniaAsync(IEnumerable<int> badanieIds, CancellationToken ct = default)
    {
        var ids = badanieIds.ToList();
        _logger.LogInformation("Odpinanie {Count} badań od listy/faktury", ids.Count);
        await _unitOfWork.Badania.UnassignFromListaAsync(ids, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
