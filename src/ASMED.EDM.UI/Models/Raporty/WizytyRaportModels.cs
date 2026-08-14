using System.Collections.ObjectModel;
using System.Linq;

namespace ASMED.EDM.UI.Models.Raporty;

/// <summary>
/// DTO dla raportu niezafakturowanych badań pogrupowanych po firmie.
/// Przeniesione z legacy AccessDbContext.NiezafakturowaneBadaniaDto.
/// </summary>
public class NiezafakturowaneBadaniaDto
{
    public string? NazwaFirmy { get; set; }
    public decimal? SumaWartosci { get; set; }
    public int LiczbaBadan { get; set; }

    public string SumaWartosciFormatted => SumaWartosci.HasValue
        ? $"{SumaWartosci.Value:N2} zł"
        : "0,00 zł";
}

/// <summary>
/// DTO dla statystyk miesięcznych skierowań/wizyt/badań.
/// Przeniesione z legacy AccessDbContext.StatystykaMiesiecznaDto.
/// </summary>
public class StatystykaMiesiecznaDto
{
    public int Rok { get; set; }
    public int Miesiac { get; set; }
    public string MiesiacNazwa { get; set; } = string.Empty;

    // Skierowania (B_RegistrationDate)
    public int LiczbaSkierowan { get; set; }

    // Badania wg typu (B_TypBadania)
    public int BadaniaOkresowe { get; set; }
    public int BadaniaWstepne { get; set; }
    public int BadaniaKontrolne { get; set; }
    public int BadaniaInne { get; set; }

    // Książeczki (B_ksiazeczka = true)
    public int LiczbaKsiazeczek { get; set; }

    // Badania odbyte (Bad_Data)
    public int BadaniaOdbyte { get; set; }

    // Wizyty zarejestrowane (R_Data)
    public int WizytyZarejestrowane { get; set; }

    // Wartość badań w miesiącu (suma Bad_Razem)
    public decimal WartoscBadan { get; set; }

    public string MiesiacRokDisplay => $"{MiesiacNazwa} {Rok}";
}

/// <summary>
/// DTO ze skalowanymi wartościami [zł] do zakresu osi Y (ilości), używane na wykresie.
/// </summary>
public class StatystykaMiesiecznaSkalowanaDto
{
    public string MiesiacNazwa { get; set; } = string.Empty;
    public int LiczbaSkierowan { get; set; }
    public int BadaniaOdbyte { get; set; }
    public int WizytyZarejestrowane { get; set; }
    public decimal WartoscBadan { get; set; }
    public double WartoscBadanSkalowana { get; set; }

    public static ObservableCollection<StatystykaMiesiecznaSkalowanaDto> FromStatystyki(
        ObservableCollection<StatystykaMiesiecznaDto>? statystyki)
    {
        var result = new ObservableCollection<StatystykaMiesiecznaSkalowanaDto>();

        if (statystyki == null || statystyki.Count == 0)
            return result;

        var maxIlosc = System.Math.Max(
            statystyki.Max(x => x.LiczbaSkierowan),
            System.Math.Max(
                statystyki.Max(x => x.BadaniaOdbyte),
                statystyki.Max(x => x.WizytyZarejestrowane)));

        var maxWartosc = statystyki.Max(x => x.WartoscBadan);
        var wspolczynnik = maxWartosc > 0 ? (double)maxIlosc / (double)maxWartosc : 0;

        foreach (var item in statystyki)
        {
            result.Add(new StatystykaMiesiecznaSkalowanaDto
            {
                MiesiacNazwa = item.MiesiacNazwa,
                LiczbaSkierowan = item.LiczbaSkierowan,
                BadaniaOdbyte = item.BadaniaOdbyte,
                WizytyZarejestrowane = item.WizytyZarejestrowane,
                WartoscBadan = item.WartoscBadan,
                WartoscBadanSkalowana = (double)item.WartoscBadan * wspolczynnik
            });
        }

        return result;
    }
}
