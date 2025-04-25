// AiStudio4.Services\BuiltinToolService.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    /// <summary>
    /// Service that manages built-in tool implementations by consuming an injected collection of ITool.
    /// </summary>
    public class BuiltinToolService : IBuiltinToolService
    {
        private readonly ILogger<BuiltinToolService> _logger;
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly IBuiltInToolExtraPropertiesService _builtInToolExtraPropertiesService;
        private readonly IStatusMessageService _statusMessageService;
        private readonly List<ITool> _builtinTools;

        public BuiltinToolService(
            ILogger<BuiltinToolService> logger,
            IGeneralSettingsService generalSettingsService,
            IBuiltInToolExtraPropertiesService builtInToolExtraPropertiesService,
            IStatusMessageService statusMessageService,
            IEnumerable<ITool> builtinTools)
        {
            _logger = logger;
            _generalSettingsService = generalSettingsService;
            _builtInToolExtraPropertiesService = builtInToolExtraPropertiesService;
            _statusMessageService = statusMessageService;
            _builtinTools = builtinTools.ToList();

            // Load extra properties for all built-in tools
            LoadExtraProperties();
        }

        private void LoadExtraProperties()
        {
            foreach (var tool in _builtinTools)
            {
                var def = tool.GetToolDefinition();
                if (def != null && !string.IsNullOrWhiteSpace(def.Name))
                {
                    var lower = $"{def.Name.Substring(0, 1).ToLower()}{def.Name.Substring(1)}";
                    var persisted = _builtInToolExtraPropertiesService.GetExtraProperties(lower);
                    if (persisted != null && persisted.Count > 0)
                    {
                        def.ExtraProperties = new Dictionary<string, string>(persisted);
                    }
                }
            }
        }

        public List<Tool> GetBuiltinTools()
        {
            var toolDefs = _builtinTools.Select(t => t.GetToolDefinition()).ToList();
            foreach (var tool in toolDefs)
            {
                // Load persisted extra properties for this tool
                var lower = $"{tool.Name.Substring(0, 1).ToLower()}{tool.Name.Substring(1)}";
                var persisted = _builtInToolExtraPropertiesService.GetExtraProperties(lower);
                if (persisted != null && persisted.Count > 0)
                {
                    tool.ExtraProperties = new Dictionary<string, string>(persisted);
                }
            }
            return toolDefs;
        }

        public void SaveBuiltInToolExtraProperties(string toolName, Dictionary<string, string> extraProperties)
        {
            _builtInToolExtraPropertiesService.SaveExtraProperties(toolName, extraProperties);
        }

        public void UpdateProjectRoot()
        {
            foreach(var tool in _builtinTools)
            {
                tool.UpdateProjectRoot();
            }
        }

        public async Task<BuiltinToolResult> ProcessBuiltinToolAsync(string toolName, string toolParameters, Dictionary<string, string> extraProperties = null, string clientId = null)
        {
            try
            {
                var tool = _builtinTools.FirstOrDefault(t => t.GetToolDefinition().Name == toolName);
                if (tool == null)
                {
                    _logger.LogWarning("Tool {ToolName} not found", toolName);
                    return new BuiltinToolResult { WasProcessed = false, ContinueProcessing = true };
                }

                // If the tool is a BaseToolImplementation, set up status updates
                if (tool is BaseToolImplementation baseToolImpl)
                {
                    // Set the client ID if provided
                    if (!string.IsNullOrEmpty(clientId))
                    {
                        baseToolImpl.SetClientId(clientId);
                    }
                }

                return await tool.ProcessAsync(toolParameters, extraProperties ?? new Dictionary<string, string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing built-in tool {ToolName}", toolName);
                return new BuiltinToolResult
                {
                    WasProcessed = false,
                    ContinueProcessing = true,
                    ResultMessage = $"Error processing built-in tool: {ex.Message}"
                };
            }
        }
    }
}