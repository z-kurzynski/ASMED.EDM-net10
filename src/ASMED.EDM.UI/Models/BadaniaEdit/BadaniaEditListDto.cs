using System;

namespace ASMED.EDM.UI.Models.BadaniaEdit;

/// <summary>
/// DTO dla listy badań w widoku "Edycja Badań" (BadaniaEditNewView)
/// Zawiera dane potrzebne do wyświetlenia listy zamkniętych badań
/// </summary>
public class BadaniaEditListDto
{
    // Skierowanie
    public int B_ID { get; set; }
    public DateTime? B_DataSkierowania { get; set; }
    public string? B_TypBadania { get; set; }
    public string? B_Stanowisko { get; set; }

    // Pacjent
    public string? P_imie { get; set; }
    public string? P_nazwisko { get; set; }
    public string? P_pesel { get; set; }

    // Firma
    public string? Firma_Nazwa { get; set; }
    public string? Firma_NIP { get; set; }

    // Badanie (dla edycji ważne są wykonane badania z Bad_Data)
    public DateTime? Bad_Data { get; set; }
    public string? Bad_Wynik { get; set; }
    public string? Bad_Fakt { get; set; }

    // Computed properties
    public string PacjentDisplay => $"{P_nazwisko} {P_imie}";
    public string DataBadaniaDisplay => Bad_Data?.ToString("yyyy-MM-dd") ?? "Brak";
    public string FormattedPesel => P_pesel ?? "---";
}
