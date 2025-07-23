using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools.Vite
{
    /// <summary>
    /// Implementation of the GetViteProjectInfo tool
    /// </summary>
    [McpServerToolType]
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
                Guid = ToolGuids.GET_VITE_PROJECT_INFO_TOOL_GUID,
                Name = "GetViteProjectInfo",
                Description = "Returns information about the Vite project",
                Schema = """
{
  "name": "GetViteProjectInfo",
  "description": "Returns information about the Vite project, such as dependencies, scripts, and configuration.",
  "input_schema": {
    "properties": {
      "projectDirectory": {
        "title": "Project Directory",
        "type": "string",
        "description": "Directory containing the Vite project"
      }
    },
    "required": ["projectDirectory"],
    "title": "GetViteProjectInfoArguments",
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
                result.AppendLine($"## Project: {(packageJsonObj.ContainsKey("name") ? packageJsonObj["name"] : "Unknown")}")
                      .AppendLine($"Version: {(packageJsonObj.ContainsKey("version") ? packageJsonObj["version"] : "Unknown")}\n");

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

        [McpServerTool, Description("Returns information about the Vite project")]
        public async Task<string> GetViteProjectInfo([Description("JSON parameters for GetViteProjectInfo")] string parameters = "{}")
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
