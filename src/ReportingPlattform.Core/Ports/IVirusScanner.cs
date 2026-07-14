namespace ReportingPlattform.Core.Ports;

public enum ScanResult { Clean, Infected, Unavailable }

/// <summary>
/// Port für den Pflicht-Virenscan beim Upload (Technische Doku § 9.6).
/// Adapter: ClamAV-Sidecar (on-prem/cloud) oder Defender for Storage (cloud).
/// Infizierte Dateien werden nie abgelegt.
/// </summary>
public interface IVirusScanner
{
    Task<ScanResult> ScanAsync(Stream content, CancellationToken ct = default);
}
