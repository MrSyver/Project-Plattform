using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReportingPlattform.Core.Domain;
using ReportingPlattform.Infrastructure.Data;

namespace ReportingPlattform.Infrastructure.Auth;

/// <summary>
/// Verwaltung lokaler Accounts (Fallback ohne AD). Hashing über den ASP.NET-Core-
/// <see cref="PasswordHasher{TUser}"/> (PBKDF2). Keine Klartext-Passwörter, nirgends.
/// </summary>
public sealed class LocalUserService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<LocalUser> _hasher = new();

    public LocalUserService(AppDbContext db) => _db = db;

    public async Task<LocalUser> CreateAsync(string email, string displayName, string password, PlatformRole role, CancellationToken ct = default)
    {
        var user = new LocalUser { Email = email.Trim().ToLowerInvariant(), DisplayName = displayName, Role = role };
        user.PasswordHash = _hasher.HashPassword(user, password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    /// <summary>Liefert den Nutzer bei korrektem Passwort, sonst null (kein Unterschied „unbekannt/falsch" nach außen).</summary>
    public async Task<LocalUser?> ValidateAsync(string email, string password, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized && u.IsActive, ct);
        if (user is null) return null;
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded ? user : null;
    }

    /// <summary>Erst-Seed eines Admin-Accounts, falls noch keinerlei Nutzer existieren (Setup-Komfort).</summary>
    public async Task SeedAdminIfEmptyAsync(string? email, string? password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        if (await _db.Users.AnyAsync(ct)) return;
        await CreateAsync(email, "Administrator", password, PlatformRole.Admin, ct);
    }
}
