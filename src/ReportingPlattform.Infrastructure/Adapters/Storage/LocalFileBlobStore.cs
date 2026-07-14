using ReportingPlattform.Core.Ports;

namespace ReportingPlattform.Infrastructure.Adapters.Storage;

/// <summary>
/// On-Prem-Adapter für <see cref="IBlobStore"/>: legt Dateien in einem gemounteten
/// Verzeichnis ab. Cloud-Pendant (Azure Blob) ist ein separater Adapter. § 8.1.
/// </summary>
public sealed class LocalFileBlobStore : IBlobStore
{
    private readonly string _root;

    public LocalFileBlobStore(string rootPath)
    {
        _root = rootPath;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(string container, string fileName, Stream content, CancellationToken ct = default)
    {
        var id = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var dir = Path.Combine(_root, container);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, id);
        await using var fs = File.Create(path);
        await content.CopyToAsync(fs, ct);
        return id;
    }

    public Task<Stream?> OpenAsync(string container, string storageId, CancellationToken ct = default)
    {
        var path = Path.Combine(_root, container, storageId);
        Stream? result = File.Exists(path) ? File.OpenRead(path) : null;
        return Task.FromResult(result);
    }
}
