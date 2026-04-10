namespace Definely.Vault.IManagePoc.Data.Entities;

public class DmsSyncCrawlProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DmsSyncJobInfoId { get; set; }
    public required string LibraryId { get; set; }
    public required string EndpointName { get; set; }
    public string? LastCursor { get; set; }
    public int RecordsSaved { get; set; }
    public bool IsComplete { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DmsSyncJobInfo DmsSyncJobInfo { get; set; } = null!;
}
