using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;










namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the StartViteDevServer tool
    /// </summary>
    [McpServerToolType]
    public class StartViteDevServerTool : BaseToolImplementation
    {
        private static Process _runningDevServer;
        private readonly IDialogService _dialogService;

        public StartViteDevServerTool(ILogger<StartViteDevServerTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IDialogService dialogService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _dialogService = dialogService;
        }

        
        
        
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.START_VITE_DEV_SERVER_TOOL_GUID,
                Name = "StartViteDevServer",
                Description = "Starts the Vite development server",
                Schema = """
{
  "name": "StartViteDevServer",
  "description": "Starts the Vite development server.",
  "input_schema": {
    "properties": {
      "workingDirectory": { "title": "Working Directory", "type": "string", "description": "Directory containing the Vite project" },
      "port": { "title": "Port", "type": "integer", "description": "Custom port to run on (defaults to 5173)" },
      "host": { "title": "Host", "type": "string", "description": "Host to bind to (defaults to localhost)" }
    },
    "required": ["workingDirectory"],
    "title": "StartViteDevServerArguments",
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

        
        
        
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting StartViteDevServer tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                
                var workingDirectory = parameters.ContainsKey("workingDirectory") ? parameters["workingDirectory"].ToString() : "";
                var port = parameters.ContainsKey("port") ? Convert.ToInt32(parameters["port"]) : 5174;
                var host = parameters.ContainsKey("host") ? parameters["host"].ToString() : "localhost";

                
                var workingPath = _projectRoot;
                if (!string.IsNullOrEmpty(workingDirectory) && workingDirectory != _projectRoot)
                {
                    workingPath = Path.GetFullPath(Path.Combine(_projectRoot, workingDirectory));
                    if (!workingPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        SendStatusUpdate("Error: Working directory is outside the allowed directory.");
                        return CreateResult(true, true, "Error: Working directory is outside the allowed directory.");
                    }
                }

                
                var packageJsonPath = Path.Combine(workingPath, "package.json");
                if (!File.Exists(packageJsonPath))
                {
                    SendStatusUpdate("Error: package.json not found in the specified directory.");
                    return CreateResult(true, true, "Error: package.json not found in the specified directory.");
                }

                
                if (_runningDevServer != null && !_runningDevServer.HasExited)
                {
                    try
                    {
                        _runningDevServer.Kill(true);
                        _runningDevServer = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error stopping previous dev server");
                    }
                }

                
                string commandForDisplay = $"npm run dev -- --port {port} --host {host}";
                string confirmationPrompt = $"AI wants to start the Vite development server in directory '{workingPath}' on {host}:{port}. This will open a new command window. Proceed?";
                
                bool confirmed = await _dialogService.ShowConfirmationAsync("Confirm Vite Dev Server Start", confirmationPrompt, commandForDisplay);
                if (!confirmed)
                {
                    SendStatusUpdate($"Vite dev server start in {workingPath} cancelled by user.");
                    return CreateResult(true, false, "Operation cancelled by user.");
                }

                SendStatusUpdate($"Starting Vite dev server in {workingPath} on {host}:{port}...");

                
                string command = $"npm run dev -- --port {port} --host {host}";

                
                string npmCommand = "npm";
                bool useCmd = true; 
                
                
                _runningDevServer = ViteCommandHelper.CreateProcess(npmCommand, command.Replace("npm ", ""), useCmd, workingPath, _logger, true);

                
                _runningDevServer.Start();

                
                
                
                
                
                
                
                
                
                
                
                

                
                

                
                

                SendStatusUpdate("Vite dev server started successfully.");
                return CreateResult(true, true, $"Vite dev server started successfully on http://{host}:{port}\n\nInitial output:\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing StartViteDevServer tool");
                SendStatusUpdate($"Error processing StartViteDevServer tool: {ex.Message}");
                return CreateResult(false, true, $"Error processing StartViteDevServer tool: {ex.Message}");
            }
        }

        [McpServerTool, Description("Starts the Vite development server")]
        public async Task<string> StartViteDevServer([Description("JSON parameters for StartViteDevServer")] string parameters = "{}")
        {
            try
            {
                var result = await ProcessAsync(parameters, new Dictionary<string, string>());
                
                if (!result.WasProcessed)
                {
                    return "Tool was not processed successfully.";
                }
                
                return result.ResultMessage ?? "Tool executed successfully with no output.";
            }
            catch (Exception ex)
            {
                return $"Error executing tool: {ex.Message}";
            }
        }
    }
}
