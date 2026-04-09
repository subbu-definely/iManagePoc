namespace Definely.Vault.IManagePoc.Data.Entities;

public class DmsSyncDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DmsSyncJobInfoId { get; set; }
    public string DmsId { get; set; } = string.Empty;
    public string EnvId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Ext { get; set; } = string.Empty;
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? Modified { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public decimal Version { get; set; }
    public string Cabinet { get; set; } = string.Empty;
    public string CabinetName { get; set; } = string.Empty;
    public string? WorkspaceId { get; set; }
    public bool IsGoldStandard { get; set; }
    public bool IsLoadVersions { get; set; }
    public bool IsInheritPermissions { get; set; }
    public bool PermissionsProcessed { get; set; }
    public int DocumentSource { get; set; } // StorageItemSourceEnum
    public string? CustomAttributesJson { get; set; } = "[]";
    public string[] ParentIdsJson { get; set; } = [];
    public Guid RuleId { get; set; } // ImportRuleId
    public string[] AuthorsJson { get; set; } = [];

    public DmsSyncJobInfo DmsSyncJobInfo { get; set; } = null!;
}
