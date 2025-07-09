using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AiStudio4.Core.Interfaces;

namespace AiStudio4.Services.Mcp
{
    /// <summary>
    /// Custom SSE (Server-Sent Events) transport for MCP server
    /// This allows HTTP-based clients to connect to the MCP server
    /// </summary>
    public class SseServerTransport : IDisposable
    {
        private readonly int _port;
        private readonly ILogger<SseServerTransport>? _logger;
        private readonly IServiceProvider _serviceProvider;
        private HttpListener? _listener;
        private readonly ConcurrentDictionary<string, SseClient> _clients = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private IMcpServer? _mcpServer;

        public SseServerTransport(int port, IServiceProvider serviceProvider, ILogger<SseServerTransport>? logger = null)
        {
            _port = port;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task RunAsync(IMcpServer server, CancellationToken cancellationToken)
        {
            _mcpServer = server;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            _listener = new HttpListener();
            //_listener.Prefixes.Add($"http://localhost:{_port}/");
            //_listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            //_listener.Prefixes.Add($"http://0.0.0.0:{_port}/");
            _listener.Prefixes.Add($"http://*:{_port}/");
            try
            {
                _listener.Start();
                _logger?.LogInformation($"SSE server started on http://localhost:{_port}/");
            }
            catch (HttpListenerException ex)
            {
                _logger?.LogError(ex, "Failed to start HTTP listener on port {Port}", _port);
                throw new InvalidOperationException($"Failed to start HTTP listener on port {_port}. Make sure the port is not already in use and you have sufficient permissions.", ex);
            }

            var listenerTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
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
                await listenerTask;
            }
            finally
            {
                _listener?.Stop();
                _listener?.Close();
                
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
                _logger?.LogDebug("Handling request to {Path}", context.Request.Url?.AbsolutePath);
                
                // Add CORS headers
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

                if (context.Request.HttpMethod == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                    return;
                }

                switch (context.Request.Url?.AbsolutePath)
                {
                    case "/":
                        await HandleRootRequest(context);
                        break;
                    case "/sse":
                        await HandleSseConnection(context, cancellationToken);
                        break;
                    case "/jsonrpc":
                        await HandleJsonRpcRequest(context);
                        break;
                    default:
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
                catch
                {
                    // Ignore errors when trying to send error response
                }
            }
        }

        private async Task HandleRootRequest(HttpListenerContext context)
        {
            context.Response.ContentType = "text/html";
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>AiStudio4 MCP Server</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .endpoint {{ background: #f5f5f5; padding: 10px; margin: 10px 0; border-radius: 5px; }}
        code {{ background: #eee; padding: 2px 5px; border-radius: 3px; }}
    </style>
</head>
<body>
    <h1>AiStudio4 MCP Server</h1>
    <p>Server is running on port {_port}</p>
    
    <h2>Available Endpoints:</h2>
    <div class='endpoint'>
        <strong>GET /sse</strong> - Server-Sent Events stream for real-time communication
    </div>
    <div class='endpoint'>
        <strong>POST /jsonrpc</strong> - JSON-RPC endpoint for MCP protocol
    </div>
    
    <h2>Example Usage:</h2>
    <p>Connect to SSE stream:</p>
    <code>curl http://localhost:{_port}/sse</code>
    <code>claude mcp add --transport sse AiStudio4 http://localhost:3000</code>
    <p>Send JSON-RPC request:</p>
    <code>curl -X POST http://localhost:{_port}/jsonrpc -H ""Content-Type: application/json"" -d '{{""jsonrpc"":""2.0"",""method"":""tools/list"",""id"":1}}'</code>
    
    <p>Tools available: {(_mcpServer?.ServerOptions?.Capabilities?.Tools != null ? "Yes" : "No")}</p>
</body>
</html>";
            var bytes = Encoding.UTF8.GetBytes(html);
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }

        private async Task HandleSseConnection(HttpListenerContext context, CancellationToken cancellationToken)
        {
            // Set up SSE headers
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.Add("Cache-Control", "no-cache");
            context.Response.Headers.Add("Connection", "keep-alive");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            var clientId = Guid.NewGuid().ToString();
            var client = new SseClient(context.Response, clientId, _logger);
            
            _clients.TryAdd(clientId, client);
            _logger?.LogInformation("SSE client connected: {ClientId}", clientId);

            try
            {
                // Send initial connection event
                await client.SendEventAsync("connected", System.Text.Json.JsonSerializer.Serialize(new 
                { 
                    status = "connected",
                    clientId = clientId,
                    serverName = "AiStudio4-MCP-Server",
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
                _logger?.LogError(ex, "Error in SSE client connection for {ClientId}", clientId);
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                client.Dispose();
                _logger?.LogInformation("SSE client disconnected: {ClientId}", clientId);
            }
        }

        private async Task HandleJsonRpcRequest(HttpListenerContext context)
        {
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
            
            _logger?.LogDebug("Received JSON-RPC request: {Request}", requestBody);

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
                _logger?.LogError(ex, "Error processing JSON-RPC request");
                
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
                                name = "AiStudio4-MCP-Server",
                                version = "1.0.0"
                            }
                        }
                    };

                case "tools/list":
                    if (capabilities.Tools?.ListToolsHandler != null)
                    {
                        // Directly call the adapter since we can't access internal Request types
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
                        // Extract tool call parameters
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
                            
                            // Execute tool directly through adapter
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
                availableEndpoints = new[] { "/", "/sse", "/jsonrpc" }
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
                _listener?.Stop();
            }
            catch (Exception e)
            {
            }
            _listener?.Close();
            
            foreach (var client in _clients.Values)
            {
                client.Dispose();
            }
            _clients.Clear();
            
            _cancellationTokenSource?.Dispose();
        }

        private IToolToMcpAdapter GetToolAdapter()
        {
            // Try to get existing adapter from service provider
            var existingAdapter = _serviceProvider.GetService<IToolToMcpAdapter>();
            if (existingAdapter != null)
            {
                return existingAdapter;
            }
            
            // Create a new adapter instance
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