using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;

namespace AiStudio4.Services.ProtectedMcpServer
{
    /// <summary>
    /// Wrapper that automatically injects extra properties into ITool instances for MCP server execution.
    /// This allows existing tools to work in MCP context without any modifications.
    /// </summary>
    public class McpToolWrapper : ITool
    {
        private readonly ITool _wrappedTool;
        private readonly IBuiltInToolExtraPropertiesService _extraPropertiesService;

        public McpToolWrapper(ITool wrappedTool, IBuiltInToolExtraPropertiesService extraPropertiesService)
        {
            _wrappedTool = wrappedTool;
            _extraPropertiesService = extraPropertiesService;
        }

        public Tool GetToolDefinition()
        {
            return _wrappedTool.GetToolDefinition();
        }

        public async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            // Automatically fetch the latest extra properties for this tool
            var toolDefinition = _wrappedTool.GetToolDefinition();
            var toolName = toolDefinition.Name;
            
            // Convert tool name to the expected format (first letter lowercase)
            var formattedToolName = $"{toolName.Substring(0, 1).ToLower()}{toolName.Substring(1)}";
            
            // Get the latest extra properties from the service
            var latestExtraProperties = _extraPropertiesService.GetExtraProperties(formattedToolName);
            
            // Merge with any provided extra properties (provided ones take precedence)
            var mergedExtraProperties = new Dictionary<string, string>(latestExtraProperties);
            if (extraProperties != null)
            {
                foreach (var kvp in extraProperties)
                {
                    mergedExtraProperties[kvp.Key] = kvp.Value;
                }
            }
            
            // Call the wrapped tool with the merged extra properties
            return await _wrappedTool.ProcessAsync(toolParameters, mergedExtraProperties);
        }

        public void UpdateProjectRoot()
        {
            _wrappedTool.UpdateProjectRoot();
        }
    }
}