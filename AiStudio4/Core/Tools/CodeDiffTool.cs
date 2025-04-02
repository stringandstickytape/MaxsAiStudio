// AiStudio4.Core\Tools\CodeDiffTool.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses;
using SharedClasses.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; // Assuming MessageBox and Clipboard are still desired for UI feedback

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the CodeDiff tool that applies code changes directly,
    /// using a secondary AI to process content modifications.
    /// </summary>
    public class CodeDiffTool : BaseToolImplementation
    {
        private readonly StringBuilder _errorMessages;
        private readonly ISecondaryAiService _secondaryAiService;
        private const string SuccessMarker = "SUCCESS"; // Marker for successful AI content processing
        private const string ErrorMarker = "ERROR:"; // Marker for AI processing errors


        public CodeDiffTool(ILogger<CodeDiffTool> logger, ISettingsService settingsService, ISecondaryAiService secondaryAiService) : base(logger, settingsService)
        {
            _errorMessages = new StringBuilder();
            _secondaryAiService = secondaryAiService ?? throw new ArgumentNullException(nameof(secondaryAiService));
        }

        public override Tool GetToolDefinition()
        {
            // Schema remains the same as it describes the input structure
            return new Tool
            {
                Guid = "a1b2c3d4-e5f6-7890-1234-567890abcdef", // Fixed GUID for CodeDiff
                Description = "Applies specified edits, file creations, replacements, renames, and deletions to files within the project.",
                Name = "CodeDiff",
                Schema = @"{
  ""name"": ""CodeDiff"",
  ""description"": ""Allows you to specify an array of changes (modify, create, replace, rename, delete) for one or more files. Apply changes sequentially. Uses a secondary AI to process content modifications."",
  ""input_schema"": {
    ""type"": ""object"",
    ""properties"": {
      ""changeset"": {
        ""type"": ""object"",
        ""description"": ""A collection of changes to apply."",
        ""properties"": {
          ""description"": {
            ""type"": ""string"",
            ""description"": ""A description of this changeset""
          },
          ""files"": {
            ""type"": ""array"",
            ""description"": ""An array of files to be modified, created, deleted, etc."",
            ""items"": {
              ""type"": ""object"",
              ""properties"": {
                ""path"": {
                  ""type"": ""string"",
                  ""description"": ""The original filename and ABSOLUTE path where the changes are to occur""
                },
                ""changes"": {
                  ""type"": ""array"",
                  ""description"": ""An array of changes for this specific file."",
                  ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""lineNumber"": {
                        ""type"": ""integer"",
                        ""description"": ""The approximate line number where a 'modifyFile' change starts (ignored for other change types).""
                      },
                      ""change_type"": {
                        ""type"": ""string"",
                        ""description"": ""The type of change."",
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
                        ""description"": ""The lines to be removed or replaced in a 'modifyFile' operation. Should include significant context. Ignored for other change types.""
                      },
                      ""newContent"": {
                        ""type"": ""string"",
                        ""description"": ""The lines to be added or to replace oldContent in 'modifyFile'. For 'createnewFile' or 'replaceFile', this is the entire file content. For 'renameFile', this is the new absolute file path.""
                      },
                      ""description"": {
                        ""type"": ""string"",
                        ""description"": ""A human-readable explanation of this specific change""
                      }
                    },
                    ""required"": [
                      ""change_type""
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
                OutputFileType = "json", // Output summarizes the operation result
                Filetype = string.Empty,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Processes a CodeDiff tool call, validates inputs, performs file operations,
        /// and uses a secondary AI for content modifications.
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            _errorMessages.Clear();
            var overallSuccess = true;
            var resultsSummary = new StringBuilder();
            JObject parameters;
            JObject changeset = null;
            string changesetDescription = "No description provided";
            JArray files = null;

            // --- 1. Parse and Validate Input ---
            try
            {
                parameters = JObject.Parse(toolParameters);
                changeset = parameters["changeset"] as JObject;

                if (changeset == null)
                {
                    _errorMessages.AppendLine("Error: Missing or invalid 'changeset' object in parameters.");
                    return CreateResult(false, false, _errorMessages.ToString());
                }

                changesetDescription = changeset["description"]?.ToString() ?? "No description provided";
                _logger.LogInformation("Processing changeset: {Description}", changesetDescription);

                files = changeset["files"] as JArray;
                if (files == null || !files.Any())
                {
                    _errorMessages.AppendLine("Error: 'files' array is missing or empty.");
                    return CreateResult(false, false, _errorMessages.ToString());
                }

                if (!ValidateChangesetStructure(files))
                {
                    // Validation errors are added to _errorMessages within ValidateChangesetStructure
                    overallSuccess = false;
                }
            }
            catch (JsonException jsonEx)
            {
                _errorMessages.AppendLine($"Error parsing tool parameters JSON: {jsonEx.Message}");
                overallSuccess = false;
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Unexpected error during initial parsing: {ex.Message}");
                _logger.LogError(ex, "Unexpected error during CodeDiff initial parsing.");
                overallSuccess = false;
            }

            if (!overallSuccess)
            {
                _logger.LogError("CodeDiff request validation failed:\n{Errors}", _errorMessages.ToString());
                MessageBox.Show(_errorMessages.ToString(), "CodeDiff Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Clipboard.SetText(_errorMessages.ToString());
                return CreateResult(false, false, $"Validation failed: {_errorMessages.ToString()}");
            }

            // --- 2. Process Each File Operation ---
            _logger.LogInformation("Validation successful. Starting file operations for changeset '{Description}'.", changesetDescription);
            resultsSummary.AppendLine($"Changeset '{changesetDescription}' processing results:");

            foreach (var fileToken in files)
            {
                var fileObj = fileToken as JObject;
                if (fileObj == null) continue; // Should not happen if validation passed

                string filePath = fileObj["path"]?.ToString();
                var changes = fileObj["changes"] as JArray;

                // Determine the primary action for the file
                var deleteChange = changes.FirstOrDefault(c => c["change_type"]?.ToString() == "deleteFile");
                var renameChange = changes.FirstOrDefault(c => c["change_type"]?.ToString() == "renameFile");
                var replaceChange = changes.FirstOrDefault(c => c["change_type"]?.ToString() == "replaceFile");
                var createChange = changes.FirstOrDefault(c => c["change_type"]?.ToString() == "createnewFile");
                var modifyChanges = changes.Where(c => c["change_type"]?.ToString() == "modifyFile").ToList();

                string fileResult;

                try
                {
                    if (deleteChange != null)
                    {
                        fileResult = await HandleDeleteFileAsync(filePath, deleteChange);
                    }
                    else if (renameChange != null)
                    {
                        fileResult = await HandleRenameFileAsync(filePath, renameChange);
                    }
                    else if (replaceChange != null)
                    {
                        fileResult = await HandleReplaceFileAsync(filePath, replaceChange);
                    }
                    else if (createChange != null)
                    {
                        fileResult = await HandleCreateFileAsync(filePath, createChange);
                    }
                    else if (modifyChanges.Any())
                    {
                        fileResult = await HandleModifyFileAsync(filePath, modifyChanges);
                    }
                    else
                    {
                        // Should have been caught by validation, but handle defensively
                        fileResult = $"Skipped: No valid change type found for file '{filePath}'.";
                        _logger.LogWarning("No valid change type processed for file '{FilePath}'", filePath);
                        overallSuccess = false; // Treat this as an error
                    }
                }
                catch (Exception ex)
                {
                    fileResult = $"Failed: Unexpected error processing file '{filePath}'. Error: {ex.Message}";
                    _logger.LogError(ex, "Unexpected error processing file operation for '{FilePath}'", filePath);
                    overallSuccess = false;
                }

                resultsSummary.AppendLine($"- {filePath}: {fileResult}");
                if (fileResult.StartsWith("Failed:") || fileResult.StartsWith("Skipped:"))
                {
                    overallSuccess = false;
                }
            }

            // --- 3. Finalize and Report ---
            string finalMessage = resultsSummary.ToString();
            if (!overallSuccess && _errorMessages.Length > 0)
            {
                finalMessage += "\n\nErrors Encountered:\n" + _errorMessages.ToString();
            }

            _logger.LogInformation("CodeDiff processing finished. Overall Success: {Success}. Summary:\n{Summary}", overallSuccess, resultsSummary.ToString());

            MessageBox.Show(finalMessage, overallSuccess ? "CodeDiff Success" : "CodeDiff Completed with Errors", MessageBoxButton.OK, overallSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
            if (!overallSuccess)
            {
                Clipboard.SetText(finalMessage); // Copy summary including errors
            }

            // Return a JSON summary of the operation
            var resultJson = new JObject
            {
                ["overallSuccess"] = overallSuccess,
                ["summary"] = resultsSummary.ToString().Trim(),
                ["errors"] = _errorMessages.ToString().Trim()
            };

            // We return success=true from the tool itself if it *ran*,
            // the actual outcome of the file ops is in the JSON content.
            // The 'hasOutput' parameter depends on whether we want the calling AI to see the summary. Let's assume yes.
            return CreateResult(true, true, toolParameters); // Return original code block, for now...
        }


        // --- File Operation Handlers ---

        private async Task<string> HandleDeleteFileAsync(string filePath, JToken change)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _errorMessages.AppendLine($"Delete Failed: File '{filePath}' not found.");
                    return $"Failed: File not found.";
                }
                File.Delete(filePath);
                _logger.LogInformation("Deleted file '{FilePath}'", filePath);
                return "Success: File deleted.";
            }
            catch (IOException ioEx)
            {
                _errorMessages.AppendLine($"Delete Failed: IO Error deleting file '{filePath}'. {ioEx.Message}");
                _logger.LogError(ioEx, "IO Error deleting file '{FilePath}'", filePath);
                return $"Failed: IO Error. {ioEx.Message}";
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _errorMessages.AppendLine($"Delete Failed: Permissions error deleting file '{filePath}'. {uaEx.Message}");
                _logger.LogError(uaEx, "Permissions error deleting file '{FilePath}'", filePath);
                return $"Failed: Permissions error. {uaEx.Message}";
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Delete Failed: Unexpected error deleting file '{filePath}'. {ex.Message}");
                _logger.LogError(ex, "Unexpected error deleting file '{FilePath}'", filePath);
                return $"Failed: Unexpected error. {ex.Message}";
            }
        }

        private async Task<string> HandleRenameFileAsync(string oldFilePath, JToken change)
        {
            string newFilePath = change["newContent"]?.ToString();
            if (string.IsNullOrEmpty(newFilePath))
            {
                _errorMessages.AppendLine($"Rename Failed: 'newContent' (the new path) is missing for rename operation on '{oldFilePath}'.");
                return "Failed: New path missing.";
            }
            if (!IsPathWithinProjectRoot(newFilePath)) // Validate new path
            {
                _errorMessages.AppendLine($"Rename Failed: Security Error: The new rename path '{newFilePath}' for file '{oldFilePath}' is outside the project root. Access denied.");
                return $"Failed: New path '{newFilePath}' is outside project root.";
            }

            try
            {
                if (!File.Exists(oldFilePath))
                {
                    _errorMessages.AppendLine($"Rename Failed: Source file '{oldFilePath}' not found.");
                    return $"Failed: Source file not found.";
                }
                if (File.Exists(newFilePath))
                {
                    _errorMessages.AppendLine($"Rename Failed: Target file '{newFilePath}' already exists.");
                    return $"Failed: Target file '{newFilePath}' already exists.";
                }

                // Ensure target directory exists
                string targetDir = Path.GetDirectoryName(newFilePath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    _logger.LogInformation("Created directory '{DirectoryPath}' for rename operation.", targetDir);
                }


                File.Move(oldFilePath, newFilePath);
                _logger.LogInformation("Renamed file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return $"Success: Renamed to '{newFilePath}'.";
            }
            catch (IOException ioEx)
            {
                _errorMessages.AppendLine($"Rename Failed: IO Error renaming file '{oldFilePath}' to '{newFilePath}'. {ioEx.Message}");
                _logger.LogError(ioEx, "IO Error renaming file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return $"Failed: IO Error. {ioEx.Message}";
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _errorMessages.AppendLine($"Rename Failed: Permissions error renaming file '{oldFilePath}' to '{newFilePath}'. {uaEx.Message}");
                _logger.LogError(uaEx, "Permissions error renaming file '{OldFilePath}'", oldFilePath);
                return $"Failed: Permissions error. {uaEx.Message}";
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Rename Failed: Unexpected error renaming file '{oldFilePath}' to '{newFilePath}'. {ex.Message}");
                _logger.LogError(ex, "Unexpected error renaming file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return $"Failed: Unexpected error. {ex.Message}";
            }
        }

        private async Task<string> HandleCreateFileAsync(string filePath, JToken change)
        {
            string initialContent = change["newContent"]?.ToString() ?? ""; // Allow empty file creation
            string changeDesc = change["description"]?.ToString() ?? "Create file";


            try
            {
                if (File.Exists(filePath))
                {
                    _errorMessages.AppendLine($"Create Failed: File '{filePath}' already exists.");
                    return $"Failed: File already exists.";
                }

                // Ensure target directory exists
                string targetDir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    _logger.LogInformation("Created directory '{DirectoryPath}' for create operation.", targetDir);
                }

                // Ask AI to finalize/format the content
                string prompt = $"You are a file content creation assistant. Create the content for the file '{Path.GetFileName(filePath)}'. Description: {changeDesc}. Initial content provided below.\n" +
                                $"Ensure the final content is well-formatted and complete. Respond ONLY with the final, complete file content.\n" +
                                $"---\n" +
                                $"{initialContent}";

                var aiResponse = await _secondaryAiService.ProcessRequestAsync(prompt);

                if (!aiResponse.Success || string.IsNullOrWhiteSpace(aiResponse.Response))
                {
                    _errorMessages.AppendLine($"Create Failed: Secondary AI failed to generate content for '{filePath}'. Error: {aiResponse.Error}. Response: {aiResponse.Response}");
                    _logger.LogError("Secondary AI failed for create file '{FilePath}'. Error: {Error}. Response: {Response}", filePath, aiResponse.Error, aiResponse.Response);
                    return $"Failed: AI content generation failed. {aiResponse.Error}";
                }

                string finalContent = aiResponse.Response;
                // Basic check for accidental AI markers in response (should ideally not happen with good prompting)
                if (finalContent.StartsWith(SuccessMarker) || finalContent.StartsWith(ErrorMarker))
                {
                    _logger.LogWarning("AI response for file creation contained unexpected marker. Content: {Content}", finalContent);
                    // Attempt to recover or mark as failure? For now, log and proceed. Could add logic to strip marker.
                }

                finalContent = RemoveBacktickQuotingIfPresent(finalContent);
                await File.WriteAllTextAsync(filePath, finalContent, Encoding.UTF8);
                _logger.LogInformation("Created file '{FilePath}' with AI-processed content.", filePath);
                return "Success: File created.";

            }
            catch (IOException ioEx)
            {
                _errorMessages.AppendLine($"Create Failed: IO Error creating file '{filePath}'. {ioEx.Message}");
                _logger.LogError(ioEx, "IO Error creating file '{FilePath}'", filePath);
                return $"Failed: IO Error. {ioEx.Message}";
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _errorMessages.AppendLine($"Create Failed: Permissions error creating file '{filePath}'. {uaEx.Message}");
                _logger.LogError(uaEx, "Permissions error creating file '{FilePath}'", filePath);
                return $"Failed: Permissions error. {uaEx.Message}";
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Create Failed: Unexpected error creating file '{filePath}'. {ex.Message}");
                _logger.LogError(ex, "Unexpected error creating file '{FilePath}'", filePath);
                return $"Failed: Unexpected error. {ex.Message}";
            }
        }

        private async Task<string> HandleReplaceFileAsync(string filePath, JToken change)
        {
            string initialContent = change["newContent"]?.ToString();
            string changeDesc = change["description"]?.ToString() ?? "Replace file content";


            if (initialContent == null) // Replacement requires content
            {
                _errorMessages.AppendLine($"Replace Failed: 'newContent' is missing for replace operation on '{filePath}'.");
                return "Failed: New content missing.";
            }

            try
            {
                // Although replacing, check if dir exists, maybe it's replacing a deleted file path
                string targetDir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    _logger.LogInformation("Created directory '{DirectoryPath}' for replace operation.", targetDir);
                }

                // Ask AI to finalize/format the content
                string prompt = $"You are a file content replacement assistant. Replace the entire content for the file '{Path.GetFileName(filePath)}'. Description: {changeDesc}. New content provided below.\n" +
                                $"Ensure the final content is well-formatted and complete. Respond ONLY with the final, complete file content.\n" +
                                $"---\n" +
                                $"{initialContent}";

                var aiResponse = await _secondaryAiService.ProcessRequestAsync(prompt);

                if (!aiResponse.Success || string.IsNullOrWhiteSpace(aiResponse.Response))
                {
                    _errorMessages.AppendLine($"Replace Failed: Secondary AI failed to generate content for '{filePath}'. Error: {aiResponse.Error}. Response: {aiResponse.Response}");
                    _logger.LogError("Secondary AI failed for replace file '{FilePath}'. Error: {Error}. Response: {Response}", filePath, aiResponse.Error, aiResponse.Response);
                    return $"Failed: AI content generation failed. {aiResponse.Error}";
                }

                string finalContent = aiResponse.Response;
                // Basic check for accidental AI markers
                if (finalContent.StartsWith(SuccessMarker) || finalContent.StartsWith(ErrorMarker))
                {
                    _logger.LogWarning("AI response for file replacement contained unexpected marker. Content: {Content}", finalContent);
                }

                finalContent = RemoveBacktickQuotingIfPresent(finalContent);

                await File.WriteAllTextAsync(filePath, finalContent, Encoding.UTF8);
                _logger.LogInformation("Replaced file '{FilePath}' with AI-processed content.", filePath);
                return "Success: File replaced.";

            }
            catch (IOException ioEx)
            {
                _errorMessages.AppendLine($"Replace Failed: IO Error writing file '{filePath}'. {ioEx.Message}");
                _logger.LogError(ioEx, "IO Error replacing file '{FilePath}'", filePath);
                return $"Failed: IO Error. {ioEx.Message}";
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _errorMessages.AppendLine($"Replace Failed: Permissions error writing file '{filePath}'. {uaEx.Message}");
                _logger.LogError(uaEx, "Permissions error replacing file '{FilePath}'", filePath);
                return $"Failed: Permissions error. {uaEx.Message}";
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Replace Failed: Unexpected error replacing file '{filePath}'. {ex.Message}");
                _logger.LogError(ex, "Unexpected error replacing file '{FilePath}'", filePath);
                return $"Failed: Unexpected error. {ex.Message}";
            }
        }

        private async Task<string> HandleModifyFileAsync(string filePath, List<JToken> changes)
        {
            string originalContent;
            try
            {
                if (!File.Exists(filePath))
                {
                    _errorMessages.AppendLine($"Modify Failed: File '{filePath}' not found for modification.");
                    return $"Failed: File not found.";
                }
                originalContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            }
            catch (IOException ioEx)
            {
                _errorMessages.AppendLine($"Modify Failed: IO Error reading file '{filePath}'. {ioEx.Message}");
                _logger.LogError(ioEx, "IO Error reading file '{FilePath}' for modification.", filePath);
                return $"Failed: IO Error reading file. {ioEx.Message}";
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _errorMessages.AppendLine($"Modify Failed: Permissions error reading file '{filePath}'. {uaEx.Message}");
                _logger.LogError(uaEx, "Permissions error reading file '{FilePath}' for modification.", filePath);
                return $"Failed: Permissions error reading file. {uaEx.Message}";
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Modify Failed: Unexpected error reading file '{filePath}'. {ex.Message}");
                _logger.LogError(ex, "Unexpected error reading file '{FilePath}' for modification", filePath);
                return $"Failed: Unexpected error reading file. {ex.Message}";
            }

            // --- Prepare AI Request ---
            var modificationsJson = new JArray(changes.Select(ch => new JObject
            {
                // Include relevant fields for the AI to understand the modification
                ["lineNumber"] = ch["lineNumber"],
                ["oldContent"] = ch["oldContent"],
                ["newContent"] = ch["newContent"],
                ["description"] = ch["description"]
            })).ToString(Formatting.Indented);

            string prompt = $"You are a code modification assistant. Apply the following modifications to the original file content provided below.\n" +
                           $"The 'modifications' array describes the changes. Apply them carefully, considering line numbers and context ('oldContent').\n" +
                           $"Respond ONLY with the complete, modified file content. Do not include explanations or summaries.\n\n" +
                           $"File Path: {filePath}\n\n" +
                           $"Modifications JSON:\n{modificationsJson}\n\n" +
                           $"--- ORIGINAL FILE CONTENT ---\n" +
                           $"{originalContent}";


            // --- Send to AI and Process Response ---
            try
            {
                var aiResponse = await _secondaryAiService.ProcessRequestAsync(prompt);

                if (!aiResponse.Success || string.IsNullOrEmpty(aiResponse.Response)) // Allow empty only if original was empty and changes resulted in empty
                {
                    _errorMessages.AppendLine($"Modify Failed: Secondary AI failed to process modifications for '{filePath}'. Error: {aiResponse.Error}. Response: {aiResponse.Response}");
                    _logger.LogError("Secondary AI failed modifications for '{FilePath}'. Error: {Error}. Response: {Response}", filePath, aiResponse.Error, aiResponse.Response);
                    return $"Failed: AI modification processing failed. {aiResponse.Error}";
                }

                string modifiedContent = aiResponse.Response;
                // Basic check for accidental AI markers
                if (modifiedContent.StartsWith(SuccessMarker) || modifiedContent.StartsWith(ErrorMarker))
                {
                    _logger.LogWarning("AI response for file modification contained unexpected marker. Content snippet: {Content}", modifiedContent.Substring(0, Math.Min(100, modifiedContent.Length)));
                }

                modifiedContent = RemoveBacktickQuotingIfPresent(modifiedContent);


                // --- Write Modified Content Back ---
                await File.WriteAllTextAsync(filePath, modifiedContent, Encoding.UTF8);
                _logger.LogInformation("Modified file '{FilePath}' with AI-processed changes.", filePath);
                return $"Success: Applied {changes.Count} modification(s).";
            }
            catch (IOException ioEx)
            {
                _errorMessages.AppendLine($"Modify Failed: IO Error writing modified file '{filePath}'. {ioEx.Message}");
                _logger.LogError(ioEx, "IO Error writing modified file '{FilePath}'.", filePath);
                return $"Failed: IO Error writing file. {ioEx.Message}";
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _errorMessages.AppendLine($"Modify Failed: Permissions error writing modified file '{filePath}'. {uaEx.Message}");
                _logger.LogError(uaEx, "Permissions error writing modified file '{FilePath}'.", filePath);
                return $"Failed: Permissions error writing file. {uaEx.Message}";
            }
            catch (Exception ex)
            {
                _errorMessages.AppendLine($"Modify Failed: Unexpected error during AI call or writing file '{filePath}'. {ex.Message}");
                _logger.LogError(ex, "Unexpected error during AI call or writing modified file '{FilePath}'", filePath);
                return $"Failed: Unexpected error during AI processing/write. {ex.Message}";
            }
        }

        private static string RemoveBacktickQuotingIfPresent(string modifiedContent)
        {
            var lines = modifiedContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (lines.Length >= 2 && lines[0].StartsWith(BacktickHelper.ThreeTicks) && lines[lines.Length - 1].StartsWith(BacktickHelper.ThreeTicks))
                modifiedContent = string.Join(lines[0].Contains("\r") ? "\r\n" : "\n", lines.Skip(1).Take(lines.Length - 2));
            return modifiedContent;
        }


        // --- Validation Helpers ---

        private bool ValidateChangesetStructure(JArray files)
        {
            bool validationSuccess = true;
            var uniquePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Count; i++)
            {
                var fileObj = files[i] as JObject;
                if (fileObj == null)
                {
                    _errorMessages.AppendLine($"Error: Item at index {i} in 'files' array is not a valid JSON object.");
                    validationSuccess = false;
                    continue;
                }

                string filePath = fileObj["path"]?.ToString();
                if (string.IsNullOrEmpty(filePath))
                {
                    _errorMessages.AppendLine($"Error: File path is missing or empty for file entry at index {i}.");
                    validationSuccess = false;
                    continue;
                }

                // Prevent duplicate file entries in the same changeset (simplifies processing logic)
                // Note: Case-insensitive check for Windows paths. Adjust if needed for other OS.
                if (!uniquePaths.Add(Path.GetFullPath(filePath))) // Normalize before adding
                {
                    _errorMessages.AppendLine($"Error: Duplicate file path '{filePath}' found in changeset. Each file should appear only once.");
                    validationSuccess = false;
                    // Continue validation for other files
                }


                // Security check: Ensure the file path is within the project root
                if (!IsPathWithinProjectRoot(filePath))
                {
                    _errorMessages.AppendLine($"Security Error: The file path '{filePath}' is outside the project root. Access denied.");
                    validationSuccess = false;
                    continue; // Check next file
                }

                var changes = fileObj["changes"] as JArray;
                if (changes == null || !changes.Any())
                {
                    _errorMessages.AppendLine($"Error: No changes specified for file '{filePath}'.");
                    validationSuccess = false;
                    continue; // Check next file
                }

                // Check for conflicting top-level actions (e.g., delete and modify same file)
                var actionTypes = changes.Select(c => c["change_type"]?.ToString()).Where(ct => !string.IsNullOrEmpty(ct)).ToList();
                var fileLevelActions = actionTypes.Count(a => a == "deleteFile" || a == "renameFile" || a == "replaceFile" || a == "createnewFile");

                if (fileLevelActions > 1)
                {
                    _errorMessages.AppendLine($"Error: Conflicting file-level operations (delete, rename, replace, create) specified for file '{filePath}'. Only one is allowed per file entry.");
                    validationSuccess = false;
                }
                if (fileLevelActions > 0 && actionTypes.Any(a => a == "modifyFile"))
                {
                    _errorMessages.AppendLine($"Error: Cannot specify 'modifyFile' along with delete, rename, replace, or create for file '{filePath}'.");
                    validationSuccess = false;
                }


                // Validate each change within the file
                for (int j = 0; j < changes.Count; j++)
                {
                    var change = changes[j] as JObject;
                    if (change == null)
                    {
                        _errorMessages.AppendLine($"Error: Change entry at index {j} for file '{filePath}' is not a valid JSON object.");
                        validationSuccess = false;
                        continue;
                    }

                    string changeType = change["change_type"]?.ToString();
                    if (string.IsNullOrEmpty(changeType) || !IsValidChangeType(changeType))
                    {
                        _errorMessages.AppendLine($"Error: Missing or invalid change_type ('{changeType}') for change {j} in file '{filePath}'.");
                        validationSuccess = false;
                        continue; // Check next change
                    }

                    string oldContent = change["oldContent"]?.ToString(); // Null is acceptable for some types
                    string newContent = change["newContent"]?.ToString(); // Null is acceptable for delete/modify

                    // Content Validation based on type
                    if (changeType == "modifyFile" && oldContent == null) // newContent can be empty for deletion within modify
                    {
                        _errorMessages.AppendLine($"Error: oldContent is required for 'modifyFile' operation {j} on '{filePath}'.");
                        validationSuccess = false;
                    }
                    if ((changeType == "createnewFile" || changeType == "replaceFile") && newContent == null)
                    {
                        _errorMessages.AppendLine($"Error: newContent is required for '{changeType}' operation {j} on '{filePath}'.");
                        validationSuccess = false;
                    }
                    if (changeType == "renameFile")
                    {
                        if (string.IsNullOrEmpty(newContent))
                        {
                            _errorMessages.AppendLine($"Error: newContent (new path) is required for 'renameFile' operation {j} on '{filePath}'.");
                            validationSuccess = false;
                        }
                        else if (!IsPathWithinProjectRoot(newContent)) // Validate new path security here too
                        {
                            _errorMessages.AppendLine($"Security Error: The new rename path '{newContent}' for file '{filePath}' (change {j}) is outside the project root. Access denied.");
                            validationSuccess = false;
                        }
                    }

                    // Ensure directory for create is valid (redundant with file path check, but good practice)
                    string directoryPath = changeType == "createnewFile" ? Path.GetDirectoryName(filePath) : null;
                    if (directoryPath != null && !IsPathWithinProjectRoot(directoryPath))
                    {
                        _errorMessages.AppendLine($"Security Error: Cannot create directory '{directoryPath}' for file '{filePath}' outside the project root.");
                        validationSuccess = false;
                    }
                }
            }

            return validationSuccess;
        }

        private bool IsValidChangeType(string changeType)
        {
            return changeType == "modifyFile" ||
                   changeType == "createnewFile" ||
                   changeType == "replaceFile" ||
                   changeType == "renameFile" ||
                   changeType == "deleteFile";
        }


        /// <summary>
        /// Check if a path is within the project root directory
        /// </summary>
        private bool IsPathWithinProjectRoot(string path)
        {
            if (string.IsNullOrEmpty(_projectRoot))
            {
                // Logged during SettingsService initialization usually, but good to check here
                if (!_errorMessages.ToString().Contains("Project root path is not set")) // Avoid duplicate messages
                {
                    _errorMessages.AppendLine("Error: Project root path is not set. Cannot validate file paths.");
                    _logger.LogError("Project root path is not set in CodeDiffTool.");
                }
                return false; // Cannot validate if root is not set
            }
            if (string.IsNullOrEmpty(path))
            {
                if (!_errorMessages.ToString().Contains("Received an empty path"))
                {
                    _errorMessages.AppendLine("Error: Received an empty path for validation.");
                    _logger.LogWarning("Received an empty path for validation in IsPathWithinProjectRoot.");
                }
                return false; // Empty path is invalid
            }

            try
            {
                // Normalize paths to ensure consistent comparison
                string normalizedPath = Path.GetFullPath(path); // Resolves relative paths, ., ..
                string normalizedRoot = Path.GetFullPath(_projectRoot);

                // Ensure the root path ends with a directory separator for accurate StartsWith check
                string rootWithSeparator = normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
                    ? normalizedRoot
                    : normalizedRoot + Path.DirectorySeparatorChar;


                // Check if the normalized path starts with the normalized root path
                // Using OrdinalIgnoreCase for case-insensitivity (common on Windows)
                return normalizedPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException argEx)
            {
                _errorMessages.AppendLine($"Error validating path '{path}': Invalid characters or format. {argEx.Message}");
                _logger.LogWarning(argEx, "ArgumentException during path normalization for '{Path}'.", path);
                return false;
            }
            catch (PathTooLongException ptle)
            {
                _errorMessages.AppendLine($"Error validating path '{path}': Path is too long. {ptle.Message}");
                _logger.LogWarning(ptle, "PathTooLongException during path normalization for '{Path}'.", path);
                return false;
            }
            catch (NotSupportedException nse)
            {
                _errorMessages.AppendLine($"Error validating path '{path}': Path format not supported. {nse.Message}");
                _logger.LogWarning(nse, "NotSupportedException during path normalization for '{Path}'.", path);
                return false;
            }
            catch (System.Security.SecurityException secEx) // Catch security exceptions during path normalization
            {
                _errorMessages.AppendLine($"Security error validating path '{path}': {secEx.Message}");
                _logger.LogWarning(secEx, "SecurityException during path normalization for '{Path}'.", path);
                return false;
            }
            catch (Exception ex) // Catch other potential exceptions
            {
                _errorMessages.AppendLine($"Error validating path '{path}': {ex.Message}");
                _logger.LogError(ex, "Unexpected error validating path '{Path}'.", path);
                return false;
            }
        }
    }
}