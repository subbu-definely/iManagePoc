using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Definely.Vault.IManagePoc.Auth;
using Definely.Vault.IManagePoc.Metrics;

namespace Definely.Vault.IManagePoc.Client;

public class iManageSyncApiClient
{
    private readonly HttpClient _httpClient;
    private readonly iManageAuthClient _authClient;
    private readonly string _baseUrl;
    private readonly int _customerId;
    private readonly string _libraryId;
    private readonly BenchmarkMetrics _metrics;
    private readonly int _maxRecords;

    // Rate limit tracking
    private int _rateLimitRemaining = int.MaxValue;
    private int _rateLimitResetSeconds = 0;
    private const int RateLimitSlowdownThreshold = 10;
    private const int RateLimitSlowdownDelayMs = 500;

    public iManageSyncApiClient(HttpClient httpClient, iManageAuthClient authClient, string baseUrl, int customerId, string libraryId, BenchmarkMetrics metrics, int maxRecords = 0)
    {
        _httpClient = httpClient;
        _authClient = authClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _customerId = customerId;
        _libraryId = libraryId;
        _metrics = metrics;
        _maxRecords = maxRecords;
    }

    /// <summary>
    /// Verify the Sync API is available and we're authenticated as a system user.
    /// </summary>
    public async Task<bool> VerifyAccessAsync(CancellationToken ct = default)
    {
        var token = await _authClient.GetAccessTokenAsync(ct);

        // Check API info — verify user_type is "system"
        var apiRequest = new HttpRequestMessage(HttpMethod.Get, iManageEndpoints.ApiInfo(_baseUrl));
        apiRequest.Headers.Add("X-Auth-Token", token);
        var apiResponse = await _httpClient.SendAsync(apiRequest, ct);
        apiResponse.EnsureSuccessStatusCode();

        var apiJson = await apiResponse.Content.ReadAsStringAsync(ct);
        var apiDoc = JsonDocument.Parse(apiJson);
        var userType = apiDoc.RootElement.GetProperty("data").GetProperty("user").GetProperty("user_type").GetString();

        if (userType != "system")
        {
            Console.WriteLine($"[Warning] Authenticated as user_type '{userType}' — expected 'system' for Sync API access");
            return false;
        }

        Console.WriteLine($"[Verify] Authenticated as system user — Sync API access confirmed");

        // Check features — verify data_sync_import if available
        try
        {
            var featuresRequest = new HttpRequestMessage(HttpMethod.Get, iManageEndpoints.Features(_baseUrl, _customerId));
            featuresRequest.Headers.Add("X-Auth-Token", token);
            var featuresResponse = await _httpClient.SendAsync(featuresRequest, ct);

            if (featuresResponse.IsSuccessStatusCode)
            {
                var featuresJson = await featuresResponse.Content.ReadAsStringAsync(ct);
                var featuresDoc = JsonDocument.Parse(featuresJson);
                if (featuresDoc.RootElement.TryGetProperty("data", out var featuresData) &&
                    featuresData.TryGetProperty("data_sync_import", out var syncImport))
                {
                    Console.WriteLine($"[Verify] data_sync_import: {syncImport.GetBoolean()}");
                }
            }
        }
        catch
        {
            Console.WriteLine("[Verify] Could not check features — continuing anyway");
        }

        return true;
    }

    // ==================== Crawl Endpoints ====================

