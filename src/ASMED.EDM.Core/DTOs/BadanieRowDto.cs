namespace ASMED.EDM.Core.DTOs;

/// <summary>
/// DTO zwracane przez Dapper z pełnym JOIN Badanie + P_Pacjent + Firma.
/// Używane zarówno w warstwie Data (repozytorium) jak i UI (ViewModel).
/// </summary>
public class BadanieRowDto
{
    public int?     Bad_ID        { get; set; }
    public int?     Bad_L_ID      { get; set; }
    /// <summary>ID faktury (Badanie.Bad_F_ID) — to samo co Bad_Fakt_ID.</summary>
    public int?     Bad_F_ID      { get; set; }
    public int?     Bad_P_ID      { get; set; }
    /// <summary>ID skierowania (Badanie.Bad_S_ID)</summary>
    public int?     Bad_S_ID      { get; set; }

    public DateTime? Bad_Data     { get; set; }
    public DateTime? Bad_Data_Do  { get; set; }
    public string?  Bad_Typ       { get; set; }
    public string?  Bad_Wynik     { get; set; }
    public string?  Bad_Nr_KS     { get; set; }
    public bool?    Bad_END       { get; set; }
    public string?  Bad_bn_cennik { get; set; }

    public decimal? Bad_Razem  { get; set; }
    public decimal? Bad_Cena1  { get; set; }
    public decimal? Bad_Cena2  { get; set; }
    public decimal? Bad_Cena3  { get; set; }
    public decimal? Bad_Cena4  { get; set; }
    public decimal? Bad_Cena5  { get; set; }
    public decimal? Bad_Cena6  { get; set; }
    public decimal? Bad_Cena7  { get; set; }
    public decimal? Bad_Cena8  { get; set; }
    public decimal? Bad_Cena9  { get; set; }
    public decimal? Bad_Cena10 { get; set; }

    public string?  P_imie      { get; set; }
    public string?  P_nazwisko  { get; set; }
    public string?  P_zawod     { get; set; }

    public string?  FirmaNazwa   { get; set; }
    public string?  FirmaCennik  { get; set; }

    public string PacjentDisplay => $"{P_imie} {P_nazwisko}".Trim();

    // Numer porządkowy w prawej liście
    public int Lp { get; set; }
}
