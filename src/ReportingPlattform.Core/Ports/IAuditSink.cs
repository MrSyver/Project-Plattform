namespace ReportingPlattform.Core.Ports;

/// <summary>Ein manipulationssicher zu protokollierendes Ereignis (Technische Doku § 9.4).</summary>
public record AuditEvent(
    DateTimeOffset At,
    string Actor,
    string Action,
    string Resource,
    string? Detail = null);

/// <summary>
/// Port für das append-only Audit-Log. Cloud-Adapter: Azure Monitor/Log Analytics;
/// On-Prem-Adapter: Syslog → Kunden-SIEM. Jeder Datenzugriff wird protokolliert.
/// </summary>
public interface IAuditSink
{
    Task WriteAsync(AuditEvent evt, CancellationToken ct = default);
}
