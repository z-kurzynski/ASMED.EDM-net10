using ASMED.EDM.Printing.Models;

namespace ASMED.EDM.Printing.Services;

/// <summary>
/// Mapuje dane pacjenta/skierowania na nazwy pól formularza PDF (AcroForm).
/// Port 1:1 z legacy: WizytyViewViewModel.MapujPolaFormularza / SkierPacjentaViewModel.MapujPolaFormularza.
/// Nazwy pól MUSZĄ pozostać identyczne, ponieważ te same szablony PDF są używane
/// przez stary (ASMED.WPF) i nowy (ASMED.EDM.UI) program.
/// 
/// formularz pola PDF (AcroForm) - nazwa pola w szablonie PDF, np. "B_Nazwisko_B_Imię"
/// 
/// --- karta pacjenta/skierowania ---
/// 
/// x Data_wizyty - data wizyty w formacie dd.MM.yyyy
/// 
/// x B_Nazwisko_B_Imię
/// 
/// x P_1, P_2, ..., P_11 - PESEL rozbity na cyfry
/// 
/// x du_1, du_2, ..., du_8 - Data urodzenia rozbita na cyfry (ddMMyyyy)
/// 
/// x P_adr - adres pacjenta
/// 
/// dr_1, dr_2, mr_1, mr_2, yr_1, yr_2, yr_3, yr_4 - Data skierowania rozbita na cyfry (ddMMyyyy)
/// 
/// x header3 - nagłówek z numerem skierowania
/// 
/// x M,K - zaznaczenie płci stawiamy "X" w odpowiednim polu
/// 
/// x  kp_1, kp_2 - kod pocztowy pacjenta rozbity na dwie części
/// 
/// x B_Firma - nazwa firmy pacjenta
/// 
/// x F_adr - adres firmy pacjenta
/// 
/// IS_TAK, IS_NIE - zaznaczenie, czy są czynniki szkodliwe
/// 
/// SB_Tak, SZ_Tak, SZ_NIE - zaznaczenie, czy są czynniki szkodliwe
/// 
/// SZ_Tak, SZ_NIE - zaznaczenie, czy są czynniki szkodliwe
/// 
/// rr_1, rr_2, rm_1, rm_2, ry_1, ry_2, ry_3, ry_4 - Data rejestracji karty rozbita na cyfry (ddMMyyyy)
/// 
/// ddData badania,ddData badania_2,mc_4,mc_5,rok_3,rok_4 - Data badania rozbita na cyfry (ddMMyyyy)
/// 
/// IS_OPIS - opis czynników szkodliwych
/// 
/// header - nagłówek z numerem skierowania i datą skierowania
/// 
/// RR -nie używane, legacy
/// 
/// B_zawód - zawód pacjenta
/// 
/// ddData wydania orzeczenia,ddData wydania orzeczenia_2,mc_7,mc_8,rok_5,rok_6 - Data wydania orzeczenia rozbita na cyfry (ddMMyyyy)
/// 
/// --- ankieta ---
/// 
/// 
/// 
/// </summary>
public static class PatientFormFieldMapper
{
    /// <summary>
    /// Nazwy szablonów PDF, dla których nie generujemy kodu QR/kreskowego.
    /// </summary>
    private static readonly HashSet<string> TemplatesWithoutBarcode = new(StringComparer.OrdinalIgnoreCase)
    {
        "ASMED_Orzeczenie.pdf",
        "ASMED__Sanitarne.pdf",
        "ASMED_Ankieta.pdf"
    };

