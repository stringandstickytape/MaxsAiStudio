using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AiStudio4.Core.Interfaces;
using McpTool = ModelContextProtocol.Protocol.Tool;
using McpContentBlock = ModelContextProtocol.Protocol.ContentBlock;
using McpTextContentBlock = ModelContextProtocol.Protocol.TextContentBlock;
using McpCallToolResult = ModelContextProtocol.Protocol.CallToolResult;
// McpException might not exist, so we'll use InvalidOperationException
using AiStudioTool = AiStudio4.Core.Models.Tool;

namespace AiStudio4.Services.Mcp
{
    public class IToolToMcpAdapter
    {
        private readonly IEnumerable<ITool> _tools;
        private readonly IBuiltInToolExtraPropertiesService _extraPropertiesService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IToolToMcpAdapter> _logger;
        private readonly Dictionary<string, ITool> _toolLookup;

        public IToolToMcpAdapter(
            IEnumerable<ITool> tools, 
            IBuiltInToolExtraPropertiesService extraPropertiesService,
            IServiceProvider serviceProvider,
            ILogger<IToolToMcpAdapter> logger)
        {
            _tools = tools;
            _extraPropertiesService = extraPropertiesService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _toolLookup = tools.ToDictionary(t => t.GetToolDefinition().Name, t => t);
        }

        public McpTool ConvertToMcpTool(ITool tool)
        {
            var toolDef = tool.GetToolDefinition();
            
            try
            {
                return new McpTool
                {
                    Name = toolDef.Name,
                    Description = toolDef.Description,
                    InputSchema = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(toolDef.Schema)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert tool {ToolName} to MCP format", toolDef.Name);
                // Return a basic schema if parsing fails
                return new McpTool
                {
                    Name = toolDef.Name,
                    Description = toolDef.Description,
                    InputSchema = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(@"{""type"":""object"",""properties"":{}}")
                };
            }
        }

        public async Task<McpCallToolResult> ExecuteTool(
            string toolName, 
            Dictionary<string, object?> arguments, 
            CancellationToken cancellationToken)
        {
            if (!_toolLookup.TryGetValue(toolName, out var tool))
            {
                throw new InvalidOperationException($"Unknown tool: '{toolName}'");
            }

            try
            {
                _logger.LogInformation("Executing tool {ToolName} via MCP", toolName);
                
                // Convert arguments to JSON string as expected by ITool
                var jsonArguments = System.Text.Json.JsonSerializer.Serialize(arguments);
                
                // Get extra properties for the tool
                var extraProperties = _extraPropertiesService.GetExtraProperties(
                    tool.GetToolDefinition().Name);

                // Execute the tool
                var result = await tool.ProcessAsync(jsonArguments, extraProperties ?? new Dictionary<string, string>());

                // Convert result to MCP format
                var content = new List<McpContentBlock>();

                if (!string.IsNullOrEmpty(result.ResultMessage))
                {
                    content.Add(new McpTextContentBlock 
                    { 
                        Type = "text", 
                        Text = result.ResultMessage 
                    });
                }

                if (!string.IsNullOrEmpty(result.StatusMessage))
                {
                    content.Add(new McpTextContentBlock 
                    { 
                        Type = "text", 
                        Text = $"Status: {result.StatusMessage}" 
                    });
                }

                // If no content was added, add a success message
                if (content.Count == 0)
                {
                    content.Add(new McpTextContentBlock 
                    { 
                        Type = "text", 
                        Text = "Tool executed successfully" 
                    });
                }

                return new McpCallToolResult { Content = content };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool execution failed for {ToolName}", toolName);
                throw new InvalidOperationException($"Tool execution failed: {ex.Message}", ex);
            }
        }
    }
}