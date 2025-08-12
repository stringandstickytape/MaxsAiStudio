using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AiStudio4.McpStandalone.Helpers;

namespace AiStudio4.McpStandalone.Services;

/// <summary>
/// Helper class to initialize the OAuth server with default clients
/// </summary>
public class OAuthServerInitializer : IDisposable
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private const string OAUTH_CLIENT_FILE = "oauth_client_credentials.json";
    private const string FIXED_CLIENT_ID = "mcp-standalone-client-001";
    private const string FIXED_CLIENT_NAME = "MCP Standalone Client";

    public OAuthServerInitializer(ILogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Registers a default test client with the OAuth server or reuses existing credentials
    /// </summary>
    public async Task RegisterDefaultClientAsync()
    {
        try
        {
            // Wait a moment for the server to be fully ready
            await Task.Delay(2000);

            // Check if we have stored client credentials
            var credentialsPath = Path.Combine(PathHelper.GetProfileSubPath("OAuth"), OAUTH_CLIENT_FILE);
            
            if (File.Exists(credentialsPath))
            {
                try
                {
                    // Load and use existing credentials
                    var existingCredentials = await File.ReadAllTextAsync(credentialsPath);
                    var credentials = JsonSerializer.Deserialize<JsonElement>(existingCredentials);
                    
                    if (credentials.TryGetProperty("client_id", out var clientId) &&
                        credentials.TryGetProperty("client_secret", out var clientSecret))
                    {
                        _logger.LogInformation("Using existing OAuth client credentials");
                        _logger.LogInformation("Client ID: {ClientId}", clientId.GetString());
                        _logger.LogInformation("Client Secret: {ClientSecret}", clientSecret.GetString());
                        return; // Use existing credentials
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load existing OAuth credentials, will register new client");
                }
            }

            // Register new client if no valid credentials exist
            // Use a fixed client ID to ensure persistence
            var registrationRequest = new
            {
                client_id = FIXED_CLIENT_ID,
                client_name = FIXED_CLIENT_NAME,
                redirect_uris = new[] { "http://localhost:1179/callback" },
                grant_types = new[] { "authorization_code", "refresh_token" },
                response_types = new[] { "code" },
                scope = "openid profile mcp:*",
                token_endpoint_auth_method = "client_secret_post"
            };

            var json = JsonSerializer.Serialize(registrationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:7029/register", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully registered new OAuth client: {Response}", responseContent);
                
                // Parse the response to get client_id and client_secret
                var registrationResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                if (registrationResponse.TryGetProperty("client_id", out var clientId) &&
                    registrationResponse.TryGetProperty("client_secret", out var clientSecret))
                {
                    _logger.LogInformation("Client ID: {ClientId}", clientId.GetString());
                    _logger.LogInformation("Client Secret: {ClientSecret}", clientSecret.GetString());
                    
                    // Save credentials for future use
                    var credentialsToSave = new
                    {
                        client_id = clientId.GetString(),
                        client_secret = clientSecret.GetString(),
                        redirect_uri = "http://localhost:1179/callback",
                        registered_at = DateTime.UtcNow
                    };
                    
                    await File.WriteAllTextAsync(credentialsPath, 
                        JsonSerializer.Serialize(credentialsToSave, new JsonSerializerOptions { WriteIndented = true }));
                    _logger.LogInformation("Saved OAuth client credentials for future use");
                }
            }
            else
            {
                _logger.LogWarning("Failed to register OAuth client: {StatusCode} - {Content}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering default OAuth client");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}