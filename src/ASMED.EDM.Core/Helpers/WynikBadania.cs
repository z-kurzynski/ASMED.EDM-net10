namespace ASMED.EDM.Core.Helpers;

/// <summary>Pozycja słownika wyniku badania (symbol werdyktu orzeczenia EDM).</summary>
public sealed record WynikBadaniaOption(string Kod, string Opis)
{
    public string Nazwa => $"{Kod} - {Opis}";
}

/// <summary>
/// Wynik badania medycyny pracy zapisywany w Badanie.Bad_Wynik jako symbol
/// werdyktu orzeczenia przekazywany do EDM (MpWerdykt.Symbol).
/// </summary>
public static class WynikBadania
{
    public const string BrakPrzeciwwskazan = "21";
    public const string PrzeciwwskazaniaZdrowotne = "22";
    public const string UtrataZdolnosci = "23";

    public const string Domyslny = BrakPrzeciwwskazan;

    public static IReadOnlyList<WynikBadaniaOption> Opcje { get; } =
    [
        new(BrakPrzeciwwskazan, "brak przeciwwskazań"),
        new(PrzeciwwskazaniaZdrowotne, "przeciwwskazania zdrowotne"),
        new(UtrataZdolnosci, "utrata zdolności"),
    ];

    /// <summary>
    /// Zamienia wartość z bazy na symbol EDM. Obsługuje stare wartości
    /// ("pozytywne"/"negatywne", "1 - Pozytywne"/"2 - Negatywne").
    /// Pusta wartość daje symbol domyślny, nierozpoznana jest zwracana bez zmian.
    /// </summary>
    public static string Normalizuj(string? wartosc)
    {
        if (string.IsNullOrWhiteSpace(wartosc))
            return Domyslny;

        var v = wartosc.Trim();

        foreach (var opcja in Opcje)
        {
            if (v == opcja.Kod || v.StartsWith(opcja.Kod + " ", StringComparison.Ordinal))
                return opcja.Kod;
        }

        var lower = v.ToLowerInvariant();
        if (lower == "1" || lower.StartsWith("1 ") || lower.StartsWith("1-") || lower.Contains("pozytyw"))
            return BrakPrzeciwwskazan;
        if (lower == "2" || lower.StartsWith("2 ") || lower.StartsWith("2-") || lower.Contains("negatyw"))
            return PrzeciwwskazaniaZdrowotne;

        return v;
    }

    /// <summary>Tekst do wyświetlenia (np. "21 - brak przeciwwskazań"); pusta wartość daje pusty tekst.</summary>
    public static string Opis(string? wartosc)
    {
        if (string.IsNullOrWhiteSpace(wartosc))
            return string.Empty;

        var kod = Normalizuj(wartosc);
        return Opcje.FirstOrDefault(o => o.Kod == kod)?.Nazwa ?? kod;
    }
}
