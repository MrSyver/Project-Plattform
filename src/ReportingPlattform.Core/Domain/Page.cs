namespace ReportingPlattform.Core.Domain;

/// <summary>
/// Eine Projektseite. Aufbau: Seite → Zonen → Blöcke (Technische Doku § 4.4).
/// Bearbeitung läuft auf einem Entwurf, „Veröffentlichen" schaltet live.
/// </summary>
public class Page
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectSpaceId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public PublishState State { get; set; } = PublishState.Draft;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Zone> Zones { get; set; } = new();
}

/// <summary>Ein horizontaler Abschnitt einer Seite, enthält Blöcke.</summary>
public class Zone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<ContentBlock> Blocks { get; set; } = new();
}

/// <summary>
/// Ein Baustein. Der typ-spezifische Inhalt/Config liegt als JSON in <see cref="ConfigJson"/>,
/// damit neue Blocktypen ohne Schema-Änderung ergänzt werden können (Plugin-Registry).
/// </summary>
public class ContentBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ZoneId { get; set; }
    public BlockType Type { get; set; }
    public int Order { get; set; }

    /// <summary>Block-spezifische Konfiguration als JSON (z. B. BI-Report-Referenz, Textinhalt).</summary>
    public string ConfigJson { get; set; } = "{}";
}
