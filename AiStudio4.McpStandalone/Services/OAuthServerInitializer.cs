using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AiStudio4.McpStandalone.Services;

/// <summary>
/// Helper class to initialize the OAuth server with default clients
/// </summary>
public class OAuthServerInitializer : IDisposable
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public OAuthServerInitializer(ILogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Registers a default test client with the OAuth server
    /// </summary>
    public async Task RegisterDefaultClientAsync()
    {
        try
        {
            // Wait a moment for the server to be fully ready
            await Task.Delay(2000);

            var registrationRequest = new
            {
                client_name = "MCP Standalone Client",
                redirect_uris = new[] { "http://localhost:1179/callback" },
                grant_types = new[] { "authorization_code", "refresh_token" },
                response_types = new[] { "code" },
                scope = "openid profile mcp:*"
            };

            var json = JsonSerializer.Serialize(registrationRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:7029/register", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully registered OAuth client: {Response}", responseContent);
                
                // Parse the response to get client_id and client_secret
                var registrationResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                if (registrationResponse.TryGetProperty("client_id", out var clientId))
                {
                    _logger.LogInformation("Client ID: {ClientId}", clientId.GetString());
                }
                if (registrationResponse.TryGetProperty("client_secret", out var clientSecret))
                {
                    _logger.LogInformation("Client Secret: {ClientSecret}", clientSecret.GetString());
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