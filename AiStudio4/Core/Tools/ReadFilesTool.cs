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
    /// Implementation of the ReadFile tool
    /// </summary>
    public class ReadFilesTool : BaseToolImplementation
    {
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();

        public ReadFilesTool(ILogger<CodeDiffTool> logger, IGeneralSettingsService generalSettingsService) : base(logger, generalSettingsService)
        {
        }

        /// <summary>
        /// Gets the ReadFile tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "b2c3d4e5-f6a7-8901-2345-67890abcdef05", // Fixed GUID for ReadFile
                Name = "ReadFiles",
                ExtraProperties = new Dictionary<string, string> {
                    { "ExcludedFileExtensions (CSV)", "" }, //".cs" }
                },
                Description = "Read the contents of one or multiple files.",
                Schema = @"{
  ""name"": ""ReadFiles"",
  ""description"": ""Read the contents of one or multiple files.  Can read a single file or multiple files simultaneously. When reading multiple files, each file's content is returned with its path as a reference. Failed reads for individual files won't stop the entire operation. Only works within allowed directories.  YOU MUST NEVER fetch the same file twice in succession."",
  ""input_schema"": {
                ""properties"": {
                ""paths"": {
                    ""anyOf"": [
                        {""items"": {""type"": ""string""}, ""type"": ""array""},
                        {""type"": ""string""}
                    ],
                    ""description"": ""absolute path to the file or files to read""
                }
            },
            ""required"": [""paths""],
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
        /// Processes a ReadFile tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _extraProperties = extraProperties;
            _logger.LogInformation("ReadFile tool called");
            var resultBuilder = new StringBuilder();

            // If user-edited extraProperties are provided, override defaults for excluded extensions
            var toolDef = GetToolDefinition();


            var excludedExtensionsCsv = _extraProperties.TryGetValue("ExcludedFileExtensions (CSV)", out var extCsv) ? extCsv : ".cs";
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
                        resultBuilder.AppendLine($"---Skipped {relativePath}: Excluded file extension '{fileExt}'.---");
                        continue;
                    }

                    if (!fullPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning($"Attempted to read file outside the project root: {relativePath} (Resolved: {fullPath})");
                        resultBuilder.AppendLine($"---Error reading {relativePath}: Access denied - Path is outside the allowed directory.---");
                        continue; // Skip this file
                    }

                    try
                    {
                        if (File.Exists(fullPath))
                        {
                            var content = await File.ReadAllTextAsync(fullPath);
                            resultBuilder.AppendLine($"--- File: {relativePath} ---");
                            resultBuilder.AppendLine(content);
                        }
                        else
                        {
                            resultBuilder.AppendLine($"---Error reading {relativePath}: File not found. Did you get the directory wrong?");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error reading file: {fullPath}");
                        resultBuilder.AppendLine($"---Error reading {relativePath}: {ex.Message}---");
                    }
                    resultBuilder.AppendLine(); // Add a separator between files
                }

                return CreateResult(true, true, resultBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ReadFile tool");
                return CreateResult(true, true, $"Error processing ReadFile tool: {ex.Message}");
            }
        }
    }
}