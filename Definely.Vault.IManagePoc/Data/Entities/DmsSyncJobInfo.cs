namespace Definely.Vault.IManagePoc.Data.Entities;

public class DmsSyncJobInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int SyncJobState { get; set; } = 10; // ImportJobState.Created
    public int DocumentsPathFound { get; set; }
    public int FolderPermissionsFound { get; set; }
    public int DocumentsUploadSent { get; set; }
    public string ErrorDetails { get; set; } = string.Empty;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastModifiedAt { get; set; }

    public ICollection<DmsSyncDocument> DmsSyncDocuments { get; set; } = [];
    public ICollection<DmsSyncFolder> DmsSyncFolders { get; set; } = [];
    public ICollection<DmsSyncDocumentPermission> DmsSyncDocumentPermissions { get; set; } = [];
    public ICollection<DmsSyncFolderPermission> DmsSyncFolderPermissions { get; set; } = [];
    public ICollection<DmsSyncJobUser> DmsSyncJobUsers { get; set; } = [];
    public ICollection<DmsSyncJobCabinetGroup> DmsSyncJobCabinetGroups { get; set; } = [];
    public ICollection<DmsSyncJobGroupMember> DmsSyncJobGroupMembers { get; set; } = [];
}
