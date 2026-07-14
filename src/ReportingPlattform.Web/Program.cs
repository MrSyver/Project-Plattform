using Microsoft.AspNetCore.Authentication.Cookies;
using ReportingPlattform.Core.Services;
using ReportingPlattform.Infrastructure.Auth;
using ReportingPlattform.Infrastructure.Data;
using ReportingPlattform.Infrastructure.DependencyInjection;
using ReportingPlattform.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Blazor Web App (serverseitige Interaktivität – Logik bleibt am Server, § 3 / ADR-002).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Ports & Adapters: Infrastruktur-Adapter binden (ADR-015).
builder.Services.AddReportingInfrastructure(builder.Configuration);

// Editor-Policy: „Editor = E-Mail-Allowlist ODER Editor-Rolle" (ADR-018).
var editorAllowlist = builder.Configuration.GetSection("Editors:Allowlist").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddSingleton(new EditorPolicy(editorAllowlist));
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
