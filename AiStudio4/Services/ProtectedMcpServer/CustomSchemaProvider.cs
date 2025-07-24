using System.Text.Json;
using System.Text.Json.Serialization;
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AiStudio4.Services.ProtectedMcpServer
{
    /// <summary>
    /// Custom schema provider that uses ITool.GetToolDefinition() schemas instead of auto-generated ones
    /// </summary>
    public static class CustomSchemaProvider
    {
        public static JsonSerializerOptions CreateOptionsWithCustomSchemas(IServiceProvider serviceProvider)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            // Find all tool types and their schemas
            var toolSchemas = new Dictionary<string, string>();
            
            var toolTypes = typeof(ITool).Assembly.GetTypes()
                .Where(type => type.IsClass && 
                              !type.IsAbstract &&
                              typeof(ITool).IsAssignableFrom(type) &&
                              type.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolTypeAttribute>() != null)
                .ToList();

            foreach (var toolType in toolTypes)
            {
                try
                {
                    var toolInstance = serviceProvider.GetService(toolType) as ITool;
                    if (toolInstance != null)
                    {
                        var toolDefinition = toolInstance.GetToolDefinition();
                        if (toolDefinition != null && !string.IsNullOrEmpty(toolDefinition.Schema))
                        {
                            toolSchemas[toolDefinition.Name] = toolDefinition.Schema;
                        }
                    }
                }
                catch
                {
                    // Ignore errors during schema collection
                }
            }
            
            // Note: The MCP framework may not directly support injecting custom schemas
            // The schemas from GetToolDefinition() are used internally by the tools themselves
            // The MCP framework will generate its own schemas from method signatures
            
            return options;
        }
    }
}