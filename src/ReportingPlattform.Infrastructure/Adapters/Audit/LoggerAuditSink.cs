using Microsoft.Extensions.Logging;
using ReportingPlattform.Core.Ports;

namespace ReportingPlattform.Infrastructure.Adapters.Audit;

/// <summary>
/// Adapter für <see cref="IAuditSink"/>, der strukturiert loggt. In Prod hängt an diesem
/// Logger die Ausleitung ins SIEM (Azure Monitor bzw. Syslog). Technische Doku § 9.4.
/// </summary>
public sealed class LoggerAuditSink : IAuditSink
{
    private readonly ILogger<LoggerAuditSink> _log;
    public LoggerAuditSink(ILogger<LoggerAuditSink> log) => _log = log;

    public Task WriteAsync(AuditEvent evt, CancellationToken ct = default)
    {
        _log.LogInformation("AUDIT {At:o} actor={Actor} action={Action} resource={Resource} detail={Detail}",
            evt.At, evt.Actor, evt.Action, evt.Resource, evt.Detail);
        return Task.CompletedTask;
    }
}
