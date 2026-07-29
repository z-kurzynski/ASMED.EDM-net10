using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace ASMED.WPF.Services
{
    /// <summary>
    /// Serwis pobierania danych firm z GUS REGON API (BIR 1.1).
    /// Zawiera WSZYSTKIE podmioty zarejestrowane w Polsce (CEIDG, KRS, inne).
    /// 
    /// ═══════════════════════════════════════════════════════════════
    /// JAK URUCHOMIĆ Z PRAWDZIWYMI DANYMI:
    /// 
    /// 1. Wejdź na: https://api.stat.gov.pl/Home/RegonApi
    /// 2. Kliknij "Wnioskuj o dostęp" → wypełnij formularz (darmowe)
    /// 3. Klucz produkcyjny przyjdzie mailem (zwykle kilka minut)
    /// 4. Zmień poniżej:
    ///      UseTestServer = false
    ///      ProductionApiKey = "twój_klucz_z_maila"
    /// ═══════════════════════════════════════════════════════════════
    /// </summary>
    public class RegonApiService
    {
        private static readonly HttpClient _http = new HttpClient();

        // ══════════════════════════════════════════════════
        //  KONFIGURACJA — zmień na false + wpisz klucz
        // ══════════════════════════════════════════════════
        private const bool UseTestServer = false;  // ← false = produkcja (prawdziwe dane)

        private const string ProductionApiKey = "be5ba01f95c4440784bc"; // ← WPISZ klucz z https://api.stat.gov.pl/Home/RegonApi

        // ── Stałe URL i klucz testowy (nie zmieniaj) ──
        private const string TestUrl = "https://wyszukiwarkaregontest.stat.gov.pl/wsBIR/UslugaBIRzwornik.svc";
        private const string ProdUrl = "https://wyszukiwarkaregon.stat.gov.pl/wsBIR/UslugaBIRzewnPubl.svc"; // ✅ Poprawiony URL z maila GUS
        private const string TestApiKey = "abcde12345abcde12345";

        private static string ServiceUrl => UseTestServer ? TestUrl : ProdUrl;
        private static string ApiKey => UseTestServer ? TestApiKey : ProductionApiKey;

        // ── Namespace'y SOAP ──
        private static readonly XNamespace Soap = "http://www.w3.org/2003/05/soap-envelope";
        private static readonly XNamespace Wsa = "http://www.w3.org/2005/08/addressing";

        // ✅ Body namespace jest TAKI SAM dla test i produkcja
        private static readonly XNamespace Ns = "http://CIS/BIR/PUBL/2014/07";
        private static readonly XNamespace Dat = "http://CIS/BIR/PUBL/2014/07/DataContract";

        /// <summary>
        /// Pobiera dane firmy po NIP z rejestru REGON (GUS).
        /// Zwraca null jeśli nie znaleziono lub błąd.
        /// </summary>
        public async Task<FirmaDaneDto?> PobierzDaneFirmyPoNIP(string nip)
        {
            string? sid = null;
            //MessageBox.Show("UWAGA: Ten serwis korzysta z GUS REGON API, które może być wolne i niestabilne. Pobieranie danych może zająć kilka sekund. Proszę o cierpliwość.", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
            try
            {
                nip = nip?.Replace(" ", "").Replace("-", "") ?? "";
                if (nip.Length != 10)
                    throw new ArgumentException("NIP musi mieć 10 cyfr");

                // Sprawdź czy klucz produkcyjny jest ustawiony
                if (!UseTestServer && string.IsNullOrWhiteSpace(ProductionApiKey))
                {
                    // System.Diagnostics.Debug.WriteLine("[REGON] Brak klucza produkcyjnego! Zarejestruj się na https://api.stat.gov.pl/Home/RegonApi");
                    return null;
                }

                // System.Diagnostics.Debug.WriteLine($"[REGON] Serwer: {(UseTestServer ? "TEST" : "PRODUKCJA")}, URL: {ServiceUrl}");

                // 1. Zaloguj — pobierz SID
                sid = await Zaloguj();
                if (string.IsNullOrEmpty(sid))
                    throw new Exception("Nie udało się zalogować do GUS REGON API");

                // System.Diagnostics.Debug.WriteLine($"[REGON] Zalogowano, SID={sid.Substring(0, Math.Min(8, sid.Length))}...");

                // 2. Szukaj podmiotu po NIP
                var (podstawowe, typ, regon14) = await DaneSzukajPodmioty(sid, nip);
                if (podstawowe == null)
                    return null;

                // 3. Pobierz pełny raport (dla nazwy skróconej)
                string nazwaSkrocona = null;
                if (!string.IsNullOrEmpty(regon14) && !string.IsNullOrEmpty(typ))
                {
                    nazwaSkrocona = await PobierzNazweSkrocona(sid, regon14, typ);
                }

                // Uzupełnij DTO nazwą skróconą
                if (!string.IsNullOrEmpty(nazwaSkrocona) && nazwaSkrocona != podstawowe.Nazwa)
                {
                    podstawowe.Nazwa = $"{podstawowe.Nazwa} ({nazwaSkrocona})";
                }

                return podstawowe;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[REGON] Błąd: {ex.Message}");
                return null;
            }
            finally
            {
                // 4. Wyloguj
                if (!string.IsNullOrEmpty(sid))
                {
                    try { await Wyloguj(sid); } catch { /* ignoruj */ }
                }
            }
        }

        // ══════════════════════════════════════════════════════
        //  SOAP: Zaloguj
        // ══════════════════════════════════════════════════════
        private async Task<string?> Zaloguj()
        {
            var actionUrl = UseTestServer 
                ? "http://CIS/BIR/PUBL/2014/07/IUslugaBIR/Zaloguj"
                : "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/Zaloguj";

            var body = new XElement(Soap + "Envelope",
                new XElement(Soap + "Header",
                    new XElement(Wsa + "Action", actionUrl),
                    new XElement(Wsa + "To", ServiceUrl)),
                new XElement(Soap + "Body",
                    new XElement(Ns + "Zaloguj",
                        new XElement(Ns + "pKluczUzytkownika", ApiKey))));

            var responseXml = await WyslijSoap(body, null);
            var zalogujResult = responseXml?.Descendants(Ns + "ZalogujResult").FirstOrDefault();

            // System.Diagnostics.Debug.WriteLine($"[REGON] Zaloguj result: {zalogujResult?.Value ?? "null"}");

            return zalogujResult?.Value;
        }

        // ══════════════════════════════════════════════════════
        //  SOAP: Wyloguj
        // ══════════════════════════════════════════════════════
        private async Task Wyloguj(string sid)
        {
            var actionUrl = UseTestServer
                ? "http://CIS/BIR/PUBL/2014/07/IUslugaBIR/Wyloguj"
                : "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/Wyloguj";

            var body = new XElement(Soap + "Envelope",
                new XElement(Soap + "Header",
                    new XElement(Wsa + "Action", actionUrl),
                    new XElement(Wsa + "To", ServiceUrl)),
                new XElement(Soap + "Body",
                    new XElement(Ns + "Wyloguj",
                        new XElement(Ns + "pIdentyfikatorSesji", sid))));

            await WyslijSoap(body, sid);
        }

        // ══════════════════════════════════════════════════════
        //  SOAP: DaneSzukajPodmioty
        // ══════════════════════════════════════════════════════
        private async Task<(FirmaDaneDto? dto, string? typ, string? regon14)> DaneSzukajPodmioty(string sid, string nip)
        {
            var actionUrl = UseTestServer
                ? "http://CIS/BIR/PUBL/2014/07/IUslugaBIR/DaneSzukajPodmioty"
                : "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/DaneSzukajPodmioty";

            var body = new XElement(Soap + "Envelope",
                new XElement(Soap + "Header",
                    new XElement(Wsa + "Action", actionUrl),
                    new XElement(Wsa + "To", ServiceUrl)),
                new XElement(Soap + "Body",
                    new XElement(Ns + "DaneSzukajPodmioty",
                        new XElement(Ns + "pParametryWyszukiwania",
                            new XElement(Dat + "Nip", nip)))));

            var responseXml = await WyslijSoap(body, sid);
            var resultNode = responseXml?.Descendants(Ns + "DaneSzukajPodmiotyResult").FirstOrDefault();
            var cdataValue = resultNode?.Value;

            // System.Diagnostics.Debug.WriteLine($"[REGON] DaneSzukajPodmioty - resultNode: {(resultNode == null ? "NULL" : "OK")}");
            // System.Diagnostics.Debug.WriteLine($"[REGON] DaneSzukajPodmioty - CDATA length: {cdataValue?.Length ?? 0}");

            if (!string.IsNullOrWhiteSpace(cdataValue))
            {
                // System.Diagnostics.Debug.WriteLine($"[REGON] DaneSzukajPodmioty - CDATA content:\n{cdataValue}");
            }

            if (string.IsNullOrWhiteSpace(cdataValue))
            {
                // System.Diagnostics.Debug.WriteLine($"[REGON] ❌ Brak danych w CDATA (pusty wynik lub błąd GUS)");
                return (null, null, null);
            }

            // Parsuj XML z CDATA
            var dataXml = XDocument.Parse(cdataValue);
            var dane = dataXml.Descendants("dane").FirstOrDefault();
            if (dane == null)
                return (null, null, null);

            var nazwa = dane.Element("Nazwa")?.Value;
            var regon = dane.Element("Regon")?.Value;
            var miejscowosc = dane.Element("Miejscowosc")?.Value;
            var kodPocztowy = dane.Element("KodPocztowy")?.Value;
            var ulica = dane.Element("Ulica")?.Value;
            var nrDomu = dane.Element("NrNieruchomosci")?.Value;
            var nrLokalu = dane.Element("NrLokalu")?.Value;
            var typ = dane.Element("Typ")?.Value; // F=fizyczna, P=prawna
            var statusNip = dane.Element("StatusNip")?.Value;

            // System.Diagnostics.Debug.WriteLine($"[REGON] Znaleziono: {nazwa}, NIP={nip}, REGON={regon}, Typ={typ}");

            var dto = new FirmaDaneDto
            {
                Nazwa = nazwa,
                NIP = nip,
                REGON = regon,
                Ulica = ulica,
                NumerDomu = nrDomu,
                NumerLokalu = nrLokalu,
                KodPocztowy = kodPocztowy,
                Miejscowosc = miejscowosc,
                Status = string.IsNullOrEmpty(statusNip) || statusNip == "" ? "Aktywna" : "Nieaktywna"
            };

            return (dto, typ, regon);
        }

        // ══════════════════════════════════════════════════════
        //  SOAP: DanePobierzPelnyRaport (nazwa skrócona)
        // ══════════════════════════════════════════════════════
        private async Task<string?> PobierzNazweSkrocona(string sid, string regon, string typ)
        {
            // Wybierz raport w zależności od typu podmiotu
            string nazwaRaportu;
            string poleNazwySkroconej;

            if (typ == "F") // osoba fizyczna (CEIDG)
            {
                nazwaRaportu = "BIR11OsFizycznaDzworker";
                poleNazwySkroconej = "fiz_nazwaSkrocona";
            }
            else if (typ == "P") // osoba prawna (KRS)
            {
                nazwaRaportu = "BIR11OsPrawna";
                poleNazwySkroconej = "praw_nazwaSkrocona";
            }
            else
            {
                return null;
            }

            try
            {
                var actionUrl = UseTestServer
                    ? "http://CIS/BIR/PUBL/2014/07/IUslugaBIR/DanePobierzPelnyRaport"
                    : "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/DanePobierzPelnyRaport";

                var body = new XElement(Soap + "Envelope",
                    new XElement(Soap + "Header",
                        new XElement(Wsa + "Action", actionUrl),
                        new XElement(Wsa + "To", ServiceUrl)),
                    new XElement(Soap + "Body",
                        new XElement(Ns + "DanePobierzPelnyRaport",
                            new XElement(Ns + "pRegon", regon),
                            new XElement(Ns + "pNazwaRaportu", nazwaRaportu))));

                var responseXml = await WyslijSoap(body, sid);
                var resultNode = responseXml?.Descendants(Ns + "DanePobierzPelnyRaportResult").FirstOrDefault();
                var cdataValue = resultNode?.Value;

                if (string.IsNullOrWhiteSpace(cdataValue))
                    return null;

                var dataXml = XDocument.Parse(cdataValue);
                var dane = dataXml.Descendants("dane").FirstOrDefault();
                var nazwaSkr = dane?.Element(poleNazwySkroconej)?.Value;

                // System.Diagnostics.Debug.WriteLine($"[REGON] Nazwa skrócona: {nazwaSkr}");
                return nazwaSkr;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[REGON] Błąd pobierania raportu: {ex.Message}");
                return null;
            }
        }

        // ══════════════════════════════════════════════════════
        //  Helper: Wyślij SOAP i zwróć sparsowany XML
        // ══════════════════════════════════════════════════════
        private async Task<XDocument?> WyslijSoap(XElement envelope, string? sid)
        {
            var soapString = envelope.ToString();
            // System.Diagnostics.Debug.WriteLine($"[REGON] ═══════════════════════════════════════");
            // System.Diagnostics.Debug.WriteLine($"[REGON] ═══ SOAP REQUEST ═══");
            // System.Diagnostics.Debug.WriteLine(soapString);

            // ✅ SOAP 1.2: application/soap+xml; charset=utf-8
            var content = new StringContent(soapString, Encoding.UTF8);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/soap+xml")
            {
                CharSet = "utf-8"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ServiceUrl);
            request.Content = content;

            // ✅ Wyciągnij Action z envelope i dodaj do Content-Type
            var action = envelope.Descendants(Wsa + "Action").FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(action))
            {
                // SOAP 1.2: action w Content-Type zamiast SOAPAction header
                content.Headers.ContentType.Parameters.Add(
                    new System.Net.Http.Headers.NameValueHeaderValue("action", $"\"{action}\""));
                // System.Diagnostics.Debug.WriteLine($"[REGON] Content-Type action={action}");
            }

            if (!string.IsNullOrEmpty(sid))
            {
                request.Headers.TryAddWithoutValidation("sid", sid);
                // System.Diagnostics.Debug.WriteLine($"[REGON] Header: sid={sid}");
            }

            // System.Diagnostics.Debug.WriteLine($"[REGON] Content-Type: {content.Headers.ContentType}");

            var response = await _http.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            // System.Diagnostics.Debug.WriteLine($"[REGON] ═══ SOAP RESPONSE ═══");
            // System.Diagnostics.Debug.WriteLine($"[REGON] HTTP Status: {response.StatusCode}");
            // System.Diagnostics.Debug.WriteLine($"[REGON] Content-Type: {response.Content.Headers.ContentType}");
            // System.Diagnostics.Debug.WriteLine(responseString.Substring(0, Math.Min(1000, responseString.Length)));
            // System.Diagnostics.Debug.WriteLine($"[REGON] ═══════════════════════════════════════");

            if (!response.IsSuccessStatusCode)
            {
                // System.Diagnostics.Debug.WriteLine($"[REGON] ❌ HTTP ERROR {response.StatusCode}");
                return null;
            }

            // ✅ MTOM/XOP Support: wyciągnij SOAP z multipart/related
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "";
            if (contentType.Contains("multipart/related"))
            {
                // System.Diagnostics.Debug.WriteLine($"[REGON] Wykryto MTOM/XOP (multipart/related)");

                // Wyciągnij SOAP envelope z MIME parts
                var soapPart = ExtractSoapFromMultipart(responseString);
                if (!string.IsNullOrEmpty(soapPart))
                {
                    responseString = soapPart;
                    // System.Diagnostics.Debug.WriteLine($"[REGON] Wyekstrahowano SOAP envelope: {responseString.Substring(0, Math.Min(300, responseString.Length))}...");
                }
            }

            try
            {
                return XDocument.Parse(responseString);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[REGON] ❌ XML Parse Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Wyciąga SOAP envelope z MTOM/XOP multipart response
        /// </summary>
        private string? ExtractSoapFromMultipart(string multipartResponse)
        {
            try
            {
                // MTOM format:
                // --boundary
                // Content-ID: ...
                // Content-Type: application/xop+xml...
                // 
                // <s:Envelope>...</s:Envelope>
                // --boundary--

                // Znajdź pozycję <s:Envelope lub <Envelope
                var envelopeStart = multipartResponse.IndexOf("<s:Envelope");
                if (envelopeStart == -1)
                    envelopeStart = multipartResponse.IndexOf("<Envelope");

                if (envelopeStart == -1)
                {
                    // System.Diagnostics.Debug.WriteLine("[REGON] ❌ Nie znaleziono <Envelope> w multipart");
                    return null;
                }

                // Znajdź koniec </s:Envelope> lub </Envelope>
                var envelopeEnd = multipartResponse.IndexOf("</s:Envelope>", envelopeStart);
                if (envelopeEnd == -1)
                    envelopeEnd = multipartResponse.IndexOf("</Envelope>", envelopeStart);

                if (envelopeEnd == -1)
                {
                    // System.Diagnostics.Debug.WriteLine("[REGON] ❌ Nie znaleziono </Envelope> w multipart");
                    return null;
                }

                // Wytnij SOAP envelope (+długość </s:Envelope> = 13)
                var soapXml = multipartResponse.Substring(envelopeStart, envelopeEnd - envelopeStart + 13);

                return soapXml.Trim();
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[REGON] ❌ Błąd ekstrahowania SOAP z multipart: {ex.Message}");
                return null;
            }
        }
    }
}
