using System;

namespace ASMED.EDM.UI.Models.BadaniaNew;

/// <summary>
/// DTO dla listy badań w widoku "Zakończ Badanie" (BadaniaNewView)
/// Zawiera minimalne dane potrzebne do wyświetlenia listy w lewej kolumnie
/// </summary>
public class BadaniaNewListDto
{
    // Skierowanie
    public int B_ID { get; set; }
    public DateTime? B_DataSkierowania { get; set; }
    public string? B_TypBadania { get; set; }
    public string? B_Stanowisko { get; set; }
    public bool? B_Zaswiadczenie { get; set; }
    public bool? B_ksiazeczka { get; set; }
    public bool? B_wywiad { get; set; }
    public bool? B_ankieta { get; set; } = false;
    public bool? B_orzeczenie { get; set; }
    public int? B_orzeczenie_ID { get; set; }
    public bool? B_Activ { get; set; }
    public string? B_uwagi { get; set; }
    public int? B_edm_document_id { get; set; }

    // Pacjent (tylko podstawowe dane dla listy)
    public int P_ID { get; set; } // Dodane dla SaveBadanie
    public string? P_imie { get; set; }
    public string? P_nazwisko { get; set; }
    public string? P_pesel { get; set; }
    public string? P_zawod { get; set; } // Dodane dla SaveBadanie (Stanowisko w UpdateSkierowanie)

    // Firma (tylko nazwa dla listy)
    public int? FirmaId { get; set; } // Dodane dla SaveBadanie
    public string? Firma_Nazwa { get; set; }
    public string? Firma_NIP { get; set; }

    // Badanie
    public DateTime? Bad_Data { get; set; }

    // Computed properties for display
    public string PacjentDisplay => $"{P_nazwisko} {P_imie}";
    public string FormattedPesel => P_pesel ?? "---";
}
