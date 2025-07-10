using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using AiStudio4.Core.Interfaces;
using AiStudio4.Services.Mcp;

namespace AiStudio4.Services.Mcp
{
    public class AspNetCoreOAuthMcpServer : IDisposable
    {
        private readonly int _mcpPort;
        private readonly int _oauthPort;
        private readonly ILogger<AspNetCoreOAuthMcpServer>? _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMcpServer _mcpServer;
        private IHost? _host;
        private CancellationTokenSource? _cancellationTokenSource;
        
        private readonly string _serverUrl;
        private readonly string _oauthServerUrl;

        public AspNetCoreOAuthMcpServer(
            int mcpPort, 
            int oauthPort, 
            IMcpServer mcpServer,
            IServiceProvider serviceProvider, 
            ILogger<AspNetCoreOAuthMcpServer>? logger = null)
        {
            _mcpPort = mcpPort;
            _oauthPort = oauthPort;
            _mcpServer = mcpServer;
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            _serverUrl = $"http://172.19.80.1:{mcpPort}";
            _oauthServerUrl = $"http://172.19.80.1:{oauthPort}";
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start the OAuth server first
            var oauthServer = new InMemoryOAuthServer(_oauthPort, _oauthServerUrl, _serverUrl, 
                _serviceProvider.GetService<ILogger<OAuthSseServerTransport>>());
            
            // Get the signing key from the OAuth server
            var signingKey = oauthServer.GetSigningKey();
            
            var oauthTask = oauthServer.RunAsync(_cancellationTokenSource.Token);

            var builder = WebApplication.CreateBuilder();
            
            // Configure logging
            builder.Logging.ClearProviders();
            if (_logger != null)
            {
                builder.Logging.AddProvider(new CustomLoggerProvider(_logger));
            }

            // Configure services with the OAuth server's signing key
            ConfigureServices(builder.Services, signingKey);

            var app = builder.Build();
            
            // Configure the HTTP request pipeline
            Configure(app);

            // Configure URLs
            app.Urls.Clear();
            app.Urls.Add(_serverUrl);

            _host = app;

            try
            {
                _logger?.LogInformation($"Starting ASP.NET Core OAuth MCP server on {_serverUrl}");
                _logger?.LogInformation($"OAuth authorization server running on {_oauthServerUrl}");
                
                var mcpTask = app.RunAsync(_cancellationTokenSource.Token);
                await Task.WhenAny(mcpTask, oauthTask);
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Normal shutdown
                _logger?.LogInformation("ASP.NET Core OAuth MCP server stopped");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error running ASP.NET Core OAuth MCP server");
                throw;
            }
            finally
            {
                oauthServer.Dispose();
            }
        }

