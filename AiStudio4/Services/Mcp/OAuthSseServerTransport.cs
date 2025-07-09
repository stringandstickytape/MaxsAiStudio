using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AiStudio4.Core.Interfaces;
using AiStudio4.Services.Mcp;

namespace AiStudio4.Services.Mcp
{
    public class OAuthSseServerTransport : IDisposable
    {
        private readonly int _mcpPort;
        private readonly int _oauthPort;
        private readonly ILogger<OAuthSseServerTransport>? _logger;
        private readonly IServiceProvider _serviceProvider;
        private HttpListener? _mcpListener;
        private InMemoryOAuthServer? _oauthServer;
        private readonly ConcurrentDictionary<string, SseClient> _clients = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private IMcpServer? _mcpServer;
        
        private readonly string _serverUrl;
        private readonly string _oauthServerUrl;
        private readonly TokenValidationParameters _tokenValidationParameters;
        
        public OAuthSseServerTransport(int mcpPort, int oauthPort, IServiceProvider serviceProvider, ILogger<OAuthSseServerTransport>? logger = null)
        {
            _mcpPort = mcpPort;
            _oauthPort = oauthPort;
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            _serverUrl = $"http://172.19.80.1:{mcpPort}/";
            _oauthServerUrl = $"http://172.19.80.1:{oauthPort}";
            
            // Create OAuth server
            _oauthServer = new InMemoryOAuthServer(oauthPort, _oauthServerUrl, _serverUrl, logger);
            
            // Configure token validation
            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = _serverUrl,
                ValidIssuer = _oauthServerUrl,
                NameClaimType = "name",
                RoleClaimType = "roles",
                IssuerSigningKeyResolver = (token, securityToken, keyIdentifier, parameters) =>
                {
                    // For simplicity, return the OAuth server's key
                    return new[] { _oauthServer.GetSigningKey() };
                }
            };
        }
        
