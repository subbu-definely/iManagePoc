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

    public iManageSyncApiClient(HttpClient httpClient, iManageAuthClient authClient, string baseUrl, int customerId, string libraryId, BenchmarkMetrics metrics, int maxRecords = 0)
    {
        _httpClient = httpClient;
        _authClient = authClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _customerId = customerId;
        _libraryId = libraryId;
        _metrics = metrics;
        _maxRecords = maxRecords; // 0 = no limit
    }

    public async Task<List<JsonElement>> CrawlLibrariesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/platform/api/v2/customers/{_customerId}/sync/libraries/crawl",
            "crawl_libraries", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlDocumentsAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/work/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/documents/crawl",
            "crawl_documents", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlDocumentParentsAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/work/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/documents/parents/search",
            "crawl_document_parents", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlFoldersAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/work/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/folders/crawl",
            "crawl_folders", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlWorkspacesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/work/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/workspaces/crawl",
            "crawl_workspaces", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlAllowedDocumentTrusteesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/work/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/documents/security/allowed-trustees/search",
            "crawl_allowed_doc_trustees", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlDeniedDocumentTrusteesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/work/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/documents/security/denied-trustees/search",
            "crawl_denied_doc_trustees", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlAllowedContainerTrusteesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/work/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/containers/security/allowed-trustees/search",
            "crawl_allowed_container_trustees", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlDeniedContainerTrusteesAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/work/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/containers/security/denied-trustees/search",
            "crawl_denied_container_trustees", pageSize, ct);
    }

    public async Task<List<JsonElement>> CrawlGlobalUsersAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        var token = await _authClient.GetAccessTokenAsync(ct);
        var url = $"{_baseUrl}/platform/api/v2/customers/{_customerId}/sync/users?limit={pageSize}";
        var allResults = new List<JsonElement>();

        while (true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Auth-Token", token);
            _metrics.IncrementApiCall("crawl_global_users");

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var item in data.EnumerateArray())
                    allResults.Add(item.Clone());
            }

            if (doc.RootElement.TryGetProperty("cursor", out var cursor) && data.GetArrayLength() >= pageSize)
            {
                if (_maxRecords > 0 && allResults.Count >= _maxRecords) break;
                url = $"{_baseUrl}/platform/api/v2/customers/{_customerId}/sync/users?limit={pageSize}&cursor={cursor.GetString()}";
                token = await _authClient.GetAccessTokenAsync(ct);
            }
            else break;
        }

        Console.WriteLine($"[Crawl] Global users: {allResults.Count} records");
        return allResults;
    }

    public async Task<List<JsonElement>> CrawlLibraryUsersAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        var token = await _authClient.GetAccessTokenAsync(ct);
        var url = $"{_baseUrl}/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/users?limit={pageSize}";
        var allResults = new List<JsonElement>();

        while (true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Auth-Token", token);
            _metrics.IncrementApiCall("crawl_library_users");

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var item in data.EnumerateArray())
                    allResults.Add(item.Clone());
            }

            if (doc.RootElement.TryGetProperty("cursor", out var cursor) && data.GetArrayLength() >= pageSize)
            {
                if (_maxRecords > 0 && allResults.Count >= _maxRecords) break;
                url = $"{_baseUrl}/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/users?limit={pageSize}&cursor={cursor.GetString()}";
                token = await _authClient.GetAccessTokenAsync(ct);
            }
            else break;
        }

        Console.WriteLine($"[Crawl] Library users: {allResults.Count} records");
        return allResults;
    }

    public async Task<List<JsonElement>> CrawlGroupsAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        var token = await _authClient.GetAccessTokenAsync(ct);
        var url = $"{_baseUrl}/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/groups?limit={pageSize}";
        var allResults = new List<JsonElement>();

        while (true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Auth-Token", token);
            _metrics.IncrementApiCall("crawl_groups");

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var item in data.EnumerateArray())
                    allResults.Add(item.Clone());
            }

            if (doc.RootElement.TryGetProperty("cursor", out var cursor) && data.GetArrayLength() >= pageSize)
            {
                if (_maxRecords > 0 && allResults.Count >= _maxRecords) break;
                url = $"{_baseUrl}/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/groups?limit={pageSize}&cursor={cursor.GetString()}";
                token = await _authClient.GetAccessTokenAsync(ct);
            }
            else break;
        }

        Console.WriteLine($"[Crawl] Groups: {allResults.Count} records");
        return allResults;
    }

    public async Task<List<JsonElement>> CrawlGroupMembersAsync(int pageSize = 1000, CancellationToken ct = default)
    {
        return await CrawlAsync(
            $"{_baseUrl}/work/api/v2/customers/{_customerId}/libraries/{_libraryId}/sync/groups/members/search",
            "crawl_group_members", pageSize, ct);
    }

    private async Task<List<JsonElement>> CrawlAsync(string url, string metricName, int pageSize, CancellationToken ct)
    {
        var allResults = new List<JsonElement>();
        string? cursor = null;

        while (true)
        {
            var token = await _authClient.GetAccessTokenAsync(ct);

            var body = cursor == null
                ? new { limit = pageSize }
                : (object)new { limit = pageSize, cursor };

            var requestBody = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Auth-Token", token);
            request.Content = requestBody;

            _metrics.IncrementApiCall(metricName);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            var dataArray = doc.RootElement.GetProperty("data");
            var count = dataArray.GetArrayLength();

            foreach (var item in dataArray.EnumerateArray())
                allResults.Add(item.Clone());

            if (count < pageSize || !doc.RootElement.TryGetProperty("cursor", out var cursorElement))
                break;

            if (_maxRecords > 0 && allResults.Count >= _maxRecords)
            {
                Console.WriteLine($"[Crawl] {metricName}: reached max records limit ({_maxRecords})");
                break;
            }

            cursor = cursorElement.GetString();
        }

        Console.WriteLine($"[Crawl] {metricName}: {allResults.Count} records");
        return allResults;
    }
}