    public static Dictionary<string, string> Map(PatientPrintData? patient, string templateFileName = "")
    {
        var map = new Dictionary<string, string>();

        if (patient == null)
            return map;

        // ASMED - Firma
        map["A_Nazwa"] = "Niepubliczny ZOZ ASMED S.C \n Kurzyńska Stefania  Kurzyński Zbigniew\n 04-028 Warszawa \n AL. Stanów Zjednoczonyc 51 pok. 204 \n tel. 22 81 44 02 \n NIP 113 83 31 776  Nr Księgi 7582";

        // Typ badania
        if (!string.IsNullOrEmpty(patient.B_TypBadania))
        {
            if (patient.B_TypBadania.ToUpper() == "W")
                map["Typ_Bad"] = "wstępne";
            else if (patient.B_TypBadania.ToUpper() == "O")
                map["Typ_Bad"] = "okresowe";
            else if (patient.B_TypBadania.ToUpper() == "K")
                map["Typ_Bad"] = "kontrolne";
        }

        // Basic fields
        map["B_TypBadania"] = patient.B_TypBadania ?? string.Empty;
        map["B_Nazwisko_B_Imię"] = $"{patient.P_Nazwisko} {patient.P_Imie}".Trim();
        map["B_Firma"] = patient.Firma_Nazwa ?? string.Empty;
        map["B_zawód"] = !string.IsNullOrWhiteSpace(patient.B_Stanowisko)
            ? patient.B_Stanowisko
            : patient.P_Zawod ?? string.Empty;

        // Data urodzenia (ddMMyyyy)
        var dataUrodzenia = patient.P_DataUrodzenia.HasValue
            ? patient.P_DataUrodzenia.Value.ToString("ddMMyyyy")
            : "";
        for (int i = 0; i < 8; i++)
            map[$"du_{i + 1}"] = dataUrodzenia.Length > i ? dataUrodzenia[i].ToString() : string.Empty;

        // Data skierowania
        if (patient.B_DataSkierowania.HasValue)
        {
            map["B_DataSkierowania"] = patient.B_DataSkierowania.Value.ToString("dd.MM.yyyy");

            var registrationDate = patient.B_DataSkierowania.Value.ToString("ddMMyyyy");

            map["dr_1"] = registrationDate.Length > 0 ? registrationDate[0].ToString() : string.Empty;
            map["dr_2"] = registrationDate.Length > 1 ? registrationDate[1].ToString() : string.Empty;
            map["mr_1"] = registrationDate.Length > 2 ? registrationDate[2].ToString() : string.Empty;
            map["mr_2"] = registrationDate.Length > 3 ? registrationDate[3].ToString() : string.Empty;
            map["yr_1"] = registrationDate.Length > 4 ? registrationDate[4].ToString() : string.Empty;
            map["yr_2"] = registrationDate.Length > 5 ? registrationDate[5].ToString() : string.Empty;
            map["yr_3"] = registrationDate.Length > 6 ? registrationDate[6].ToString() : string.Empty;
            map["yr_4"] = registrationDate.Length > 7 ? registrationDate[7].ToString() : string.Empty;
        }
        else
        {
            map["B_DataSkierowania"] = string.Empty;
            map["dr_1"] = string.Empty;
            map["dr_2"] = string.Empty;
            map["mr_1"] = string.Empty;
            map["mr_2"] = string.Empty;
            map["yr_1"] = string.Empty;
            map["yr_2"] = string.Empty;
            map["yr_3"] = string.Empty;
            map["yr_4"] = string.Empty;
        }

        // Data rejestracji (B_RegistrationDate) - rozbicie na cyfry
        if (patient.B_RegistrationDate.HasValue)
        {
            map["B_RegistrationDate"] = patient.B_RegistrationDate.Value.ToString("dd.MM.yyyy");

            var registrationDateStr = patient.B_RegistrationDate.Value.ToString("ddMMyyyy");

            map["rr_1"] = registrationDateStr.Length > 0 ? registrationDateStr[0].ToString() : string.Empty;
            map["rr_2"] = registrationDateStr.Length > 1 ? registrationDateStr[1].ToString() : string.Empty;
            map["rm_1"] = registrationDateStr.Length > 2 ? registrationDateStr[2].ToString() : string.Empty;
            map["rm_2"] = registrationDateStr.Length > 3 ? registrationDateStr[3].ToString() : string.Empty;
            map["ry_1"] = registrationDateStr.Length > 4 ? registrationDateStr[4].ToString() : string.Empty;
            map["ry_2"] = registrationDateStr.Length > 5 ? registrationDateStr[5].ToString() : string.Empty;
            map["ry_3"] = registrationDateStr.Length > 6 ? registrationDateStr[6].ToString() : string.Empty;
            map["ry_4"] = registrationDateStr.Length > 7 ? registrationDateStr[7].ToString() : string.Empty;
        }
        else
        {
            map["B_RegistrationDate"] = string.Empty;
            map["rr_1"] = string.Empty;
            map["rr_2"] = string.Empty;
            map["rm_1"] = string.Empty;
            map["rm_2"] = string.Empty;
            map["ry_1"] = string.Empty;
            map["ry_2"] = string.Empty;
            map["ry_3"] = string.Empty;
            map["ry_4"] = string.Empty;
        }

        // PESEL
        var pesel = patient.P_Pesel ?? string.Empty;
        for (int i = 1; i <= 11; i++)
            map[$"P_{i}"] = pesel.Length >= i ? pesel[i - 1].ToString() : string.Empty;
        map["PESEL"] = pesel;

        // Płeć
        map["M"] = (patient.P_Plec?.ToUpper() == "M") ? "X" : string.Empty;
        map["K"] = (patient.P_Plec?.ToUpper() == "K") ? "X" : string.Empty;
        map["Plec"] = patient.P_Plec ?? string.Empty;

        // Adres pacjenta
        map["P_adr"] = $"{patient.P_AdresUlica} {patient.P_AdresMiasto}".Trim();
        var kod = patient.P_AdresKod ?? string.Empty;
        var kodParts = kod.Split('-');
        map["kp_1"] = kodParts.Length > 0 ? kodParts[0] : string.Empty;
        map["kp_2"] = kodParts.Length > 1 ? kodParts[1] : string.Empty;
        map["Full_Adress"] = $"{patient.P_AdresKod} {patient.P_AdresMiasto}, {patient.P_AdresUlica}".Trim();

        // Adres firmy
        map["F_adr"] = $"{patient.Firma_Ulica} {patient.Firma_Miejscowosc}".Trim();
        var kodF = patient.Firma_Kod ?? string.Empty;
        var kodFParts = kodF.Split('-');
        map["KF_1"] = kodFParts.Length > 0 ? kodFParts[0] : string.Empty;
        map["KF_2"] = kodFParts.Length > 1 ? kodFParts[1] : string.Empty;
        map["Full_Adress_Firma"] = $"{patient.Firma_Kod} {patient.Firma_Miejscowosc}, {patient.Firma_Ulica}".Trim();

        // Czynniki szkodliwe
        map["SB_Tak"] = "X";
        bool szkodliwy = patient.B_CzynnikFizyczny == true ||
                 patient.B_CzynnikPylowy == true ||
                 patient.B_CzynnikChemiczny == true ||
                 patient.B_CzynnikBiologiczny == true ||
                 patient.B_CzynnikInny == true;
        map["SZ_Tak"] = szkodliwy ? "X" : string.Empty;
        map["SZ_NIE"] = !szkodliwy ? "X" : string.Empty;

        var opisy = new[]
        {
            patient.B_CzynnikFizycznyOpis,
            patient.B_CzynnikPylowyOpis,
            patient.B_CzynnikChemicznyOpis,
            patient.B_CzynnikBiologicznyOpis,
            patient.B_CzynnikInnyOpis
        };
        bool czyOpis = opisy.Any(x => !string.IsNullOrWhiteSpace(x));
        map["IS_TAK"] = czyOpis ? "X" : string.Empty;
        map["IS_NIE"] = !czyOpis ? "X" : string.Empty;
        map["IS_OPIS"] = string.Join(" - ", opisy.Where(x => !string.IsNullOrWhiteSpace(x)));

        // Nagłówki/stopki
        map["header"] = $"({patient.B_ID ?? 0}) - {(patient.B_DataSkierowania ?? DateTime.Now):dd.MM.yyyy}".Trim();
        map["header4"] = $"{patient.B_ID ?? 0} - {(patient.B_DataSkierowania ?? DateTime.Now):dd.MM.yyyy} {patient.P_Imie} {patient.P_Nazwisko}".Trim();
        map["footer"] = "ASMED - Niepubliczny Zakład Opieki Zdrowotnej04-028 Warszawa Al. Stanów Zjednoczonych51 pok.204 tel.22814402 NIP1138331776 Nr Księgi7582";
        map["header2"] = $"ID: {patient.B_ID ?? 0} / {patient.P_ID}".Trim();
        map["header3"] = $"Karta nr: {patient.B_ID ?? 0} ".Trim();

        int skierowanieId = patient.B_ID ?? 0;
        map["header3_barcode"] = (skierowanieId > 0)
            ? $"#{skierowanieId.ToString().PadLeft(4, '0')}"
            : string.Empty;
        map["PatientLastName"] = patient.P_Nazwisko ?? string.Empty;

        // Data badania (rozbicie na pojedyncze cyfry)
        if (patient.DataBadaniaDoWydruku.HasValue)
        {
            var dataBadaniaStr = patient.DataBadaniaDoWydruku.Value.ToString("ddMMyyyy");

            map["bd1"] = dataBadaniaStr.Length > 0 ? dataBadaniaStr[0].ToString() : string.Empty;
            map["bd2"] = dataBadaniaStr.Length > 1 ? dataBadaniaStr[1].ToString() : string.Empty;
            map["bm1"] = dataBadaniaStr.Length > 2 ? dataBadaniaStr[2].ToString() : string.Empty;
            map["bm2"] = dataBadaniaStr.Length > 3 ? dataBadaniaStr[3].ToString() : string.Empty;
            map["by1"] = dataBadaniaStr.Length > 4 ? dataBadaniaStr[4].ToString() : string.Empty;
            map["by2"] = dataBadaniaStr.Length > 5 ? dataBadaniaStr[5].ToString() : string.Empty;
            map["by3"] = dataBadaniaStr.Length > 6 ? dataBadaniaStr[6].ToString() : string.Empty;
            map["by4"] = dataBadaniaStr.Length > 7 ? dataBadaniaStr[7].ToString() : string.Empty;

            map["DataBadania"] = patient.DataBadaniaDoWydruku.Value.ToString("dd.MM.yyyy");
            map["DataBadania_Full"] = patient.DataBadaniaDoWydruku.Value.ToString("dd.MMMM.yyyy");
            map["DataBadania_Txt"] = patient.DataBadaniaDoWydruku.Value.ToString("dd-MMMMM-yyyy");
            map["DataBadania_Day"] = patient.DataBadaniaDoWydruku.Value.ToString("dd");
            map["DataBadania_Month"] = patient.DataBadaniaDoWydruku.Value.ToString("MM");
            map["DataBadania_Year"] = patient.DataBadaniaDoWydruku.Value.ToString("yyyy");
            map["DataBadania_DayOfWeek"] = patient.DataBadaniaDoWydruku.Value.ToString("dddd");
            map["DataBadania_Time"] = patient.DataBadaniaDoWydruku.Value.ToString("HH:mm");
            map["DataBadania_Timestamp"] = patient.DataBadaniaDoWydruku.Value.ToString("yyyyMMddHHmmss");
        }

        // Jeśli drukujemy Ankietę/Orzeczenie/Sanitarne, nie generujemy kodów - obsługiwane w PdfFormService na podstawie nazwy szablonu
        _ = TemplatesWithoutBarcode.Contains(templateFileName);

        return map;
    }
}
