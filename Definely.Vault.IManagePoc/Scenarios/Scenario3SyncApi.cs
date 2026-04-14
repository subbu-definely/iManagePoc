using System.Text.Json;
using Definely.Vault.IManagePoc.Auth;
using Definely.Vault.IManagePoc.Client;
using Definely.Vault.IManagePoc.Data;
using Definely.Vault.IManagePoc.Data.Entities;
using Definely.Vault.IManagePoc.Metrics;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Definely.Vault.IManagePoc.Scenarios;

public class Scenario3SyncApi : IScenario
{
    public string Name => "Scenario 3: Sync API (Bulk Crawl)";

    public async Task RunAsync(PocDbContext db, HttpClient httpClient, iManageAuthClient authClient,
        IConfiguration config, CancellationToken ct)
    {
        var metrics = new BenchmarkMetrics();
        var imanage = config.GetSection("IManage");
        var baseUrl = imanage["BaseUrl"]!;
        var customerId = int.Parse(imanage["CustomerId"]!);
        var libraryIdConfig = imanage["LibraryId"]!;

        var poc = config.GetSection("Poc");
        var maxRecords = int.TryParse(poc["MaxRecordsPerEndpoint"], out var mr) ? mr : 0;
        var pageSize = int.TryParse(poc["PageSize"], out var ps) ? ps : 1000;

        if (maxRecords > 0)
            Console.WriteLine($"[Config] MaxRecordsPerEndpoint: {maxRecords}, PageSize: {pageSize}");
        else
            Console.WriteLine($"[Config] No record limit — full crawl, PageSize: {pageSize}");

        // Verify access
        var verifyClient = new iManageSyncApiClient(httpClient, authClient, baseUrl, customerId, "verify", metrics, maxRecords);
        if (!await verifyClient.VerifyAccessAsync(ct))
        {
            Console.WriteLine("[Error] Sync API access verification failed. Aborting.");
            return;
        }

        // Resolve library list
        List<string> libraryIds;
        if (libraryIdConfig == "*")
        {
            Console.WriteLine("\n--- Discovering all libraries ---");
            var discoveryClient = new iManageSyncApiClient(httpClient, authClient, baseUrl, customerId, "placeholder", metrics, maxRecords);
            var libraries = await discoveryClient.CrawlLibrariesAsync(pageSize, ct);
            libraryIds = libraries.Select(l => l.GetProperty("id").GetString()!).ToList();
            Console.WriteLine($"[Libraries] Found {libraryIds.Count}: {string.Join(", ", libraryIds)}");
        }
        else
        {
            libraryIds = libraryIdConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            Console.WriteLine($"[Libraries] Configured: {string.Join(", ", libraryIds)}");
        }

        // Check for incomplete sync job to resume
        var existingJob = await db.DmsSyncJobInfos
            .Include(j => j.CrawlProgress)
            .Where(j => j.SyncJobState != 128 && j.SyncJobState != 256) // Not Completed or Error
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);

        DmsSyncJobInfo syncJob;
        bool isResume = false;

        if (existingJob != null)
        {
            var jobAge = DateTimeOffset.UtcNow - existingJob.CreatedAt;
            var completedEndpoints = existingJob.CrawlProgress.Count(p => p.IsComplete);
            var totalEndpoints = existingJob.CrawlProgress.Count;

            Console.WriteLine($"\n[Resume] Found incomplete sync job:");
            Console.WriteLine($"  Job ID:    {existingJob.Id}");
            Console.WriteLine($"  Created:   {existingJob.CreatedAt:g} ({jobAge.TotalHours:F1} hours ago)");
            Console.WriteLine($"  Progress:  {completedEndpoints}/{totalEndpoints} endpoints complete");

            if (jobAge.TotalHours > 1)
                Console.WriteLine($"  [Warning] Job is over 1 hour old — cursors may have expired");

            Console.WriteLine();
            Console.WriteLine("  r - Resume from where it stopped");
            Console.WriteLine("  n - Start a new run (abandon this job)");
            Console.WriteLine("  q - Quit (do nothing)");
            Console.Write("> ");

            var answer = Console.ReadLine()?.Trim().ToLower();

            switch (answer)
            {
                case "r":
                    syncJob = existingJob;
                    isResume = true;
                    Console.WriteLine($"[Resume] Resuming sync job {syncJob.Id}");
                    break;
                case "n":
                    existingJob.SyncJobState = 256;
                    existingJob.ErrorDetails = "Abandoned — new run started";
                    existingJob.FinishedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(ct);

                    syncJob = new DmsSyncJobInfo { SyncJobState = 10, StartedAt = DateTimeOffset.UtcNow };
                    db.DmsSyncJobInfos.Add(syncJob);
                    await db.SaveChangesAsync(ct);
                    Console.WriteLine($"[Scenario 3] New sync job created: {syncJob.Id}");
                    break;
                case "q":
                    Console.WriteLine("[Quit] No action taken.");
                    return;
                default:
                    Console.WriteLine("[Quit] Invalid selection — no action taken.");
                    return;
            }
        }
        else
        {
            syncJob = new DmsSyncJobInfo { SyncJobState = 10, StartedAt = DateTimeOffset.UtcNow };
            db.DmsSyncJobInfos.Add(syncJob);
            await db.SaveChangesAsync(ct);
            Console.WriteLine($"[Scenario 3] Sync job created: {syncJob.Id}");
        }

