namespace Definely.Vault.IManagePoc.Client;

public static class iManageEndpoints
{
    // Authentication
    public static string OAuthToken(string serverUrl) =>
        $"{serverUrl}/auth/oauth2/token";

    public static string ApiInfo(string baseUrl) =>
        $"{baseUrl}/api";

    public static string Features(string baseUrl, int customerId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/features";

    // Sync Export / Crawl endpoints
    public static string CrawlLibraries(string baseUrl, int customerId) =>
        $"{baseUrl}/platform/api/v2/customers/{customerId}/sync/libraries/crawl";

    public static string CrawlDocuments(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/libraries/{libraryId}/sync/documents/crawl";

    public static string CrawlDocumentParents(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/libraries/{libraryId}/sync/documents/parents/search";

    public static string CrawlFolders(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/libraries/{libraryId}/sync/folders/crawl";

    public static string CrawlWorkspaces(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/libraries/{libraryId}/sync/workspaces/crawl";

    public static string CrawlAllowedDocumentTrustees(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/libraries/{libraryId}/sync/documents/security/allowed-trustees/search";

    public static string CrawlDeniedDocumentTrustees(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/libraries/{libraryId}/sync/documents/security/denied-trustees/search";

    public static string CrawlAllowedContainerTrustees(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/libraries/{libraryId}/sync/containers/security/allowed-trustees/search";

    public static string CrawlDeniedContainerTrustees(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/libraries/{libraryId}/sync/containers/security/denied-trustees/search";

    public static string CrawlGlobalUsers(string baseUrl, int customerId) =>
        $"{baseUrl}/platform/api/v2/customers/{customerId}/sync/users";

    public static string CrawlLibraryUsers(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/api/v2/customers/{customerId}/libraries/{libraryId}/sync/users";

    public static string CrawlGroups(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/api/v2/customers/{customerId}/libraries/{libraryId}/sync/groups";

    public static string CrawlGroupMembers(string baseUrl, int customerId, string libraryId) =>
        $"{baseUrl}/work/api/v2/customers/{customerId}/libraries/{libraryId}/sync/groups/members/search";
}
