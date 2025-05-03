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
    /// Implementation of the NpmCreateVite tool
    /// </summary>
    public class NpmCreateViteTool : BaseToolImplementation
    {
        public NpmCreateViteTool(ILogger<NpmCreateViteTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the NpmCreateVite tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef01",
                Name = "NpmCreateVite",
                Description = "Creates a new Vite project",
                Schema = @"{
  ""name"": ""NpmCreateVite"",
  ""description"": ""Creates a new Vite project with the specified configuration."",
  ""input_schema"": {
                ""properties"": {
""projectName"": {
                    ""title"": ""Project Name"",
                    ""type"": ""string"",
                    ""description"":""Name of the project to create""
},
 ""template"": {
                    ""default"": ""react"",
                    ""title"": ""Template"",
                    ""type"": ""string"",
                    ""description"": ""Template to use (e.g., react, vue, vanilla)""
},
""typescript"": {
                    ""default"": false,
                    ""title"": ""TypeScript"",
                    ""type"": ""boolean"",
                    ""description"": ""Whether to use TypeScript""
},
""targetDirectory"": {
                    ""title"": ""Target Directory"",
                    ""type"": ""string"",
                    ""description"": ""Directory where the project should be created""
}
            },
           ""required"": [""projectName"", ""targetDirectory""],
            ""title"": ""NpmCreateViteArguments"",
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
        /// Processes a NpmCreateVite tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting NpmCreateVite tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters with defaults
                var projectName = parameters.ContainsKey("projectName") ? parameters["projectName"].ToString() : "";
                var template = parameters.ContainsKey("template") ? parameters["template"].ToString() : "react";
                var typescript = parameters.ContainsKey("typescript") ? Convert.ToBoolean(parameters["typescript"]) : false;
                var targetDirectory = parameters.ContainsKey("targetDirectory") ? parameters["targetDirectory"].ToString() : "";

                if (string.IsNullOrEmpty(projectName))
                {
                    return CreateResult(false, true, "Error: Project name is required.");
                }

                // Get the target path (relative to project root for security)
                var targetPath = _projectRoot;
                if (!string.IsNullOrEmpty(targetDirectory) && targetDirectory != _projectRoot)
                {
                    targetPath = Path.GetFullPath(Path.Combine(_projectRoot, targetDirectory));
                    if (!targetPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        SendStatusUpdate("Error: Target directory is outside the allowed directory.");
                        return CreateResult(false, true, "Error: Target directory is outside the allowed directory.");
                    }
                }

                // Ensure the target directory exists
                //if (!Directory.Exists(targetPath))
                //{
                //    Directory.CreateDirectory(targetPath);
                //}

                SendStatusUpdate($"Creating Vite project '{projectName}' with template '{template}'...");

                // Build the command
                string templateArg = template;
                if (typescript)
                {
                    templateArg += "-ts";
                }

                // Execute npm create vite command using the helper
                string command = "npm";
                string arguments = $"create vite@latest {projectName} -- --template {templateArg}";
                bool useCmd = true; // npm is a batch file and needs cmd.exe
                
                // Use the enhanced helper to execute the command
                var result = await ViteCommandHelper.ExecuteCommandAsync(command, arguments, useCmd, _projectRoot, _logger);
                
                if (!result.Success)
                {
                    SendStatusUpdate($"Error creating Vite project: {result.Error}");
                    return CreateResult(false, true, $"Error creating Vite project: {result.Error}");
                }
                
                string output = result.Output;

                SendStatusUpdate("Vite project created successfully.");
                return CreateResult(true, true, $"Vite project '{projectName}' created successfully with template '{templateArg}'\n\nOutput:\n{output}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing NpmCreateVite tool");
                SendStatusUpdate($"Error processing NpmCreateVite tool: {ex.Message}");
                return CreateResult(false, true, $"Error processing NpmCreateVite tool: {ex.Message}");
            }
        }
    }
}