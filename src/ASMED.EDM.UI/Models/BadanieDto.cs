using System;

namespace ASMED.EDM.UI.Models;

/// <summary>
/// DTO dla badania z danymi skierowania, pacjenta i firmy
/// </summary>
public class BadanieDto
{
    // Badanie
    public int Bad_ID { get; set; }
    public DateTime? Bad_Data { get; set; }
    public DateTime? Bad_Data_Do { get; set; }
    public string? Bad_Wynik { get; set; }
    public string? Bad_Nr_KS { get; set; }
    public string? Bad_bn_cennik { get; set; }
    public string? Bad_Fakt { get; set; }
    public decimal? Bad_Razem { get; set; }
    public decimal? Bad_Cena1 { get; set; }
    public decimal? Bad_Cena2 { get; set; }
    public decimal? Bad_Cena3 { get; set; }
    public decimal? Bad_Cena4 { get; set; }
    public decimal? Bad_Cena5 { get; set; }
    public decimal? Bad_Cena6 { get; set; }
    public decimal? Bad_Cena7 { get; set; }
    public decimal? Bad_Cena8 { get; set; }

    // Skierowanie
    public int B_ID { get; set; }
    public DateTime? B_DataSkierowania { get; set; }
    public string? B_TypBadania { get; set; }
    public string? B_Stanowisko { get; set; }
    public bool? B_ksiazeczka { get; set; }
    public bool? B_Zaswiadczenie { get; set; }

    // Pacjent
    public int P_ID { get; set; }
    public string? P_imie { get; set; }
    public string? P_nazwisko { get; set; }
    public string? P_pesel { get; set; }
    public string? P_zawod { get; set; }

    // Firma
    public int Firma_id { get; set; }
    public string? Firma_Nazwa { get; set; }
    public string? Firma_NIP { get; set; }
    public string? Firma_Cennik { get; set; }

    // Computed properties for display
    public string PacjentDisplay => $"{P_nazwisko} {P_imie}";
    public string DataBadaniaDisplay => Bad_Data?.ToString("yyyy-MM-dd") ?? "Brak";
    public string CenaDisplay => Bad_Razem?.ToString("N2") + " PLN" ?? "0,00 PLN";
    public string FormattedPesel => P_pesel ?? "---";
}
