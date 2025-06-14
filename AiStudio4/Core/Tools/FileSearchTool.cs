




 // Needed for JArray parsing
using SharedClasses.Git;








namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the FileSearch tool
    /// </summary>
    public class FileSearchTool : BaseToolImplementation
    {
        private readonly IProjectFileWatcherService _projectFileWatcherService;
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();


        public FileSearchTool(ILogger<FileSearchTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IProjectFileWatcherService projectFileWatcherService) : base(logger, generalSettingsService, statusMessageService)
        {
            _projectFileWatcherService = projectFileWatcherService ?? throw new ArgumentNullException(nameof(projectFileWatcherService));
        }

        /// <summary>
        /// Gets the FileSearch tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.FILE_SEARCH_TOOL_GUID,
                Name = "FileSearch",
                Description = "Searches for files containing specific terms within a directory tree.",
                Schema = """
{
  "name": "FileSearch",
  "description": "Recursively searches for files within a specified path that contain any of the provided search terms. Respects .gitignore rules by default.",
  "input_schema": {
    "properties": {
      "path": { "title": "Path", "type": "string", "description": "The path to the directory to start searching from (relative to project root)." },
      "depth": { "default": 0, "title": "Depth", "type": "integer", "description": "The maximum depth to search recursively (0 for unlimited)." },
      "search_terms": { "title": "Search Terms", "type": "array", "items": { "type": "string" }, "description": "An array of strings to search for within file content (case-insensitive)." }
    },
    "required": ["path", "search_terms"],
    "title": "FileSearchArguments",
    "type": "object"
  }
}
""",
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
        private void SearchFilesRecursively(string rootSearchPath, string currentPath, int remainingDepth, string[] searchTerms, List<string> results)
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

            string normalizedCurrentPath = currentPath.Replace("\\", "/").TrimEnd('/');

            // --- Process Files in Current Directory ---
            try
            {
                var excludedExtensionsCsv = _extraProperties.TryGetValue("ExcludedFileExtensions (CSV)", out var extCsv) ? extCsv : _extraProperties.TryGetValue("excludedFileExtensions (CSV)", out var extCsv2) ? extCsv2 : string.Empty;
                var excludedPrefixesCsv = _extraProperties.TryGetValue("ExcludedFilePrefixes (CSV)", out var preCsv) ? preCsv : _extraProperties.TryGetValue("excludedFilePrefixes (CSV)", out var preCsv2) ? preCsv2 : string.Empty;
                var excludedExtensions = excludedExtensionsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().ToLowerInvariant()).Where(e => e.StartsWith(".")).ToList();
                var excludedPrefixes = excludedPrefixesCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim().ToLowerInvariant()).Where(p => !string.IsNullOrEmpty(p)).ToList();

                // Use ProjectFileWatcherService.Files
                foreach (var filePath in _projectFileWatcherService.Files)
                {
                    string normalizedFilePath = filePath.Replace("\\", "/");
                    string fileDirectory = Path.GetDirectoryName(normalizedFilePath).Replace("\\", "/").TrimEnd('/');

                    if (fileDirectory != normalizedCurrentPath)
                        continue; // Only process files directly in the currentPath

                    // Gitignore is handled by ProjectFileWatcherService, so direct check is removed.

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
                    // Use ProjectFileWatcherService.Directories
                    foreach (var dirPath in _projectFileWatcherService.Directories)
                    {
                        string normalizedDirPath = dirPath.Replace("\\", "/").TrimEnd('/');
                        string parentOfDirPath = Path.GetDirectoryName(normalizedDirPath)?.Replace("\\", "/").TrimEnd('/');

                        if (parentOfDirPath != normalizedCurrentPath)
                            continue; // Only process direct child directories

                        // Hardcoded exclusions (ProjectFileWatcherService might already exclude if in .gitignore)
                        string dirName = Path.GetFileName(normalizedDirPath);
                        if (dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase) || 
                            dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) || 
                            dirName.Equals("dist", StringComparison.OrdinalIgnoreCase) || 
                            dirName.Equals("obj", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Gitignore is handled by ProjectFileWatcherService

                        // Recurse with decremented depth if depth is limited
                        SearchFilesRecursively(rootSearchPath, normalizedDirPath, initialDepth > 0 ? remainingDepth - 1 : 0, searchTerms, results);
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
                    string suggestion = FindAlternativeDirectory(searchPath);
                    string errorMessage = $"Error: Directory not found: {searchPath}";
                    if (!string.IsNullOrEmpty(suggestion)) {
                        errorMessage += $"\n{suggestion}";
                    }
                    SendStatusUpdate(errorMessage);
                    return Task.FromResult(CreateResult(true, true, errorMessage));
                }
                
                SendStatusUpdate($"Searching for terms: {string.Join(", ", validSearchTerms)} in {Path.GetFileName(searchPath)}...");

                // --- Perform Search ---
                SendStatusUpdate($"Beginning file search with depth: {depth}...");
                SearchFilesRecursively(searchPath, searchPath, depth, validSearchTerms, matchingFiles);


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
