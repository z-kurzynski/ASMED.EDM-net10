using System;

namespace ASMED.EDM.UI.Models.BadaniaEdit;

/// <summary>
/// DTO dla szczegółów badania w widoku "Edycja Badań" (BadaniaEditNewView)
/// Zawiera pełne dane potrzebne do wypełnienia prawej kolumny po kliknięciu w listę
/// SQL: User-provided MySQL join query for BadaniaEditNewView (różni się joinem od BadaniaNewView)
/// </summary>
public class BadaniaEditDetailsDto
{
    // B_Skierowania
    public int B_ID { get; set; }
    public DateTime? B_DataSkierowania { get; set; }
    public string? B_TypBadania { get; set; }
    public string? B_Stanowisko { get; set; }
    public bool? B_Zaswiadczenie { get; set; }
    public bool? B_ksiazeczka { get; set; }
    public bool? B_wywiad { get; set; }
    public bool? B_orzeczenie { get; set; }
    public int? B_orzeczenie_ID { get; set; }
    public int? B_edm_document_id { get; set; }
    public bool? B_ankieta { get; set; }

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
    public string? P_zawod { get; set; }

    // Firma
    public int Firma_id { get; set; }
    public string? Firma_Nazwa { get; set; }
    public string? Firma_Ulica { get; set; }
    public string? Firma_Kod { get; set; }
    public string? Firma_Miejscowosc { get; set; }
    public string? Firma_NIP { get; set; }

    // Badanie (EDIT ma wszystkie pola bo badanie już wykonane)
    public int Bad_ID { get; set; }
    public string? Bad_bn_cennik { get; set; }  // nazwa cennika (string), nie ID
    public string? Bad_Fakt { get; set; }
    public string? Bad_Typ { get; set; }
    public DateTime? Bad_Data { get; set; }
    public DateTime? Bad_Data_Do { get; set; }
    public string? Bad_Wynik { get; set; }
    public decimal? Bad_Cena1 { get; set; }
    public decimal? Bad_Cena2 { get; set; }
    public decimal? Bad_Cena3 { get; set; }
    public decimal? Bad_Cena4 { get; set; }
    public decimal? Bad_Cena5 { get; set; }
    public decimal? Bad_Cena6 { get; set; }
    public decimal? Bad_Cena7 { get; set; }
    public decimal? Bad_Cena8 { get; set; }
    public decimal? Bad_Cena9 { get; set; }
    public decimal? Bad_Cena10 { get; set; }
    public decimal? Bad_Razem { get; set; }
    public string? Bad_Nr_KS { get; set; }

    // Computed properties dla UI
    public string PacjentDisplay => $"{P_imie} {P_nazwisko}";
    public string FirmaDisplay => Firma_Nazwa ?? "Brak";
    public string AdresDisplay => 
        $"{Firma_Ulica ?? ""}\n{Firma_Kod ?? ""} {Firma_Miejscowosc ?? ""}".Trim();
    public string DataBadaniaDisplay => Bad_Data?.ToString("yyyy-MM-dd") ?? "Brak";
}
