namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Cennik badań — mapuje tabelę Access: BAD_Cennik
/// Kolumny: b_Cennik (Nazwa), b_activ (IsActiv)
/// </summary>
public class Cennik : BaseEntity
{
    /// <summary>Nazwa cennika — Access: b_Cennik</summary>
    public string Nazwa { get; set; } = string.Empty;

    /// <summary>Czy cennik jest aktywny — Access: b_activ</summary>
    public bool IsActiv { get; set; } = true;

    /// <summary>Cena badania podstawowego (lekarz) — Access: Bad_Cena1</summary>
    public decimal? CenaPodstawowa { get; set; }

    /// <summary>Cena laryngologa — Access: Bad_Cena2</summary>
    public decimal? CenaLaryngolog { get; set; }

    /// <summary>Cena okulisty — Access: Bad_Cena3</summary>
    public decimal? CenaOkulista { get; set; }

    /// <summary>Cena książeczki sanepid — Access: Bad_Cena4</summary>
    public decimal? CenaKsiazeczka { get; set; }

    /// <summary>Cena lipidogramu — Access: Bad_Cena5</summary>
    public decimal? CenaLipidogram { get; set; }

    /// <summary>Cena EKG — Access: Bad_Cena6</summary>
    public decimal? CenaEKG { get; set; }

    /// <summary>Cena poradni/urlopowej — Access: Bad_Cena7</summary>
    public decimal? CenaPoradnia { get; set; }

    /// <summary>Cena inne — Access: Bad_Cena8</summary>
    public decimal? CenaInne { get; set; }
}
