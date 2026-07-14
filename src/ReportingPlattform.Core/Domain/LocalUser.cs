namespace ReportingPlattform.Core.Domain;

/// <summary>
/// Lokaler Account (Fallback ohne AD, ADR-004/ADR-012). Passwörter nur als Hash
/// (ASP.NET Core PasswordHasher). Bei Auth-Mode "entra" bleibt diese Tabelle leer.
/// </summary>
public class LocalUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public PlatformRole Role { get; set; } = PlatformRole.Viewer;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
