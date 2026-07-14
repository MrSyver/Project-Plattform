using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReportingPlattform.Core.Ports;
using ReportingPlattform.Infrastructure.Adapters.Antivirus;
using ReportingPlattform.Infrastructure.Adapters.Audit;
using ReportingPlattform.Infrastructure.Adapters.Bi;
using ReportingPlattform.Infrastructure.Adapters.Connectors;
using ReportingPlattform.Infrastructure.Adapters.Secrets;
using ReportingPlattform.Infrastructure.Adapters.Storage;
using ReportingPlattform.Infrastructure.Data;

namespace ReportingPlattform.Infrastructure.DependencyInjection;

/// <summary>
/// Bindet die Ports (Core) an konkrete Adapter (Ports &amp; Adapters, ADR-015).
/// Welche Adapter geladen werden, steuert die Konfiguration – gleiches Image,
/// andere <c>.env</c> je Deployment-Pfad (§ 8).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportingInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // App-DB: Provider per Config — "sqlserver" (Prod, § 8.1) oder "sqlite" (lokale Entwicklung).
        var provider = config["Database:Provider"] ?? "sqlite";
        var appDb = config.GetConnectionString("AppDb");
        if (string.Equals(provider, "sqlserver", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(appDb))
        {
            services.AddDbContext<AppDbContext>(o => o.UseSqlServer(appDb));
        }
        else
        {
            var dbPath = string.IsNullOrWhiteSpace(appDb)
                ? Path.Combine(AppContext.BaseDirectory, "data", "app.db")
                : appDb;
            if (!dbPath.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
                dbPath = $"Data Source={dbPath}";
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "data"));
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(dbPath));
        }

        services.AddScoped<Auth.LocalUserService>();

        services.AddSingleton<ISecretStore, EnvSecretStore>();

        var storagePath = config["Storage:LocalPath"] ?? Path.Combine(AppContext.BaseDirectory, "data", "files");
        services.AddSingleton<IBlobStore>(_ => new LocalFileBlobStore(storagePath));

        services.AddSingleton<IVirusScanner, DevPermissiveVirusScanner>();
        services.AddSingleton<IAuditSink, LoggerAuditSink>();
        services.AddSingleton<IDbConnector, ReadOnlySqlConnector>();

        // BI-Adapter nach Konfiguration (Cloud: PowerBiService | On-Prem: PowerBiReportServer folgt).
        var biProvider = config["Bi:Provider"] ?? "PowerBiService";
        services.AddSingleton<IBiProvider>(_ => biProvider switch
        {
            "PowerBiService" => new PowerBiServiceProvider(),
            _ => new PowerBiServiceProvider(),
        });

        return services;
    }
}
