# Reporting Plattform

Modulare Enterprise-Web-Plattform: bindet **Power-BI-Reports** ein und erweitert sie um
Website-Bausteine (Text, Datei-Austausch, Video, Formulare). Inhalte in **Projekträumen**
mit eigener Zugriffssteuerung; **In-App-Bearbeitungsmodus** für Editoren. Deploybar
**komplett Cloud (Azure)** oder **komplett On-Prem** – gleiches Image, andere Konfiguration.

> Ausführliche Doku (Architektur, Nutzer, Prozesse) liegt im Obsidian-Vault
> unter `01_Projekte/Arbeit/Reporting Plattform/`.

## Architektur (Kurzform)
- **Backend:** ASP.NET Core (.NET 8), modularer Monolith.
- **Frontend:** Blazor Web App (Server-Interaktivität).
- **Struktur (Ports & Adapters):**
  - `src/ReportingPlattform.Core` – Domäne (Projektraum → Seite → Zone → Block), **Ports** (Interfaces), Services.
  - `src/ReportingPlattform.Infrastructure` – **Adapter** je Port (Storage, Secrets, BI, Antivirus, Audit, Connectors) + EF Core.
  - `src/ReportingPlattform.Web` – Blazor-UI + DI-Wiring + Health-Endpunkte.
  - `tests/ReportingPlattform.Tests` – xUnit.

## Lokaler Start (ohne Docker)
```bash
export PATH="$HOME/.dotnet:$PATH"      # .NET 8 SDK liegt in ~/.dotnet
dotnet build ReportingPlattform.sln
dotnet run --project src/ReportingPlattform.Web
```
Health-Check: `http://localhost:<port>/healthz`

## Tests
```bash
dotnet test ReportingPlattform.sln
```

## Container / On-Prem
```bash
cp .env.example .env    # Werte setzen (DB_PASSWORD etc.)
docker compose up -d    # App + SQL Server + ClamAV
```

## Konfiguration
Alles über Umgebungsvariablen (12-Factor), siehe `.env.example` und
`src/ReportingPlattform.Web/appsettings.json`.

## Status
Phase 2 – lauffähiges Skelett (baut, testet, startet). Nächste Phasen: Auth, Projekträume,
Editor, Power-BI-Embed, DB-Gateway. Details in der Obsidian-Status-Notiz.
