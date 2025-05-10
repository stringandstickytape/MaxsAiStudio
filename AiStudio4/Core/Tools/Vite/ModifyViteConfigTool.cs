// AiStudio4/Core/Tools/Vite/ModifyViteConfigTool.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the ModifyViteConfig tool
    /// </summary>
    public class ModifyViteConfigTool : BaseToolImplementation
    {
        private readonly IDialogService _dialogService;

        public ModifyViteConfigTool(ILogger<ModifyViteConfigTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IDialogService dialogService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _dialogService = dialogService;
        }

        /// <summary>
        /// Gets the ModifyViteConfig tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef08",
                Name = "ModifyViteConfig",
                Description = "Modifies the Vite configuration file",
                Schema = @"{
  ""name"": ""ModifyViteConfig"",
  ""description"": ""Modifies the Vite configuration file with the specified changes."",
  ""input_schema"": {
                ""properties"": {
""projectDirectory"": {
                    ""title"": ""Project Directory"",
                    ""type"": ""string"",
                    ""description"":""Directory containing the Vite project""
},
 ""configChanges"": {
                    ""title"": ""Configuration Changes"",
                    ""type"": ""object"",
                    ""description"": ""Configuration changes to apply""
}
            },
           ""required"": [""projectDirectory"", ""configChanges""],
            ""title"": ""ModifyViteConfigArguments"",
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
        /// Processes a ModifyViteConfig tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting ModifyViteConfig tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters
                var projectDirectory = parameters.ContainsKey("projectDirectory") ? parameters["projectDirectory"].ToString() : "";
                var configChanges = parameters.ContainsKey("configChanges") ? 
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters["configChanges"].ToString()) : 
                    new Dictionary<string, object>();

                if (configChanges.Count == 0)
                {
                    return CreateResult(false, true, "Error: No configuration changes specified.");
                }

                // Get the project directory path (relative to project root for security)
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

                // Find vite.config.js or vite.config.ts
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

                // Read the current config file
                string configContent = await File.ReadAllTextAsync(viteConfigPath);
                string originalContent = configContent;

                // Confirmation Dialog
                string changesSummary = string.Join(", ", configChanges.Keys);
                string confirmationPrompt = $"AI wants to modify the Vite configuration file: '{Path.GetFileName(viteConfigPath)}'. This will alter project settings. Proceed?";
                string commandForDisplay = $"Modify: {viteConfigPath}\nChanges involve: [{changesSummary}]";

                bool confirmed = await _dialogService.ShowConfirmationAsync("Confirm Vite Config Modification", confirmationPrompt, commandForDisplay);
                if (!confirmed)
                {
                    SendStatusUpdate($"Vite config modification for {Path.GetFileName(viteConfigPath)} cancelled by user.");
                    return CreateResult(true, false, "Operation cancelled by user.");
                }

                // Apply changes to the config file
                foreach (var change in configChanges)
                {
                    string key = change.Key;
                    string value = JsonConvert.SerializeObject(change.Value);

                    // Handle special case for plugins array
                    if (key == "plugins")
                    {
                        configContent = ModifyPluginsArray(configContent, value);
                        continue;
                    }

                    // For other properties, try to find and replace the property
                    string pattern = $@"({key}\s*:\s*)(.*?)(,|\n|\r|\}})";
                    if (Regex.IsMatch(configContent, pattern, RegexOptions.Singleline))
                    {
                        // Property exists, update it
                        configContent = Regex.Replace(configContent, pattern, m => 
                        {
                            return $"{m.Groups[1].Value}{value}{m.Groups[3].Value}";
                        }, RegexOptions.Singleline);
                    }
                    else
                    {
                        // Property doesn't exist, add it to the defineConfig object
                        configContent = Regex.Replace(configContent, 
                            @"(defineConfig\s*\(\s*\{)([^\}]*)(\}\s*\))", 
                            $"$1$2  {key}: {value},$3");
                    }
                }

                // Write the updated config back to the file
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

        /// <summary>
        /// Helper method to modify the plugins array in the Vite config
        /// </summary>
        private string ModifyPluginsArray(string configContent, string pluginsValue)
        {
            // Check if plugins array exists
            if (Regex.IsMatch(configContent, @"plugins\s*:\s*\[.*?\]", RegexOptions.Singleline))
            {
                // Replace the existing plugins array
                return Regex.Replace(configContent, 
                    @"(plugins\s*:\s*)\[.*?\](,|\n|\r|\}})", 
                    $"$1{pluginsValue}$2", 
                    RegexOptions.Singleline);
            }
            else
            {
                // Add plugins array to the defineConfig object
                return Regex.Replace(configContent, 
                    @"(defineConfig\s*\(\s*\{)([^\}]*)(\}\s*\))", 
                    $"$1$2  plugins: {pluginsValue},$3");
            }
        }
    }
}