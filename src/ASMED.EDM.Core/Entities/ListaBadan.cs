namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Lista badań do fakturowania — mapuje tabelę Access: Listy_Badan
/// Kolumny: L_ID (Id), powiązania z Firma i Badanie
/// </summary>
public class ListaBadan : BaseEntity
{
    /// <summary>Numer/identyfikator listy — Access: L_Numer lub Bad_L_ID (referencja)</summary>
    public string? NumerListy { get; set; }

    /// <summary>Data utworzenia listy</summary>
    public DateTime DataUtworzenia { get; set; } = DateTime.Today;

    /// <summary>Klucz obcy do firmy — Access: L_Firma_ID</summary>
    public int? FirmaId { get; set; }
    public Firma? Firma { get; set; }

    /// <summary>Klucz obcy do faktury (jeśli lista jest już sfakturowana) — Access: L_Faktura_ID</summary>
    public int? FakturaId { get; set; }
    public Faktura? Faktura { get; set; }

    /// <summary>Suma wartości badań na liście — Access: L_Suma</summary>
    public decimal? SumaRazem { get; set; }

    /// <summary>Uwagi do listy</summary>
    public string? Uwagi { get; set; }

    // Relacje
    public ICollection<Badanie> Badania { get; set; } = new List<Badanie>();
}
