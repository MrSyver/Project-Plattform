using ReportingPlattform.Core.Domain;

namespace ReportingPlattform.Core.Services;

/// <summary>Prüft die Eingaben des Einrichtungs-Assistenten schrittweise (deutsche Meldungen).</summary>
public static class SetupValidation
{
    public static string? Organization(SetupSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.OrganizationName)) return "Bitte den Namen der Organisation angeben.";
        if (string.IsNullOrWhiteSpace(s.PlatformTitle)) return "Bitte einen Titel für die Plattform angeben.";
        return null;
    }

    public static string? Operations(SetupSettings s)
    {
        if (s.DeploymentMode is not ("cloud" or "onprem")) return "Ungültige Betriebsart.";
        if (s.AuthMode is not ("local" or "entra" or "hybrid")) return "Ungültiger Anmelde-Modus.";
        if (s.DatabaseProvider is not ("sqlite" or "sqlserver")) return "Ungültige Datenbank-Auswahl.";
        if (s.DatabaseProvider == "sqlserver" && string.IsNullOrWhiteSpace(s.ConnectionString))
            return "Für SQL Server wird eine Verbindungszeichenfolge benötigt.";
        return null;
    }

    public static string? Administrator(string? email, string? password, string? passwordRepeat)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "Bitte eine gültige E-Mail-Adresse angeben.";
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            return "Das Passwort muss mindestens 12 Zeichen lang sein.";
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            return "Das Passwort muss Groß-, Kleinbuchstaben und Ziffern enthalten.";
        if (password != passwordRepeat) return "Die Passwörter stimmen nicht überein.";
        return null;
    }

    public static string? Files(int maxMb, IEnumerable<string> extensions)
    {
        if (maxMb is < 1 or > 2048) return "Die maximale Dateigröße muss zwischen 1 und 2048 MB liegen.";
        var list = extensions.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        if (list.Count == 0) return "Mindestens ein erlaubter Dateityp muss angegeben werden.";
        if (list.Any(e => e.Contains('*'))) return "Platzhalter sind nicht erlaubt — bitte einzelne Dateiendungen angeben.";
        return null;
    }

    /// <summary>Zerlegt Freitext-Eingaben (Komma/Semikolon/Zeilenumbruch/Leerzeichen) in eine Liste.</summary>
    public static List<string> SplitList(string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? new List<string>()
            : input.Split(new[] { ',', ';', '\n', '\r', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Select(x => x.TrimStart('.').ToLowerInvariant())
                   .Distinct()
                   .ToList();
}
