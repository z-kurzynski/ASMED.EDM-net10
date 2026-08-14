using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using ASMED.EDM.P1.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASMED.EDM.P1.Cda;

/// <summary>
/// Podpisuje dokumenty XML pieczątem placówki (certyfikat PKCS#12 / WSS).
/// Używa standardowego podpisu XMLDSig (enveloped).
/// </summary>
public class XadesSigner
{
    private readonly P1Settings _settings;
    private readonly ILogger<XadesSigner> _logger;

    public XadesSigner(IOptions<P1Settings> options, ILogger<XadesSigner> logger)
    {
        _settings = options.Value;
        _logger   = logger;
    }

    /// <summary>Czy certyfikat WSS jest dostępny i plik istnieje.</summary>
    public bool CanSign =>
        !string.IsNullOrWhiteSpace(_settings.WssCertificatePath) &&
        File.Exists(_settings.WssCertificatePath);

    /// <summary>
    /// Podpisuje dokument XML i zwraca nowy XDocument z elementem Signature.
    /// </summary>
    /// <exception cref="InvalidOperationException">Gdy certyfikat jest niedostępny.</exception>
    public XDocument Sign(XDocument document)
    {
        if (!CanSign)
            throw new InvalidOperationException(
                $"Certyfikat WSS nie jest dostępny: {_settings.WssCertificatePath}");

        var cert = X509CertificateLoader.LoadPkcs12FromFile(
            _settings.WssCertificatePath,
            _settings.WssCertificatePassword,
            X509KeyStorageFlags.EphemeralKeySet);

        var xmlDoc = ToXmlDocument(document);

        var signedXml = new SignedXml(xmlDoc);
        signedXml.SigningKey = cert.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Certyfikat nie zawiera klucza prywatnego RSA.");

        var reference = new Reference { Uri = "" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();
        xmlDoc.DocumentElement!.AppendChild(signedXml.GetXml());

        _logger.LogInformation("Dokument podpisany certyfikatem {Subject}", cert.Subject);

        return ToXDocument(xmlDoc);
    }

    /// <summary>
    /// Weryfikuje podpis XMLDSig dokumentu.
    /// </summary>
    public bool Verify(XDocument document)
    {
        var xmlDoc = ToXmlDocument(document);
        var sigNodes = xmlDoc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
        if (sigNodes.Count == 0) return false;

        try
        {
            var signedXml = new SignedXml(xmlDoc);
            signedXml.LoadXml((XmlElement)sigNodes[0]!);
            return signedXml.CheckSignature();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Weryfikacja podpisu nie powiodła się");
            return false;
        }
    }

    private static XmlDocument ToXmlDocument(XDocument xdoc)
    {
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        using var reader = xdoc.CreateReader();
        xmlDoc.Load(reader);
        return xmlDoc;
    }

    private static XDocument ToXDocument(XmlDocument xmlDoc)
    {
        using var ms = new MemoryStream();
        xmlDoc.Save(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return XDocument.Load(ms);
    }
}
