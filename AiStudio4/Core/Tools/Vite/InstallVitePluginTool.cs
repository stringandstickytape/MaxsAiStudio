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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the InstallVitePlugin tool
    /// </summary>
    public class InstallVitePluginTool : BaseToolImplementation
    {
        public InstallVitePluginTool(ILogger<InstallVitePluginTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the InstallVitePlugin tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef09",
                Name = "InstallVitePlugin",
                Description = "Installs a Vite plugin and updates the configuration to use it",
                Schema = @"{
  ""name"": ""InstallVitePlugin"",
  ""description"": ""Installs a Vite plugin and updates the configuration to use it."",
  ""input_schema"": {
                ""properties"": {
""pluginName"": {
                    ""title"": ""Plugin Name"",
                    ""type"": ""string"",
                    ""description"":""Name of the Vite plugin to install""
},
 ""projectDirectory"": {
                    ""title"": ""Project Directory"",
                    ""type"": ""string"",
                    ""description"": ""Directory containing the Vite project""
}
            },
           ""required"": [""pluginName"", ""projectDirectory""],
            ""title"": ""InstallVitePluginArguments"",
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
        /// Processes an InstallVitePlugin tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting InstallVitePlugin tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters
                var pluginName = parameters.ContainsKey("pluginName") ? parameters["pluginName"].ToString() : "";
                var projectDirectory = parameters.ContainsKey("projectDirectory") ? parameters["projectDirectory"].ToString() : "";

                if (string.IsNullOrEmpty(pluginName))
                {
                    return CreateResult(false, true, "Error: Plugin name is required.");
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

                // Check if package.json exists
                var packageJsonPath = Path.Combine(projectPath, "package.json");
                if (!File.Exists(packageJsonPath))
                {
                    SendStatusUpdate("Error: package.json not found in the specified directory.");
                    return CreateResult(false, true, "Error: package.json not found in the specified directory.");
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

                // Install the plugin using the helper
                SendStatusUpdate($"Installing Vite plugin: {pluginName}...");
                string npmCommand = "npm";
                string arguments = $"install {pluginName} --save-dev";
                bool useCmd = true; // npm is a batch file and needs cmd.exe
                
                // Use the helper to run the command
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = useCmd ? "cmd.exe" : npmCommand,
                        Arguments = useCmd ? $"/c {npmCommand} {arguments}" : arguments,
                        WorkingDirectory = projectPath,
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
                    SendStatusUpdate($"Error installing Vite plugin: {error}");
                    return CreateResult(false, true, $"Error installing Vite plugin: {error}");
                }

                // Update the Vite config to use the plugin
                SendStatusUpdate("Updating Vite configuration to use the plugin...");
                string configContent = File.ReadAllText(viteConfigPath);
                string originalContent = configContent;

                // Extract plugin name without version or scope
                string pluginImportName = pluginName;
                if (pluginName.Contains("/"))
                {
                    pluginImportName = pluginName.Split('/').Last();
                }
                if (pluginImportName.StartsWith("vite-plugin-"))
                {
                    pluginImportName = pluginImportName.Substring("vite-plugin-".Length);
                }

                // Add import statement for the plugin
                string importStatement = isTypeScript ?
                    $"import {pluginImportName} from '{pluginName}';\n" :
                    $"import {pluginImportName} from '{pluginName}'\n";

                // Add import at the top of the file, after any existing imports
                if (Regex.IsMatch(configContent, @"import\s+.*?\s+from\s+['""].*?['""];?\s*\n"))
                {
                    // Match all consecutive import statements as a group
                    configContent = Regex.Replace(configContent,
                        @"((?:import\s+.*?\s+from\s+['""].*?['""];?\s*\n)+)",
                        $"$1{importStatement}");
                }
                else
                {
                    configContent = importStatement + configContent;
                }

                // Add the plugin to the plugins array
                if (Regex.IsMatch(configContent, @"plugins\s*:\s*\[.*?\]", RegexOptions.Singleline))
                {
                    // Add to existing plugins array
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
                    // Add new plugins array to the defineConfig object
                    configContent = Regex.Replace(configContent, 
                        @"(defineConfig\s*\(\s*\{)([^\}]*)(\}\s*\))", 
                        $"$1$2  plugins: [{pluginImportName}()],$3");
                }

                // Write the updated config back to the file
                if (configContent != originalContent)
                {
                    File.WriteAllText(viteConfigPath, configContent);
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
    }
}