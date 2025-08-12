using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AiStudio4.McpStandalone.Services
{
    /// <summary>
    /// Manages dynamic registration and unregistration of MCP tools at runtime
    /// </summary>
    public interface IDynamicToolManager
    {
        void Initialize(McpServerOptions mcpServerOptions);
        void UpdateToolRegistration(string toolId, bool isEnabled);
        void RefreshAllTools();
    }

    public class DynamicToolManager : IDynamicToolManager
    {
        private readonly ILogger<DynamicToolManager> _logger;
        private readonly StandaloneSettingsService _settingsService;
        private readonly IServiceProvider _serviceProvider;
        private McpServerOptions? _mcpServerOptions;
        private readonly Dictionary<string, List<McpServerTool>> _toolsByName = new();
        private bool _initialized = false;

        public DynamicToolManager(
            ILogger<DynamicToolManager> logger,
            StandaloneSettingsService settingsService,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _settingsService = settingsService;
            _serviceProvider = serviceProvider;
        }

        public void Initialize(McpServerOptions mcpServerOptions)
        {
            _mcpServerOptions = mcpServerOptions;
            
            // Store all initially registered tools
            var allTools = _mcpServerOptions.Capabilities.Tools.ToolCollection.ToList();
            
            // Group tools by their type name
            foreach (var tool in allTools)
            {
                // For now, assume all tools are YouTube tools since that's what we registered
                // We'll need to enhance this when we add more tools
                if (!_toolsByName.ContainsKey("YouTubeSearchTool"))
                {
                    _toolsByName["YouTubeSearchTool"] = new List<McpServerTool>();
                }
                _toolsByName["YouTubeSearchTool"].Add(tool);
                // Add more tool identification logic as needed when more tools are added
            }

            _logger.LogInformation("Found {Count} total tools in collection", allTools.Count);
            foreach (var kvp in _toolsByName)
            {
                _logger.LogInformation("Tool group {Name}: {Count} methods", kvp.Key, kvp.Value.Count);
            }

            _initialized = true;
            RefreshAllTools();
        }

        public void UpdateToolRegistration(string toolId, bool isEnabled)
        {
            if (_mcpServerOptions == null || !_initialized)
            {
                _logger.LogWarning("MCP Server options not initialized, cannot update tool registration");
                return;
            }

            try
            {
                if (!_toolsByName.TryGetValue(toolId, out var tools))
                {
                    _logger.LogWarning("Unknown tool ID: {ToolId}", toolId);
                    return;
                }

                var collection = _mcpServerOptions.Capabilities.Tools.ToolCollection;
                
                if (isEnabled)
                {
                    // Add tools if not already present
                    foreach (var tool in tools)
                    {
                        if (!collection.Contains(tool))
                        {
                            collection.Add(tool);
                            _logger.LogInformation("Added tool method to collection for {ToolId}", toolId);
                        }
                    }
                }
                else
                {
                    // Remove tools if present
                    foreach (var tool in tools)
                    {
                        if (collection.Contains(tool))
                        {
                            collection.Remove(tool);
                            _logger.LogInformation("Removed tool method from collection for {ToolId}", toolId);
                        }
                    }
                }

                // The collection's Changed event will automatically notify the client
                _logger.LogInformation("Tool {ToolId} is now {State}", toolId, isEnabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tool registration for {ToolId}", toolId);
            }
        }

        public void RefreshAllTools()
        {
            if (_mcpServerOptions == null || !_initialized)
            {
                _logger.LogWarning("MCP Server options not initialized, cannot refresh tools");
                return;
            }

            var enabledTools = _settingsService.GetEnabledTools();
            _logger.LogInformation("Refreshing tools. Enabled: {Tools}", string.Join(", ", enabledTools));
            
            foreach (var kvp in _toolsByName)
            {
                var isEnabled = enabledTools.Contains(kvp.Key);
                UpdateToolRegistration(kvp.Key, isEnabled);
            }
        }
    }
}