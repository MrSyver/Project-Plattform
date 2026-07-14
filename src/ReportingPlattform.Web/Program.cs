using ReportingPlattform.Core.Services;
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Health/Readiness für Container-Orchestrierung (§ 7).
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapGet("/readyz", () => Results.Ok(new { status = "ready" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
