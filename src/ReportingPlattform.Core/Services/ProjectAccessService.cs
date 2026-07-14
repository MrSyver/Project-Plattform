using ReportingPlattform.Core.Domain;

namespace ReportingPlattform.Core.Services;

/// <summary>Auth-Kontext des angemeldeten Nutzers, provider-unabhängig (Entra oder lokal).</summary>
public record UserContext(
    string? Email,
    IReadOnlyCollection<string> PlatformRoles,
    IReadOnlyCollection<string> GroupIds,
    bool CanUseEditor);

/// <summary>
/// Zweistufige Autorisierung (Technische Doku § 2.4, ADR-011):
/// App Role = grobe Fähigkeit, Projekt-ACL = welcher Raum mit welcher Rolle.
/// </summary>
public sealed class ProjectAccessService
{
    /// <summary>Admin/Auditor sehen alles; sonst entscheidet die ACL des Raums.</summary>
    public bool CanView(UserContext u, ProjectSpace p)
        => HasRole(u, PlatformRole.Admin) || HasRole(u, PlatformRole.Auditor) || EffectiveRole(u, p) is not null;

    /// <summary>
    /// Admin darf immer; sonst Editor-Fähigkeit (Allowlist ODER Editor-Rolle, ADR-018)
    /// UND Projektrolle mindestens „Beitragender".
    /// </summary>
    public bool CanEdit(UserContext u, ProjectSpace p)
    {
        if (HasRole(u, PlatformRole.Admin)) return true;
        var role = EffectiveRole(u, p);
        return u.CanUseEditor && role is ProjectRole.Beitragender or ProjectRole.Owner;
    }

    /// <summary>Höchste Projektrolle über alle passenden ACL-Einträge (E-Mail bzw. Gruppen-Id).</summary>
    public ProjectRole? EffectiveRole(UserContext u, ProjectSpace p)
    {
        ProjectRole? best = null;
        foreach (var e in p.AccessList)
        {
            var match = e.SubjectType switch
            {
                AccessSubjectType.SecurityGroup => u.GroupIds.Contains(e.Subject, StringComparer.OrdinalIgnoreCase),
                AccessSubjectType.InternalUser or AccessSubjectType.ExternalGuest =>
                    u.Email is not null && string.Equals(u.Email, e.Subject, StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
            if (match && (best is null || e.Role > best)) best = e.Role;
        }
        return best;
    }

    private static bool HasRole(UserContext u, PlatformRole role)
        => u.PlatformRoles.Contains(role.ToString(), StringComparer.OrdinalIgnoreCase);
}
