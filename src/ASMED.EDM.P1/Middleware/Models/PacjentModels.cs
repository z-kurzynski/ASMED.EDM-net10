using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ASMED.EDM.P1.Middleware.Models;

/// <summary>
/// Dane autora dokumentu (lekarza medycyny pracy i placówki) wymagane przez
/// wszystkie operacje /medycynapracy/*.
/// </summary>
public class MpAuthor
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "LEK";

    [JsonPropertyName("authorType")]
    public string AuthorType { get; set; } = string.Empty;

    [JsonPropertyName("authorTypeDetail")]
    public string AuthorTypeDetail { get; set; } = string.Empty;

    [JsonPropertyName("pwz")]
    public string Pwz { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("surname")]
    public string? Surname { get; set; }

    [JsonPropertyName("clinicKsr")]
    public string ClinicKsr { get; set; } = string.Empty;

    [JsonPropertyName("clinicName")]
    public string? ClinicName { get; set; }

    [JsonPropertyName("clinicPhone")]
    public string? ClinicPhone { get; set; }

    [JsonPropertyName("clinicPostCode")]
    public string? ClinicPostCode { get; set; }

    [JsonPropertyName("clinicCity")]
    public string? ClinicCity { get; set; }

    [JsonPropertyName("clinicStreetName")]
    public string? ClinicStreetName { get; set; }

    [JsonPropertyName("clinicHouseNumber")]
    public string? ClinicHouseNumber { get; set; }

    [JsonPropertyName("clinicRegon")]
    public string? ClinicRegon { get; set; }

    [JsonPropertyName("clinicNFZ")]
    public string? ClinicNfz { get; set; }

    [JsonPropertyName("idIzbyLekarskiej")]
    public string? IdIzbyLekarskiej { get; set; }

    [JsonPropertyName("Res7")]
    public string? Res7 { get; set; }

    [JsonPropertyName("Res8")]
    public string? Res8 { get; set; }

    [JsonPropertyName("Res8Name")]
    public string? Res8Name { get; set; }

    [JsonPropertyName("assistantName")]
    public string? AssistantName { get; set; }

    [JsonPropertyName("assistantSurname")]
    public string? AssistantSurname { get; set; }

    [JsonPropertyName("assistantId")]
    public string? AssistantId { get; set; }

    [JsonPropertyName("authorMedycynaPracyId")]
    public string? AuthorMedycynaPracyId { get; set; }
}

/// <summary>
/// Adres w modelu MedycynaPracy (pacjent / pracodawca).
/// </summary>
public class MpAdres
{
    [JsonPropertyName("miejscowosc")]
    public string? Miejscowosc { get; set; }

    [JsonPropertyName("kodPocztowy")]
    public string? KodPocztowy { get; set; }

    [JsonPropertyName("ulica")]
    public string? Ulica { get; set; }

    [JsonPropertyName("nrDomu")]
    public string? NrDomu { get; set; }

    [JsonPropertyName("nrLokalu")]
    public string? NrLokalu { get; set; }
}

/// <summary>
/// Zasób pacjenta (uproszczona reprezentacja FHIR Patient wg profilu PLPatient)
/// używany przy rejestracji/aktualizacji/wyszukiwaniu/odczycie w /medycynapracy/pacjent*.
/// </summary>
public class MpPacjent
{
    /// <summary>Identyfikator pacjenta nadany przez P1 (zwracany przy wyszukiwaniu/odczycie).</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>PESEL pacjenta.</summary>
    [JsonPropertyName("identyfikator")]
    public string Identyfikator { get; set; } = string.Empty;

    [JsonPropertyName("imie")]
    public string Imie { get; set; } = string.Empty;

    [JsonPropertyName("nazwisko")]
    public string Nazwisko { get; set; } = string.Empty;

    /// <summary>"K" lub "M".</summary>
    [JsonPropertyName("plec")]
    public string Plec { get; set; } = string.Empty;

    /// <summary>Format yyyy-MM-dd.</summary>
    [JsonPropertyName("dataUrodzenia")]
    public string DataUrodzenia { get; set; } = string.Empty;

    [JsonPropertyName("adres")]
    public MpAdres? Adres { get; set; }

