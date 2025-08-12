using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AiStudio4.McpStandalone.Helpers;

namespace AiStudio4.McpStandalone.Services;

/// <summary>
/// Manages OAuth client registration and persistence for the MCP Standalone app
/// </summary>
public class OAuthClientManager
{
    private readonly ILogger<OAuthClientManager> _logger;
    private readonly HttpClient _httpClient;
    private const string CLIENT_CREDENTIALS_FILE = "mcp_oauth_client.json";
    private const string CLIENT_NAME = "MCP Standalone Client";
    private string? _clientId;
    private string? _clientSecret;

    public string? ClientId => _clientId;
    public string? ClientSecret => _clientSecret;

    public OAuthClientManager(ILogger<OAuthClientManager> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Ensures we have a valid OAuth client registered
    /// </summary>
    public async Task<bool> EnsureClientRegisteredAsync()
    {
        try
        {
            // Wait for OAuth server to be ready
            await Task.Delay(2000);

            var credentialsPath = Path.Combine(PathHelper.GetProfileSubPath("OAuth"), CLIENT_CREDENTIALS_FILE);
            _logger.LogInformation("Looking for OAuth client credentials at: {Path}", credentialsPath);
            
            // Try to load existing credentials
            if (File.Exists(credentialsPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(credentialsPath);
                    var credentials = JsonSerializer.Deserialize<JsonElement>(json);
                    
                    if (credentials.TryGetProperty("client_id", out var clientId) &&
                        credentials.TryGetProperty("client_secret", out var clientSecret))
                    {
                        _clientId = clientId.GetString();
                        _clientSecret = clientSecret.GetString();
                        _logger.LogInformation("Found stored OAuth client: {ClientId}", _clientId);
                        
                        // Check if this client still exists in the OAuth server
                        var isValid = await ValidateClientExistsAsync();
                        _logger.LogInformation("Client validation result: {IsValid}", isValid);
                        
                        if (isValid)
                        {
                            _logger.LogInformation("Using existing OAuth client: {ClientId}", _clientId);
                            return true;
                        }
                        else
                        {
                            _logger.LogWarning("Stored OAuth client {ClientId} no longer valid, will register new one", _clientId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load OAuth credentials");
                }
            }

            // Register a new client
            return await RegisterNewClientAsync(credentialsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure OAuth client registration");
            return false;
        }
    }

    private async Task<bool> ValidateClientExistsAsync()
    {
        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            return false;

        try
        {
            // Try to get a token with the stored credentials (using client_credentials grant)
            // This is just to validate the client exists
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("scope", "mcp:*")
            });

            var response = await _httpClient.PostAsync("http://localhost:7029/token", tokenRequest);
            
            // If we get 400 with invalid_client, the client doesn't exist
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                if (error.Contains("invalid_client", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // If we can't validate, assume it doesn't exist
            return false;
        }
    }

    private async Task<bool> RegisterNewClientAsync(string credentialsPath)
    {
        try
        {
            var registrationRequest = new
            {
                client_name = CLIENT_NAME,
                redirect_uris = new[] { "http://localhost:1179/callback" },
                grant_types = new[] { "authorization_code", "refresh_token", "client_credentials" },
                response_types = new[] { "code" },
                scope = "openid profile mcp:*"
            };

            var json = JsonSerializer.Serialize(registrationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:7029/register", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var registrationResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                if (registrationResponse.TryGetProperty("client_id", out var clientId) &&
                    registrationResponse.TryGetProperty("client_secret", out var clientSecret))
                {
                    _clientId = clientId.GetString();
                    _clientSecret = clientSecret.GetString();
                    
                    _logger.LogInformation("Registered new OAuth client: {ClientId}", _clientId);
                    
                    // Save credentials
                    var credentialsToSave = new
                    {
                        client_id = _clientId,
                        client_secret = _clientSecret,
                        registered_at = DateTime.UtcNow,
                        client_name = CLIENT_NAME
                    };
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(credentialsPath)!);
                    await File.WriteAllTextAsync(credentialsPath, 
                        JsonSerializer.Serialize(credentialsToSave, new JsonSerializerOptions { WriteIndented = true }));
                    
                    _logger.LogInformation("Saved OAuth client credentials");
                    return true;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to register OAuth client: {StatusCode} - {Error}", 
                    response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering new OAuth client");
        }
        
        return false;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}