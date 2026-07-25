using Microsoft.EntityFrameworkCore;
using ReportingPlattform.Core.Domain;
using ReportingPlattform.Core.Ports;
using ReportingPlattform.Core.Services;
using ReportingPlattform.Infrastructure.Data;

namespace ReportingPlattform.Infrastructure.Files;

/// <summary>
/// Dateibibliothek je Projektraum — Upload-Kette nach § 4.5:
/// Rechte (Aufrufer) → Typ-/Größen-Limit → PFLICHT-Virenscan → Ablage → Audit.
/// Infizierte oder ungescannte Dateien werden NIE abgelegt (§ 9.6).
/// </summary>
public sealed class FileLibraryService
{
    private readonly AppDbContext _db;
    private readonly IBlobStore _blobs;
    private readonly IVirusScanner _scanner;
    private readonly IAuditSink _audit;
    private readonly FileValidation _validation;

    public FileLibraryService(AppDbContext db, IBlobStore blobs, IVirusScanner scanner, IAuditSink audit, FileValidation validation)
    {
        _db = db; _blobs = blobs; _scanner = scanner; _audit = audit; _validation = validation;
    }

    /// <summary>Liefert bei Erfolg die neue Datei, sonst eine Fehlermeldung (deutsch, nutzerfreundlich).</summary>
    public async Task<(ProjectFile? File, string? Error)> UploadAsync(
        ProjectSpace space, string fileName, string contentType, long size, Stream content, string actor,
        CancellationToken ct = default)
    {
        var safeName = Path.GetFileName(fileName); // Pfad-Anteile abstreifen

        if (_validation.Check(safeName, size) is { } validationError)
            return (null, validationError);

        // Pflicht-Virenscan VOR jeder Ablage. Scanner nicht erreichbar ⇒ ablehnen,
        // niemals ungescannt speichern (§ 9.6).
        var scan = await _scanner.ScanAsync(content, ct);
        if (scan == ScanResult.Infected)
        {
            await _audit.WriteAsync(new AuditEvent(DateTimeOffset.UtcNow, actor, "file.upload.blocked",
                $"project:{space.Slug}", $"{safeName} (Virenfund)"), ct);
            return (null, "Upload abgelehnt: Virenscan hat einen Fund gemeldet.");
        }
        if (scan == ScanResult.Unavailable)
            return (null, "Upload derzeit nicht möglich: Virenscanner nicht erreichbar.");

        if (content.CanSeek) content.Position = 0;
        var storageId = await _blobs.SaveAsync(space.Id.ToString("N"), safeName, content, ct);

        var version = await _db.Files
            .Where(f => f.ProjectSpaceId == space.Id && f.FileName == safeName)
            .Select(f => (int?)f.Version).MaxAsync(ct) ?? 0;

        var file = new ProjectFile
        {
            ProjectSpaceId = space.Id,
            FileName = safeName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Size = size,
            Version = version + 1,
            StorageId = storageId,
            UploadedBy = actor,
        };
        _db.Files.Add(file); // explizit (Guid-Key-Konvention)
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(new AuditEvent(DateTimeOffset.UtcNow, actor, "file.upload",
            $"project:{space.Slug}", $"{safeName} v{file.Version} ({size} B)"), ct);
        return (file, null);
    }

    /// <summary>Öffnet eine Datei zum Download (null bei unbekannter Id) und auditiert den Zugriff.</summary>
    public async Task<(Stream Content, ProjectFile Meta)?> OpenAsync(ProjectSpace space, Guid fileId, string actor, CancellationToken ct = default)
    {
        var meta = await _db.Files.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.ProjectSpaceId == space.Id, ct);
        if (meta is null) return null;

        var stream = await _blobs.OpenAsync(space.Id.ToString("N"), meta.StorageId, ct);
        if (stream is null) return null;

        await _audit.WriteAsync(new AuditEvent(DateTimeOffset.UtcNow, actor, "file.download",
            $"project:{space.Slug}", $"{meta.FileName} v{meta.Version}"), ct);
        return (stream, meta);
    }

    /// <summary>Neueste Version je Dateiname, inkl. Versionsanzahl.</summary>
    public async Task<List<(ProjectFile Latest, int VersionCount)>> ListAsync(Guid projectSpaceId, CancellationToken ct = default)
    {
        var all = await _db.Files.AsNoTracking()
            .Where(f => f.ProjectSpaceId == projectSpaceId)
            .ToListAsync(ct);
        return all
            .GroupBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.OrderByDescending(f => f.Version).First(), g.Count()))
            .OrderBy(t => t.Item1.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
