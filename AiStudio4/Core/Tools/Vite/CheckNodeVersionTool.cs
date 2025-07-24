using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the CheckNodeVersion tool
    /// </summary>
    [McpServerToolType]
    public class CheckNodeVersionTool : BaseToolImplementation
    {
        public CheckNodeVersionTool(ILogger<CheckNodeVersionTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the CheckNodeVersion tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.CHECK_NODE_VERSION_TOOL_GUID,
                Name = "CheckNodeVersion",
                Description = "Checks if Node.js and npm are installed and returns their versions",
                Schema = """
{
  "name": "CheckNodeVersion",
  "description": "Checks if Node.js and npm are installed and returns their versions.",
  "input_schema": {
    "properties": {},
    "title": "CheckNodeVersionArguments",
    "type": "object"
  }
}
""",
                Categories = new List<string> { "Vite" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a CheckNodeVersion tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting CheckNodeVersion tool execution...");

                // Check Node.js version
                string nodeVersion = await GetCommandOutputAsync("node", "-v");
                if (string.IsNullOrEmpty(nodeVersion))
                {
                    return CreateResult(false, true, "Error: Node.js is not installed or not in the PATH.");
                }

                // Check npm version
                string npmVersion = await GetCommandOutputAsync("npm", "-v");
                if (string.IsNullOrEmpty(npmVersion))
                {
                    return CreateResult(false, true, $"Node.js version: {nodeVersion}\nError: npm is not installed or not in the PATH.");
                }

                SendStatusUpdate("Node.js and npm versions checked successfully.");
                return CreateResult(true, true, $"Node.js version: {nodeVersion.Trim()}\nnpm version: {npmVersion.Trim()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CheckNodeVersion tool");
                SendStatusUpdate($"Error processing CheckNodeVersion tool: {ex.Message}");
                return CreateResult(false, true, $"Error processing CheckNodeVersion tool: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to get command output
        /// </summary>
        private async Task<string> GetCommandOutputAsync(string command, string arguments)
        {
            // For npm and potentially other commands that are batch files (.cmd, .bat)
            // we need to use cmd.exe to execute them
            bool useCmd = command.Equals("npm", StringComparison.OrdinalIgnoreCase);
            
            return await ViteCommandHelper.GetCommandOutputAsync(command, arguments, useCmd, _logger);
        }

        [McpServerTool, Description("Checks if Node.js and npm are installed and returns their versions")]
        public async Task<string> CheckNodeVersion([Description("JSON parameters for CheckNodeVersion")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
