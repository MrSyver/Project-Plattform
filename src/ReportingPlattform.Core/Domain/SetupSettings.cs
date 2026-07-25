namespace ReportingPlattform.Core.Domain;

/// <summary>
/// Alle beim Ersteinrichtung gewählten Einstellungen. Wird als <c>setup.json</c> im
/// Datenverzeichnis abgelegt und beim Start als Konfigurationsquelle geladen —
/// dadurch überschreibt der Assistent keine ausgelieferten appsettings.
/// </summary>
public class SetupSettings
{
    // --- Schritt 1: Organisation ---
    public string OrganizationName { get; set; } = string.Empty;
    public string PlatformTitle { get; set; } = "Reporting Plattform";

    // --- Schritt 2: Betrieb ---
    /// <summary>"cloud" oder "onprem" — steuert die Adapter-Vorauswahl (§ 8.1).</summary>
    public string DeploymentMode { get; set; } = "onprem";
    /// <summary>"local" | "entra" | "hybrid".</summary>
    public string AuthMode { get; set; } = "local";
    /// <summary>"sqlite" (Dev/klein) | "sqlserver".</summary>
    public string DatabaseProvider { get; set; } = "sqlite";
    public string? ConnectionString { get; set; }

    // --- Schritt 3: Administrator (Passwort wird NIE hier gespeichert, nur gehasht in der DB) ---
    public string AdminEmail { get; set; } = string.Empty;

    // --- Schritt 4: Editoren ---
    public List<string> EditorAllowlist { get; set; } = new();

    // --- Schritt 5: Power BI (optional) ---
    public string BiProvider { get; set; } = "PowerBiService";
    public string? PowerBiWorkspaceId { get; set; }

    // --- Schritt 6: Dateien ---
    public int MaxFileSizeMb { get; set; } = 100;
    public List<string> AllowedExtensions { get; set; } = new()
        { "pdf", "docx", "xlsx", "pptx", "csv", "txt", "md", "png", "jpg", "jpeg", "zip" };

    // --- Abschluss ---
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool CreateDemoProject { get; set; } = true;
}
