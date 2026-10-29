using System;

namespace ASMED.EDM.UI.Models.Orzeczenie;

/// <summary>
/// Element listy w lewej kolumnie widoku OrzeczenieView.
/// Dane pochodz\u0105 z Rejestracja + B_Skierowania + P_Pacjent + Firma (jak w BadanieViewModel).
/// </summary>
public class OrzeczenieListItem
{
    // Rejestracja
    public int? R_ID { get; set; }
    public DateTime? R_Data { get; set; }
    public string? R_Status { get; set; }

    // Skierowanie
    public int? B_ID { get; set; }
    public DateTime? B_DataSkierowania { get; set; }
    public string? B_typ { get; set; }
    public string? B_Stanowisko { get; set; }

    // Flagi B_Skierowania (bistabilne przyciski)
    public bool B_Bad { get; set; }
    public bool B_laryngolog { get; set; }
    public bool B_okulista { get; set; }
    public bool B_ksiazeczka { get; set; }
    public bool B_Urlop { get; set; }

    // Pacjent
    public int? P_ID { get; set; }
    public string? P_imie { get; set; }
    public string? P_nazwisko { get; set; }
    public string? P_pesel { get; set; }

    // Firma
    public string? Firma_Nazwa { get; set; }
    public string? Firma_NIP { get; set; }

    public string? P_Miasto { get; set; }
    public string? P_Adres { get; set; }
    public string? P_KodPocztowy { get; set; }

    // Wla\u015Bciwo\u015Bci pomocnicze dla UI
    public string FormattedPesel => string.IsNullOrWhiteSpace(P_pesel) ? "---" : P_pesel;
    public string P_Firma => Firma_Nazwa ?? string.Empty;
    public string ImieNazwisko => $"{P_imie} {P_nazwisko}".Trim();
    public string Status => R_Status ?? string.Empty;
    public string StatusWizytyTekst => R_Status ?? string.Empty;
    public string? B_TypBadania => B_typ;
}
