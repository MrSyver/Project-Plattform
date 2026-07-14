using ReportingPlattform.Core.Services;
using Xunit;

namespace ReportingPlattform.Tests;

public class EditorPolicyTests
{
    private static readonly string[] Allowlist = { "Max@Kunde.de", " admin@kunde.de " };
    private static readonly string[] NoRoles = Array.Empty<string>();

    [Fact]
    public void Email_in_allowlist_may_edit_case_insensitive_and_trimmed()
    {
        var policy = new EditorPolicy(Allowlist);
        Assert.True(policy.CanEdit("max@kunde.de", NoRoles));
        Assert.True(policy.CanEdit("ADMIN@KUNDE.DE", NoRoles));
    }

    [Fact]
    public void Editor_role_may_edit_even_without_allowlist_entry()
    {
        var policy = new EditorPolicy(Allowlist);
        Assert.True(policy.CanEdit("someone.else@kunde.de", new[] { "Editor" }));
    }

    [Fact]
    public void Unknown_email_without_role_may_not_edit()
    {
        var policy = new EditorPolicy(Allowlist);
        Assert.False(policy.CanEdit("intruder@evil.com", new[] { "Viewer" }));
    }

    [Fact]
    public void Null_email_without_role_may_not_edit()
    {
        var policy = new EditorPolicy(Allowlist);
        Assert.False(policy.CanEdit(null, NoRoles));
    }
}
