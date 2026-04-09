using Definely.Vault.IManagePoc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Definely.Vault.IManagePoc.Data;

public class PocDbContext : DbContext
{
    public PocDbContext(DbContextOptions<PocDbContext> options) : base(options) { }

    public DbSet<PocDocument> Documents { get; set; } = null!;
    public DbSet<PocFolder> Folders { get; set; } = null!;
    public DbSet<PocPermission> Permissions { get; set; } = null!;
    public DbSet<BenchmarkRun> BenchmarkRuns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PocDocument>(e =>
        {
            e.ToTable("poc_documents");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BenchmarkRunId, x.DmsId });
        });

        modelBuilder.Entity<PocFolder>(e =>
        {
            e.ToTable("poc_folders");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BenchmarkRunId, x.DmsId });
        });

        modelBuilder.Entity<PocPermission>(e =>
        {
            e.ToTable("poc_permissions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BenchmarkRunId, x.DocumentOrFolderId });
        });

        modelBuilder.Entity<BenchmarkRun>(e =>
        {
            e.ToTable("poc_benchmark_runs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RunId).IsUnique();
        });
    }
}