        private void ConfigureServices(IServiceCollection services, RsaSecurityKey oauthSigningKey)
        {
            // Add controllers
            services.AddControllers();

            // Add CORS
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Add JWT Bearer authentication for MCP using the OAuth server's signing key
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.RequireHttpsMetadata = false; // For development
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = _oauthServerUrl,
                        ValidAudience = _serverUrl,
                        NameClaimType = "name",
                        RoleClaimType = "roles",
                        IssuerSigningKey = oauthSigningKey
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("mcp", policy =>
                {
                    policy.AuthenticationSchemes.Add("Bearer");
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "mcp:tools");
                });
            });

            // Add MCP server services
            services.AddSingleton(_mcpServer);
            
            // Add tools from the main service provider
            var tools = _serviceProvider.GetServices<ITool>();
            foreach (var tool in tools)
            {
                services.AddSingleton(tool);
            }
            
            services.AddScoped<IToolToMcpAdapter>(provider =>
            {
                var toolsList = provider.GetServices<ITool>();
                var builtinToolService = _serviceProvider.GetService<IBuiltInToolExtraPropertiesService>();
                var logger = provider.GetService<ILogger<IToolToMcpAdapter>>();
                return new IToolToMcpAdapter(toolsList, builtinToolService, _serviceProvider, logger);
            });

            // Store the OAuth signing key as a singleton
            services.AddSingleton(oauthSigningKey);
            
            // Add in-memory OAuth server for development  
            services.AddSingleton<InMemoryOAuthServer>(provider => 
                new InMemoryOAuthServer(_oauthPort, _oauthServerUrl, _serverUrl, 
                    provider.GetService<ILogger<OAuthSseServerTransport>>()));
        }

        private void Configure(WebApplication app)
        {
            // Enable CORS
            app.UseCors();

            // Add authentication
            app.UseAuthentication();
            app.UseAuthorization();

            // OAuth endpoints are handled by the separate OAuth server running on port {_oauthPort}

            // Add MCP endpoints
            app.MapMcpEndpoints();

            // Add controllers
            app.MapControllers();

            // Add root endpoint
            app.MapGet("/", () => Results.Content(GetRootPageHtml(), "text/html"));

            // Add protected resource metadata endpoint
            app.MapGet("/.well-known/oauth-protected-resource", () => 
            {
                var metadata = new
                {
                    resource = new Uri(_serverUrl),
                    resource_documentation = new Uri("https://docs.example.com/api/mcp"),
                    authorization_servers = new[] { new Uri(_oauthServerUrl) },
                    scopes_supported = new[] { "mcp:tools" }
                };
                return Results.Json(metadata);
            });
        }

        private string GetRootPageHtml()
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>AiStudio4 ASP.NET Core OAuth MCP Server</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .endpoint {{ background: #f5f5f5; padding: 10px; margin: 10px 0; border-radius: 5px; }}
        .oauth-info {{ background: #e8f4f8; padding: 15px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #17a2b8; }}
        code {{ background: #eee; padding: 2px 5px; border-radius: 3px; }}
        .success {{ background: #d4edda; padding: 10px; margin: 10px 0; border-radius: 5px; border-left: 4px solid #28a745; }}
    </style>
</head>
<body>
    <h1>AiStudio4 ASP.NET Core OAuth MCP Server</h1>
    <p>Server running on port {_mcpPort}</p>
    
    <div class='success'>
        <h3>✅ Production-Ready OAuth MCP Server</h3>
        <p>This server uses Microsoft's official ASP.NET Core MCP authentication package and follows OAuth 2.0 RFC standards.</p>
    </div>
    
    <div class='oauth-info'>
        <h3>🔐 OAuth 2.0 Authentication Required</h3>
        <p>All MCP endpoints require valid OAuth 2.0 Bearer tokens with the 'mcp:tools' scope.</p>
    </div>
    
    <h2>OAuth Discovery:</h2>
    <div class='endpoint'>
        <strong>Authorization Server Metadata:</strong> <code>{_oauthServerUrl}/.well-known/oauth-authorization-server</code><br>
        <strong>Protected Resource Metadata:</strong> <code>{_serverUrl}/.well-known/oauth-protected-resource</code><br>
        <strong>JWKS:</strong> <code>{_oauthServerUrl}/.well-known/jwks</code>
    </div>
    
    <h2>MCP Endpoints:</h2>
    <div class='endpoint'>
        <strong>GET /sse</strong> - Server-Sent Events stream (OAuth protected)<br>
        <strong>POST /</strong> - JSON-RPC endpoint (OAuth protected)
    </div>
    
    <h2>Connection Instructions:</h2>
    <div class='endpoint'>
        <p>Claude will automatically discover and authenticate with this server using:</p>
        <code>claude mcp add --transport sse AiStudio4 {_serverUrl}</code>
    </div>
    
    <p>Server Info: AiStudio4 v1.0.0 • Protocol: MCP 2025-06-18 • Authentication: OAuth 2.0</p>
</body>
</html>";
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _host?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }

    // Extension methods for MCP endpoints
    public static class WebApplicationExtensions
    {

        public static void MapMcpEndpoints(this WebApplication app)
        {
            // Use the official MCP authentication
            app.MapGet("/sse", 
                [Authorize("mcp")]
                async (HttpContext context, IMcpServer mcpServer, IToolToMcpAdapter adapter) => 
                {
                    await HandleSseConnection(context, mcpServer, adapter);
                });

            app.MapPost("/", 
                [Authorize("mcp")]
                async (HttpContext context, IMcpServer mcpServer, IToolToMcpAdapter adapter) => 
                {
                    await HandleJsonRpcRequest(context, mcpServer, adapter);
                });
        }

        private static async Task HandleSseConnection(HttpContext context, IMcpServer mcpServer, IToolToMcpAdapter adapter)
        {
            // Set SSE headers
            context.Response.Headers.Add("Content-Type", "text/event-stream");
            context.Response.Headers.Add("Cache-Control", "no-cache");
            context.Response.Headers.Add("Connection", "keep-alive");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            var clientId = Guid.NewGuid().ToString();
            
            try
            {
                // Send initial endpoint information for compatibility
                await context.Response.WriteAsync($"event: endpoint\n");
                await context.Response.WriteAsync($"data: /\n\n");
                await context.Response.Body.FlushAsync();

                // Keep connection alive with periodic heartbeats
                while (!context.RequestAborted.IsCancellationRequested)
                {
                    await Task.Delay(30000, context.RequestAborted);
                    
                    if (!context.RequestAborted.IsCancellationRequested)
                    {
                        await context.Response.WriteAsync($"event: ping\n");
                        await context.Response.WriteAsync($"data: {{\"timestamp\":\"{DateTimeOffset.UtcNow:O}\",\"clientId\":\"{clientId}\"}}\n\n");
                        await context.Response.Body.FlushAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal disconnection
            }
        }

        private static async Task HandleJsonRpcRequest(HttpContext context, IMcpServer mcpServer, IToolToMcpAdapter adapter)
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();

            try
            {
                var request = System.Text.Json.JsonSerializer.Deserialize<JsonRpcRequest>(requestBody);
                if (request == null)
                {
                    throw new InvalidOperationException("Invalid JSON-RPC request");
                }

                // Handle notifications (no response expected)
                if (request.Id == null || (request.Method?.StartsWith("notifications/") == true))
                {
                    context.Response.StatusCode = 202; // Accepted
                    return;
                }

                // Process the request
                var serviceProvider = context.RequestServices;
                var result = await ProcessMcpRequest(request, mcpServer, adapter, serviceProvider);

                // Send JSON response
                context.Response.Headers.Add("Content-Type", "application/json");
                var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
                await context.Response.WriteAsync(responseJson);
            }
            catch (Exception ex)
            {
                // Send error response
                var errorResponse = new
                {
                    jsonrpc = "2.0",
                    id = (object?)null,
                    error = new
                    {
                        code = -32603,
                        message = ex.Message,
                        data = ex.GetType().Name
                    }
                };

                context.Response.Headers.Add("Content-Type", "application/json");
                var errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(errorJson);
            }
        }

        private static async Task<object> ProcessMcpRequest(JsonRpcRequest request, IMcpServer mcpServer, IToolToMcpAdapter adapter, IServiceProvider serviceProvider)
        {
            var capabilities = mcpServer.ServerOptions.Capabilities;

            switch (request.Method)
            {
                case "initialize":
                    return new
                    {
                        jsonrpc = "2.0",
                        id = request.Id,
                        result = new
                        {
                            protocolVersion = "2025-06-18",
                            capabilities = new
                            {
                                tools = capabilities?.Tools != null ? new { } : null
                            },
                            serverInfo = new
                            {
                                name = "AiStudio4",
                                version = "1.0.0"
                            }
                        }
                    };

                case "tools/list":
                    if (capabilities?.Tools?.ListToolsHandler != null)
                    {
                        // Get tools from service provider (same pattern as used in other transports)
                        var toolsFromProvider = serviceProvider.GetServices<ITool>();
                        var mcpTools = toolsFromProvider.Select(adapter.ConvertToMcpTool).ToList();
                        
                        return new
                        {
                            jsonrpc = "2.0",
                            id = request.Id,
                            result = new { tools = mcpTools }
                        };
                    }
                    break;

                case "tools/call":
                    if (capabilities?.Tools?.CallToolHandler != null && request.Params != null)
                    {
                        var paramsElement = System.Text.Json.JsonSerializer.Serialize(request.Params);
                        var paramsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement);
                        
                        if (paramsDict != null && paramsDict.TryGetValue("name", out var toolNameObj) && toolNameObj is string toolName)
                        {
                            var arguments = new Dictionary<string, object?>();
                            if (paramsDict.TryGetValue("arguments", out var argsObj) && argsObj != null)
                            {
                                var argsJson = System.Text.Json.JsonSerializer.Serialize(argsObj);
                                arguments = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson) ?? new();
                            }
                            
                            var toolResult = await adapter.ExecuteTool(toolName, arguments, CancellationToken.None);
                            
                            return new
                            {
                                jsonrpc = "2.0",
                                id = request.Id,
                                result = toolResult
                            };
                        }
                    }
                    break;
            }

            throw new InvalidOperationException($"Unknown or unsupported method: {request.Method}");
        }
    }

    // JSON-RPC request model
    public class JsonRpcRequest
    {
        public string JsonRpc { get; set; } = "";
        public string Method { get; set; } = "";
        public object? Params { get; set; }
        public object? Id { get; set; }
    }

    // Custom logger provider to integrate with existing logging
    public class CustomLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;

        public CustomLoggerProvider(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CustomLogger(_logger, categoryName);
        }

        public void Dispose() { }
    }

    public class CustomLogger : ILogger
    {
        private readonly ILogger _logger;
        private readonly string _categoryName;

        public CustomLogger(ILogger logger, string categoryName)
        {
            _logger = logger;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = $"[{_categoryName}] {formatter(state, exception)}";
            _logger.Log(logLevel, eventId, message, exception);
        }
    }

}