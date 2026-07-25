using ReportingPlattform.Core.Ports;

namespace ReportingPlattform.Infrastructure.Adapters.Bi;

/// <summary>
/// On-Prem-Adapter: Power BI Report Server (PBIRS). Eingebettet wird per iframe mit
/// <c>?rs:embed=true</c>; SSO läuft über Integrated Windows Auth / Kerberos des Browsers (§ 4.2c).
/// Zulässig sind ausschließlich Report-Pfade unterhalb der konfigurierten Basis-URL —
/// dadurch kann über den Block kein fremder Inhalt eingebettet werden.
/// </summary>
public sealed class PowerBiReportServerProvider : IBiProvider
{
    private readonly Uri _baseUrl;

    public PowerBiReportServerProvider(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Ungültige Basis-URL für den Report Server.", nameof(baseUrl));
        _baseUrl = uri;
    }

    public string Kind => "PowerBiReportServer";

    /// <summary>Nimmt eine PBIRS-Report-URL und liefert deren Report-Pfad als ReportId.</summary>
    public BiReportRef ResolveLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Leerer Report-Link.", nameof(url));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Der Link ist keine gültige Adresse.");

        if (!string.Equals(uri.Host, _baseUrl.Host, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Nur Reports von {_baseUrl.Host} sind erlaubt.");

        var path = uri.AbsolutePath;
        if (string.IsNullOrWhiteSpace(path) || path == "/")
            throw new InvalidOperationException("Aus dem Link ließ sich kein Report-Pfad ermitteln.");

        return new BiReportRef(WorkspaceId: string.Empty, ReportId: path);
    }

    public Task<BiEmbedConfig> GetEmbedConfigAsync(BiReportRef report, EmbedContext ctx, CancellationToken ct = default)
    {
        // Pfad neu an der Basis-URL aufhängen — nie die Eingabe direkt verwenden.
        var builder = new UriBuilder(_baseUrl) { Path = report.ReportId, Query = "rs:embed=true" };
        return Task.FromResult(new BiEmbedConfig(builder.Uri.ToString(), Mode: "iframe"));
    }
}
