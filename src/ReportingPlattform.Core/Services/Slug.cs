using System.Text;

namespace ReportingPlattform.Core.Services;

/// <summary>URL-taugliche Slugs aus Namen (deutsch-freundlich: ä→ae usw.).</summary>
public static class Slug
{
    public static string From(string name)
    {
        var s = name.Trim().ToLowerInvariant()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
        var sb = new StringBuilder(s.Length);
        var lastDash = true;
        foreach (var c in s)
        {
            if (char.IsAsciiLetterOrDigit(c)) { sb.Append(c); lastDash = false; }
            else if (!lastDash) { sb.Append('-'); lastDash = true; }
        }
        return sb.ToString().TrimEnd('-') is { Length: > 0 } r ? r : "seite";
    }
}
