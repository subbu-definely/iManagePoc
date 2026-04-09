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
    public DbSet<BenchmarkRun> BenchmarkRuns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // DmsSyncJobInfo (parent)
        modelBuilder.Entity<DmsSyncJobInfo>(e =>
        {
            e.ToTable("dms_sync_job_infos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        // DmsSyncDocument
        modelBuilder.Entity<DmsSyncDocument>(e =>
        {
            e.ToTable("dms_sync_documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.DmsSyncDocuments)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DmsSyncJobInfoId);
            e.Property(x => x.ParentIdsJson).HasColumnType("jsonb");
            e.Property(x => x.AuthorsJson).HasColumnType("jsonb");
        });

        // DmsSyncFolder
        modelBuilder.Entity<DmsSyncFolder>(e =>
        {
            e.ToTable("dms_sync_folders");
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
            e.ToTable("dms_sync_document_permissions");
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
            e.ToTable("dms_sync_folder_permissions");
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
            e.ToTable("dms_sync_job_users");
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
            e.ToTable("dms_sync_job_cabinet_groups");
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
            e.ToTable("dms_sync_job_group_members");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasOne(x => x.DmsSyncJobInfo)
                .WithMany(x => x.DmsSyncJobGroupMembers)
                .HasForeignKey(x => x.DmsSyncJobInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DmsSyncJobInfoId);
            e.Property(x => x.MembersJson).HasColumnType("jsonb");
        });

        // BenchmarkRun (POC-only)
        modelBuilder.Entity<BenchmarkRun>(e =>
        {
            e.ToTable("poc_benchmark_runs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RunId).IsUnique();
        });
    }
}
