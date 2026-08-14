namespace ASMED.EDM.P1.Configuration;

/// <summary>
/// Ustawienia integracji z systemem P1 (e-zdrowie) — sekcja "P1Settings" w appsettings.json
/// </summary>
public class P1Settings
{
    public const string SectionName = "P1Settings";

    /// <summary>
    /// Tryb stub — praca bez połączenia z P1 (do czasu uzyskania certyfikatów)
    /// </summary>
    public bool UseStub { get; set; } = true;

    /// <summary>
    /// Adres bazowy API Zdarzeń Medycznych P1 (INT lub PROD)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Ścieżka do pliku PKCS#12 z certyfikatem TLS (mutual TLS)
    /// </summary>
    public string TlsCertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Hasło do pliku certyfikatu TLS (zalecane: zmienna środowiskowa, nie plik konfiguracyjny)
    /// </summary>
    public string TlsCertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// Ścieżka do pliku PKCS#12 z certyfikatem WSS (pieczęć placówki / podpis dokumentów)
    /// </summary>
    public string WssCertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Hasło do pliku certyfikatu WSS
    /// </summary>
    public string WssCertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// OID usługodawcy
    /// </summary>
    public string SubjectOid { get; set; } = string.Empty;

    /// <summary>
    /// Numer księgi rejestrowej RPWDL
    /// </summary>
    public string RpwdlBookNumber { get; set; } = string.Empty;

    /// <summary>
    /// 14-znakowy REGON zakładu leczniczego
    /// </summary>
    public string Regon14 { get; set; } = string.Empty;

    /// <summary>
    /// Kod resortowy komórki organizacyjnej (cz. VII)
    /// </summary>
    public string OrganizationalUnitCode { get; set; } = string.Empty;

    /// <summary>
    /// Nazwa placówki (do dokumentów CDA)
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Identyfikator organizacji w P1 (REGON14 lub inny)
    /// </summary>
    public string OrganizationIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Katalog lokalnego repozytorium plików EDM (XML)
    /// </summary>
    public string EdmRepositoryPath { get; set; } = "EdmRepository";

    /// <summary>
    /// OID systemu identyfikacji PESEL (stały wg PIK)
    /// </summary>
    public string PeselOid { get; set; } = "2.16.840.1.113883.3.4424.1.1.616";

    /// <summary>
    /// OID systemu identyfikacji NPWZ lekarza (stały wg PIK)
    /// </summary>
    public string NpwzOid { get; set; } = "2.16.840.1.113883.3.4424.1.6.2";

    /// <summary>
    /// Adres bazowy API Middleware (np. https://telsa.e-recepty.online/medyczneapi.testcered.pl)
    /// </summary>
    public string MiddlewareBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Login do API Middleware (endpoint /login)
    /// </summary>
    public string MiddlewareUsername { get; set; } = string.Empty;

    /// <summary>
    /// Hasło do API Middleware (zalecane: zmienna środowiskowa, nie plik konfiguracyjny)
    /// </summary>
    public string MiddlewarePassword { get; set; } = string.Empty;

    /// <summary>
    /// Numer PWZ lekarza wystawiającego orzeczenia medycyny pracy (pole "author.pwz" w Middleware)
    /// </summary>
    public string DoctorPwz { get; set; } = string.Empty;

    /// <summary>Imię lekarza (pole "author.name" w Middleware).</summary>
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>Nazwisko lekarza (pole "author.surname" w Middleware).</summary>
    public string DoctorSurname { get; set; } = string.Empty;

    /// <summary>Telefon placówki (pole "author.clinicPhone" w Middleware).</summary>
    public string ClinicPhone { get; set; } = string.Empty;

    /// <summary>Kod pocztowy placówki (pole "author.clinicPostCode" w Middleware).</summary>
    public string ClinicPostCode { get; set; } = string.Empty;

    /// <summary>Miejscowość placówki (pole "author.clinicCity" w Middleware).</summary>
    public string ClinicCity { get; set; } = string.Empty;

    /// <summary>Nazwa ulicy placówki (pole "author.clinicStreetName" w Middleware).</summary>
    public string ClinicStreetName { get; set; } = string.Empty;

    /// <summary>Numer domu placówki (pole "author.clinicHouseNumber" w Middleware).</summary>
    public string ClinicHouseNumber { get; set; } = string.Empty;

    /// <summary>Kod oddziału NFZ (pole "author.clinicNFZ" w Middleware).</summary>
    public string ClinicNfz { get; set; } = string.Empty;

    /// <summary>Identyfikator izby lekarskiej (pole "author.idIzbyLekarskiej" w Middleware).</summary>
    public string IdIzbyLekarskiej { get; set; } = string.Empty;

    /// <summary>Kod resortowy cz. VII (pole "author.Res7" w Middleware).</summary>
    public string Res7 { get; set; } = string.Empty;

    /// <summary>Kod resortowy cz. VIII (pole "author.Res8" w Middleware).</summary>
    public string Res8 { get; set; } = string.Empty;

    /// <summary>Nazwa komórki organizacyjnej cz. VIII (pole "author.Res8Name" w Middleware).</summary>
    public string Res8Name { get; set; } = string.Empty;

    /// <summary>Imię osoby towarzyszącej/asystenta (pole "author.assistantName" w Middleware).</summary>
    public string AssistantName { get; set; } = string.Empty;

    /// <summary>Nazwisko osoby towarzyszącej/asystenta (pole "author.assistantSurname" w Middleware).</summary>
    public string AssistantSurname { get; set; } = string.Empty;

    /// <summary>Identyfikator asystenta (pole "author.assistantId" w Middleware).</summary>
    public string AssistantId { get; set; } = string.Empty;

    /// <summary>Identyfikator autora w module Medycyny Pracy (pole "author.authorMedycynaPracyId" w Middleware).</summary>
    public string AuthorMedycynaPracyId { get; set; } = string.Empty;

    /// <summary>
    /// Identyfikator podmiotu (OID) wymagany przez moduł Medycyny Pracy przy operacjach na orzeczeniach
    /// (pole "idPodmiotu" w Middleware, np. "2.16.840.1.113883.3.4424.2.7.18").
    /// </summary>
    public string IdPodmiotu { get; set; } = string.Empty;

    /// <summary>
    /// Hasło do certyfikatu podpisu lekarza używanego do podpisywania orzeczeń
    /// (pole "doctor_pass" w Middleware, wymagane przy weryfikacji i wystawianiu orzeczenia).
    /// Zalecane: zmienna środowiskowa, nie plik konfiguracyjny.
    /// </summary>
    public string DoctorCertificatePassword { get; set; } = string.Empty;
}
