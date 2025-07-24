using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the ReadFile tool
    /// </summary>
    [McpServerToolType]
    public class ReadFilesTool : BaseToolImplementation
    {
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();

        public ReadFilesTool(ILogger<ReadFilesTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the ReadFile tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.READ_FILES_TOOL_GUID,
                ExtraProperties = new Dictionary<string, string> {
                    { "excludedFileExtensions (CSV)", "" }, //".cs" }
                },
                Description = "Read the contents of one or multiple files.",
                Name="ReadFiles",
                Schema = """
{
  "name": "ReadFiles",
  "description": "Read the contents of one or multiple files.  Can read a single file or multiple files simultaneously. When reading multiple files, each file's content is returned with its path as a reference. Failed reads for individual files won't stop the entire operation. Only works within allowed directories.  YOU MUST NEVER fetch the same file twice in succession.",
  "input_schema": {
    "properties": {
      "paths": {
        "anyOf": [
          { "items": { "type": "string" }, "type": "array" },
          { "type": "string" }
        ],
        "description": "absolute path to the file or files to read"
      }
    },
    "required": ["paths"],
    "type": "object"
  }
}
""",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a ReadFile tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _extraProperties = extraProperties;
            _logger.LogInformation("ReadFile tool called");
            SendStatusUpdate("Starting ReadFiles tool execution...");
            var resultBuilder = new StringBuilder();

            // If user-edited extraProperties are provided, override defaults for excluded extensions
            var toolDef = GetToolDefinition();


            var excludedExtensionsCsv = _extraProperties.TryGetValue("ExcludedFileExtensions (CSV)", out var extCsv) ? extCsv :
                _extraProperties.TryGetValue("excludedFileExtensions (CSV)", out var extCsv2) ? extCsv2 : 
                "";
            var excludedExtensions = excludedExtensionsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant()).Where(e => e.StartsWith(".")).ToList();

            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                var pathsObject = parameters["paths"];
                List<string> pathsToRead = new List<string>();

                if (pathsObject is string singlePath)
                {
                    if (singlePath.StartsWith("[")) singlePath = singlePath.Substring(1);
                    if (singlePath.StartsWith("\"")) singlePath = singlePath.Substring(1);
                    if (singlePath.EndsWith("]")) singlePath = singlePath.Substring(0, singlePath.Length - 2);
                    singlePath = singlePath.Replace("\\\\", "\\");
                    pathsToRead.Add(singlePath);
                }
                else if (pathsObject is JArray pathArray)
                {
                    pathsToRead.AddRange(pathArray.Select(p => (string)p));
                }
                else
                {
                    throw new ArgumentException("Invalid format for 'paths' parameter. Expected string or array of strings.");
                }

                foreach (var relativePathL in pathsToRead)
                {
                    var relativePath = relativePathL;
                    //if (relativePath.EndsWith("cs"))
                    //    continue;

                    if (relativePath.StartsWith("\"")) relativePath = relativePath.Substring(1);
                    if (relativePath.EndsWith("\"")) relativePath = relativePath.Substring(0, relativePath.Length - 2);

                    // Security check: Ensure the path is within the project root
                    var fullPath = Path.GetFullPath(Path.Combine(_projectRoot, relativePath));

                    // Skip files with excluded extensions
                    var fileExt = Path.GetExtension(fullPath).ToLowerInvariant();
                    if (excludedExtensions.Contains(fileExt))
                    {
                        SendStatusUpdate($"Skipping file with excluded extension: {Path.GetFileName(relativePath)}");
                        resultBuilder.AppendLine($"---Skipped {relativePath}: Excluded file extension '{fileExt}'.---");
                        continue;
                    }

                    if (!fullPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning($"Attempted to read file outside the project root: {relativePath} (Resolved: {fullPath})");
                        SendStatusUpdate($"Error: Path is outside the allowed directory: {Path.GetFileName(relativePath)}");
                        resultBuilder.AppendLine($"---Error reading {relativePath}: Access denied - Path is outside the allowed directory.---");
                        continue; // Skip this file
                    }

                    try
                    {
                        if (File.Exists(fullPath))
                        {
                            SendStatusUpdate($"Reading file: {Path.GetFileName(fullPath)}");
                            var content = await File.ReadAllTextAsync(fullPath);
                            resultBuilder.AppendLine($"--- File: {relativePath} ---");
                            resultBuilder.AppendLine(content);
                        }
                        else
                        {
                            SendStatusUpdate($"Error: File not found: {Path.GetFileName(fullPath)}");
                            resultBuilder.AppendLine($"---Error reading {relativePath}: File not found. Did you get the directory wrong?");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error reading file: {fullPath}");
                        SendStatusUpdate($"Error reading file: {Path.GetFileName(fullPath)}");
                        resultBuilder.AppendLine($"---Error reading {relativePath}: {ex.Message}---");
                    }
                    resultBuilder.AppendLine(); // Add a separator between files
                }

                SendStatusUpdate("ReadFiles tool completed successfully.");
                return CreateResult(true, true, resultBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ReadFile tool");
                SendStatusUpdate($"Error processing ReadFiles tool: {ex.Message}");
                return CreateResult(true, true, $"Error processing ReadFile tool: {ex.Message}");
            }
        }

        [McpServerTool, Description("Read the contents of one or multiple files.")]
        public async Task<string> ReadFiles([Description("JSON parameters for ReadFiles")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
