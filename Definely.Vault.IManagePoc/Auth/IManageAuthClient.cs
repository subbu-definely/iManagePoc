using System.Text.Json;
using System.Text.Json.Serialization;

namespace Definely.Vault.IManagePoc.Auth;

public class iManageAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly string _authUrl;
    private readonly string _username;
    private readonly string _password;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public int TokenRefreshCount { get; private set; }

    public iManageAuthClient(HttpClient httpClient, string authUrl, string username, string password, string clientId, string clientSecret)
    {
        _httpClient = httpClient;
        _authUrl = authUrl;
        _username = username;
        _password = password;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-2))
        {
            return _accessToken;
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = _username,
            ["password"] = _password,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret
        });

        var response = await _httpClient.PostAsync(_authUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

        _accessToken = tokenResponse!.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        TokenRefreshCount++;

        Console.WriteLine($"[Auth] Token obtained, expires in {tokenResponse.ExpiresIn}s (refresh #{TokenRefreshCount})");
        return _accessToken;
    }
}

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
