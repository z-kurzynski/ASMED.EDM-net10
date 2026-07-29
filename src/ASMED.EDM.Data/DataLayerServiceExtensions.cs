using ASMED.EDM.Core.Configuration;
using ASMED.EDM.Core.Interfaces.Repositories;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.Core.Services;
using ASMED.EDM.Data.Repositories;
using ASMED.EDM.Data.Services;
using ASMED.EDM.Data.Services.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASMED.EDM.Data;

/// <summary>
/// Extension methods do konfiguracji Data Layer w DI
/// </summary>
public static class DataLayerServiceExtensions
{
    /// <summary>
    /// Rejestruje DbContext z MySQL i failover connection management
    /// </summary>
    public static IServiceCollection AddAsmedDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string? preloadedConnectionString = null)
    {
        // Rejestracja ustawień bazy danych
        var dbSettings = new DatabaseSettings();
        configuration.GetSection("DatabaseSettings").Bind(dbSettings);
        services.AddSingleton(Options.Create(dbSettings));

        // Rejestracja serwisu zarządzania połączeniami
        services.AddSingleton<IDatabaseConnectionService, DatabaseConnectionService>();

        // Rejestracja fabryki połączeń (Registry + appsettings.json fallback)
        services.AddSingleton<DbConnectionFactory>();

        // Rejestracja DbContext z dynamicznym connection stringiem (rejestr → failover)
        services.AddDbContext<AsmedDbContext>((serviceProvider, options) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<AsmedDbContext>>();
            var dbFactory = serviceProvider.GetRequiredService<DbConnectionFactory>();

            // Czyta ActiveConnectionType z rejestru → zwraca właściwy connection string
            // (ustawiony przez DatabaseConnectionService po ostatnim teście failoveru)
            var connectionString = preloadedConnectionString ?? dbFactory.ActiveConnectionString;

            logger.LogInformation(
                "Konfiguracja DbContext z połączeniem {ConnectionType}: {Server}",
                dbFactory.ActiveConnectionType,
                connectionString.Split(';').FirstOrDefault(p => p.StartsWith("Server", StringComparison.OrdinalIgnoreCase)) ?? "?");

            var serverVersion = new MySqlServerVersion(new Version(8, 4, 0));

            options.UseMySql(
                connectionString,
                serverVersion,
                mySqlOptions =>
                {
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);

                    mySqlOptions.CommandTimeout(30);
                })
                .EnableSensitiveDataLogging(false)
                .EnableDetailedErrors(true);
        });

        // Rejestracja Repository Pattern
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Rejestracja specjalistycznych repozytoriów
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
        services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Rejestracja serwisów domenowych
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IVisitService, VisitService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();

        // Rejestracja serwisu migracji danych (Access → MySQL)
        services.AddSingleton<IMigrationService, MigrationService>();

        return services;
    }
}


