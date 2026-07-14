# Multi-Stage-Build – gleiches Image für beide Deployment-Pfade (ADR-015).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ReportingPlattform.sln
RUN dotnet publish src/ReportingPlattform.Web/ReportingPlattform.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime: schlank, non-root (Härtung § 9.5).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Container läuft als non-root (uid 64198 ist im aspnet-Image vordefiniert).
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ReportingPlattform.Web.dll"]
