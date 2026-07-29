using System;

namespace ASMED.EDM.UI.Models;

/// <summary>
/// DTO dla szczegółowych danych badania (używane w widokach BadaniaNew/Edit)
/// </summary>
public class BadanieDetailsDto
{
    // B_Skierowania
    public int B_ID { get; set; }
    public DateTime? B_DataSkierowania { get; set; }
    public string? B_TypBadania { get; set; }
    public string? B_Stanowisko { get; set; }
    public string? B_Zaswiadczenie { get; set; }
    public string? B_ksiazeczka { get; set; }

    // P_Pacjent
    public int P_ID { get; set; }
    public string? P_pesel { get; set; }
    public string? P_imie { get; set; }
    public string? P_nazwisko { get; set; }
    public string? P_zawod { get; set; }

    // Firma
    public int Firma_id { get; set; }
    public string? Firma_Cennik { get; set; }
    public string? Firma_Nazwa { get; set; }
    public string? Firma_Ulica { get; set; }
    public string? Firma_Kod { get; set; }
    public string? Firma_Miejscowosc { get; set; }
    public string? Firma_NIP { get; set; }

    // Computed display properties
    public string PacjentDisplay => $"{P_nazwisko} {P_imie}";
    public string FirmaDisplay => $"{Firma_Nazwa} ({Firma_NIP})";
    public string AdresDisplay => $"{Firma_Ulica}, {Firma_Kod} {Firma_Miejscowosc}";
}
