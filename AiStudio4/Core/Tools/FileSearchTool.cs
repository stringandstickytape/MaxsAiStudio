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
        private void SearchFilesRecursively(string rootSearchPath, string currentPath, int remainingDepth, string[] searchTerms, GitIgnoreFilterManager gitIgnoreFilter, List<string> results)
        {
            // Base case: Invalid depth (0 means unlimited, so check for < 0 only if depth was initially > 0)
            if (remainingDepth < 0 && GetToolDefinition().Schema.Contains("\"default\": 0")) // Check schema default if it matters, 0 here means infinite
            {
                // If the initial depth wasn't 0, then < 0 means we exceeded it.
                // If initial depth was 0, we never decrement, so this condition isn't strictly needed unless we change depth handling.
                // Let's assume initial depth > 0 implies limited search.
                if (!parameters.ContainsKey("depth") || Convert.ToInt32(parameters["depth"]) > 0)
                    return;
            }
            // Simplified depth check: If depth was specified > 0 initially, and remainingDepth becomes negative, stop.
            int initialDepth = parameters.ContainsKey("depth") ? Convert.ToInt32(parameters["depth"]) : 0;
            if (initialDepth > 0 && remainingDepth < 0)
            {
                return;
            }


            // --- Process Files in Current Directory ---
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
                    // Check if file is ignored by .gitignore
                    if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(filePath))
                    {
                        // _logger.LogTrace("Ignoring file due to .gitignore: {FilePath}", filePath);
                        continue;
                    }

                    var fileName = Path.GetFileName(filePath).ToLowerInvariant();
                    var fileExt = Path.GetExtension(filePath).ToLowerInvariant();
                    // Exclude by extension
                    if (excludedExtensions.Contains(fileExt))
                        continue;
                    // Exclude by prefix
                    if (excludedPrefixes.Any(prefix => fileName.StartsWith(prefix)))
                        continue;

                    // Search within the file
                    try
                    {
                        Debug.WriteLine("Searchtool checking " + filePath);
                        // Avoid reading huge files entirely into memory
                        using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                        {
                            string line;
                            int lineNumber = 0;
                            var fileLines = new List<(int LineNumber, string Content)>();
                            var matchingLineNumbers = new List<int>();

                            // First, read the file and find matching lines
                            while ((line = reader.ReadLine()) != null)
                                   // First, read the file and find matching lines
                            while ((line = reader.ReadLine()) != null)
                            {
                                lineNumber++;
                                fileLines.Add((lineNumber, line));

                                // Case-insensitive search
                                if (searchTerms.Any(term => line.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    matchingLineNumbers.Add(lineNumber);
                                }
                            }

                            // If we found matches, add the file path and context to the results
                            if (matchingLineNumbers.Any())
                            {
                                var matchDetails = new StringBuilder();
                                matchDetails.AppendLine(filePath);

                                // Output only first 10 matching lines with line number and pipe prefix
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
                    }                    catch (UnauthorizedAccessException uaEx)
                    {
                        _logger.LogWarning(uaEx, "Access denied reading file {FilePath}. Skipping.", filePath);
                    }
                    catch (Exception fileEx) // Catch other potential file reading errors
                    {
                        _logger.LogError(fileEx, "Unexpected error reading file {FilePath}. Skipping.", filePath);
                    }
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogWarning(uaEx, "Access denied listing files in directory {CurrentPath}. Skipping directory.", currentPath);
                return; // Can't proceed in this directory
            }
            catch (Exception dirEx)
            {
                _logger.LogError(dirEx, "Error listing files in directory {CurrentPath}. Skipping directory.", currentPath);
                return; // Can't proceed in this directory
            }

            // --- Recurse into Subdirectories ---
            // Only recurse if depth allows (depth == 0 means unlimited, otherwise decrement)
            if (initialDepth == 0 || remainingDepth > 0)
            {
                try
                {
                    foreach (var dirPath in Directory.EnumerateDirectories(currentPath))
                    {
                        if (dirPath.EndsWith("node_modules") || dirPath.EndsWith("bin") || dirPath.EndsWith("dist") || dirPath.EndsWith("obj") || dirPath.EndsWith("obj"))
                            continue;
                        // Check if directory is ignored by .gitignore
                        // Note: GitIgnoreFilterManager needs to correctly handle directory patterns (e.g., ending with '/')
                        if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(dirPath + Path.DirectorySeparatorChar)) // Append slash for directory check
                        {
                            // _logger.LogTrace("Ignoring directory due to .gitignore: {DirPath}", dirPath);
                            continue;
                        }

                        // Recurse with decremented depth if depth is limited
                        SearchFilesRecursively(rootSearchPath, dirPath, initialDepth > 0 ? remainingDepth - 1 : 0, searchTerms, gitIgnoreFilter, results);
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
        private Dictionary<string, object> parameters = new Dictionary<string, object>();

        // Override ProcessAsync to store parameters before calling the recursive function
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _extraProperties = extraProperties;

            try
            {
                SendStatusUpdate("Starting FileSearch tool execution...");
                parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Now call the main logic which uses this.parameters
                return ProcessSearchInternal(toolParameters);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing FileSearch parameters");
                return Task.FromResult(CreateResult(true, true, $"Error processing FileSearch tool parameters: Invalid JSON format. {jsonEx.Message}"));
            }
            catch (Exception ex) // Catch potential null reference if deserialization fails badly
            {
                _logger.LogError(ex, "Critical error during parameter setup for FileSearch tool");
                return Task.FromResult(CreateResult(true, true, $"Critical error setting up FileSearch tool: {ex.Message}"));
            }
        }

        // Renamed original ProcessAsync content to avoid recursion issues with parameter storing
        private Task<BuiltinToolResult> ProcessSearchInternal(string toolParameters) // toolParameters string is technically redundant now but keeps signature
        {
            List<string> matchingFiles = new List<string>(); // Now contains formatted content with context
            try
            {
                // --- Extract Parameters (using the class member 'parameters') ---
                var path = parameters.ContainsKey("path") ? parameters["path"].ToString() : string.Empty;
                var depth = parameters.ContainsKey("depth") ? Convert.ToInt32(parameters["depth"]) : 0;
                var includeFiltered = parameters.ContainsKey("include_filtered") ? Convert.ToBoolean(parameters["include_filtered"]) : false;
                string[] searchTerms;

                if (parameters.TryGetValue("search_terms", out var searchTermsObj) && searchTermsObj is JArray searchTermsArray)
                {
                    searchTerms = searchTermsArray.ToObject<string[]>() ?? Array.Empty<string>();
                }
                else
                {
                    _logger.LogWarning("Search terms parameter missing or invalid. Using empty array.");
                    searchTerms = Array.Empty<string>();
                }

                // --- Validation (as before) ---
                if (string.IsNullOrWhiteSpace(path)) {
                    SendStatusUpdate("Error: 'path' parameter is required.");
                    return Task.FromResult(CreateResult(true, true, "Error: 'path' parameter is required."));
                }
                if (searchTerms.Length == 0 || searchTerms.All(string.IsNullOrWhiteSpace)) {
                    SendStatusUpdate("Error: 'search_terms' parameter must contain at least one non-empty term.");
                    return Task.FromResult(CreateResult(true, true, "Error: 'search_terms' parameter must contain at least one non-empty term."));
                }
                var validSearchTerms = searchTerms.Where(st => !string.IsNullOrWhiteSpace(st)).ToArray();
                var searchPath = Path.GetFullPath(Path.Combine(_projectRoot, path));
                if (!searchPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase)) {
                    SendStatusUpdate("Error: Path is outside the allowed directory.");
                    return Task.FromResult(CreateResult(true, true, "Error: Path is outside the allowed directory."));
                }
                if (!Directory.Exists(searchPath)) {
                    SendStatusUpdate($"Error: Directory not found: {searchPath}");
                    return Task.FromResult(CreateResult(true, true, $"Error: Directory not found: {searchPath}"));
                }
                
                SendStatusUpdate($"Searching for terms: {string.Join(", ", validSearchTerms)} in {Path.GetFileName(searchPath)}...");

                // --- GitIgnore Setup (as before) ---
                GitIgnoreFilterManager gitIgnoreFilterManager = null;
                // (Code for finding and loading .gitignore remains the same)
                if (!includeFiltered)
                {
                    // ... (same gitignore finding logic as above) ...
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
                            // Crucial: Pass the base directory of the .gitignore file
                            gitIgnoreFilterManager = new GitIgnoreFilterManager(gitignoreContent);
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
                SearchFilesRecursively(searchPath, searchPath, depth, validSearchTerms, gitIgnoreFilterManager, matchingFiles);


                // --- Format Result with match context ---
                if (matchingFiles.Any())
                {
                    // The results are now already formatted with context
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
            // Keep outer catch block for general errors during processing
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing FileSearch tool");
                SendStatusUpdate($"Error processing FileSearch tool: {ex.Message}");
                return Task.FromResult(CreateResult(true, true, $"Error processing FileSearch tool: {ex.Message}"));
            }
        }
    }
}