

Think


Think: # Design for Vite Tool Category

Based on the DirectoryTreeTool.cs file, I'll design nine new tools for the Vite category. I'll follow the same structure and patterns used in the existing tool.

## Common Structure

All tools will:
1. Inherit from `BaseToolImplementation`
2. Have a constructor that takes `ILogger`, `IGeneralSettingsService`, and `IStatusMessageService`
3. Implement `GetToolDefinition()` to return a `Tool` object with appropriate schema
4. Implement `ProcessAsync()` to handle the tool execution
5. Include appropriate error handling
6. Use the project root as a base for path operations
7. Send status updates during processing

## Tool Designs

### 1. NpmCreateViteTool

```csharp
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
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                SendStatusUpdate($"Creating Vite project '{projectName}' with template '{template}'...");

                // Build the command
                string templateArg = template;
                if (typescript)
                {
                    templateArg += "-ts";
                }

                // Execute npm create vite command
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c npm create vite@latest {projectName} -- --template {templateArg}",
                        WorkingDirectory = targetPath,
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
                    SendStatusUpdate($"Error creating Vite project: {error}");
                    return CreateResult(false, true, $"Error creating Vite project: {error}");
                }

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
```

### 2. NpmInstallTool

```csharp
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

                // Execute npm install command
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
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
```

### 3. NpmRunScriptTool

```csharp
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

                // Execute npm run command
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
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
```

### 4. StartViteDevServerTool

```csharp
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
    /// Implementation of the StartViteDevServer tool
    /// </summary>
    public class StartViteDevServerTool : BaseToolImplementation
    {
        private static Process _runningDevServer;

        public StartViteDevServerTool(ILogger<StartViteDevServerTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the StartViteDevServer tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef04",
                Name = "StartViteDevServer",
                Description = "Starts the Vite development server",
                Schema = @"{
  ""name"": ""StartViteDevServer"",
  ""description"": ""Starts the Vite development server."",
  ""input_schema"": {
                ""properties"": {
""workingDirectory"": {
                    ""title"": ""Working Directory"",
                    ""type"": ""string"",
                    ""description"":""Directory containing the Vite project""
                },
 ""port"": {
                    ""title"": ""Port"",
                    ""type"": ""integer"",
                    ""description"": ""Custom port to run on (defaults to 5173)""
                },
""host"": {
                    ""title"": ""Host"",
                    ""type"": ""string"",
                    ""description"": ""Host to bind to (defaults to localhost)""
                }
            },
           ""required"": [""workingDirectory""],
            ""title"": ""StartViteDevServerArguments"",
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
        /// Processes a StartViteDevServer tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting StartViteDevServer tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters with defaults
                var workingDirectory = parameters.ContainsKey("workingDirectory") ? parameters["workingDirectory"].ToString() : "";
                var port = parameters.ContainsKey("port") ? Convert.ToInt32(parameters["port"]) : 5173;
                var host = parameters.ContainsKey("host") ? parameters["host"].ToString() : "localhost";

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

                // Kill any existing dev server process
                if (_runningDevServer != null && !_runningDevServer.HasExited)
                {
                    try
                    {
                        _runningDevServer.Kill(true);
                        _runningDevServer = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error stopping previous dev server");
                    }
                }

                SendStatusUpdate($"Starting Vite dev server in {workingPath} on {host}:{port}...");

                // Build the command with host and port options
                string command = $"npm run dev -- --port {port} --host {host}";

                // Execute the dev server command
                _runningDevServer = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        WorkingDirectory = workingPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                _runningDevServer.Start();

                // Read the first few lines of output to get the server URL
                string output = "";
                for (int i = 0; i < 20; i++)
                {
                    string line = await _runningDevServer.StandardOutput.ReadLineAsync();
                    if (line == null) break;
                    output += line + "\n";
                    if (line.Contains("Local:") || line.Contains("ready in"))
                    {
                        break;
                    }
                }

                // Start a background task to continue reading output
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!_runningDevServer.StandardOutput.EndOfStream)
                        {
                            string line = await _runningDevServer.StandardOutput.ReadLineAsync();
                            if (line != null)
                            {
                                _logger.LogInformation($"Vite server: {line}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading Vite server output");
                    }
                });

                // Start a background task to read error output
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (!_runningDevServer.StandardError.EndOfStream)
                        {
                            string line = await _runningDevServer.StandardError.ReadLineAsync();
                            if (line != null)
                            {
                                _logger.LogError($"Vite server error: {line}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading Vite server error output");
                    }
                });

                SendStatusUpdate("Vite dev server started successfully.");
                return CreateResult(true, true, $"Vite dev server started successfully on http://{host}:{port}\n\nInitial output:\n{output}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing StartViteDevServer tool");
                SendStatusUpdate($"Error processing StartViteDevServer tool: {ex.Message}");
                return CreateResult(false, true, $"Error processing StartViteDevServer tool: {ex.Message}");
            }
        }
    }
}
```

