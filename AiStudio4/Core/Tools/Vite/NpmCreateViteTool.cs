// AiStudio4/Core/Tools/Vite/NpmCreateViteTool.cs
﻿using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;










namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the NpmCreateVite tool
    /// </summary>
    [McpServerToolType]
    public class NpmCreateViteTool : BaseToolImplementation
    {
        private readonly IDialogService _dialogService;

        public NpmCreateViteTool(ILogger<NpmCreateViteTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IDialogService dialogService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _dialogService = dialogService;
        }

        /// <summary>
        /// Gets the NpmCreateVite tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.NPM_CREATE_VITE_TOOL_GUID,
                Name = "NpmCreateVite",
                Description = "Creates a new Vite project",
                Schema = """
{
  "name": "NpmCreateVite",
  "description": "Creates a new Vite project with the specified configuration.",
  "input_schema": {
    "properties": {
      "projectName": {
        "title": "Project Name",
        "type": "string",
        "description": "Name of the project to create"
      },
      "template": {
        "default": "react",
        "title": "Template",
        "type": "string",
        "description": "Template to use (e.g., react, vue, vanilla)"
      },
      "typescript": {
        "default": false,
        "title": "TypeScript",
        "type": "boolean",
        "description": "Whether to use TypeScript"
      },
      "targetDirectory": {
        "title": "Target Directory",
        "type": "string",
        "description": "Directory where the project should be created"
      }
    },
    "required": ["projectName", "targetDirectory"],
    "title": "NpmCreateViteArguments",
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

                // Confirmation Dialog
                string confirmationPrompt = $"AI wants to create a new Vite project named '{projectName}' in directory '{targetPath}'. This will create new files and folders. Proceed?";
                string commandForDisplay = $"npm create vite@latest {projectName} -- --template {(typescript ? template + "-ts" : template)}";

                bool confirmed = await _dialogService.ShowConfirmationAsync("Confirm Project Creation", confirmationPrompt, commandForDisplay);
                if (!confirmed)
                {
                    SendStatusUpdate($"Vite project creation in {targetPath} cancelled by user.");
                    return CreateResult(true, false, "Operation cancelled by user.");
                }

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

        [McpServerTool, Description("Creates a new Vite project")]
        public async Task<string> NpmCreateVite([Description("JSON parameters for NpmCreateVite")] string parameters = "{}")
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
