// AiStudio4/Core/Tools/Vite/NpmInstallTool.cs
﻿










namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the NpmInstall tool
    /// </summary>
    public class NpmInstallTool : BaseToolImplementation
    {
        private readonly IDialogService _dialogService;

        public NpmInstallTool(ILogger<NpmInstallTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IDialogService dialogService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _dialogService = dialogService;
        }

        /// <summary>
        /// Gets the NpmInstall tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.NPM_INSTALL_TOOL_GUID,
                Name = "NpmInstall",
                Description = "Installs npm dependencies",
                Schema = """
{
  "name": "NpmInstall",
  "description": "Installs npm dependencies in the specified directory.",
  "input_schema": {
    "properties": {
      "workingDirectory": { "title": "Working Directory", "type": "string", "description": "Directory containing package.json" },
      "packageName": { "title": "Package Name", "type": "string", "description": "Specific package to install (if not provided, installs all dependencies)" },
      "isDev": { "default": false, "title": "Is Dev Dependency", "type": "boolean", "description": "Whether to install as a dev dependency" },
      "version": { "title": "Version", "type": "string", "description": "Specific version to install" }
    },
    "required": ["workingDirectory"],
    "title": "NpmInstallArguments",
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
                string command = "install";
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

                // Confirmation Dialog
                string commandArguments = command; // command variable already holds the arguments for pnpm install
                string confirmationPrompt = $"AI wants to run 'pnpm install {commandArguments}' in directory '{workingPath}'. This will install/update packages. Proceed?";
                string commandForDisplay = $"pnpm install {commandArguments}";

                bool confirmed = await _dialogService.ShowConfirmationAsync("Confirm NPM Install", confirmationPrompt, commandForDisplay);
                if (!confirmed)
                {
                    SendStatusUpdate($"npm install in {workingPath} cancelled by user.");
                    return CreateResult(true, false, "Operation cancelled by user.");
                }

                SendStatusUpdate($"Running: pnpm {command} in {workingPath}...");

                // Execute npm install command using the helper
                string npmCommand = "pnpm";
                bool useCmd = true; // npm is a batch file and needs cmd.exe
                
                // Use the enhanced helper to execute the command
                var result = await ViteCommandHelper.ExecuteCommandAsync(npmCommand, command, useCmd, workingPath, _logger);
                
                if (!result.Success)
                {
                    SendStatusUpdate($"Error installing npm packages: {result.Error}");
                    return CreateResult(false, true, $"Error installing npm packages: {result.Error}");
                }
                
                string output = result.Output;

                SendStatusUpdate("Npm packages installed successfully.");
                return CreateResult(true, true, $"All specified npm packages were installed successfully for command `pnpm {command}`");
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
