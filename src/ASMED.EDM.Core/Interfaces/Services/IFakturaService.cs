using ASMED.EDM.Core.DTOs;
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

    /// <summary>Dapper — pełny JOIN, gotowe BadanieRowDto do UI</summary>
    Task<IEnumerable<BadanieRowDto>> GetAvailableBadaniaRowsAsync(CancellationToken ct = default);

    // ── Listy badań ───────────────────────────────────────────────────────────

    /// <summary>Pobiera listę badań z pełnymi danymi</summary>
    Task<ListaBadan?> GetListaBadanAsync(int id, CancellationToken ct = default);

    // ── Zapis transakcyjny ────────────────────────────────────────────────────

    /// <summary>
    /// Zapisuje listę badań i tworzy/aktualizuje fakturę — jedna transakcja MySQL.
    /// Odpowiednik legacy: SaveListOfInvoiceAsync
    /// Kroki:
    ///   1. Znajdź (TRIM(FK_Numer)) lub utwórz Faktura
    ///   2. Utwórz lub zaktualizuj ListyBadan (przy modyfikacji odpina poprzednie badania)
    ///   3. Dla <see cref="SaveListaBadanRequest.Pozycje"/>: znajdź/utwórz pacjenta, utwórz skierowanie,
    ///      INSERT/UPDATE Badanie (wygenerowane Id są wpisywane z powrotem do DTO)
    ///   4. Ustaw Bad_L_ID, Bad_Fakt_ID, Bad_Fakt oraz FK_Suma_Bad / FK_Num_Listy
    /// </summary>
    Task<SaveListaBadanResult> SaveListaBadanAsync(
        SaveListaBadanRequest request,
        IProgress<string>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Odpina badania od listy i faktury — Bad_L_ID = null, Bad_Fakt_ID = null.
    /// Access: UPDATE Badanie SET Bad_L_ID = 0, Bad_F_ID = 0
    /// </summary>
    Task UnassignBadaniaAsync(IEnumerable<int> badanieIds, CancellationToken ct = default);
}

// ── DTOs żądania/odpowiedzi ────────────────────────────────────────────────────

/// <summary>
/// Dane żądania zapisu listy badań do faktury.
/// Gdy podano <paramref name="Pozycje"/>, zapisywane są pełne dane pozycji (także dodane ręcznie,
/// bez Bad_ID) a <paramref name="BadanieIds"/> jest ignorowane. Bez pozycji do listy przypisywane są
/// tylko istniejące badania z <paramref name="BadanieIds"/>.
/// </summary>
public record SaveListaBadanRequest(
    int FirmaId,
    string? NumerFaktury,
    DateTime DataWystawienia,
    decimal? KwotaBrutto,
    string? Uwagi,
    string? NazwaCennika,
    IEnumerable<int> BadanieIds,
    int? IstniejacaListaBadanId = null,
    IReadOnlyList<BadanieRowDto>? Pozycje = null
);

/// <summary>Wynik zapisu listy badań</summary>
public record SaveListaBadanResult(
    bool Success,
    int? FakturaId,
    int ListaBadanId,
    string? ErrorMessage = null
);
