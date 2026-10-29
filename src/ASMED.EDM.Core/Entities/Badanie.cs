namespace ASMED.EDM.Core.Entities;

/// <summary>
/// Badanie medyczne — mapuje tabelę Access: Badanie
/// Kolumny: Bad_ID, Bad_L_ID, Bad_F_ID, Bad_P_ID, Bad_S_ID, Bad_R_ID,
///          Bad_Typ, Bad_Data, Bad_Data_Do, Bad_Wynik, Bad_Nr_KS, Bad_END,
///          Bad_bn_cennik, Bad_Cena1-10, Bad_Razem
/// </summary>
public class Badanie : BaseEntity
{
    // ─── Typ i daty ───────────────────────────────────────────────────────────

    /// <summary>Typ badania (Wstępne / Okresowe / Kontrolne) — Access: Bad_Typ</summary>
    public string? TypBadania { get; set; }

    /// <summary>Data wykonania badania — Access: Bad_Data</summary>
    public DateTime? DataBadania { get; set; }

    /// <summary>Data ważności orzeczenia — Access: Bad_Data_Do</summary>
    public DateTime? DataWaznosci { get; set; }

    /// <summary>
    /// Wynik badania — symbol werdyktu EDM: 21 brak przeciwwskazań / 22 przeciwwskazania zdrowotne /
    /// 23 utrata zdolności (zob. Helpers.WynikBadania) — Access: Bad_Wynik
    /// </summary>
    public string? Wynik { get; set; }

    /// <summary>Numer księgi — Access: Bad_Nr_KS</summary>
    public string? NrKsiegi { get; set; }

    /// <summary>Czy badanie zakończone — Access: Bad_END</summary>
    public bool IsZakonczone { get; set; } = false;

    /// <summary>Nazwa cennika użytego przy badaniu — Access: Bad_bn_cennik</summary>
    public string? NazwaCennika { get; set; }

    // ─── Ceny jednostkowe (Bad_Cena1-10) ─────────────────────────────────────

    /// <summary>Cena badania podstawowego (lekarz) — Access: Bad_Cena1</summary>
    public decimal? Cena1Podstawowa { get; set; }

    /// <summary>Cena laryngologa — Access: Bad_Cena2</summary>
    public decimal? Cena2Laryngolog { get; set; }

    /// <summary>Cena okulisty — Access: Bad_Cena3</summary>
    public decimal? Cena3Okulista { get; set; }

    /// <summary>Cena książeczki sanepid — Access: Bad_Cena4</summary>
    public decimal? Cena4Ksiazeczka { get; set; }

    /// <summary>Cena lipidogramu — Access: Bad_Cena5</summary>
    public decimal? Cena5Lipidogram { get; set; }

    /// <summary>Cena EKG — Access: Bad_Cena6</summary>
    public decimal? Cena6EKG { get; set; }

    /// <summary>Cena poradni/urlopowej — Access: Bad_Cena7</summary>
    public decimal? Cena7Poradnia { get; set; }

    /// <summary>Cena inne — Access: Bad_Cena8</summary>
    public decimal? Cena8Inne { get; set; }

    /// <summary>Cena dodatkowa 9 — Access: Bad_Cena9</summary>
    public decimal? Cena9 { get; set; }

    /// <summary>Cena dodatkowa 10 — Access: Bad_Cena10</summary>
    public decimal? Cena10 { get; set; }

    /// <summary>Łączna wartość badania (suma cen) — Access: Bad_Razem</summary>
    public decimal? Razem { get; set; }

    // ─── Klucze obce ─────────────────────────────────────────────────────────

    /// <summary>ID pacjenta — Access: Bad_P_ID</summary>
    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>ID listy badań (powiązanie z fakturą) — Access: Bad_L_ID</summary>
    public int? ListaBadanId { get; set; }
    public ListaBadan? ListaBadan { get; set; }

    /// <summary>ID faktury — Access: Bad_F_ID</summary>
    public int? FakturaId { get; set; }
    public Faktura? Faktura { get; set; }

    /// <summary>ID skierowania — Access: Bad_S_ID</summary>
    public int? SkierowanieId { get; set; }

    /// <summary>ID rejestracji — Access: Bad_R_ID</summary>
    public int? RejestracjaId { get; set; }
}
