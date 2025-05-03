using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the NpmRunScript tool
    /// </summary>
    public class NpmRunScriptTool : BaseToolImplementation
    {
        public NpmRunScriptTool(ILogger<NpmRunScriptTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the NpmRunScript tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef03",
                Name = "NpmRunScript",
                Description = "Runs an npm script from package.json",
                Schema = @"{
  ""name"": ""NpmRunScript"",
  ""description"": ""Runs an npm script from package.json."",
  ""input_schema"": {
                ""properties"": {
""scriptName"": {
                    ""title"": ""Script Name"",
                    ""type"": ""string"",
                    ""description"":""Name of the script to run (e.g., dev, build)""
},
 ""workingDirectory"": {
                    ""title"": ""Working Directory"",
                    ""type"": ""string"",
                    ""description"": ""Directory containing package.json""
},
""args"": {
                    ""title"": ""Arguments"",
                    ""type"": ""array"",
                    ""items"": {
                        ""type"": ""string""
                    },
                    ""description"": ""Additional arguments to pass to the script""
}
            },
           ""required"": [""scriptName"", ""workingDirectory""],
            ""title"": ""NpmRunScriptArguments"",
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
        /// Processes a NpmRunScript tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting NpmRunScript tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters with defaults
                var scriptName = parameters.ContainsKey("scriptName") ? parameters["scriptName"].ToString() : "";
                var workingDirectory = parameters.ContainsKey("workingDirectory") ? parameters["workingDirectory"].ToString() : "";
                var args = parameters.ContainsKey("args") ? 
                    JsonConvert.DeserializeObject<List<string>>(parameters["args"].ToString()) : 
                    new List<string>();

                if (string.IsNullOrEmpty(scriptName))
                {
                    return CreateResult(false, true, "Error: Script name is required.");
                }

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

                // Check if the script exists in package.json
                var packageJson = File.ReadAllText(packageJsonPath);
                var packageJsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(packageJson);
                if (!packageJsonObj.ContainsKey("scripts") || 
                    !((Newtonsoft.Json.Linq.JObject)packageJsonObj["scripts"]).ContainsKey(scriptName))
                {
                    SendStatusUpdate($"Error: Script '{scriptName}' not found in package.json.");
                    return CreateResult(false, true, $"Error: Script '{scriptName}' not found in package.json.");
                }

                // Build the npm run command
                string argsString = args.Any() ? " -- " + string.Join(" ", args) : "";
                string command = $"npm run {scriptName}{argsString}";

                SendStatusUpdate($"Running: {command} in {workingPath}...");

                // Execute npm run command using the helper
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
                    SendStatusUpdate($"Error running npm script: {error}");
                    return CreateResult(false, true, $"Error running npm script: {error}");
                }

                SendStatusUpdate($"Npm script '{scriptName}' executed successfully.");
                return CreateResult(true, true, $"Npm script '{scriptName}' executed successfully.\n\nOutput:\n{output}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing NpmRunScript tool");
                SendStatusUpdate($"Error processing NpmRunScript tool: {ex.Message}");
                return CreateResult(false, true, $"Error processing NpmRunScript tool: {ex.Message}");
            }
        }
    }
}