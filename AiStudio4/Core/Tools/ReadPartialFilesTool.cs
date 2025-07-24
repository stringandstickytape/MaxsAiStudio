// AiStudio4\Core\Tools\ReadPartialFilesTool.cs
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the ReadPartialFiles tool
    /// </summary>
    [McpServerToolType]
    public class ReadPartialFilesTool : BaseToolImplementation
    {
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();
        private const int MaxLineCount = 500;
        private const int MaxCharacterLength = 50000; // Maximum characters to read in one request

        public ReadPartialFilesTool(ILogger<ReadPartialFilesTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the ReadPartialFiles tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.READ_PARTIAL_FILES_TOOL_GUID,
                Name = "ReadPartialFiles",
                ExtraProperties = new Dictionary<string, string> {
                    { "excludedFileExtensions (CSV)", "" },
                },
                Description = "Read specified line ranges or character ranges from one or multiple files. Each file request must specify either line-based parameters (start_line, line_count) or character-based parameters (start_character, length).",
                Schema = """
{
  "name": "ReadPartialFiles",
  "description": "Read specified line ranges or character ranges from one or multiple files. Each file request must specify either line-based parameters (start_line, line_count) or character-based parameters (start_character, length). Returns the specified content for each file. Failed reads for individual files won't stop the entire operation. Only works within allowed directories.",
  "input_schema": {
    "properties": {
      "requests": {
        "type": "array",
        "items": {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "absolute path to the file to read" },
            "start_line": { "type": "integer", "minimum": 1, "description": "1-based line number to start reading from (for line-based reading)" },
            "line_count": { "type": "integer", "minimum": 1, "maximum": 500, "description": "number of lines to read (max 500, for line-based reading)" },
            "start_character": { "type": "integer", "minimum": 0, "description": "0-based character position to start reading from (for character-based reading)" },
            "length": { "type": "integer", "minimum": 1, "description": "number of characters to read (for character-based reading)" }
          },
          "required": ["path"]
        },
        "description": "List of file read requests. Each request must specify either (start_line, line_count) for line-based reading or (start_character, length) for character-based reading."
      }
    },
    "required": ["requests"],
    "type": "object"
  }
}
""",
                Categories = new List<string> { "MaxCode-Alt" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a ReadPartialFiles tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _extraProperties = extraProperties;
            _logger.LogInformation("ReadPartialFiles tool called");
            SendStatusUpdate("Starting ReadPartialFiles tool execution...");
            var resultBuilder = new StringBuilder();

            var excludedExtensionsCsv = _extraProperties.TryGetValue("ExcludedFileExtensions (CSV)", out var extCsv) ? extCsv :
                _extraProperties.TryGetValue("excludedFileExtensions (CSV)", out var extCsv2) ? extCsv2 :
                "";
            var excludedExtensions = excludedExtensionsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant()).Where(e => e.StartsWith(".")).ToList();

            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);
                if (!parameters.TryGetValue("requests", out var requestsObj) || !(requestsObj is JArray requestsArray))
                {
                    throw new ArgumentException("Missing or invalid 'requests' parameter.");
                }

                foreach (var reqToken in requestsArray)
                {
                    var req = reqToken as JObject;
                    if (req == null)
                        continue;
                    var path = req.Value<string>("path");
                    
                    // Check if this is line-based or character-based reading
                    var hasLineParams = req.ContainsKey("start_line") && req.ContainsKey("line_count");
                    var hasCharParams = req.ContainsKey("start_character") && req.ContainsKey("length");
                    
                    if (!hasLineParams && !hasCharParams)
                    {
                        resultBuilder.AppendLine($"---Error: Must specify either (start_line, line_count) or (start_character, length) for {path}---");
                        continue;
                    }
                    if (hasLineParams && hasCharParams)
                    {
                        resultBuilder.AppendLine($"---Error: Cannot specify both line-based and character-based parameters for {path}---");
                        continue;
                    }
                    
                    if (hasLineParams)
                    {
                        var startLine = req.Value<int>("start_line");
                        var lineCount = req.Value<int>("line_count");
                        
                        if (lineCount > MaxLineCount)
                        {
                            resultBuilder.AppendLine($"---Error: Requested line_count {lineCount} exceeds maximum of {MaxLineCount} for {path}---");
                            continue;
                        }
                        if (startLine < 1 || lineCount < 1)
                        {
                            resultBuilder.AppendLine($"---Error: Invalid start_line or line_count for {path}---");
                            continue;
                        }
                        
                        await ProcessLineBasedRequest(path, startLine, lineCount, resultBuilder);
                    }
                    else // hasCharParams
                    {
                        var startCharacter = req.Value<int>("start_character");
                        var length = req.Value<int>("length");
                        
                        if (length > MaxCharacterLength)
                        {
                            resultBuilder.AppendLine($"---Error: Requested length {length} exceeds maximum of {MaxCharacterLength} for {path}---");
                            continue;
                        }
                        if (startCharacter < 0 || length < 1)
                        {
                            resultBuilder.AppendLine($"---Error: Invalid start_character or length for {path}---");
                            continue;
                        }
                        
                        await ProcessCharacterBasedRequest(path, startCharacter, length, resultBuilder);
                    }

                }

                SendStatusUpdate("ReadPartialFiles tool completed successfully.");
                return CreateResult(true, true, resultBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ReadPartialFiles tool");
                SendStatusUpdate($"Error processing ReadPartialFiles tool: {ex.Message}");
                return CreateResult(true, true, $"Error processing ReadPartialFiles tool: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a line-based read request
        /// </summary>
        private async Task ProcessLineBasedRequest(string path, int startLine, int lineCount, StringBuilder resultBuilder)
        {
            var (relativePath, fullPath, shouldSkip, skipReason) = ValidateAndPreparePath(path);
            if (shouldSkip)
            {
                resultBuilder.AppendLine(skipReason);
                return;
            }

            try
            {
                if (File.Exists(fullPath))
                {
                    SendStatusUpdate($"Reading lines {startLine}-{startLine + lineCount - 1} from file: {Path.GetFileName(fullPath)}");
                    var allLines = await File.ReadAllLinesAsync(fullPath);
                    var totalLines = allLines.Length;
                    var startIdx = startLine - 1;
                    if (startIdx >= totalLines)
                    {
                        resultBuilder.AppendLine($"--- File: {relativePath} ---");
                        resultBuilder.AppendLine($"(No lines: start_line {startLine} beyond end of file.)");
                    }
                    else
                    {
                        var linesToTake = Math.Min(lineCount, totalLines - startIdx);
                        var selectedLines = allLines.Skip(startIdx).Take(linesToTake).ToArray();
                        resultBuilder.AppendLine($"--- File: {relativePath} (lines {startLine}-{startLine + linesToTake - 1}) ---");
                        foreach (var line in selectedLines)
                        {
                            resultBuilder.AppendLine(line);
                        }
                    }
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
            resultBuilder.AppendLine();
        }

        /// <summary>
        /// Processes a character-based read request
        /// </summary>
        private async Task ProcessCharacterBasedRequest(string path, int startCharacter, int length, StringBuilder resultBuilder)
        {
            var (relativePath, fullPath, shouldSkip, skipReason) = ValidateAndPreparePath(path);
            if (shouldSkip)
            {
                resultBuilder.AppendLine(skipReason);
                return;
            }

            try
            {
                if (File.Exists(fullPath))
                {
                    SendStatusUpdate($"Reading characters {startCharacter}-{startCharacter + length - 1} from file: {Path.GetFileName(fullPath)}");
                    var allText = await File.ReadAllTextAsync(fullPath);
                    var totalChars = allText.Length;
                    
                    if (startCharacter >= totalChars)
                    {
                        resultBuilder.AppendLine($"--- File: {relativePath} ---");
                        resultBuilder.AppendLine($"(No characters: start_character {startCharacter} beyond end of file.)");
                    }
                    else
                    {
                        var charsToTake = Math.Min(length, totalChars - startCharacter);
                        var selectedText = allText.Substring(startCharacter, charsToTake);
                        resultBuilder.AppendLine($"--- File: {relativePath} (characters {startCharacter}-{startCharacter + charsToTake - 1}) ---");
                        resultBuilder.AppendLine(selectedText);
                    }
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
            resultBuilder.AppendLine();
        }

        /// <summary>
        /// Validates and prepares the file path, checking for security and extension restrictions
        /// </summary>
        private (string relativePath, string fullPath, bool shouldSkip, string skipReason) ValidateAndPreparePath(string path)
        {
            var excludedExtensionsCsv = _extraProperties.TryGetValue("ExcludedFileExtensions (CSV)", out var extCsv) ? extCsv :
                _extraProperties.TryGetValue("excludedFileExtensions (CSV)", out var extCsv2) ? extCsv2 :
                ".cs";
            var excludedExtensions = excludedExtensionsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant()).Where(e => e.StartsWith(".")).ToList();

            var relativePath = path;
            if (relativePath.StartsWith("\"")) relativePath = relativePath.Substring(1);
            if (relativePath.EndsWith("\"")) relativePath = relativePath.Substring(0, relativePath.Length - 2);

            var fullPath = Path.GetFullPath(Path.Combine(_projectRoot, relativePath));
            var fileExt = Path.GetExtension(fullPath).ToLowerInvariant();
            
            if (excludedExtensions.Contains(fileExt))
            {
                SendStatusUpdate($"Skipping file with excluded extension: {Path.GetFileName(relativePath)}");
                return (relativePath, fullPath, true, $"---Skipped {relativePath}: Excluded file extension '{fileExt}'.---");
            }
            
            if (!fullPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Attempted to read file outside the project root: {relativePath} (Resolved: {fullPath})");
                SendStatusUpdate($"Error: Path is outside the allowed directory: {Path.GetFileName(relativePath)}");
                return (relativePath, fullPath, true, $"---Error reading {relativePath}: Access denied - Path is outside the allowed directory.---");
            }

            return (relativePath, fullPath, false, string.Empty);
        }

        [McpServerTool, Description("Read specified line ranges or character ranges from one or multiple files. Each file request must specify either line-based parameters (start_line, line_count) or character-based parameters (start_character, length).")]
        public async Task<string> ReadPartialFiles([Description("JSON parameters for ReadPartialFiles")] string parameters = "{}")
        {
            return await ExecuteWithExtraProperties(parameters);
        }
    }
}
