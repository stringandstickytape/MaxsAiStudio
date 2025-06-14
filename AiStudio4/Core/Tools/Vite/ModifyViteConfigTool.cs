







using System.Text.RegularExpressions;


namespace AiStudio4.Core.Tools.Vite
{
    
    
    
    public class ModifyViteConfigTool : BaseToolImplementation
    {
        private readonly IDialogService _dialogService;

        public ModifyViteConfigTool(ILogger<ModifyViteConfigTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IDialogService dialogService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _dialogService = dialogService;
        }

        
        
        
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.MODIFY_VITE_CONFIG_TOOL_GUID,
                Name = "ModifyViteConfig",
                Description = "Modifies the Vite configuration file",
                Schema = """
{
  "name": "ModifyViteConfig",
  "description": "Modifies the Vite configuration file with the specified changes.",
  "input_schema": {
    "properties": {
      "projectDirectory": {
        "title": "Project Directory",
        "type": "string",
        "description": "Directory containing the Vite project"
      },
      "configChanges": {
        "title": "Configuration Changes",
        "type": "object",
        "description": "Configuration changes to apply"
      }
    },
    "required": ["projectDirectory", "configChanges"],
    "title": "ModifyViteConfigArguments",
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
                SendStatusUpdate("Starting ModifyViteConfig tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                
                var projectDirectory = parameters.ContainsKey("projectDirectory") ? parameters["projectDirectory"].ToString() : "";
                var configChanges = parameters.ContainsKey("configChanges") ? 
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters["configChanges"].ToString()) : 
                    new Dictionary<string, object>();

                if (configChanges.Count == 0)
                {
                    return CreateResult(false, true, "Error: No configuration changes specified.");
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

                
                string configContent = await File.ReadAllTextAsync(viteConfigPath);
                string originalContent = configContent;

                
                string changesSummary = string.Join(", ", configChanges.Keys);
                string confirmationPrompt = $"AI wants to modify the Vite configuration file: '{Path.GetFileName(viteConfigPath)}'. This will alter project settings. Proceed?";
                string commandForDisplay = $"Modify: {viteConfigPath}\nChanges involve: [{changesSummary}]";

                bool confirmed = await _dialogService.ShowConfirmationAsync("Confirm Vite Config Modification", confirmationPrompt, commandForDisplay);
                if (!confirmed)
                {
                    SendStatusUpdate($"Vite config modification for {Path.GetFileName(viteConfigPath)} cancelled by user.");
                    return CreateResult(true, false, "Operation cancelled by user.");
                }

                
                foreach (var change in configChanges)
                {
                    string key = change.Key;
                    string value = JsonConvert.SerializeObject(change.Value);

                    
                    if (key == "plugins")
                    {
                        configContent = ModifyPluginsArray(configContent, value);
                        continue;
                    }

                    
                    string pattern = $@"({key}\s*:\s*)(.*?)(,|\n|\r|\}})";
                    if (Regex.IsMatch(configContent, pattern, RegexOptions.Singleline))
                    {
                        
                        configContent = Regex.Replace(configContent, pattern, m => 
                        {
                            return $"{m.Groups[1].Value}{value}{m.Groups[3].Value}";
                        }, RegexOptions.Singleline);
                    }
                    else
                    {
                        
                        configContent = Regex.Replace(configContent, 
                            @"(defineConfig\s*\(\s*\{)([^\}]*)(\}\s*\))", 
                            $"$1$2  {key}: {value},$3");
                    }
                }

                
                if (configContent != originalContent)
                {
                    await File.WriteAllTextAsync(viteConfigPath, configContent);
                    SendStatusUpdate("Vite configuration updated successfully.");
                    return CreateResult(true, true, "Vite configuration updated successfully.");
                }
                else
                {
                    SendStatusUpdate("No changes were made to the Vite configuration.");
                    return CreateResult(true, true, "No changes were made to the Vite configuration.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ModifyViteConfig tool");
                SendStatusUpdate($"Error processing ModifyViteConfig tool: {ex.Message}");
                return CreateResult(false, true, $"Error processing ModifyViteConfig tool: {ex.Message}");
            }
        }

        
        
        
        private string ModifyPluginsArray(string configContent, string pluginsValue)
        {
            
            if (Regex.IsMatch(configContent, @"plugins\s*:\s*\[.*?\]", RegexOptions.Singleline))
            {
                
                return Regex.Replace(configContent, 
                    @"(plugins\s*:\s*)\[.*?\](,|\n|\r|\}})", 
                    $"$1{pluginsValue}$2", 
                    RegexOptions.Singleline);
            }
            else
            {
                
                return Regex.Replace(configContent, 
                    @"(defineConfig\s*\(\s*\{)([^\}]*)(\}\s*\))", 
                    $"$1$2  plugins: {pluginsValue},$3");
            }
        }
    }
}
