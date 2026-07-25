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
        services.AddScoped<Files.FileLibraryService>();

        // Upload-Regeln: Typ-Whitelist + Größen-Limit (§ 9.6).
        // Scoped + Live-Lesen aus setup.json, damit Änderungen des Einrichtungs-Assistenten
        // sofort greifen (ohne Neustart).
        services.AddScoped(sp =>
        {
            var saved = sp.GetService<Setup.SetupService>()?.Load();
            var ext = saved is { IsCompleted: true, AllowedExtensions.Count: > 0 }
                ? saved.AllowedExtensions.ToArray()
                : config.GetSection("Files:AllowedExtensions").Get<string[]>()
                  ?? new[] { "pdf", "docx", "xlsx", "pptx", "csv", "txt", "md", "png", "jpg", "jpeg", "zip" };
            var maxMb = saved is { IsCompleted: true } && saved.MaxFileSizeMb > 0
                ? saved.MaxFileSizeMb
                : (int)(config.GetValue<long?>("Files:MaxSizeMb") ?? 100);
            return new Core.Services.FileValidation(ext, maxMb * 1024L * 1024L);
        });

        services.AddSingleton<ISecretStore, EnvSecretStore>();

        // Achtung: leere Config-Werte sind "" (nicht null) → IsNullOrWhiteSpace prüfen, nicht ??.
        var configuredPath = config["Storage:LocalPath"];
        var storagePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "data", "files")
            : configuredPath;
        services.AddSingleton<IBlobStore>(_ => new LocalFileBlobStore(storagePath));

        services.AddSingleton<IVirusScanner, DevPermissiveVirusScanner>();
        services.AddSingleton<IAuditSink, LoggerAuditSink>();
        services.AddSingleton<IDbConnector, ReadOnlySqlConnector>();

        // BI-Adapter nach Konfiguration: Cloud = Power BI Service, On-Prem = Report Server (§ 8.1).
        services.AddScoped<IBiProvider>(sp =>
        {
            var saved = sp.GetService<Setup.SetupService>()?.Load();
            var kind = saved is { IsCompleted: true } && !string.IsNullOrWhiteSpace(saved.BiProvider)
                ? saved.BiProvider
                : config["Bi:Provider"] ?? "PowerBiService";

            if (string.Equals(kind, "PowerBiReportServer", StringComparison.OrdinalIgnoreCase))
            {
                var baseUrl = config["Bi:ReportServerUrl"];
                if (!string.IsNullOrWhiteSpace(baseUrl))
                    return new PowerBiReportServerProvider(baseUrl);
                // Ohne Basis-URL ist der On-Prem-Adapter nicht nutzbar → Cloud-Adapter als Rückfall.
            }
            return new PowerBiServiceProvider();
        });

        return services;
    }
}
