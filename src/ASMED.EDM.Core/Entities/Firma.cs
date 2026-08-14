namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Firma (pracodawca) — mapuje tabelę Access: Firma
/// Kolumny: id, Nazwa, NIP, activ, cennik, FKemail
/// </summary>
public class Firma : BaseEntity
{
    /// <summary>Nazwa firmy (pracodawcy) — Access: Nazwa</summary>
    public string Nazwa { get; set; } = string.Empty;

    /// <summary>Numer identyfikacji podatkowej — Access: NIP</summary>
    public string? NIP { get; set; }

    /// <summary>Czy firma jest aktywna — Access: activ</summary>
    public bool? Activ { get; set; } = true;

    /// <summary>Domyślny cennik przypisany do firmy — Access: cennik</summary>
    public string? NazwaCennika { get; set; }

    /// <summary>Adres e-mail do faktur — Access: FKemail</summary>
    public string? FkEmail { get; set; }

    /// <summary>Kod pocztowy — Access: Kod</summary>
    public string? Kod { get; set; }

    /// <summary>Miejscowość — Access: Miejscowosc</summary>
    public string? Miejscowosc { get; set; }

    /// <summary>Ulica i numer — Access: Ulica</summary>
    public string? Ulica { get; set; }

    /// <summary>Data obowiazywania umowy — MySQL: umowa_do</summary>
    public DateTime? UmowaDo { get; set; }

    /// <summary>Czy umowa jest na czas nieokreslony — MySQL: czas_nieokreslon</summary>
    public bool CzasNieokreslon { get; set; }

    /// <summary>Osoba kontaktowa — MySQL: Osoba_kontaktowa</summary>
    public string? Osoba_kontaktowa { get; set; }

    /// <summary>Telefon kontaktowy — MySQL: Telefon</summary>
    public string? Telefon { get; set; }

    // Relacje
    public ICollection<Faktura> Faktury { get; set; } = new List<Faktura>();
    public ICollection<ListaBadan> ListyBadan { get; set; } = new List<ListaBadan>();
}
