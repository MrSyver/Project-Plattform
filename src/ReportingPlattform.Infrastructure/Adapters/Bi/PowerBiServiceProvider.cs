using System.Text.RegularExpressions;
using ReportingPlattform.Core.Ports;

namespace ReportingPlattform.Infrastructure.Adapters.Bi;

/// <summary>
/// Cloud-Adapter für Power BI Service (Embedded). Implementiert das Parsen einer eingefügten
/// Report-URL (§ 4.6). Der eigentliche Embed-Token-Flow (OBO „user owns data" bzw. Service
/// Principal, § 4.2) folgt in Phase 7 – hier als klar markierter Stub.
/// </summary>
public sealed partial class PowerBiServiceProvider : IBiProvider
{
    public string Kind => "PowerBiService";

    [GeneratedRegex(@"groups/(?<ws>[0-9a-fA-F-]{36}).*reports/(?<rep>[0-9a-fA-F-]{36})", RegexOptions.IgnoreCase)]
    private static partial Regex GroupReportRegex();

    [GeneratedRegex(@"reports/(?<rep>[0-9a-fA-F-]{36})", RegexOptions.IgnoreCase)]
    private static partial Regex ReportOnlyRegex();

    public BiReportRef ResolveLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Leerer Report-Link.", nameof(url));

        // "publish to web"-Links (…/view?r=…) sind öffentlich und werden abgelehnt (§ 4.6).
        if (url.Contains("/view?r=", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("„Publish to web\"-Links sind aus Sicherheitsgründen nicht erlaubt.");

        var m = GroupReportRegex().Match(url);
        if (m.Success)
            return new BiReportRef(m.Groups["ws"].Value, m.Groups["rep"].Value);

        var r = ReportOnlyRegex().Match(url);
        if (r.Success)
            return new BiReportRef(WorkspaceId: string.Empty, ReportId: r.Groups["rep"].Value);

        throw new InvalidOperationException("Aus dem Link ließen sich keine Report-IDs ermitteln.");
    }

    public Task<BiEmbedConfig> GetEmbedConfigAsync(BiReportRef report, EmbedContext ctx, CancellationToken ct = default)
    {
        // TODO Phase 7: ctx.IsEntraUser ? OBO/„user owns data" : Service-Principal/„app owns data".
        throw new NotImplementedException("Power-BI-Embed-Token-Flow wird in Phase 7 implementiert.");
    }
}
