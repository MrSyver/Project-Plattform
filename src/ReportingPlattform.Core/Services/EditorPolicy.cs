namespace ReportingPlattform.Core.Services;

/// <summary>
/// Entscheidet, wer den Bearbeitungsmodus nutzen darf (ADR-018):
/// <c>Editor = in E-Mail-Allowlist ODER Editor-App-Role</c>.
/// Es zählt nur die verifizierte E-Mail aus dem IdP-Claim (Spoofing-Schutz, § 11).
/// </summary>
public sealed class EditorPolicy
{
    private readonly HashSet<string> _allowlist;

    public EditorPolicy(IEnumerable<string> allowlistEmails)
    {
        _allowlist = new HashSet<string>(
            allowlistEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(Normalize),
            StringComparer.OrdinalIgnoreCase);
    }

    public bool CanEdit(string? verifiedEmail, IEnumerable<string> platformRoles)
    {
        var byAllowlist = verifiedEmail is not null && _allowlist.Contains(Normalize(verifiedEmail));
        var byRole = platformRoles.Any(r => string.Equals(r, "Editor", StringComparison.OrdinalIgnoreCase));
        return byAllowlist || byRole;
    }

    public IReadOnlyCollection<string> Allowlist => _allowlist;

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
