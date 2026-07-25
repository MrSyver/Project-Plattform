namespace ReportingPlattform.Core.Ports;

/// <summary>Referenz auf einen Report (aus Picker oder aus eingefügtem Link geparst).</summary>
public record BiReportRef(string WorkspaceId, string ReportId);

/// <summary>Kontext für die Embed-Strategie-Wahl (Technische Doku § 4.2).</summary>
public record EmbedContext(string? UserId, bool IsEntraUser, IReadOnlyList<string> RlsRoles);

/// <summary>
/// Fertige Embed-Konfiguration für das Frontend.
/// <paramref name="EmbedUrl"/> wird IMMER serverseitig aus den IDs gebaut — nie aus einer
/// Nutzereingabe übernommen (sonst ließe sich beliebiger Fremdinhalt einbetten).
/// <paramref name="Mode"/>: "iframe" (Secure Embed, Identität des angemeldeten Nutzers)
/// oder "js" (Embed-Token via OBO/Service Principal, spätere Ausbaustufe).
/// </summary>
public record BiEmbedConfig(string EmbedUrl, string Mode, string? Token = null, string? TokenType = null);

/// <summary>
/// Port für Power BI. Cloud-Adapter: Power BI Service (Embedded, OBO/SP);
/// On-Prem-Adapter: Power BI Report Server (Kerberos/URL). Technische Doku § 4.2 / § 8.1.
/// </summary>
public interface IBiProvider
{
    /// <summary>"PowerBiService" | "PowerBiReportServer".</summary>
    string Kind { get; }

    /// <summary>
    /// Parst eine eingefügte Report-URL zu <see cref="BiReportRef"/>. Der Link dient NUR der
    /// Id-Ermittlung – gerendert wird immer über den sicheren Embed-Flow (nie „publish to web").
    /// </summary>
    BiReportRef ResolveLink(string url);

    /// <summary>Liefert die sichere Embed-Konfiguration; wählt intern SSO/OBO bzw. App-owns-data.</summary>
    Task<BiEmbedConfig> GetEmbedConfigAsync(BiReportRef report, EmbedContext ctx, CancellationToken ct = default);
}
