// AiStudio4.Core\Tools\CodeDiff\FileOperationHandlers\ModifyFileHandler.cs
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
            SendStatusUpdate($"Attempting programmatic modification for: {Path.GetFileName(filePath)}");
            string failureReason = "";
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
            
            // If programmatic modification failed or threw an exception, fall back to AI-based approach
            SendStatusUpdate($"Programmatic modification unsuccessful for: {Path.GetFileName(filePath)}. Falling back to AI-based approach.");
            
            // Save debug information for the merge failure
            programmaticModifier.SaveMergeFailureDebugInfo(filePath, originalContent, changes, failureReason);
            
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
    }
}