### 5. OpenBrowserTool

```csharp
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
    /// Implementation of the OpenBrowser tool
    /// </summary>
    public class OpenBrowserTool : BaseToolImplementation
    {
        public OpenBrowserTool(ILogger<OpenBrowserTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the OpenBrowser tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef05",
                Name = "OpenBrowser",
                Description = "Opens a URL in the default or specified web browser",
                Schema = @"{
  ""name"": ""OpenBrowser"",
  ""description"": ""Opens a URL in the default or specified web browser."",
  ""input_schema"": {
                ""properties"": {
""url"": {
                    ""title"": ""URL"",
                    ""type"": ""string"",
                    ""description"":""URL to open in the browser""
                },
 ""browser"": {
                    ""title"": ""Browser"",
                    ""type"": ""string"",
                    ""description"": ""Specific browser to use""
                }
            },
           ""required"": [""url""],
            ""title"": ""OpenBrowserArguments"",
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
        /// Processes an OpenBrowser tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting OpenBrowser tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters
                var url = parameters.ContainsKey("url") ? parameters["url"].ToString() : "";
                var browser = parameters.ContainsKey("browser") ? parameters["browser"].ToString() : "";

                if (string.IsNullOrEmpty(url))
                {
                    return Task.FromResult(CreateResult(false, true, "Error: URL is required."));
                }

                // Validate URL format
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || 
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    SendStatusUpdate("Error: Invalid URL format. URL must start with http:// or https://");
                    return Task.FromResult(CreateResult(false, true, "Error: Invalid URL format. URL must start with http:// or https://"));
                }

                SendStatusUpdate($"Opening URL: {url}");

                // Open the URL in the browser
                if (string.IsNullOrEmpty(browser))
                {
                    // Use default browser
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                else
                {
                    // Use specified browser
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = browser,
                        Arguments = url,
                        UseShellExecute = true
                    });
                }

                SendStatusUpdate("Browser opened successfully.");
                return Task.FromResult(CreateResult(true, true, $"URL '{url}' opened successfully in the browser."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OpenBrowser tool");
                SendStatusUpdate($"Error processing OpenBrowser tool: {ex.Message}");
                return Task.FromResult(CreateResult(false, true, $"Error processing OpenBrowser tool: {ex.Message}"));
            }
        }
    }
}
```

### 6. CheckNodeVersionTool

```csharp
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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return string.Empty;
                }

                return output;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
```

### 7. GetViteProjectInfoTool

