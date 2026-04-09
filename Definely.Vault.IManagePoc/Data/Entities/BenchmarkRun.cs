namespace Definely.Vault.IManagePoc.Data.Entities;

public class BenchmarkRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string RunId { get; set; }
    public required string Scenario { get; set; }
    public required string Library { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public double? ElapsedSeconds { get; set; }

    // API call counts
    public int SearchCalls { get; set; }
    public int PathResolutionCalls { get; set; }
    public int FolderPermissionCalls { get; set; }
    public int DocumentPermissionCalls { get; set; }
    public int CrawlDocumentCalls { get; set; }
    public int CrawlParentCalls { get; set; }
    public int CrawlFolderCalls { get; set; }
    public int CrawlWorkspaceCalls { get; set; }
    public int CrawlAllowedDocTrusteeCalls { get; set; }
    public int CrawlDeniedDocTrusteeCalls { get; set; }
    public int CrawlAllowedContainerTrusteeCalls { get; set; }
    public int CrawlDeniedContainerTrusteeCalls { get; set; }
    public int ChangeEventCalls { get; set; }
    public int TokenRefreshes { get; set; }
    public int TotalApiCalls { get; set; }

    // Record counts
    public int DocumentsDiscovered { get; set; }
    public int FoldersDiscovered { get; set; }
    public int WorkspacesDiscovered { get; set; }
    public int PermissionRecords { get; set; }

    // Memory
    public double? PeakMemoryMb { get; set; }

    public string? Notes { get; set; }
}
