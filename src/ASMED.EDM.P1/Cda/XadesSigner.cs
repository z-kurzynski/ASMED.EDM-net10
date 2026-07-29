using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using ASMED.EDM.P1.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASMED.EDM.P1.Cda;

/// <summary>
/// Podpisuje dokumenty XML (HL7 CDA) podpisem enveloped (XMLDSig / baza pod XAdES)
/// certyfikatem placówki (pieczęć — certyfikat WSS z P1).
/// </summary>
public class XadesSigner
{
    private readonly P1Settings _settings;
    private readonly ILogger<XadesSigner> _logger;

    public XadesSigner(IOptions<P1Settings> settings, ILogger<XadesSigner> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Czy podpisywanie jest możliwe (skonfigurowany certyfikat WSS).
    /// </summary>
    public bool CanSign =>
        !string.IsNullOrWhiteSpace(_settings.WssCertificatePath) &&
        File.Exists(_settings.WssCertificatePath);

    /// <summary>
    /// Podpisuje dokument XML podpisem enveloped i zwraca podpisany dokument.
    /// </summary>
    public XDocument Sign(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!CanSign)
            throw new InvalidOperationException("Brak skonfigurowanego certyfikatu WSS (P1Settings:WssCertificatePath).");

        using var certificate = new X509Certificate2(
            _settings.WssCertificatePath,
            _settings.WssCertificatePassword,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

        if (!certificate.HasPrivateKey)
            throw new InvalidOperationException("Certyfikat WSS nie zawiera klucza prywatnego.");

        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        using (var reader = document.CreateReader())
        {
            xmlDoc.Load(reader);
        }

        var signedXml = new SignedXml(xmlDoc)
        {
            SigningKey = certificate.GetRSAPrivateKey()
                ?? throw new InvalidOperationException("Certyfikat WSS nie zawiera klucza prywatnego RSA.")
        };

        var reference = new Reference { Uri = string.Empty };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificate));
        signedXml.KeyInfo = keyInfo;

        signedXml.ComputeSignature();
        var signatureElement = signedXml.GetXml();
        xmlDoc.DocumentElement!.AppendChild(xmlDoc.ImportNode(signatureElement, true));

        _logger.LogInformation("Dokument podpisany certyfikatem placówki (thumbprint: {Thumbprint})", certificate.Thumbprint);

        using var nodeReader = new XmlNodeReader(xmlDoc);
        return XDocument.Load(nodeReader);
    }

    /// <summary>
    /// Weryfikuje podpis enveloped dokumentu XML.
    /// </summary>
    public bool Verify(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        using (var reader = document.CreateReader())
        {
            xmlDoc.Load(reader);
        }

        var signatureNode = xmlDoc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)
            .OfType<XmlElement>()
            .FirstOrDefault();

        if (signatureNode is null)
            return false;

        var signedXml = new SignedXml(xmlDoc);
        signedXml.LoadXml(signatureNode);
        return signedXml.CheckSignature();
    }
}
