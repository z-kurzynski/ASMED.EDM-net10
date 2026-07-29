using System;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using ASMED.EDM.P1.Configuration;

namespace ASMED.EDM.UI.ViewModels.ustawienia;

/// <summary>
/// ViewModel widoku Dane Placówki — trzy kolumny:
/// (1) podstawowe dane NZOZ, (2) integracja P1/e-zdrowie, (3) repozytorium EDM
/// </summary>
[SupportedOSPlatform("windows")]
public partial class FacilityDataViewModel : ViewModelBase
{
    private const string RegistryBase = @"Software\ASMED\EDM";
    private const string RegistryP1   = @"Software\ASMED\EDM\P1";

    private readonly ILogger<FacilityDataViewModel> _logger;
    private readonly P1Settings _p1Settings;

    // ── Kolumna 1: Dane placówki ────────────────────────────────────────────

    [ObservableProperty] private string _organizationName   = string.Empty;
    [ObservableProperty] private string _facilityType       = "NZOZ";
    [ObservableProperty] private string _nip                = string.Empty;
    [ObservableProperty] private string _regon14            = string.Empty;
    [ObservableProperty] private string _rpwdlBookNumber    = string.Empty;
    [ObservableProperty] private string _street             = string.Empty;
    [ObservableProperty] private string _postalCode         = string.Empty;
    [ObservableProperty] private string _city               = string.Empty;
    [ObservableProperty] private string _phone              = string.Empty;
    [ObservableProperty] private string _email              = string.Empty;

    // ── Kolumna 2: Integracja P1 ────────────────────────────────────────────

    [ObservableProperty] private bool   _useStub            = true;
    [ObservableProperty] private string _baseUrl            = string.Empty;
    [ObservableProperty] private string _subjectOid         = string.Empty;
    [ObservableProperty] private string _organizationalUnitCode = string.Empty;

    [ObservableProperty] private string _tlsCertificatePath    = string.Empty;
    [ObservableProperty] private string _tlsCertificatePassword = string.Empty;
    [ObservableProperty] private string _wssCertificatePath    = string.Empty;
    [ObservableProperty] private string _wssCertificatePassword = string.Empty;

    [ObservableProperty] private string _tlsCertStatus     = "–";
    [ObservableProperty] private string _wssCertStatus     = "–";
    [ObservableProperty] private bool   _tlsCertOk         = false;
    [ObservableProperty] private bool   _wssCertOk         = false;

    // ── Kolumna 3: Repozytorium EDM ────────────────────────────────────────

    [ObservableProperty] private string _edmRepositoryPath = "EdmRepository";
    [ObservableProperty] private string _edmRepositoryStatus = "–";
    [ObservableProperty] private bool   _edmRepositoryOk    = false;
    [ObservableProperty] private string _edmDocumentCount   = "–";

    // ── Wspólne ────────────────────────────────────────────────────────────

    [ObservableProperty] private string _statusMessage = "Wczytaj lub zapisz konfigurację.";
    [ObservableProperty] private bool   _isBusyLocal   = false;

    public FacilityDataViewModel(
        IOptions<P1Settings> p1Options,
        ILogger<FacilityDataViewModel> logger)
    {
        _p1Settings = p1Options.Value;
        _logger = logger;
        LoadFromRegistry();
    }

    // ── Ładowanie ──────────────────────────────────────────────────────────

    private void LoadFromRegistry()
    {
        try
        {
            // Dane placówki
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryBase))
            {
                if (key != null)
                {
                    OrganizationName  = key.GetValue("OrganizationName")  as string ?? _p1Settings.OrganizationName;
                    FacilityType      = key.GetValue("FacilityType")      as string ?? "NZOZ";
                    Nip               = key.GetValue("NIP")               as string ?? string.Empty;
                    Street            = key.GetValue("Street")            as string ?? string.Empty;
                    PostalCode        = key.GetValue("PostalCode")        as string ?? string.Empty;
                    City              = key.GetValue("City")              as string ?? string.Empty;
                    Phone             = key.GetValue("Phone")             as string ?? string.Empty;
                    Email             = key.GetValue("Email")             as string ?? string.Empty;
                }
                else
                {
                    OrganizationName = _p1Settings.OrganizationName;
                }
            }

