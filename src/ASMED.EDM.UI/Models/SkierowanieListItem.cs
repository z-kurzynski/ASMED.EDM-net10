namespace ASMED.EDM.UI.Models;

/// <summary>
/// Model DTO dla wiersza listy skierowań (kart badań) w widoku SkierowaniaView.
/// </summary>
public class SkierowanieListItem
{
    public int Id { get; set; }
    public int PacjentId { get; set; }
    public string Typ { get; set; } = string.Empty;
    public string Imie { get; set; } = string.Empty;
    public string Nazwisko { get; set; } = string.Empty;
    public string Pesel { get; set; } = string.Empty;
    public string Firma { get; set; } = string.Empty;
    public DateTime? Data { get; set; }
    public DateTime? DataRej { get; set; }
    public DateTime? DataBad { get; set; }
    public DateTime? DataDo { get; set; }
    public string? NumerFaktury { get; set; }
    public bool BadanieWstepne { get; set; } = false;
    public bool BadanieOkresowe { get; set; } = false;
    public bool BadanieKontrolne { get; set; } = false;
    public bool Wywiad { get; set; } = false;
    public bool Orzeczenie { get; set; } = false;
    public bool B_EDM { get; set; } = false;
    public int? EdmDocumentId { get; set; }

    /// <summary>
    /// True gdy którykolwiek rekord Rejestracja dla tego skierowania ma R_Data == dzisiaj.
    /// Używane przez DataTrigger do zmiany koloru przycisku wizyta_na_Dzis na zielony.
    /// </summary>
    public bool Rejestracja => DataRej?.Date == DateTime.Today;

    public bool Urlop { get; internal set; }
    public bool Laryngolog { get; internal set; }
    public bool Okulista { get; internal set; }
    public string StatusBad { get; internal set; } = string.Empty;
}
