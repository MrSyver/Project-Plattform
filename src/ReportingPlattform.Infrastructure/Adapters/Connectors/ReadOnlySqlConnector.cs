using ReportingPlattform.Core.Ports;

namespace ReportingPlattform.Infrastructure.Adapters.Connectors;

/// <summary>
/// Adapter für read-only Zugriff auf Kundendatenbanken (§ 4.3). Skelett: erzwingt bereits die
/// Nutzung parametrisierter Queries über die Signatur. Konkrete Ausführung inkl. Gateway/Direkt,
/// Row-/Time-Limits und Vault-Credentials folgt in Phase 8.
/// </summary>
public sealed class ReadOnlySqlConnector : IDbConnector
{
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        string connectorId,
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default)
    {
        // TODO Phase 8: Verbindung aus Vault, read-only, Row-/Time-Limits, ausschließlich parametrisiert.
        throw new NotImplementedException("DB-Gateway wird in Phase 8 implementiert.");
    }
}
