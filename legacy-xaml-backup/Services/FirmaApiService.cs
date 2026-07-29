using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;

namespace ASMED.WPF.Services
{
    public class FirmaApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static FirmaApiService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ASMED-WPF-App/1.0");
        }

        /// <summary>
        /// Pobiera dane firmy z Białej Listy VAT (Ministerstwo Finansów)
        /// </summary>
        public async Task<FirmaDaneDto> PobierzDaneFirmyPoNIP(string nip)
        {
            try
            {
                // Walidacja NIP
                if (string.IsNullOrWhiteSpace(nip))
                    throw new ArgumentException("NIP nie może być pusty");

                // Usuń spacje i myślniki
                nip = nip.Replace(" ", "").Replace("-", "");

                if (nip.Length != 10)
                    throw new ArgumentException("NIP musi mieć 10 cyfr");

                // API Białej Listy VAT
                var url = $"https://wl-api.mf.gov.pl/api/search/nip/{nip}?date={DateTime.Now:yyyy-MM-dd}";

                // System.Diagnostics.Debug.WriteLine($"[FirmaAPI] Wywołanie: {url}");

                var response = await _httpClient.GetAsync(url);

                // System.Diagnostics.Debug.WriteLine($"[FirmaAPI] Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return null;

                    throw new Exception($"API zwróciło błąd: {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();

                using var jsonDoc = JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                // Sprawdź czy odpowiedź zawiera dane
                if (!root.TryGetProperty("result", out var result))
                {
                    return null;
                }

                // result może być obiektem lub tablicą
                JsonElement subject;

                if (result.ValueKind == JsonValueKind.Object)
                {
                    if (!result.TryGetProperty("subject", out subject))
                    {
                        return null;
                    }
                }
                else if (result.ValueKind == JsonValueKind.Array)
                {
                    var subjects = result.EnumerateArray().ToList();
                    if (!subjects.Any())
                    {
                        return null;
                    }
                    subject = subjects.First();
                }
                else
                {
                    return null;
                }

                // Parsowanie danych firmy
                var nazwa = SafeGetString(subject, "name");
                var statusVat = SafeGetString(subject, "statusVat");
                var regon = SafeGetString(subject, "regon");

                // System.Diagnostics.Debug.WriteLine($"[FirmaAPI] Pobrano: {nazwa}, NIP: {nip}, REGON: {regon}");

                // Parsowanie adresu
                string? ulica = null, numerDomu = null, numerLokalu = null, kodPocztowy = null, miejscowosc = null;

                // residenceAddress może być stringiem "ULICA NUMER, KOD MIASTO"
                if (subject.TryGetProperty("residenceAddress", out var resAddress) && resAddress.ValueKind == JsonValueKind.String)
                {
                    var adresString = resAddress.GetString();
                    // System.Diagnostics.Debug.WriteLine($"[FirmaAPI] Parsowanie adresu z stringa: {adresString}");

                    var parsed = ParseAdresString(adresString);
                    ulica = parsed.Ulica;
                    numerDomu = parsed.NumerDomu;
                    numerLokalu = parsed.NumerLokalu;
                    kodPocztowy = parsed.KodPocztowy;
                    miejscowosc = parsed.Miejscowosc;
                }
                // workingAddress może też być stringiem
                else if (subject.TryGetProperty("workingAddress", out var workAddress) && workAddress.ValueKind == JsonValueKind.String)
                {
                    var adresString = workAddress.GetString();
                    // System.Diagnostics.Debug.WriteLine($"[FirmaAPI] Parsowanie adresu z workingAddress: {adresString}");

                    var parsed = ParseAdresString(adresString);
                    ulica = parsed.Ulica;
                    numerDomu = parsed.NumerDomu;
                    numerLokalu = parsed.NumerLokalu;
                    kodPocztowy = parsed.KodPocztowy;
                    miejscowosc = parsed.Miejscowosc;
                }

                // System.Diagnostics.Debug.WriteLine($"[FirmaAPI] Wynikowy adres: {ulica} {numerDomu}/{numerLokalu}, {kodPocztowy} {miejscowosc}");

                return new FirmaDaneDto
                {
                    Nazwa = nazwa,
                    NIP = nip,
                    REGON = regon,
                    Ulica = ulica,
                    NumerDomu = numerDomu,
                    NumerLokalu = numerLokalu,
                    KodPocztowy = kodPocztowy,
                    Miejscowosc = miejscowosc,
                    Status = statusVat == "Czynny" ? "Aktywna" : "Nieaktywna"
                };
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[FirmaAPI] Błąd: {ex.Message}");
                throw new Exception($"Błąd pobierania danych: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parsuje string adresu w formacie "ULICA NUMER, KOD MIASTO" lub "ULICA NUMER/LOKAL, KOD MIASTO"
        /// </summary>
        private (string Ulica, string NumerDomu, string NumerLokalu, string KodPocztowy, string Miejscowosc) ParseAdresString(string adres)
        {
            if (string.IsNullOrWhiteSpace(adres))
                return (null, null, null, null, null);

            try
            {
                // Format: "LITEWSKA 12, 00-581 WARSZAWA"
                // lub: "LITEWSKA 12/3, 00-581 WARSZAWA"

                var parts = adres.Split(',');
                if (parts.Length < 2)
                    return (null, null, null, null, null);

                // Pierwsza część: ulica i numer
                var ulicaNumer = parts[0].Trim();

                // Druga część: kod i miasto
                var kodMiasto = parts[1].Trim();

                // Parsuj ulicę i numer
                // Znajdź ostatnią cyfrę - to będzie początek numeru
                var match = Regex.Match(ulicaNumer, @"^(.+?)\s+(\d+[A-Za-z]?)(/(\d+[A-Za-z]?))?$");

                string? ulica = null, numerDomu = null, numerLokalu = null;

                if (match.Success)
                {
                    ulica = match.Groups[1].Value.Trim();
                    numerDomu = match.Groups[2].Value;
                    if (match.Groups[4].Success)
                    {
                        numerLokalu = match.Groups[4].Value;
                    }
                }
                else
                {
                    // Fallback: wszystko przed ostatnią spacją to ulica
                    var lastSpace = ulicaNumer.LastIndexOf(' ');
                    if (lastSpace > 0)
                    {
                        ulica = ulicaNumer.Substring(0, lastSpace).Trim();
                        var numer = ulicaNumer.Substring(lastSpace + 1).Trim();

                        // Sprawdź czy numer zawiera /
                        if (numer.Contains('/'))
                        {
                            var numerParts = numer.Split('/');
                            numerDomu = numerParts[0];
                            numerLokalu = numerParts.Length > 1 ? numerParts[1] : null;
                        }
                        else
                        {
                            numerDomu = numer;
                        }
                    }
                    else
                    {
                        ulica = ulicaNumer;
                    }
                }

                // Parsuj kod pocztowy i miasto
                // Format: "00-581 WARSZAWA"
                var kodMatch = Regex.Match(kodMiasto, @"^(\d{2}-\d{3})\s+(.+)$");

                string kodPocztowy = null, miejscowosc = null;

                if (kodMatch.Success)
                {
                    kodPocztowy = kodMatch.Groups[1].Value;
                    miejscowosc = kodMatch.Groups[2].Value.Trim();
                }
                else
                {
                    // Fallback: pierwsze 6 znaków to kod (jeśli pasuje do formatu)
                    if (kodMiasto.Length >= 6 && kodMiasto[2] == '-')
                    {
                        kodPocztowy = kodMiasto.Substring(0, 6);
                        miejscowosc = kodMiasto.Substring(6).Trim();
                    }
                    else
                    {
                        miejscowosc = kodMiasto;
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"[FirmaAPI] Sparsowano: ulica={ulica}, dom={numerDomu}, lokal={numerLokalu}, kod={kodPocztowy}, miasto={miejscowosc}");

                return (ulica, numerDomu, numerLokalu, kodPocztowy, miejscowosc);
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[FirmaAPI] Błąd parsowania adresu: {ex.Message}");
                return (null, null, null, null, null);
            }
        }

        /// <summary>
        /// Bezpiecznie pobiera wartość string z JsonElement
        /// </summary>
        private string SafeGetString(JsonElement element, params string[] propertyNames)
        {
            foreach (var propName in propertyNames)
            {
                if (element.TryGetProperty(propName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        var value = prop.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                    else if (prop.ValueKind == JsonValueKind.Number)
                    {
                        return prop.ToString();
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// DTO zawierający dane firmy pobrane z API
    /// </summary>
    public class FirmaDaneDto
    {
        public string? Nazwa { get; set; }
        public string? NIP { get; set; }
        public string? REGON { get; set; }
        public string? Ulica { get; set; }
        public string? NumerDomu { get; set; }
        public string? NumerLokalu { get; set; }
        public string? KodPocztowy { get; set; }
        public string? Miejscowosc { get; set; }
        public DateTime? DataRozpoczecia { get; set; }
        public DateTime? DataZakonczenia { get; set; }
        public string? Status { get; set; }

        /// <summary>
        /// Pełny adres w formacie: ul. Nazwa 12/3, 00-000 Miasto
        /// </summary>
        public string PelnyAdres
        {
            get
            {
                var adres = string.Empty;

                if (!string.IsNullOrWhiteSpace(Ulica))
                    adres = $"ul. {Ulica}";

                if (!string.IsNullOrWhiteSpace(NumerDomu))
                    adres += $" {NumerDomu}";

                if (!string.IsNullOrWhiteSpace(NumerLokalu))
                    adres += $"/{NumerLokalu}";

                if (!string.IsNullOrWhiteSpace(KodPocztowy) || !string.IsNullOrWhiteSpace(Miejscowosc))
                {
                    if (!string.IsNullOrWhiteSpace(adres))
                        adres += ", ";

                    adres += $"{KodPocztowy} {Miejscowosc}".Trim();
                }

                return adres;
            }
        }

        /// <summary>
        /// Czy firma jest aktywna
        /// </summary>
        public bool CzyAktywna => Status?.ToLower() == "aktywna" || Status?.ToLower() == "aktywny" || Status?.ToLower() == "czynny";
    }
}
