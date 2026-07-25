using ReportingPlattform.Infrastructure.Adapters.Bi;
using Xunit;

namespace ReportingPlattform.Tests;

public class PowerBiLinkResolverTests
{
    private readonly PowerBiServiceProvider _bi = new();

    [Fact]
    public void Parses_workspace_and_report_id_from_group_url()
    {
        var url = "https://app.powerbi.com/groups/11111111-1111-1111-1111-111111111111/reports/22222222-2222-2222-2222-222222222222/ReportSection";
        var r = _bi.ResolveLink(url);
        Assert.Equal("11111111-1111-1111-1111-111111111111", r.WorkspaceId);
        Assert.Equal("22222222-2222-2222-2222-222222222222", r.ReportId);
    }

    [Fact]
    public void Rejects_publish_to_web_links()
    {
        var url = "https://app.powerbi.com/view?r=eyJrIjoi_public_token";
        Assert.Throws<InvalidOperationException>(() => _bi.ResolveLink(url));
    }

    [Fact]
    public void Throws_on_unparseable_link()
    {
        Assert.Throws<InvalidOperationException>(() => _bi.ResolveLink("https://example.com/not-a-report"));
    }

    [Theory]
    // Fremde Domain mit report-ähnlichem Pfad: darf NICHT stillschweigend als Report gelesen werden
    [InlineData("https://angreifer.example.com/reports/33333333-3333-3333-3333-333333333333")]
    // Täuschende Hostnamen
    [InlineData("https://app.powerbi.com.angreifer.example/reports/33333333-3333-3333-3333-333333333333")]
    [InlineData("https://notpowerbi.com/reports/33333333-3333-3333-3333-333333333333")]
    // Unverschlüsselt
    [InlineData("http://app.powerbi.com/reports/33333333-3333-3333-3333-333333333333")]
    public void Rejects_links_outside_power_bi(string url)
        => Assert.Throws<InvalidOperationException>(() => _bi.ResolveLink(url));

    [Fact]
    public void Accepts_power_bi_subdomains()
    {
        var r = _bi.ResolveLink("https://msit.powerbi.com/groups/11111111-1111-1111-1111-111111111111/reports/22222222-2222-2222-2222-222222222222");
        Assert.Equal("22222222-2222-2222-2222-222222222222", r.ReportId);
    }
}
