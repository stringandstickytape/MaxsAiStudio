using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using AiStudio4.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services;

/// <summary>
/// Centralized tool execution service that handles both built-in and MCP tools.
/// This replaces the logic currently in ToolProcessorService for individual tool execution.
/// </summary>
public class ToolExecutor : IToolExecutor
{
    private readonly ILogger<ToolExecutor> _logger;
    private readonly IBuiltinToolService _builtinToolService;
    private readonly IMcpService _mcpService;
    private readonly IToolService _toolService;
    private readonly IInterjectionService _interjectionService;
    private readonly IWebSocketNotificationService _notificationService;

    public ToolExecutor(
        ILogger<ToolExecutor> logger,
        IBuiltinToolService builtinToolService, 
        IMcpService mcpService, 
        IToolService toolService,
        IInterjectionService interjectionService,
        IWebSocketNotificationService notificationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _builtinToolService = builtinToolService ?? throw new ArgumentNullException(nameof(builtinToolService));
        _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
        _toolService = toolService ?? throw new ArgumentNullException(nameof(toolService));
        _interjectionService = interjectionService ?? throw new ArgumentNullException(nameof(interjectionService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<BuiltinToolResult> ExecuteToolAsync(string toolName, string toolParameters, ToolExecutionContext context, CancellationToken cancellationToken)
    {
        // Check for user interjections before tool execution
        if (await _interjectionService.HasInterjectionAsync(context.ClientId))
        {
            var interjection = await _interjectionService.GetAndClearInterjectionAsync(context.ClientId);
            return new BuiltinToolResult 
            { 
                WasProcessed = true, 
                ContinueProcessing = false,
                ResultMessage = $"User interjection: {interjection}",
                UserInterjection = interjection
            };
        }

        // Extract and handle task_description
        string taskDescription = null;
        string cleanedToolParameters = toolParameters;

        try
        {
            var toolCallArgs = JObject.Parse(toolParameters);
            if (toolCallArgs.TryGetValue("task_description", out JToken taskDescriptionToken))
            {
                taskDescription = taskDescriptionToken.ToString();
                
                // Send the description to the front-end as a status update
                await _notificationService.NotifyStatusMessage(context.ClientId, taskDescription);
                
                // Remove the property from the arguments object
                toolCallArgs.Remove("task_description");
                
                // Update the parameters to be used by the tool handlers
                cleanedToolParameters = toolCallArgs.ToString(Formatting.None);
                
            }
        }
        catch (JsonException)
        {
            // The response might not be a JSON object, which is fine for some tools.
            _logger.LogWarning("Tool parameters for '{ToolName}' is not a valid JSON object. Cannot extract task description.", toolName);
        }

        // Handle built-in tools
        var tool = await _toolService.GetToolByToolNameAsync(toolName);
        if (tool != null && tool.IsBuiltIn)
        {
            // Notify UI of tool execution start
            await _notificationService.NotifyStatusMessage(context.ClientId, $"Executing tool: {toolName}");
            
            var result = await _builtinToolService.ProcessBuiltinToolAsync(
                tool.Name, 
                cleanedToolParameters, 
                tool.ExtraProperties, 
                context.ClientId);

            // Add task description and output file type to result
            result.TaskDescription = taskDescription;
            result.OutputFileType = tool.OutputFileType;

            // Notify UI of tool execution completion
            await _notificationService.NotifyStatusMessage(context.ClientId, $"Tool {toolName} completed");
            
            return result;
        }

        // Handle MCP tools
        var serverDefinitions = await _mcpService.GetAllServerDefinitionsAsync();
        
        // Check direct MCP tool naming pattern (serverId_toolName)
        if (toolName.Contains("_") && serverDefinitions.Any(x => x.IsEnabled && toolName.StartsWith(x.Id + "_")))
        {
            var serverId = toolName.Split('_')[0];
            var actualToolName = string.Join("_", toolName.Split('_').Skip(1));
            
            return await ExecuteMcpTool(serverId, actualToolName, cleanedToolParameters, context, toolName, cancellationToken, taskDescription);
        }

        // Check for tools without prefix (Claude sometimes drops the prefix)
        foreach (var serverDefinition in serverDefinitions.Where(x => x.IsEnabled))
        {
            var tools = await _mcpService.ListToolsAsync(serverDefinition.Id);
            var mcpTool = tools.FirstOrDefault(x => x.Name == toolName);

            if (mcpTool != null)
            {
                return await ExecuteMcpTool(serverDefinition.Id, toolName, cleanedToolParameters, context, toolName, cancellationToken, taskDescription);
            }
        }

        // Tool not found
        _logger.LogWarning("Tool '{ToolName}' is not an enabled MCP tool or recognized built-in tool.", toolName);

        return new BuiltinToolResult
        {
            WasProcessed = true,
            ContinueProcessing = false,
            ResultMessage = toolParameters,
            TaskDescription = taskDescription,
            OutputFileType = tool.Filetype // MCP tools always return JSON
        };
    }

    private async Task<BuiltinToolResult> ExecuteMcpTool(string serverId, string actualToolName, string cleanedToolParameters, ToolExecutionContext context, string displayToolName, CancellationToken cancellationToken, string taskDescription = null)
    {
        try
        {
            var args = JsonConvert.DeserializeObject<Dictionary<string, object>>(cleanedToolParameters);
            
            // Notify UI of MCP tool execution start
            await _notificationService.NotifyStatusMessage(context.ClientId, $"Executing MCP tool: {displayToolName}");
            
            var mcpResult = await _mcpService.CallToolAsync(serverId, actualToolName, args, cancellationToken);
            
            var result = new BuiltinToolResult 
            { 
                WasProcessed = true, 
                ContinueProcessing = true, // Provider will decide when to stop
                ResultMessage = JsonConvert.SerializeObject(mcpResult.Content),
                TaskDescription = taskDescription,
                OutputFileType = "json" // MCP tools always return JSON
            };

            // Notify UI of MCP tool execution completion
            await _notificationService.NotifyStatusMessage(context.ClientId, $"MCP tool {displayToolName} completed");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MCP tool {ToolName} on server {ServerId}", actualToolName, serverId);
            return new BuiltinToolResult 
            { 
                WasProcessed = false, 
                ContinueProcessing = false,
                ResultMessage = $"Error executing MCP tool '{displayToolName}': {ex.Message}" 
            };
        }
    }

    public async Task<IEnumerable<Core.Models.Tool>> GetAvailableToolsAsync(IEnumerable<string> toolIds)
    {
        var allTools = await _toolService.GetAllToolsAsync();
        return allTools.Where(t => toolIds.Contains(t.Guid));
    }
}