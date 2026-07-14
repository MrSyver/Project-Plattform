namespace ReportingPlattform.Core.Ports;

/// <summary>
/// Port für die Dateiablage der Projektbibliotheken. Cloud-Adapter: Azure Blob;
/// On-Prem-Adapter: gemountetes Volume / MinIO. Technische Doku § 8.1 / § 9.6.
/// </summary>
public interface IBlobStore
{
    /// <summary>Legt Inhalt ab und liefert eine Storage-Id/Referenz zurück.</summary>
    Task<string> SaveAsync(string container, string fileName, Stream content, CancellationToken ct = default);

    /// <summary>Öffnet einen abgelegten Inhalt zum Lesen (null, wenn nicht vorhanden).</summary>
    Task<Stream?> OpenAsync(string container, string storageId, CancellationToken ct = default);
}
