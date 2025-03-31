using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    /// Implementation of the DirectoryTree tool
    /// </summary>
    public class DirectoryTreeTool : BaseToolImplementation
    {
        private readonly string _projectRoot;

        public DirectoryTreeTool(ILogger<CodeDiffTool> logger, ISettingsService settingsService) : base(logger, settingsService)
        {
        }

        /// <summary>
        /// Gets the DirectoryTree tool definition
        /// </summary>
        public override Tool GetToolDefinition()
        {
            return new Tool
            {
                Guid = "b2c3d4e5-f6a7-8901-2345-67890abcdef04",
                Name = "DirectoryTree",
                Description = "Gets a directory tree",
                Schema = @"{
  ""name"": ""DirectoryTree"",
  ""description"": ""Get a recursive tree view of files and directories with customizable depth and filtering.

Returns a structured view of the directory tree with files and subdirectories. Directories are marked with trailing slashes. The output is formatted as an indented list for readability. By default, common development directories like .git, node_modules, and venv are noted but not traversed unless explicitly requested. Only works within allowed directories."",
  ""input_schema"": {
                ""properties"": {
""path"": {
                    ""title"": ""Path"",
                    ""type"": ""string"",
                    ""description"":""The path to the directory to view""
                },
 ""depth"": {
                    ""default"": 3,
                    ""title"": ""Depth"",
                    ""type"": ""integer"",
                    ""description"": ""The maximum depth to traverse (0 for unlimited)""
                },
""include_filtered"": {
                    ""default"": ""False"",
                    ""title"": ""Include Filtered"",
                    ""type"": ""boolean"",
                    ""description"": ""Include directories that are normally filtered""
                }
            },
           ""required"": [""path""],
            ""title"": ""DirectoryTreeArguments"",
            ""type"": ""object""
  }
}",
                Categories = new List<string> { "MaxCode" },
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a DirectoryTree tool call
        /// </summary>
        public override Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);

                // Extract parameters with defaults
                var path = parameters.ContainsKey("path") ? parameters["path"].ToString() : _projectRoot;
                var depth = parameters.ContainsKey("depth") ? Convert.ToInt32(parameters["depth"]) : 2;
                var includeFiltered = parameters.ContainsKey("include_filtered") ? Convert.ToBoolean(parameters["include_filtered"]) : false;

                // Get the search path (relative to project root for security)
                var searchPath = _projectRoot;
                if (!string.IsNullOrEmpty(path) && path != _projectRoot)
                {
                    searchPath = Path.GetFullPath(Path.Combine(_projectRoot, path));
                    if (!searchPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(CreateResult(true, true, "Error: Path is outside the allowed directory."));
                    }
                }

                // Get files recursively
                var files = GetFilesRecursively(searchPath, depth);

                // Apply gitignore filtering if not including filtered files
                if (!includeFiltered)
                {
                    // First check for .gitignore in the project root
                    var gitIgnorePath = Path.Combine(_projectRoot, ".gitignore");
                    // If not found, try one level higher
                    if (!File.Exists(gitIgnorePath))
                    {
                        var parentDirectory = Directory.GetParent(_projectRoot)?.FullName;
                        if (parentDirectory != null)
                        {
                            gitIgnorePath = Path.Combine(parentDirectory, ".gitignore");
                        }
                    }

                    // Only apply gitignore filtering if we found a .gitignore file
                    if (File.Exists(gitIgnorePath))
                    {
                        var gitignore = File.ReadAllText(gitIgnorePath);
                        var gitIgnoreFilterManager = new GitIgnoreFilterManager(gitignore);
                        files = gitIgnoreFilterManager.FilterNonIgnoredPaths(files);
                    }
                }

                // Generate pretty file tree
                var prettyPrintedResult = GeneratePrettyFileTree(files, searchPath);
                return Task.FromResult(CreateResult(true, true, prettyPrintedResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DirectoryTree tool");
                return Task.FromResult(CreateResult(true, true, $"Error processing DirectoryTree tool: {ex.Message}"));
            }
        }

        /// <summary>
        /// Recursively fetches a list of files from the specified path up to the given depth.
        /// </summary>
        private static List<string> GetFilesRecursively(string searchPath, int searchDepth)
        {
            var fileList = new List<string>();

            // Base case: If the directory doesn't exist or we've reached an invalid depth
            if (!Directory.Exists(searchPath) || searchDepth < 0)
            {
                return fileList;
            }

            try
            {
                // Add all files in the current directory
                fileList.AddRange(Directory.GetFiles(searchPath));

                // If we haven't reached the maximum depth, continue recursively
                if (searchDepth > 0)
                {
                    foreach (var directory in Directory.GetDirectories(searchPath))
                    {
                        // Recursively get files from subdirectories with a decremented depth
                        fileList.AddRange(GetFilesRecursively(directory, searchDepth - 1));
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions (like access denied)
                Console.WriteLine($"Error accessing {searchPath}: {ex.Message}");
            }

            return fileList;
        }

        /// <summary>
        /// Generates a pretty-printed tree representation of file paths relative to a root directory.
        /// </summary>
        private static string GeneratePrettyFileTree(IEnumerable<string> files, string rootDirectory)
        {
            // Convert to relative paths and sort
            var relativeFiles = files
                .Select(file => Path.GetRelativePath(rootDirectory, file))
                .OrderBy(path => path)
                .ToList();

            var fileTree = new StringBuilder();
            fileTree.AppendLine(rootDirectory);

            // Track the last displayed directory structure
            List<string> previousPathParts = new List<string>();

            foreach (var relativePath in relativeFiles)
            {
                var pathParts = relativePath.Split(Path.DirectorySeparatorChar);
                var fileName = pathParts[pathParts.Length - 1];
                var dirParts = pathParts.Take(pathParts.Length - 1).ToList();

                // Compare with previous path to determine which directories to show
                int commonDirLength = 0;
                while (commonDirLength < previousPathParts.Count &&
                       commonDirLength < dirParts.Count &&
                       previousPathParts[commonDirLength] == dirParts[commonDirLength])
                {
                    commonDirLength++;
                }

                // Display new directories that weren't displayed before
                for (int i = commonDirLength; i < dirParts.Count; i++)
                {
                    fileTree.AppendLine($"{new string(' ', i * 2)}?? {dirParts[i]}/");
                }

                // Display the file
                fileTree.AppendLine($"{new string(' ', dirParts.Count * 2)}• {fileName}");

                // Update previous path parts for next comparison
                previousPathParts = dirParts;
            }

            return fileTree.ToString();
        }
    }
}