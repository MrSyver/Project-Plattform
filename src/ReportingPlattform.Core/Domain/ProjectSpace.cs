namespace ReportingPlattform.Core.Domain;

/// <summary>
/// Ein Projektraum: oberste Struktureinheit mit eigenen Seiten, eigener
/// Zugriffsliste (ACL) und eigener Dateibibliothek. Siehe Technische Doku § 2.4.
/// </summary>
public class ProjectSpace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Frei kombinierbare Zugriffseinträge: Gruppe, interner Nutzer, externer Gast.</summary>
    public List<AccessEntry> AccessList { get; set; } = new();

    public List<Page> Pages { get; set; } = new();
}

/// <summary>Ein Eintrag der Projektraum-ACL (Subjekt + Rolle im Raum).</summary>
public class AccessEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectSpaceId { get; set; }

    public AccessSubjectType SubjectType { get; set; }

    /// <summary>Group-Object-Id, E-Mail (Nutzer/Gast) o. ä. – je nach SubjectType.</summary>
    public string Subject { get; set; } = string.Empty;

    public ProjectRole Role { get; set; } = ProjectRole.Betrachter;
}
