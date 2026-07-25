namespace ReportingPlattform.Core.Domain;

/// <summary>
/// Eine Datei der Projekt-Dateibibliothek (§ 9.6). Der Inhalt liegt im IBlobStore
/// (verschlüsselter Store in Prod), hier nur Metadaten. Gleicher Dateiname im
/// selben Raum ⇒ neue Version (alte Zeilen bleiben erhalten).
/// </summary>
public class ProjectFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectSpaceId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long Size { get; set; }
    public int Version { get; set; } = 1;

    /// <summary>Referenz im IBlobStore (nie direkt nach außen geben — Downloads laufen über die App).</summary>
    public string StorageId { get; set; } = string.Empty;

    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
