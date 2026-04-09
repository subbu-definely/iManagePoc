namespace Definely.Vault.IManagePoc.Data.Entities;

public class PocDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string DmsId { get; set; }
    public required string Name { get; set; }
    public string? Extension { get; set; }
    public long Size { get; set; }
    public string? AuthorId { get; set; }
    public string? ParentContainerId { get; set; }
    public string? ResolvedPath { get; set; }
    public string? DocumentClass { get; set; }
    public string? DocumentType { get; set; }
    public string? DefaultSecurity { get; set; }
    public int DocumentNumber { get; set; }
    public int Version { get; set; }
    public DateTimeOffset? CreateDate { get; set; }
    public DateTimeOffset? EditDate { get; set; }
    public required string BenchmarkRunId { get; set; }
    public required string Scenario { get; set; }
}
