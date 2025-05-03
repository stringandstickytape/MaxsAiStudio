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
    /// Implementation of the NpmInstall tool
    /// </summary>
    public class NpmInstallTool : BaseToolImplementation
    {
        public NpmInstallTool(ILogger<NpmInstallTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the NpmInstall tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef02",
                Name = "NpmInstall",
                Description = "Installs npm dependencies",
                Schema = @"{
  ""name"": ""NpmInstall"",
  ""description"": ""Installs npm dependencies in the specified directory."",
  ""input_schema"": {
                ""properties"": {
""workingDirectory"": {
                    ""title"": ""Working Directory"",
                    ""type"": ""string"",
                    ""description"":""Directory containing package.json""
},
 ""packageName"": {
                    ""title"": ""Package Name"",
                    ""type"": ""string"",
                    ""description"": ""Specific package to install (if not provided, installs all dependencies)""
},
""isDev"": {
                    ""default"": false,
                    ""title"": ""Is Dev Dependency"",
                    ""type"": ""boolean"",
                    ""description"": ""Whether to install as a dev dependency""
},
""version"": {
                    ""title"": ""Version"",
                    ""type"": ""string"",
                    ""description"": ""Specific version to install""
}
            },
           ""required"": [""workingDirectory""],
            ""title"": ""NpmInstallArguments"",
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
        /// Processes a NpmInstall tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting NpmInstall tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters with defaults
                var workingDirectory = parameters.ContainsKey("workingDirectory") ? parameters["workingDirectory"].ToString() : "";
                var packageName = parameters.ContainsKey("packageName") ? parameters["packageName"].ToString() : "";
                var isDev = parameters.ContainsKey("isDev") ? Convert.ToBoolean(parameters["isDev"]) : false;
                var version = parameters.ContainsKey("version") ? parameters["version"].ToString() : "";

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

                // Build the npm install command
                string command = "npm install";
                if (!string.IsNullOrEmpty(packageName))
                {
                    command += $" {packageName}";
                    if (!string.IsNullOrEmpty(version))
                    {
                        command += $"@{version}";
                    }
                    if (isDev)
                    {
                        command += " --save-dev";
                    }
                }

                SendStatusUpdate($"Running: {command} in {workingPath}...");

                // Execute npm install command using the helper
                string npmCommand = "npm";
                bool useCmd = true; // npm is a batch file and needs cmd.exe
                
                // Create a process to capture output and error
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = useCmd ? "cmd.exe" : npmCommand,
                        Arguments = useCmd ? $"/c {command}" : command,
                        WorkingDirectory = workingPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    SendStatusUpdate($"Error installing npm packages: {error}");
                    return CreateResult(false, true, $"Error installing npm packages: {error}");
                }

                SendStatusUpdate("Npm packages installed successfully.");
                return CreateResult(true, true, $"Npm packages installed successfully.\n\nOutput:\n{output}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing NpmInstall tool");
                SendStatusUpdate($"Error processing NpmInstall tool: {ex.Message}");
                return CreateResult(false, true, $"Error processing NpmInstall tool: {ex.Message}");
            }
        }
    }
}