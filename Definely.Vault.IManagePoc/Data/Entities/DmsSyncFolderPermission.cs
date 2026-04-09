namespace Definely.Vault.IManagePoc.Data.Entities;

public class DmsSyncFolderPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DmsSyncJobInfoId { get; set; }
    public string DmsFolderId { get; set; } = string.Empty;
    public string AclJson { get; set; } = "[]";

    public DmsSyncJobInfo DmsSyncJobInfo { get; set; } = null!;
}
