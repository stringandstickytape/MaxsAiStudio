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
    /// Service that manages built-in tool implementations
    /// </summary>
    public class BuiltinToolService : IBuiltinToolService
    {
        private readonly ILogger<BuiltinToolService> _logger;
        private readonly Dictionary<string, ITool> _tools;

        public BuiltinToolService(
            ILogger<BuiltinToolService> logger,
            CodeDiffTool codeDiffTool,
            StopTool stopTool,
            ReadFilesTool readFilesTool,
            ReadDatabaseSchemaTool readDatabaseSchemaTool,
            ThinkTool thinkTool,
            DirectoryTreeTool directoryTreeTool,
            FileSearchTool fileSearchTool,
            RetrieveTextFromUrlTool retrieveTextFromUrlTool,
            RunDuckDuckGoSearchTool runDuckDuckGoSearchTool)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize the tools dictionary
            _tools = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase)
            {
                { codeDiffTool.GetToolDefinition().Name, codeDiffTool },
                { stopTool.GetToolDefinition().Name, stopTool },
                { readFilesTool.GetToolDefinition().Name, readFilesTool },
                { thinkTool.GetToolDefinition().Name, thinkTool },
                { directoryTreeTool.GetToolDefinition().Name, directoryTreeTool },
                { fileSearchTool.GetToolDefinition().Name, fileSearchTool },
                { retrieveTextFromUrlTool.GetToolDefinition().Name, retrieveTextFromUrlTool },
                { readDatabaseSchemaTool.GetToolDefinition().Name,  readDatabaseSchemaTool }

                { runDuckDuckGoSearchTool.GetToolDefinition().Name, runDuckDuckGoSearchTool }
                // Add more tools here as they are implemented
            };
        }

        /// <summary>
        /// Gets the list of built-in tools
        /// </summary>
        public List<Tool> GetBuiltinTools()
        {
            return _tools.Values.Select(t => t.GetToolDefinition()).ToList();
        }

        /// <summary>
        /// Processes a built-in tool call
        /// </summary>
        public async Task<BuiltinToolResult> ProcessBuiltinToolAsync(string toolName, string toolParameters)
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
                    return await tool.ProcessAsync(toolParameters);
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