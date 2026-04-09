namespace Definely.Vault.IManagePoc.Data.Entities;

public class PocPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string DocumentOrFolderId { get; set; }
    public required string PermissionType { get; set; } // "allowed" or "denied"
    public required string TargetType { get; set; } // "document" or "folder"
    public string? UserId { get; set; }
    public string? GroupId { get; set; }
    public string? AccessLevel { get; set; }
    public required string BenchmarkRunId { get; set; }
    public required string Scenario { get; set; }
}
