using ReportingPlattform.Core.Domain;
using ReportingPlattform.Core.Services;
using Xunit;

namespace ReportingPlattform.Tests;

public class SetupValidationTests
{
    [Fact]
    public void Organization_requires_name_and_title()
    {
        Assert.NotNull(SetupValidation.Organization(new SetupSettings { OrganizationName = "", PlatformTitle = "X" }));
        Assert.NotNull(SetupValidation.Organization(new SetupSettings { OrganizationName = "Bank", PlatformTitle = " " }));
        Assert.Null(SetupValidation.Organization(new SetupSettings { OrganizationName = "Bank", PlatformTitle = "Portal" }));
    }

    [Fact]
    public void SqlServer_requires_connection_string()
    {
        var s = new SetupSettings { DatabaseProvider = "sqlserver", ConnectionString = null };
        Assert.NotNull(SetupValidation.Operations(s));

        s.ConnectionString = "Server=db;Database=RP;";
        Assert.Null(SetupValidation.Operations(s));
    }

    [Fact]
    public void Sqlite_needs_no_connection_string()
        => Assert.Null(SetupValidation.Operations(new SetupSettings { DatabaseProvider = "sqlite" }));

    [Theory]
    [InlineData("keine-email", "Sicher12345X", "Sicher12345X")]        // ungültige Mail
    [InlineData("a@b.de", "kurz1A", "kurz1A")]                          // zu kurz
    [InlineData("a@b.de", "alleskleingeschrieben1", "alleskleingeschrieben1")] // keine Großbuchstaben
    [InlineData("a@b.de", "OhneZiffernAber", "OhneZiffernAber")]        // keine Ziffer
    [InlineData("a@b.de", "Sicher12345X", "Anders12345X")]              // stimmt nicht überein
    public void Weak_or_mismatched_admin_credentials_are_rejected(string email, string pw, string repeat)
        => Assert.NotNull(SetupValidation.Administrator(email, pw, repeat));

    [Fact]
    public void Strong_matching_admin_credentials_pass()
        => Assert.Null(SetupValidation.Administrator("admin@kunde.de", "Sicher12345X", "Sicher12345X"));

    [Fact]
    public void File_settings_validate_size_and_extensions()
    {
        Assert.NotNull(SetupValidation.Files(0, new[] { "pdf" }));
        Assert.NotNull(SetupValidation.Files(5000, new[] { "pdf" }));
        Assert.NotNull(SetupValidation.Files(100, Array.Empty<string>()));
        Assert.NotNull(SetupValidation.Files(100, new[] { "*" }));
        Assert.Null(SetupValidation.Files(100, new[] { "pdf", "docx" }));
    }

    [Fact]
    public void SplitList_handles_mixed_separators_and_normalizes()
    {
        var r = SetupValidation.SplitList(".PDF, docx;xlsx\n txt  pdf");
        Assert.Equal(new[] { "pdf", "docx", "xlsx", "txt" }, r);
    }

    [Fact]
    public void SplitList_of_empty_is_empty()
        => Assert.Empty(SetupValidation.SplitList("  "));
}
