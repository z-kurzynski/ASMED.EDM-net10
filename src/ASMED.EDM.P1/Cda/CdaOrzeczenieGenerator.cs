using System.Xml.Linq;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.P1.Configuration;
using Microsoft.Extensions.Options;

namespace ASMED.EDM.P1.Cda;

/// <summary>
/// Generator dokumentu HL7 CDA — orzeczenie o zdolności do pracy (wg PIK HL7 CDA).
/// </summary>
public class CdaOrzeczenieGenerator
{
    private static readonly XNamespace Hl7 = "urn:hl7-org:v3";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>OID szablonu dokumentu orzeczenia wg PIK HL7 CDA (poziom 3)</summary>
    private const string TemplateOid = "2.16.840.1.113883.3.4424.13.10.1.24";

    /// <summary>Kod LOINC dokumentu orzeczenia/zaświadczenia lekarskiego</summary>
    private const string LoincCertificateCode = "11503-0";

    private readonly P1Settings _settings;

    public CdaOrzeczenieGenerator(IOptions<P1Settings> settings)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Generuje XML HL7 CDA orzeczenia o zdolności do pracy.
    /// </summary>
    /// <returns>Krotka: dokument XML oraz nadany identyfikator instancji dokumentu (extension)</returns>
    public (XDocument Document, string DocumentId) Generate(
        WorkAbilityCertificateRequest request,
        Patient patient,
        Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(patient);
        ArgumentNullException.ThrowIfNull(doctor);

        var documentId = Guid.NewGuid().ToString();
        var now = DateTime.Now;
        var statementText = BuildStatementText(request);

        var clinicalDocument = new XElement(Hl7 + "ClinicalDocument",
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi),
            new XAttribute("classCode", "DOCCLIN"),
            new XAttribute("moodCode", "EVN"),
            new XElement(Hl7 + "typeId",
                new XAttribute("root", "2.16.840.1.113883.1.3"),
                new XAttribute("extension", "POCD_HD000040")),
            new XElement(Hl7 + "templateId", new XAttribute("root", TemplateOid)),
            new XElement(Hl7 + "id",
                new XAttribute("root", $"{_settings.SubjectOid}.2"),
                new XAttribute("extension", documentId)),
            new XElement(Hl7 + "code",
                new XAttribute("code", LoincCertificateCode),
                new XAttribute("codeSystem", "2.16.840.1.113883.6.1"),
                new XAttribute("codeSystemName", "LOINC"),
                new XAttribute("displayName", "Zaświadczenie lekarskie")),
            new XElement(Hl7 + "title", "Orzeczenie lekarskie o zdolności do pracy"),
            new XElement(Hl7 + "effectiveTime", new XAttribute("value", now.ToString("yyyyMMddHHmmss"))),
            new XElement(Hl7 + "confidentialityCode",
                new XAttribute("code", "N"),
                new XAttribute("codeSystem", "2.16.840.1.113883.5.25")),
            new XElement(Hl7 + "languageCode", new XAttribute("code", "pl-PL")),
            BuildRecordTarget(patient),
            BuildAuthor(doctor, now),
            BuildCustodian(),
            BuildComponent(request, statementText));

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            clinicalDocument);

        return (document, documentId);
    }

    private XElement BuildRecordTarget(Patient patient) =>
        new(Hl7 + "recordTarget",
            new XElement(Hl7 + "patientRole",
                new XElement(Hl7 + "id",
                    new XAttribute("root", _settings.PeselOid),
                    new XAttribute("extension", patient.IdentificationNumber)),
                new XElement(Hl7 + "addr",
                    new XElement(Hl7 + "city", patient.City ?? string.Empty),
                    new XElement(Hl7 + "postalCode", patient.PostalCode ?? string.Empty),
                    new XElement(Hl7 + "streetAddressLine", patient.Address ?? string.Empty)),
                new XElement(Hl7 + "patient",
                    new XElement(Hl7 + "name",
                        new XElement(Hl7 + "given", patient.FirstName),
                        new XElement(Hl7 + "family", patient.LastName)),
                    new XElement(Hl7 + "birthTime",
                        new XAttribute("value", patient.DateOfBirth.ToString("yyyyMMdd"))))));

    private XElement BuildAuthor(Doctor doctor, DateTime now) =>
        new(Hl7 + "author",
            new XElement(Hl7 + "time", new XAttribute("value", now.ToString("yyyyMMddHHmmss"))),
            new XElement(Hl7 + "assignedAuthor",
                new XElement(Hl7 + "id",
                    new XAttribute("root", _settings.NpwzOid),
                    new XAttribute("extension", doctor.MedicalLicenseNumber)),
                new XElement(Hl7 + "assignedPerson",
                    new XElement(Hl7 + "name",
                        new XElement(Hl7 + "given", doctor.User?.FirstName ?? string.Empty),
                        new XElement(Hl7 + "family", doctor.User?.LastName ?? string.Empty)))));

    private XElement BuildCustodian() =>
        new(Hl7 + "custodian",
            new XElement(Hl7 + "assignedCustodian",
                new XElement(Hl7 + "representedCustodianOrganization",
                    new XElement(Hl7 + "id",
                        new XAttribute("root", string.IsNullOrEmpty(_settings.SubjectOid) ? "0.0" : _settings.SubjectOid),
                        new XAttribute("extension", _settings.Regon14)),
                    new XElement(Hl7 + "name", _settings.OrganizationName))));

    private XElement BuildComponent(WorkAbilityCertificateRequest request, string statementText) =>
        new(Hl7 + "component",
            new XElement(Hl7 + "structuredBody",
                new XElement(Hl7 + "component",
                    new XElement(Hl7 + "section",
                        new XElement(Hl7 + "code",
                            new XAttribute("code", "51848-0"),
                            new XAttribute("codeSystem", "2.16.840.1.113883.6.1"),
                            new XAttribute("codeSystemName", "LOINC"),
                            new XAttribute("displayName", "Treść orzeczenia")),
                        new XElement(Hl7 + "title", "Orzeczenie"),
                        new XElement(Hl7 + "text",
                            new XElement(Hl7 + "paragraph", statementText),
                            string.IsNullOrWhiteSpace(request.WorkPosition)
                                ? null
                                : new XElement(Hl7 + "paragraph", $"Stanowisko: {request.WorkPosition}"),
                            string.IsNullOrWhiteSpace(request.EmployerName)
                                ? null
                                : new XElement(Hl7 + "paragraph", $"Pracodawca: {request.EmployerName}"),
                            request.NextExaminationDate is null
                                ? null
                                : new XElement(Hl7 + "paragraph",
                                    $"Data następnego badania okresowego: {request.NextExaminationDate:yyyy-MM-dd}"))))));

    private static string BuildStatementText(WorkAbilityCertificateRequest request)
    {
        var baseText = request.IsAbleToWork
            ? "Wobec braku przeciwwskazań zdrowotnych orzeka się, że badany jest zdolny do wykonywania pracy na wskazanym stanowisku."
            : "Wobec istnienia przeciwwskazań zdrowotnych orzeka się, że badany jest niezdolny do wykonywania pracy na wskazanym stanowisku.";

        return string.IsNullOrWhiteSpace(request.Statement)
            ? baseText
            : $"{baseText} {request.Statement}";
    }
}
