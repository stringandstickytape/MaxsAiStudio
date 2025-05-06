using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    /// Implementation of the FindAndReplace tool
    /// </summary>
    public class FindAndReplaceTool : BaseToolImplementation
    {
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();

        public FindAndReplaceTool(ILogger<FindAndReplaceTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService) : base(logger, generalSettingsService, statusMessageService)
        {
        }

        /// <summary>
        /// Gets the FindAndReplace tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "a1b2c3d4-e5f6-7890-1234-56789abcdef08",
                Name = "FindAndReplace",
                Description = "Finds and replaces text in files within a directory tree.",
                Schema = @"{
  ""name"": ""FindAndReplace"",
  ""description"": ""Recursively searches for files within a specified path and replaces text matching the provided search terms with replacement text. Respects .gitignore rules by default."",
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
      ""preview_only"": {
        ""default"": true,
        ""title"": ""Preview Only"",
        ""type"": ""boolean"",
        ""description"": ""If true, only shows what would be replaced without making actual changes.""
      },
      ""replacements"": {
        ""title"": ""Replacements"",
        ""type"": ""array"",
        ""items"": {
          ""type"": ""object"",
          ""properties"": {
            ""search"": {
              ""type"": ""string"",
              ""description"": ""The text to search for.""
            },
            ""replace"": {
              ""type"": ""string"",
              ""description"": ""The text to replace it with.""
            },
            ""case_sensitive"": {
              ""type"": ""boolean"",
              ""default"": false,
              ""description"": ""Whether the search should be case-sensitive.""
            }
          },
          ""required"": [""search"", ""replace""]
        },
        ""description"": ""An array of search and replace pairs.""
      }
    },
    ""required"": [""path"", ""replacements""],
    ""title"": ""FindAndReplaceArguments"",
    ""type"": ""object""
  }
}",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "excludedFileExtensions (CSV)", "" },
                    { "excludedFilePrefixes (CSV)", "" }
                }
            };
        }

        /// <summary>
        /// Represents a search and replace pair
        /// </summary>
        private class ReplacementPair
        {
            public string Search { get; set; }
            public string Replace { get; set; }
            public bool CaseSensitive { get; set; }

            public StringComparison ComparisonType => CaseSensitive ? 
                StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        }

        /// <summary>
        /// Represents a file modification result
        /// </summary>
        private class FileModificationResult
        {
            public string FilePath { get; set; }
            public int ReplacementsCount { get; set; }
            public List<string> ModifiedLines { get; set; } = new List<string>();
            public Dictionary<string, int> ReplacementCounts { get; set; } = new Dictionary<string, int>();
        }

        /// <summary>
        /// Recursively searches and replaces text in files within a directory
        /// </summary>
        private void ProcessFilesRecursively(
            string rootSearchPath, 
            string currentPath, 
            int remainingDepth, 
            List<ReplacementPair> replacements, 
            GitIgnoreFilterManager gitIgnoreFilter, 
            bool previewOnly,
            List<FileModificationResult> results)
        {
            // Base case: Check depth limitations
            int initialDepth = parameters.ContainsKey("depth") ? Convert.ToInt32(parameters["depth"]) : 0;
            if (initialDepth > 0 && remainingDepth < 0)
            {
                return;
            }

            // --- Process Files in Current Directory ---
            try
            {
                var excludedExtensionsCsv = _extraProperties.TryGetValue("ExcludedFileExtensions (CSV)", out var extCsv) ? extCsv : 
                                            _extraProperties.TryGetValue("excludedFileExtensions (CSV)", out var extCsv2) ? extCsv2 : string.Empty;
                var excludedPrefixesCsv = _extraProperties.TryGetValue("ExcludedFilePrefixes (CSV)", out var preCsv) ? preCsv : 
                                          _extraProperties.TryGetValue("excludedFilePrefixes (CSV)", out var preCsv2) ? preCsv2 : string.Empty;
                
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
                    
                    // Exclude by extension
                    if (excludedExtensions.Contains(fileExt))
                        continue;
                    
                    // Exclude by prefix
                    if (excludedPrefixes.Any(prefix => fileName.StartsWith(prefix)))
                        continue;

                    // Process the file
                    try
                    {
                        Debug.WriteLine("FindAndReplace checking " + filePath);
                        ProcessFile(filePath, replacements, previewOnly, results);
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
                        _logger.LogError(fileEx, "Unexpected error processing file {FilePath}. Skipping.", filePath);
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
            if (initialDepth == 0 || remainingDepth > 0)
            {
                try
                {
                    foreach (var dirPath in Directory.EnumerateDirectories(currentPath))
                    {
                        // Skip common large directories
                        if (dirPath.EndsWith("node_modules") || dirPath.EndsWith("bin") || 
                            dirPath.EndsWith("dist") || dirPath.EndsWith("obj"))
                            continue;
                        
                        // Check if directory is ignored by .gitignore
                        if (gitIgnoreFilter != null && gitIgnoreFilter.PathIsIgnored(dirPath + Path.DirectorySeparatorChar))
                        {
                            continue;
                        }

                        // Recurse with decremented depth if depth is limited
                        ProcessFilesRecursively(
                            rootSearchPath, 
                            dirPath, 
                            initialDepth > 0 ? remainingDepth - 1 : 0, 
                            replacements, 
                            gitIgnoreFilter, 
                            previewOnly,
                            results);
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

        /// <summary>
        /// Process a single file for find and replace operations
        /// </summary>
        private void ProcessFile(string filePath, List<ReplacementPair> replacements, bool previewOnly, List<FileModificationResult> results)
        {
            // Read the file content
            string content = File.ReadAllText(filePath);
            string originalContent = content;
            bool fileModified = false;
            
            var result = new FileModificationResult { FilePath = filePath };
            
            // Process each line separately to track line numbers and show context
            var lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string originalLine = lines[i];
                string modifiedLine = originalLine;
                
                foreach (var replacement in replacements)
                {
                    // Skip if the search term isn't in the line (optimization)
                    if (!ContainsText(modifiedLine, replacement.Search, replacement.ComparisonType))
                        continue;
                    
                    // Count occurrences before replacement
                    int count = CountOccurrences(modifiedLine, replacement.Search, replacement.ComparisonType);
                    if (count > 0)
                    {
                        // Perform the replacement
                        modifiedLine = ReplaceText(modifiedLine, replacement.Search, replacement.Replace, replacement.ComparisonType);
                        
                        // Update counts
                        result.ReplacementsCount += count;
                        if (!result.ReplacementCounts.ContainsKey(replacement.Search))
                            result.ReplacementCounts[replacement.Search] = 0;
                        result.ReplacementCounts[replacement.Search] += count;
                        
                        fileModified = true;
                    }
                }
                
                // If the line was modified, add it to the results
                if (originalLine != modifiedLine)
                {
                    result.ModifiedLines.Add($"Line {i+1}:\n- {originalLine}\n+ {modifiedLine}");
                    lines[i] = modifiedLine; // Update the line for actual replacement
                }
            }
            
            // If we found replacements, add the result
            if (fileModified)
            {
                // Only write the file if not in preview mode
                if (!previewOnly)
                {
                    File.WriteAllLines(filePath, lines);
                }
                
                results.Add(result);
            }
        }
        
        /// <summary>
        /// Check if a string contains a substring using the specified comparison
        /// </summary>
        private bool ContainsText(string source, string search, StringComparison comparison)
        {
            return source.IndexOf(search, comparison) >= 0;
        }
        
        /// <summary>
        /// Count occurrences of a substring in a string using the specified comparison
        /// </summary>
        private int CountOccurrences(string source, string search, StringComparison comparison)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(search, index, comparison)) >= 0)
            {
                count++;
                index += search.Length;
            }
            return count;
        }
        
        /// <summary>
        /// Replace all occurrences of a substring in a string using the specified comparison
        /// </summary>
        private string ReplaceText(string source, string search, string replacement, StringComparison comparison)
        {
            if (comparison == StringComparison.Ordinal)
                return source.Replace(search, replacement);
                
            StringBuilder result = new StringBuilder();
            int currentIndex = 0;
            int matchIndex;
            
            // Find and replace all occurrences
            while ((matchIndex = source.IndexOf(search, currentIndex, comparison)) >= 0)
            {
                // Append everything up to the match
                result.Append(source.Substring(currentIndex, matchIndex - currentIndex));
                // Append the replacement
                result.Append(replacement);
                // Move past this match
                currentIndex = matchIndex + search.Length;
            }
            
            // Append any remaining text
            if (currentIndex < source.Length)
                result.Append(source.Substring(currentIndex));
                
            return result.ToString();
        }

        // Helper to get parameters safely, needed for depth check inside recursive function
        private Dictionary<string, object> parameters = new Dictionary<string, object>();

        // Override ProcessAsync to store parameters before calling the recursive function
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _extraProperties = extraProperties;

            try
            {
                SendStatusUpdate("Starting FindAndReplace tool execution...");
                parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters) ?? new Dictionary<string, object>();

                // Now call the main logic which uses this.parameters
                return ProcessFindAndReplaceInternal(toolParameters);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error deserializing FindAndReplace parameters");
                return Task.FromResult(CreateResult(true, true, $"Error processing FindAndReplace tool parameters: Invalid JSON format. {jsonEx.Message}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during parameter setup for FindAndReplace tool");
                return Task.FromResult(CreateResult(true, true, $"Critical error setting up FindAndReplace tool: {ex.Message}"));
            }
        }

        // Main processing logic
        private Task<BuiltinToolResult> ProcessFindAndReplaceInternal(string toolParameters)
        {
            List<FileModificationResult> results = new List<FileModificationResult>();
            try
            {
                // --- Extract Parameters ---
                var path = parameters.ContainsKey("path") ? parameters["path"].ToString() : string.Empty;
                var depth = parameters.ContainsKey("depth") ? Convert.ToInt32(parameters["depth"]) : 0;
                var includeFiltered = parameters.ContainsKey("include_filtered") ? Convert.ToBoolean(parameters["include_filtered"]) : false;
                var previewOnly = parameters.ContainsKey("preview_only") ? Convert.ToBoolean(parameters["preview_only"]) : true;
                
                List<ReplacementPair> replacements = new List<ReplacementPair>();
                
                if (parameters.TryGetValue("replacements", out var replacementsObj) && replacementsObj is JArray replacementsArray)
                {
                    foreach (JObject item in replacementsArray)
                    {
                        if (item.TryGetValue("search", out var searchToken) && 
                            item.TryGetValue("replace", out var replaceToken))
                        {
                            bool caseSensitive = false;
                            if (item.TryGetValue("case_sensitive", out var caseToken))
                                caseSensitive = caseToken.Value<bool>();
                                
                            replacements.Add(new ReplacementPair
                            {
                                Search = searchToken.Value<string>(),
                                Replace = replaceToken.Value<string>(),
                                CaseSensitive = caseSensitive
                            });
                        }
                    }
                }

                // --- Validation ---
                if (string.IsNullOrWhiteSpace(path))
                {
                    SendStatusUpdate("Error: 'path' parameter is required.");
                    return Task.FromResult(CreateResult(true, true, "Error: 'path' parameter is required."));
                }
                
                if (replacements.Count == 0)
                {
                    SendStatusUpdate("Error: 'replacements' parameter must contain at least one valid search/replace pair.");
                    return Task.FromResult(CreateResult(true, true, "Error: 'replacements' parameter must contain at least one valid search/replace pair."));
                }
                
                var searchPath = Path.GetFullPath(Path.Combine(_projectRoot, path));
                if (!searchPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    SendStatusUpdate("Error: Path is outside the allowed directory.");
                    return Task.FromResult(CreateResult(true, true, "Error: Path is outside the allowed directory."));
                }
                
                if (!Directory.Exists(searchPath))
                {
                    string errorMessage = $"Error: Directory not found: {searchPath}";
                    SendStatusUpdate(errorMessage);
                    return Task.FromResult(CreateResult(true, true, errorMessage));
                }
                
                // Log what we're doing
                StringBuilder operationDescription = new StringBuilder();
                operationDescription.AppendLine($"Operation: {(previewOnly ? "Preview" : "Replace")} in {Path.GetFileName(searchPath)}");
                operationDescription.AppendLine("Replacements:");
                foreach (var replacement in replacements)
                {
                    operationDescription.AppendLine($"- '{replacement.Search}' → '{replacement.Replace}' (Case {(replacement.CaseSensitive ? "sensitive" : "insensitive")})");
                }
                
                SendStatusUpdate(operationDescription.ToString());

                // --- GitIgnore Setup ---
                GitIgnoreFilterManager gitIgnoreFilterManager = null;
                if (!includeFiltered)
                {
                    string currentIgnorePath = searchPath;
                    string gitIgnoreFilePath = null;
                    while (currentIgnorePath != null && currentIgnorePath.Length >= _projectRoot.Length && 
                           currentIgnorePath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
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

                // --- Perform Find and Replace ---
                SendStatusUpdate($"Beginning find and replace with depth: {depth}...");
                ProcessFilesRecursively(searchPath, searchPath, depth, replacements, gitIgnoreFilterManager, previewOnly, results);

                // --- Format Result ---
                if (results.Any())
                {
                    StringBuilder resultText = new StringBuilder();
                    resultText.AppendLine($"{(previewOnly ? "Preview of changes" : "Changes made")} in {results.Count} files:");
                    resultText.AppendLine();
                    
                    int totalReplacements = results.Sum(r => r.ReplacementsCount);
                    resultText.AppendLine($"Total replacements: {totalReplacements}");
                    resultText.AppendLine();
                    
                    // Summarize replacements by search term
                    resultText.AppendLine("Replacement counts by search term:");
                    var allReplacementCounts = new Dictionary<string, int>();
                    foreach (var result in results)
                    {
                        foreach (var kvp in result.ReplacementCounts)
                        {
                            if (!allReplacementCounts.ContainsKey(kvp.Key))
                                allReplacementCounts[kvp.Key] = 0;
                            allReplacementCounts[kvp.Key] += kvp.Value;
                        }
                    }
                    
                    foreach (var kvp in allReplacementCounts)
                    {
                        resultText.AppendLine($"- '{kvp.Key}': {kvp.Value} occurrences");
                    }
                    resultText.AppendLine();
                    
                    // Show details for each file
                    foreach (var result in results)
                    {
                        resultText.AppendLine($"File: {result.FilePath}");
                        resultText.AppendLine($"Replacements: {result.ReplacementsCount}");
                        
                        // Show the first 10 modified lines for context
                        int linesToShow = Math.Min(10, result.ModifiedLines.Count);
                        for (int i = 0; i < linesToShow; i++)
                        {
                            resultText.AppendLine(result.ModifiedLines[i]);
                        }
                        
                        if (result.ModifiedLines.Count > linesToShow)
                        {
                            resultText.AppendLine($"... and {result.ModifiedLines.Count - linesToShow} more changes");
                        }
                        
                        resultText.AppendLine();
                    }
                    
                    SendStatusUpdate($"{(previewOnly ? "Preview" : "Operation")} completed. Found replacements in {results.Count} files.");
                    return Task.FromResult(CreateResult(true, true, resultText.ToString()));
                }
                else
                {
                    SendStatusUpdate("Operation completed. No replacements found.");
                    return Task.FromResult(CreateResult(true, true, "No replacements found for the specified search terms."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing FindAndReplace tool");
                SendStatusUpdate($"Error processing FindAndReplace tool: {ex.Message}");
                return Task.FromResult(CreateResult(true, true, $"Error processing FindAndReplace tool: {ex.Message}"));
            }
        }
    }
}