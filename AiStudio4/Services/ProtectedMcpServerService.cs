using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Server;
using System.Reflection;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using AiStudio4.Core.Interfaces;
using AiStudio4.Services.ProtectedMcpServer;
using AiStudio4.InjectedDependencies;
using AiStudio4.Core;
using System.Linq;
using AiStudio4.InjectedDependencies.WebSocket;

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
    private readonly IBuiltinToolService _builtinToolService;
    private readonly IGeneralSettingsService _settingsService;
    private WebApplication? _app;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _runningTask;

    public string ServerUrl { get; } = "http://localhost:7071/";
    public string OAuthServerUrl { get; } = "http://localhost:7029";
    public bool IsServerRunning => _app != null && _runningTask != null && !_runningTask.IsCompleted;

    public ProtectedMcpServerService(ILogger<ProtectedMcpServerService> logger, IBuiltinToolService builtinToolService, IGeneralSettingsService settingsService)
    {
        _logger = logger;
        _builtinToolService = builtinToolService;
        _settingsService = settingsService;
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
                options.RequireHttpsMetadata = false; // Allow HTTP for development
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
                    ResourceDocumentation = new Uri("https://docs.example.com/api/builtin-tools"),
                    AuthorizationServers = { new Uri(OAuthServerUrl) },
                    ScopesSupported = ["mcp:tools"],
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();
            
            // Register the builtin tool service as a singleton in the MCP server
            builder.Services.AddSingleton(_builtinToolService);
            
            // Register all tools using the same registration pattern as the main app
            builder.Services.AddToolServices();
            
            // Register all required services for tools (copy from main app DI)
            
            // Core services
            builder.Services.AddSingleton<IConvStorage, FileSystemConvStorage>();
            builder.Services.AddSingleton<AiStudio4.InjectedDependencies.IGeneralSettingsService, AiStudio4.InjectedDependencies.GeneralSettingsService>();
            builder.Services.AddSingleton<AiStudio4.InjectedDependencies.IAppearanceSettingsService, AiStudio4.InjectedDependencies.AppearanceSettingsService>();
            
            // WebSocket services
            builder.Services.AddSingleton<AiStudio4.InjectedDependencies.WebSocketManagement.WebSocketConnectionManager>();
            builder.Services.AddSingleton<AiStudio4.InjectedDependencies.WebSocket.WebSocketMessageHandler>();
            builder.Services.AddSingleton<AiStudio4.InjectedDependencies.WebSocketServer>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IWebSocketNotificationService, AiStudio4.Services.WebSocketNotificationService>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IStatusMessageService, AiStudio4.Services.StatusMessageService>();
            
            // Chat and conversation services
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IChatService, AiStudio4.Services.DefaultChatService>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IToolService, AiStudio4.Services.ToolService>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.ISystemPromptService, AiStudio4.Services.SystemPromptService>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IProjectService, AiStudio4.Services.ProjectService>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IPinnedCommandService, AiStudio4.Services.PinnedCommandService>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IUserPromptService, AiStudio4.Services.UserPromptService>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IInterjectionService, AiStudio4.Services.InterjectionService>();
            builder.Services.AddSingleton<IProjectFileWatcherService, ProjectFileWatcherService>();
            builder.Services.AddSingleton<IMcpService, McpService>();
            builder.Services.AddSingleton<IDialogService, WpfDialogService>();

            // Tool and processing services
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IToolProcessorService, AiStudio4.Services.ToolProcessorService>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.ISecondaryAiService, AiStudio4.Services.SecondaryAiService>();
            builder.Services.AddSingleton<LicenseService>();
            builder.Services.AddSingleton<Interfaces.IDotNetProjectAnalyzerService, AiStudio4.Services.DotNetProjectAnalyzerService>();
            
            // Conversation and archive services
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IConversationArchivingService, AiStudio4.Services.ConversationArchivingService>();
            builder.Services.AddSingleton<ITipOfTheDayService, TipOfTheDayService>();
            builder.Services.AddSingleton<AiStudio4.Core.Interfaces.IBuiltInToolExtraPropertiesService, BuiltInToolExtraPropertiesService>();
            
            // Processing services
            builder.Services.AddSingleton<ConvService>();
            builder.Services.AddSingleton<AiStudio4.Services.ChatProcessingService>();
            builder.Services.AddSingleton<AiStudio4.InjectedDependencies.ChatManager>();
            builder.Services.AddSingleton<AiStudio4.InjectedDependencies.FileServer>();
            
            // Tool executor and cost strategies
            builder.Services.AddScoped<AiStudio4.Core.Interfaces.IToolExecutor, AiStudio4.Services.ToolExecutor>();
            builder.Services.AddSingleton<AiStudio4.Services.CostingStrategies.NoCachingTokenCostStrategy>();
            builder.Services.AddSingleton<AiStudio4.Services.CostingStrategies.ClaudeCachingTokenCostStrategy>();
            builder.Services.AddSingleton<AiStudio4.Services.CostingStrategies.OpenAICachingTokenCostStrategy>();
            builder.Services.AddSingleton<AiStudio4.Services.CostingStrategies.GeminiCachingTokenCostStrategy>();
            builder.Services.AddSingleton<AiStudio4.Services.CostingStrategies.ITokenCostStrategyFactory, AiStudio4.Services.CostingStrategies.TokenCostStrategyFactory>();
            

            // Find all ITool implementations with MCP attributes in our own assembly
            var allToolTypes = typeof(ITool).Assembly.GetTypes()
                .Where(type => type.IsClass && 
                              !type.IsAbstract &&
                              typeof(ITool).IsAssignableFrom(type) &&
                              type.GetCustomAttribute<McpServerToolTypeAttribute>() != null)
                .ToList();

            // Filter tools based on user settings - only include enabled tools
            var enabledToolTypes = new List<Type>();
            foreach (var toolType in allToolTypes)
            {
                try
                {
                    // Use service locator pattern to create instance with dependencies
                    var toolInstance = builder.Services.BuildServiceProvider().GetService(toolType) as ITool;
                    if (toolInstance != null)
                    {
                        var toolDefinition = toolInstance.GetToolDefinition();
                        if (toolDefinition != null && _settingsService.IsMcpToolEnabled(toolDefinition.Guid))
                        {
                            enabledToolTypes.Add(toolType);
                            _logger.LogInformation("Tool {ToolName} ({ToolGuid}) is enabled and will be registered", toolDefinition.Name, toolDefinition.Guid);
                        }
                        else if (toolDefinition != null)
                        {
                            _logger.LogInformation("Tool {ToolName} ({ToolGuid}) is disabled and will be skipped", toolDefinition.Name, toolDefinition.Guid);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not resolve tool instance for {ToolType}", toolType.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check tool enablement status for {ToolType}", toolType.Name);
                }
            }
            
            var toolTypes = enabledToolTypes;
            _logger.LogInformation("Found {TotalCount} total MCP tools, {EnabledCount} enabled for registration", allToolTypes.Count, toolTypes.Count);
            
            // Build MCP server configuration string and compile dynamically
            var registrationCode = "builder.Services.AddMcpServer()";
            foreach (var toolType in toolTypes)
            {
                registrationCode += $".WithTools<{toolType.FullName}>()";
                _logger.LogInformation("Will register MCP tool: {ToolType}", toolType.Name);
            }
            registrationCode += ".WithHttpTransport();";
            
            _logger.LogInformation("MCP Registration chain: {Chain}", registrationCode);
            
            // Manually register tools based on discovery
            var mcpBuilder = builder.Services.AddMcpServer();
            
            // Register discovered tools dynamically
            foreach (var toolType in toolTypes)
            {
                try
                {
                    // Find the WithTools extension method
                    var withToolsMethod = typeof(Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions)
                        .GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .Where(m => m.Name == "WithTools" && m.IsGenericMethodDefinition)
                        .FirstOrDefault();
                        
                    if (withToolsMethod != null)
                    {
                        var genericMethod = withToolsMethod.MakeGenericMethod(toolType);
                        // Extension method: first param is 'this', second is the optional JsonSerializerOptions (null)
                        mcpBuilder = (IMcpServerBuilder)genericMethod.Invoke(null, new object[] { mcpBuilder, null });
                        _logger.LogInformation("Successfully registered MCP tool: {ToolType}", toolType.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register MCP tool: {ToolType}", toolType.Name);
                }
            }
            
            mcpBuilder.WithHttpTransport();

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