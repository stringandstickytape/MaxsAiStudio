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
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the FileSearch tool
    /// </summary>
    public class FileSearchTool : BaseToolImplementation
    {
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();


        public FileSearchTool(ILogger<FileSearchTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the FileSearch tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "a1b2c3d4-e5f6-7890-1234-56789abcdef07",
                Name = "FileSearch",
                Description = "Searches for files containing specific terms within a directory tree.",
                Schema = @"{
  ""name"": ""FileSearch"",
  ""description"": ""Recursively searches for files within a specified path that contain any of the provided search terms. Respects .gitignore rules by default."",
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
      ""search_terms"": {
        ""title"": ""Search Terms"",
        ""type"": ""array"",
        ""items"": {
          ""type"": ""string""
        },
        ""description"": ""An array of strings to search for within file content (case-insensitive).""
      }
    },
    ""required"": [""path"", ""search_terms""],
    ""title"": ""FileSearchArguments"",
    ""type"": ""object""
  }
}",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "txt",
                Filetype = string.Empty, // Or specify if relevant, e.g., "text"
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "excludedFileExtensions (CSV)", "" }, //".cs,.dll,.xml,.map,.7z,.png" },
                    { "excludedFilePrefixes (CSV)", "" }, //"jquery" }
                }
            };
        }



        /// <summary>
        /// Recursively searches files within a directory for given search terms.
        /// </summary>
        private void SearchFilesRecursively(string activeRoot, string rootSearchPath, string currentPath, int initialDepth, int currentRemainingDepth, string[] searchTerms, GitIgnoreFilterManager gitIgnoreFilter, List<string> results, IReadOnlyDictionary<string, string> currentExtraProperties)
        {
            // Depth check: If initialDepth was > 0 (limited search), and currentRemainingDepth becomes negative, stop.
            if (initialDepth > 0 && currentRemainingDepth < 0)
            {
                return;
            }

            // --- Process Files in Current Directory ---
            try
            {
                var excludedExtensionsCsv = currentExtraProperties.TryGetValue("ExcludedFileExtensions (CSV)", out var extCsv) ? extCsv : currentExtraProperties.TryGetValue("excludedFileExtensions (CSV)", out var extCsv2) ? extCsv2 : string.Empty;
                var excludedPrefixesCsv = currentExtraProperties.TryGetValue("ExcludedFilePrefixes (CSV)", out var preCsv) ? preCsv : currentExtraProperties.TryGetValue("excludedFilePrefixes (CSV)", out var preCsv2) ? preCsv2 : string.Empty;
                var excludedExtensions = excludedExtensionsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().ToLowerInvariant()).Where(e => e.StartsWith(".")).ToList();
                var excludedPrefixes = excludedPrefixesCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim().ToLowerInvariant()).Where(p => !string.IsNullOrEmpty(p)).ToList();

                foreach (var filePath in Directory.EnumerateFiles(currentPath))
                {
                    // Check if file is ignored by .gitignore
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
                        Debug.WriteLine("Searchtool checking " + filePath);
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
                                if (searchTerms.Any(term => line.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    matchingLineNumbers.Add(lineNumber);
                                }
                            }

                            if (matchingLineNumbers.Any())
                            {
                                var matchDetails = new StringBuilder();
                                // Use Path.GetRelativePath to show path relative to activeRoot for consistency if needed, or keep absolute.
                                // For now, keeping absolute path as per original logic.
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

            // --- Recurse into Subdirectories ---
            if (initialDepth == 0 || currentRemainingDepth > 0) // currentRemainingDepth is used here
            {
                try
                {
                    foreach (var dirPath in Directory.EnumerateDirectories(currentPath))
                    {
                        // Simplified exclusion for common build/dependency folders
                        string dirName = Path.GetFileName(dirPath).ToLowerInvariant();
                        if (dirName == "node_modules" || dirName == "bin" || dirName == "dist" || dirName == "obj")
                            continue;
                            
                        if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(dirPath + Path.DirectorySeparatorChar))
                        {
                            continue;
                        }
                        SearchFilesRecursively(activeRoot, rootSearchPath, dirPath, initialDepth, initialDepth > 0 ? currentRemainingDepth - 1 : 0, searchTerms, gitIgnoreFilter, results, currentExtraProperties);
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

        // Helper to get parameters safely, needed for depth check inside recursive function
        // private Dictionary<string, object> parameters = new Dictionary<string, object>(); // Removed

        // Override ProcessAsync to store parameters before calling the recursive function
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties, string projectRootPathOverride = null)
        {
            _extraProperties = extraProperties;
            var activeRoot = GetActiveProjectRoot(projectRootPathOverride);
            List<string> matchingFiles = new List<string>();

            try
            {
                SendStatusUpdate("Starting FileSearch tool execution...");
                var localParameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // --- Extract Parameters (using localParameters) ---
                var path = localParameters.ContainsKey("path") ? localParameters["path"].ToString() : string.Empty;
                var depth = localParameters.ContainsKey("depth") ? Convert.ToInt32(localParameters["depth"]) : 0; // This is initialDepth
                var includeFiltered = localParameters.ContainsKey("include_filtered") ? Convert.ToBoolean(localParameters["include_filtered"]) : false;
                string[] searchTerms;

                if (localParameters.TryGetValue("search_terms", out var searchTermsObj) && searchTermsObj is JArray searchTermsArray)
                {
                    searchTerms = searchTermsArray.ToObject<string[]>() ?? Array.Empty<string>();
                }
                else
                {
                    _logger.LogWarning("Search terms parameter missing or invalid. Using empty array.");
                    searchTerms = Array.Empty<string>();
                }

                // --- Validation ---
                if (string.IsNullOrWhiteSpace(path)) {
                    SendStatusUpdate("Error: 'path' parameter is required.");
                    return Task.FromResult(CreateResult(true, true, "Error: 'path' parameter is required."));
                }
                if (searchTerms.Length == 0 || searchTerms.All(string.IsNullOrWhiteSpace)) {
                    SendStatusUpdate("Error: 'search_terms' parameter must contain at least one non-empty term.");
                    return Task.FromResult(CreateResult(true, true, "Error: 'search_terms' parameter must contain at least one non-empty term."));
                }
                var validSearchTerms = searchTerms.Where(st => !string.IsNullOrWhiteSpace(st)).ToArray();
                var searchPath = Path.GetFullPath(Path.Combine(activeRoot, path));
                if (!searchPath.StartsWith(activeRoot, StringComparison.OrdinalIgnoreCase)) {
                    SendStatusUpdate("Error: Path is outside the allowed directory.");
                    return Task.FromResult(CreateResult(true, true, "Error: Path is outside the allowed directory."));
                }
                if (!Directory.Exists(searchPath)) {
                    string suggestion = FindAlternativeDirectory(searchPath, activeRoot);
                    string errorMessage = $"Error: Directory not found: {searchPath}";
                    if (!string.IsNullOrEmpty(suggestion)) {
                        errorMessage += $"\n{suggestion}";
                    }
                    SendStatusUpdate(errorMessage);
                    return Task.FromResult(CreateResult(true, true, errorMessage));
                }
                
                SendStatusUpdate($"Searching for terms: {string.Join(", ", validSearchTerms)} in {Path.GetFileName(searchPath)}...");

                // --- GitIgnore Setup ---
                GitIgnoreFilterManager gitIgnoreFilterManager = null;
                if (!includeFiltered)
                {
                    string currentIgnorePath = searchPath;
                    string gitIgnoreFilePath = null;
                    // Traverse up from searchPath to activeRoot to find .gitignore
                    while (currentIgnorePath != null && currentIgnorePath.Length >= activeRoot.Length && currentIgnorePath.StartsWith(activeRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        gitIgnoreFilePath = Path.Combine(currentIgnorePath, ".gitignore");
                        if (File.Exists(gitIgnoreFilePath))
                        {
                            break;
                        }
                        if (currentIgnorePath.Equals(activeRoot, StringComparison.OrdinalIgnoreCase)) break; // Stop if we are at the activeRoot
                        currentIgnorePath = Directory.GetParent(currentIgnorePath)?.FullName;
                    }

                    if (File.Exists(gitIgnoreFilePath))
                    {
                        try
                        {
                            _logger.LogInformation("Using .gitignore: {GitIgnorePath}", gitIgnoreFilePath);
                            var gitignoreContent = File.ReadAllText(gitIgnoreFilePath);
                            // Pass activeRoot as the base directory for GitIgnoreFilterManager relative path calculations
                            gitIgnoreFilterManager = new GitIgnoreFilterManager(gitignoreContent, activeRoot);
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

                // --- Perform Search ---
                SendStatusUpdate($"Beginning file search with depth: {depth}...");
                // Pass 'depth' as both initialDepth and remainingDepth for the first call
                SearchFilesRecursively(activeRoot, searchPath, searchPath, depth, depth, validSearchTerms, gitIgnoreFilterManager, matchingFiles, _extraProperties);

                // --- Format Result with match context ---
                if (matchingFiles.Any())
                {
                    string resultText = $"Found matches for specified search terms of {string.Join("/", searchTerms)} in {matchingFiles.Count} files (searching in '{path}'):\n\n" +
                                        string.Join("\n", matchingFiles);
                    SendStatusUpdate($"Search completed. Found matches in {matchingFiles.Count} files.");
                    return Task.FromResult(CreateResult(true, true, resultText));
                }
                else
                {
                    SendStatusUpdate("Search completed. No matches found.");
                    return Task.FromResult(CreateResult(true, true, $"No files found containing the specified search terms of {string.Join("/", searchTerms)}"));
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing FileSearch parameters");
                return Task.FromResult(CreateResult(true, true, $"Error processing FileSearch tool parameters: Invalid JSON format. {jsonEx.Message}"));
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Critical error during FileSearch tool processing");
                return Task.FromResult(CreateResult(true, true, $"Critical error processing FileSearch tool: {ex.Message}"));
            }
        }
    }
}