using ReportingPlattform.Core.Ports;
using ReportingPlattform.Infrastructure.Adapters.Bi;
using Xunit;

namespace ReportingPlattform.Tests;

public class BiEmbedTests
{
    private static readonly EmbedContext Ctx = new("user@kunde.de", IsEntraUser: false, RlsRoles: Array.Empty<string>());
    private const string Ws = "11111111-1111-1111-1111-111111111111";
    private const string Rep = "22222222-2222-2222-2222-222222222222";

    // ---- Power BI Service (Cloud) ----

    [Fact]
    public async Task Builds_secure_embed_url_from_ids()
    {
        var bi = new PowerBiServiceProvider();
        var cfg = await bi.GetEmbedConfigAsync(new BiReportRef(Ws, Rep), Ctx);

        Assert.Equal("iframe", cfg.Mode);
        Assert.StartsWith("https://app.powerbi.com/reportEmbed?", cfg.EmbedUrl);
        Assert.Contains($"reportId={Rep}", cfg.EmbedUrl);
        Assert.Contains($"groupId={Ws}", cfg.EmbedUrl);
        // Secure Embed: niemals der öffentliche publish-to-web-Pfad
        Assert.DoesNotContain("/view?r=", cfg.EmbedUrl);
    }

    [Fact]
    public async Task Omits_group_when_workspace_unknown()
    {
        var bi = new PowerBiServiceProvider();
        var cfg = await bi.GetEmbedConfigAsync(new BiReportRef("", Rep), Ctx);
        Assert.DoesNotContain("groupId", cfg.EmbedUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("../../evil")]
    [InlineData("")]
    public async Task Rejects_non_guid_report_ids(string reportId)
    {
        var bi = new PowerBiServiceProvider();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => bi.GetEmbedConfigAsync(new BiReportRef(Ws, reportId), Ctx));
    }

    [Fact]
    public async Task Malicious_workspace_value_is_ignored_not_injected()
    {
        var bi = new PowerBiServiceProvider();
        var cfg = await bi.GetEmbedConfigAsync(new BiReportRef("evil\" onload=x", Rep), Ctx);
        Assert.DoesNotContain("onload", cfg.EmbedUrl);
    }

    // ---- Power BI Report Server (On-Prem) ----

    [Fact]
    public async Task ReportServer_builds_embed_url_under_configured_host()
    {
        var bi = new PowerBiReportServerProvider("https://pbirs.kunde.local/Reports");
        var reference = bi.ResolveLink("https://pbirs.kunde.local/Reports/powerbi/Finanzen/Quartal");
        var cfg = await bi.GetEmbedConfigAsync(reference, Ctx);

        Assert.StartsWith("https://pbirs.kunde.local/", cfg.EmbedUrl);
        Assert.Contains("rs:embed=true", cfg.EmbedUrl);
    }

    [Fact]
    public void ReportServer_rejects_foreign_hosts()
    {
        var bi = new PowerBiReportServerProvider("https://pbirs.kunde.local/Reports");
        Assert.Throws<InvalidOperationException>(() => bi.ResolveLink("https://angreifer.example.com/Reports/x"));
    }

    [Fact]
    public void ReportServer_requires_valid_base_url()
        => Assert.Throws<ArgumentException>(() => new PowerBiReportServerProvider("nicht-mal-eine-url"));
}