    public async Task<List<JsonElement>> CrawlLibrariesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlLibraries(_baseUrl, _customerId),
            "crawl_libraries", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlDocumentsAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlDocuments(_baseUrl, _customerId, _libraryId),
            "crawl_documents", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlDocumentParentsAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlDocumentParents(_baseUrl, _customerId, _libraryId),
            "crawl_document_parents", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlFoldersAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlFolders(_baseUrl, _customerId, _libraryId),
            "crawl_folders", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlWorkspacesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlWorkspaces(_baseUrl, _customerId, _libraryId),
            "crawl_workspaces", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlAllowedDocumentTrusteesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlAllowedDocumentTrustees(_baseUrl, _customerId, _libraryId),
            "crawl_allowed_doc_trustees", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlDeniedDocumentTrusteesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlDeniedDocumentTrustees(_baseUrl, _customerId, _libraryId),
            "crawl_denied_doc_trustees", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlAllowedContainerTrusteesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlAllowedContainerTrustees(_baseUrl, _customerId, _libraryId),
            "crawl_allowed_container_trustees", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlDeniedContainerTrusteesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlDeniedContainerTrustees(_baseUrl, _customerId, _libraryId),
            "crawl_denied_container_trustees", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlGlobalUsersAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlGetAsync(
            iManageEndpoints.CrawlGlobalUsers(_baseUrl, _customerId),
            "crawl_global_users", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlLibraryUsersAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlGetAsync(
            iManageEndpoints.CrawlLibraryUsers(_baseUrl, _customerId, _libraryId),
            "crawl_library_users", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlGroupsAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlGetAsync(
            iManageEndpoints.CrawlGroups(_baseUrl, _customerId, _libraryId),
            "crawl_groups", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlGroupMembersAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlPostAsync(
            iManageEndpoints.CrawlGroupMembers(_baseUrl, _customerId, _libraryId),
            "crawl_group_members", pageSize, ct);
    }

    // ==================== Core Crawl Methods ====================

    /// <summary>
    /// Crawl result for a single page, including the cursor for resume support.
    /// </summary>
    public record CrawlPageResult(List<JsonElement> Items, string? Cursor, int PageNumber, int TotalSoFar);

    /// <summary>
    /// POST-based crawl with per-page callback for incremental saving.
    /// onPage is called after each page is fetched. resumeCursor allows resuming from a previous cursor.
    /// </summary>
    public async Task<int> CrawlPostWithCallbackAsync(string url, string metricName, int pageSize,
        Func<CrawlPageResult, Task> onPage, string? resumeCursor = null, int resumeTotal = 0, CancellationToken ct = default)
    {
        var totalRecords = resumeTotal;
        var pageNumber = resumeTotal > 0 ? (resumeTotal / pageSize) + 1 : 1;
        string? cursor = resumeCursor;

        if (resumeCursor != null)
            Console.WriteLine($"[Crawl] {metricName}: resuming from cursor (page ~{pageNumber}, {resumeTotal} records already saved)");

        while (true)
        {
            await ThrottleIfNeededAsync(ct);

            var token = await _authClient.GetAccessTokenAsync(ct);

            var body = cursor == null
                ? new { limit = pageSize }
                : (object)new { limit = pageSize, cursor };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Auth-Token", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            _metrics.IncrementApiCall(metricName);

            var response = await SendWithRetryAsync(request, metricName, ct);

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            var dataArray = doc.RootElement.GetProperty("data");
            var count = dataArray.GetArrayLength();

            var pageItems = new List<JsonElement>();
            foreach (var item in dataArray.EnumerateArray())
                pageItems.Add(item.Clone());

            totalRecords += count;

            // Get cursor before calling callback
            string? nextCursor = null;
            if (doc.RootElement.TryGetProperty("cursor", out var cursorElement))
                nextCursor = cursorElement.GetString();

            // Call the per-page callback — this is where the caller saves to DB
            await onPage(new CrawlPageResult(pageItems, nextCursor, pageNumber, totalRecords));

            Console.WriteLine($"[Crawl] {metricName}: page {pageNumber} — {count} records ({totalRecords} total)");

            if (count < pageSize || nextCursor == null)
                break;

            if (_maxRecords > 0 && totalRecords >= _maxRecords)
            {
                Console.WriteLine($"[Crawl] {metricName}: reached max records limit ({_maxRecords})");
                break;
            }

            cursor = nextCursor;
            pageNumber++;
        }

        Console.WriteLine($"[Crawl] {metricName}: complete — {totalRecords} records");
        return totalRecords;
    }

    /// <summary>
    /// GET-based crawl with per-page callback for incremental saving.
    /// </summary>
    public async Task<int> CrawlGetWithCallbackAsync(string baseEndpointUrl, string metricName, int pageSize,
        Func<CrawlPageResult, Task> onPage, string? resumeCursor = null, int resumeTotal = 0, CancellationToken ct = default)
    {
        var totalRecords = resumeTotal;
        var pageNumber = resumeTotal > 0 ? (resumeTotal / pageSize) + 1 : 1;
        var url = resumeCursor != null
            ? $"{baseEndpointUrl}?limit={pageSize}&cursor={resumeCursor}"
            : $"{baseEndpointUrl}?limit={pageSize}";

        if (resumeCursor != null)
            Console.WriteLine($"[Crawl] {metricName}: resuming from cursor (page ~{pageNumber}, {resumeTotal} records already saved)");

        while (true)
        {
            await ThrottleIfNeededAsync(ct);

            var token = await _authClient.GetAccessTokenAsync(ct);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Auth-Token", token);

            _metrics.IncrementApiCall(metricName);

            var response = await SendWithRetryAsync(request, metricName, ct);

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var data))
                break;

            var pageItems = new List<JsonElement>();
            foreach (var item in data.EnumerateArray())
                pageItems.Add(item.Clone());

            totalRecords += pageItems.Count;

            string? nextCursor = null;
            if (doc.RootElement.TryGetProperty("cursor", out var cursor) && data.GetArrayLength() >= pageSize)
                nextCursor = cursor.GetString();

            await onPage(new CrawlPageResult(pageItems, nextCursor, pageNumber, totalRecords));

            Console.WriteLine($"[Crawl] {metricName}: page {pageNumber} — {pageItems.Count} records ({totalRecords} total)");

            if (nextCursor == null || data.GetArrayLength() < pageSize)
                break;

            if (_maxRecords > 0 && totalRecords >= _maxRecords)
            {
                Console.WriteLine($"[Crawl] {metricName}: reached max records limit ({_maxRecords})");
                break;
            }

            url = $"{baseEndpointUrl}?limit={pageSize}&cursor={nextCursor}";
            pageNumber++;
        }

        Console.WriteLine($"[Crawl] {metricName}: complete — {totalRecords} records");
        return totalRecords;
    }

