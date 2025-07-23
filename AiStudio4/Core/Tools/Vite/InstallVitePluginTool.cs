using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the InstallVitePlugin tool
    /// </summary>
    [McpServerToolType]
    public class InstallVitePluginTool : BaseToolImplementation
    {
        private readonly IDialogService _dialogService;

        public InstallVitePluginTool(ILogger<InstallVitePluginTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IDialogService dialogService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _dialogService = dialogService;
        }

        
        
        
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.INSTALL_VITE_PLUGIN_TOOL_GUID,
                Name = "InstallVitePlugin",
                Description = "Installs a Vite plugin and updates the configuration to use it",
                Schema = """
{
  "name": "InstallVitePlugin",
  "description": "Installs a Vite plugin and updates the configuration to use it.",
  "input_schema": {
    "properties": {
      "pluginName": {
        "title": "Plugin Name",
        "type": "string",
        "description": "Name of the Vite plugin to install"
      },
      "projectDirectory": {
        "title": "Project Directory",
        "type": "string",
        "description": "Directory containing the Vite project"
      }
    },
    "required": ["pluginName", "projectDirectory"],
    "title": "InstallVitePluginArguments",
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
                SendStatusUpdate("Starting InstallVitePlugin tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                
                var pluginName = parameters.ContainsKey("pluginName") ? parameters["pluginName"].ToString() : "";
                var projectDirectory = parameters.ContainsKey("projectDirectory") ? parameters["projectDirectory"].ToString() : "";

                if (string.IsNullOrEmpty(pluginName))
                {
                    return CreateResult(false, true, "Error: Plugin name is required.");
                }

                
                var projectPath = _projectRoot;
                if (!string.IsNullOrEmpty(projectDirectory) && projectDirectory != _projectRoot)
                {
                    projectPath = Path.GetFullPath(Path.Combine(_projectRoot, projectDirectory));
                    if (!projectPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        SendStatusUpdate("Error: Project directory is outside the allowed directory.");
                        return CreateResult(false, true, "Error: Project directory is outside the allowed directory.");
                    }
                }

                
                var packageJsonPath = Path.Combine(projectPath, "package.json");
                if (!File.Exists(packageJsonPath))
                {
                    SendStatusUpdate("Error: package.json not found in the specified directory.");
                    return CreateResult(false, true, "Error: package.json not found in the specified directory.");
                }

                
                string viteConfigPath = Path.Combine(projectPath, "vite.config.js");
                bool isTypeScript = false;
                if (!File.Exists(viteConfigPath))
                {
                    viteConfigPath = Path.Combine(projectPath, "vite.config.ts");
                    isTypeScript = true;
                    if (!File.Exists(viteConfigPath))
                    {
                        SendStatusUpdate("Error: Vite configuration file not found.");
                        return CreateResult(false, true, "Error: Vite configuration file not found.");
                    }
                }

                
                string confirmationPrompt = $"AI wants to:\n1. Install npm package '{pluginName}'.\n2. Modify the Vite configuration file '{Path.GetFileName(viteConfigPath)}' to include it.\nProceed with both actions?";
                string commandForDisplay = $"1. npm install {pluginName} --save-dev\n2. Modify: {Path.GetFileName(viteConfigPath)}";

                bool confirmed = await _dialogService.ShowConfirmationAsync("Confirm Plugin Installation and Config Update", confirmationPrompt, commandForDisplay);
                if (!confirmed)
                {
                    SendStatusUpdate($"Plugin installation and config update for '{pluginName}' cancelled by user.");
                    return CreateResult(true, false, "Operation cancelled by user.");
                }

                
                SendStatusUpdate($"Installing Vite plugin: {pluginName}...");
                string npmCommand = "npm";
                string arguments = $"install {pluginName} --save-dev";
                bool useCmd = true; 
                
                
                var result = await ViteCommandHelper.ExecuteCommandAsync(npmCommand, arguments, useCmd, projectPath, _logger);
                
                if (!result.Success)
                {
                    SendStatusUpdate($"Error installing Vite plugin: {result.Error}");
                    return CreateResult(false, true, $"Error installing Vite plugin: {result.Error}");
                }
                
                string output = result.Output;

                
                SendStatusUpdate("Updating Vite configuration to use the plugin...");
                string configContent = await File.ReadAllTextAsync(viteConfigPath);
                string originalContent = configContent;

                
                string pluginImportName = pluginName;
                if (pluginName.Contains("/"))
                {
                    pluginImportName = pluginName.Split('/').Last();
                }
                if (pluginImportName.StartsWith("vite-plugin-"))
                {
                    pluginImportName = pluginImportName.Substring("vite-plugin-".Length);
                }

                
                string importStatement = isTypeScript ?
                    $"import {pluginImportName} from '{pluginName}';\n" :
                    $"import {pluginImportName} from '{pluginName}'\n";

                
                if (Regex.IsMatch(configContent, @"import\s+.*?\s+from\s+['""].*?['""];?\s*\n"))
                {
                    
                    configContent = Regex.Replace(configContent,
                        @"((?:import\s+.*?\s+from\s+['""].*?['""];?\s*\n)+)",
                        $"$1{importStatement}");
                }
                else
                {
                    configContent = importStatement + configContent;
                }

                
                if (Regex.IsMatch(configContent, @"plugins\s*:\s*\[.*?\]", RegexOptions.Singleline))
                {
                    
                    configContent = Regex.Replace(configContent, 
                        @"(plugins\s*:\s*\[)(.*?)(\])", 
                        m => {
                            string current = m.Groups[2].Value;
                            return current.Trim().Length > 0 ?
                                $"{m.Groups[1].Value}{current}, {pluginImportName}(){m.Groups[3].Value}" :
                                $"{m.Groups[1].Value}{pluginImportName}(){m.Groups[3].Value}";
                        }, 
                        RegexOptions.Singleline);
                }
                else
                {
                    
                    configContent = Regex.Replace(configContent, 
                        @"(defineConfig\s*\(\s*\{)([^\}]*)(\}\s*\))", 
                        $"$1$2  plugins: [{pluginImportName}()],$3");
                }

                
                if (configContent != originalContent)
                {
                    await File.WriteAllTextAsync(viteConfigPath, configContent);
                    SendStatusUpdate("Vite plugin installed and configuration updated successfully.");
                    return CreateResult(true, true, $"Vite plugin '{pluginName}' installed and configuration updated successfully.\n\nOutput:\n{output}");
                }
                else
                {
                    SendStatusUpdate("Vite plugin installed but no changes were made to the configuration.");
                    return CreateResult(true, true, $"Vite plugin '{pluginName}' installed but no changes were made to the configuration.\n\nOutput:\n{output}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing InstallVitePlugin tool");
                SendStatusUpdate($"Error processing InstallVitePlugin tool: {ex.Message}");
                return CreateResult(false, true, $"Error processing InstallVitePlugin tool: {ex.Message}");
            }
        }

        [McpServerTool, Description("Installs a Vite plugin and updates the configuration to use it")]
        public async Task<string> InstallVitePlugin([Description("JSON parameters for InstallVitePlugin")] string parameters = "{}")
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
