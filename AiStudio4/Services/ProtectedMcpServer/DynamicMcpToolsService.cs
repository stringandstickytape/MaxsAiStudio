using AiStudio4.Core.Interfaces;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AiStudio4.Services.ProtectedMcpServer
{
    /// <summary>
    /// Dynamic MCP tools service that exposes all ITool implementations using their existing schemas.
    /// This service dynamically creates MCP tool methods based on the ITool.GetToolDefinition() schemas.
    /// </summary>
    [McpServerToolType]
    public class DynamicMcpToolsService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DynamicMcpToolsService> _logger;
        private readonly IBuiltInToolExtraPropertiesService _extraPropertiesService;
        private readonly Dictionary<string, Type> _toolTypeMapping;

        public DynamicMcpToolsService(
            IServiceProvider serviceProvider, 
            ILogger<DynamicMcpToolsService> logger,
            IBuiltInToolExtraPropertiesService extraPropertiesService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _extraPropertiesService = extraPropertiesService;
            _toolTypeMapping = new Dictionary<string, Type>();
            
            // Build mapping of tool names to their types
            BuildToolMapping();
        }

        private void BuildToolMapping()
        {
            var toolTypes = typeof(ITool).Assembly.GetTypes()
                .Where(type => type.IsClass && 
                              !type.IsAbstract &&
                              typeof(ITool).IsAssignableFrom(type) &&
                              type.GetCustomAttribute<McpServerToolTypeAttribute>() != null)
                .ToList();

            foreach (var toolType in toolTypes)
            {
                try
                {
                    var toolInstance = _serviceProvider.GetService(toolType) as ITool;
                    if (toolInstance != null)
                    {
                        var toolDefinition = toolInstance.GetToolDefinition();
                        if (toolDefinition != null)
                        {
                            _toolTypeMapping[toolDefinition.Name] = toolType;
                            _logger.LogInformation("Mapped tool {ToolName} to type {ToolType}", toolDefinition.Name, toolType.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to map tool type {ToolType}", toolType.Name);
                }
            }
        }

        private async Task<string> ExecuteTool(string toolName, string parameters)
        {
            try
            {
                if (!_toolTypeMapping.TryGetValue(toolName, out var toolType))
                {
                    return JsonSerializer.Serialize(new { error = $"Tool '{toolName}' not found" });
                }

                var toolInstance = _serviceProvider.GetService(toolType) as ITool;
                if (toolInstance == null)
                {
                    return JsonSerializer.Serialize(new { error = $"Could not create instance of tool '{toolName}'" });
                }

                // Wrap the tool to provide extra properties
                var wrapper = new McpToolWrapper(toolInstance, _extraPropertiesService);
                
                // Execute the tool
                var result = await wrapper.ProcessAsync(parameters, new Dictionary<string, string>());
                
                if (result.WasProcessed)
                {
                    return result.ResultMessage ?? "Tool executed successfully";
                }
                else
                {
                    return JsonSerializer.Serialize(new { error = result.ResultMessage ?? "Tool execution failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
                return JsonSerializer.Serialize(new { error = $"Tool execution error: {ex.Message}" });
            }
        }

        // Individual tool methods for each discovered tool

        [McpServerTool, Description("Modifies content within one or more existing files.")]
        public async Task<string> ModifyFiles([Description("JSON parameters for ModifyFiles")] string parameters = "{}")
        {
            return await ExecuteTool("ModifyFiles", parameters);
        }

        [McpServerTool, Description("Read the contents of one or multiple files.")]
        public async Task<string> ReadFiles([Description("JSON parameters for ReadFiles")] string parameters = "{}")
        {
            return await ExecuteTool("ReadFiles", parameters);
        }

        [McpServerTool, Description("Creates a new file with the specified content.")]
        public async Task<string> CreateNewFile([Description("JSON parameters for CreateNewFile")] string parameters = "{}")
        {
            return await ExecuteTool("CreateNewFile", parameters);
        }

        [McpServerTool, Description("Searches for text patterns in files using regular expressions.")]
        public async Task<string> FileRegExSearch([Description("JSON parameters for FileRegExSearch")] string parameters = "{}")
        {
            return await ExecuteTool("FileRegExSearch", parameters);
        }

        [McpServerTool, Description("Searches for files and folders using glob patterns.")]
        public async Task<string> FileSearch([Description("JSON parameters for FileSearch")] string parameters = "{}")
        {
            return await ExecuteTool("FileSearch", parameters);
        }

        [McpServerTool, Description("Displays the directory structure of a specified path.")]
        public async Task<string> DirectoryTree([Description("JSON parameters for DirectoryTree")] string parameters = "{}")
        {
            return await ExecuteTool("DirectoryTree", parameters);
        }

        [McpServerTool, Description("Deletes a file from the filesystem.")]
        public async Task<string> DeleteFile([Description("JSON parameters for DeleteFile")] string parameters = "{}")
        {
            return await ExecuteTool("DeleteFile", parameters);
        }

        [McpServerTool, Description("Renames or moves a file or directory.")]
        public async Task<string> RenameFile([Description("JSON parameters for RenameFile")] string parameters = "{}")
        {
            return await ExecuteTool("RenameFile", parameters);
        }

        [McpServerTool, Description("Replaces the entire content of a file.")]
        public async Task<string> ReplaceFile([Description("JSON parameters for ReplaceFile")] string parameters = "{}")
        {
            return await ExecuteTool("ReplaceFile", parameters);
        }

        [McpServerTool, Description("Finds and replaces text in files.")]
        public async Task<string> FindAndReplace([Description("JSON parameters for FindAndReplace")] string parameters = "{}")
        {
            return await ExecuteTool("FindAndReplace", parameters);
        }

        [McpServerTool, Description("Launches a URL in the default browser.")]
        public async Task<string> LaunchUrl([Description("JSON parameters for LaunchUrl")] string parameters = "{}")
        {
            return await ExecuteTool("LaunchUrl", parameters);
        }

        [McpServerTool, Description("Performs a DuckDuckGo search.")]
        public async Task<string> RunDuckDuckGoSearch([Description("JSON parameters for RunDuckDuckGoSearch")] string parameters = "{}")
        {
            return await ExecuteTool("RunDuckDuckGoSearch", parameters);
        }

        [McpServerTool, Description("Retrieves text content from a URL.")]
        public async Task<string> RetrieveTextFromUrl([Description("JSON parameters for RetrieveTextFromUrl")] string parameters = "{}")
        {
            return await ExecuteTool("RetrieveTextFromUrl", parameters);
        }

        [McpServerTool, Description("Performs a Google search using Gemini API.")]
        public async Task<string> GeminiGoogleSearch([Description("JSON parameters for GeminiGoogleSearch")] string parameters = "{}")
        {
            return await ExecuteTool("GeminiGoogleSearch", parameters);
        }

        [McpServerTool, Description("Performs a Google search using Custom Search API.")]
        public async Task<string> GoogleCustomSearchApi([Description("JSON parameters for GoogleCustomSearchApi")] string parameters = "{}")
        {
            return await ExecuteTool("GoogleCustomSearchApi", parameters);
        }

        [McpServerTool, Description("Shows the current git status.")]
        public async Task<string> GitStatus([Description("JSON parameters for GitStatus")] string parameters = "{}")
        {
            return await ExecuteTool("GitStatus", parameters);
        }

        [McpServerTool, Description("Shows git log history.")]
        public async Task<string> GitLog([Description("JSON parameters for GitLog")] string parameters = "{}")
        {
            return await ExecuteTool("GitLog", parameters);
        }

        [McpServerTool, Description("Creates a git commit.")]
        public async Task<string> GitCommit([Description("JSON parameters for GitCommit")] string parameters = "{}")
        {
            return await ExecuteTool("GitCommit", parameters);
        }

        [McpServerTool, Description("Manages git branches.")]
        public async Task<string> GitBranch([Description("JSON parameters for GitBranch")] string parameters = "{}")
        {
            return await ExecuteTool("GitBranch", parameters);
        }
    }
}