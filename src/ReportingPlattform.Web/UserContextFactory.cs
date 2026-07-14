using System.Security.Claims;
using ReportingPlattform.Core.Services;

namespace ReportingPlattform.Web;

/// <summary>Baut den provider-unabhängigen <see cref="UserContext"/> aus den Claims (Entra oder lokal).</summary>
public static class UserContextFactory
{
    public static UserContext From(ClaimsPrincipal principal, EditorPolicy editorPolicy)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        // Entra liefert Gruppen-Ids im "groups"-Claim (nur der App zugewiesene Gruppen, § 2.4).
        var groups = principal.FindAll("groups").Select(c => c.Value).ToArray();
        return new UserContext(email, roles, groups, editorPolicy.CanEdit(email, roles));
    }
}
