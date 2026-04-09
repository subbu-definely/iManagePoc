using System.Text.Json;
using Definely.Vault.IManagePoc.Auth;
using Definely.Vault.IManagePoc.Client;
using Definely.Vault.IManagePoc.Data;
using Definely.Vault.IManagePoc.Data.Entities;
using Definely.Vault.IManagePoc.Metrics;
using Microsoft.Extensions.Configuration;

namespace Definely.Vault.IManagePoc.Scenarios;

public class Scenario3SyncApi : IScenario
{
    public string Name => "Scenario 3: Sync API (Bulk Crawl)";

    public async Task RunAsync(PocDbContext db, HttpClient httpClient, iManageAuthClient authClient,
        IConfigurationSection config, CancellationToken ct)
    {
        var metrics = new BenchmarkMetrics();
        var baseUrl = config["BaseUrl"]!;
        var customerId = int.Parse(config["CustomerId"]!);
        var libraryId = config["LibraryId"]!;

        var client = new iManageSyncApiClient(httpClient, authClient, baseUrl, customerId, libraryId, metrics);

        // Create sync job record
        var syncJob = new DmsSyncJobInfo
        {
            SyncJobState = 10, // Created
            StartedAt = DateTimeOffset.UtcNow
        };
        db.DmsSyncJobInfos.Add(syncJob);
        await db.SaveChangesAsync(ct);

        Console.WriteLine($"[Scenario 3] Sync job created: {syncJob.Id}");
        metrics.Start();

        try
        {
            // Step 1: Crawl documents
            Console.WriteLine("\n--- Step 1: Crawl Document Profiles ---");
            var documents = await client.CrawlDocumentsAsync(ct: ct);
            var docEntities = MapDocuments(documents, syncJob.Id, libraryId);
            db.DmsSyncDocuments.AddRange(docEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {docEntities.Count} documents");

            // Step 2: Crawl document parents
            Console.WriteLine("\n--- Step 2: Crawl Document Parents ---");
            var parents = await client.CrawlDocumentParentsAsync(ct: ct);
            UpdateDocumentParents(docEntities, parents, libraryId);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Updated parent IDs for documents");

            // Step 3: Crawl workspaces
            Console.WriteLine("\n--- Step 3: Crawl Workspaces ---");
            var workspaces = await client.CrawlWorkspacesAsync(ct: ct);
            var wsEntities = MapWorkspaces(workspaces, syncJob.Id, libraryId);
            db.DmsSyncFolders.AddRange(wsEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {wsEntities.Count} workspaces");

            // Step 4: Crawl folders
            Console.WriteLine("\n--- Step 4: Crawl Folders ---");
            var folders = await client.CrawlFoldersAsync(ct: ct);
            var folderEntities = MapFolders(folders, syncJob.Id, libraryId);
            db.DmsSyncFolders.AddRange(folderEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {folderEntities.Count} folders");

            // Step 5: Crawl allowed document trustees
            Console.WriteLine("\n--- Step 5: Crawl Allowed Document Trustees ---");
            var allowedDocTrustees = await client.CrawlAllowedDocumentTrusteesAsync(ct: ct);
            var allowedDocPermEntities = MapDocumentPermissions(allowedDocTrustees, syncJob.Id);
            db.DmsSyncDocumentPermissions.AddRange(allowedDocPermEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {allowedDocPermEntities.Count} allowed document permissions");

            // Step 6: Crawl denied document trustees
            Console.WriteLine("\n--- Step 6: Crawl Denied Document Trustees ---");
            var deniedDocTrustees = await client.CrawlDeniedDocumentTrusteesAsync(ct: ct);
            var deniedDocPermEntities = MapDocumentPermissions(deniedDocTrustees, syncJob.Id);
            db.DmsSyncDocumentPermissions.AddRange(deniedDocPermEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {deniedDocPermEntities.Count} denied document permissions");

            // Step 7: Crawl allowed container trustees
            Console.WriteLine("\n--- Step 7: Crawl Allowed Container Trustees ---");
            var allowedContainerTrustees = await client.CrawlAllowedContainerTrusteesAsync(ct: ct);
            var allowedFolderPermEntities = MapFolderPermissions(allowedContainerTrustees, syncJob.Id);
            db.DmsSyncFolderPermissions.AddRange(allowedFolderPermEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {allowedFolderPermEntities.Count} allowed container permissions");

            // Step 8: Crawl denied container trustees
            Console.WriteLine("\n--- Step 8: Crawl Denied Container Trustees ---");
            var deniedContainerTrustees = await client.CrawlDeniedContainerTrusteesAsync(ct: ct);
            var deniedFolderPermEntities = MapFolderPermissions(deniedContainerTrustees, syncJob.Id);
            db.DmsSyncFolderPermissions.AddRange(deniedFolderPermEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {deniedFolderPermEntities.Count} denied container permissions");

            // Step 9: Crawl users
            Console.WriteLine("\n--- Step 9: Crawl Users ---");
            var users = await client.CrawlUsersAsync(ct: ct);
            var userEntities = MapUsers(users, syncJob.Id);
            db.DmsSyncJobUsers.AddRange(userEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {userEntities.Count} users");

            // Step 10: Crawl groups + members
            Console.WriteLine("\n--- Step 10: Crawl Groups & Members ---");
            var groups = await client.CrawlGroupsAsync(ct: ct);
            var groupEntities = MapCabinetGroups(groups, syncJob.Id, libraryId);
            db.DmsSyncJobCabinetGroups.AddRange(groupEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {groupEntities.Count} cabinet groups");

            var groupMembers = await client.CrawlGroupMembersAsync(ct: ct);
            var memberEntities = MapGroupMembers(groupMembers, syncJob.Id);
            db.DmsSyncJobGroupMembers.AddRange(memberEntities);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[DB] Saved {memberEntities.Count} group members");

            // Update sync job
            syncJob.SyncJobState = 128; // Completed
            syncJob.FinishedAt = DateTimeOffset.UtcNow;
            syncJob.LastModifiedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            metrics.Stop();

            // Save benchmark run
            var run = new BenchmarkRun
            {
                RunId = metrics.RunId,
                Scenario = Name,
                Library = libraryId,
                StartTime = syncJob.StartedAt!.Value,
                EndTime = syncJob.FinishedAt!.Value,
                ElapsedSeconds = metrics.ElapsedSeconds,
                CrawlDocumentCalls = metrics.GetApiCallCount("crawl_documents"),
                CrawlParentCalls = metrics.GetApiCallCount("crawl_document_parents"),
                CrawlFolderCalls = metrics.GetApiCallCount("crawl_folders"),
                CrawlWorkspaceCalls = metrics.GetApiCallCount("crawl_workspaces"),
                CrawlAllowedDocTrusteeCalls = metrics.GetApiCallCount("crawl_allowed_doc_trustees"),
                CrawlDeniedDocTrusteeCalls = metrics.GetApiCallCount("crawl_denied_doc_trustees"),
                CrawlAllowedContainerTrusteeCalls = metrics.GetApiCallCount("crawl_allowed_container_trustees"),
                CrawlDeniedContainerTrusteeCalls = metrics.GetApiCallCount("crawl_denied_container_trustees"),
                TokenRefreshes = authClient.TokenRefreshCount,
                TotalApiCalls = metrics.TotalApiCalls,
                DocumentsDiscovered = docEntities.Count,
                FoldersDiscovered = folderEntities.Count,
                WorkspacesDiscovered = wsEntities.Count,
                PermissionRecords = allowedDocPermEntities.Count + deniedDocPermEntities.Count
                    + allowedFolderPermEntities.Count + deniedFolderPermEntities.Count,
                PeakMemoryMb = metrics.PeakMemoryMb
            };
            db.BenchmarkRuns.Add(run);
            await db.SaveChangesAsync(ct);

            metrics.PrintSummary(Name);
        }
        catch (Exception ex)
        {
            metrics.Stop();
            syncJob.SyncJobState = 256; // Error
            syncJob.ErrorDetails = ex.Message;
            syncJob.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[Error] {ex.Message}");
            throw;
        }
    }

    private static List<DmsSyncDocument> MapDocuments(List<JsonElement> documents, Guid jobId, string libraryId)
    {
        return documents.Select(d => new DmsSyncDocument
        {
            DmsSyncJobInfoId = jobId,
            DmsId = d.GetProperty("id").GetString() ?? string.Empty,
            EnvId = libraryId,
            Name = d.GetProperty("name").GetString() ?? string.Empty,
            Ext = d.TryGetProperty("extension", out var ext) ? ext.GetString() ?? string.Empty : string.Empty,
            Created = d.TryGetProperty("create_date", out var cd) ? DateTimeOffset.Parse(cd.GetString()!) : DateTimeOffset.MinValue,
            Modified = d.TryGetProperty("edit_date", out var ed) ? DateTimeOffset.Parse(ed.GetString()!) : null,
            CreatedBy = d.TryGetProperty("author", out var author) && author.TryGetProperty("id", out var aid)
                ? aid.GetString() ?? string.Empty : string.Empty,
            Version = d.TryGetProperty("version", out var ver) ? ver.GetDecimal() : 1,
            Cabinet = libraryId,
            CabinetName = libraryId,
            DocumentSource = 3, // StorageItemSourceEnum.IManage
            IsInheritPermissions = d.TryGetProperty("default_security", out var ds) && ds.GetString() == "inherit",
            DocumentNumber = d.TryGetProperty("document_number", out var dn) ? dn.GetInt32() : 0,
            CustomAttributesJson = "[]",
            ParentIdsJson = [],
            AuthorsJson = []
        }).ToList();
    }

    private static void UpdateDocumentParents(List<DmsSyncDocument> documents, List<JsonElement> parents, string libraryId)
    {
        var parentMap = new Dictionary<int, string>();
        foreach (var p in parents)
        {
            var docNum = p.GetProperty("document_number").GetInt32();
            var parentId = p.GetProperty("parent_id").GetString() ?? string.Empty;
            parentMap[docNum] = parentId;
        }

        foreach (var doc in documents)
        {
            if (parentMap.TryGetValue(doc.DocumentNumber, out var parentId))
            {
                doc.ParentIdsJson = [parentId];
            }
        }
    }

    private static List<DmsSyncFolder> MapWorkspaces(List<JsonElement> workspaces, Guid jobId, string libraryId)
    {
        return workspaces.Select(w => new DmsSyncFolder
        {
            DmsSyncJobInfoId = jobId,
            DmsId = w.GetProperty("id").GetString() ?? string.Empty,
            ParentDmsId = null,
            Name = w.GetProperty("name").GetString() ?? string.Empty,
            Source = 3, // IManage
            Cabinet = libraryId,
            IsWorkspace = true,
            IsRootElement = false,
            IsGoldStandard = false,
            IsInheritPermissions = false,
            WorkspaceId = w.GetProperty("id").GetString()
        }).ToList();
    }

    private static List<DmsSyncFolder> MapFolders(List<JsonElement> folders, Guid jobId, string libraryId)
    {
        return folders.Select(f => new DmsSyncFolder
        {
            DmsSyncJobInfoId = jobId,
            DmsId = f.GetProperty("id").GetString() ?? string.Empty,
            ParentDmsId = f.TryGetProperty("parent_id", out var pid) ? pid.GetString() : null,
            Name = f.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
            Source = 3, // IManage
            Cabinet = libraryId,
            IsWorkspace = false,
            IsRootElement = false,
            IsGoldStandard = false,
            IsInheritPermissions = f.TryGetProperty("default_security", out var ds) && ds.GetString() == "inherit",
            WorkspaceId = f.TryGetProperty("workspace_id", out var wsId) ? wsId.GetString() : null
        }).ToList();
    }

    private static List<DmsSyncDocumentPermission> MapDocumentPermissions(List<JsonElement> trustees, Guid jobId)
    {
        var grouped = new Dictionary<string, List<JsonElement>>();
        foreach (var t in trustees)
        {
            var docId = t.GetProperty("document_id").GetString() ?? string.Empty;
            if (!grouped.ContainsKey(docId))
                grouped[docId] = [];
            grouped[docId].Add(t);
        }

        return grouped.Select(g => new DmsSyncDocumentPermission
        {
            DmsSyncJobInfoId = jobId,
            DmsDocumentId = g.Key,
            AclJson = JsonSerializer.Serialize(g.Value)
        }).ToList();
    }

    private static List<DmsSyncFolderPermission> MapFolderPermissions(List<JsonElement> trustees, Guid jobId)
    {
        var grouped = new Dictionary<string, List<JsonElement>>();
        foreach (var t in trustees)
        {
            // Container trustees may use "container_id" or similar — adapt based on actual response
            var containerId = string.Empty;
            if (t.TryGetProperty("container_id", out var cid))
                containerId = cid.GetString() ?? string.Empty;
            else if (t.TryGetProperty("folder_id", out var fid))
                containerId = fid.GetString() ?? string.Empty;

            if (string.IsNullOrEmpty(containerId)) continue;

            if (!grouped.ContainsKey(containerId))
                grouped[containerId] = [];
            grouped[containerId].Add(t);
        }

        return grouped.Select(g => new DmsSyncFolderPermission
        {
            DmsSyncJobInfoId = jobId,
            DmsFolderId = g.Key,
            AclJson = JsonSerializer.Serialize(g.Value)
        }).ToList();
    }

    private static List<DmsSyncJobUser> MapUsers(List<JsonElement> users, Guid jobId)
    {
        return users.Select(u => new DmsSyncJobUser
        {
            DmsSyncJobInfoId = jobId,
            DmsUserId = u.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
            UserJson = u.GetRawText()
        }).ToList();
    }

    private static List<DmsSyncJobCabinetGroup> MapCabinetGroups(List<JsonElement> groups, Guid jobId, string libraryId)
    {
        return [new DmsSyncJobCabinetGroup
        {
            DmsSyncJobInfoId = jobId,
            CabinetId = libraryId,
            GroupsJson = JsonSerializer.Serialize(groups)
        }];
    }

    private static List<DmsSyncJobGroupMember> MapGroupMembers(List<JsonElement> members, Guid jobId)
    {
        var grouped = new Dictionary<string, List<JsonElement>>();
        foreach (var m in members)
        {
            var groupId = string.Empty;
            if (m.TryGetProperty("group_id", out var gid))
                groupId = gid.GetString() ?? string.Empty;
            else if (m.TryGetProperty("id", out var id))
                groupId = id.GetString() ?? string.Empty;

            if (string.IsNullOrEmpty(groupId)) continue;

            if (!grouped.ContainsKey(groupId))
                grouped[groupId] = [];
            grouped[groupId].Add(m);
        }

        return grouped.Select(g => new DmsSyncJobGroupMember
        {
            DmsSyncJobInfoId = jobId,
            GroupId = g.Key,
            MembersJson = JsonSerializer.Serialize(g.Value)
        }).ToList();
    }

    private int DocumentNumber { get; set; }
}
