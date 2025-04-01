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

        public ReadFilesTool(ILogger<CodeDiffTool> logger, ISettingsService settingsService) : base(logger, settingsService)
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
                Description = "Read the contents of one or multiple files.",
                Schema = @"{
  ""name"": ""ReadFiles"",
  ""description"": ""Read the contents of one or multiple files.  Can read a single file or multiple files simultaneously. When reading multiple files, each file's content is returned with its path as a reference. Failed reads for individual files won't stop the entire operation. Only works within allowed directories."",
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
                OutputFileType = "file",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a ReadFile tool call
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            _logger.LogInformation("ReadFile tool called");
            var resultBuilder = new StringBuilder();

            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);
                var pathsObject = parameters["paths"];
                List<string> pathsToRead = new List<string>();

                if (pathsObject is string singlePath)
                {
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

                foreach (var relativePath in pathsToRead)
                {
                    // Security check: Ensure the path is within the project root
                    var fullPath = Path.GetFullPath(Path.Combine(_projectRoot, relativePath));

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
                            resultBuilder.AppendLine($"---Error reading {relativePath}: File not found.---");
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