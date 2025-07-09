using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AiStudio4.Core.Interfaces;
using AiStudio4.Services.Mcp;

namespace AiStudio4.Services
{
    public class AiStudioMcpServerService : IMcpServerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AiStudioMcpServerService> _logger;
        private readonly IBuiltInToolExtraPropertiesService _extraPropertiesService;
        private IMcpServer? _currentServer;
        private CancellationTokenSource? _serverCts;
        private McpServerTransportType? _currentTransportType;
        private readonly List<string> _connectedClients = new();

        public bool IsRunning => _currentServer != null && _serverCts != null && !_serverCts.Token.IsCancellationRequested;
        public McpServerTransportType CurrentTransportType => _currentTransportType ?? McpServerTransportType.Stdio;
        public event EventHandler<McpServerStatusChangedEventArgs>? StatusChanged;

        public AiStudioMcpServerService(
            IServiceProvider serviceProvider,
            ILogger<AiStudioMcpServerService> logger,
            IBuiltInToolExtraPropertiesService extraPropertiesService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _extraPropertiesService = extraPropertiesService;
        }

        public async Task<IMcpServer> StartServerAsync(McpServerTransportType transportType, McpServerConfig config)
        {
            try
            {
                // Stop existing server if running
                await StopServerAsync();

                _logger.LogInformation($"Starting MCP server with {transportType} transport");

                // Create transport based on type
                var transport = transportType switch
                {
                    McpServerTransportType.Stdio => new StdioServerTransport("AiStudio4-MCP"),
                    McpServerTransportType.Sse => throw new NotImplementedException("SSE transport is not yet implemented"),
                    _ => throw new NotSupportedException($"Transport type {transportType} is not supported")
                };

                // Configure server with tools
                var options = CreateServerOptions(config);
                _currentServer = McpServerFactory.Create(transport, options);
                _currentTransportType = transportType;

                // Start server
                _serverCts = new CancellationTokenSource();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _currentServer.RunAsync(_serverCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal shutdown
                        _logger.LogInformation("MCP server stopped");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "MCP server error");
                        OnStatusChanged(false, $"Server error: {ex.Message}");
                    }
                }, _serverCts.Token);

                // Give the server a moment to start
                await Task.Delay(500);
                
                OnStatusChanged(true, $"Server started on {transportType}");
                return _currentServer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start MCP server");
                throw;
            }
        }

        public async Task StopServerAsync()
        {
            if (_serverCts != null)
            {
                _logger.LogInformation("Stopping MCP server");
                _serverCts.Cancel();
                _serverCts.Dispose();
                _serverCts = null;
            }

            if (_currentServer != null)
            {
                await _currentServer.DisposeAsync();
                _currentServer = null;
            }

            _connectedClients.Clear();
            _currentTransportType = null;
            OnStatusChanged(false, "Server stopped");
        }

        public IReadOnlyList<string> GetConnectedClients()
        {
            return _connectedClients.AsReadOnly();
        }

        private McpServerOptions CreateServerOptions(McpServerConfig config)
        {
            var tools = _serviceProvider.GetServices<ITool>()
                .Where(t => !config.ExcludedToolGuids.Contains(t.GetToolDefinition().Guid.ToString()))
                .ToList();

            _logger.LogInformation($"Loaded {tools.Count} tools for MCP server");

            var adapter = new IToolToMcpAdapter(tools, _extraPropertiesService, _serviceProvider, 
                _serviceProvider.GetRequiredService<ILogger<IToolToMcpAdapter>>());

            return new McpServerOptions
            {
                ServerInfo = new Implementation
                {
                    Name = "AiStudio4-MCP-Server",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
                },
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability
                    {
                        ListToolsHandler = (request, ct) =>
                        {
                            _logger.LogDebug("Listing tools for MCP client");
                            var mcpTools = tools.Select(t => 
                            {
                                try
                                {
                                    return adapter.ConvertToMcpTool(t);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to convert tool {ToolName}", t.GetToolDefinition().Name);
                                    return null;
                                }
                            })
                            .Where(t => t != null)
                            .Cast<ModelContextProtocol.Protocol.Tool>()
                            .ToList();

                            return ValueTask.FromResult(new ListToolsResult
                            {
                                Tools = mcpTools
                            });
                        },

                        CallToolHandler = async (request, ct) =>
                        {
                            if (request.Params?.Name == null)
                                throw new InvalidOperationException("Tool name is required");

                            _logger.LogInformation($"Executing tool: {request.Params.Name}");

                            var arguments = request.Params.Arguments?
                                .ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);

                            return await adapter.ExecuteTool(
                                request.Params.Name,
                                arguments ?? new Dictionary<string, object?>(),
                                ct);
                        }
                    }
                }
            };
        }

        private void OnStatusChanged(bool isRunning, string message)
        {
            StatusChanged?.Invoke(this, new McpServerStatusChangedEventArgs
            {
                IsRunning = isRunning,
                Message = message,
                TransportType = _currentTransportType
            });
        }
    }
}