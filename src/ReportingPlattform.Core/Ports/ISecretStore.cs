namespace ReportingPlattform.Core.Ports;

/// <summary>
/// Port für Secrets. Cloud-Adapter: Azure Key Vault; On-Prem-Adapter: HashiCorp Vault.
/// Secrets kommen nie aus Image/Config im Klartext. Technische Doku § 8.1.
/// </summary>
public interface ISecretStore
{
    Task<string?> GetSecretAsync(string name, CancellationToken ct = default);
}
