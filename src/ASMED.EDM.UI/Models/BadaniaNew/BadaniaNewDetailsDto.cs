using System;

namespace ASMED.EDM.UI.Models.BadaniaNew;

/// <summary>
/// DTO dla szczegółów badania w widoku "Zakończ Badanie" (BadaniaNewView)
/// Zawiera pełne dane potrzebne do wypełnienia prawej kolumny po kliknięciu w listę
/// SQL: User-provided simplified query for BadaniaNewView (only B_Skierowania + P_Pacjent + Firma)
/// </summary>
public class BadaniaNewDetailsDto
{
    // B_Skierowania
    public int B_ID { get; set; }
    public int B_Badanie_ID { get; set; }
    public DateTime? B_DataSkierowania { get; set; }
    public string? B_TypBadania { get; set; }
    public string? B_Stanowisko { get; set; }
    public bool? B_Zaswiadczenie { get; set; }
    public bool? B_ksiazeczka { get; set; }
    public bool? B_wywiad { get; set; }
    public bool? B_orzeczenie { get; set; }
    public int? B_orzeczenie_ID { get; set; }
    public bool? B_Activ { get; set; }
    public string? B_uwagi { get; set; }
    public int? B_edm_document_id { get; set; }
    public bool? B_ankieta   { get; set; }

    // backward-compatible PascalCase alias used by some XAML bindings
    public bool? B_Ankieta
    {
        get => B_ankieta;
        set => B_ankieta = value;
    }

    // P_Pacjent
    public int P_ID { get; set; }
    public string? P_pesel { get; set; }
    public string? P_imie { get; set; }
    public string? P_nazwisko { get; set; }

    // Firma
    public int Firma_id { get; set; }
    public string? Firma_Nazwa { get; set; }
    public string? Firma_Ulica { get; set; }
    public string? Firma_Kod { get; set; }
    public string? Firma_Miejscowosc { get; set; }
    public string? Firma_NIP { get; set; }
    public string? Firma_Cennik { get; set; } = null;

    // Computed properties dla UI
    public string PacjentDisplay => $"{P_imie} {P_nazwisko}";
    public string FirmaDisplay => Firma_Nazwa ?? "Brak";
    public string AdresDisplay => 
        $"{Firma_Ulica ?? ""}\n{Firma_Kod ?? ""} {Firma_Miejscowosc ?? ""}".Trim();
    public string DataBadaniaDisplay => B_DataSkierowania?.ToString("dd.MM.yyyy") ?? "---";
}
