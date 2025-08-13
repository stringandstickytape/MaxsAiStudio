using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Server;
using AiStudio4.McpStandalone.McpServer;
using AiStudio4.Tools.Interfaces;
using AiStudio4.Tools.Services.SmartFileEditor;
using System.Linq;
using System.Reflection;

namespace AiStudio4.McpStandalone.Services
{
    public interface ISimpleMcpServerService
    {
        Task<bool> StartServerAsync();
        Task StopServerAsync();
        Task RestartServerAsync();
        bool IsServerRunning { get; }
        string ServerUrl { get; }
        string OAuthServerUrl { get; }
        void UpdateToolState(string toolId, bool isEnabled);
    }

    public class SimpleMcpServerService : ISimpleMcpServerService
    {
        private readonly ILogger<SimpleMcpServerService> _logger;
        private readonly StandaloneSettingsService _settingsService;
        private readonly IDialogService _dialogService;
        private readonly IStatusMessageService _statusMessageService;
        private WebApplication? _app;
        private CancellationTokenSource _cancellationTokenSource;
        private Task? _runningTask;

        public string ServerUrl => $"http://localhost:{_settingsService.GetMcpServerPort()}/";
        public string OAuthServerUrl => $"http://localhost:{_settingsService.GetOAuthServerPort()}";
        public bool IsServerRunning => _app != null && _runningTask != null && !_runningTask.IsCompleted;

        public SimpleMcpServerService(
            ILogger<SimpleMcpServerService> logger, 
            StandaloneSettingsService settingsService,
            IDialogService dialogService,
            IStatusMessageService statusMessageService)
        {
            _logger = logger;
            _settingsService = settingsService;
            _dialogService = dialogService;
            _statusMessageService = statusMessageService;
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
                
                // Register services needed by tools
                builder.Services.AddSingleton<IDialogService>(_dialogService);
                builder.Services.AddSingleton<IGeneralSettingsService>(_settingsService);
                builder.Services.AddSingleton<IStatusMessageService>(_statusMessageService);
                builder.Services.AddSingleton<ISmartFileEditor, SmartFileEditorService>();
                
                // Add authentication services - matching the original app
                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    // Configure to validate tokens from our OAuth server
                    options.Authority = OAuthServerUrl;
                    options.RequireHttpsMetadata = false; // Allow HTTP for development
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudience = ServerUrl, // Validate that the audience matches the resource metadata
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
                })
                .AddMcp(options =>
                {
                    options.ResourceMetadata = new()
                    {
                        Resource = new Uri(ServerUrl),
                        ResourceDocumentation = new Uri("https://docs.example.com/api/mcp-standalone"),
                        AuthorizationServers = { new Uri(OAuthServerUrl) },
                        ScopesSupported = ["mcp:*", "mcp:tools"],
                    };
                });

                builder.Services.AddAuthorization();

                // Register services for shared tools
                builder.Services.AddSingleton<IGeneralSettingsService, StandaloneSettingsService>();
                builder.Services.AddSingleton<IStatusMessageService, NoOpStatusMessageService>();
                builder.Services.AddSingleton<IBuiltInToolExtraPropertiesService, StandaloneExtraPropertiesService>();

                // Register shared tools dynamically
                RegisterSharedTools(builder.Services);

                // Add MCP server with HTTP transport
                var mcpBuilder = builder.Services.AddMcpServer();
                
