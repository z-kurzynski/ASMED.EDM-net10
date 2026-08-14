using System;

namespace ASMED.EDM.UI.Models.Badanie;

/// <summary>
/// Element listy w lewej kolumnie widoku BadanieView.
/// Dane pochodzą z Rejestracja + B_Skierowania + P_Pacjent + Firma (jak w VisitsViewModel).
/// </summary>
public class BadanieListItem
{
    internal string? P_Ulica;

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
    public bool B_EKG { get; set; }
    public bool B_Lipidogram { get; set; }
    public bool B_ksiazeczka { get; set; }
    public bool B_Urlop { get; set; }
    public bool B_okulista { get; set; }
    public bool B_Ankieta { get; set; }

    // Pacjent
    public int? P_ID { get; set; }
    public string? P_imie { get; set; }
    public string? P_nazwisko { get; set; }
    public string? P_pesel { get; set; }

    // Firma
    public string? Firma_Nazwa { get; set; }
    public string? Firma_NIP { get; set; }

    // Właściwości pomocnicze dla UI
    public string FormattedPesel => string.IsNullOrWhiteSpace(P_pesel) ? "---" : P_pesel;
    public string P_Firma => Firma_Nazwa ?? string.Empty;
    public string ImieNazwisko => $"{P_imie} {P_nazwisko}".Trim();
    public string Status => R_Status ?? string.Empty;
    public string StatusWizytyTekst => R_Status ?? string.Empty;
    public string? B_TypBadania => B_typ;

    public string? P_Miasto { get; internal set; }
    public string? P_NrDomu { get; internal set; }
    public string? P_NrMieszkania { get; internal set; }
    public string? P_KodPocztowy { get; internal set; }
    public string? P_Adres { get; internal set; }
}
