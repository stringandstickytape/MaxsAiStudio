using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the CheckNodeVersion tool
    /// </summary>
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
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef06",
                Name = "CheckNodeVersion",
                Description = "Checks if Node.js and npm are installed and returns their versions",
                Schema = @"{
  ""name"": ""CheckNodeVersion"",
  ""description"": ""Checks if Node.js and npm are installed and returns their versions."",
  ""input_schema"": {
                ""properties"": {},
            ""title"": ""CheckNodeVersionArguments"",
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
            try
            {
                // For npm and potentially other commands that are batch files (.cmd, .bat)
                // we need to use cmd.exe to execute them
                bool useCmd = command.Equals("npm", StringComparison.OrdinalIgnoreCase);
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = useCmd ? "cmd.exe" : command,
                    Arguments = useCmd ? $"/c {command} {arguments}" : arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // Ensure the process inherits environment variables, particularly PATH
                // Combine Process, Machine, and User PATH variables to ensure all locations are searched
                string processPath = Environment.GetEnvironmentVariable("PATH");
                string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
                string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                
                string combinedPath = string.Join(";", 
                    new[] { processPath, machinePath, userPath }
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct());
                
                startInfo.EnvironmentVariables["PATH"] = combinedPath;
                
                // Log the PATH for debugging purposes
                _logger.LogDebug($"Using PATH: {combinedPath}");
                _logger.LogDebug($"Executing: {startInfo.FileName} {startInfo.Arguments}");
                
                var process = new Process { StartInfo = startInfo };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning($"Command exited with code {process.ExitCode}: {error}");
                    return string.Empty;
                }

                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing command {command} {arguments}");
                return string.Empty;
            }
        }
    }
}