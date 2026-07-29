using System.Xml.Linq;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.P1.Cda;
using ASMED.EDM.P1.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace ASMED.EDM.P1.Tests;

public class CdaOrzeczenieGeneratorTests
{
    private static readonly XNamespace Hl7 = "urn:hl7-org:v3";

    private static P1Settings CreateSettings() => new()
    {
        SubjectOid = "2.16.840.1.113883.3.4424.2.3.1.999",
        Regon14 = "12345678901234",
        OrganizationName = "Testowy NZOZ"
    };

    private static (Patient, Doctor) CreateTestData()
    {
        var patient = new Patient
        {
            Id = 1,
            FirstName = "Jan",
            LastName = "Kowalski",
            IdentificationNumber = "80010112345",
            DateOfBirth = new DateTime(1980, 1, 1),
            City = "Warszawa",
            PostalCode = "00-001",
            Address = "ul. Testowa 1"
        };

        var doctor = new Doctor
        {
            Id = 2,
            MedicalLicenseNumber = "1234567",
            User = new User { FirstName = "Anna", LastName = "Nowak" }
        };

        return (patient, doctor);
    }

    private static CdaOrzeczenieGenerator CreateGenerator() =>
        new(Options.Create(CreateSettings()));

    [Fact]
    public void Generate_ReturnsClinicalDocument_WithHl7Namespace()
    {
        var (patient, doctor) = CreateTestData();
        var request = new WorkAbilityCertificateRequest { PatientId = 1, DoctorId = 2, IsAbleToWork = true };

        var (document, documentId) = CreateGenerator().Generate(request, patient, doctor);

        Assert.NotNull(document.Root);
        Assert.Equal(Hl7 + "ClinicalDocument", document.Root!.Name);
        Assert.False(string.IsNullOrWhiteSpace(documentId));
    }

    [Fact]
    public void Generate_ContainsPatientPesel_InRecordTarget()
    {
        var (patient, doctor) = CreateTestData();
        var request = new WorkAbilityCertificateRequest { PatientId = 1, DoctorId = 2, IsAbleToWork = true };

        var (document, _) = CreateGenerator().Generate(request, patient, doctor);

        var patientRoleId = document.Root!
            .Element(Hl7 + "recordTarget")!
            .Element(Hl7 + "patientRole")!
            .Element(Hl7 + "id")!;

        Assert.Equal("2.16.840.1.113883.3.4424.1.1.616", patientRoleId.Attribute("root")!.Value);
        Assert.Equal("80010112345", patientRoleId.Attribute("extension")!.Value);
    }

    [Fact]
    public void Generate_ContainsDoctorNpwz_InAuthor()
    {
        var (patient, doctor) = CreateTestData();
        var request = new WorkAbilityCertificateRequest { PatientId = 1, DoctorId = 2, IsAbleToWork = true };

        var (document, _) = CreateGenerator().Generate(request, patient, doctor);

        var authorId = document.Root!
            .Element(Hl7 + "author")!
            .Element(Hl7 + "assignedAuthor")!
            .Element(Hl7 + "id")!;

        Assert.Equal("2.16.840.1.113883.3.4424.1.6.2", authorId.Attribute("root")!.Value);
        Assert.Equal("1234567", authorId.Attribute("extension")!.Value);
    }

    [Theory]
    [InlineData(true, "zdolny")]
    [InlineData(false, "niezdolny")]
    public void Generate_StatementText_ReflectsAbilityToWork(bool isAble, string expectedFragment)
    {
        var (patient, doctor) = CreateTestData();
        var request = new WorkAbilityCertificateRequest { PatientId = 1, DoctorId = 2, IsAbleToWork = isAble };

        var (document, _) = CreateGenerator().Generate(request, patient, doctor);

        Assert.Contains(expectedFragment, document.ToString());
    }

    [Fact]
    public void Generate_IncludesWorkPositionAndEmployer_WhenProvided()
    {
        var (patient, doctor) = CreateTestData();
        var request = new WorkAbilityCertificateRequest
        {
            PatientId = 1,
            DoctorId = 2,
            IsAbleToWork = true,
            WorkPosition = "Operator wózka widłowego",
            EmployerName = "Firma Testowa Sp. z o.o."
        };

        var (document, _) = CreateGenerator().Generate(request, patient, doctor);
        var text = document.ToString();

        Assert.Contains("Operator wózka widłowego", text);
        Assert.Contains("Firma Testowa Sp. z o.o.", text);
    }

    [Fact]
    public void Generate_ContainsCustodianWithOrganizationData()
    {
        var (patient, doctor) = CreateTestData();
        var request = new WorkAbilityCertificateRequest { PatientId = 1, DoctorId = 2, IsAbleToWork = true };

        var (document, _) = CreateGenerator().Generate(request, patient, doctor);

        var custodianOrg = document.Root!
            .Element(Hl7 + "custodian")!
            .Element(Hl7 + "assignedCustodian")!
            .Element(Hl7 + "representedCustodianOrganization")!;

        Assert.Equal("Testowy NZOZ", custodianOrg.Element(Hl7 + "name")!.Value);
        Assert.Equal("12345678901234", custodianOrg.Element(Hl7 + "id")!.Attribute("extension")!.Value);
    }

    [Fact]
    public void Generate_DocumentIds_AreUnique()
    {
        var (patient, doctor) = CreateTestData();
        var request = new WorkAbilityCertificateRequest { PatientId = 1, DoctorId = 2, IsAbleToWork = true };
        var generator = CreateGenerator();

        var (_, id1) = generator.Generate(request, patient, doctor);
        var (_, id2) = generator.Generate(request, patient, doctor);

        Assert.NotEqual(id1, id2);
    }
}
