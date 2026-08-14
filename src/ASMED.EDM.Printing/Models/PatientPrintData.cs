namespace ASMED.EDM.Printing.Models;

/// <summary>
/// Dane pacjenta/skierowania potrzebne do wypełnienia formularzy PDF (Karta/Orzeczenie/Sanitarne/Ankieta).
/// Współdzielony DTO - wypełniany z różnych ViewModeli (Wizyty, Skierowania) mapując pola z lokalnych modeli.
/// Nazwy odpowiadają legacy WybranyPacjent (WizytyViewViewModel / SkierPacjentaViewModel).
/// </summary>
public class PatientPrintData
{
    public int? B_ID { get; set; }
    public int? P_ID { get; set; }

    public string? P_Imie { get; set; }
    public string? P_Nazwisko { get; set; }
    public string? P_Pesel { get; set; }
    public string? P_Plec { get; set; }
    public DateTime? P_DataUrodzenia { get; set; }
    public string? P_Zawod { get; set; }
    public string? P_AdresUlica { get; set; }
    public string? P_AdresKod { get; set; }
    public string? P_AdresMiasto { get; set; }

    public string? Firma_Nazwa { get; set; }
    public string? Firma_Ulica { get; set; }
    public string? Firma_Kod { get; set; }
    public string? Firma_Miejscowosc { get; set; }
    public string? Firma_Email { get; set; }

    public DateTime? B_DataSkierowania { get; set; }
    public string? B_TypBadania { get; set; }
    public string? B_Stanowisko { get; set; }
    public DateTime? B_RegistrationDate { get; set; }

    public bool? B_CzynnikFizyczny { get; set; }
    public string? B_CzynnikFizycznyOpis { get; set; }
    public bool? B_CzynnikPylowy { get; set; }
    public string? B_CzynnikPylowyOpis { get; set; }
    public bool? B_CzynnikChemiczny { get; set; }
    public string? B_CzynnikChemicznyOpis { get; set; }
    public bool? B_CzynnikBiologiczny { get; set; }
    public string? B_CzynnikBiologicznyOpis { get; set; }
    public bool? B_CzynnikInny { get; set; }
    public string? B_CzynnikInnyOpis { get; set; }

    public string? SkierowanieNumer { get; set; }

    /// <summary>Data badania do naniesienia na formularz (opcjonalna - jeśli null, pola bd/bm/by pozostają puste).</summary>
    public DateTime? DataBadaniaDoWydruku { get; set; }
}
