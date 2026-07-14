namespace ReportingPlattform.Core.Ports;

/// <summary>
/// Port für den read-only Zugriff auf Kundendatenbanken (Technische Doku § 4.3).
/// Cloud: über On-prem Data Gateway; On-Prem: direkt im Netz.
/// Ausschließlich parametrisierte Queries; Row-/Time-Limits im Adapter.
/// </summary>
public interface IDbConnector
{
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        string connectorId,
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default);
}
