using ASMED.EDM.Core.Entities;

namespace ASMED.EDM.Core.Interfaces.Services;

/// <summary>
/// Serwis zarządzania fakturami i listami badań do fakturowania
/// </summary>
public interface IFakturaService
{
    // ── Firmy ──────────────────────────────────────────────────────────────────

    /// <summary>Pobiera aktywne firmy, opcjonalnie filtrowane</summary>
    Task<IEnumerable<Firma>> GetActiveFirmyAsync(string? searchQuery = null, CancellationToken ct = default);

    // ── Cenniki ────────────────────────────────────────────────────────────────

    /// <summary>Pobiera aktywne nazwy cenników — Access: BAD_Cennik WHERE b_activ = TRUE</summary>
    Task<IEnumerable<string>> GetActiveCennikNazwyAsync(CancellationToken ct = default);

    /// <summary>Pobiera cennik (z cenami) po nazwie</summary>
    Task<Cennik?> GetCennikByNazwaAsync(string nazwa, CancellationToken ct = default);

    // ── Badania ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pobiera badania dostępne do fakturowania (niezapisane na liście).
    /// Access: Bad_L_ID IS NULL OR Bad_L_ID = 0
    /// </summary>
    Task<IEnumerable<Badanie>> GetAvailableBadaniaAsync(int? firmaId = null, CancellationToken ct = default);

    // ── Listy badań ───────────────────────────────────────────────────────────

    /// <summary>Pobiera listę badań z pełnymi danymi</summary>
    Task<ListaBadan?> GetListaBadanAsync(int id, CancellationToken ct = default);

    // ── Zapis transakcyjny ────────────────────────────────────────────────────

    /// <summary>
    /// Zapisuje listę badań i tworzy/aktualizuje fakturę — transakcja EF Core.
    /// Odpowiednik legacy: SaveListOfInvoiceAsync
    /// Kroki:
    ///   1. Utwórz lub znajdź Faktura po NumerFaktury
    ///   2. Utwórz lub zaktualizuj ListaBadan (z FirmaId, SumaRazem)
    ///   3. Zaktualizuj Bad_L_ID i Bad_F_ID dla każdego badania na liście
    /// </summary>
    Task<SaveListaBadanResult> SaveListaBadanAsync(
        SaveListaBadanRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Odpina badania od listy i faktury — Bad_L_ID = null, Bad_F_ID = null.
    /// Access: UPDATE Badanie SET Bad_L_ID = 0, Bad_F_ID = 0
    /// </summary>
    Task UnassignBadaniaAsync(IEnumerable<int> badanieIds, CancellationToken ct = default);
}

// ── DTOs żądania/odpowiedzi ────────────────────────────────────────────────────

/// <summary>Dane żądania zapisu listy badań do faktury</summary>
public record SaveListaBadanRequest(
    int FirmaId,
    string? NumerFaktury,
    DateTime DataWystawienia,
    decimal? KwotaBrutto,
    string? Uwagi,
    string? NazwaCennika,
    IEnumerable<int> BadanieIds,
    int? IstniejacaListaBadanId = null
);

/// <summary>Wynik zapisu listy badań</summary>
public record SaveListaBadanResult(
    bool Success,
    int? FakturaId,
    int ListaBadanId,
    string? ErrorMessage = null
);
