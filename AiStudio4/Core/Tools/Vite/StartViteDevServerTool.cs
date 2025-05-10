// AiStudio4/Core/Tools/Vite/StartViteDevServerTool.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the StartViteDevServer tool
    /// </summary>
    public class StartViteDevServerTool : BaseToolImplementation
    {
        private static Process _runningDevServer;
        private readonly IDialogService _dialogService;

        public StartViteDevServerTool(ILogger<StartViteDevServerTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IDialogService dialogService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _dialogService = dialogService;
        }

        /// <summary>
        /// Gets the StartViteDevServer tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef04",
                Name = "StartViteDevServer",
                Description = "Starts the Vite development server",
                Schema = @"{
  ""name"": ""StartViteDevServer"",
  ""description"": ""Starts the Vite development server."",
  ""input_schema"": {
                ""properties"": {
""workingDirectory"": {
                    ""title"": ""Working Directory"",
                    ""type"": ""string"",
                    ""description"":""Directory containing the Vite project""
},
 ""port"": {
                    ""title"": ""Port"",
                    ""type"": ""integer"",
                    ""description"": ""Custom port to run on (defaults to 5173)""
},
""host"": {
                    ""title"": ""Host"",
                    ""type"": ""string"",
                    ""description"": ""Host to bind to (defaults to localhost)""
}
            },
           ""required"": [""workingDirectory""],
            ""title"": ""StartViteDevServerArguments"",
            ""type"": ""object""
  }
}",
                Categories = new List<string> { "Vite" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a StartViteDevServer tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting StartViteDevServer tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters with defaults
                var workingDirectory = parameters.ContainsKey("workingDirectory") ? parameters["workingDirectory"].ToString() : "";
                var port = parameters.ContainsKey("port") ? Convert.ToInt32(parameters["port"]) : 5174;
                var host = parameters.ContainsKey("host") ? parameters["host"].ToString() : "localhost";

                // Get the working directory path (relative to project root for security)
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

                // Check if package.json exists
                var packageJsonPath = Path.Combine(workingPath, "package.json");
                if (!File.Exists(packageJsonPath))
                {
                    SendStatusUpdate("Error: package.json not found in the specified directory.");
                    return CreateResult(true, true, "Error: package.json not found in the specified directory.");
                }

                // Kill any existing dev server process
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

                // Confirmation Dialog
                string commandForDisplay = $"npm run dev -- --port {port} --host {host}";
                string confirmationPrompt = $"AI wants to start the Vite development server in directory '{workingPath}' on {host}:{port}. This will open a new command window. Proceed?";
                
                bool confirmed = await _dialogService.ShowConfirmationAsync("Confirm Vite Dev Server Start", confirmationPrompt, commandForDisplay);
                if (!confirmed)
                {
                    SendStatusUpdate($"Vite dev server start in {workingPath} cancelled by user.");
                    return CreateResult(true, false, "Operation cancelled by user.");
                }

                SendStatusUpdate($"Starting Vite dev server in {workingPath} on {host}:{port}...");

                // Build the command with host and port options
                string command = $"npm run dev -- --port {port} --host {host}";

                // Execute the dev server command using cmd.exe for npm
                string npmCommand = "npm";
                bool useCmd = true; // npm is a batch file and needs cmd.exe
                
                // Use the helper to create the process with window shown
                _runningDevServer = ViteCommandHelper.CreateProcess(npmCommand, command.Replace("npm ", ""), useCmd, workingPath, _logger, true);

                // Start the process and don't redirect output/error since we want to show the window
                _runningDevServer.Start();

                // Read the first few lines of output to get the server URL
                //string output = "";
                //for (int i = 0; i < 20; i++)
                //{
                //    string line = await _runningDevServer.StandardOutput.ReadLineAsync();
                //    if (line == null) break;
                //    output += line + "\n";
                //    if (line.Contains("Local:") || line.Contains("ready in"))
                //    {
                //        break;
                //    }
                //}

                // We don't need to read output if we're showing the window
                // The user will see the output directly in the cmd window

                // We don't need to read error output if we're showing the window
                // The user will see the errors directly in the cmd window

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
    }
}