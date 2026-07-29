using System.Security.Cryptography;
using System.Text;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.P1.Cda;
using ASMED.EDM.P1.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASMED.EDM.P1.Services;

/// <summary>
/// Lokalne repozytorium dokumentów EDM — generacja CDA, podpis pieczęcią placówki,
/// zapis (baza + plik XML), przygotowanie pod indeksację w P1.
/// </summary>
public class EdmDocumentService : IEdmDocumentService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CdaOrzeczenieGenerator _cdaGenerator;
    private readonly XadesSigner _signer;
    private readonly IAuditService _auditService;
    private readonly P1Settings _settings;
    private readonly ILogger<EdmDocumentService> _logger;

    public EdmDocumentService(
        IServiceScopeFactory scopeFactory,
        CdaOrzeczenieGenerator cdaGenerator,
        XadesSigner signer,
        IAuditService auditService,
        IOptions<P1Settings> settings,
        ILogger<EdmDocumentService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _cdaGenerator = cdaGenerator ?? throw new ArgumentNullException(nameof(cdaGenerator));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EdmDocument> CreateWorkAbilityCertificateAsync(
        WorkAbilityCertificateRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new InvalidOperationException($"Nie znaleziono pacjenta o ID {request.PatientId}.");
        var doctor = await unitOfWork.Doctors.GetByIdAsync(request.DoctorId, cancellationToken)
            ?? throw new InvalidOperationException($"Nie znaleziono lekarza o ID {request.DoctorId}.");

        _logger.LogInformation("Generowanie orzeczenia o zdolności do pracy dla pacjenta {PatientId}", patient.Id);

        var (xml, documentId) = _cdaGenerator.Generate(request, patient, doctor);

        var signatureStatus = EdmSignatureStatus.Unsigned;
        DateTime? signedAt = null;
        if (_signer.CanSign)
        {
            try
            {
                xml = _signer.Sign(xml);
                signatureStatus = EdmSignatureStatus.Signed;
                signedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podpisu dokumentu {DocumentId}", documentId);
                signatureStatus = EdmSignatureStatus.SignatureFailed;
            }
        }
        else
        {
            _logger.LogWarning("Brak certyfikatu WSS — dokument {DocumentId} zapisany bez podpisu", documentId);
        }

        // Zapis pliku XML do repozytorium plikowego
        Directory.CreateDirectory(_settings.EdmRepositoryPath);
        var fileName = $"orzeczenie_{documentId}.xml";
        var filePath = Path.Combine(_settings.EdmRepositoryPath, fileName);
        var xmlContent = xml.Declaration + Environment.NewLine + xml;
        await File.WriteAllTextAsync(filePath, xmlContent, Encoding.UTF8, cancellationToken);

        var contentHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(xmlContent))).ToLowerInvariant();

        var document = new EdmDocument
        {
            DocumentType = EdmDocumentType.WorkAbilityCertificate,
            PatientId = patient.Id,
            VisitId = request.VisitId,
            DoctorId = doctor.Id,
            DocumentOid = $"{_settings.SubjectOid}.2^{documentId}",
            Title = "Orzeczenie lekarskie o zdolności do pracy",
            FilePath = filePath,
            ContentHash = contentHash,
            SignatureStatus = signatureStatus,
            SignedAt = signedAt,
            P1IndexStatus = P1SyncStatus.Pending,
            CreatedById = userId
        };

        await unitOfWork.EdmDocuments.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogOperationAsync(
            userId, "Create", nameof(EdmDocument), document.Id.ToString(),
            newValues: $"Orzeczenie {documentId}, pacjent {patient.Id}",
            cancellationToken: cancellationToken);

        return document;
    }

    public async Task<EdmDocument?> GetDocumentByIdAsync(int documentId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        return await unitOfWork.EdmDocuments.GetByIdAsync(documentId, cancellationToken);
    }

    public async Task<IEnumerable<EdmDocument>> GetPatientDocumentsAsync(int patientId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        return await unitOfWork.EdmDocuments.FindAsync(d => d.PatientId == patientId, cancellationToken);
    }

    public async Task<string> GetDocumentContentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var document = await unitOfWork.EdmDocuments.GetByIdAsync(documentId, cancellationToken)
            ?? throw new InvalidOperationException($"Nie znaleziono dokumentu o ID {documentId}.");

        if (!File.Exists(document.FilePath))
            throw new FileNotFoundException("Plik dokumentu EDM nie istnieje w repozytorium.", document.FilePath);

        var content = await File.ReadAllTextAsync(document.FilePath, Encoding.UTF8, cancellationToken);

        // Weryfikacja integralności
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
        if (!string.Equals(hash, document.ContentHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Naruszona integralność dokumentu EDM {documentId} — hash niezgodny.");

        return content;
    }

    public async Task<P1MedicalEventResult> IndexDocumentInP1Async(int documentId, CancellationToken cancellationToken = default)
    {
        // Etap 2 — indeksacja dokumentu w P1. Obecnie stub: oznacza dokument jako oczekujący.
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var document = await unitOfWork.EdmDocuments.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
            return new P1MedicalEventResult { Success = false, ErrorMessage = $"Nie znaleziono dokumentu {documentId}." };

        _logger.LogInformation("[STUB P1] Indeksacja dokumentu EDM {DocumentId} — funkcjonalność etapu 2", documentId);
        return new P1MedicalEventResult
        {
            Success = false,
            ErrorMessage = "Indeksacja dokumentów EDM w P1 zostanie udostępniona w etapie 2."
        };
    }
}
