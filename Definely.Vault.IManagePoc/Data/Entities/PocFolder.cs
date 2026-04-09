namespace Definely.Vault.IManagePoc.Data.Entities;

public class PocFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string DmsId { get; set; }
    public required string Name { get; set; }
    public string? ParentDmsId { get; set; }
    public string? WorkspaceDmsId { get; set; }
    public bool IsWorkspace { get; set; }
    public string? DefaultSecurity { get; set; }
    public string? OwnerId { get; set; }
    public required string BenchmarkRunId { get; set; }
    public required string Scenario { get; set; }
}
