using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the CodeDiff tool that automatically applies code changes with safety guardrails
    /// </summary>
    public class CodeDiffTool : BaseToolImplementation
    {
        private readonly StringBuilder _errorMessages;

        public CodeDiffTool(ILogger<CodeDiffTool> logger, ISettingsService settingsService) : base(logger, settingsService)
        {
            _errorMessages = new StringBuilder();
        }

        /// <summary>
        /// Gets the CodeDiff tool definition
        /// </summary>
        public override Tool GetToolDefinition()
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
                        ""description"": ""The line number where the change starts, adjusted for previous changes in this changeset (set to zero for file creation, replacement, renaming, or deletion)""
                      },
                      ""change_type"": {
                        ""type"": ""string"",
                        ""description"": ""The type of change that occurred"",
                        ""enum"": [
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
                      ""newContent"",
                      ""lineNumber""
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
                Categories = new List<string> { "MaxCode" },
                OutputFileType = "json",
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a CodeDiff tool call and applies all changes automatically
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            bool success = true;
            _errorMessages.Clear(); // Reset error messages for this new changeset

            try
            {               
                JObject parameters = JObject.Parse(toolParameters);
                if (parameters["changeset"] == null)
                {
                    _errorMessages.AppendLine("Error: Missing 'changeset' object in parameters.");
                    return CreateResult(false, false, _errorMessages.ToString());
                }

                var changeset = parameters["changeset"] as JObject;
                if (changeset == null)
                {
                    _errorMessages.AppendLine("Error: 'changeset' is not a valid JSON object.");
                    return CreateResult(false, false, _errorMessages.ToString());
                }

                string changesetDescription = changeset["description"]?.ToString() ?? "No description provided";
                _logger.LogInformation($"Processing changeset: {changesetDescription}");

                var files = changeset["files"] as JArray;
                if (files == null || !files.Any())
                {
                    _errorMessages.AppendLine("Error: 'files' array is missing or empty.");
                    return CreateResult(false, false, _errorMessages.ToString());
                }

                foreach (var fileObj in files)
                {
                    string filePath = fileObj["path"]?.ToString();
                    if (string.IsNullOrEmpty(filePath))
                    {
                        _errorMessages.AppendLine("Error: File path is missing or empty.");
                        success = false;
                        continue;
                    }

                    // Security check: Ensure the file path is within the project root
                    if (!IsPathWithinProjectRoot(filePath))
                    {
                        _errorMessages.AppendLine($"Security Error: The file path '{filePath}' is outside the project root. Access denied.");
                        success = false;
                        continue;
                    }

                    var changes = fileObj["changes"] as JArray;
                    if (changes == null || !changes.Any())
                    {
                        _errorMessages.AppendLine($"Error: No changes specified for file '{filePath}'.");
                        success = false;
                        continue;
                    }

                    // Process all changes for this file
                    if (!await ProcessFileChangesAsync(filePath, changes))
                    {
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Unexpected error: {ex.Message}");
                success = false;
            }
            
            MessageBox.Show(success ? "All changes applied successfully." : _errorMessages.ToString());
            string resultMessage = success ? "All changes applied successfully." : _errorMessages.ToString();
            return CreateResult(success, false, toolParameters);
        }

        /// <summary>
        /// Process all changes for a specific file
        /// </summary>
        private async Task<bool> ProcessFileChangesAsync(string filePath, JArray changes)
        {
            bool success = true;
            string directoryPath = Path.GetDirectoryName(filePath);

            // Process all changes for this file
            foreach (var change in changes)
            {
                string changeType = change["change_type"]?.ToString();
                if (string.IsNullOrEmpty(changeType))
                {
                    _errorMessages.AppendLine($"Error: Missing change_type for a change in file '{filePath}'.");
                    success = false;
                    continue;
                }

                try
                {
                    switch (changeType)
                    {
                        case "modifyFile":
                            if (!await ModifyFileAsync(filePath, change))
                            {
                                success = false;
                            }
                            break;

                        case "createnewFile":
                            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                            {
                                // Ensure the directory exists
                                if (IsPathWithinProjectRoot(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }
                                else
                                {
                                    _errorMessages.AppendLine($"Security Error: Cannot create directory '{directoryPath}' outside the project root.");
                                    success = false;
                                    continue;
                                }
                            }

                            if (!await CreateNewFileAsync(filePath, change))
                            {
                                success = false;
                            }
                            break;

                        case "replaceFile":
                            if (!await ReplaceFileAsync(filePath, change))
                            {
                                success = false;
                            }
                            break;

                        case "renameFile":
                            if (!await RenameFileAsync(filePath, change))
                            {
                                success = false;
                            }
                            break;

                        case "deleteFile":
                            if (!await DeleteFileAsync(filePath))
                            {
                                success = false;
                            }
                            break;

                        default:
                            _errorMessages.AppendLine($"Error: Unknown change_type '{changeType}' for file '{filePath}'.");
                            success = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _errorMessages.AppendLine($"Error processing {changeType} for '{filePath}': {ex.Message}");
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Modify an existing file by replacing oldContent with newContent
        /// </summary>
        private async Task<bool> ModifyFileAsync(string filePath, JToken change)
        {
            if (!File.Exists(filePath))
            {
                _errorMessages.AppendLine($"Error: Cannot modify non-existent file '{filePath}'.");
                return false;
            }

            string oldContent = change["oldContent"]?.ToString();
            string newContent = change["newContent"]?.ToString();
            int lineNumber = change["lineNumber"]?.Value<int>() ?? 0;

            if (string.IsNullOrEmpty(oldContent))
            {
                _errorMessages.AppendLine($"Error: oldContent is required for modifyFile operation on '{filePath}'.");
                return false;
            }

            try
            {
                // Read all lines to work with line numbers
                string[] fileLines = await File.ReadAllLinesAsync(filePath);
                string fileContent = await File.ReadAllTextAsync(filePath);

                // Normalize line endings and whitespace for comparison
                string normalizedOldContent = NormalizeText(oldContent);
                string normalizedFileContent = NormalizeText(fileContent);

                // If line number is provided, search outward from that line
                if (lineNumber > 0 && lineNumber <= fileLines.Length)
                {
                    int matchIndex = FindClosestMatch(fileLines, normalizedOldContent, lineNumber);
                    if (matchIndex >= 0)
                    {
                        // Found a match starting at this line
                        string matchedContent = ExtractOriginalText(fileContent, normalizedFileContent, normalizedOldContent, matchIndex);
                        string updatedContent = fileContent.Replace(matchedContent, newContent);
                        await File.WriteAllTextAsync(filePath, updatedContent);
                        _logger.LogInformation($"Modified file: {filePath} at line {matchIndex + 1}");
                        return true;
                    }
                }

                // Fallback: try to find the content anywhere in the file
                if (normalizedFileContent.Contains(normalizedOldContent))
                {
                    string matchedContent = ExtractOriginalText(fileContent, normalizedFileContent, normalizedOldContent, 0);
                    string updatedContent = fileContent.Replace(matchedContent, newContent);
                    await File.WriteAllTextAsync(filePath, updatedContent);
                    _logger.LogInformation($"Modified file: {filePath}");
                    return true;
                }

                _errorMessages.AppendLine($"Error: Could not find oldContent in file '{filePath}'.");
                return false;
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Error modifying file '{filePath}': {ex.Message}");
                return false;
            }
        }

        // Normalize text for comparison by trimming each line and standardizing line endings
        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Split by any type of line ending
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Trim each line and rejoin with standard line endings
            return string.Join("\n", lines.Select(line => line.Trim()));
        }

        // Find the closest matching block of text to the specified line number
        private int FindClosestMatch(string[] fileLines, string normalizedOldContent, int targetLineNumber)
        {
            // Convert normalized content to lines for comparison
            string[] oldContentLines = normalizedOldContent.Split('\n');

            if (oldContentLines.Length == 0) return -1;

            // Start at the target line and expand outward
            int maxDistance = Math.Max(fileLines.Length, targetLineNumber);

            for (int distance = 0; distance < maxDistance; distance++)
            {
                // Try below the target line
                int belowIndex = targetLineNumber - 1 + distance;
                if (belowIndex <= fileLines.Length - oldContentLines.Length)
                {
                    if (IsMatchAtIndex(fileLines, oldContentLines, belowIndex))
                        return belowIndex;
                }

                // Try above the target line (but don't check negative indices)
                int aboveIndex = targetLineNumber - 1 - distance;
                if (aboveIndex >= 0 && aboveIndex <= fileLines.Length - oldContentLines.Length)
                {
                    if (IsMatchAtIndex(fileLines, oldContentLines, aboveIndex))
                        return aboveIndex;
                }
            }

            return -1; // No match found
        }

        // Check if the pattern matches at the given index in the file
        private bool IsMatchAtIndex(string[] fileLines, string[] patternLines, int startIndex)
        {
            if (startIndex < 0 || startIndex + patternLines.Length > fileLines.Length)
                return false;

            for (int i = 0; i < patternLines.Length; i++)
            {
                string normalizedFileLine = fileLines[startIndex + i].Trim();
                string normalizedPatternLine = patternLines[i].Trim();

                if (normalizedFileLine != normalizedPatternLine)
                    return false;
            }

            return true;
        }

        // Extract the original text (with original whitespace and line endings) that matches the normalized pattern
        private string ExtractOriginalText(string originalContent, string normalizedContent, string normalizedPattern, int approximateIndex)
        {
            int normalizedIndex = normalizedContent.IndexOf(normalizedPattern);
            if (normalizedIndex < 0) return string.Empty;

            // Get the character count before the match in normalized content
            int charCountBeforeMatch = normalizedContent.Substring(0, normalizedIndex).Length;

            // Find the corresponding position in the original content
            // This is approximate since whitespace may differ
            int originalStartPos = FindOriginalPosition(originalContent, normalizedContent, normalizedIndex);

            // The length in the original might be different due to whitespace
            int originalEndPos = FindOriginalPosition(originalContent, normalizedContent, normalizedIndex + normalizedPattern.Length);

            return originalContent.Substring(originalStartPos, originalEndPos - originalStartPos);
        }

        // Find the position in the original text that corresponds to a position in the normalized text
        private int FindOriginalPosition(string originalContent, string normalizedContent, int normalizedPos)
        {
            if (normalizedPos <= 0) return 0;
            if (normalizedPos >= normalizedContent.Length) return originalContent.Length;

            // Count normalized characters
            int originalPos = 0;
            int normalizedCount = 0;

            while (originalPos < originalContent.Length && normalizedCount < normalizedPos)
            {
                char c = originalContent[originalPos];
                originalPos++;

                // Skip whitespace differences in counting
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    // Only increment normalized count if we're at whitespace in normalized content
                    if (normalizedCount < normalizedContent.Length &&
                        (normalizedContent[normalizedCount] == ' ' || normalizedContent[normalizedCount] == '\n'))
                    {
                        normalizedCount++;
                    }
                }
                else
                {
                    normalizedCount++;
                }
            }

            return originalPos;
        }

        /// <summary>
        /// Create a new file with the provided content
        /// </summary>
        private async Task<bool> CreateNewFileAsync(string filePath, JToken change)
        {
            if (File.Exists(filePath))
            {
                _errorMessages.AppendLine($"Error: File '{filePath}' already exists. Cannot create new file.");
                return false;
            }

            string newContent = change["newContent"]?.ToString() ?? string.Empty;

            try
            {
                await File.WriteAllTextAsync(filePath, newContent);
                _logger.LogInformation($"Created new file: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Error creating file '{filePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Replace an existing file with new content
        /// </summary>
        private async Task<bool> ReplaceFileAsync(string filePath, JToken change)
        {
            string newContent = change["newContent"]?.ToString() ?? string.Empty;

            try
            {
                await File.WriteAllTextAsync(filePath, newContent);
                _logger.LogInformation($"Replaced file: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Error replacing file '{filePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Rename a file to a new path
        /// </summary>
        private async Task<bool> RenameFileAsync(string filePath, JToken change)
        {
            if (!File.Exists(filePath))
            {
                _errorMessages.AppendLine($"Error: Cannot rename non-existent file '{filePath}'.");
                return false;
            }

            string newPath = change["newContent"]?.ToString();
            if (string.IsNullOrEmpty(newPath))
            {
                _errorMessages.AppendLine($"Error: New path is required for renameFile operation on '{filePath}'.");
                return false;
            }

            // Security check: Ensure the new path is within the project root
            if (!IsPathWithinProjectRoot(newPath))
            {
                _errorMessages.AppendLine($"Security Error: The new path '{newPath}' is outside the project root. Access denied.");
                return false;
            }

            try
            {
                // Ensure the target directory exists
                string targetDir = Path.GetDirectoryName(newPath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // If destination file exists, delete it first
                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                }

                File.Move(filePath, newPath);
                _logger.LogInformation($"Renamed file: {filePath} to {newPath}");
                return true;
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Error renaming file '{filePath}' to '{newPath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete an existing file
        /// </summary>
        private async Task<bool> DeleteFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _errorMessages.AppendLine($"Error: Cannot delete non-existent file '{filePath}'.");
                return false;
            }

            try
            {
                File.Delete(filePath);
                _logger.LogInformation($"Deleted file: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Error deleting file '{filePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a path is within the project root directory
        /// </summary>
        private bool IsPathWithinProjectRoot(string path)
        {
            try
            {
                // Normalize paths to ensure consistent comparison
                string normalizedPath = Path.GetFullPath(path);
                string normalizedRoot = Path.GetFullPath(_projectRoot);
                
                // Check if the normalized path starts with the normalized root path
                return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Error validating path '{path}': {ex.Message}");
                return false;
            }
        }
    }
}