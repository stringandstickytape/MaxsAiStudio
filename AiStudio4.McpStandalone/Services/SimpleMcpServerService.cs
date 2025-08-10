using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Server;
using AiStudio4.McpStandalone.McpServer;

namespace AiStudio4.McpStandalone.Services
{
    public interface ISimpleMcpServerService
    {
        Task<bool> StartServerAsync();
        Task StopServerAsync();
        bool IsServerRunning { get; }
        string ServerUrl { get; }
        string OAuthServerUrl { get; }
    }

    public class SimpleMcpServerService : ISimpleMcpServerService
    {
        private readonly ILogger<SimpleMcpServerService> _logger;
        private WebApplication? _app;
        private CancellationTokenSource _cancellationTokenSource;
        private Task? _runningTask;

        public string ServerUrl { get; } = "http://localhost:7071/";
        public string OAuthServerUrl { get; } = "http://localhost:7029";
        public bool IsServerRunning => _app != null && _runningTask != null && !_runningTask.IsCompleted;

        public SimpleMcpServerService(ILogger<SimpleMcpServerService> logger)
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

                // Create a new cancellation token source if the current one is cancelled
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = new CancellationTokenSource();
                    _logger.LogInformation("Created new cancellation token source for MCP server restart");
                }

                _logger.LogInformation("Starting MCP server with OAuth authorization at {ServerUrl}", ServerUrl);

                var builder = WebApplication.CreateBuilder();
                
                // Configure Kestrel to listen on the specific port
                builder.WebHost.UseUrls(ServerUrl);
                
                // Add authentication services
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Configure to validate tokens from our OAuth server
                    options.Authority = OAuthServerUrl;
                    options.RequireHttpsMetadata = false; // Allow HTTP for development
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false, // Set to false for testing
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = OAuthServerUrl,
                        NameClaimType = "name",
                        RoleClaimType = "roles"
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            _logger.LogError("Authentication failed: {Error}", context.Exception?.Message);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            _logger.LogInformation("Token validated successfully for {Principal}", 
                                context.Principal?.Identity?.Name);
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            _logger.LogInformation("Authentication challenge issued");
                            return Task.CompletedTask;
                        }
                    };
                });

                builder.Services.AddAuthorization();

                // Add MCP server with our hello world tool
                builder.Services.AddMcpServer()
                    .WithTools<HelloWorldTool>()
                    .WithHttpTransport();

                // Add logging
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();
                builder.Logging.AddDebug();

                _app = builder.Build();

                // Configure the HTTP request pipeline
                _app.UseAuthentication();
                _app.UseAuthorization();
                
                // Map MCP endpoints
                _app.MapMcp("/mcp")
                    .RequireAuthorization(); // Require OAuth authentication

                // Add a health check endpoint (no auth required)
                _app.MapGet("/health", () => "MCP Server is running")
                    .AllowAnonymous();

                // Add metadata endpoint for OAuth discovery
                _app.MapGet("/.well-known/oauth-authorization-server", () => new
                {
                    issuer = OAuthServerUrl,
                    authorization_endpoint = $"{OAuthServerUrl}/authorize",
                    token_endpoint = $"{OAuthServerUrl}/token",
                    jwks_uri = $"{OAuthServerUrl}/.well-known/jwks.json",
                    response_types_supported = new[] { "code", "token" },
                    grant_types_supported = new[] { "authorization_code", "refresh_token" },
                    token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post" }
                }).AllowAnonymous();

                // Start the server in a background task
                _runningTask = Task.Run(async () =>
                {
                    try
                    {
                        await _app.RunAsync(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("MCP server was cancelled");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error running MCP server");
                    }
                }, _cancellationTokenSource.Token);

                // Wait a moment for the server to start
                await Task.Delay(1000);

                _logger.LogInformation("MCP server started successfully at {ServerUrl}", ServerUrl);
                _logger.LogInformation("OAuth server expected at {OAuthServerUrl}", OAuthServerUrl);
                _logger.LogInformation("Health check available at {ServerUrl}health", ServerUrl);
                _logger.LogInformation("MCP endpoint available at {ServerUrl}mcp (requires OAuth)", ServerUrl);

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
                if (!IsServerRunning)
                {
                    _logger.LogInformation("MCP server is not running");
                    return;
                }

                _logger.LogInformation("Stopping MCP server...");

                // Request cancellation
                _cancellationTokenSource.Cancel();

                // Stop the application
                if (_app != null)
                {
                    await _app.StopAsync();
                    await _app.DisposeAsync();
                    _app = null;
                }

                // Wait for the running task to complete
                if (_runningTask != null)
                {
                    try
                    {
                        await _runningTask.WaitAsync(TimeSpan.FromSeconds(5));
                    }
                    catch (TimeoutException)
                    {
                        _logger.LogWarning("MCP server did not stop within timeout");
                    }
                    _runningTask = null;
                }

                _logger.LogInformation("MCP server stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping MCP server");
            }
        }
    }
}