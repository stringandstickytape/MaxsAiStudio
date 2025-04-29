using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Tools.CodeDiff.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers
{
    /// <summary>
    /// Handles file modification operations, first attempting programmatic modification
    /// before falling back to using a secondary AI service
    /// </summary>
    public class ModifyFileHandler : BaseFileOperationHandler
    {
        private readonly ISecondaryAiService _secondaryAiService;
        private const string SuccessMarker = "SUCCESS"; // Marker for successful AI content processing
        private const string ErrorMarker = "ERROR:"; // Marker for AI processing errors

        public ModifyFileHandler(ILogger logger, IStatusMessageService statusMessageService, 
            string clientId, ISecondaryAiService secondaryAiService) 
            : base(logger, statusMessageService, clientId)
        {
            _secondaryAiService = secondaryAiService ?? throw new ArgumentNullException(nameof(secondaryAiService));
        }

        /// <summary>
        /// Handles the file modification operation
        /// </summary>
        public override async Task<FileOperationResult> HandleAsync(string filePath, JObject change)
        {
            // For ModifyFileHandler, we expect a list of changes rather than a single change
            // This is handled by the CodeDiffTool class which collects all modify changes for a file
            // and passes them as a list to this handler
            return await HandleModifyFileAsync(filePath, new List<JObject> { change });
        }

        /// <summary>
        /// Handles multiple modification operations for a single file
        /// First attempts programmatic modification, then falls back to AI if needed
        /// </summary>
        public async Task<FileOperationResult> HandleModifyFileAsync(string filePath, List<JObject> changes)
        {
            string originalContent;
            try
            {
                // Double check existence - handled by caller ProcessSingleFileSequentiallyAsync
                if (!File.Exists(filePath))
                {
                    _logger.LogError("Modify Failed: File '{FilePath}' not found.", filePath);
                    return new FileOperationResult(false, $"Failed: File not found.");
                }
                originalContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error reading file '{FilePath}' for modification.", filePath);
                return new FileOperationResult(false, $"Failed: IO Error reading file. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error reading file '{FilePath}' for modification.", filePath);
                return new FileOperationResult(false, $"Failed: Permissions error reading file. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error reading file '{FilePath}' for modification", filePath);
                return new FileOperationResult(false, $"Failed: Unexpected error reading file. {ex.Message}");
            }

            // Only proceed if there are valid modifications to apply
            if (!changes.Any())
            {
                _logger.LogWarning("Modify operation for '{FilePath}' called with no valid modification details.", filePath);
                return new FileOperationResult(true, "Success: No modifications to apply."); // Or false if this state is unexpected
            }

            // NEW: First try programmatic modification
            var programmaticModifier = new ProgrammaticModifier(_logger, _statusMessageService, _clientId);

            programmaticModifier.SaveMergeDebugInfo(filePath, originalContent, changes, "");

            string failureReason = "";

            if (false) // temporarily disabled, do not remove
            {
                SendStatusUpdate($"Attempting programmatic modification for: {Path.GetFileName(filePath)}");
                
                try
                {
                    if (programmaticModifier.TryApplyModifications(filePath, originalContent, changes, out string modifiedContent, out failureReason))
                    {
                        // If successful, write the modified content and return success
                        await File.WriteAllTextAsync(filePath, modifiedContent, Encoding.UTF8);
                        _logger.LogInformation("Programmatically modified file '{FilePath}' with {Count} change(s).", filePath, changes.Count);
                        SendStatusUpdate($"Successfully applied {changes.Count} modification(s) programmatically to {Path.GetFileName(filePath)}");
                        return new FileOperationResult(true, $"Success: Applied {changes.Count} modification(s) programmatically.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during programmatic modification attempt for '{FilePath}'. Falling back to AI.", filePath);
                    SendStatusUpdate($"Error during programmatic modification: {ex.Message}. Falling back to AI.");
                    // Continue to AI-based approach
                }
            }
            // If programmatic modification failed or threw an exception, fall back to AI-based approach
            SendStatusUpdate($"Programmatic modification unsuccessful for: {Path.GetFileName(filePath)}. Falling back to AI-based approach.");
            
            // Save debug information for the merge failure
            programmaticModifier.SaveMergeDebugInfo(filePath, originalContent, changes, failureReason);
            
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
                SendStatusUpdate($"Processing with Secondary AI for: {Path.GetFileName(filePath)}");
                var aiResponse = await _secondaryAiService.ProcessRequestAsync(prompt);

                if (!aiResponse.Success) // Allow empty response if successful AI call intended it
                {
                    string errorMsg = $"Modify Failed: Secondary AI failed to process modifications for '{filePath}'. Error: {aiResponse.Error ?? "Unknown AI Error"}. Response: {aiResponse.Response ?? "<null>"}";
                    _logger.LogError("Secondary AI failed modifications for '{FilePath}'. Error: {Error}. Response: {Response}", filePath, aiResponse.Error, aiResponse.Response);
                    return new FileOperationResult(false, $"Failed: AI modification processing failed. {aiResponse.Error ?? "See Logs"}");
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
                return new FileOperationResult(true, $"Success: Applied {changes.Count} modification(s).");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error writing modified file '{FilePath}'.", filePath);
                return new FileOperationResult(false, $"Failed: IO Error writing file. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error writing modified file '{FilePath}'.", filePath);
                return new FileOperationResult(false, $"Failed: Permissions error writing file. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during AI call or writing modified file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: Unexpected error during AI processing/write. {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to reapply a previously failed merge operation from a merge failure JSON file
        /// </summary>
        /// <param name="mergeFailureJsonPath">Path to the merge_failure_{timestamp}_{filename}.json file</param>
        /// <param name="logger">Logger instance for logging operations</param>
        /// <param name="statusMessageService">Status message service for sending updates</param>
        /// <param name="clientId">Client ID for status updates</param>
        /// <param name="secondaryAiService">Secondary AI service for processing if needed</param>
        /// <returns>A FileOperationResult indicating success or failure</returns>
        public static async Task<FileOperationResult> ReapplyMergeFailureAsync(
            string mergeFailureJsonPath,
            ILogger logger,
            IStatusMessageService statusMessageService,
            string clientId,
            ISecondaryAiService secondaryAiService)
        {
            if (!File.Exists(mergeFailureJsonPath))
            {
                logger.LogError("Merge failure file not found: {Path}", mergeFailureJsonPath);
                return new FileOperationResult(false, $"Failed: Merge failure file not found: {mergeFailureJsonPath}");
            }

            try
            {
                // Read and parse the merge failure JSON file
                string jsonContent = await File.ReadAllTextAsync(mergeFailureJsonPath);
                JObject mergeFailureData = JObject.Parse(jsonContent);

                // Extract the original file path from the merge failure data
                string originalFilePath = mergeFailureData["filePath"]?.ToString();
                if (string.IsNullOrEmpty(originalFilePath))
                {
                    logger.LogError("Invalid merge failure file: Missing file path");
                    return new FileOperationResult(false, "Failed: Invalid merge failure file: Missing file path");
                }

                // Extract the original content and changes
                string originalContent = mergeFailureData["originalContent"]?.ToString();
                JArray changesArray = mergeFailureData["changes"] as JArray;

                if (string.IsNullOrEmpty(originalContent) || changesArray == null || !changesArray.Any())
                {
                    logger.LogError("Invalid merge failure file: Missing content or changes");
                    return new FileOperationResult(false, "Failed: Invalid merge failure file: Missing content or changes");
                }

                // Convert JArray to List<JObject>
                List<JObject> changes = changesArray.Select(c => c as JObject).Where(c => c != null).ToList();

                // Check if the original file still exists
                if (!File.Exists(originalFilePath))
                {
                    logger.LogError("Original file no longer exists: {Path}", originalFilePath);
                    return new FileOperationResult(false, $"Failed: Original file no longer exists: {originalFilePath}");
                }

                // Create a ModifyFileHandler instance and apply the changes
                var handler = new ModifyFileHandler(logger, statusMessageService, clientId, secondaryAiService);
                return await handler.HandleModifyFileAsync(originalFilePath, changes);
            }
            catch (JsonException jsonEx)
            {
                logger.LogError(jsonEx, "Error parsing merge failure JSON: {Path}", mergeFailureJsonPath);
                return new FileOperationResult(false, $"Failed: Error parsing merge failure JSON: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error reapplying merge failure: {Path}", mergeFailureJsonPath);
                return new FileOperationResult(false, $"Failed: Unexpected error reapplying merge failure: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds the most recent merge failure file for a given file path
        /// </summary>
        /// <param name="originalFilePath">The original file path that had a merge failure</param>
        /// <returns>Path to the most recent merge failure file, or null if none found</returns>
        public static string FindMostRecentMergeFailureFile(string originalFilePath)
        {
            try
            {
                string fileName = Path.GetFileName(originalFilePath);
                string directory = Path.GetDirectoryName(originalFilePath);

                if (string.IsNullOrEmpty(directory))
                {
                    directory = Directory.GetCurrentDirectory();
                }

                // Pattern for merge failure files: merge_failure_{timestamp}_{filename}.json
                string pattern = $"merge_failure_.*_{Regex.Escape(fileName)}\\.json";
                var mergeFailureFiles = Directory.GetFiles(directory)
                    .Where(f => Regex.IsMatch(Path.GetFileName(f), pattern))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                return mergeFailureFiles.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}