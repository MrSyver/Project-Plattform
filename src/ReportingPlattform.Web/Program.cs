using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ReportingPlattform.Core.Services;
using ReportingPlattform.Infrastructure.Auth;
using ReportingPlattform.Infrastructure.Data;
using ReportingPlattform.Infrastructure.DependencyInjection;
using ReportingPlattform.Infrastructure.Files;
using ReportingPlattform.Infrastructure.Setup;
using ReportingPlattform.Web;
using ReportingPlattform.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Einrichtungs-Assistent: setup.json (falls vorhanden) als zusätzliche Konfigurationsquelle —
// überschreibt appsettings, wird aber selbst von Umgebungsvariablen überschrieben.
var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
var setupService = new SetupService(dataDirectory);
if (setupService.Load() is { IsCompleted: true } saved)
    builder.Configuration.AddInMemoryCollection(SetupService.ToConfiguration(saved));
builder.Services.AddSingleton(setupService);

// Blazor Web App (serverseitige Interaktivität – Logik bleibt am Server, § 3 / ADR-002).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Ports & Adapters: Infrastruktur-Adapter binden (ADR-015).
builder.Services.AddReportingInfrastructure(builder.Configuration);

// Editor-Policy: „Editor = E-Mail-Allowlist ODER Editor-Rolle" (ADR-018).
// Scoped + Live-Lesen aus setup.json, damit im Assistenten gepflegte Editoren
// sofort wirksam sind (ohne Neustart).
builder.Services.AddScoped(sp =>
{
    var saved = sp.GetRequiredService<SetupService>().Load();
    var emails = saved is { IsCompleted: true }
        ? saved.EditorAllowlist.ToArray()
        : builder.Configuration.GetSection("Editors:Allowlist").Get<string[]>() ?? Array.Empty<string>();
    return new EditorPolicy(emails);
});
builder.Services.AddSingleton<ProjectAccessService>();

// Auth: Cookie-Login für lokale Accounts (Phase 3). Entra-OIDC folgt als zweiter
// Adapter über Auth:Mode=entra (ADR-004) – bewusst erst mit echtem Test-Mandanten.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/login";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Solange die Plattform nicht eingerichtet ist, führt jeder Aufruf zum Assistenten.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";
    var isExempt = path.StartsWith("/setup", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/readyz", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/_", StringComparison.OrdinalIgnoreCase)   // Blazor/Framework
                   || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/bootstrap", StringComparison.OrdinalIgnoreCase)
                   || Path.HasExtension(path);

    if (!setupService.IsConfigured && !isExempt)
    {
        context.Response.Redirect("/setup");
        return;
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Health/Readiness für Container-Orchestrierung (§ 7).
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapGet("/readyz", () => Results.Ok(new { status = "ready" }));

// Abmelden (Cookie löschen, zurück zur Startseite).
app.MapGet("/auth/logout", async context =>
{
    await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions
        .SignOutAsync(context, CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/");
});

// ---- Dateibibliothek (§ 4.5): Upload mit Pflicht-Virenscan, Download mit Audit ----

app.MapPost("/api/projekt/{slug}/dateien", async (
    string slug, IFormFile? datei, HttpContext http,
    AppDbContext db, FileLibraryService files, ProjectAccessService access, EditorPolicy editors) =>
{
    var ctx = UserContextFactory.From(http.User, editors);
    var space = await db.ProjectSpaces.Include(p => p.AccessList).FirstOrDefaultAsync(p => p.Slug == slug);
    if (space is null || !access.CanUploadFiles(ctx, space))
        return Results.Redirect($"/projekt/{slug}/dateien?fehler={Uri.EscapeDataString("Keine Berechtigung zum Hochladen.")}");
    if (datei is null || datei.Length == 0)
        return Results.Redirect($"/projekt/{slug}/dateien?fehler={Uri.EscapeDataString("Keine Datei ausgewählt.")}");

    var actor = http.User.FindFirstValue(ClaimTypes.Email) ?? "unbekannt";
    await using var stream = datei.OpenReadStream();
    var (_, error) = await files.UploadAsync(space, datei.FileName, datei.ContentType, datei.Length, stream, actor);

    return Results.Redirect(error is null
        ? $"/projekt/{slug}/dateien"
        : $"/projekt/{slug}/dateien?fehler={Uri.EscapeDataString(error)}");
}).RequireAuthorization();

app.MapGet("/projekt/{slug}/datei/{id:guid}", async (
    string slug, Guid id, HttpContext http,
    AppDbContext db, FileLibraryService files, ProjectAccessService access, EditorPolicy editors) =>
{
    var ctx = UserContextFactory.From(http.User, editors);
    var space = await db.ProjectSpaces.Include(p => p.AccessList).FirstOrDefaultAsync(p => p.Slug == slug);
    if (space is null || !access.CanView(ctx, space)) return Results.NotFound();

    var actor = http.User.FindFirstValue(ClaimTypes.Email) ?? "unbekannt";
    var result = await files.OpenAsync(space, id, actor);
    return result is null
        ? Results.NotFound()
        : Results.File(result.Value.Content, result.Value.Meta.ContentType, result.Value.Meta.FileName);
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// DB-Schema sicherstellen + Erst-Admin seeden (Dev-Komfort; echte Migrationen folgen).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    var users = scope.ServiceProvider.GetRequiredService<LocalUserService>();
    await users.SeedAdminIfEmptyAsync(
        app.Configuration["Admin:Email"],
        app.Configuration["Admin:Password"]);
}

app.Run();
