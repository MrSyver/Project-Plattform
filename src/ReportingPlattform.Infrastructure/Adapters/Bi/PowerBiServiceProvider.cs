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

        // Nur Links aus dem Power-BI-Dienst annehmen. Ohne diese Prüfung würde aus einem
        // Fremd-Link stillschweigend eine Report-Id gelesen und ein anderer Report angezeigt.
        if (!IsPowerBiHost(url))
            throw new InvalidOperationException("Nur Report-Links aus dem Power-BI-Dienst (powerbi.com) sind erlaubt.");

        var m = GroupReportRegex().Match(url);
        if (m.Success)
            return new BiReportRef(m.Groups["ws"].Value, m.Groups["rep"].Value);

        var r = ReportOnlyRegex().Match(url);
        if (r.Success)
            return new BiReportRef(WorkspaceId: string.Empty, ReportId: r.Groups["rep"].Value);

        throw new InvalidOperationException("Aus dem Link ließen sich keine Report-IDs ermitteln.");
    }

    /// <summary>
    /// Aktuell: <b>Secure Embed</b> („Einbetten für die Organisation") per iframe — der Report
    /// rendert mit der Identität des im Browser angemeldeten Power-BI-Nutzers, dessen
    /// Berechtigungen und RLS greifen. Kein Service Principal, kein „publish to web".
    /// Die URL wird ausschließlich aus den validierten IDs gebaut, nie aus der Eingabe.
    /// Später (Phase 7): OBO-Token für vollen JS-Embed bzw. App-owns-data für Gäste.
    /// </summary>
    public Task<BiEmbedConfig> GetEmbedConfigAsync(BiReportRef report, EmbedContext ctx, CancellationToken ct = default)
    {
        if (!IsGuid(report.ReportId))
            throw new InvalidOperationException("Ungültige Report-Id.");

        var url = $"https://app.powerbi.com/reportEmbed?reportId={report.ReportId}";
        if (IsGuid(report.WorkspaceId))
            url += $"&groupId={report.WorkspaceId}";

        return Task.FromResult(new BiEmbedConfig(url, Mode: "iframe"));
    }

    private static bool IsGuid(string? value) => Guid.TryParse(value, out _);

    /// <summary>Akzeptiert nur https-Links auf powerbi.com (inkl. Subdomains wie app./msit.).</summary>
    private static bool IsPowerBiHost(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        var host = uri.Host;
        return host.Equals("powerbi.com", StringComparison.OrdinalIgnoreCase)
               || host.EndsWith(".powerbi.com", StringComparison.OrdinalIgnoreCase);
    }
}
