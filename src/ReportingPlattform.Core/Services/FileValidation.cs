namespace ReportingPlattform.Core.Services;

/// <summary>
/// Typ-Whitelist + Größen-Limit für Uploads (§ 9.6). Konfigurierbar je Instanz;
/// bewusst Whitelist statt Blacklist (Bank-Anforderung).
/// </summary>
public sealed class FileValidation
{
    private readonly HashSet<string> _allowed;
    private readonly long _maxBytes;

    public FileValidation(IEnumerable<string> allowedExtensions, long maxSizeBytes)
    {
        _allowed = new HashSet<string>(
            allowedExtensions.Select(e => e.Trim().TrimStart('.').ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
        _maxBytes = maxSizeBytes;
    }

    public long MaxSizeBytes => _maxBytes;

    /// <summary>Liefert eine deutsche Fehlermeldung oder null (= ok).</summary>
    public string? Check(string fileName, long size)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !_allowed.Contains(ext))
            return $"Dateityp \".{ext}\" ist nicht erlaubt.";
        if (size <= 0)
            return "Leere Datei.";
        if (size > _maxBytes)
            return $"Datei zu groß (max. {_maxBytes / (1024 * 1024)} MB).";
        return null;
    }
}
