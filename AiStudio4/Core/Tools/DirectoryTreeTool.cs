




using SharedClasses.Git;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;







namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the DirectoryTree tool
    /// </summary>
    [McpServerToolType]
    public class DirectoryTreeTool : BaseToolImplementation
    {
        private readonly IProjectFileWatcherService _projectFileWatcherService;
        private Dictionary<string, string> _extraProperties { get; set; } = new Dictionary<string, string>();
        public DirectoryTreeTool(ILogger<DirectoryTreeTool> logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IProjectFileWatcherService projectFileWatcherService) : base(logger, generalSettingsService, statusMessageService)
        {
            _projectFileWatcherService = projectFileWatcherService ?? throw new ArgumentNullException(nameof(projectFileWatcherService));
        }

        /// <summary>
        /// Gets the DirectoryTree tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = ToolGuids.DIRECTORY_TREE_TOOL_GUID,
                Name = "DirectoryTree",
                Description = "Gets a directory tree",
                Schema = """
{
  "name": "DirectoryTree",
  "description": "Get a recursive tree view of files and directories with customizable depth and filtering.\n\nReturns a structured view of the directory tree with files and subdirectories. Directories are marked with trailing slashes. The output is formatted as an indented list for readability. By default, common development directories like .git, node_modules, and venv are noted but not traversed unless explicitly requested. Only works within allowed directories.",
  "input_schema": {
    "properties": {
      "path": { "title": "Path", "type": "string", "description": "The path to the directory to view" },
      "depth": { "default": 3, "title": "Depth", "type": "integer", "description": "The maximum depth to traverse (0 for unlimited)" }
    },
    "required": ["path"],
    "title": "DirectoryTreeArguments",
    "type": "object"
  }
}
""",
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "txt",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow,
                ExtraProperties = new Dictionary<string, string> {
                    { "excludedFileExtensions (CSV)", "" }, //".cs" },
                    { "excludedDirectories (CSV)", "" }, //"build,bin,obj,node_modules,dist,.vs,PhysicalDeletes,logs" }
                }
            };
        }

        /// <summary>
        /// Processes a DirectoryTree tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            try
            {
                SendStatusUpdate("Starting DirectoryTree tool execution...");

                _extraProperties = extraProperties;
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                var path = parameters.ContainsKey("path") ? parameters["path"].ToString() : string.Empty; // Default to empty, resolve to _projectRoot if empty later
                var depth = parameters.ContainsKey("depth") ? Convert.ToInt32(parameters["depth"]) : 3; // Default from schema
                var searchPath = Path.GetFullPath(string.IsNullOrEmpty(path) ? _projectRoot : Path.Combine(_projectRoot, path));

                if (!searchPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    SendStatusUpdate("Error: Path is outside the allowed directory.");
                    return Task.FromResult(CreateResult(true, true, "Error: Path is outside the allowed directory."));
                }

                SendStatusUpdate($"Generating directory tree for: {searchPath} with depth {depth}...");

                if (!System.IO.Directory.Exists(searchPath)) // Use System.IO.Directory to avoid ambiguity
                {
                    string suggestion = FindAlternativeDirectory(searchPath);
                    string errorMessage = $"Error: Directory not found: {searchPath}";
                    if (!string.IsNullOrEmpty(suggestion)) errorMessage += $"\n{suggestion}";
                    SendStatusUpdate(errorMessage);
                    return Task.FromResult(CreateResult(true, true, errorMessage));
                }

                List<string> excludedDirNames = new List<string>();
                List<string> excludedExtensions = new List<string>();
                    var excludedDirsCsv = extraProperties.TryGetValue("ExcludedDirectories (CSV)", out var dirCsv) ? dirCsv : string.Empty;
                    excludedDirNames = excludedDirsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(d => d.Trim().ToLowerInvariant()).Where(d => !string.IsNullOrEmpty(d)).ToList();

                    var excludedExtensionsCsv = extraProperties.TryGetValue("ExcludedFileExtensions (CSV)", out var extCsv) ? extCsv : string.Empty;
                    excludedExtensions = excludedExtensionsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim().ToLowerInvariant()).Where(e => e.StartsWith(".")).ToList();
                
                var searchPathDirName = Path.GetFileName(searchPath.Replace("\\", "/").TrimEnd('/'));
                if (excludedDirNames.Contains(searchPathDirName.ToLowerInvariant()))
                {
                    var sbInfo = new StringBuilder();
                    sbInfo.AppendLine(searchPath.Replace("\\", "/").TrimEnd('/'));
                    sbInfo.AppendLine($"  (Directory '{searchPathDirName}' is excluded by tool's filter list)");
                    SendStatusUpdate($"Directory tree for {searchPathDirName} skipped as it's an excluded name by tool's filter list.");
                    return Task.FromResult(CreateResult(true, true, sbInfo.ToString()));
                }

                var itemsForTree = new List<string>();
                var normalizedSearchPath = searchPath.Replace("\\", "/").TrimEnd('/') + "/";

                // Process Directories from ProjectFileWatcherService
                foreach (var dirAbsPath in _projectFileWatcherService.Directories)
                {
                    var normalizedDirAbsPath = dirAbsPath.Replace("\\", "/").TrimEnd('/') + "/";
                    if (!normalizedDirAbsPath.StartsWith(normalizedSearchPath, StringComparison.OrdinalIgnoreCase)) continue;

                    var relativePath = normalizedDirAbsPath.Substring(normalizedSearchPath.Length);
                    var pathSegments = relativePath.TrimEnd('/').Split(new[]{'/'}, StringSplitOptions.RemoveEmptyEntries);

                    if (depth > 0 && pathSegments.Length > depth) continue; // Directory itself is deeper than max depth

                    bool isExcludedByName = false;
                    foreach (var segment in pathSegments)
                    {
                        if (excludedDirNames.Contains(segment.ToLowerInvariant()))
                        {
                            isExcludedByName = true;
                            break;
                        }
                    }
                    if (isExcludedByName) continue;

                    itemsForTree.Add(normalizedDirAbsPath);
                }

                // Process Files from ProjectFileWatcherService
                foreach (var fileAbsPath in _projectFileWatcherService.Files)
                {
                    var normalizedFileAbsPath = fileAbsPath.Replace("\\", "/");
                    if (!normalizedFileAbsPath.StartsWith(normalizedSearchPath, StringComparison.OrdinalIgnoreCase)) continue;

                    var relativePath = normalizedFileAbsPath.Substring(normalizedSearchPath.Length);
                    // pathSegments for file includes the filename itself as the last segment.
                    var pathSegments = relativePath.Split(new[]{'/'}, StringSplitOptions.RemoveEmptyEntries); 

                    // Depth for a file is its number of segments. 'file.txt' in searchPath is depth 1.
                    if (depth > 0 && pathSegments.Length > depth) continue; 

                    bool parentIsExcluded = false;
                    for (int i = 0; i < pathSegments.Length - 1; i++) // Check parent directory segments
                    {
                        if (excludedDirNames.Contains(pathSegments[i].ToLowerInvariant()))
                        {
                            parentIsExcluded = true;
                            break;
                        }
                    }
                    if (parentIsExcluded) continue;

                    var fileExt = Path.GetExtension(normalizedFileAbsPath).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(fileExt) && excludedExtensions.Contains(fileExt)) continue;

                    itemsForTree.Add(normalizedFileAbsPath);
                }
                
                var relativeItemPathsForTree = itemsForTree
                    .Select(absPath => absPath.Substring(normalizedSearchPath.Length))
                    .Distinct() // Ensure uniqueness, especially if a dir is listed and also inferred from a file path
                    .OrderBy(relPath => relPath)
                    .ToList();

                string prettyPrintedResult = GeneratePrettyFileTreeStandard(relativeItemPathsForTree, searchPath.Replace("\\", "/").TrimEnd('/'));
                SendStatusUpdate("Directory tree generated successfully.");
                return Task.FromResult(CreateResult(true, true, prettyPrintedResult));
            }
            catch (Exception ex)
            {
                string paramInfo = $"Parameters: {toolParameters}'";
                _logger.LogError(ex, $"Error processing DirectoryTree tool. {paramInfo}");
                SendStatusUpdate($"Error processing DirectoryTree tool: {ex.Message}. {paramInfo}");
                return Task.FromResult(CreateResult(true, true, $"Error processing DirectoryTree tool: {ex.Message}. {paramInfo}"));
            }
        }

        private static string GeneratePrettyFileTreeStandard(IEnumerable<string> relativeItemPaths, string displayRootName)
        {
            var fileTree = new StringBuilder();
            fileTree.AppendLine(displayRootName);

            List<string> previousPathParts = new List<string>();

            foreach (var relativePath in relativeItemPaths)
            {
                if (string.IsNullOrEmpty(relativePath)) continue; // Skip if somehow an empty relative path made it

                bool isDirectory = relativePath.EndsWith("/");
                var pathParts = relativePath.TrimEnd('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                
                var itemName = pathParts.LastOrDefault() ?? ""; 
                
                // Determine segments representing the directory structure for the current item
                var dirPathSegments = isDirectory ? pathParts : pathParts.Take(pathParts.Length - 1).ToArray();

                int commonPrefixLength = 0;
                while (commonPrefixLength < previousPathParts.Count &&
                       commonPrefixLength < dirPathSegments.Length &&
                       previousPathParts[commonPrefixLength] == dirPathSegments[commonPrefixLength])
                {
                    commonPrefixLength++;
                }

                // Print new directory levels
                for (int i = commonPrefixLength; i < dirPathSegments.Length; i++)
                {
                    fileTree.AppendLine($"{new string(' ', i * 2)}> {dirPathSegments[i]}/");
                }
                
                // Print file name if it's a file
                if (!isDirectory)
                {
                    fileTree.AppendLine($"{new string(' ', dirPathSegments.Length * 2)} {itemName}");
                }
                // If it's a directory and it's empty, its structure up to its name was printed by the loop above.
                // If a directory is explicitly in relativeItemPaths (e.g. "dir1/"), and it's empty or contains other items,
                // its name up to the trailing slash should have been printed by the dirPathSegments loop.

                previousPathParts = dirPathSegments.ToList(); // Update based on the directory structure processed
            }

            return fileTree.ToString();
        }

        [McpServerTool, Description("Gets a directory tree")]
        public async Task<string> DirectoryTree([Description("JSON parameters for DirectoryTree")] string parameters = "{}")
        {
            try
            {
                var result = await ProcessAsync(parameters, new Dictionary<string, string>());
                
                if (!result.WasProcessed)
                {
                    return $"Tool was not processed successfully.";
                }
                
                return result.ResultMessage ?? "Tool executed successfully with no output.";
            }
            catch (Exception ex)
            {
                return $"Error executing tool: {ex.Message}";
            }
        }
    }
}
