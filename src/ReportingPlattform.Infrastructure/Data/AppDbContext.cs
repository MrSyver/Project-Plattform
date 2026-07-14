using Microsoft.EntityFrameworkCore;
using ReportingPlattform.Core.Domain;

namespace ReportingPlattform.Infrastructure.Data;

/// <summary>
/// App-Datenbank (MSSQL / Azure SQL) via EF Core. Hält Projekträume, Seiten-Definitionen
/// und ACLs. Report-Daten und Dateien liegen NICHT hier (Präsentations-Ebene, § 6).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProjectSpace> ProjectSpaces => Set<ProjectSpace>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<ContentBlock> Blocks => Set<ContentBlock>();
    public DbSet<AccessEntry> AccessEntries => Set<AccessEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ProjectSpace>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Slug).HasMaxLength(200);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasMany(x => x.Pages).WithOne().HasForeignKey(p => p.ProjectSpaceId);
            e.HasMany(x => x.AccessList).WithOne().HasForeignKey(a => a.ProjectSpaceId);
        });

        b.Entity<Page>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.Slug).HasMaxLength(200);
            e.HasMany(x => x.Zones).WithOne().HasForeignKey(z => z.PageId);
        });

        b.Entity<Zone>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Blocks).WithOne().HasForeignKey(bl => bl.ZoneId);
        });

        b.Entity<ContentBlock>(e => e.HasKey(x => x.Id));
        b.Entity<AccessEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Subject).HasMaxLength(320);
        });
    }
}
