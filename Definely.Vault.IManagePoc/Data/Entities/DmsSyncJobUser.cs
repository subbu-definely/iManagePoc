namespace Definely.Vault.IManagePoc.Data.Entities;

public class DmsSyncJobUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DmsSyncJobInfoId { get; set; }
    public string DmsUserId { get; set; } = string.Empty;
    public string UserJson { get; set; } = "{}";

    public DmsSyncJobInfo DmsSyncJobInfo { get; set; } = null!;
}
