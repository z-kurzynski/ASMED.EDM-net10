namespace ASMED.EDM.UI.Models;

public class SkierowanieUpdateDto
{
    public int? PacjentId { get; set; }
    public int? FirmaId { get; set; }
    public string? TypBadania { get; set; }
    public string? Stanowisko { get; set; }
    public bool Zaswiadczenie { get; set; }
    public bool Ksiazeczka { get; set; }
}
