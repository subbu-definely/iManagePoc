namespace Definely.Vault.IManagePoc.Data.Entities;

public class DmsSyncFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DmsSyncJobInfoId { get; set; }
    public string DmsId { get; set; } = string.Empty;
    public string? ParentDmsId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Source { get; set; } // StorageItemSourceEnum
    public string? WorkspaceId { get; set; }
    public string Cabinet { get; set; } = string.Empty;
    public bool IsWorkspace { get; set; }
    public bool IsRootElement { get; set; }
    public bool IsGoldStandard { get; set; }
    public bool IsInheritPermissions { get; set; }

    public DmsSyncJobInfo DmsSyncJobInfo { get; set; } = null!;
}
