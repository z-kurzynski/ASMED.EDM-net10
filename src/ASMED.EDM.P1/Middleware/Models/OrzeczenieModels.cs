using System.Text.Json;
using System.Text.Json.Serialization;

namespace ASMED.EDM.P1.Middleware.Models;

/// <summary>Pracodawca — element modelu orzeczenia medycyny pracy.</summary>
public class MpPracodawca
{
    [JsonPropertyName("nip")]
    public string? Nip { get; set; }

    [JsonPropertyName("regon9")]
    public string? Regon9 { get; set; }

    [JsonPropertyName("nazwa")]
    public string? Nazwa { get; set; }

    [JsonPropertyName("adres")]
    public MpAdres? Adres { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }
}

/// <summary>Stanowisko pracy, którego dotyczy orzeczenie.</summary>
public class MpStanowisko
{
    [JsonPropertyName("zawod")]
    public string? Zawod { get; set; }

    [JsonPropertyName("pracodawca")]
    public MpPracodawca? Pracodawca { get; set; }
}

/// <summary>Okres przeniesienia do innej pracy (dla werdyktu o utracie zdolności).</summary>
public class MpOkres
{
    [JsonPropertyName("od")]
    public string? Od { get; set; }

    [JsonPropertyName("do")]
    public string? Do { get; set; }
}

/// <summary>Werdykt orzeczenia (symbol wg rozporządzenia + opcjonalne dane utraty zdolności).</summary>
public class MpWerdykt
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("dataUtratyZdolnosci")]
    public string? DataUtratyZdolnosci { get; set; }

    [JsonPropertyName("okresPrzeniesienia")]
    public MpOkres? OkresPrzeniesienia { get; set; }

    [JsonPropertyName("rodzajOkresuPrzeniesienia")]
    public string? RodzajOkresuPrzeniesienia { get; set; }
}

/// <summary>
/// Dokument orzeczenia lekarza medycyny pracy — używany przy wystawianiu
/// (POST /medycynapracy/orzeczenie), weryfikacji (POST .../orzeczenie/weryfikuj)
/// oraz jako baza modelu przy odczycie.
/// </summary>
public class MpOrzeczenie
{
    [JsonPropertyName("numerDokumentu")]
    public string NumerDokumentu { get; set; } = string.Empty;

    [JsonPropertyName("numerSkierowania")]
    public string? NumerSkierowania { get; set; }

    /// <summary>Format yyyy-MM-dd.</summary>
    [JsonPropertyName("dataSkierowania")]
    public string? DataSkierowania { get; set; }

    [JsonPropertyName("pacjent")]
    public MpPacjent Pacjent { get; set; } = new();

    /// <summary>WSTEPNE / OKRESOWE / KONTROLNE.</summary>
    [JsonPropertyName("rodzajBadania")]
    public string RodzajBadania { get; set; } = string.Empty;

    /// <summary>PRZYJMOWANY / ZATRUDNIONY.</summary>
    [JsonPropertyName("rodzajZatrudnienia")]
    public string RodzajZatrudnienia { get; set; } = string.Empty;

    [JsonPropertyName("stanowiska")]
    public List<MpStanowisko> Stanowiska { get; set; } = [];

    [JsonPropertyName("werdykt")]
    public MpWerdykt Werdykt { get; set; } = new();

    /// <summary>Format yyyy-MM-dd.</summary>
    [JsonPropertyName("dataNastepnegoBadania")]
    public string? DataNastepnegoBadania { get; set; }

    [JsonPropertyName("miejsceWystawienia")]
    public string? MiejsceWystawienia { get; set; }

    [JsonPropertyName("uwagi")]
    public string? Uwagi { get; set; }
}

/// <summary>
/// Kryteria odczytu orzeczenia po identyfikatorze dokumentu (POST /medycynapracy/orzeczenie/odczyt).
/// Zgodnie z oficjalną kolekcją Postman ("Odczytaj orzeczenie"), API oczekuje dwóch osobnych pól
/// formularza: identyfikatorRoot i identyfikatorExtension (nie połączonego identyfikatorDokumentu).
/// </summary>
public class MpOrzeczenieOdczytKryteria
{
    public string IdentyfikatorRoot { get; set; } = string.Empty;

    public string IdentyfikatorExtension { get; set; } = string.Empty;
}

/// <summary>
/// Kryteria wyszukiwania orzeczeń (POST /medycynapracy/orzeczenie/wyszukaj).
/// API akceptuje wyłącznie pola: dataWystawieniaOd, dataWystawieniaDo, identyfikatorUslugobiorcy,
/// identyfikatorAutora, identyfikatorDokumentuSkierowania, statusDokumentu, rodzajDokumentu.
/// </summary>
public class MpKryteriaWyszukiwaniaOrzeczenia
{
    [JsonPropertyName("dataWystawieniaOd")]
    public string? DataWystawieniaOd { get; set; }

    [JsonPropertyName("dataWystawieniaDo")]
    public string? DataWystawieniaDo { get; set; }

    [JsonPropertyName("identyfikatorUslugobiorcy")]
    public string? IdentyfikatorUslugobiorcy { get; set; }

    [JsonPropertyName("identyfikatorAutora")]
    public string? IdentyfikatorAutora { get; set; }

    [JsonPropertyName("identyfikatorDokumentuSkierowania")]
    public string? IdentyfikatorDokumentuSkierowania { get; set; }

    [JsonPropertyName("statusDokumentu")]
    public string? StatusDokumentu { get; set; }

    [JsonPropertyName("rodzajDokumentu")]
    public string? RodzajDokumentu { get; set; }
}

/// <summary>Dane anulowania dokumentu orzeczenia (POST /medycynapracy/orzeczenie/anuluj).</summary>
public class MpAnulowanie
{
    [JsonPropertyName("numerDokumentu")]
    public string NumerDokumentu { get; set; } = string.Empty;

    [JsonPropertyName("anulowanyNumerDokumentu")]
    public string AnulowanyNumerDokumentu { get; set; } = string.Empty;

    [JsonPropertyName("anulowanaWersja")]
    public int AnulowanaWersja { get; set; }
}

/// <summary>Koperta odpowiedzi Middleware dla operacji na orzeczeniu.</summary>
public class MiddlewareOrzeczenieResponse<TOrzeczenie>
{
    [JsonPropertyName("isError")]
    public bool IsError { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// API zwraca komunikaty walidacyjne w polu "messages" w niespójnym formacie:
    /// czasem jako string, czasem jako obiekt { severity, diagnostics, location }.
    /// </summary>
    [JsonPropertyName("messages")]
    public JsonElement Messages { get; set; }

    [JsonIgnore]
    public string? MessagesText => MiddlewareMessageParser.ExtractText(Messages);

    [JsonPropertyName("orzeczenie")]
    public TOrzeczenie? Orzeczenie { get; set; }
}