        metrics.Start();

        var totalDocs = 0;
        var totalFolders = 0;
        var totalWorkspaces = 0;
        var totalDocPerms = 0;
        var totalFolderPerms = 0;
        var totalLibraryUsers = 0;
        var totalGlobalUsers = 0;
        var libraryStats = new List<string>();

        try
        {
            // Crawl global users (not library-scoped)
            await CrawlEndpointWithProgressAsync(db, syncJob.Id, "global", "crawl_global_users",
                async (resumeCursor, resumeTotal) =>
                {
                    var client = new iManageSyncApiClient(httpClient, authClient, baseUrl, customerId, libraryIds[0], metrics, maxRecords);
                    return await client.CrawlGetWithCallbackAsync(
                        iManageEndpoints.CrawlGlobalUsers(baseUrl, customerId),
                        "crawl_global_users", pageSize,
                        async page =>
                        {
                            var entities = MapUsers(page.Items, syncJob.Id);
                            db.DmsSyncJobUsers.AddRange(entities);
                            await db.BulkSaveChangesAsync(cancellationToken: ct);
                            db.ChangeTracker.Clear();
                            db.Attach(syncJob);
                            await UpdateCrawlProgressAsync(db, syncJob.Id, "global", "crawl_global_users", page.Cursor, page.TotalSoFar, ct);
                        },
                        resumeCursor, resumeTotal, ct);
                }, ct);
            totalGlobalUsers = await db.DmsSyncJobUsers.CountAsync(u => u.DmsSyncJobInfoId == syncJob.Id, ct);

            // Crawl each library
            foreach (var libraryId in libraryIds)
            {
                Console.WriteLine($"\n========== Library: {libraryId} ==========");
                var client = new iManageSyncApiClient(httpClient, authClient, baseUrl, customerId, libraryId, metrics, maxRecords);

                var libDocs = 0;
                var libWorkspaces = 0;
                var libFolders = 0;
                var libDocPerms = 0;
                var libFolderPerms = 0;
                var libUsers = 0;

                // Step 1: Crawl documents
                Console.WriteLine("\n--- Step 1: Crawl Document Profiles ---");
                await CrawlEndpointWithProgressAsync(db, syncJob.Id, libraryId, "crawl_documents",
                    async (resumeCursor, resumeTotal) =>
                    {
                        return await client.CrawlPostWithCallbackAsync(
                            iManageEndpoints.CrawlDocuments(baseUrl, customerId, libraryId),
                            "crawl_documents", pageSize,
                            async page =>
                            {
                                var entities = MapDocuments(page.Items, syncJob.Id, libraryId);
                                db.DmsSyncDocuments.AddRange(entities);
                                await db.BulkSaveChangesAsync(cancellationToken: ct);
                                db.ChangeTracker.Clear();
                                db.Attach(syncJob);
                                await UpdateCrawlProgressAsync(db, syncJob.Id, libraryId, "crawl_documents", page.Cursor, page.TotalSoFar, ct);
                            },
                            resumeCursor, resumeTotal, ct);
                    }, ct);
                libDocs = await db.DmsSyncDocuments.CountAsync(d => d.DmsSyncJobInfoId == syncJob.Id && d.Cabinet == libraryId, ct);

                // Step 2: Crawl document parents — two phases: crawl into memory, then batch-update DB
                Console.WriteLine("\n--- Step 2: Crawl Document Parents ---");
                await CrawlEndpointWithProgressAsync(db, syncJob.Id, libraryId, "crawl_document_parents",
                    async (resumeCursor, resumeTotal) =>
                    {
                        // Phase 1: Crawl all parent mappings into a dictionary (small — just docnum + parentId)
                        var parentMap = new Dictionary<int, List<string>>();
                        var totalCrawled = await client.CrawlPostWithCallbackAsync(
                            iManageEndpoints.CrawlDocumentParents(baseUrl, customerId, libraryId),
                            "crawl_document_parents", pageSize,
                            page =>
                            {
                                foreach (var p in page.Items)
                                {
                                    var docNum = p.GetProperty("document_number").GetInt32();
                                    var parentId = p.GetProperty("parent_id").GetString() ?? string.Empty;
                                    if (!parentMap.ContainsKey(docNum))
                                        parentMap[docNum] = [];
                                    if (!parentMap[docNum].Contains(parentId))
                                        parentMap[docNum].Add(parentId);
                                }
                                return Task.CompletedTask;
                            },
                            resumeCursor, resumeTotal, ct);

                        Console.WriteLine($"[Parents] {parentMap.Count} unique document numbers with parents, applying to DB...");

                        // Phase 2: Load documents in batches and update ParentIdsJson via EF Core
                        const int updateBatchSize = 500;
                        var totalDmsDocCount = await db.DmsSyncDocuments
                            .CountAsync(d => d.DmsSyncJobInfoId == syncJob.Id && d.Cabinet == libraryId, ct);
                        var processed = 0;

                        while (processed < totalDmsDocCount)
                        {
                            var batch = await db.DmsSyncDocuments
                                .Where(d => d.DmsSyncJobInfoId == syncJob.Id && d.Cabinet == libraryId)
                                .OrderBy(d => d.Id)
                                .Skip(processed)
                                .Take(updateBatchSize)
                                .ToListAsync(ct);

                            if (batch.Count == 0) break;

                            foreach (var doc in batch)
                            {
                                // Extract document number from DmsId (e.g., "Dev!7525.1" → 7525)
                                var dmsIdParts = doc.DmsId.Split('!');
                                if (dmsIdParts.Length == 2)
                                {
                                    var numPart = dmsIdParts[1].Split('.')[0];
                                    if (int.TryParse(numPart, out var docNum) && parentMap.TryGetValue(docNum, out var parents))
                                    {
                                        doc.ParentIdsJson = parents.ToArray();
                                    }
                                }
                            }

                            await db.BulkSaveChangesAsync(cancellationToken: ct);
                            db.ChangeTracker.Clear();
                            db.Attach(syncJob);
                            processed += batch.Count;
                            Console.WriteLine($"[Parents] Updated {processed}/{totalDmsDocCount} documents");
                        }

                        await UpdateCrawlProgressAsync(db, syncJob.Id, libraryId, "crawl_document_parents", null, totalCrawled, ct);
                        return totalCrawled;
                    }, ct);

                // Step 3: Crawl workspaces
                Console.WriteLine("\n--- Step 3: Crawl Workspaces ---");
                await CrawlEndpointWithProgressAsync(db, syncJob.Id, libraryId, "crawl_workspaces",
                    async (resumeCursor, resumeTotal) =>
                    {
                        return await client.CrawlPostWithCallbackAsync(
                            iManageEndpoints.CrawlWorkspaces(baseUrl, customerId, libraryId),
                            "crawl_workspaces", pageSize,
                            async page =>
                            {
                                var entities = MapWorkspaces(page.Items, syncJob.Id, libraryId);
                                db.DmsSyncFolders.AddRange(entities);
                                await db.BulkSaveChangesAsync(cancellationToken: ct);
                                db.ChangeTracker.Clear();
                                db.Attach(syncJob);
                                await UpdateCrawlProgressAsync(db, syncJob.Id, libraryId, "crawl_workspaces", page.Cursor, page.TotalSoFar, ct);
                            },
                            resumeCursor, resumeTotal, ct);
                    }, ct);
                libWorkspaces = await db.DmsSyncFolders.CountAsync(f => f.DmsSyncJobInfoId == syncJob.Id && f.Cabinet == libraryId && f.IsWorkspace, ct);

                // Step 4: Crawl folders
                Console.WriteLine("\n--- Step 4: Crawl Folders ---");
                await CrawlEndpointWithProgressAsync(db, syncJob.Id, libraryId, "crawl_folders",
                    async (resumeCursor, resumeTotal) =>
                    {
                        return await client.CrawlPostWithCallbackAsync(
                            iManageEndpoints.CrawlFolders(baseUrl, customerId, libraryId),
                            "crawl_folders", pageSize,
                            async page =>
                            {
                                var entities = MapFolders(page.Items, syncJob.Id, libraryId);
                                db.DmsSyncFolders.AddRange(entities);
                                await db.BulkSaveChangesAsync(cancellationToken: ct);
                                db.ChangeTracker.Clear();
                                db.Attach(syncJob);
                                await UpdateCrawlProgressAsync(db, syncJob.Id, libraryId, "crawl_folders", page.Cursor, page.TotalSoFar, ct);
                            },
                            resumeCursor, resumeTotal, ct);
                    }, ct);
                libFolders = await db.DmsSyncFolders.CountAsync(f => f.DmsSyncJobInfoId == syncJob.Id && f.Cabinet == libraryId && !f.IsWorkspace, ct);

                // Step 5: Crawl allowed document trustees
                Console.WriteLine("\n--- Step 5: Crawl Allowed Document Trustees ---");
                await CrawlEndpointWithProgressAsync(db, syncJob.Id, libraryId, "crawl_allowed_doc_trustees",
                    async (resumeCursor, resumeTotal) =>
                    {
                        return await client.CrawlPostWithCallbackAsync(
                            iManageEndpoints.CrawlAllowedDocumentTrustees(baseUrl, customerId, libraryId),
                            "crawl_allowed_doc_trustees", pageSize,
                            async page =>
                            {
                                var entities = MapDocumentPermissions(page.Items, syncJob.Id);
                                db.DmsSyncDocumentPermissions.AddRange(entities);
                                await db.BulkSaveChangesAsync(cancellationToken: ct);
                                db.ChangeTracker.Clear();
                                db.Attach(syncJob);
                                await UpdateCrawlProgressAsync(db, syncJob.Id, libraryId, "crawl_allowed_doc_trustees", page.Cursor, page.TotalSoFar, ct);
                            },
                            resumeCursor, resumeTotal, ct);
                    }, ct);

                // Step 6: Crawl denied document trustees
                Console.WriteLine("\n--- Step 6: Crawl Denied Document Trustees ---");
                await CrawlEndpointWithProgressAsync(db, syncJob.Id, libraryId, "crawl_denied_doc_trustees",
                    async (resumeCursor, resumeTotal) =>
                    {
                        return await client.CrawlPostWithCallbackAsync(
                            iManageEndpoints.CrawlDeniedDocumentTrustees(baseUrl, customerId, libraryId),
                            "crawl_denied_doc_trustees", pageSize,
                            async page =>
                            {
                                var entities = MapDocumentPermissions(page.Items, syncJob.Id);
                                db.DmsSyncDocumentPermissions.AddRange(entities);
                                await db.BulkSaveChangesAsync(cancellationToken: ct);
                                db.ChangeTracker.Clear();
                                db.Attach(syncJob);
                                await UpdateCrawlProgressAsync(db, syncJob.Id, libraryId, "crawl_denied_doc_trustees", page.Cursor, page.TotalSoFar, ct);
                            },
                            resumeCursor, resumeTotal, ct);
                    }, ct);
                libDocPerms = await db.DmsSyncDocumentPermissions.CountAsync(p => p.DmsSyncJobInfoId == syncJob.Id, ct);

                // Step 7: Crawl allowed container trustees
                Console.WriteLine("\n--- Step 7: Crawl Allowed Container Trustees ---");
                await CrawlEndpointWithProgressAsync(db, syncJob.Id, libraryId, "crawl_allowed_container_trustees",
                    async (resumeCursor, resumeTotal) =>
                    {
                        return await client.CrawlPostWithCallbackAsync(
                            iManageEndpoints.CrawlAllowedContainerTrustees(baseUrl, customerId, libraryId),
                            "crawl_allowed_container_trustees", pageSize,
                            async page =>
                            {
                                var entities = MapFolderPermissions(page.Items, syncJob.Id);
                                db.DmsSyncFolderPermissions.AddRange(entities);
                                await db.BulkSaveChangesAsync(cancellationToken: ct);
                                db.ChangeTracker.Clear();
                                db.Attach(syncJob);
                                await UpdateCrawlProgressAsync(db, syncJob.Id, libraryId, "crawl_allowed_container_trustees", page.Cursor, page.TotalSoFar, ct);
                            },
                            resumeCursor, resumeTotal, ct);
                    }, ct);

                // Step 8: Crawl denied container trustees
                Console.WriteLine("\n--- Step 8: Crawl Denied Container Trustees ---");
                await CrawlEndpointWithProgressAsync(db, syncJob.Id, libraryId, "crawl_denied_container_trustees",
                    async (resumeCursor, resumeTotal) =>
                    {
                        return await client.CrawlPostWithCallbackAsync(
                            iManageEndpoints.CrawlDeniedContainerTrustees(baseUrl, customerId, libraryId),
                            "crawl_denied_container_trustees", pageSize,
                            async page =>
                            {
                                var entities = MapFolderPermissions(page.Items, syncJob.Id);
                                db.DmsSyncFolderPermissions.AddRange(entities);
                                await db.BulkSaveChangesAsync(cancellationToken: ct);
                                db.ChangeTracker.Clear();
                                db.Attach(syncJob);
                                await UpdateCrawlProgressAsync(db, syncJob.Id, libraryId, "crawl_denied_container_trustees", page.Cursor, page.TotalSoFar, ct);
                            },
                            resumeCursor, resumeTotal, ct);
                    }, ct);
                libFolderPerms = await db.DmsSyncFolderPermissions.CountAsync(p => p.DmsSyncJobInfoId == syncJob.Id, ct);

                // Step 9: Crawl library users (informational)
                Console.WriteLine("\n--- Step 9: Crawl Library Users ---");
                var libUsersList = await client.CrawlLibraryUsersAsync(pageSize, ct);
                libUsers = libUsersList.Count;
                Console.WriteLine($"[Info] Library users for {libraryId}: {libUsers}");

                // Step 10: Crawl groups + members
                Console.WriteLine("\n--- Step 10: Crawl Groups & Members ---");
                var groups = await client.CrawlGroupsAsync(pageSize, ct);
                var groupEntities = MapCabinetGroups(groups, syncJob.Id, libraryId);
                db.DmsSyncJobCabinetGroups.AddRange(groupEntities);
                await db.BulkSaveChangesAsync(cancellationToken: ct);
                db.ChangeTracker.Clear();
                db.Attach(syncJob);
                Console.WriteLine($"[DB] Saved {groupEntities.Count} cabinet groups");

                var groupMembers = await client.CrawlGroupMembersAsync(pageSize, ct);
                var memberEntities = MapGroupMembers(groupMembers, syncJob.Id);
                db.DmsSyncJobGroupMembers.AddRange(memberEntities);
                await db.BulkSaveChangesAsync(cancellationToken: ct);
                db.ChangeTracker.Clear();
                db.Attach(syncJob);
                Console.WriteLine($"[DB] Saved {memberEntities.Count} group members");

                // Per-library stats
                libraryStats.Add($"{libraryId}: docs={libDocs}, workspaces={libWorkspaces}, " +
                    $"folders={libFolders}, docPerms={libDocPerms}, folderPerms={libFolderPerms}, " +
                    $"libUsers={libUsers}, groups={groupEntities.Count}, groupMembers={memberEntities.Count}");

                totalDocs += libDocs;
                totalWorkspaces += libWorkspaces;
                totalFolders += libFolders;
                totalDocPerms += libDocPerms;
                totalFolderPerms += libFolderPerms;
                totalLibraryUsers += libUsers;
            }

            // Update sync job
            syncJob.SyncJobState = 128; // Completed
            syncJob.FinishedAt = DateTimeOffset.UtcNow;
            syncJob.LastModifiedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            metrics.Stop();

            // Save benchmark run
            var librariesCrawled = string.Join(", ", libraryIds);
            var run = new BenchmarkRun
            {
                RunId = metrics.RunId,
                Scenario = Name,
                Library = librariesCrawled,
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
                DocumentsDiscovered = totalDocs,
                FoldersDiscovered = totalFolders,
                WorkspacesDiscovered = totalWorkspaces,
                PermissionRecords = totalDocPerms + totalFolderPerms,
                PeakMemoryMb = metrics.PeakMemoryMb,
                Notes = $"Libraries={librariesCrawled}, PageSize={pageSize}, MaxRecords={maxRecords}, " +
                    $"FullCrawl={maxRecords == 0}, AllLibraries={libraryIdConfig == "*"}, Resumed={isResume}, " +
                    $"GlobalUsers={totalGlobalUsers}, LibraryUsers={totalLibraryUsers} | " +
                    string.Join(" | ", libraryStats)
            };
            db.BenchmarkRuns.Add(run);
            await db.SaveChangesAsync(ct);

            metrics.PrintSummary(Name);
        }
        catch (Exception ex)
        {
            metrics.Stop();
            Console.WriteLine($"[Error] {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"[Inner] {ex.InnerException.Message}");
            if (ex.InnerException?.InnerException != null)
                Console.WriteLine($"[Inner2] {ex.InnerException.InnerException.Message}");

            try
            {
                syncJob.SyncJobState = 20; // InProgress but interrupted — can be resumed
                syncJob.ErrorDetails = ex.InnerException?.Message ?? ex.Message;
                syncJob.LastModifiedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                Console.WriteLine("[Info] Sync job saved as resumable — run again to continue from where it stopped");
            }
            catch { /* ignore save errors during error handling */ }
        }
    }

