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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; // Assuming MessageBox and Clipboard are still desired for UI feedback

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Implementation of the CodeDiff tool that applies code changes.
    /// Groups changes by file path and processes unique files in parallel (up to MaxConcurrency),
    /// while ensuring all operations for a single file are executed sequentially.
    /// Uses a secondary AI for content modifications ('modify', 'create', 'replace').
    /// </summary>
    public class CodeDiffTool : BaseToolImplementation
    {
        private readonly StringBuilder _validationErrorMessages; // For pre-flight validation errors
        private readonly ISecondaryAiService _secondaryAiService;
        private const string SuccessMarker = "SUCCESS"; // Marker for successful AI content processing
        private const string ErrorMarker = "ERROR:"; // Marker for AI processing errors
        private const int MaxConcurrency = 4; // Limit parallel file operations

        public CodeDiffTool(ILogger<CodeDiffTool> logger, IGeneralSettingsService generalSettingsService, ISecondaryAiService secondaryAiService) : base(logger, generalSettingsService)
        {
            _validationErrorMessages = new StringBuilder();
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
                  ""description"": ""Allows you to specify an array of changes (modify, create, replace, rename, delete) for one or more files. Groups changes by file path and processes unique files concurrently, ensuring operations on the same file are sequential. Uses a secondary AI to process content modifications."",
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
                            ""description"": ""An array of file entries, each specifying a path and one or more changes. The same path can appear multiple times; changes will be grouped."",
                            ""items"": {
                              ""type"": ""object"",
                              ""properties"": {
                                ""path"": {
                                  ""type"": ""string"",
                                  ""description"": ""The original filename and ABSOLUTE path where the changes are to occur""
                                },
                                ""changes"": {
                                  ""type"": ""array"",
                                  ""description"": ""An array of changes for this specific file entry."",
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
                                        ""description"": ""The lines to be removed or replaced in a 'modifyFile' operation. Should include significant context. Ignored, and should be left blank, for all other change types.""
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
                                      ""change_type"",
                                      ""oldContent"",
                                      ""newContent"",
                                      ""description"",
                                      // Note: oldContent/newContent requirements depend on change_type, validated later
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
        /// Processes a CodeDiff tool call.
        /// 1. Validates the input structure and security.
        /// 2. Groups all changes by unique, normalized file path.
        /// 3. Processes each unique file path concurrently using a semaphore.
        /// 4. Within each file path's task, determines the net operation and executes changes sequentially.
        /// 5. Aggregates results and reports status.
        /// </summary>
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters)
        {
            _validationErrorMessages.Clear();
            var overallSuccess = true;
            var resultsSummary = new StringBuilder();
            var aggregatedErrors = new StringBuilder(); // For errors during parallel execution
            
            // Preprocess the input to handle multiple tool calls merged into one
            toolParameters = PreprocessMultipleChangesets(toolParameters);
            
            JObject parameters;
            JObject changeset = null;
            string changesetDescription = "No description provided";
            JArray filesArray = null;
            Dictionary<string, List<JObject>> changesByPath = new Dictionary<string, List<JObject>>(StringComparer.OrdinalIgnoreCase);

            // --- 1. Parse and Validate Input Structure ---
            try
            {
                try
                {
                    parameters = JObject.Parse(toolParameters);
                }
                catch(Exception e)
                {
                    parameters = JObject.Parse(toolParameters+"}");
                }
                changeset = parameters["changeset"] as JObject;

                if (changeset == null)
                {
                    _validationErrorMessages.AppendLine("Error: Missing or invalid 'changeset' object in parameters.");
                    return CreateResult(false, false, _validationErrorMessages.ToString());
                }

                changesetDescription = changeset["description"]?.ToString() ?? "No description provided";
                _logger.LogInformation("Processing changeset: {Description}", changesetDescription);

                filesArray = changeset["files"] as JArray;
                if (filesArray == null || !filesArray.Any())
                {
                    _validationErrorMessages.AppendLine("Error: 'files' array is missing or empty.");
                    return CreateResult(false, false, _validationErrorMessages.ToString());
                }

                // --- 2. Group Changes by Path and Perform Detailed Validation ---
                if (!GroupAndValidateChanges(filesArray, changesByPath, _validationErrorMessages))
                {
                    overallSuccess = false;
                }
            }
            catch (JsonException jsonEx)
            {
                _validationErrorMessages.AppendLine($"Error parsing tool parameters JSON: {jsonEx.Message}");
                overallSuccess = false;
            }
            catch (Exception ex)
            {
                _validationErrorMessages.AppendLine($"Unexpected error during initial parsing or validation: {ex.Message}");
                _logger.LogError(ex, "Unexpected error during CodeDiff initial parsing/validation.");
                overallSuccess = false;
            }

            // --- Stop if Validation Failed ---
            if (!overallSuccess)
            {
                _logger.LogError("CodeDiff request validation failed:\n{Errors}", _validationErrorMessages.ToString());
                MessageBox.Show(_validationErrorMessages.ToString(), "CodeDiff Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Clipboard.SetText(_validationErrorMessages.ToString());
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }

            // --- 3. Process Each Unique File Path Concurrently ---
            _logger.LogInformation("Validation successful. Starting parallel file processing (MaxConcurrency={MaxConcurrency}) for {UniqueFileCount} unique files in changeset '{Description}'.", MaxConcurrency, changesByPath.Count, changesetDescription);
            resultsSummary.AppendLine($"Changeset '{changesetDescription}' processing results:");

            var fileProcessingTasks = new List<Task>();
            var semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
            var resultsBag = new ConcurrentBag<string>(); // Thread-safe collection for result strings
            var errorBag = new ConcurrentBag<string>();   // Thread-safe collection for error messages from execution

            foreach (var kvp in changesByPath)
            {
                string filePath = kvp.Key;
                List<JObject> allChangesForFile = kvp.Value;

                // Create a task for processing all changes for this specific file path
                fileProcessingTasks.Add(ProcessSingleFileSequentiallyAsync(filePath, allChangesForFile, semaphore, resultsBag, errorBag));
            }

            // --- Wait for all file processing tasks to complete ---
            await Task.WhenAll(fileProcessingTasks);

            // --- 4. Aggregate Results and Finalize ---
            overallSuccess = errorBag.IsEmpty; // Overall success means no errors were added during execution

            // Aggregate results and errors *after* all tasks complete
            foreach (var result in resultsBag.OrderBy(r => r)) // Order for consistent output
            {
                resultsSummary.AppendLine(result);
            }
            foreach (var error in errorBag)
            {
                aggregatedErrors.AppendLine(error);
            }

            string finalMessage = resultsSummary.ToString();
            if (!overallSuccess && aggregatedErrors.Length > 0)
            {
                finalMessage += "\n\nErrors Encountered During Execution:\n" + aggregatedErrors.ToString();
            }

            _logger.LogInformation("CodeDiff processing finished. Overall Execution Success: {Success}. Summary:\n{Summary}", overallSuccess, resultsSummary.ToString());

            // Show message box if any errors occurred during execution
            if (!overallSuccess)
            {
                MessageBox.Show(finalMessage, "CodeDiff Completed with Errors", MessageBoxButton.OK, MessageBoxImage.Error);
                Clipboard.SetText(finalMessage); // Copy summary including errors
            }
            else if (aggregatedErrors.Length > 0) // Success but with warnings/minor issues logged?
            {
                // Optionally show info for success with non-critical errors if needed
                // MessageBox.Show(finalMessage, "CodeDiff Success with Notices", MessageBoxButton.OK, MessageBoxImage.Information);
            }


            // Return a JSON summary (optional, could return simpler status)
            var resultJson = new JObject
            {
                ["overallSuccess"] = overallSuccess,
                ["summary"] = resultsSummary.ToString().Trim(),
                ["errors"] = aggregatedErrors.ToString().Trim() // Errors from execution phase
            };

            // We return success=true from the tool itself if it *ran* (didn't crash validation),
            // the actual outcome of the file ops is indicated by the UI/log and optionally the returned JSON.
            // Consider returning resultJson.ToString() instead of toolParameters if the caller needs the summary.
            return CreateResult(true, true, toolParameters, overallSuccess ? "File changes applied successfully." : "There were errors applying the file changes.");
        }


        /// <summary>
        /// Processes all changes for a single unique file path sequentially.
        /// This method is executed within a limited-concurrency task pool.
        /// It determines the net effect of all changes and calls the appropriate handler.
        /// </summary>
        private async Task ProcessSingleFileSequentiallyAsync(string filePath, List<JObject> allChangesForFile, SemaphoreSlim semaphore, ConcurrentBag<string> resultsBag, ConcurrentBag<string> errorBag)
        {
            bool taskSuccess = false;
            string taskResultMessage = "Unknown processing error.";

            await semaphore.WaitAsync();
            try
            {
                // --- Determine Net Operation ---
                // Prioritize: Delete > Rename > Replace > Create > Modify
                JObject deleteChange = allChangesForFile.LastOrDefault(c => c["change_type"]?.ToString() == "deleteFile");
                JObject renameChange = allChangesForFile.LastOrDefault(c => c["change_type"]?.ToString() == "renameFile"); // Taking last rename if multiple (validation should prevent)
                JObject replaceChange = allChangesForFile.LastOrDefault(c => c["change_type"]?.ToString() == "replaceFile"); // Taking last replace
                JObject createChange = allChangesForFile.LastOrDefault(c => c["change_type"]?.ToString() == "createnewFile"); // Taking last create
                List<JObject> modifyChanges = allChangesForFile.Where(c => c["change_type"]?.ToString() == "modifyFile").ToList();

                // Log warnings for potentially conflicting operations that were already caught by validation but might indicate user confusion
                int nonModifyActions = (deleteChange != null ? 1 : 0) + (renameChange != null ? 1 : 0) + (replaceChange != null ? 1 : 0) + (createChange != null ? 1 : 0);
                if (nonModifyActions > 1 || (nonModifyActions > 0 && modifyChanges.Any()))
                {
                    _logger.LogWarning("Processing file '{FilePath}': Multiple conflicting actions requested (Delete/Rename/Replace/Create/Modify). Prioritizing according to rules (Delete > Rename > Replace > Create > Modify). Validation should have caught direct conflicts.", filePath);
                    // Validation handles direct errors, this logic implements the precedence.
                }


                (bool success, string message) fileResult;

                // --- Execute Prioritized Action ---
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
                    // Ensure it doesn't exist (race condition check, though validation tries to prevent create+delete etc.)
                    if (File.Exists(filePath))
                    {
                        fileResult = (false, $"Failed: Attempted to create file '{filePath}' which already exists (possible race or validation gap).");
                        _logger.LogError("Create operation failed for '{FilePath}' because it already exists.", filePath);
                    }
                    else
                    {
                        fileResult = await HandleCreateFileAsync(filePath, createChange);
                    }
                }
                else if (modifyChanges.Any())
                {
                    // Ensure it exists (race condition check)
                    if (!File.Exists(filePath))
                    {
                        fileResult = (false, $"Failed: Attempted to modify file '{filePath}' which does not exist (possible race or validation gap).");
                        _logger.LogError("Modify operation failed for '{FilePath}' because it does not exist.", filePath);
                    }
                    else
                    {
                        fileResult = await HandleModifyFileAsync(filePath, modifyChanges); // Pass all modify changes
                    }
                }
                else
                {
                    // Should not happen if validation requires at least one change per file entry
                    fileResult = (false, "Skipped: No effective change operation determined (internal error or validation gap).");
                    _logger.LogWarning("No effective change operation determined for file '{FilePath}' during sequential processing.", filePath);
                }

                taskSuccess = fileResult.success;
                taskResultMessage = fileResult.message;

                if (!taskSuccess)
                {
                    errorBag.Add($"Error processing '{filePath}': {taskResultMessage}");
                }
            }
            catch (Exception ex)
            {
                // Catch unexpected errors during this specific file's processing
                taskSuccess = false;
                taskResultMessage = $"Failed: Unexpected sequential processing error. {ex.Message}";
                string errorDetail = $"Unexpected error processing file '{filePath}' sequentially. Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
                errorBag.Add(errorDetail);
                _logger.LogError(ex, "Unexpected error processing file operation for '{FilePath}' within sequential task.", filePath);
            }
            finally
            {
                resultsBag.Add($"- {filePath}: {taskResultMessage}"); // Always add a result summary line
                semaphore.Release();
            }
        }


        // --- File Operation Handlers (Return tuple, no shared state mutation except logs) ---

        private async Task<(bool Success, string Message)> HandleDeleteFileAsync(string filePath, JObject change) // Takes JObject now, though may not use details
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    // This might be acceptable if a previous step (like rename) moved it, but generally indicates an issue.
                    _logger.LogWarning("Delete request ignored: File '{FilePath}' not found (might have been previously renamed/deleted).", filePath);
                    // Returning true as the desired state (file doesn't exist) is achieved, but log warning.
                    return (true, "Success: File not found or already deleted.");
                    // Alternatively, return (false, "Failed: File not found.") if non-existence should be an error. Choose based on desired strictness.
                }
                File.Delete(filePath);
                _logger.LogInformation("Deleted file '{FilePath}'", filePath);
                return (true, "Success: File deleted.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error deleting file '{FilePath}'", filePath);
                return (false, $"Failed: IO Error. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error deleting file '{FilePath}'", filePath);
                return (false, $"Failed: Permissions error. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting file '{FilePath}'", filePath);
                return (false, $"Failed: Unexpected error. {ex.Message}");
            }
        }

        private async Task<(bool Success, string Message)> HandleRenameFileAsync(string oldFilePath, JObject change)
        {
            string newFilePath = change["newContent"]?.ToString();
            // newFilePath validity and security (within project root) checked during validation phase

            if (string.IsNullOrEmpty(newFilePath)) // Should be caught by validation
            {
                _logger.LogError("Rename Failed: 'newContent' (new path) is missing or invalid for rename on '{OldFilePath}' (Validation Gap?).", oldFilePath);
                return (false, "Failed: New path missing or invalid.");
            }

            try
            {
                if (!File.Exists(oldFilePath))
                {
                    _logger.LogWarning("Rename Failed: Source file '{OldFilePath}' not found.", oldFilePath);
                    return (false, "Failed: Source file not found.");
                }
                if (File.Exists(newFilePath))
                {
                    _logger.LogError("Rename Failed: Target file '{NewFilePath}' already exists.", newFilePath);
                    return (false, $"Failed: Target file '{newFilePath}' already exists.");
                }

                // Ensure target directory exists
                string targetDir = Path.GetDirectoryName(newFilePath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                    _logger.LogInformation("Created directory '{DirectoryPath}' for rename operation.", targetDir);
                }

                File.Move(oldFilePath, newFilePath);
                _logger.LogInformation("Renamed file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return (true, $"Success: Renamed to '{newFilePath}'.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error renaming file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return (false, $"Failed: IO Error. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error renaming file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return (false, $"Failed: Permissions error. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error renaming file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return (false, $"Failed: Unexpected error. {ex.Message}");
            }
        }

        private async Task<(bool Success, string Message)> HandleCreateFileAsync(string filePath, JObject change)
        {
            string initialContent = change["newContent"]?.ToString() ?? ""; // Allow empty file creation
            string changeDesc = change["description"]?.ToString() ?? "Create file";
            // File path validity / security checked during validation phase

            try
            {
                // Double check existence (race condition mitigation) - already handled in ProcessSingleFileSequentiallyAsync caller logic
                // if (File.Exists(filePath))...

                // Ensure target directory exists
                string targetDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
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

                if (!aiResponse.Success) // Allow empty response if successful AI call intended it
                {
                    string errorMsg = $"Create Failed: Secondary AI failed to generate content for '{filePath}'. Error: {aiResponse.Error ?? "Unknown AI Error"}. Response: {aiResponse.Response ?? "<null>"}";
                    _logger.LogError("Secondary AI failed for create file '{FilePath}'. Error: {Error}. Response: {Response}", filePath, aiResponse.Error, aiResponse.Response);
                    return (false, $"Failed: AI content generation failed. {aiResponse.Error ?? "See Logs"}");
                }

                string finalContent = aiResponse.Response ?? ""; // Use empty string if AI response is null but Success=true
                                                                 // Basic check for accidental AI markers
                if (finalContent.StartsWith(SuccessMarker) || finalContent.StartsWith(ErrorMarker))
                {
                    _logger.LogWarning("AI response for file creation contained unexpected marker. Content: {Content}", finalContent);
                }

                finalContent = RemoveBacktickQuotingIfPresent(finalContent);
                await File.WriteAllTextAsync(filePath, finalContent, Encoding.UTF8);
                _logger.LogInformation("Created file '{FilePath}' with AI-processed content.", filePath);
                return (true, "Success: File created.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error creating file '{FilePath}'", filePath);
                return (false, $"Failed: IO Error. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error creating file '{FilePath}'", filePath);
                return (false, $"Failed: Permissions error. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating file '{FilePath}'", filePath);
                return (false, $"Failed: Unexpected error. {ex.Message}");
            }
        }

        private async Task<(bool Success, string Message)> HandleReplaceFileAsync(string filePath, JObject change)
        {
            string initialContent = change["newContent"]?.ToString();
            string changeDesc = change["description"]?.ToString() ?? "Replace file content";
            // File path validity / security checked during validation phase

            if (initialContent == null) // Should be caught by validation
            {
                _logger.LogError("Replace Failed: 'newContent' is missing for replace operation on '{FilePath}' (Validation Gap?).", filePath);
                return (false, "Failed: New content missing.");
            }

            try
            {
                // Ensure target directory exists (maybe replacing a deleted file path)
                string targetDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
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

                if (!aiResponse.Success) // Allow empty response if successful AI call intended it
                {
                    string errorMsg = $"Replace Failed: Secondary AI failed to generate content for '{filePath}'. Error: {aiResponse.Error ?? "Unknown AI Error"}. Response: {aiResponse.Response ?? "<null>"}";
                    _logger.LogError("Secondary AI failed for replace file '{FilePath}'. Error: {Error}. Response: {Response}", filePath, aiResponse.Error, aiResponse.Response);
                    return (false, $"Failed: AI content generation failed. {aiResponse.Error ?? "See Logs"}");
                }

                string finalContent = aiResponse.Response ?? ""; // Use empty string if AI response is null but Success=true
                if (finalContent.StartsWith(SuccessMarker) || finalContent.StartsWith(ErrorMarker))
                {
                    _logger.LogWarning("AI response for file replacement contained unexpected marker. Content: {Content}", finalContent);
                }

                finalContent = RemoveBacktickQuotingIfPresent(finalContent);

                await File.WriteAllTextAsync(filePath, finalContent, Encoding.UTF8);
                _logger.LogInformation("Replaced file '{FilePath}' with AI-processed content.", filePath);
                return (true, "Success: File replaced.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error writing file '{FilePath}'", filePath);
                return (false, $"Failed: IO Error. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error writing file '{FilePath}'", filePath);
                return (false, $"Failed: Permissions error. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error replacing file '{FilePath}'", filePath);
                return (false, $"Failed: Unexpected error. {ex.Message}");
            }
        }

        private async Task<(bool Success, string Message)> HandleModifyFileAsync(string filePath, List<JObject> changes) // Takes list of modify changes for this file
        {
            string originalContent;
            try
            {
                // Double check existence - handled by caller ProcessSingleFileSequentiallyAsync
                // if (!File.Exists(filePath))...
                originalContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error reading file '{FilePath}' for modification.", filePath);
                return (false, $"Failed: IO Error reading file. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error reading file '{FilePath}' for modification.", filePath);
                return (false, $"Failed: Permissions error reading file. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error reading file '{FilePath}' for modification", filePath);
                return (false, $"Failed: Unexpected error reading file. {ex.Message}");
            }

            // --- Prepare AI Request ---
            // Filter only necessary fields for the AI prompt to avoid clutter
            var modificationsForPrompt = changes.Select(ch => new JObject
            {
                // Only include fields relevant to applying the change
                ["lineNumber"] = ch["lineNumber"], // Keep for context
                ["oldContent"] = ch["oldContent"], // Keep for context matching
                ["newContent"] = ch["newContent"], // The replacement/addition
                ["description"] = ch["description"] // Optional context for AI
            }).ToList();

            // Only proceed if there are valid modifications to apply
            if (!modificationsForPrompt.Any())
            {
                _logger.LogWarning("Modify operation for '{FilePath}' called with no valid modification details.", filePath);
                return (true, "Success: No modifications to apply."); // Or false if this state is unexpected
            }

            var modificationsJson = new JArray(modificationsForPrompt).ToString(Formatting.None); // Compact JSON for prompt

            string prompt = $"You are a code modification assistant. Apply the following modifications sequentially to the original file content provided below.\n" +
                           $"The 'modifications' array describes the changes. Apply them carefully, considering line numbers and context ('oldContent'). Preserve line endings (CRLF vs LF) from the original file.\n" +
                           $"Respond ONLY with the complete, modified file content. Do not include explanations, summaries, or markdown formatting like ```.\n\n" +
                           $"File Path: {filePath}\n\n" +
                           $"Modifications JSON:\n{modificationsJson}\n\n" +
                           $"--- ORIGINAL FILE CONTENT ---\n" +
                           $"{originalContent}";


            // --- Send to AI and Process Response ---
            try
            {
                var aiResponse = await _secondaryAiService.ProcessRequestAsync(prompt);

                if (!aiResponse.Success) // Allow empty response if successful AI call intended it
                {
                    string errorMsg = $"Modify Failed: Secondary AI failed to process modifications for '{filePath}'. Error: {aiResponse.Error ?? "Unknown AI Error"}. Response: {aiResponse.Response ?? "<null>"}";
                    _logger.LogError("Secondary AI failed modifications for '{FilePath}'. Error: {Error}. Response: {Response}", filePath, aiResponse.Error, aiResponse.Response);
                    return (false, $"Failed: AI modification processing failed. {aiResponse.Error ?? "See Logs"}");
                }

                string modifiedContent = aiResponse.Response ?? ""; // Use empty string if AI response is null but Success=true

                // Stronger check for unwanted formatting/markers
                if (modifiedContent.StartsWith(SuccessMarker) || modifiedContent.StartsWith(ErrorMarker) || modifiedContent.TrimStart().StartsWith("```"))
                {
                    _logger.LogWarning("AI response for file modification contained unexpected markers or formatting. Attempting cleanup. File: {FilePath}", filePath);
                    modifiedContent = RemoveBacktickQuotingIfPresent(modifiedContent);
                    // Potentially add more cleanup here if needed
                }

                // --- Write Modified Content Back ---
                // Preserve original encoding? For now, assume UTF8 is acceptable.
                await File.WriteAllTextAsync(filePath, modifiedContent, Encoding.UTF8);
                _logger.LogInformation("Modified file '{FilePath}' with {Count} AI-processed change(s).", filePath, changes.Count);
                return (true, $"Success: Applied {changes.Count} modification(s).");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error writing modified file '{FilePath}'.", filePath);
                return (false, $"Failed: IO Error writing file. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error writing modified file '{FilePath}'.", filePath);
                return (false, $"Failed: Permissions error writing file. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during AI call or writing modified file '{FilePath}'", filePath);
                return (false, $"Failed: Unexpected error during AI processing/write. {ex.Message}");
            }
        }

        /// <summary>
        /// Removes surrounding backticks and language specifier if present.
        /// </summary>
        private static string RemoveBacktickQuotingIfPresent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return content;

            content = content.Trim(); // Trim whitespace first

            if (content.StartsWith("```") && content.EndsWith("```"))
            {
                content = content.Substring(3, content.Length - 6).Trim(); // Remove triple backticks

                // Check if the first line is just a language specifier (e.g., "csharp", "json")
                var firstNewLine = content.IndexOfAny(new[] { '\r', '\n' });
                if (firstNewLine >= 0)
                {
                    string firstLine = content.Substring(0, firstNewLine).Trim();
                    // Basic check: is the first line short and without typical code chars?
                    if (firstLine.Length > 0 && firstLine.Length < 20 && !firstLine.Any(c => c == ' ' || c == '{' || c == '(' || c == ';'))
                    {
                        content = content.Substring(firstNewLine).TrimStart(); // Remove language line
                    }
                }
                return content; // Return content within backticks
            }
            return content; // Return original content if not quoted
        }


        // --- Validation Helpers ---

        /// <summary>
        /// Groups changes by normalized path and validates the overall changeset consistency.
        /// Populates changesByPath and validationErrorMessages.
        /// </summary>
        private bool GroupAndValidateChanges(JArray filesArray, Dictionary<string, List<JObject>> changesByPath, StringBuilder validationErrorMessages)
        {
            bool validationSuccess = true;
            var pathDetails = new Dictionary<string, PathValidationDetails>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < filesArray.Count; i++)
            {
                var fileObj = filesArray[i] as JObject;
                if (fileObj == null)
                {
                    validationErrorMessages.AppendLine($"Error: Item at index {i} in 'files' array is not a valid JSON object.");
                    validationSuccess = false;
                    continue;
                }

                string rawFilePath = fileObj["path"]?.ToString();
                if (string.IsNullOrEmpty(rawFilePath))
                {
                    validationErrorMessages.AppendLine($"Error: File path is missing or empty for file entry at index {i}.");
                    validationSuccess = false;
                    continue;
                }

                // --- Normalize and Security Check Path ---
                string normalizedPath;
                try
                {
                    normalizedPath = Path.GetFullPath(rawFilePath);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is System.Security.SecurityException)
                {
                    validationErrorMessages.AppendLine($"Error: Invalid or disallowed file path '{rawFilePath}' at index {i}. {ex.Message}");
                    _logger.LogWarning(ex, "Path normalization failed for input path '{RawPath}'", rawFilePath);
                    validationSuccess = false;
                    continue;
                }

                if (!IsPathWithinProjectRoot(normalizedPath, validationErrorMessages, rawFilePath)) // Pass raw path for better error msg
                {
                    // Error message added within IsPathWithinProjectRoot
                    validationSuccess = false;
                    continue; // Don't process changes for invalid paths
                }

                // --- Get or Create Path Details for Validation ---
                if (!pathDetails.TryGetValue(normalizedPath, out var details))
                {
                    details = new PathValidationDetails { FilePath = normalizedPath };
                    pathDetails[normalizedPath] = details;
                    changesByPath[normalizedPath] = new List<JObject>(); // Initialize change list for this path
                }

                // --- Validate and Add Changes for this File Entry ---
                var changes = fileObj["changes"] as JArray;
                if (changes == null || !changes.Any())
                {
                    validationErrorMessages.AppendLine($"Error: No changes specified for file entry {i} ('{rawFilePath}'). Each entry must have at least one change.");
                    validationSuccess = false;
                    continue;
                }

                foreach (var changeToken in changes)
                {
                    var change = changeToken as JObject;
                    if (change == null)
                    {
                        validationErrorMessages.AppendLine($"Error: Change entry is not a valid JSON object for file '{rawFilePath}' (entry {i}).");
                        validationSuccess = false;
                        continue;
                    }

                    string changeType = change["change_type"]?.ToString();
                    if (string.IsNullOrEmpty(changeType) || !IsValidChangeType(changeType))
                    {
                        validationErrorMessages.AppendLine($"Error: Missing or invalid change_type ('{changeType ?? "null"}') for a change in file '{rawFilePath}' (entry {i}).");
                        validationSuccess = false;
                        continue;
                    }

                    // Validate content presence based on type
                    string oldContent = change["oldContent"]?.ToString();
                    string newContent = change["newContent"]?.ToString();

                    bool contentValid = ValidateChangeContent(changeType, oldContent, newContent, normalizedPath, validationErrorMessages);
                    if (!contentValid)
                    {
                        validationSuccess = false;
                        continue; // Don't add invalid change
                    }

                    // If rename, validate the target path as well
                    if (changeType == "renameFile")
                    {
                        string newPathRaw = newContent; // newContent holds the new path for rename
                        string newPathNormalized;
                        try
                        {
                            newPathNormalized = Path.GetFullPath(newPathRaw);
                        }
                        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is System.Security.SecurityException)
                        {
                            validationErrorMessages.AppendLine($"Error: Invalid or disallowed target path '{newPathRaw}' specified in rename operation for '{rawFilePath}'. {ex.Message}");
                            _logger.LogWarning(ex, "Path normalization failed for rename target path '{NewPathRaw}' from '{RawPath}'", newPathRaw, rawFilePath);
                            validationSuccess = false;
                            continue;
                        }
                        if (!IsPathWithinProjectRoot(newPathNormalized, validationErrorMessages, newPathRaw))
                        {
                            validationSuccess = false;
                            continue;
                        }
                        // Check for rename collision (another file being renamed TO the same target) - basic check here
                        if (pathDetails.Values.Any(pd => pd.RenameTargetPath == newPathNormalized))
                        {
                            validationErrorMessages.AppendLine($"Error: Multiple files are being renamed to the same target path '{newPathNormalized}'.");
                            validationSuccess = false;
                            // Note: More complex collision detection (e.g., file A->B, file C->A) is harder to validate upfront.
                        }
                        details.RenameTargetPath = newPathNormalized; // Store for collision check
                    }


                    // Add valid change to the grouped list
                    changesByPath[normalizedPath].Add(change);

                    // Update validation details for conflict checks
                    if (changeType == "deleteFile") details.HasDelete = true;
                    if (changeType == "renameFile") details.HasRename = true;
                    if (changeType == "replaceFile") details.HasReplace = true;
                    if (changeType == "createnewFile") details.HasCreate = true;
                    if (changeType == "modifyFile") details.HasModify = true;

                } // End foreach change in entry
            } // End for each file entry

            // --- Final Cross-Path Validation ---
            foreach (var kvp in pathDetails)
            {
                string path = kvp.Key;
                var details = kvp.Value;

                // Check for conflicting top-level actions on the SAME path
                int exclusiveActions = (details.HasDelete ? 1 : 0) + (details.HasRename ? 1 : 0) + (details.HasReplace ? 1 : 0) + (details.HasCreate ? 1 : 0);

                if (exclusiveActions > 1)
                {
                    validationErrorMessages.AppendLine($"Error: Conflicting exclusive operations (delete, rename, replace, create) requested for the same file path '{path}'.");
                    validationSuccess = false;
                }
                if (exclusiveActions > 0 && details.HasModify)
                {
                    validationErrorMessages.AppendLine($"Error: Cannot specify 'modifyFile' along with delete, rename, replace, or create for the same file path '{path}'.");
                    validationSuccess = false;
                }

                // Add more checks? E.g., check if a rename target path conflicts with a create/replace path?
                if (details.HasRename && !string.IsNullOrEmpty(details.RenameTargetPath))
                {
                    if (pathDetails.TryGetValue(details.RenameTargetPath, out var targetDetails))
                    {
                        if (targetDetails.HasCreate || targetDetails.HasReplace)
                        {
                            validationErrorMessages.AppendLine($"Error: Rename operation targets path '{details.RenameTargetPath}', which is also targeted by a create or replace operation.");
                            validationSuccess = false;
                        }
                        if (targetDetails.HasDelete)
                        {
                            // Renaming to a path that is also being deleted might be okay, but log warning?
                            _logger.LogWarning("File '{OriginalPath}' is being renamed to '{TargetPath}', which is also marked for deletion.", path, details.RenameTargetPath);
                        }
                    }
                }
            }


            return validationSuccess;
        }

        // Helper class for validation tracking per path
        private class PathValidationDetails
        {
            public string FilePath { get; set; }
            public bool HasDelete { get; set; }
            public bool HasRename { get; set; }
            public bool HasReplace { get; set; }
            public bool HasCreate { get; set; }
            public bool HasModify { get; set; }
            public string RenameTargetPath { get; set; } // Store normalized target path for collision checks
        }


        /// <summary>
        /// Validates required content fields based on change type.
        /// </summary>
        private bool ValidateChangeContent(string changeType, string oldContent, string newContent, string filePath, StringBuilder validationErrorMessages)
        {
            bool isValid = true;
            switch (changeType)
            {
                case "modifyFile":
                    // oldContent is technically required for context matching by the AI, though AI might handle missing.
                    // newContent can be null/empty if the intention is to delete the oldContent lines.
                    if (oldContent == null) // Make oldContent mandatory for modify
                    {
                        validationErrorMessages.AppendLine($"Error: 'oldContent' is required for 'modifyFile' operation on '{filePath}'.");
                        isValid = false;
                    }
                    // newContent can be null, so no check here.
                    break;
                case "createnewFile":
                case "replaceFile":
                    if (newContent == null)
                    {
                        validationErrorMessages.AppendLine($"Error: 'newContent' is required for '{changeType}' operation on '{filePath}'.");
                        isValid = false;
                    }
                    // oldContent is ignored
                    break;
                case "renameFile":
                    if (string.IsNullOrEmpty(newContent)) // newContent holds the new path here
                    {
                        validationErrorMessages.AppendLine($"Error: 'newContent' (the new path) is required and cannot be empty for 'renameFile' operation on '{filePath}'.");
                        isValid = false;
                    }
                    // oldContent is ignored
                    break;
                case "deleteFile":
                    // oldContent and newContent are ignored
                    break;
                default:
                    // Should be caught earlier, but defensive check
                    validationErrorMessages.AppendLine($"Internal Error: Unexpected change type '{changeType}' encountered in ValidateChangeContent for '{filePath}'.");
                    isValid = false;
                    break;
            }
            return isValid;
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
        /// Check if a path is within the project root directory.
        /// Appends error messages to the provided StringBuilder if validation fails.
        /// </summary>
        /// <param name="normalizedPath">The fully normalized path to check.</param>
        /// <param name="errorMessages">StringBuilder to append errors to.</param>
        /// <param name="originalPathForErrorMsg">The original path string provided by the user, for clearer error messages.</param>
        /// <returns>True if path is within root, false otherwise.</returns>
        private bool IsPathWithinProjectRoot(string normalizedPath, StringBuilder errorMessages, string originalPathForErrorMsg)
        {
            // Check if project root itself is set
            if (string.IsNullOrEmpty(_projectRoot))
            {
                if (!errorMessages.ToString().Contains("Project root path is not set")) // Avoid duplicate messages
                {
                    errorMessages.AppendLine("Error: Project root path is not set. Cannot validate file paths.");
                    _logger.LogError("Project root path is not set in CodeDiffTool.");
                }
                return false;
            }

            // Should not happen if called after normalization, but check anyway.
            if (string.IsNullOrEmpty(normalizedPath))
            {
                if (!errorMessages.ToString().Contains("Received an empty path"))
                {
                    errorMessages.AppendLine($"Error: Received an empty or invalid path ('{originalPathForErrorMsg}') for validation.");
                    _logger.LogWarning("Received an empty path ('{OriginalPath}') for validation in IsPathWithinProjectRoot.", originalPathForErrorMsg);
                }
                return false;
            }

            try
            {
                // Normalize root path once (could be done in constructor)
                string normalizedRoot = Path.GetFullPath(_projectRoot);

                // Ensure paths end with separator for accurate StartsWith comparison
                string pathWithSeparator = normalizedPath.EndsWith(Path.DirectorySeparatorChar.ToString()) || normalizedPath.EndsWith(Path.AltDirectorySeparatorChar.ToString())
                   ? normalizedPath
                   : normalizedPath + Path.DirectorySeparatorChar;
                string rootWithSeparator = normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString()) || normalizedRoot.EndsWith(Path.AltDirectorySeparatorChar.ToString())
                    ? normalizedRoot
                    : normalizedRoot + Path.DirectorySeparatorChar;

                // Perform case-insensitive comparison
                bool isWithin = pathWithSeparator.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);

                if (!isWithin)
                {
                    errorMessages.AppendLine($"Security Error: The path '{originalPathForErrorMsg}' (resolves to '{normalizedPath}') is outside the allowed project root '{normalizedRoot}'. Access denied.");
                    _logger.LogWarning("Path validation failed: '{NormalizedPath}' is outside project root '{NormalizedRoot}'. Original input: '{OriginalPath}'", normalizedPath, normalizedRoot, originalPathForErrorMsg);
                }

                return isWithin;
            }
            catch (Exception ex) // Catch unexpected errors during the check itself
            {
                errorMessages.AppendLine($"Error validating path '{originalPathForErrorMsg}': {ex.Message}");
                _logger.LogError(ex, "Unexpected error during IsPathWithinProjectRoot check for path '{OriginalPath}' (Normalized: '{NormalizedPath}')", originalPathForErrorMsg, normalizedPath);
                return false;
            }
        }
        
        /// <summary>
        /// Preprocesses the input to handle multiple tool calls merged into one string.
        /// Detects and combines multiple JSON objects into a single changeset.
        /// </summary>
        /// <param name="toolParameters">The original tool parameters string which may contain multiple JSON objects</param>
        /// <returns>A preprocessed JSON string with a single combined changeset</returns>
        private string PreprocessMultipleChangesets(string toolParameters)
        {
            try
            {
                // Check if the input might contain multiple JSON objects
                if (!toolParameters.TrimStart().StartsWith("{") || !toolParameters.TrimEnd().EndsWith("}"))
                {
                    _logger.LogInformation("Input doesn't appear to be a standard JSON object, attempting to parse multiple objects");
                }
                
                // Try to parse as a single object first
                try
                {
                    JObject.Parse(toolParameters);
                    // If parsing succeeds, it's a valid single object, no preprocessing needed
                    return toolParameters;
                }
                catch (JsonException)
                {
                    // Not a single valid JSON object, continue with preprocessing
                    _logger.LogInformation("Input is not a single valid JSON object, attempting to extract multiple changesets");
                }
                
                // Look for patterns that might indicate multiple tool calls
                // Common pattern: {"name":"CodeDiff","args":{...}}{"name":"CodeDiff","args":{...}}
                List<JObject> allChangesets = new List<JObject>();
                Dictionary<string, List<JObject>> combinedFilesByPath = new Dictionary<string, List<JObject>>(StringComparer.OrdinalIgnoreCase);
                string combinedDescription = "Combined multiple changesets: ";
                bool foundAnyChangesets = false;
                
                // Use regex to find JSON objects with the CodeDiff pattern
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    toolParameters, 
                    @"\{\s*[""']name[""']\s*:\s*[""']CodeDiff[""']\s*,\s*[""'](?:args|parameters)[""']\s*:\s*(\{[^{}]*(?:\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}[^{}]*)*\})\s*\}");
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count < 2) continue;
                    
                    string argsJson = match.Groups[1].Value;
                    try
                    {
                        var args = JObject.Parse(argsJson);
                        var changeset = args["changeset"] as JObject;
                        if (changeset != null)
                        {
                            foundAnyChangesets = true;
                            string desc = changeset["description"]?.ToString() ?? "Unnamed changeset";
                            combinedDescription += desc + "; ";
                            
                            var files = changeset["files"] as JArray;
                            if (files != null)
                            {
                                foreach (JObject fileObj in files.OfType<JObject>())
                                {
                                    string path = fileObj["path"]?.ToString();
                                    if (string.IsNullOrEmpty(path)) continue;
                                    
                                    var changes = fileObj["changes"] as JArray;
                                    if (changes == null || !changes.Any()) continue;
                                    
                                    if (!combinedFilesByPath.TryGetValue(path, out var changesList))
                                    {
                                        changesList = new List<JObject>();
                                        combinedFilesByPath[path] = changesList;
                                    }
                                    
                                    // Add all changes for this file
                                    foreach (JObject change in changes.OfType<JObject>())
                                    {
                                        changesList.Add(change);
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Failed to parse a potential changeset: {Error}", ex.Message);
                    }
                }
                
                // If we found and processed any changesets, build a new combined one
                if (foundAnyChangesets)
                {
                    _logger.LogInformation("Successfully combined {Count} changesets", matches.Count);
                    
                    // Create combined files array
                    var combinedFiles = new JArray();
                    foreach (var kvp in combinedFilesByPath)
                    {
                        var fileObj = new JObject
                        {
                            ["path"] = kvp.Key,
                            ["changes"] = new JArray(kvp.Value)
                        };
                        combinedFiles.Add(fileObj);
                    }
                    
                    // Create the combined changeset
                    var combinedChangeset = new JObject
                    {
                        ["description"] = combinedDescription.TrimEnd(';', ' '),
                        ["files"] = combinedFiles
                    };
                    
                    // Create the final result
                    var combinedResult = new JObject
                    {
                        ["changeset"] = combinedChangeset
                    };
                    
                    return combinedResult.ToString(Formatting.None);
                }
                
                // If we couldn't find multiple changesets, return the original
                return toolParameters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preprocessing multiple changesets, will use original input");
                return toolParameters;
            }
        }
    }
}