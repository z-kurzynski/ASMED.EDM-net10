using System;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Pomocnik do normalizacji tekstÛw (usuwanie polskich znakÛw diakrytycznych)
    /// </summary>
    public static class TextNormalizationHelper
    {
        /// <summary>
        /// Usuwa polskie znaki diakrytyczne i zastÍpuje je odpowiednikami ASCII.
        /// Uøywane do wyszukiwania i kodÛw kreskowych.
        /// </summary>
        /// <param name="text">Tekst do znormalizowania</param>
        /// <returns>Tekst bez polskich znakÛw (π?a, Ê?c, Í?e, ≥?l, Ò?n, Û?o, ú?s, ü?z, ø?z)</returns>
        public static string RemovePolishDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalized = text
                .Replace("•", "A").Replace("π", "a")
                .Replace("∆", "C").Replace("Ê", "c")
                .Replace(" ", "E").Replace("Í", "e")
                .Replace("£", "L").Replace("≥", "l")
                .Replace("—", "N").Replace("Ò", "n")
                .Replace("”", "O").Replace("Û", "o")
                .Replace("å", "S").Replace("ú", "s")
                .Replace("è", "Z").Replace("ü", "z")
                .Replace("Ø", "Z").Replace("ø", "z");

            return normalized;
        }

        /// <summary>
        /// Sprawdza czy tekst zawiera szukany fragment (ignoruje polskie znaki).
        /// Uøywane do filtrowania/wyszukiwania nazwisk z polskimi znakami.
        /// </summary>
        /// <param name="text">Tekst do przeszukania</param>
        /// <param name="searchTerm">Szukany fragment</param>
        /// <param name="ignoreCase">Czy ignorowaÊ wielkoúÊ liter (domyúlnie: true)</param>
        /// <returns>True jeúli zawiera (po normalizacji)</returns>
        public static bool ContainsIgnoringDiacritics(string text, string searchTerm, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerm))
                return false;

            var normalizedText = RemovePolishDiacritics(text);
            var normalizedSearch = RemovePolishDiacritics(searchTerm);

            if (ignoreCase)
            {
                normalizedText = normalizedText.ToLower();
                normalizedSearch = normalizedSearch.ToLower();
            }

            return normalizedText.Contains(normalizedSearch);
        }
    }
}
