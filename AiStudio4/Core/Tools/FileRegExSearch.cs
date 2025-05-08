// AiStudio4\Core\Tools\FileRegExSearch.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Needed for JArray parsing
using SharedClasses.Git;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the FileRegExSearch tool
    /// </summary>
    public class FileRegExSearch : BaseToolImplementation
    {
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();

        public FileRegExSearch(ILogger<FileRegExSearch> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the FileRegExSearch tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "b2c3d4e5-f6a7-8901-2345-6789abcdef08",
                Name = "FileRegExSearch",
                Description = "Searches for files containing lines matching any of the provided regular expressions within a directory tree.",
                Schema = @"{
  ""name"": ""FileRegExSearch"",
  ""description"": ""Recursively searches for files within a specified path that contain any of the provided regular expressions. Respects .gitignore rules by default."",
  ""input_schema"": {
    ""properties"": {
      ""path"": {
        ""title"": ""Path"",
        ""type"": ""string"",
        ""description"": ""The path to the directory to start searching from (relative to project root).""
      },
      ""depth"": {
        ""default"": 0,
        ""title"": ""Depth"",
        ""type"": ""integer"",
        ""description"": ""The maximum depth to search recursively (0 for unlimited).""
      },
      ""include_filtered"": {
        ""default"": false,
        ""title"": ""Include Filtered"",
        ""type"": ""boolean"",
        ""description"": ""Include files and directories that are normally filtered by .gitignore.""
      },
      ""search_regexes"": {
        ""title"": ""Search Regexes"",
        ""type"": ""array"",
        ""items"": {
          ""type"": ""string""
        },
        ""description"": ""An array of regular expressions to search for within file content (case-insensitive).""
      }
    },
    ""required"": [""path"", ""search_regexes""],
    ""title"": ""FileRegExSearchArguments"",
    ""type"": ""object""
  }
}",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "excludedFileExtensions (CSV)", "" },
                    { "excludedFilePrefixes (CSV)", "" },
                }
            };
        }

        /// <summary>
        /// Recursively searches files within a directory for lines matching any regex.
        /// </summary>
        private void SearchFilesRecursively(string rootSearchPath, string currentPath, int remainingDepth, Regex[] regexes, GitIgnoreFilterManager gitIgnoreFilter, List<string> results)
        {
            int initialDepth = parameters.ContainsKey("depth") ? Convert.ToInt32(parameters["depth"]) : 0;
            if (initialDepth > 0 && remainingDepth < 0)
            {
                return;
            }

            try
            {
                var excludedExtensionsCsv = _extraProperties.TryGetValue("ExcludedFileExtensions (CSV)", out var extCsv) ? extCsv : _extraProperties.TryGetValue("excludedFileExtensions (CSV)", out var extCsv2) ? extCsv2 : string.Empty;
                var excludedPrefixesCsv = _extraProperties.TryGetValue("ExcludedFilePrefixes (CSV)", out var preCsv) ? preCsv : _extraProperties.TryGetValue("excludedFilePrefixes (CSV)", out var preCsv2) ? preCsv2 : string.Empty;
                var excludedExtensions = excludedExtensionsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().ToLowerInvariant()).Where(e => e.StartsWith(".")).ToList();
                var excludedPrefixes = excludedPrefixesCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim().ToLowerInvariant()).Where(p => !string.IsNullOrEmpty(p)).ToList();

                foreach (var filePath in Directory.EnumerateFiles(currentPath))
                {
                    if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(filePath))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileName(filePath).ToLowerInvariant();
                    var fileExt = Path.GetExtension(filePath).ToLowerInvariant();
                    if (excludedExtensions.Contains(fileExt))
                        continue;
                    if (excludedPrefixes.Any(prefix => fileName.StartsWith(prefix)))
                        continue;

                    try
                    {
                        Debug.WriteLine("FileRegExSearch checking " + filePath);
                        using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                        {
                            string line;
                            int lineNumber = 0;
                            var fileLines = new List<(int LineNumber, string Content)>();
                            var matchingLineNumbers = new List<int>();

                            while ((line = reader.ReadLine()) != null)
                            {
                                lineNumber++;
                                fileLines.Add((lineNumber, line));
                                if (regexes.Any(rx => rx.IsMatch(line)))
                                {
                                    matchingLineNumbers.Add(lineNumber);
                                }
                            }

                            if (matchingLineNumbers.Any())
                            {
                                var matchDetails = new StringBuilder();
                                matchDetails.AppendLine(filePath);
                                int maxMatchesToShow = 10;
                                int matchesShown = 0;
                                foreach (var lineNum in matchingLineNumbers)
                                {
                                    if (matchesShown >= maxMatchesToShow) break;
                                    var content = fileLines[lineNum - 1].Content;
                                    if (content.Length > 200) content = content.Substring(0, 200);
                                    matchDetails.AppendLine($"{lineNum}|{content}");
                                    matchesShown++;
                                }
                                results.Add(matchDetails.ToString());
                            }
                        }
                    }
                    catch (IOException ioEx)
                    {
                        _logger.LogWarning(ioEx, "IO Error reading file {FilePath}. Skipping.", filePath);
                    }
                    catch (UnauthorizedAccessException uaEx)
                    {
                        _logger.LogWarning(uaEx, "Access denied reading file {FilePath}. Skipping.", filePath);
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogError(fileEx, "Unexpected error reading file {FilePath}. Skipping.", filePath);
                    }
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogWarning(uaEx, "Access denied listing files in directory {CurrentPath}. Skipping directory.", currentPath);
                return;
            }
            catch (Exception dirEx)
            {
                _logger.LogError(dirEx, "Error listing files in directory {CurrentPath}. Skipping directory.", currentPath);
                return;
            }

            if (initialDepth == 0 || remainingDepth > 0)
            {
                try
                {
                    foreach (var dirPath in Directory.EnumerateDirectories(currentPath))
                    {
                        if (dirPath.EndsWith("node_modules") || dirPath.EndsWith("bin") || dirPath.EndsWith("dist") || dirPath.EndsWith("obj"))
                            continue;
                        if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(dirPath + Path.DirectorySeparatorChar))
                        {
                            continue;
                        }
                        SearchFilesRecursively(rootSearchPath, dirPath, initialDepth > 0 ? remainingDepth - 1 : 0, regexes, gitIgnoreFilter, results);
                    }
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    _logger.LogWarning(uaEx, "Access denied listing directories in {CurrentPath}. Skipping subdirectories.", currentPath);
                }
                catch (Exception dirEx)
                {
                    _logger.LogError(dirEx, "Error listing directories in {CurrentPath}. Skipping subdirectories.", currentPath);
                }
            }
        }

        private Dictionary<string, object> parameters = new Dictionary<string, object>();

        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _extraProperties = extraProperties;

            try
            {
                SendStatusUpdate("Starting FileRegExSearch tool execution...");
                parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();
                return ProcessSearchInternal(toolParameters);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing FileRegExSearch parameters");
                return Task.FromResult(CreateResult(true, true, $"Error processing FileRegExSearch tool parameters: Invalid JSON format. {jsonEx.Message}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during parameter setup for FileRegExSearch tool");
                return Task.FromResult(CreateResult(true, true, $"Critical error setting up FileRegExSearch tool: {ex.Message}"));
            }
        }

        private Task<BuiltinToolResult> ProcessSearchInternal(string toolParameters)
        {
            List<string> matchingFiles = new List<string>();
            try
            {
                var path = parameters.ContainsKey("path") ? parameters["path"].ToString() : string.Empty;
                var depth = parameters.ContainsKey("depth") ? Convert.ToInt32(parameters["depth"]) : 0;
                var includeFiltered = parameters.ContainsKey("include_filtered") ? Convert.ToBoolean(parameters["include_filtered"]) : false;
                string[] searchRegexes;

                if (parameters.TryGetValue("search_regexes", out var searchRegexesObj) && searchRegexesObj is JArray searchRegexesArray)
                {
                    searchRegexes = searchRegexesArray.ToObject<string[]>() ?? Array.Empty<string>();
                }
                else
                {
                    _logger.LogWarning("Search regexes parameter missing or invalid. Using empty array.");
                    searchRegexes = Array.Empty<string>();
                }

                if (string.IsNullOrWhiteSpace(path)) {
                    SendStatusUpdate("Error: 'path' parameter is required.");
                    return Task.FromResult(CreateResult(true, true, "Error: 'path' parameter is required."));
                }
                if (searchRegexes.Length == 0 || searchRegexes.All(string.IsNullOrWhiteSpace)) {
                    SendStatusUpdate("Error: 'search_regexes' parameter must contain at least one non-empty regex.");
                    return Task.FromResult(CreateResult(true, true, "Error: 'search_regexes' parameter must contain at least one non-empty regex."));
                }
                var validSearchRegexes = searchRegexes.Where(st => !string.IsNullOrWhiteSpace(st)).ToArray();
                var searchPath = Path.GetFullPath(Path.Combine(_projectRoot, path));
                if (!searchPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase)) {
                    SendStatusUpdate("Error: Path is outside the allowed directory.");
                    return Task.FromResult(CreateResult(true, true, "Error: Path is outside the allowed directory."));
                }
                if (!Directory.Exists(searchPath)) {
                    string suggestion = FindAlternativeDirectory(searchPath);
                    string errorMessage = $"Error: Directory not found: {searchPath}";
                    if (!string.IsNullOrEmpty(suggestion)) {
                        errorMessage += $"\n{suggestion}";
                    }
                    SendStatusUpdate(errorMessage);
                    return Task.FromResult(CreateResult(true, true, errorMessage));
                }

                // Compile regexes (case-insensitive)
                Regex[] regexes;
                try
                {
                    regexes = validSearchRegexes.Select(rx => new Regex(rx, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
                }
                catch (Exception rex)
                {
                    SendStatusUpdate($"Error: Invalid regular expression: {rex.Message}");
                    return Task.FromResult(CreateResult(true, true, $"Error: Invalid regular expression: {rex.Message}"));
                }

                SendStatusUpdate($"Searching for regexes: {string.Join(", ", validSearchRegexes)} in {Path.GetFileName(searchPath)}...");

                GitIgnoreFilterManager gitIgnoreFilterManager = null;
                if (!includeFiltered)
                {
                    string currentIgnorePath = searchPath;
                    string gitIgnoreFilePath = null;
                    while (currentIgnorePath != null && currentIgnorePath.Length >= _projectRoot.Length && currentIgnorePath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        gitIgnoreFilePath = Path.Combine(currentIgnorePath, ".gitignore");
                        if (File.Exists(gitIgnoreFilePath))
                        {
                            break;
                        }
                        if (currentIgnorePath.Equals(_projectRoot, StringComparison.OrdinalIgnoreCase)) break;
                        currentIgnorePath = Directory.GetParent(currentIgnorePath)?.FullName;
                    }

                    if (File.Exists(gitIgnoreFilePath))
                    {
                        try
                        {
                            _logger.LogInformation("Using .gitignore: {GitIgnorePath}", gitIgnoreFilePath);
                            var gitignoreContent = File.ReadAllText(gitIgnoreFilePath);
                            string gitignoreBaseDir = Path.GetDirectoryName(gitIgnoreFilePath);
                            gitIgnoreFilterManager = new GitIgnoreFilterManager(gitignoreContent, _projectRoot);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error reading or processing .gitignore file: {GitIgnorePath}", gitIgnoreFilePath);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No .gitignore file found in search path or ancestors up to project root.");
                    }
                }

                SendStatusUpdate($"Beginning file regex search with depth: {depth}...");
                SearchFilesRecursively(searchPath, searchPath, depth, regexes, gitIgnoreFilterManager, matchingFiles);

                if (matchingFiles.Any())
                {
                    string resultText = $"Found matches for the specified regexes ({string.Join("/", searchRegexes)}) in {matchingFiles.Count} files (searching in '{path}'):\n\n" +
                                        string.Join("\n", matchingFiles);
                    SendStatusUpdate($"Search completed. Found matches in {matchingFiles.Count} files.");
                    return Task.FromResult(CreateResult(true, true, resultText));
                }
                else
                {
                    SendStatusUpdate("Search completed. No matches found.");
                    return Task.FromResult(CreateResult(true, true, $"No files found containing lines matching the specified regexes: {string.Join("/", searchRegexes)}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing FileRegExSearch tool");
                SendStatusUpdate($"Error processing FileRegExSearch tool: {ex.Message}");
                return Task.FromResult(CreateResult(true, true, $"Error processing FileRegExSearch tool: {ex.Message}"));
            }
        }
    }
}