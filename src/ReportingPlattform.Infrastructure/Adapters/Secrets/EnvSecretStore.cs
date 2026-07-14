using Microsoft.Extensions.Configuration;
using ReportingPlattform.Core.Ports;

namespace ReportingPlattform.Infrastructure.Adapters.Secrets;

/// <summary>
/// Einfacher Secret-Adapter für lokale Entwicklung: liest aus Konfiguration/Env
/// (Abschnitt "Secrets:*"). In Prod durch Key-Vault-/Vault-Adapter ersetzen (§ 8.1).
/// </summary>
public sealed class EnvSecretStore : ISecretStore
{
    private readonly IConfiguration _config;
    public EnvSecretStore(IConfiguration config) => _config = config;

    public Task<string?> GetSecretAsync(string name, CancellationToken ct = default)
        => Task.FromResult(_config[$"Secrets:{name}"] ?? _config[name]);
}
