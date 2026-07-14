using Microsoft.Extensions.Logging;
using ReportingPlattform.Core.Ports;

namespace ReportingPlattform.Infrastructure.Adapters.Antivirus;

/// <summary>
/// ⚠️ NUR ENTWICKLUNG: gibt jede Datei als „clean" durch. Für Betrieb ZWINGEND durch den
/// ClamAV-Sidecar-Adapter ersetzen (Technische Doku § 9.6). Loggt bei jedem Aufruf eine Warnung,
/// damit dieser Stub nicht versehentlich produktiv läuft.
/// </summary>
public sealed class DevPermissiveVirusScanner : IVirusScanner
{
    private readonly ILogger<DevPermissiveVirusScanner> _log;
    public DevPermissiveVirusScanner(ILogger<DevPermissiveVirusScanner> log) => _log = log;

    public Task<ScanResult> ScanAsync(Stream content, CancellationToken ct = default)
    {
        _log.LogWarning("DevPermissiveVirusScanner aktiv – KEIN echter Virenscan! Nur für Entwicklung.");
        return Task.FromResult(ScanResult.Clean);
    }
}