```csharp
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the GetViteProjectInfo tool
    /// </summary>
    public class GetViteProjectInfoTool : BaseToolImplementation
    {
        public GetViteProjectInfoTool(ILogger<GetViteProjectInfoTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the GetViteProjectInfo tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "v1t3c4e5-f6a7-8901-2345-67890abcdef07",
                Name = "GetViteProjectInfo",
                Description = "Returns information about the Vite project",
                Schema = @"{
  ""name"": ""GetViteProjectInfo"",
  ""description"": ""Returns information about the Vite project, such as dependencies, scripts, and configuration."",
  ""input_schema"": {
                ""properties"": {
""projectDirectory"": {
                    ""title"": ""Project Directory"",
                    ""type"": ""string"",
                    ""description"":""Directory containing the Vite project""
                }
            },
           ""required"": [""projectDirectory""],
            ""title"": ""GetViteProjectInfoArguments"",
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
        /// Processes a GetViteProjectInfo tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting GetViteProjectInfo tool execution...");
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters
                var projectDirectory = parameters.ContainsKey("projectDirectory") ? parameters["projectDirectory"].ToString() : "";

                // Get the project directory path (relative to project root for security)
                var projectPath = _projectRoot;
                if (!string.IsNullOrEmpty(projectDirectory) && projectDirectory != _projectRoot)
                {
                    projectPath = Path.GetFullPath(Path.Combine(_projectRoot, projectDirectory));
                    if (!projectPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        SendStatusUpdate("Error: Project directory is outside the allowed directory.");
                        return Task.FromResult(CreateResult(false, true, "Error: Project directory is outside the allowed directory."));
                    }
                }

                // Check if package.json exists
                var packageJsonPath = Path.Combine(projectPath, "package.json");
                if (!File.Exists(packageJsonPath))
                {
                    SendStatusUpdate("Error: package.json not found in the specified directory.");
                    return Task.FromResult(CreateResult(false, true, "Error: package.json not found in the specified directory."));
                }

                // Read package.json
                var packageJson = File.ReadAllText(packageJsonPath);
                var packageJsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(packageJson);

                // Check for vite.config.js or vite.config.ts
                string viteConfigPath = Path.Combine(projectPath, "vite.config.js");
                if (!File.Exists(viteConfigPath))
                {
                    viteConfigPath = Path.Combine(projectPath, "vite.config.ts");
                }

                string viteConfig = File.Exists(viteConfigPath) ? File.ReadAllText(viteConfigPath) : "Vite config file not found";

                // Get project structure
                var projectStructure = new Dictionary<string, List<string>>
                {
                    { "src", Directory.Exists(Path.Combine(projectPath, "src")) ? 
                        Directory.GetFiles(Path.Combine(projectPath, "src"), "*.*", SearchOption.TopDirectoryOnly)
                            .Select(f => Path.GetFileName(f)).ToList() : 
                        new List<string>() },
                    { "public", Directory.Exists(Path.Combine(projectPath, "public")) ? 
                        Directory.GetFiles(Path.Combine(projectPath, "public"), "*.*", SearchOption.TopDirectoryOnly)
                            .Select(f => Path.GetFileName(f)).ToList() : 
                        new List<string>() }
                };

                // Build the result
                var result = new StringBuilder();
                result.AppendLine("# Vite Project Information\n");

                // Project name and version
                result.AppendLine($"## Project: {packageJsonObj.ContainsKey("name") ? packageJsonObj["name"] : "Unknown"}")
                      .AppendLine($"Version: {packageJsonObj.ContainsKey("version") ? packageJsonObj["version"] : "Unknown"}\n");

                // Scripts
                result.AppendLine("## Scripts")
                      .AppendLine("```json")
                      .AppendLine(JsonConvert.SerializeObject(packageJsonObj.ContainsKey("scripts") ? packageJsonObj["scripts"] : new object(), Formatting.Indented))
                      .AppendLine("```\n");

                // Dependencies
                result.AppendLine("## Dependencies")
                      .AppendLine("```json")
                      .AppendLine(JsonConvert.SerializeObject(packageJsonObj.ContainsKey("dependencies") ? packageJsonObj["dependencies"] : new object(), Formatting.Indented))
                      .AppendLine("```\n");

                // Dev Dependencies
                result.AppendLine("## Dev Dependencies")
                      .AppendLine("```json")
                      .AppendLine(JsonConvert.SerializeObject(packageJsonObj.ContainsKey("devDependencies") ? packageJsonObj["devDependencies"] : new object(), Formatting.Indented))
                      .AppendLine("```\n");

                // Vite Config
                result.AppendLine("## Vite Configuration")
                      .AppendLine("```javascript")
                      .AppendLine(viteConfig)
                      .AppendLine("```\n");

                // Project Structure
                result.AppendLine("## Project Structure")
                      .AppendLine("```")
                      .AppendLine($"src/: {string.Join(", ", projectStructure["src"])}")
                      .AppendLine($"public/: {string.Join(", ", projectStructure["public"])}")
                      .AppendLine("```");

                SendStatusUpdate("Vite project information retrieved successfully.");
                return Task.FromResult(CreateResult(true, true, result.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GetViteProjectInfo tool");
                SendStatusUpdate($"Error processing GetViteProjectInfo tool: {ex.Message}");
                return Task.FromResult(CreateResult(false, true, $"Error processing GetViteProjectInfo tool: {ex.Message}"));
            }
        }
    }
}
```

### 8. ModifyViteConfigTool

```csharp
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
        public ModifyViteConfigTool(ILogger<ModifyViteConfigTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
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
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
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
                    return Task.FromResult(CreateResult(false, true, "Error: No configuration changes specified."));
                }

                // Get the project directory path (relative to project root for security)
                var projectPath = _projectRoot;
                if (!string.IsNullOrEmpty(projectDirectory) && projectDirectory != _projectRoot)
                {
                    projectPath = Path.GetFullPath(Path.Combine(_projectRoot, projectDirectory));
                    if (!projectPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        SendStatusUpdate("Error: Project directory is outside the allowed directory.");
                        return Task.FromResult(CreateResult(false, true, "Error: Project directory is outside the allowed directory."));
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
                        return Task.FromResult(CreateResult(false, true, "Error: Vite configuration file not found."));
                    }
                }

                // Read the current config file
                string configContent = File.ReadAllText(viteConfigPath);
                string originalContent = configContent;

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
                    File.WriteAllText(viteConfigPath, configContent);
                    SendStatusUpdate("Vite configuration updated successfully.");
                    return Task.FromResult(CreateResult(true, true, "Vite configuration updated successfully."));
                }
                else
                {
                    SendStatusUpdate("No changes were made to the Vite configuration.");
                    return Task.FromResult(CreateResult(true, true, "No changes were made to the Vite configuration."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ModifyViteConfig tool");
                SendStatusUpdate($"Error processing ModifyViteConfig tool: {ex.Message}");
                return Task.FromResult(CreateResult(false, true, $"Error processing ModifyViteConfig tool: {ex.Message}"));
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
```

### 9. InstallVitePluginTool

```csharp
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

                // Install the plugin
                SendStatusUpdate($"Installing Vite plugin: {pluginName}...");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c npm install {pluginName} --save-dev",
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
                if (Regex.IsMatch(configContent, @"import\s+.*?\s+from\s+['"].*?['"]\s*;?\s*\n"))
                {
                    configContent = Regex.Replace(configContent, 
                        @"(import\s+.*?\s+from\s+['"].*?['"]\s*;?\s*\n)(.*)", 
                        $"$1{importStatement}$2");
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
```

## Summary

1. **NpmCreateViteTool** - Creates a new Vite project with specified template and configuration
2. **NpmInstallTool** - Installs npm dependencies in a specified directory
3. **NpmRunScriptTool** - Runs npm scripts from package.json
4. **StartViteDevServerTool** - Starts the Vite development server
5. **OpenBrowserTool** - Opens a URL in the default or specified web browser
6. **CheckNodeVersionTool** - Checks if Node.js and npm are installed and returns their versions
7. **GetViteProjectInfoTool** - Returns information about a Vite project
8. **ModifyViteConfigTool** - Modifies the Vite configuration file
9. **InstallVitePluginTool** - Installs a Vite plugin and updates the configuration

All tools follow the same structure as the DirectoryTreeTool, with proper error handling, security checks for paths, and status updates during processing. They all inherit from BaseToolImplementation and implement the required methods.


