using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Needed for JArray parsing
using SharedClasses.Git;
using System;
using System.Collections.Generic;
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

        public FileSearchTool(ILogger<CodeDiffTool> logger, ISettingsService settingsService) : base(logger, settingsService)
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
                LastModified = DateTime.UtcNow
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
                if (!parameters.ContainsKey("depth") || Convert.ToInt32(parameters["depth"]) > 0) return;
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
                foreach (var filePath in Directory.EnumerateFiles(currentPath))
                {
                    // Check if file is ignored by .gitignore
                    if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(filePath))
                    {
                        // _logger.LogTrace("Ignoring file due to .gitignore: {FilePath}", filePath);
                        continue;
                    }

                    // Search within the file
                    try
                    {
                        // Avoid reading huge files entirely into memory
                        using (var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                        {
                            string line;
                            int lineNumber = 0;
                            var fileLines = new List<(int LineNumber, string Content)>();
                            var matchingLineNumbers = new List<int>();

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

                                // Group consecutive matching lines to avoid redundant context
                                List<(int Start, int End)> matchGroups = new List<(int Start, int End)>();
                                
                                if (matchingLineNumbers.Count > 0)
                                {
                                    int groupStart = matchingLineNumbers[0];
                                    int groupEnd = matchingLineNumbers[0];
                                    
                                    for (int i = 1; i < matchingLineNumbers.Count; i++)
                                    {
                                        // If this line is consecutive to the previous one, extend the group
                                        if (matchingLineNumbers[i] == groupEnd + 1)
                                        {
                                            groupEnd = matchingLineNumbers[i];
                                        }
                                        // Otherwise, finalize the current group and start a new one
                                        else
                                        {
                                            matchGroups.Add((groupStart, groupEnd));
                                            groupStart = matchingLineNumbers[i];
                                            groupEnd = matchingLineNumbers[i];
                                        }
                                    }
                                    // Add the last group
                                    matchGroups.Add((groupStart, groupEnd));
                                }

                                // Process each group of consecutive matches
                                int maxContextsPerFile = 10;
                                foreach (var (groupStart, groupEnd) in matchGroups.Take(maxContextsPerFile))
                                {
                                    // Get context lines (3 lines before first match and 3 lines after last match)
                                    int contextStart = Math.Max(1, groupStart - 3);
                                    int contextEnd = Math.Min(fileLines.Count, groupEnd + 3);

                                    if (groupStart == groupEnd)
                                    {
                                        matchDetails.AppendLine($"  Match at line {groupStart}:");
                                    }
                                    else
                                    {
                                        matchDetails.AppendLine($"  Matches at lines {groupStart}-{groupEnd}:");
                                    }

                                    // Display context with line numbers
                                    for (int i = contextStart - 1; i < contextEnd; i++)
                                    {
                                        // Handle potential index out of bounds if fileLines is smaller than contextEnd (safety check)
                                        if (i < 0 || i >= fileLines.Count) continue;
                                        var (lineNum, content) = fileLines[i];
                                        string prefix = matchingLineNumbers.Contains(lineNum) ? "* " : "  ";
                                        matchDetails.AppendLine($"{lineNum,4} {prefix}{content}");
                                    }
                                    matchDetails.AppendLine();
                                }

                                // Add the "and more" message if there were more matches than shown
                                if (matchGroups.Count > maxContextsPerFile)
                                {
                                    matchDetails.AppendLine($"  ... and {matchGroups.Count - maxContextsPerFile} more matches found in this file.");
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
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            try
            {
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
                if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(CreateResult(true, true, "Error: 'path' parameter is required."));
                if (searchTerms.Length == 0 || searchTerms.All(string.IsNullOrWhiteSpace)) return Task.FromResult(CreateResult(true, true, "Error: 'search_terms' parameter must contain at least one non-empty term."));
                var validSearchTerms = searchTerms.Where(st => !string.IsNullOrWhiteSpace(st)).ToArray();
                var searchPath = Path.GetFullPath(Path.Combine(_projectRoot, path));
                if (!searchPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase)) return Task.FromResult(CreateResult(true, true, "Error: Path is outside the allowed directory."));
                if (!Directory.Exists(searchPath)) return Task.FromResult(CreateResult(true, true, $"Error: Directory not found: {searchPath}"));


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
                SearchFilesRecursively(searchPath, searchPath, depth, validSearchTerms, gitIgnoreFilterManager, matchingFiles);


                // --- Format Result with match context ---
                if (matchingFiles.Any())
                {
                    // The results are now already formatted with context
                    string resultText = $"Found matches in {matchingFiles.Count} files (searching in '{path}'):\n\n" +
                                        string.Join("\n", matchingFiles);
                    return Task.FromResult(CreateResult(true, true, resultText));
                }
                else
                {
                    return Task.FromResult(CreateResult(true, true, "No files found containing the specified search terms."));
                }
            }
            // Keep outer catch block for general errors during processing
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing FileSearch tool");
                return Task.FromResult(CreateResult(true, true, $"Error processing FileSearch tool: {ex.Message}"));
            }
        }
    }
}