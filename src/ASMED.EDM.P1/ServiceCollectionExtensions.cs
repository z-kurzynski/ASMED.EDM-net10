using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.P1.Cda;
using ASMED.EDM.P1.Client;
using ASMED.EDM.P1.Clients;
using ASMED.EDM.P1.Configuration;
using ASMED.EDM.P1.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace ASMED.EDM.P1;

/// <summary>
/// Rejestracja usług integracji z systemem P1 (e-zdrowie) w kontenerze DI
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje integrację P1: konfigurację, klientów FHIR (lub stub wg P1Settings:UseStub),
    /// generator CDA, podpis oraz lokalne repozytorium EDM.
    /// </summary>
    public static IServiceCollection AddP1Integration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<P1Settings>(configuration.GetSection(P1Settings.SectionName));

        // Nadpisz P1Settings wartościami z rejestru Windows (zapisanymi w zakładce Placówka)
        // Rejestr ma wyższy priorytet niż appsettings.json
        services.PostConfigure<P1Settings>(settings =>
        {
            const string regKey = @"Software\ASMED\EDM\P1";
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(regKey);
                if (key is null) return;

                if (key.GetValue("UseStub") is string useStubStr)
                    settings.UseStub = useStubStr == "1";
                if (key.GetValue("BaseUrl")                is string v1) settings.BaseUrl                = v1;
                if (key.GetValue("SubjectOid")             is string v2) settings.SubjectOid             = v2;
                if (key.GetValue("Regon14")                is string v3) settings.Regon14                = v3;
                if (key.GetValue("RpwdlBookNumber")        is string v4) settings.RpwdlBookNumber        = v4;
                if (key.GetValue("OrganizationalUnitCode") is string v5) settings.OrganizationalUnitCode = v5;
                if (key.GetValue("TlsCertificatePath")     is string v6) settings.TlsCertificatePath     = v6;
                if (key.GetValue("TlsCertificatePassword") is string v7) settings.TlsCertificatePassword = v7;
                if (key.GetValue("WssCertificatePath")     is string v8) settings.WssCertificatePath     = v8;
                if (key.GetValue("WssCertificatePassword") is string v9) settings.WssCertificatePassword = v9;
                if (key.GetValue("EdmRepositoryPath")      is string va) settings.EdmRepositoryPath      = va;
            }
            catch { /* rejestr niedostępny — zostają wartości z appsettings */ }
        });

        services.AddSingleton<P1Client>();
        services.AddSingleton<CdaOrzeczenieGenerator>();
        services.AddSingleton<XadesSigner>();
        services.AddTransient<IEdmDocumentService, EdmDocumentService>();

        // Odczytaj UseStub z rejestru (priorytet) lub appsettings.json
        bool useStub;
        try
        {
            using var regKey2 = Registry.CurrentUser.OpenSubKey(@"Software\ASMED\EDM\P1");
            if (regKey2?.GetValue("UseStub") is string regStubStr)
                useStub = regStubStr == "1";
            else
                useStub = configuration.GetValue($"{P1Settings.SectionName}:UseStub", defaultValue: true);
        }
        catch
        {
            useStub = configuration.GetValue($"{P1Settings.SectionName}:UseStub", defaultValue: true);
        }

        if (useStub)
        {
            services.AddSingleton<StubP1Client>();
            services.AddSingleton<IP1PatientVerificationService>(sp => sp.GetRequiredService<StubP1Client>());
            services.AddSingleton<IMedicalEventService>(sp => sp.GetRequiredService<StubP1Client>());
        }
        else
        {
            services.AddSingleton<IP1PatientVerificationService, P1PatientService>();
            services.AddSingleton<IMedicalEventService, P1EncounterService>();
        }

        return services;
    }
}
