using ReportingPlattform.Core.Services;
using Xunit;

namespace ReportingPlattform.Tests;

public class FileValidationTests
{
    private static readonly FileValidation V = new(
        new[] { "pdf", ".docx", "XLSX", "txt", "png", "zip" },
        maxSizeBytes: 10 * 1024 * 1024);

    [Theory]
    [InlineData("bericht.pdf")]
    [InlineData("Daten.XLSX")]     // case-insensitive
    [InlineData("notiz.txt")]
    public void Allowed_extensions_pass(string name)
        => Assert.Null(V.Check(name, 1024));

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("script.sh")]
    [InlineData("ohne-endung")]
    public void Disallowed_or_missing_extensions_fail(string name)
        => Assert.NotNull(V.Check(name, 1024));

    [Fact]
    public void Oversized_file_fails()
        => Assert.NotNull(V.Check("gross.pdf", 11 * 1024 * 1024));

    [Fact]
    public void Empty_file_fails()
        => Assert.NotNull(V.Check("leer.pdf", 0));
}
