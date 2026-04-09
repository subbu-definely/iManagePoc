namespace Definely.Vault.IManagePoc.Data.Entities;

public class DmsSyncJobGroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DmsSyncJobInfoId { get; set; }
    public string GroupId { get; set; } = string.Empty;
    public string MembersJson { get; set; } = "[]";

    public DmsSyncJobInfo DmsSyncJobInfo { get; set; } = null!;
}