                // Only register tools that are enabled
                var enabledTools = _settingsService.GetEnabledTools();
                if (enabledTools.Contains("YouTubeSearchTool"))
                {
                    mcpBuilder = mcpBuilder.WithTools<AiStudio4.Tools.YouTube.YouTubeSearchTool>();
                    _logger.LogInformation("Registered YouTubeSearchTool");
                }
                if (enabledTools.Contains("AzureDevOpsSearchWikiTool"))
                {
                    mcpBuilder = mcpBuilder.WithTools<AiStudio4.Tools.AzureDevOps.AzureDevOpsSearchWikiTool>();
                    _logger.LogInformation("Registered AzureDevOpsSearchWikiTool");
                }
                if (enabledTools.Contains("AzureDevOpsGetWikiPageContentTool"))
                {
                    mcpBuilder = mcpBuilder.WithTools<AiStudio4.Tools.AzureDevOps.AzureDevOpsGetWikiPageContentTool>();
                    _logger.LogInformation("Registered AzureDevOpsGetWikiPageContentTool");
                }
                if (enabledTools.Contains("AzureDevOpsGetWikiPagesTool"))
                {
                    mcpBuilder = mcpBuilder.WithTools<AiStudio4.Tools.AzureDevOps.AzureDevOpsGetWikiPagesTool>();
                    _logger.LogInformation("Registered AzureDevOpsGetWikiPagesTool");
                }
                if (enabledTools.Contains("AzureDevOpsCreateOrUpdateWikiPageTool"))
                {
                    try
                    {
                        mcpBuilder = mcpBuilder.WithTools<AiStudio4.Tools.AzureDevOps.AzureDevOpsCreateOrUpdateWikiPageTool>();
                        _logger.LogInformation("Successfully registered AzureDevOpsCreateOrUpdateWikiPageTool");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to register AzureDevOpsCreateOrUpdateWikiPageTool");
                    }
                }
                if (enabledTools.Contains("AzureDevOpsCreateOrUpdateWikiPageViaLocalTool"))
                {
                    try
                    {
                        mcpBuilder = mcpBuilder.WithTools<AiStudio4.Tools.AzureDevOps.AzureDevOpsCreateOrUpdateWikiPageViaLocalTool>();
                        _logger.LogInformation("Successfully registered AzureDevOpsCreateOrUpdateWikiPageViaLocalTool");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to register AzureDevOpsCreateOrUpdateWikiPageViaLocalTool");
                    }
                }
                
                mcpBuilder.WithHttpTransport();
                
                _logger.LogInformation("MCP server configured with {Count} enabled tools", enabledTools.Count);

                // Add logging
                builder.Logging.ClearProviders();
                builder.Logging.AddConsole();
                builder.Logging.AddDebug();

                _app = builder.Build();

                // Configure the HTTP request pipeline
                _app.UseAuthentication();
                _app.UseAuthorization();
                
                // Map MCP endpoints - both at root and /mcp for compatibility
                _app.MapMcp("/")
                    .RequireAuthorization(); // Require OAuth authentication
                
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

                // Request cancellation first
                _cancellationTokenSource.Cancel();

                // Don't wait for graceful shutdown, just dispose immediately
                if (_app != null)
                {
                    // Fire and forget the stop - don't wait
                    var stopTask = _app.StopAsync();
                    
                    // Give it just 1 second to stop gracefully
                    await Task.WhenAny(stopTask, Task.Delay(1000));
                    
                    // Dispose immediately regardless
                    try
                    {
                        // not a thing:
                        //_app.Dispose();
                    }
                    catch { }
                    
                    _app = null;
                }

                // Don't wait for the background task
                _runningTask = null;

                _logger.LogInformation("MCP server stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping MCP server");
                // Force cleanup
                _app = null;
                _runningTask = null;
            }
        }

        public void UpdateToolState(string toolId, bool isEnabled)
        {
            // Tool state is already persisted by the ViewModel
            // Server needs to be restarted for changes to take effect
            _logger.LogInformation("Tool {ToolId} state changed to {IsEnabled}. Server restart required.", 
                toolId, isEnabled);
        }
        
        public async Task RestartServerAsync()
        {
            _logger.LogInformation("Restarting MCP server to apply tool changes...");
            
            if (IsServerRunning)
            {
                await StopServerAsync();
                await Task.Delay(500); // Small delay to ensure clean shutdown
            }
            
            await StartServerAsync();
        }

        private void RegisterSharedTools(IServiceCollection services)
        {
            // Scan the shared library assembly for tools
            var sharedToolInterfaceType = typeof(ITool);
            var sharedAssembly = sharedToolInterfaceType.Assembly;
            
            var toolTypes = sharedAssembly.GetTypes()
                .Where(t => sharedToolInterfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            
            foreach (var toolType in toolTypes)
            {
                // Register the concrete type
                services.AddTransient(toolType);
                _logger.LogInformation("Registered shared tool type in DI: {ToolType}", toolType.Name);
                
                // Also check if it has the MCP attribute
                if (toolType.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolTypeAttribute>() != null)
                {
                    _logger.LogInformation("Tool {ToolType} has McpServerToolType attribute", toolType.Name);
                }
            }
        }

    }
}