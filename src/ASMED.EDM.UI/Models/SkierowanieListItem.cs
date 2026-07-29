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
    public string? NumerFaktury { get; set; }
}
