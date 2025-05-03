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

        public StartViteDevServerTool(ILogger<StartViteDevServerTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
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
                var port = parameters.ContainsKey("port") ? Convert.ToInt32(parameters["port"]) : 5173;
                var host = parameters.ContainsKey("host") ? parameters["host"].ToString() : "localhost";

                // Get the working directory path (relative to project root for security)
                var workingPath = _projectRoot;
                if (!string.IsNullOrEmpty(workingDirectory) && workingDirectory != _projectRoot)
                {
                    workingPath = Path.GetFullPath(Path.Combine(_projectRoot, workingDirectory));
                    if (!workingPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        SendStatusUpdate("Error: Working directory is outside the allowed directory.");
                        return CreateResult(false, true, "Error: Working directory is outside the allowed directory.");
                    }
                }

                // Check if package.json exists
                var packageJsonPath = Path.Combine(workingPath, "package.json");
                if (!File.Exists(packageJsonPath))
                {
                    SendStatusUpdate("Error: package.json not found in the specified directory.");
                    return CreateResult(false, true, "Error: package.json not found in the specified directory.");
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

                SendStatusUpdate($"Starting Vite dev server in {workingPath} on {host}:{port}...");

                // Build the command with host and port options
                string command = $"npm run dev -- --port {port} --host {host}";

                // Execute the dev server command
                _runningDevServer = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        WorkingDirectory = workingPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                _runningDevServer.Start();

                // Read the first few lines of output to get the server URL
                string output = "";
                for (int i = 0; i < 20; i++)
                {
                    string line = await _runningDevServer.StandardOutput.ReadLineAsync();
                    if (line == null) break;
                    output += line + "\n";
                    if (line.Contains("Local:") || line.Contains("ready in"))
                    {
                        break;
                    }
                }

                // Start a background task to continue reading output
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!_runningDevServer.StandardOutput.EndOfStream)
                        {
                            string line = await _runningDevServer.StandardOutput.ReadLineAsync();
                            if (line != null)
                            {
                                _logger.LogInformation($"Vite server: {line}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading Vite server output");
                    }
                });

                // Start a background task to read error output
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!_runningDevServer.StandardError.EndOfStream)
                        {
                            string line = await _runningDevServer.StandardError.ReadLineAsync();
                            if (line != null)
                            {
                                _logger.LogError($"Vite server error: {line}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading Vite server error output");
                    }
                });

                SendStatusUpdate("Vite dev server started successfully.");
                return CreateResult(true, true, $"Vite dev server started successfully on http://{host}:{port}\n\nInitial output:\n{output}");
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