            // P1 — z Registry lub fallback do appsettings
            using (var key = Registry.CurrentUser.OpenSubKey(RegistryP1))
            {
                UseStub                 = key?.GetValue("UseStub") is string s ? s == "1" : _p1Settings.UseStub;
                BaseUrl                 = key?.GetValue("BaseUrl")                 as string ?? _p1Settings.BaseUrl;
                SubjectOid              = key?.GetValue("SubjectOid")              as string ?? _p1Settings.SubjectOid;
                Regon14                 = key?.GetValue("Regon14")                 as string ?? _p1Settings.Regon14;
                RpwdlBookNumber         = key?.GetValue("RpwdlBookNumber")         as string ?? _p1Settings.RpwdlBookNumber;
                OrganizationalUnitCode  = key?.GetValue("OrganizationalUnitCode")  as string ?? _p1Settings.OrganizationalUnitCode;
                TlsCertificatePath          = key?.GetValue("TlsCertificatePath")          as string ?? _p1Settings.TlsCertificatePath;
                WssCertificatePath          = key?.GetValue("WssCertificatePath")          as string ?? _p1Settings.WssCertificatePath;
                EdmRepositoryPath           = key?.GetValue("EdmRepositoryPath")           as string ?? _p1Settings.EdmRepositoryPath;
            }

            RefreshCertStatus();
            RefreshEdmStatus();