        public async Task RunAsync(IMcpServer server, CancellationToken cancellationToken)
        {
            _mcpServer = server;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Start OAuth server
            var oauthTask = _oauthServer!.RunAsync(_cancellationTokenSource.Token);
            
            // Start MCP server
            _mcpListener = new HttpListener();
            _mcpListener.Prefixes.Add($"http://*:{_mcpPort}/");
            
            try
            {
                _mcpListener.Start();
                _logger?.LogInformation($"OAuth-enabled MCP server started on {_serverUrl}");
                _logger?.LogInformation($"OAuth server running on {_oauthServerUrl}");
                _logger?.LogInformation($"Protected Resource Metadata: {_serverUrl}.well-known/oauth-protected-resource");
            }
            catch (HttpListenerException ex)
            {
                _logger?.LogError(ex, "Failed to start HTTP listener on port {Port}", _mcpPort);
                throw new InvalidOperationException($"Failed to start HTTP listener on port {_mcpPort}. Make sure the port is not already in use and you have sufficient permissions.", ex);
            }
            
            var listenerTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _mcpListener.GetContextAsync();
                        _ = Task.Run(() => HandleRequest(context, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                    }
                    catch (HttpListenerException) when (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error accepting connection");
                    }
                }
            }, _cancellationTokenSource.Token);
            
            try
            {
                await Task.WhenAny(listenerTask, oauthTask);
            }
            finally
            {
                _mcpListener?.Stop();
                _mcpListener?.Close();
                
                // Close all client connections
                foreach (var client in _clients.Values)
                {
                    client.Dispose();
                }
                _clients.Clear();
            }
        }
        
        private async Task HandleRequest(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                var path = context.Request.Url?.AbsolutePath;
                var method = context.Request.HttpMethod;
                var query = context.Request.QueryString.ToString();
                
                _logger?.LogInformation($"MCP Server: {method} {path} {query}");
                
                // Add CORS headers
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                
                if (context.Request.HttpMethod == "OPTIONS")
                {
                    _logger?.LogDebug("MCP Server: Handling OPTIONS request");
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                    return;
                }
                
                switch (path)
                {
                    case "/":
                        _logger?.LogInformation("MCP Server: Serving root page");
                        await HandleRootRequest(context);
                        break;
                    case "/.well-known/oauth-protected-resource":
                        _logger?.LogInformation("MCP Server: Serving OAuth protected resource metadata");
                        await HandleProtectedResourceMetadata(context);
                        break;
                    case "/sse":
                        _logger?.LogInformation("MCP Server: Handling SSE connection");
                        await HandleSseConnection(context, cancellationToken);
                        break;
                    case "/jsonrpc":
                        _logger?.LogInformation("MCP Server: Handling JSON-RPC request");
                        await HandleJsonRpcRequest(context);
                        break;
                    default:
                        _logger?.LogWarning($"MCP Server: Unknown path requested: {path}");
                        await Handle404Request(context);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling request");
                try
                {
                    if (!context.Response.OutputStream.CanWrite) return;
                    
                    context.Response.StatusCode = 500;
                    var error = Encoding.UTF8.GetBytes($"Internal Server Error: {ex.Message}");
                    await context.Response.OutputStream.WriteAsync(error, 0, error.Length);
                    context.Response.Close();
                }
                catch { }
            }
        }
        
        private async Task HandleRootRequest(HttpListenerContext context)
        {
            context.Response.ContentType = "text/html";
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>AiStudio4 OAuth-Enabled MCP Server</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .endpoint {{ background: #f5f5f5; padding: 10px; margin: 10px 0; border-radius: 5px; }}
        .oauth-info {{ background: #e8f4f8; padding: 15px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #17a2b8; }}
        code {{ background: #eee; padding: 2px 5px; border-radius: 3px; }}
        .warning {{ background: #fff3cd; padding: 10px; margin: 10px 0; border-radius: 5px; border-left: 4px solid #ffc107; }}
    </style>
</head>
<body>
    <h1>AiStudio4 OAuth-Enabled MCP Server</h1>
    <p>MCP Server running on port {_mcpPort}</p>
    <p>OAuth Server running on port {_oauthPort}</p>
    
    <div class='oauth-info'>
        <h3>🔐 OAuth Protection Active</h3>
        <p>This MCP server requires OAuth authentication. All requests to protected endpoints must include a valid Bearer token.</p>
    </div>
    
    <h2>OAuth Endpoints:</h2>
    <div class='endpoint'>
        <strong>OAuth Server:</strong> <code>{_oauthServerUrl}</code><br>
        <strong>Token Endpoint:</strong> <code>{_oauthServerUrl}/token</code><br>
        <strong>JWKS:</strong> <code>{_oauthServerUrl}/.well-known/jwks</code><br>
        <strong>OpenID Config:</strong> <code>{_oauthServerUrl}/.well-known/openid_configuration</code>
    </div>
    
    <h2>MCP Endpoints:</h2>
    <div class='endpoint'>
        <strong>GET /sse</strong> - Server-Sent Events stream (requires authentication)
    </div>
    <div class='endpoint'>
        <strong>POST /jsonrpc</strong> - JSON-RPC endpoint for MCP protocol (requires authentication)
    </div>
    <div class='endpoint'>
        <strong>GET /.well-known/oauth-protected-resource</strong> - Protected resource metadata
    </div>
    
    <h2>Getting a Token:</h2>
    <div class='endpoint'>
        <code>curl -X POST {_oauthServerUrl}/token \<br>
        &nbsp;&nbsp;-H ""Content-Type: application/x-www-form-urlencoded"" \<br>
        &nbsp;&nbsp;-d ""grant_type=client_credentials&client_id=mcp-client&client_secret=mcp-secret&scope=mcp:tools""</code>
    </div>
    
    <h2>Using the Token:</h2>
    <div class='endpoint'>
        <code>curl -H ""Authorization: Bearer YOUR_TOKEN"" http://localhost:{_mcpPort}/jsonrpc \<br>
        &nbsp;&nbsp;-H ""Content-Type: application/json"" \<br>
        &nbsp;&nbsp;-d '{{""jsonrpc"":""2.0"",""method"":""tools/list"",""id"":1}}'</code>
    </div>
    
    <div class='warning'>
        <strong>Note:</strong> This is a development OAuth server. In production, use a proper OAuth provider like Azure AD, Auth0, or similar.
    </div>
    
    <p>Tools available: {(_mcpServer?.ServerOptions?.Capabilities?.Tools != null ? "Yes" : "No")}</p>
</body>
</html>";
            var bytes = Encoding.UTF8.GetBytes(html);
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
        
        private async Task HandleProtectedResourceMetadata(HttpListenerContext context)
        {
            var metadata = new
            {
                resource = new Uri(_serverUrl),
                resource_documentation = new Uri("https://docs.example.com/api/mcp"),
                authorization_servers = new[] { new Uri(_oauthServerUrl) },
                scopes_supported = new[] { "mcp:tools" }
            };
            
            context.Response.ContentType = "application/json";
            var json = System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            
            _logger?.LogInformation($"MCP Server: Sending protected resource metadata: {json}");
            
            var bytes = Encoding.UTF8.GetBytes(json);
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
        
        private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                
                var name = principal?.Identity?.Name ?? "unknown";
                var email = principal?.FindFirst("preferred_username")?.Value ?? "unknown";
                _logger?.LogDebug($"Token validated for: {name} ({email})");
                
                return principal;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Token validation failed");
                return null;
            }
        }
        
        private async Task<bool> AuthorizeRequest(HttpListenerContext context)
        {
            var authHeader = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                await SendUnauthorizedResponse(context, "Missing or invalid authorization header");
                return false;
            }
            
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var principal = await ValidateTokenAsync(token);
            
            if (principal == null)
            {
                await SendUnauthorizedResponse(context, "Invalid or expired token");
                return false;
            }
            
            // Check if token has required scope
            var scopes = principal.FindFirst("scope")?.Value ?? "";
            if (!scopes.Contains("mcp:tools"))
            {
                await SendForbiddenResponse(context, "Insufficient scope");
                return false;
            }
            
            return true;
        }
        
        private async Task SendUnauthorizedResponse(HttpListenerContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Add("WWW-Authenticate", $"Bearer realm=\"{_serverUrl}\", error=\"invalid_token\", error_description=\"{message}\"");
            await SendJsonError(context, "unauthorized", message);
        }
        
        private async Task SendForbiddenResponse(HttpListenerContext context, string message)
        {
            context.Response.StatusCode = 403;
            await SendJsonError(context, "forbidden", message);
        }
        
        private async Task SendJsonError(HttpListenerContext context, string error, string description)
        {
            context.Response.ContentType = "application/json";
            var errorObj = new { error = error, error_description = description };
            var json = System.Text.Json.JsonSerializer.Serialize(errorObj);
            var bytes = Encoding.UTF8.GetBytes(json);
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
        
        private async Task HandleSseConnection(HttpListenerContext context, CancellationToken cancellationToken)
        {
            // Authorize request
            if (!await AuthorizeRequest(context))
                return;
            
            // Set up SSE headers
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.Add("Cache-Control", "no-cache");
            context.Response.Headers.Add("Connection", "keep-alive");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            
            var clientId = Guid.NewGuid().ToString();
            var client = new SseClient(context.Response, clientId, _logger);
            
            _clients.TryAdd(clientId, client);
            _logger?.LogInformation("Authenticated SSE client connected: {ClientId}", clientId);
            
            try
            {
                // Send initial connection event
                await client.SendEventAsync("connected", System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = "connected",
                    clientId = clientId,
                    serverName = "AiStudio4-OAuth-MCP-Server",
                    timestamp = DateTimeOffset.UtcNow
                }));
                
                // Keep connection alive with periodic pings
                while (!cancellationToken.IsCancellationRequested && client.IsConnected)
                {
                    await Task.Delay(30000, cancellationToken); // 30 second heartbeat
                    
                    if (client.IsConnected)
                    {
                        await client.SendEventAsync("ping", System.Text.Json.JsonSerializer.Serialize(new
                        {
                            timestamp = DateTimeOffset.UtcNow,
                            clientId = clientId
                        }));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in authenticated SSE client connection for {ClientId}", clientId);
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                client.Dispose();
                _logger?.LogInformation("Authenticated SSE client disconnected: {ClientId}", clientId);
            }
        }
        
        private async Task HandleJsonRpcRequest(HttpListenerContext context)
        {
            // Authorize request
            if (!await AuthorizeRequest(context))
                return;
            
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                var methodNotAllowed = Encoding.UTF8.GetBytes("Method Not Allowed");
                await context.Response.OutputStream.WriteAsync(methodNotAllowed, 0, methodNotAllowed.Length);
                context.Response.Close();
                return;
            }
            
            using var reader = new StreamReader(context.Request.InputStream);
            var requestBody = await reader.ReadToEndAsync();
            
            _logger?.LogDebug("Received authenticated JSON-RPC request: {Request}", requestBody);
            
            try
            {
                // Parse the JSON-RPC request
                var request = System.Text.Json.JsonSerializer.Deserialize<JsonRpcRequest>(requestBody);
                if (request == null)
                {
                    throw new InvalidOperationException("Invalid JSON-RPC request");
                }
                
                // Process the request through the MCP server
                var result = await ProcessMcpRequest(request);
                
                // Send response
                context.Response.ContentType = "application/json";
                var responseJson = System.Text.Json.JsonSerializer.Serialize(result);
                _logger?.LogDebug("Sending JSON-RPC response: {Response}", responseJson);
                
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing authenticated JSON-RPC request");
                
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
                
                context.Response.ContentType = "application/json";
                var errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                var errorBytes = Encoding.UTF8.GetBytes(errorJson);
                await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
            }
            finally
            {
                context.Response.Close();
            }
        }
        
        private async Task<object> ProcessMcpRequest(JsonRpcRequest request)
        {
            if (_mcpServer?.ServerOptions?.Capabilities == null)
            {
                throw new InvalidOperationException("MCP server capabilities not available");
            }
            
            var capabilities = _mcpServer.ServerOptions.Capabilities;
            
            switch (request.Method)
            {
                case "initialize":
                    return new
                    {
                        jsonrpc = "2.0",
                        id = request.Id,
                        result = new
                        {
                            protocolVersion = "0.1.0",
                            capabilities = new
                            {
                                tools = capabilities.Tools != null ? new { } : null
                            },
                            serverInfo = new
                            {
                                name = "AiStudio4-OAuth-MCP-Server",
                                version = "1.0.0"
                            }
                        }
                    };
                
                case "tools/list":
                    if (capabilities.Tools?.ListToolsHandler != null)
                    {
                        var adapter = GetToolAdapter();
                        var tools = GetAllTools();
                        var mcpTools = tools.Select(adapter.ConvertToMcpTool).ToList();
                        
                        return new
                        {
                            jsonrpc = "2.0",
                            id = request.Id,
                            result = new { tools = mcpTools }
                        };
                    }
                    break;
                
                case "tools/call":
                    if (capabilities.Tools?.CallToolHandler != null && request.Params != null)
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
                            
                            var adapter = GetToolAdapter();
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
        
        private async Task Handle404Request(HttpListenerContext context)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";
            var error = new
            {
                error = "Not Found",
                message = "The requested endpoint was not found",
                availableEndpoints = new[] { "/", "/sse", "/jsonrpc", "/.well-known/oauth-protected-resource" }
            };
            var errorJson = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(error));
            await context.Response.OutputStream.WriteAsync(errorJson, 0, errorJson.Length);
            context.Response.Close();
        }
        
        public IReadOnlyList<string> GetConnectedClients()
        {
            return _clients.Keys.ToList();
        }
        
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            try
            {
                _mcpListener?.Stop();
            }
            catch { }
            _mcpListener?.Close();
            
            foreach (var client in _clients.Values)
            {
                client.Dispose();
            }
            _clients.Clear();
            
            _oauthServer?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
        
        private IToolToMcpAdapter GetToolAdapter()
        {
            var existingAdapter = _serviceProvider.GetService<IToolToMcpAdapter>();
            if (existingAdapter != null)
            {
                return existingAdapter;
            }
            
            var tools = GetAllTools();
            var builtinToolService = _serviceProvider.GetService<IBuiltInToolExtraPropertiesService>();
            var logger = _serviceProvider.GetService<ILogger<IToolToMcpAdapter>>();
            
            return new IToolToMcpAdapter(tools, builtinToolService, _serviceProvider, logger);
        }
        
        private IEnumerable<ITool> GetAllTools()
        {
            return _serviceProvider.GetServices<ITool>();
        }
        
        // Helper classes
        private class JsonRpcRequest
        {
            public string JsonRpc { get; set; } = "";
            public string Method { get; set; } = "";
            public object? Params { get; set; }
            public object? Id { get; set; }
        }
        
        private class SseClient : IDisposable
        {
            private readonly HttpListenerResponse _response;
            private readonly StreamWriter _writer;
            private readonly ILogger? _logger;
            public string ClientId { get; }
            public bool IsConnected { get; private set; } = true;
            
            public SseClient(HttpListenerResponse response, string clientId, ILogger? logger)
            {
                _response = response;
                ClientId = clientId;
                _logger = logger;
                _writer = new StreamWriter(response.OutputStream, Encoding.UTF8)
                {
                    AutoFlush = true
                };
            }
            
            public async Task SendEventAsync(string eventType, string data)
            {
                if (!IsConnected) return;
                
                try
                {
                    await _writer.WriteLineAsync($"event: {eventType}");
                    await _writer.WriteLineAsync($"data: {data}");
                    await _writer.WriteLineAsync();
                    await _writer.FlushAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to send SSE event to client {ClientId}", ClientId);
                    IsConnected = false;
                    throw;
                }
            }
            
            public void Dispose()
            {
                IsConnected = false;
                try
                {
                    _writer?.Dispose();
                    _response?.Close();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error disposing SSE client {ClientId}", ClientId);
                }
            }
        }
    }
}