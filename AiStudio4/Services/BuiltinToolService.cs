using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedClasses.Git;
using System.Windows.Forms;
using System.IO;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AiStudio4.Services
{
    public class BuiltinToolService : IBuiltinToolService
    {
        private readonly ILogger<BuiltinToolService> _logger;

        public BuiltinToolService(ILogger<BuiltinToolService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public List<Tool> GetBuiltinTools()
        {
            // Define default tools here with fixed GUIDs
            return new List<Tool>
            { 
                CodeDiffTool(),
                StopTool(),
                ReadFileTool(),
                ThinkingTool(),
                DirectoryTreeTool(),
                // Add more builtin tools here as needed, using fixed GUIDs
            };
        }

        private static Tool ReadFileTool()
        {
            return new Tool
            {
                Guid = "b2c3d4e5-f6a7-8901-2345-67890abcdef05", // New GUID for ReadFile
                Name = "ReadFile",
                Description = "Read the contents of one or multiple files.",
                Schema = @"{
  ""name"": ""ReadFile"",
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
                    Categories = new List<string> { "File IO" },
                    Filetype = string.Empty, // Ensure initialized
                    LastModified = DateTime.UtcNow
};
            }

        private static Tool DirectoryTreeTool()
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
                Categories = new List<string> { "Development" },
                Filetype = string.Empty, // Ensure initialized
                LastModified = DateTime.UtcNow
            };
        }

        private static Tool ThinkingTool()
        {
            return new Tool
            {
                Guid = "b2c3d4e5-f6a7-8901-2345-67890abcdef03", 
                Name = "Think",
                Description = "Use the tool to think about something.\r\n\r\nIt will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed. For example, if you explore the repo and discover the source of a bug, call this tool to brainstorm several unique ways of fixing the bug, and assess which change(s) are likely to be simplest and most effective. Alternatively, if you receive some test results, call this tool to brainstorm ways to fix the failing tests.",
                Schema = @"{
  ""name"": ""Think"",
  ""description"": ""Use the tool to think about something.

It will not obtain new information or make any changes to the repository, but just log the thought. Use it when complex reasoning or brainstorming is needed. For example, if you explore the repo and discover the source of a bug, call this tool to brainstorm several unique ways of fixing the bug, and assess which change(s) are likely to be simplest and most effective. Alternatively, if you receive some test results, call this tool to brainstorm ways to fix the failing tests."",
  ""input_schema"": {
                ""properties"": {
                ""thought"": {
                    ""title"": ""Thought"",
                    ""type"": ""string""
                }
            },
            ""required"": [""thought""],
            ""title"": ""thinkArguments"",
            ""type"": ""object""
  }
}",
                Categories = new List<string> { "Development" },
                Filetype = string.Empty, // Ensure initialized
                LastModified = DateTime.UtcNow
            };
        }

        private static Tool StopTool()
        {
            return new Tool
            {
                Guid = "b2c3d4e5-f6a7-8901-2345-67890abcdef01", // Fixed GUID for Stop
                Name = "Stop",
                Description = "A tool which allows you to indicate that all outstanding tasks are completed, or you cannot proceed any further",
                Schema = @"{
  ""name"": ""Stop"",
  ""description"": ""A tool which allows you to indicate that all outstanding tasks are completed"",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""param"": {
        ""type"": ""string"",
        ""description"": ""Information to the user goes here""
      }
    }
  }
}",
                Categories = new List<string> { "Development" },
                Filetype = string.Empty, // Ensure initialized
                LastModified = DateTime.UtcNow
            };
        }

        private static Tool CodeDiffTool()
        {
            return new Tool
            {
                Guid = "a1b2c3d4-e5f6-7890-1234-567890abcdef", // Fixed GUID for CodeDiff
                Description = "Allows you to specify edits, file creations and deletions",
                Name = "CodeDiff",
                Schema = @"{
  ""name"": ""CodeDiff"",
  ""description"": ""Allows you to specify an array of changes to make up a complete code or ASCII file diff, and includes a description of those changes. You must NEVER double-escape content in this diff."",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""changeset"": {
        ""type"": ""object"",
        ""description"": """",
        ""properties"": {
          ""description"": {
            ""type"": ""string"",
            ""description"": ""A description of this changeset""
          },
          ""files"": {
            ""type"": ""array"",
            ""description"": """",
            ""items"": {
              ""type"": ""object"",
              ""properties"": {
                ""path"": {
                  ""type"": ""string"",
                  ""description"": ""The original filename and ABSOLUTE path where the changes are to occur""
                },
                ""changes"": {
                  ""type"": ""array"",
                  ""description"": """",
                  ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""lineNumber"": {
                        ""type"": ""integer"",
                        ""description"": ""The line number where the change starts, adjusted for previous changes in this changeset (not used for file creation, replacement, renaming, or deletion)""
                      },
                      ""change_type"": {
                        ""type"": ""string"",
                        ""description"": ""The type of change that occurred"",
                        ""enum"": [
                          ""addToFile"",
                          ""deleteFromFile"",
                          ""modifyFile"",
                          ""createnewFile"",
                          ""replaceFile"",
                          ""renameFile"",
                          ""deleteFile""
                        ]
                      },
                      ""oldContent"": {
                        ""type"": ""string"",
                        ""description"": ""The lines that were removed or modified (ignored for createFile, replaceFile, renameFile, and deleteFile)""
                      },
                      ""newContent"": {
                        ""type"": ""string"",
                        ""description"": ""The lines that were added, modified, or created (for replaceFile, this contains the entire new file content; for renameFile, this contains the new file path)""
                      },
                      ""description"": {
                        ""type"": ""string"",
                        ""description"": ""A human-readable explanation of this specific change""
                      }
                    },
                    ""required"": [
                      ""change_type"",
                      ""oldContent"",
                      ""newContent""
                    ]
                  }
                }
              },
              ""required"": [
                ""path"",
                ""changes""
              ]
            }
          }
        },
        ""required"": [
          ""description"",
          ""files""
        ]
      }
    },
    ""required"": [
      ""changeset""
    ]
  }
}",
                Categories = new List<string> { "Development" }, // Assign appropriate category
                Filetype = string.Empty, // Ensure initialized
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a built-in tool call and determines if it requires special handling
        /// </summary>
        /// <param name="toolName">Name of the tool being called</param>
        /// <param name="toolParameters">The parameters passed to the tool</param>
        /// <returns>Result indicating if the tool was processed and if further processing should continue</returns>
        public async Task<BuiltinToolResult> ProcessBuiltinToolAsync(string toolName, string toolParameters)
        {
            // Create default result assuming the tool is not built-in or doesn't need special processing
            var result = new BuiltinToolResult
            {
                WasProcessed = false,
                ContinueProcessing = true
            };
            try
            {
                // Check for built-in tools that need special handling
                if (toolName.Equals("Stop", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("'Stop' tool called, signalling processing termination");
                    result.WasProcessed = true;
                    result.ContinueProcessing = false;

                    // Optionally parse the parameters to extract the message
                    try
                    {
                        if (!string.IsNullOrEmpty(toolParameters))
                        {
                            dynamic stopParams = JsonConvert.DeserializeObject(toolParameters);
                            if (stopParams?.param != null)
                            {
                                result.ResultMessage = $"Stop tool called with message: {stopParams.param}";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing Stop tool parameters");
                        result.ResultMessage = "Stop tool called (parameters could not be parsed)";
                    }
                }
                if (toolName.Equals("DirectoryTree", StringComparison.OrdinalIgnoreCase))
                {
                    var projectRoot = "C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\AiStudio4";

                    var parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(toolParameters);

 
                    var searchPath = projectRoot;
                    var searchDepth = 2;

                    var files = GetFilesRecursively(searchPath, searchDepth);

                    // First check for .gitignore in the project root
                    var gitIgnorePath = Path.Combine(projectRoot, ".gitignore");
                    // If not found, try one level higher
                    if (!File.Exists(gitIgnorePath))
                    {
                        var parentDirectory = Directory.GetParent(projectRoot)?.FullName;
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

                    var prettyPrintedResult = GeneratePrettyFileTree(files, searchPath);

                    return new BuiltinToolResult { ContinueProcessing = true, ResultMessage = prettyPrintedResult, WasProcessed = true };

                }
                else if (toolName.Equals("ReadFile", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("ReadFile tool called");
                    var projectRoot = "C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\AiStudio4"; // Base path for security - adjust as needed
                    var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);
                    var pathsObject = parameters["paths"];
                    List<string> pathsToRead = new List<string>();

                    if (pathsObject is string singlePath)
                    {
                        pathsToRead.Add(singlePath);
                    }
                    else if (pathsObject is Newtonsoft.Json.Linq.JArray pathArray)
                    {
                        pathsToRead.AddRange(pathArray.Select(p => (string)p));
                    }
                    else
                    {
                        throw new ArgumentException("Invalid format for 'paths' parameter. Expected string or array of strings.");
                    }

                    var resultBuilder = new StringBuilder();
                    foreach (var relativePath in pathsToRead)
                    {
                        // IMPORTANT SECURITY CHECK: Ensure the path is within the project root or allowed directories.
                        // For now, we'll combine with projectRoot, assuming relative paths are intended,
                        // but the description asks for absolute paths. This needs clarification/robust implementation.
                        // Let's assume for now the paths provided ARE relative to projectRoot for safety.
                        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));

                        // Security check: Ensure the resolved path is still within the project root directory.
                        if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
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

                    result.WasProcessed = true;
                    result.ContinueProcessing = true; // Reading files doesn't stop the workflow
                    result.ResultMessage = resultBuilder.ToString();
                    return result; // Return immediately after processing ReadFile
                }

                // Add cases for other built-in tools here as they are implemented
                // else if (toolName.Equals("AnotherBuiltinTool", StringComparison.OrdinalIgnoreCase)) { ... }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing built-in tool {ToolName}", toolName);
                result.ResultMessage = $"Error processing built-in tool: {ex.Message}";
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Recursively fetches a list of files from the specified path up to the given depth.
        /// </summary>
        /// <param name="searchPath">The root directory to start searching from.</param>
        /// <param name="searchDepth">How many levels deep to search (0 means only the root directory).</param>
        /// <returns>A List<string> containing the fully-qualified file paths.</returns>
        public static List<string> GetFilesRecursively(string searchPath, int searchDepth)
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
        /// <param name="files">List of absolute file paths</param>
        /// <param name="rootDirectory">The root directory to make paths relative to</param>
        /// <returns>A formatted string showing the hierarchical file structure</returns>
        private static string GeneratePrettyFileTree(IEnumerable<string> files, string rootDirectory)
        {
            // Convert to relative paths and sort
            var relativeFiles = files
                .Select(file => Path.GetRelativePath(rootDirectory, file))
                .OrderBy(path => path)
                .ToList();

            var fileTree = new StringBuilder();
            fileTree.AppendLine(rootDirectory); ;

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
                    fileTree.AppendLine($"{new string(' ', i * 2)}└─ {dirParts[i]}/");
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