using ASMED.EDM.Core.Enums;

namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Faktura — mapuje tabelę Access: Faktura
/// Kolumny: FK_ID, FK_Firma_ID, FK_Numer, FK_Data, FK_Kwota, FK_Status,
///          FK_PDF, FK_Num_Listy, FK_Suma_Bad, FK_Saldo, FK_Uwagi, FK_Cennik
/// </summary>
public class Faktura : BaseEntity
{
    /// <summary>Numer faktury — Access: FK_Numer</summary>
    public string? NumerFaktury { get; set; }

    /// <summary>Data wystawienia faktury — Access: FK_Data</summary>
    public DateTime? DataWystawienia { get; set; }

    /// <summary>Kwota brutto faktury — Access: FK_Kwota</summary>
    public decimal? KwotaBrutto { get; set; }

    /// <summary>Status faktury — Access: FK_Status (VARCHAR w bazie)</summary>
    public string? Status { get; set; }

    /// <summary>Ścieżka do pliku PDF faktury — Access: FK_PDF</summary>
    public string? PdfPath { get; set; }

    /// <summary>Suma wartości badań (bez narzutu) — Access: FK_Suma_Bad</summary>
    public decimal? SumaBadan { get; set; }

    /// <summary>Saldo (należność do zapłaty) — Access: FK_Saldo</summary>
    public decimal? Saldo { get; set; }

    /// <summary>Uwagi do faktury — Access: FK_Uwagi</summary>
    public string? Uwagi { get; set; }

    /// <summary>Nazwa cennika użytego na fakturze — Access: FK_Cennik</summary>
    public string? NazwaCennika { get; set; }

    // Klucze obce
    /// <summary>ID firmy (pracodawcy) — Access: FK_Firma_ID</summary>
    public int? FirmaId { get; set; }
    public Firma? Firma { get; set; }

    /// <summary>ID listy badań powiązanej z fakturą — Access: FK_Num_Listy</summary>
    public int? ListaBadanId { get; set; }
    public ListaBadan? ListaBadan { get; set; }
}
