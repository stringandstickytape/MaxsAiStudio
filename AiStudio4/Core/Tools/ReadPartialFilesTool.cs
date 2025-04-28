// AiStudio4\Core\Tools\ReadPartialFilesTool.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the ReadPartialFiles tool
    /// </summary>
    public class ReadPartialFilesTool : BaseToolImplementation
    {
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();
        private const int MaxLineCount = 100;

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
                Guid = "e3f4a5b6-c7d8-9012-3456-78901bcdef12", // New GUID for ReadPartialFiles
                Name = "ReadPartialFiles",
                ExtraProperties = new Dictionary<string, string> {
                    { "excludedFileExtensions (CSV)", "" },
                },
                Description = "Read specified line ranges from one or multiple files. Each file request must specify a path, start line (1-based), and line count (max 100).",
                Schema = @"{
  ""name"": ""ReadPartialFiles"",
  ""description"": ""Read specified line ranges from one or multiple files. Each file request must specify a path, start line (1-based), and line count (max 100). Returns the specified lines for each file. Failed reads for individual files won't stop the entire operation. Only works within allowed directories."",
  ""input_schema"": {
    ""properties"": {
      ""requests"": {
        ""type"": ""array"",
        ""items"": {
          ""type"": ""object"",
          ""properties"": {
            ""path"": { ""type"": ""string"", ""description"": ""absolute path to the file to read"" },
            ""start_line"": { ""type"": ""integer"", ""minimum"": 1, ""description"": ""1-based line number to start reading from"" },
            ""line_count"": { ""type"": ""integer"", ""minimum"": 1, ""maximum"": 100, ""description"": ""number of lines to read (max 100)"" }
          },
          ""required"": [""path"", ""start_line"", ""line_count""]
        },
        ""description"": ""List of file read requests, each with path, start_line, and line_count (max 100)""
      }
    },
    ""required"": [""requests""],
    ""type"": ""object""
  }
}",
                Categories = new List<string> { "MaxCode" },
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
                ".cs";
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

                    var relativePath = path;
                    if (relativePath.StartsWith("\"")) relativePath = relativePath.Substring(1);
                    if (relativePath.EndsWith("\"")) relativePath = relativePath.Substring(0, relativePath.Length - 2);

                    var fullPath = Path.GetFullPath(Path.Combine(_projectRoot, relativePath));
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
                        continue;
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
    }
}