    // ==================== Progress Tracking ====================

    private static async Task CrawlEndpointWithProgressAsync(PocDbContext db, Guid syncJobId,
        string libraryId, string endpointName,
        Func<string?, int, Task<int>> crawlAction, CancellationToken ct)
    {
        // Check if this endpoint is already complete
        var progress = await db.DmsSyncCrawlProgress
            .FirstOrDefaultAsync(p => p.DmsSyncJobInfoId == syncJobId
                && p.LibraryId == libraryId
                && p.EndpointName == endpointName, ct);

        if (progress?.IsComplete == true)
        {
            Console.WriteLine($"[Progress] {endpointName} ({libraryId}): already complete ({progress.RecordsSaved} records) — skipping");
            return;
        }

        var resumeCursor = progress?.LastCursor;
        var resumeTotal = progress?.RecordsSaved ?? 0;

        // Create progress record if it doesn't exist
        if (progress == null)
        {
            progress = new DmsSyncCrawlProgress
            {
                DmsSyncJobInfoId = syncJobId,
                LibraryId = libraryId,
                EndpointName = endpointName
            };
            db.DmsSyncCrawlProgress.Add(progress);
            await db.SaveChangesAsync(ct);
        }

        // Run the crawl — the callback updates DB per page
        var totalRecords = await crawlAction(resumeCursor, resumeTotal);

        // Mark as complete
        progress.IsComplete = true;
        progress.RecordsSaved = totalRecords;
        progress.LastUpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Helper to update crawl progress cursor after each page is saved.
    /// Call this inside the per-page callback after saving entities.
    /// </summary>
    private static async Task UpdateCrawlProgressAsync(PocDbContext db, Guid syncJobId,
        string libraryId, string endpointName, string? cursor, int totalRecords, CancellationToken ct)
    {
        var progress = await db.DmsSyncCrawlProgress
            .FirstOrDefaultAsync(p => p.DmsSyncJobInfoId == syncJobId
                && p.LibraryId == libraryId
                && p.EndpointName == endpointName, ct);

        if (progress != null)
        {
            progress.LastCursor = cursor;
            progress.RecordsSaved = totalRecords;
            progress.LastUpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    // ==================== Mapping Methods ====================

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

    private static List<DmsSyncFolder> MapWorkspaces(List<JsonElement> workspaces, Guid jobId, string libraryId)
    {
        return workspaces.Select(w => new DmsSyncFolder
        {
            DmsSyncJobInfoId = jobId,
            DmsId = w.GetProperty("id").GetString() ?? string.Empty,
            ParentDmsId = null,
            Name = w.GetProperty("name").GetString() ?? string.Empty,
            Source = 3,
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
            Source = 3,
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
}