            StatusMessage = "✅ Konfiguracja wczytana.";
            _logger.LogInformation("Wczytano dane placówki z rejestru.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd wczytywania danych placówki.");
            StatusMessage = $"⚠️ Błąd wczytywania: {ex.Message}";
        }
    }

    // ── Zapis ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Save()
    {
        try
        {
            IsBusyLocal = true;

            using (var key = Registry.CurrentUser.CreateSubKey(RegistryBase, true))
            {
                key.SetValue("OrganizationName", OrganizationName);
                key.SetValue("FacilityType",     FacilityType);
                key.SetValue("NIP",              Nip);
                key.SetValue("Street",           Street);
                key.SetValue("PostalCode",       PostalCode);
                key.SetValue("City",             City);
                key.SetValue("Phone",            Phone);
                key.SetValue("Email",            Email);
            }

            using (var key = Registry.CurrentUser.CreateSubKey(RegistryP1, true))
            {
                key.SetValue("UseStub",                UseStub ? "1" : "0");
                key.SetValue("BaseUrl",                BaseUrl);
                key.SetValue("SubjectOid",             SubjectOid);
                key.SetValue("Regon14",                Regon14);
                key.SetValue("RpwdlBookNumber",        RpwdlBookNumber);
                key.SetValue("OrganizationalUnitCode", OrganizationalUnitCode);
                key.SetValue("TlsCertificatePath",         TlsCertificatePath);
                key.SetValue("WssCertificatePath",         WssCertificatePath);
                key.SetValue("EdmRepositoryPath",          EdmRepositoryPath);
            }

            RefreshCertStatus();
            RefreshEdmStatus();

            StatusMessage = "✅ Zapisano konfigurację. Zmiany będą aktywne po restarcie aplikacji.";
            _logger.LogInformation("Zapisano dane placówki do rejestru.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd zapisu danych placówki.");
            StatusMessage = $"❌ Błąd zapisu: {ex.Message}";
        }
        finally
        {
            IsBusyLocal = false;
        }
    }

    // ── Przeglądaj pliki certyfikatów ──────────────────────────────────────

    [RelayCommand]
    private void BrowseTlsCert()
    {
        var path = BrowsePkcs12();
        if (path != null)
        {
            TlsCertificatePath = path;
            RefreshCertStatus();
        }
    }

    [RelayCommand]
    private void BrowseWssCert()
    {
        var path = BrowsePkcs12();
        if (path != null)
        {
            WssCertificatePath = path;
            RefreshCertStatus();
        }
    }

    [RelayCommand]
    private void BrowseEdmRepository()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Wybierz katalog repozytorium dokumentów EDM",
            InitialDirectory = Directory.Exists(EdmRepositoryPath) ? EdmRepositoryPath : AppContext.BaseDirectory
        };
        if (dialog.ShowDialog() == true)
        {
            EdmRepositoryPath = dialog.FolderName;
            RefreshEdmStatus();
        }
    }

    [RelayCommand]
    private void OpenEdmRepository()
    {
        try
        {
            var path = Path.IsPathRooted(EdmRepositoryPath)
                ? EdmRepositoryPath
                : Path.Combine(AppContext.BaseDirectory, EdmRepositoryPath);
            Directory.CreateDirectory(path);
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Nie można otworzyć katalogu: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RefreshStatus()
    {
        RefreshCertStatus();
        RefreshEdmStatus();
        StatusMessage = "🔄 Status odświeżony.";
    }

    // ── Pomocnicze ─────────────────────────────────────────────────────────

    private void RefreshCertStatus()
    {
        // TLS
        if (string.IsNullOrWhiteSpace(TlsCertificatePath))
        {
            TlsCertStatus = "Nie skonfigurowany";
            TlsCertOk = false;
        }
        else if (File.Exists(TlsCertificatePath))
        {
            var info = new FileInfo(TlsCertificatePath);
            TlsCertStatus = $"✅ OK — {info.Name} ({info.Length / 1024} KB)";
            TlsCertOk = true;
        }
        else
        {
            TlsCertStatus = "❌ Plik nie istnieje";
            TlsCertOk = false;
        }

        // WSS
        if (string.IsNullOrWhiteSpace(WssCertificatePath))
        {
            WssCertStatus = "Nie skonfigurowany";
            WssCertOk = false;
        }
        else if (File.Exists(WssCertificatePath))
        {
            var info = new FileInfo(WssCertificatePath);
            WssCertStatus = $"✅ OK — {info.Name} ({info.Length / 1024} KB)";
            WssCertOk = true;
        }
        else
        {
            WssCertStatus = "❌ Plik nie istnieje";
            WssCertOk = false;
        }
    }

    private void RefreshEdmStatus()
    {
        try
        {
            var path = Path.IsPathRooted(EdmRepositoryPath)
                ? EdmRepositoryPath
                : Path.Combine(AppContext.BaseDirectory, EdmRepositoryPath);

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories);
                EdmDocumentCount   = files.Length.ToString("N0");
                EdmRepositoryOk    = true;
                EdmRepositoryStatus = $"✅ Istnieje — {files.Length} dokumentów XML";
            }
            else
            {
                EdmDocumentCount    = "0";
                EdmRepositoryOk     = false;
                EdmRepositoryStatus = "⚠️ Katalog nie istnieje (zostanie utworzony przy pierwszym zapisie)";
            }
        }
        catch (Exception ex)
        {
            EdmRepositoryOk     = false;
            EdmRepositoryStatus = $"❌ Błąd: {ex.Message}";
            EdmDocumentCount    = "–";
        }
    }

    private static string? BrowsePkcs12()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Wybierz certyfikat (PKCS#12 lub PEM)",
            Filter = "Certyfikaty (*.pem;*.p12;*.pfx;*.crt;*.cer)|*.pem;*.p12;*.pfx;*.crt;*.cer|PEM (*.pem;*.crt;*.cer)|*.pem;*.crt;*.cer|PKCS#12 (*.p12;*.pfx)|*.p12;*.pfx|Wszystkie pliki (*.*)|*.*",
            CheckFileExists = true
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
