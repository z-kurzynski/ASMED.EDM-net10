namespace ASMED.EDM.UI.Models.Orzeczenie;

/// <summary>
/// DTO dla pierwszej części widoku OrzeczenieView.
/// Zawiera dane pacjenta, dane firmy oraz stany przycisków bistabilnych
/// (Badanie / Laryngolog / Okulista / Sanitarne / Urlop).
///
/// Uwaga: pola bool przycisków są przechowywane jako bit (0/1) w bazie danych.
/// Konwersja następuje na warstwie dostępu do danych.
/// </summary>
public class OrzeczenieStatusDto
{
    // ── Identyfikatory ────────────────────────────────────────────────────────

    /// <summary>Id karty badania (B_Skierowania).</summary>
    public int B_ID { get; set; }

    /// <summary>Id pacjenta (P_Pacjent).</summary>
    public int P_ID { get; set; }

    /// <summary>Id firmy (Firma).</summary>
    public int Firma_ID { get; set; }

    // ── Dane pacjenta ─────────────────────────────────────────────────────────

    /// <summary>Imię pacjenta.</summary>
    public string? P_Imie { get; set; }

    /// <summary>Nazwisko pacjenta.</summary>
    public string? P_Nazwisko { get; set; }

    /// <summary>PESEL pacjenta (11 znaków).</summary>
    public string? P_Pesel { get; set; }

    /// <summary>Data urodzenia pacjenta.</summary>
    public DateTime? P_DataUrodzenia { get; set; }

    /// <summary>Stanowisko pracy pacjenta (z karty skierowania).</summary>
    public string? P_Stanowisko { get; set; }

    /// <summary>Adres zamieszkania pacjenta (ulica, kod, miejscowość — jeden ciąg).</summary>
    public string? P_Adres { get; set; }

    // ── Dane firmy ────────────────────────────────────────────────────────────

    /// <summary>Pełna nazwa firmy.</summary>
    public string? Firma_Nazwa { get; set; }

    /// <summary>NIP firmy (10 cyfr).</summary>
    public string? Firma_NIP { get; set; }

    /// <summary>REGON firmy.</summary>
    public string? Firma_REGON { get; set; }

    /// <summary>Adres firmy (ulica, kod, miejscowość — jeden ciąg).</summary>
    public string? Firma_Adres { get; set; }

    // ── Stan przycisków bistabilnych (bool → 0/1 w bazie) ────────────────────

    /// <summary>
    /// Czy zakładka "Badanie" jest aktywna.
    /// W bazie przechowywane jako BIT: 0 = false, 1 = true.
    /// </summary>
    public bool Btn_Badanie { get; set; } = true;

    /// <summary>
    /// Czy zakładka "Laryngolog" jest aktywna.
    /// W bazie przechowywane jako BIT: 0 = false, 1 = true.
    /// </summary>
    public bool Btn_Laryngolog { get; set; } = false;

    /// <summary>
    /// Czy zakładka "Okulista" jest aktywna.
    /// W bazie przechowywane jako BIT: 0 = false, 1 = true.
    /// </summary>
    public bool Btn_Okulista { get; set; } = false;

    /// <summary>
    /// Czy zakładka "Sanitarne" jest aktywna.
    /// W bazie przechowywane jako BIT: 0 = false, 1 = true.
    /// </summary>
    public bool Btn_Sanitarne { get; set; } = false;

    /// <summary>
    /// Czy zakładka "Urlop" jest aktywna.
    /// W bazie przechowywane jako BIT: 0 = false, 1 = true.
    /// </summary>
    public bool Btn_Urlop { get; set; } = false;

    // ── Pomocnicze właściwości (konwersja bool → int dla warstwy danych) ──────

    /// <summary>Btn_Badanie jako 0/1 (do zapisu do bazy).</summary>
    public int Btn_Badanie_Int    => Btn_Badanie    ? 1 : 0;

    /// <summary>Btn_Laryngolog jako 0/1 (do zapisu do bazy).</summary>
    public int Btn_Laryngolog_Int => Btn_Laryngolog ? 1 : 0;

    /// <summary>Btn_Okulista jako 0/1 (do zapisu do bazy).</summary>
    public int Btn_Okulista_Int   => Btn_Okulista   ? 1 : 0;

    /// <summary>Btn_Sanitarne jako 0/1 (do zapisu do bazy).</summary>
    public int Btn_Sanitarne_Int  => Btn_Sanitarne  ? 1 : 0;

    /// <summary>Btn_Urlop jako 0/1 (do zapisu do bazy).</summary>
    public int Btn_Urlop_Int      => Btn_Urlop      ? 1 : 0;

    // ── Pomocnicze właściwości UI ─────────────────────────────────────────────

    /// <summary>Imię i nazwisko razem (do wyświetlenia w nagłówku).</summary>
    public string PelneNazwisko => $"{P_Imie} {P_Nazwisko}".Trim();
}
