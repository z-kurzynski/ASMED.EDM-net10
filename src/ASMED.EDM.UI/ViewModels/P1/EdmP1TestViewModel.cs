using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels.P1;

/// <summary>
/// ViewModel zakładki EDM w sekcji Baza Danych.
/// Umożliwia testowanie integracji P1: weryfikację PESEL, przeglądanie
/// dokumentów EDM pacjenta oraz wystawianie testowego orzeczenia CDA.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class EdmP1TestViewModel : ViewModelBase
{
    private readonly IP1PatientVerificationService _verificationService;
    private readonly IEdmDocumentService _edmDocumentService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EdmP1TestViewModel> _logger;

    public EdmP1TestViewModel(
        IP1PatientVerificationService verificationService,
        IEdmDocumentService edmDocumentService,
        IServiceScopeFactory scopeFactory,
        ILogger<EdmP1TestViewModel> logger)
    {
        _verificationService = verificationService;
        _edmDocumentService  = edmDocumentService;
        _scopeFactory        = scopeFactory;
        _logger              = logger;

        // Pokaż w UI jaki klient jest aktywny
        P1ClientMode = _verificationService is ASMED.EDM.P1.Clients.StubP1Client
            ? "⚠️ Tryb STUB (dane testowe)"
            : "✅ Tryb LIVE — połączenie z P1";
    }

    // ─── Panel 1: Weryfikacja PESEL ──────────────────────────────────────

    [ObservableProperty] private string _pesel = string.Empty;
    [ObservableProperty] private string _p1ClientMode = string.Empty;
    [ObservableProperty] private string _peselVerifyStatus = "Wprowadź PESEL i kliknij Weryfikuj.";
    [ObservableProperty] private bool   _peselVerifyOk     = false;
    [ObservableProperty] private string _peselPatientInfo  = string.Empty;
    [ObservableProperty] private bool   _peselFound        = false;

    [RelayCommand]
    private async Task VerifyPeselAsync()
    {
        if (string.IsNullOrWhiteSpace(Pesel) || Pesel.Length != 11)
        {
            PeselVerifyStatus = "⚠️ Wprowadź poprawny 11-cyfrowy numer PESEL.";
            return;
        }

        IsBusy      = true;
        BusyMessage = "Wyszukiwanie pacjenta po PESEL w P1...";
        PeselVerifyStatus = "⏳ Trwa zapytanie do P1...";
        PeselFound        = false;
        PeselPatientInfo  = string.Empty;
        FoundPatientId    = 0;

        try
        {
            // 1. Szukaj bezpośrednio w P1 po PESEL
            var result = await _verificationService.SearchByPeselDirectAsync(Pesel.Trim());

            PeselFound    = result.Found;
            PeselVerifyOk = result.Found && result.ErrorMessage is null;

            if (!result.Found)
            {
                PeselVerifyStatus = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "⚠️ Pacjent nie odnaleziony w P1."
                    : $"❌ Błąd P1: {result.ErrorMessage}";
                return;
            }

            // 2. Buduj czytelne info z danych P1
            var genderLabel = result.Gender switch
            {
                "Male"   => "Mężczyzna",
                "Female" => "Kobieta",
                _        => result.Gender ?? "—"
            };

            PeselPatientInfo =
                $"Imię i nazwisko: {result.FirstName} {result.LastName}\n" +
                $"Data urodzenia:  {result.DateOfBirth:dd.MM.yyyy}\n" +
                $"Płeć:            {genderLabel}\n" +
                $"Adres:           {result.Address ?? "—"}\n" +
                $"Telefon:         {result.Phone ?? "—"}\n" +
                $"ID P1 (FHIR):   {result.P1PatientId}";

            // 3. Opcjonalne dopasowanie do lokalnej bazy (pomijamy błędy DB)
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var patients = await unitOfWork.Patients.GetAllAsync();
                var localPatient = patients.FirstOrDefault(p => p.IdentificationNumber == Pesel.Trim());

                if (localPatient is not null)
                {
                    FoundPatientId    = localPatient.Id;
                    PeselVerifyStatus = "✅ Pacjent odnaleziony w P1 i dopasowany do lokalnej bazy.";
                    await LoadEdmDocumentsAsync();
                }
                else
                {
                    PeselVerifyStatus = "✅ Pacjent odnaleziony w P1 — brak rekordu w lokalnej bazie.";
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "Nie udało się dopasować pacjenta do lokalnej bazy — kontynuuję.");
                PeselVerifyStatus = "✅ Pacjent odnaleziony w P1 (brak dostępu do lokalnej bazy).";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd weryfikacji PESEL {Pesel}", Pesel.Length > 4 ? Pesel[..4] + "***" : "***");
            PeselVerifyStatus = $"❌ Wyjątek: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }


    [ObservableProperty] private int    _foundPatientId;
    [ObservableProperty] private string _edmStatus = "Wykonaj weryfikację PESEL, aby wczytać dokumenty.";

    public ObservableCollection<EdmDocument> EdmDocuments { get; } = [];

    [RelayCommand]
    private async Task LoadEdmDocumentsAsync()
    {
        if (FoundPatientId <= 0) return;
        try
        {
            var docs = await _edmDocumentService.GetPatientDocumentsAsync(FoundPatientId);
            EdmDocuments.Clear();
            foreach (var d in docs) EdmDocuments.Add(d);
            EdmStatus = EdmDocuments.Count > 0
                ? $"✅ Znaleziono {EdmDocuments.Count} dokumentów EDM."
                : "ℹ️ Brak dokumentów EDM dla tego pacjenta.";
        }
        catch (Exception ex)
        {
            EdmStatus = $"❌ Błąd wczytywania dokumentów: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenEdmDocument(EdmDocument? doc)
    {
        if (doc?.FilePath is null) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = doc.FilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            EdmStatus = $"❌ Nie można otworzyć pliku: {ex.Message}";
        }
    }

    // ─── Panel 3: Testowe orzeczenie CDA ────────────────────────────────

    [ObservableProperty] private int      _doctorId     = 1;
    [ObservableProperty] private int      _currentUserId = 1;
    [ObservableProperty] private bool     _isAbleToWork  = true;
    [ObservableProperty] private string   _workPosition  = string.Empty;
    [ObservableProperty] private string   _employerName  = string.Empty;
    [ObservableProperty] private string   _statement     = string.Empty;
    [ObservableProperty] private DateTime _nextExaminationDate = DateTime.Today.AddYears(1);
    [ObservableProperty] private string   _certificateStatus  = "Uzupełnij dane i kliknij Wystaw orzeczenie.";
    [ObservableProperty] private bool     _certificateOk      = false;
    [ObservableProperty] private string   _certificateFilePath = string.Empty;

    [RelayCommand]
    private async Task IssueCertificateAsync()
    {
        if (FoundPatientId <= 0)
        {
            CertificateStatus = "⚠️ Najpierw zweryfikuj pacjenta po PESEL.";
            return;
        }

        IsBusy      = true;
        BusyMessage = "Generowanie orzeczenia HL7 CDA...";
        CertificateOk = false;

        try
        {
            var request = new WorkAbilityCertificateRequest
            {
                PatientId           = FoundPatientId,
                DoctorId            = DoctorId,
                IsAbleToWork        = IsAbleToWork,
                WorkPosition        = WorkPosition,
                EmployerName        = EmployerName,
                Statement           = Statement,
                NextExaminationDate = NextExaminationDate
            };

            var doc = await _edmDocumentService.CreateWorkAbilityCertificateAsync(request, CurrentUserId);

            CertificateOk      = true;
            CertificateFilePath = doc.FilePath ?? string.Empty;
            CertificateStatus   = doc.SignatureStatus == EdmSignatureStatus.Signed
                ? $"✅ Orzeczenie wystawione i podpisane pieczęcią placówki  (EDM #{doc.Id})"
                : $"✅ Orzeczenie wystawione (bez podpisu — brak certyfikatu WSS)  (EDM #{doc.Id})";

            await LoadEdmDocumentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd wystawiania orzeczenia");
            CertificateStatus = $"❌ Błąd: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenCertificateFile()
    {
        if (string.IsNullOrWhiteSpace(CertificateFilePath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = CertificateFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            CertificateStatus = $"❌ Nie można otworzyć pliku: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        Pesel               = string.Empty;
        PeselVerifyStatus   = "Wprowadź PESEL i kliknij Weryfikuj.";
        PeselVerifyOk       = false;
        PeselFound          = false;
        PeselPatientInfo    = string.Empty;
        FoundPatientId      = 0;
        EdmDocuments.Clear();
        EdmStatus           = "Wykonaj weryfikację PESEL, aby wczytać dokumenty.";
        CertificateStatus   = "Uzupełnij dane i kliknij Wystaw orzeczenie.";
        CertificateOk       = false;
        CertificateFilePath = string.Empty;
    }
}
