// AiStudio4.Core\Tools\CodeDiffTool.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools.CodeDiff;
using AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers;
using AiStudio4.Core.Tools.CodeDiff.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses;
using SharedClasses.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly ChangesetPreprocessor _changesetPreprocessor;
        private readonly CodeDiffValidator _validator;
        private readonly PathSecurityManager _pathSecurityManager;
        private const int MaxConcurrency = 4; // Limit parallel file operations

        public CodeDiffTool(ILogger<CodeDiffTool> logger, IGeneralSettingsService generalSettingsService, 
            ISecondaryAiService secondaryAiService, IStatusMessageService statusMessageService) 
            : base(logger, generalSettingsService, statusMessageService)
        {
            _validationErrorMessages = new StringBuilder();
            _secondaryAiService = secondaryAiService ?? throw new ArgumentNullException(nameof(secondaryAiService));
            _changesetPreprocessor = new ChangesetPreprocessor(logger);
            _pathSecurityManager = new PathSecurityManager(logger, _projectRoot);
            _validator = new CodeDiffValidator(logger, _pathSecurityManager);
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
        public override async Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties)
        {
            _validationErrorMessages.Clear();
            var overallSuccess = true;
            var resultsSummary = new StringBuilder();
            var aggregatedErrors = new StringBuilder(); // For errors during parallel execution
            
            // Send initial status update
            SendStatusUpdate("Starting CodeDiff tool execution...");
            
            // Preprocess the input to handle multiple tool calls merged into one
            toolParameters = _changesetPreprocessor.PreprocessMultipleChangesets(toolParameters);
            
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
                if (!_validator.GroupAndValidateChanges(filesArray, changesByPath, _validationErrorMessages))
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
                SendStatusUpdate("Validation failed. See error details.");
                MessageBox.Show(_validationErrorMessages.ToString(), "CodeDiff Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Clipboard.SetText(_validationErrorMessages.ToString());
                return CreateResult(false, false, $"Validation failed: {_validationErrorMessages.ToString()}");
            }

            // --- 3. Process Each Unique File Path Concurrently ---
            _logger.LogInformation("Validation successful. Starting parallel file processing (MaxConcurrency={MaxConcurrency}) for {UniqueFileCount} unique files in changeset '{Description}'.", MaxConcurrency, changesByPath.Count, changesetDescription);
            SendStatusUpdate($"Validation successful. Processing {changesByPath.Count} files...");
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
            SendStatusUpdate("File processing completed. Finalizing results...");

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

            // Send final status update
            if (overallSuccess)
            {
                SendStatusUpdate("CodeDiff completed successfully.");
            }
            else
            {
                SendStatusUpdate("CodeDiff completed with errors. See details.");
            }

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
                // Send status update for this file
                SendStatusUpdate($"Processing file: {System.IO.Path.GetFileName(filePath)}");

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

                FileOperationResult fileResult;

                // --- Execute Prioritized Action ---
                if (deleteChange != null)
                {
                    SendStatusUpdate($"Deleting file: {System.IO.Path.GetFileName(filePath)}");
                    var handler = new DeleteFileHandler(_logger, _statusMessageService, _clientId);
                    fileResult = await handler.HandleAsync(filePath, deleteChange);
                }
                else if (renameChange != null)
                {
                    string newPath = renameChange["newContent"]?.ToString() ?? "unknown";
                    SendStatusUpdate($"Renaming file: {System.IO.Path.GetFileName(filePath)} to {System.IO.Path.GetFileName(newPath)}");
                    var handler = new RenameFileHandler(_logger, _statusMessageService, _clientId);
                    fileResult = await handler.HandleAsync(filePath, renameChange);
                }
                else if (replaceChange != null)
                {
                    SendStatusUpdate($"Replacing file: {System.IO.Path.GetFileName(filePath)}");
                    var handler = new ReplaceFileHandler(_logger, _statusMessageService, _clientId);
                    fileResult = await handler.HandleAsync(filePath, replaceChange);
                }
                else if (createChange != null)
                {
                    SendStatusUpdate($"Creating file: {System.IO.Path.GetFileName(filePath)}");
                    var handler = new CreateFileHandler(_logger, _statusMessageService, _clientId);
                    fileResult = await handler.HandleAsync(filePath, createChange);
                }
                else if (modifyChanges.Any())
                {
                    // Ensure it exists (race condition check)
                    if (!System.IO.File.Exists(filePath))
                    {
                        fileResult = new FileOperationResult(false, $"Failed: Attempted to modify file '{filePath}' which does not exist (possible race or validation gap).");
                        _logger.LogError("Modify operation failed for '{FilePath}' because it does not exist.", filePath);
                    }
                    else
                    {
                        SendStatusUpdate($"Modifying file: {System.IO.Path.GetFileName(filePath)} ({modifyChanges.Count} changes)");
                        var handler = new ModifyFileHandler(_logger, _statusMessageService, _clientId, _secondaryAiService);
                        fileResult = await handler.HandleModifyFileAsync(filePath, modifyChanges); // Pass all modify changes
                    }
                }
                else
                {
                    // Should not happen if validation requires at least one change per file entry
                    fileResult = new FileOperationResult(false, "Skipped: No effective change operation determined (internal error or validation gap).");
                    _logger.LogWarning("No effective change operation determined for file '{FilePath}' during sequential processing.", filePath);
                }

                taskSuccess = fileResult.Success;
                taskResultMessage = fileResult.Message;

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
    }
}