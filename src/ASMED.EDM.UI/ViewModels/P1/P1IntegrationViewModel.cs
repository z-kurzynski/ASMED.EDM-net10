using System.Collections.ObjectModel;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels.P1;

/// <summary>
/// ViewModel integracji P1 (e-zdrowie): weryfikacja pacjenta po PESEL,
/// ponowna wysyłka Zdarzenia Medycznego oraz wystawianie orzeczenia o zdolności do pracy.
/// Przeznaczony do bindowania w widokach pacjenta / wizyt / medycyny pracy.
/// </summary>
public partial class P1IntegrationViewModel : ViewModelBase
{
    private readonly IP1PatientVerificationService _verificationService;
    private readonly IMedicalEventService _medicalEventService;
    private readonly IEdmDocumentService _edmDocumentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<P1IntegrationViewModel> _logger;

    public P1IntegrationViewModel(
        IP1PatientVerificationService verificationService,
        IMedicalEventService medicalEventService,
        IEdmDocumentService edmDocumentService,
        IUnitOfWork unitOfWork,
        ILogger<P1IntegrationViewModel> logger)
    {
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _medicalEventService = medicalEventService ?? throw new ArgumentNullException(nameof(medicalEventService));
        _edmDocumentService = edmDocumentService ?? throw new ArgumentNullException(nameof(edmDocumentService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==================== WERYFIKACJA PESEL W P1 ====================

    /// <summary>Identyfikator pacjenta do weryfikacji</summary>
    [ObservableProperty]
    private int _patientId;

    /// <summary>Wynik weryfikacji pacjenta w P1 (tekst do wyświetlenia)</summary>
    [ObservableProperty]
    private string? _verificationStatus;

    /// <summary>Czy weryfikacja zakończyła się pozytywnie</summary>
    [ObservableProperty]
    private bool _isPatientVerified;

    [RelayCommand]
    private async Task VerifyPatientAsync()
    {
        IsBusy = true;
        BusyMessage = "Weryfikacja pacjenta w P1...";
        try
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(PatientId);
            if (patient is null)
            {
                VerificationStatus = "Nie znaleziono pacjenta.";
                IsPatientVerified = false;
                return;
            }

            var result = await _verificationService.VerifyPatientByPeselAsync(patient);
            IsPatientVerified = result.Found && result.DataMatches;
            VerificationStatus = result switch
            {
                { ErrorMessage: not null } => $"Błąd weryfikacji: {result.ErrorMessage}",
                { Found: false } => "Pacjent nie został odnaleziony w P1.",
                { DataMatches: false } => "Pacjent odnaleziony w P1, ale dane różnią się od lokalnych.",
                _ => "Pacjent zweryfikowany pozytywnie w P1."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd weryfikacji pacjenta {PatientId} w P1", PatientId);
            VerificationStatus = $"Błąd weryfikacji: {ex.Message}";
            IsPatientVerified = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ==================== ZDARZENIA MEDYCZNE (ZM) ====================

    /// <summary>Identyfikator wizyty do (ponownej) wysyłki ZM</summary>
    [ObservableProperty]
    private int _visitId;

    /// <summary>Status synchronizacji ZM (tekst do wyświetlenia)</summary>
    [ObservableProperty]
    private string? _visitSyncStatus;

    [RelayCommand]
    private async Task RetryVisitSyncAsync()
    {
        IsBusy = true;
        BusyMessage = "Wysyłanie Zdarzenia Medycznego do P1...";
        try
        {
            var visit = await _unitOfWork.Visits.GetByIdAsync(VisitId);
            if (visit is null)
            {
                VisitSyncStatus = "Nie znaleziono wizyty.";
                return;
            }

            var result = string.IsNullOrEmpty(visit.P1EventId)
                ? await _medicalEventService.RegisterVisitAsync(visit)
                : await _medicalEventService.UpdateVisitAsync(visit);

            var syncLog = new P1SyncLog
            {
                OperationType = string.IsNullOrEmpty(visit.P1EventId) ? "RegisterVisit" : "UpdateVisit",
                EntityName = nameof(Visit),
                EntityId = visit.Id,
                Status = result.Success ? P1SyncStatus.Synced : P1SyncStatus.Failed,
                P1Identifier = result.P1EventId,
                ErrorMessage = result.ErrorMessage,
                LastAttemptAt = DateTime.UtcNow
            };

            if (result.Success)
            {
                visit.P1EventId = result.P1EventId;
                visit.P1SyncStatus = P1SyncStatus.Synced;
                VisitSyncStatus = "Zdarzenie Medyczne zsynchronizowane z P1.";
            }
            else
            {
                visit.P1SyncStatus = P1SyncStatus.Failed;
                VisitSyncStatus = $"Błąd synchronizacji ZM: {result.ErrorMessage}";
            }

            _unitOfWork.Visits.Update(visit);
            await _unitOfWork.P1SyncLogs.AddAsync(syncLog);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd ponownej wysyłki ZM dla wizyty {VisitId}", VisitId);
            VisitSyncStatus = $"Błąd: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ==================== ORZECZENIE O ZDOLNOŚCI DO PRACY ====================

    /// <summary>Identyfikator lekarza wystawiającego orzeczenie</summary>
    [ObservableProperty]
    private int _doctorId;

    /// <summary>Identyfikator zalogowanego użytkownika (audyt)</summary>
    [ObservableProperty]
    private int _currentUserId;

    /// <summary>Czy pacjent jest zdolny do pracy</summary>
    [ObservableProperty]
    private bool _isAbleToWork = true;

    /// <summary>Stanowisko pracy</summary>
    [ObservableProperty]
    private string? _workPosition;

    /// <summary>Nazwa pracodawcy</summary>
    [ObservableProperty]
    private string? _employerName;

    /// <summary>Uzasadnienie / treść orzeczenia</summary>
    [ObservableProperty]
    private string? _statement;

    /// <summary>Data następnego badania okresowego</summary>
    [ObservableProperty]
    private DateTime? _nextExaminationDate;

    /// <summary>Status operacji wystawienia orzeczenia</summary>
    [ObservableProperty]
    private string? _certificateStatus;

    /// <summary>Dokumenty EDM pacjenta</summary>
    public ObservableCollection<EdmDocument> PatientDocuments { get; } = [];

    [RelayCommand]
    private async Task IssueWorkAbilityCertificateAsync()
    {
        IsBusy = true;
        BusyMessage = "Generowanie orzeczenia (HL7 CDA)...";
        try
        {
            var request = new WorkAbilityCertificateRequest
            {
                PatientId = PatientId,
                VisitId = VisitId > 0 ? VisitId : null,
                DoctorId = DoctorId,
                IsAbleToWork = IsAbleToWork,
                WorkPosition = WorkPosition,
                EmployerName = EmployerName,
                Statement = Statement,
                NextExaminationDate = NextExaminationDate
            };

            var document = await _edmDocumentService.CreateWorkAbilityCertificateAsync(request, CurrentUserId);

            CertificateStatus = document.SignatureStatus == EdmSignatureStatus.Signed
                ? $"Orzeczenie wystawione i podpisane (dokument #{document.Id})."
                : $"Orzeczenie wystawione bez podpisu (dokument #{document.Id}) — brak certyfikatu WSS.";

            await LoadPatientDocumentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd wystawiania orzeczenia dla pacjenta {PatientId}", PatientId);
            CertificateStatus = $"Błąd: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadPatientDocumentsAsync()
    {
        try
        {
            var documents = await _edmDocumentService.GetPatientDocumentsAsync(PatientId);
            PatientDocuments.Clear();
            foreach (var doc in documents)
            {
                PatientDocuments.Add(doc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd wczytywania dokumentów EDM pacjenta {PatientId}", PatientId);
        }
    }
}
