namespace Definely.Vault.IManagePoc.Data.Entities;

public class DmsSyncJobCabinetGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DmsSyncJobInfoId { get; set; }
    public string CabinetId { get; set; } = string.Empty;
    public string GroupsJson { get; set; } = "[]";

    public DmsSyncJobInfo DmsSyncJobInfo { get; set; } = null!;
}
