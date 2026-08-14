namespace ASMED.EDM.UI.Models;

public class SkierowanieUpdateDto
{
    public int? PacjentId { get; set; }
    public int? FirmaId { get; set; }
    public string? TypBadania { get; set; }
    public string? Stanowisko { get; set; }
    public bool Zaswiadczenie { get; set; }
    public bool Ksiazeczka { get; set; }
    public bool BadanieWstepne { get; set; } = false;
    public bool BadanieOkresowe { get; set; } = false;
    public bool BadanieKontrolne { get; set; } = false;
    public bool Orzeczenie { get; set; } = false;
    public bool B_EDM { get; set; } = false;

}
