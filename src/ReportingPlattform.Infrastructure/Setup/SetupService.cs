using System.Text.Json;
using ReportingPlattform.Core.Domain;

namespace ReportingPlattform.Infrastructure.Setup;

/// <summary>
/// Lädt und speichert die Einrichtungs-Einstellungen als <c>setup.json</c> im Datenverzeichnis.
/// Bewusst getrennt von den ausgelieferten appsettings: der Assistent schreibt nur in diese Datei,
/// die beim Start als zusätzliche Konfigurationsquelle eingehängt wird.
/// </summary>
public sealed class SetupService
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly string _path;

    public SetupService(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _path = Path.Combine(dataDirectory, "setup.json");
    }

    public string FilePath => _path;

    /// <summary>True, wenn der Assistent bereits erfolgreich abgeschlossen wurde.</summary>
    public bool IsConfigured => Load()?.IsCompleted == true;

    public SetupSettings? Load()
    {
        if (!File.Exists(_path)) return null;
        try
        {
            return JsonSerializer.Deserialize<SetupSettings>(File.ReadAllText(_path));
        }
        catch
        {
            return null; // beschädigte Datei ⇒ Assistent startet erneut
        }
    }

    public SetupSettings LoadOrNew() => Load() ?? new SetupSettings();

    public void Save(SetupSettings settings)
    {
        // Atomar schreiben, damit ein Abbruch keine halbe Datei hinterlässt.
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(settings, Json));
        File.Move(tmp, _path, overwrite: true);
    }

    /// <summary>Bildet die Einstellungen auf die Konfigurations-Schlüssel der App ab (§ 5).</summary>
    public static Dictionary<string, string?> ToConfiguration(SetupSettings s)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Branding:OrganizationName"] = s.OrganizationName,
            ["Branding:Title"] = s.PlatformTitle,
            ["Deployment:Mode"] = s.DeploymentMode,
            ["Auth:Mode"] = s.AuthMode,
            ["Database:Provider"] = s.DatabaseProvider,
            ["Bi:Provider"] = s.BiProvider,
            ["PowerBI:WorkspaceId"] = s.PowerBiWorkspaceId,
            ["Files:MaxSizeMb"] = s.MaxFileSizeMb.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(s.ConnectionString))
            dict["ConnectionStrings:AppDb"] = s.ConnectionString;

        for (var i = 0; i < s.EditorAllowlist.Count; i++)
            dict[$"Editors:Allowlist:{i}"] = s.EditorAllowlist[i];

        for (var i = 0; i < s.AllowedExtensions.Count; i++)
            dict[$"Files:AllowedExtensions:{i}"] = s.AllowedExtensions[i];

        return dict;
    }
}