    /// <summary>
    /// Convenience: POST-based crawl that returns all results in memory (for small datasets).
    /// </summary>
    private async Task<List<JsonElement>> CrawlPostAsync(string url, string metricName, int pageSize, CancellationToken ct)
    {
        var allResults = new List<JsonElement>();
        await CrawlPostWithCallbackAsync(url, metricName, pageSize,
            page => { allResults.AddRange(page.Items); return Task.CompletedTask; },
            ct: ct);
        return allResults;
    }

    /// <summary>
    /// Convenience: GET-based crawl that returns all results in memory (for small datasets).
    /// </summary>
    private async Task<List<JsonElement>> CrawlGetAsync(string baseEndpointUrl, string metricName, int pageSize, CancellationToken ct)
    {
        var allResults = new List<JsonElement>();
        await CrawlGetWithCallbackAsync(baseEndpointUrl, metricName, pageSize,
            page => { allResults.AddRange(page.Items); return Task.CompletedTask; },
            ct: ct);
        return allResults;
    }

    // ==================== HTTP Retry + Rate Limiting ====================

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, string metricName, CancellationToken ct, int maxRetries = 3)
    {
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                var clonedRequest = await CloneRequestAsync(request);
                response = await _httpClient.SendAsync(clonedRequest, ct);
            }
            catch (Exception ex) when (
                (ex is HttpRequestException || ex is IOException || ex.InnerException is IOException)
                && attempt < maxRetries)
            {
                var delay = Math.Pow(2, attempt + 1);
                Console.WriteLine($"[Transport] {metricName}: {ex.InnerException?.Message ?? ex.Message}");
                Console.WriteLine($"[Transport] Retrying in {delay:F0}s (attempt {attempt + 1}/{maxRetries})");
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                continue;
            }

            // Read rate limit headers from every response
            ReadRateLimitHeaders(response, metricName);

            // Handle 429 (Too Many Requests)
            if ((int)response.StatusCode == 429)
            {
                if (attempt == maxRetries)
                {
                    Console.WriteLine($"[RateLimit] {metricName}: max retries ({maxRetries}) exceeded on 429");
                    response.EnsureSuccessStatusCode();
                }

                var retryAfter = GetRetryAfterSeconds(response) ?? Math.Pow(2, attempt + 1);
                Console.WriteLine($"[RateLimit] {metricName}: 429 Too Many Requests, waiting {retryAfter:F0}s (attempt {attempt + 1}/{maxRetries})");
                await Task.Delay(TimeSpan.FromSeconds(retryAfter), ct);
                continue;
            }

            // Handle 502 (Bad Gateway — may be partial success/failure)
            if (response.StatusCode == HttpStatusCode.BadGateway)
            {
                if (attempt == maxRetries)
                {
                    Console.WriteLine($"[Retry] {metricName}: max retries ({maxRetries}) exceeded on 502");
                    response.EnsureSuccessStatusCode();
                }

                var delay = Math.Pow(2, attempt + 1);
                Console.WriteLine($"[Retry] {metricName}: 502 Bad Gateway, retrying in {delay:F0}s (attempt {attempt + 1}/{maxRetries})");
                await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                continue;
            }

            // Handle 503 (Service Unavailable)
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                if (attempt == maxRetries)
                {
                    Console.WriteLine($"[Retry] {metricName}: max retries ({maxRetries}) exceeded on 503");
                    response.EnsureSuccessStatusCode();
                }

                var retryAfter = GetRetryAfterSeconds(response) ?? Math.Pow(2, attempt + 1);
                Console.WriteLine($"[Retry] {metricName}: 503 Service Unavailable, waiting {retryAfter:F0}s (attempt {attempt + 1}/{maxRetries})");
                await Task.Delay(TimeSpan.FromSeconds(retryAfter), ct);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return response;
        }

        throw new InvalidOperationException("Retry loop exited unexpectedly");
    }

    private void ReadRateLimitHeaders(HttpResponseMessage response, string metricName)
    {
        if (response.Headers.TryGetValues("x-ratelimit-remaining", out var remainingValues))
        {
            if (int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
            {
                _rateLimitRemaining = remaining;
                _metrics.LogRateLimitInfo(metricName, remaining);

                if (remaining <= RateLimitSlowdownThreshold && remaining > 0)
                {
                    Console.WriteLine($"[RateLimit] {metricName}: remaining={remaining} — approaching limit");
                }
            }
        }

        if (response.Headers.TryGetValues("x-ratelimit-reset", out var resetValues))
        {
            if (int.TryParse(resetValues.FirstOrDefault(), out var reset))
            {
                _rateLimitResetSeconds = reset;
            }
        }
    }

    private double? GetRetryAfterSeconds(HttpResponseMessage response)
    {
        // Check x-ratelimit-retryafter first (iManage specific)
        if (response.Headers.TryGetValues("x-ratelimit-retryafter", out var retryAfterValues))
        {
            if (double.TryParse(retryAfterValues.FirstOrDefault(), out var retryAfter))
                return retryAfter;
        }

        // Check x-ratelimit-reset
        if (response.Headers.TryGetValues("x-ratelimit-reset", out var resetValues))
        {
            if (double.TryParse(resetValues.FirstOrDefault(), out var reset))
                return reset;
        }

        // Check standard Retry-After header
        if (response.Headers.RetryAfter?.Delta != null)
            return response.Headers.RetryAfter.Delta.Value.TotalSeconds;

        return null;
    }

    private async Task ThrottleIfNeededAsync(CancellationToken ct)
    {
        if (_rateLimitRemaining <= RateLimitSlowdownThreshold && _rateLimitRemaining > 0)
        {
            Console.WriteLine($"[Throttle] Rate limit remaining={_rateLimitRemaining}, slowing down ({RateLimitSlowdownDelayMs}ms)");
            await Task.Delay(RateLimitSlowdownDelayMs, ct);
        }
        else if (_rateLimitRemaining == 0)
        {
            var waitSeconds = _rateLimitResetSeconds > 0 ? _rateLimitResetSeconds : 5;
            Console.WriteLine($"[Throttle] Rate limit exhausted, waiting {waitSeconds}s for reset");
            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), ct);
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            clone.Content = new StringContent(content, Encoding.UTF8,
                request.Content.Headers.ContentType?.MediaType ?? "application/json");
        }

        return clone;
    }
}
