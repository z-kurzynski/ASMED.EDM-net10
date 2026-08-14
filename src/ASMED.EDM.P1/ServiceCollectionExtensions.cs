using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.P1.Cda;
using ASMED.EDM.P1.Clients;
using ASMED.EDM.P1.Configuration;
using ASMED.EDM.P1.Middleware;
using ASMED.EDM.P1.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace ASMED.EDM.P1;

/// <summary>
/// Rejestracja konfiguracji P1 (e-zdrowie) w kontenerze DI.
/// Implementacja usług P1 zostanie dodana w przyszłości.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje konfigurację P1Settings (appsettings + rejestr Windows).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddP1Integration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<P1Settings>(configuration.GetSection(P1Settings.SectionName));

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
                if (key.GetValue("MiddlewareBaseUrl")      is string vb) settings.MiddlewareBaseUrl      = vb;
                if (key.GetValue("MiddlewareUsername")     is string vc) settings.MiddlewareUsername     = vc;
                if (key.GetValue("MiddlewarePassword")     is string vd) settings.MiddlewarePassword     = vd;
                if (key.GetValue("DoctorPwz")               is string ve) settings.DoctorPwz              = ve;
            }
            catch { /* rejestr niedostępny — zostają wartości z appsettings */ }
        });

        return services;
    }
}

/// <summary>
/// Rejestracja usług P1 (serwisy CDA, podpis, stub/live klient).
/// </summary>
public static class P1ServicesExtensions
{
    /// <summary>
    /// Rejestruje klienta Middleware (logowanie + moduł MedycynaPracy) w kontenerze DI.
    /// Wymaga wcześniejszego wywołania <see cref="ServiceCollectionExtensions.AddP1Integration"/>.
    /// </summary>
    public static IServiceCollection AddMiddlewareIntegration(this IServiceCollection services)
    {
        services.AddTransient<MiddlewareEchoLoggingHandler>();

        services.AddHttpClient<IMiddlewareAuthTokenProvider, MiddlewareAuthTokenProvider>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<P1Settings>>().Value;
            client.BaseAddress = new Uri(EnsureTrailingSlash(GetMiddlewareBaseUrlOrThrow(settings)));
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<MiddlewareEchoLoggingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        });

        services.AddHttpClient<IMiddlewareMedycynaPracyClient, MiddlewareMedycynaPracyClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<P1Settings>>().Value;
            client.BaseAddress = new Uri(EnsureTrailingSlash(GetMiddlewareBaseUrlOrThrow(settings)));
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<MiddlewareEchoLoggingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        });

        return services;
    }

    private static string GetMiddlewareBaseUrlOrThrow(P1Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.MiddlewareBaseUrl))
        {
            throw new InvalidOperationException(
                "MiddlewareBaseUrl nie jest skonfigurowany. Uzupełnij go w appsettings.json (P1Settings:MiddlewareBaseUrl) " +
                "lub w ustawieniach \"Dane placówki\" (rejestr Software\\ASMED\\EDM\\P1).");
        }

        return settings.MiddlewareBaseUrl;
    }

    private static string EnsureTrailingSlash(string baseUrl) =>
        string.IsNullOrEmpty(baseUrl) ? baseUrl : baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
}
