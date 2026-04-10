using Definely.Vault.IManagePoc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Definely.Vault.IManagePoc.Data;

public class PocDbContext : DbContext
{
    public PocDbContext(DbContextOptions<PocDbContext> options) : base(options) { }

    public DbSet<DmsSyncJobInfo> DmsSyncJobInfos { get; set; } = null!;
    public DbSet<DmsSyncDocument> DmsSyncDocuments { get; set; } = null!;
    public DbSet<DmsSyncFolder> DmsSyncFolders { get; set; } = null!;
    public DbSet<DmsSyncDocumentPermission> DmsSyncDocumentPermissions { get; set; } = null!;
    public DbSet<DmsSyncFolderPermission> DmsSyncFolderPermissions { get; set; } = null!;
    public DbSet<DmsSyncJobUser> DmsSyncJobUsers { get; set; } = null!;
    public DbSet<DmsSyncJobCabinetGroup> DmsSyncJobCabinetGroups { get; set; } = null!;
    public DbSet<DmsSyncJobGroupMember> DmsSyncJobGroupMembers { get; set; } = null!;
    public DbSet<DmsSyncCrawlProgress> DmsSyncCrawlProgress { get; set; } = null!;
    public DbSet<BenchmarkRun> BenchmarkRuns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // DmsSyncJobInfo (parent)
        modelBuilder.Entity<DmsSyncJobInfo>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        // DmsSyncDocument
        modelBuilder.Entity<DmsSyncDocument>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.DmsSyncDocuments)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DmsSyncJobInfoId);
            e.Property(x => x.ParentIdsJson).HasColumnType("text[]");
            e.Property(x => x.AuthorsJson).HasColumnType("text[]");
            e.Ignore(x => x.DocumentNumber);
            e.HasIndex(x => x.DmsId);
        });

        // DmsSyncFolder
        modelBuilder.Entity<DmsSyncFolder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.DmsSyncFolders)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DmsSyncJobInfoId);
        });

        // DmsSyncDocumentPermission
        modelBuilder.Entity<DmsSyncDocumentPermission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.DmsSyncDocumentPermissions)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DmsSyncJobInfoId);
            e.Property(x => x.AclJson).HasColumnType("jsonb");
        });

        // DmsSyncFolderPermission
        modelBuilder.Entity<DmsSyncFolderPermission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.DmsSyncFolderPermissions)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DmsSyncJobInfoId);
            e.Property(x => x.AclJson).HasColumnType("jsonb");
        });

        // DmsSyncJobUser
        modelBuilder.Entity<DmsSyncJobUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.DmsSyncJobUsers)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DmsSyncJobInfoId);
            e.Property(x => x.UserJson).HasColumnType("jsonb");
        });

        // DmsSyncJobCabinetGroup
        modelBuilder.Entity<DmsSyncJobCabinetGroup>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.DmsSyncJobCabinetGroups)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DmsSyncJobInfoId);
            e.Property(x => x.GroupsJson).HasColumnType("jsonb");
        });

        // DmsSyncJobGroupMember
        modelBuilder.Entity<DmsSyncJobGroupMember>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.DmsSyncJobGroupMembers)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DmsSyncJobInfoId);
            e.Property(x => x.MembersJson).HasColumnType("jsonb");
        });

        // DmsSyncCrawlProgress
        modelBuilder.Entity<DmsSyncCrawlProgress>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.CrawlProgress)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.DmsSyncJobInfoId, x.LibraryId, x.EndpointName }).IsUnique();
        });

        // BenchmarkRun (POC-only)
        modelBuilder.Entity<BenchmarkRun>(e =>
        {
            e.ToTable("PocBenchmarkRuns");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RunId).IsUnique();
        });
    }
}
