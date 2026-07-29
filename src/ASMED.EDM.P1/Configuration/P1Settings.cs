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
}