    [JsonPropertyName("telefon")]
    public string? Telefon { get; set; }
}

/// <summary>
/// Kryteria wyszukiwania pacjenta (POST /medycynapracy/pacjent/wyszukaj) — alternatywa do MpPacjent,
/// gdy wyszukiwanie odbywa się po samych danych identyfikacyjnych.
/// </summary>
public class MpKryteriaWyszukiwaniaPacjenta
{
    [JsonPropertyName("identyfikator")]
    public string? Identyfikator { get; set; }

    [JsonPropertyName("imie")]
    public string? Imie { get; set; }

    [JsonPropertyName("nazwisko")]
    public string? Nazwisko { get; set; }

    [JsonPropertyName("dataUrodzenia")]
    public string? DataUrodzenia { get; set; }

    [JsonPropertyName("plec")]
    public string? Plec { get; set; }
}

/// <summary>Identyfikator w zasobie FHIR Patient (system + value, np. system PESEL).</summary>
public class MpFhirIdentifier
{
    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

/// <summary>Imię/nazwisko w zasobie FHIR Patient.</summary>
public class MpFhirName
{
    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("given")]
    public List<string>? Given { get; set; }
}

/// <summary>Zasób FHIR Patient zwracany przez /medycynapracy/pacjent/wyszukaj.</summary>
public class MpFhirPatientResource
{
    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("identifier")]
    public List<MpFhirIdentifier>? Identifier { get; set; }

    [JsonPropertyName("name")]
    public List<MpFhirName>? Name { get; set; }

    /// <summary>"male" lub "female".</summary>
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    /// <summary>Format yyyy-MM-dd.</summary>
    [JsonPropertyName("birthDate")]
    public string? BirthDate { get; set; }

    [JsonIgnore]
    public string? Pesel => Identifier?.FirstOrDefault()?.Value;

    [JsonIgnore]
    public string? Imie => Name?.FirstOrDefault()?.Given?.FirstOrDefault();

    [JsonIgnore]
    public string? Nazwisko => Name?.FirstOrDefault()?.Family;

    /// <summary>Płeć w formacie skróconym "K"/"M" u\u017cywanym przez pozosta\u0142e endpointy MedycynaPracy.</summary>
    [JsonIgnore]
    public string? Plec => Gender switch
    {
        "female" => "K",
        "male" => "M",
        _ => null
    };
}

/// <summary>Wpis wyniku wyszukiwania pacjenta w bundle'u FHIR.</summary>
public class MpPacjentSearchEntry
{
    [JsonPropertyName("fullUrl")]
    public string? FullUrl { get; set; }

    [JsonPropertyName("resource")]
    public MpFhirPatientResource? Resource { get; set; }
}

/// <summary>Kontener wyników wyszukiwania \u2014 bundle FHIR zwracany przez /medycynapracy/pacjent/wyszukaj.</summary>
public class MpPacjentSearchBundle
{
    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("entry")]
    public List<MpPacjentSearchEntry> Entry { get; set; } = [];
}

/// <summary>Odpowiedź operacji rejestracji pacjenta.</summary>
public class MpPacjentCreatedResult
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Koperta odpowiedzi Middleware dla operacji na pacjencie
/// (zawiera pole "pacjent", które w zależności od operacji jest bundle'em, zasobem lub null).
/// </summary>
public class MiddlewarePacjentResponse<TPacjent>
{
    [JsonPropertyName("isError")]
    public bool IsError { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// API zwraca komunikaty walidacyjne w polu "messages" w niespójnym formacie:
    /// czasem jako string (HTML-owana lista &lt;div&gt;), czasem jako obiekt
    /// { "severity": "error", "diagnostics": "...", "location": null }.
    /// Odczytujemy jako surowy JsonElement i wyciągamy czytelny tekst w <see cref="MessagesText"/>.
    /// </summary>
    [JsonPropertyName("messages")]
    public JsonElement Messages { get; set; }

    [JsonIgnore]
    public string? MessagesText => MiddlewareMessageParser.ExtractText(Messages);

    [JsonPropertyName("pacjent")]
    public TPacjent? Pacjent { get; set; }
}
