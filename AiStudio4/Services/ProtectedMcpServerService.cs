using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Server;
using System.Net.Http.Headers;
using System.Security.Claims;
using AiStudio4.Core.Interfaces;
using AiStudio4.Services.ProtectedMcpServer;

namespace AiStudio4.Services;

public interface IProtectedMcpServerService
{
    Task<bool> StartServerAsync();
    Task StopServerAsync();
    bool IsServerRunning { get; }
    string ServerUrl { get; }
    string OAuthServerUrl { get; }
}

public class ProtectedMcpServerService : IProtectedMcpServerService
{
    private readonly ILogger<ProtectedMcpServerService> _logger;
    private WebApplication? _app;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _runningTask;

    public string ServerUrl { get; } = "http://localhost:7071/";
    public string OAuthServerUrl { get; } = "https://localhost:7029";
    public bool IsServerRunning => _app != null && _runningTask != null && !_runningTask.IsCompleted;

    public ProtectedMcpServerService(ILogger<ProtectedMcpServerService> logger)
    {
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task<bool> StartServerAsync()
    {
        try
        {
            if (IsServerRunning)
            {
                _logger.LogWarning("MCP server is already running");
                return true;
            }

            _logger.LogInformation("Starting MCP server with authorization at {ServerUrl}", ServerUrl);

            var builder = WebApplication.CreateBuilder();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Configure to validate tokens from our in-memory OAuth server
                options.Authority = OAuthServerUrl;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = ServerUrl, // Validate that the audience matches the resource metadata as suggested in RFC 8707
                    ValidIssuer = OAuthServerUrl,
                    NameClaimType = "name",
                    RoleClaimType = "roles"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var name = context.Principal?.Identity?.Name ?? "unknown";
                        var email = context.Principal?.FindFirstValue("preferred_username") ?? "unknown";
                        _logger.LogInformation("Token validated for: {Name} ({Email})", name, email);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        _logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        _logger.LogInformation("Challenging client to authenticate with Entra ID");
                        return Task.CompletedTask;
                    }
                };
            })
            .AddMcp(options =>
            {
                options.ResourceMetadata = new()
                {
                    Resource = new Uri(ServerUrl),
                    ResourceDocumentation = new Uri("https://docs.example.com/api/weather"),
                    AuthorizationServers = { new Uri(OAuthServerUrl) },
                    ScopesSupported = ["mcp:tools"],
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();
            
            builder.Services.AddMcpServer()
                .WithTools<WeatherTools>()
                .WithHttpTransport();

            // Configure HttpClientFactory for weather.gov API
            builder.Services.AddHttpClient("WeatherApi", client =>
            {
                client.BaseAddress = new Uri("https://api.weather.gov");
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
            });

            // Configure the server URLs
            builder.WebHost.UseUrls(ServerUrl);

            _app = builder.Build();

            _app.UseAuthentication();
            _app.UseAuthorization();

            // Use the default MCP policy name that we've configured
            _app.MapMcp().RequireAuthorization();

            // Start the server on a background task
            _runningTask = Task.Run(async () =>
            {
                try
                {
                    await _app.RunAsync(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("MCP server stopped");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running MCP server");
                }
            });

            // Give the server a moment to start
            await Task.Delay(1000);

            _logger.LogInformation("MCP server started successfully");
            _logger.LogInformation("Using in-memory OAuth server at {OAuthServerUrl}", OAuthServerUrl);
            _logger.LogInformation("Protected Resource Metadata URL: {ServerUrl}.well-known/oauth-protected-resource", ServerUrl);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start MCP server");
            return false;
        }
    }

    public async Task StopServerAsync()
    {
        try
        {
            if (_app != null)
            {
                _cancellationTokenSource.Cancel();
                
                if (_runningTask != null)
                {
                    await _runningTask;
                }

                await _app.DisposeAsync();
                _app = null;
                _runningTask = null;
                
                _logger.LogInformation("MCP server stopped successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MCP server");
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _app?.DisposeAsync();
    }
}