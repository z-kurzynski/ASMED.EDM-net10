using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using ASMED.EDM.P1.Cda;
using ASMED.EDM.P1.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ASMED.EDM.P1.Tests;

public class XadesSignerTests : IDisposable
{
    private readonly string _certPath;
    private const string CertPassword = "test123";

    public XadesSignerTests()
    {
        // Wygeneruj self-signed certyfikat testowy (PKCS#12)
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Test NZOZ Pieczec", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

        _certPath = Path.Combine(Path.GetTempPath(), $"test-wss-{Guid.NewGuid():N}.p12");
        File.WriteAllBytes(_certPath, cert.Export(X509ContentType.Pkcs12, CertPassword));
    }

    public void Dispose()
    {
        if (File.Exists(_certPath))
            File.Delete(_certPath);
    }

    private XadesSigner CreateSigner(string? certPath = null) => new(
        Options.Create(new P1Settings
        {
            WssCertificatePath = certPath ?? _certPath,
            WssCertificatePassword = CertPassword
        }),
        NullLogger<XadesSigner>.Instance);

    private static XDocument CreateTestDocument() =>
        new(new XElement("ClinicalDocument", new XElement("title", "Test")));

    [Fact]
    public void CanSign_ReturnsTrue_WhenCertificateExists()
    {
        Assert.True(CreateSigner().CanSign);
    }

    [Fact]
    public void CanSign_ReturnsFalse_WhenCertificateMissing()
    {
        Assert.False(CreateSigner("nonexistent.p12").CanSign);
    }

    [Fact]
    public void Sign_AddsSignatureElement()
    {
        var signed = CreateSigner().Sign(CreateTestDocument());

        var signature = signed.Root!
            .Element(XName.Get("Signature", "http://www.w3.org/2000/09/xmldsig#"));

        Assert.NotNull(signature);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForValidSignature()
    {
        var signer = CreateSigner();
        var signed = signer.Sign(CreateTestDocument());

        Assert.True(signer.Verify(signed));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForUnsignedDocument()
    {
        Assert.False(CreateSigner().Verify(CreateTestDocument()));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForTamperedDocument()
    {
        var signer = CreateSigner();
        var signed = signer.Sign(CreateTestDocument());

        // Modyfikacja treści po podpisaniu
        signed.Root!.Element("title")!.Value = "Zmodyfikowany";

        Assert.False(signer.Verify(signed));
    }

    [Fact]
    public void Sign_Throws_WhenCertificateMissing()
    {
        Assert.Throws<InvalidOperationException>(
            () => CreateSigner("nonexistent.p12").Sign(CreateTestDocument()));
    }
}
