using ASMED.EDM.P1.Configuration;
using ASMED.EDM.P1.Middleware;
using ASMED.EDM.P1.Middleware.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ASMED.EDM.P1.Tests;

/// <summary>
/// Testy jednostkowe klienta Middleware MedycynaPracy (<see cref="MiddlewareMedycynaPracyClient"/>).
/// Dla każdej z 9 operacji /medycynapracy/* (zweryfikowanych względem kolekcji Postman
/// "Medycyna pracy przykłady dla klienta" oraz dokumentacji ReDoc) sprawdzamy:
///  - wywoływany URL (relatywny) i metodę HTTP (POST/PUT),
///  - obecność wymaganych pól formularza (author/idPodmiotu/doctor_pass/id/pacjent/orzeczenie/...),
///  - poprawne parsowanie odpowiedzi (w tym pola "messages" w obu formatach: string i obiekt).
/// </summary>
public class MiddlewareMedycynaPracyClientTests
{
    /// <summary>Fałszywy IMiddlewareAuthTokenProvider zwracający stały token bez wywołań sieciowych.</summary>
    private sealed class FakeTokenProvider : IMiddlewareAuthTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult("fake-jwt-token");
        public void Invalidate() { }
    }

    /// <summary>
    /// Fałszywy HttpMessageHandler, który przechwytuje ostatnie żądanie (URL, metodę, treść formularza)
    /// i zwraca zadeklarowaną odpowiedź JSON.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }
        public string? LastRequestUri { get; private set; }
        public Dictionary<string, string>? LastFormFields { get; private set; }
        public string ResponseJson { get; set; } = "{}";
        public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastRequestUri = request.RequestUri?.ToString();

            if (request.Content is not null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                LastFormFields = body
                    .Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(pair => pair.Split('=', 2))
                    .ToDictionary(
                        parts => Uri.UnescapeDataString(parts[0]),
                        parts => parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty);
            }

            return new HttpResponseMessage(ResponseStatusCode)
            {
                Content = new StringContent(ResponseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    private static (MiddlewareMedycynaPracyClient Client, CapturingHandler Handler) CreateClient()
    {
        var handler = new CapturingHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://telsa.e-recepty.online/medyczneapi.testcered.pl/") };

        var settings = Options.Create(new P1Settings
        {
            IdPodmiotu = "ID-PODMIOTU-TEST",
            DoctorPwz = "1234567",
            DoctorName = "Jan",
            DoctorSurname = "Testowy",
            RpwdlBookNumber = "000000012345",
            DoctorCertificatePassword = "cert-pass-123"
        });

        var client = new MiddlewareMedycynaPracyClient(httpClient, new FakeTokenProvider(), settings, NullLogger<MiddlewareMedycynaPracyClient>.Instance);
        return (client, handler);
    }

    // ---------------------------------------------------------------------
    // 1. POST /medycynapracy/pacjent/wyszukaj — Wyszukaj pacjenta
    // ---------------------------------------------------------------------
    [Fact]
    public async Task WyszukajPacjentaAsync_WysylaPoprawnyUrlIPola_OrazParsujeBundleFhir()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """
            {
              "isError": false,
              "messages": "",
              "pacjent": {
                "resourceType": "Bundle",
                "total": 1,
                "entry": [
                  { "resource": { "resourceType": "Patient", "id": "2955", "gender": "female", "birthDate": "1981-10-26",
                    "identifier": [ { "system": "PESEL", "value": "70032816894" } ],
                    "name": [ { "family": "Kasina", "given": [ "Anna" ] } ] } }
                ]
              }
            }
            """;

        var kryteria = new MpKryteriaWyszukiwaniaPacjenta { Identyfikator = "70032816894", Imie = "Anna", Nazwisko = "Kasina" };
        var wynik = await client.WyszukajPacjentaAsync(kryteria);

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.EndsWith("medycynapracy/pacjent/wyszukaj", handler.LastRequestUri);
        Assert.True(handler.LastFormFields!.ContainsKey("author"));
        Assert.True(handler.LastFormFields!.ContainsKey("pacjent"));

        Assert.False(wynik.IsError);
        Assert.Equal(1, wynik.Pacjent!.Total);
        var resource = wynik.Pacjent.Entry[0].Resource!;
        Assert.Equal("2955", resource.Id);
        Assert.Equal("70032816894", resource.Pesel);
        Assert.Equal("Anna", resource.Imie);
        Assert.Equal("Kasina", resource.Nazwisko);
        Assert.Equal("K", resource.Plec);
    }

    // ---------------------------------------------------------------------
    // 2. POST /medycynapracy/pacjent — Zarejestruj pacjenta
    // ---------------------------------------------------------------------
    [Fact]
    public async Task ZarejestrujPacjentaAsync_WysylaPoprawnyUrlIPola_OrazZwracaId()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """{ "isError": false, "messages": "", "pacjent": { "id": "2955" } }""";

        var pacjent = new MpPacjent
        {
            Identyfikator = "70032816894",
            Imie = "Anna",
            Nazwisko = "Kasina",
            Plec = "K",
            DataUrodzenia = "1981-10-26",
            Adres = new MpAdres { Miejscowosc = "Szczecin", KodPocztowy = "70-570", Ulica = "Misjonarska", NrDomu = "116" }
        };
        var wynik = await client.ZarejestrujPacjentaAsync(pacjent);

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.EndsWith("medycynapracy/pacjent", handler.LastRequestUri);
        Assert.True(handler.LastFormFields!.ContainsKey("author"));
        Assert.True(handler.LastFormFields!.ContainsKey("pacjent"));

        Assert.False(wynik.IsError);
        Assert.Equal("2955", wynik.Pacjent!.Id);
    }

    // ---------------------------------------------------------------------
    // 3. POST /medycynapracy/pacjent/odczyt — Odczytaj pacjenta
    // ---------------------------------------------------------------------
    [Fact]
    public async Task OdczytajPacjentaAsync_WysylaPoprawnyUrlIId_OrazParsujeDaneOsobowe()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """
            { "isError": false, "messages": "", "pacjent": {
              "resourceType": "Patient", "id": "2955",
              "identifier": [ { "system": "urn:oid:2.16.840.1.113883.4.29", "value": "70032816894" } ],
              "name": [ { "family": "Kasina", "given": [ "Anna" ] } ],
              "gender": "female", "birthDate": "1981-10-26" } }
            """;

        var wynik = await client.OdczytajPacjentaAsync("2955");

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.EndsWith("medycynapracy/pacjent/odczyt", handler.LastRequestUri);
        Assert.Equal("2955", handler.LastFormFields!["id"]);
        Assert.True(handler.LastFormFields!.ContainsKey("author"));

        Assert.False(wynik.IsError);
        Assert.Equal("Anna", wynik.Pacjent!.Imie);
        Assert.Equal("Kasina", wynik.Pacjent!.Nazwisko);
    }

    // ---------------------------------------------------------------------
    // 4. PUT /medycynapracy/pacjent — Aktualizuj pacjenta
    // ---------------------------------------------------------------------
    [Fact]
    public async Task AktualizujPacjentaAsync_UzywaMetodyPut_IWysylaIdOrazDanePacjenta()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """{ "isError": false, "messages": "", "pacjent": { "id": "2955" } }""";

        var pacjent = new MpPacjent { Identyfikator = "70032816894", Imie = "Anna", Nazwisko = "Kasina", Plec = "K", DataUrodzenia = "1981-10-26" };
        var wynik = await client.AktualizujPacjentaAsync("2955", pacjent);

        Assert.Equal(HttpMethod.Put, handler.LastMethod);
        Assert.EndsWith("medycynapracy/pacjent", handler.LastRequestUri);
        Assert.Equal("2955", handler.LastFormFields!["id"]);
        Assert.True(handler.LastFormFields!.ContainsKey("pacjent"));

        Assert.False(wynik.IsError);
    }

    // ---------------------------------------------------------------------
    // 5. POST /medycynapracy/orzeczenie/weryfikuj — Zweryfikuj orzeczenie (bez zapisu)
    // ---------------------------------------------------------------------
    [Fact]
    public async Task WeryfikujOrzeczenieAsync_WysylaAuthorPelnyIdPodmiotuIDoctorPass()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """{ "isError": false, "messages": "" }""";

        var orzeczenie = BuildSampleOrzeczenie();
        var wynik = await client.WeryfikujOrzeczenieAsync(orzeczenie);

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.EndsWith("medycynapracy/orzeczenie/weryfikuj", handler.LastRequestUri);
        Assert.Equal("ID-PODMIOTU-TEST", handler.LastFormFields!["idPodmiotu"]);
        Assert.Equal("cert-pass-123", handler.LastFormFields!["doctor_pass"]);
        Assert.True(handler.LastFormFields!.ContainsKey("orzeczenie"));
        // author pełny (dla orzeczeń) zawiera dane lekarza, w przeciwieństwie do author_min dla pacjenta.
        Assert.Contains("\"pwz\":\"1234567\"", handler.LastFormFields!["author"]);

        Assert.False(wynik.IsError);
    }

    // ---------------------------------------------------------------------
    // 6. POST /medycynapracy/orzeczenie — Wystaw orzeczenie
    // ---------------------------------------------------------------------
    [Fact]
    public async Task WystawOrzeczenieAsync_WysylaPoprawnyUrlIWymaganePola()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """{ "isError": false, "messages": "" }""";

        var orzeczenie = BuildSampleOrzeczenie();
        var wynik = await client.WystawOrzeczenieAsync(orzeczenie);

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.EndsWith("medycynapracy/orzeczenie", handler.LastRequestUri);
        Assert.False(handler.LastRequestUri!.Contains("/weryfikuj"));
        Assert.Equal("ID-PODMIOTU-TEST", handler.LastFormFields!["idPodmiotu"]);
        Assert.Equal("cert-pass-123", handler.LastFormFields!["doctor_pass"]);

        Assert.False(wynik.IsError);
    }

    // ---------------------------------------------------------------------
    // 7. POST /medycynapracy/orzeczenie/odczyt — Odczytaj orzeczenie
    // ---------------------------------------------------------------------
    [Fact]
    public async Task OdczytajOrzeczenieAsync_WysylaIdentyfikatorRootIExtension_OrazParsujeOrzeczenie()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """
            { "isError": false, "messages": "", "orzeczenie": {
              "numerDokumentu": "14/2026", "rodzajBadania": "WSTEPNE", "rodzajZatrudnienia": "PRZYJMOWANY",
              "pacjent": { "identyfikator": "70032816894", "imie": "Anna", "nazwisko": "Kasina", "plec": "K", "dataUrodzenia": "1981-10-26" },
              "werdykt": { "symbol": "SYMBOL_21" } } }
            """;

        var kryteria = new MpOrzeczenieOdczytKryteria { IdentyfikatorRoot = "2.16.840.1.113883.3.4424.1.1.999", IdentyfikatorExtension = "14/2026" };
        var wynik = await client.OdczytajOrzeczenieAsync(kryteria);

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.EndsWith("medycynapracy/orzeczenie/odczyt", handler.LastRequestUri);
        Assert.Equal("2.16.840.1.113883.3.4424.1.1.999", handler.LastFormFields!["identyfikatorRoot"]);
        Assert.Equal("14/2026", handler.LastFormFields!["identyfikatorExtension"]);
        Assert.True(handler.LastFormFields!.ContainsKey("author"));
        Assert.True(handler.LastFormFields!.ContainsKey("idPodmiotu"));

        Assert.False(wynik.IsError);
        Assert.Equal("14/2026", wynik.Orzeczenie!.NumerDokumentu);
        Assert.Equal("SYMBOL_21", wynik.Orzeczenie.Werdykt.Symbol);
    }

    // ---------------------------------------------------------------------
    // 8. POST /medycynapracy/orzeczenie/wyszukaj — Wyszukaj orzeczenia
    // ---------------------------------------------------------------------
    [Fact]
    public async Task WyszukajOrzeczeniaAsync_WysylaKryteriaJakoJson()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """{ "isError": false, "messages": "" }""";

        var kryteria = new MpKryteriaWyszukiwaniaOrzeczenia
        {
            IdentyfikatorUslugobiorcy = "70032816894",
            StatusDokumentu = "AKTYWNY"
        };
        var wynik = await client.WyszukajOrzeczeniaAsync(kryteria);

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.EndsWith("medycynapracy/orzeczenie/wyszukaj", handler.LastRequestUri);
        Assert.True(handler.LastFormFields!.ContainsKey("kryteria"));
        var kryteriaJson = JsonSerializer.Deserialize<JsonElement>(handler.LastFormFields!["kryteria"]);
        Assert.Equal("70032816894", kryteriaJson.GetProperty("identyfikatorUslugobiorcy").GetString());
        Assert.Equal("AKTYWNY", kryteriaJson.GetProperty("statusDokumentu").GetString());

        Assert.False(wynik.IsError);
    }

    // ---------------------------------------------------------------------
    // 9. POST /medycynapracy/orzeczenie/anuluj — Anuluj orzeczenie
    // ---------------------------------------------------------------------
    [Fact]
    public async Task AnulujOrzeczenieAsync_WysylaAnulowanieJakoJson_IDoctorPass()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """{ "isError": false, "messages": "" }""";

        var anulowanie = new MpAnulowanie { NumerDokumentu = "15/2026", AnulowanyNumerDokumentu = "14/2026", AnulowanaWersja = 1 };
        var wynik = await client.AnulujOrzeczenieAsync(anulowanie);

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.EndsWith("medycynapracy/orzeczenie/anuluj", handler.LastRequestUri);
        Assert.Equal("cert-pass-123", handler.LastFormFields!["doctor_pass"]);
        var anulowanieJson = JsonSerializer.Deserialize<JsonElement>(handler.LastFormFields!["anulowanie"]);
        Assert.Equal("15/2026", anulowanieJson.GetProperty("numerDokumentu").GetString());
        Assert.Equal("14/2026", anulowanieJson.GetProperty("anulowanyNumerDokumentu").GetString());
        Assert.Equal(1, anulowanieJson.GetProperty("anulowanaWersja").GetInt32());

        Assert.False(wynik.IsError);
    }

    // ---------------------------------------------------------------------
    // Obsługa błędów — pole "messages" w obu niespójnych formatach zwracanych przez API.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task WyszukajPacjentaAsync_GdyMessagesJestStringiem_MessagesTextZwracaTenString()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """{ "isError": true, "messages": "Brak wymaganego pola: imie" }""";

        var wynik = await client.WyszukajPacjentaAsync(new MpKryteriaWyszukiwaniaPacjenta { Identyfikator = "70032816894" });

        Assert.True(wynik.IsError);
        Assert.Equal("Brak wymaganego pola: imie", wynik.MessagesText);
    }

    [Fact]
    public async Task WystawOrzeczenieAsync_GdyMessagesJestObiektem_MessagesTextWyciagaDiagnostics()
    {
        var (client, handler) = CreateClient();
        handler.ResponseJson = """
            { "isError": true, "messages": { "severity": "error", "diagnostics": "REG.WER.12222: Niezgodność danych usługobiorcy z CWUB.", "location": null } }
            """;

        var wynik = await client.WystawOrzeczenieAsync(BuildSampleOrzeczenie());

        Assert.True(wynik.IsError);
        Assert.Contains("REG.WER.12222", wynik.MessagesText);
    }

    private static MpOrzeczenie BuildSampleOrzeczenie() => new()
    {
        NumerDokumentu = "14/2026",
        NumerSkierowania = "2345678",
        DataSkierowania = "2026-01-10",
        Pacjent = new MpPacjent { Identyfikator = "70032816894", Imie = "Anna", Nazwisko = "Kasina", Plec = "K", DataUrodzenia = "1981-10-26" },
        RodzajBadania = "WSTEPNE",
        RodzajZatrudnienia = "PRZYJMOWANY",
        Stanowiska =
        [
            new MpStanowisko
            {
                Zawod = "Specjalista ds. sprzedaży",
                Pracodawca = new MpPracodawca
                {
                    Nip = "1382222234",
                    Regon9 = "190190190",
                    Nazwa = "ABC Sp. z o.o.",
                    Telefon = "221234567",
                    Adres = new MpAdres { Miejscowosc = "Warszawa", KodPocztowy = "00-100", Ulica = "Długa", NrDomu = "8" }
                }
            }
        ],
        Werdykt = new MpWerdykt { Symbol = "SYMBOL_21" },
        DataNastepnegoBadania = "2028-01-10",
        MiejsceWystawienia = "Bydgoszcz"
    };
}
