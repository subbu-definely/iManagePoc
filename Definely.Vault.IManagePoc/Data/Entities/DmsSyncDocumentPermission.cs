namespace Definely.Vault.IManagePoc.Data.Entities;

public class DmsSyncDocumentPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DmsSyncJobInfoId { get; set; }
    public string DmsDocumentId { get; set; } = string.Empty;
    public string AclJson { get; set; } = "[]";

    public DmsSyncJobInfo DmsSyncJobInfo { get; set; } = null!;
}
