// AiStudio4.Services\BuiltinToolService.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools;
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
        private readonly Dictionary<string, ITool> _tools;
        private readonly IBuiltInToolExtraPropertiesService _extraPropertiesService;

        /// <summary>
        /// Initializes a new instance of the BuiltinToolService class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="availableTools">The collection of available tools injected by DI.</param>
        public BuiltinToolService(ILogger<BuiltinToolService> logger, IEnumerable<ITool> availableTools, IBuiltInToolExtraPropertiesService extraPropertiesService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tools = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);
            _extraPropertiesService = extraPropertiesService ?? throw new ArgumentNullException(nameof(extraPropertiesService));

            if (availableTools == null)
            {
                _logger.LogWarning("No tools were injected into BuiltinToolService.");
                return;
            }

            foreach (var tool in availableTools)
            {
                if (tool == null) continue; // Should not happen with standard DI containers

                try
                {
                    var definition = tool.GetToolDefinition();
                    if (definition == null || string.IsNullOrWhiteSpace(definition.Name))
                    {
                        _logger.LogWarning("Tool of type {ToolType} provided an invalid definition (null or missing name).", tool.GetType().FullName);
                        continue;
                    }

                    if (!_tools.TryAdd(definition.Name, tool))
                    {
                        // Log a warning if a tool with the same name already exists.
                        // The first one registered wins in this case.
                        _logger.LogWarning("Duplicate tool name detected: '{ToolName}'. Tool of type {ToolType} was ignored.", definition.Name, tool.GetType().FullName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting tool definition for type {ToolType}. Tool will be skipped.", tool.GetType().FullName);
                }
            }

            _logger.LogInformation("BuiltinToolService initialized with {ToolCount} tools.", _tools.Count);
        }

        /// <summary>
        /// Gets the list of built-in tools definitions.
        /// </summary>
        public List<Tool> GetBuiltinTools()
        {
            var toolDefs = _tools.Values.Select(t => t.GetToolDefinition()).ToList();
            foreach (var tool in toolDefs)
            {
                // Load persisted extra properties for this tool
                var persisted = _extraPropertiesService.GetExtraProperties(tool.Name);
                if (persisted != null && persisted.Count > 0)
                {
                    tool.ExtraProperties = new Dictionary<string, string>(persisted);
                }
            }
            return toolDefs;
        }

        public void SaveBuiltInToolExtraProperties(string toolName, Dictionary<string, string> extraProperties)
        {
            _extraPropertiesService.SaveExtraProperties(toolName, extraProperties);
        }

        public void UpdateProjectRoot()
        {
            foreach(var tool in _tools)
            {
                tool.Value.UpdateProjectRoot();
            }
        }

        /// <summary>
        /// Processes a built-in tool call
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="toolParameters">Parameters for the tool</param>
        /// <param name="extraProperties">User-edited extra properties for this tool instance</param>
        public async Task<BuiltinToolResult> ProcessBuiltinToolAsync(string toolName, string toolParameters, Dictionary<string, string> extraProperties = null)
        {
            // Default result assumes the tool is not built-in or doesn't need special processing
            var result = new BuiltinToolResult
            {
                WasProcessed = false,
                ContinueProcessing = true
            };

            try
            {
                // Look up the tool in our dictionary
                if (_tools.TryGetValue(toolName, out var tool))
                {
                    // Let the tool implementation handle the processing
                    return await tool.ProcessAsync(toolParameters, extraProperties ?? new Dictionary<string, string>());
                }
                
                // Tool not found in our dictionary
                _logger.LogWarning("Tool '{ToolName}' not found in built-in tools", toolName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing built-in tool {ToolName}", toolName);
                result.ResultMessage = $"Error processing built-in tool: {ex.Message}";
            }
            
            return result;
        }
    }
}