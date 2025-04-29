// AiStudio4.Core\Tools\CodeDiff\ProgrammaticModifier.cs
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AiStudio4.Core.Tools.CodeDiff
{
    /// <summary>
    /// Handles programmatic file modifications by applying changes directly without using AI
    /// </summary>
    public class ProgrammaticModifier
    {
        private readonly ILogger _logger;
        private readonly IStatusMessageService _statusMessageService;
        private readonly string _clientId;

        public ProgrammaticModifier(ILogger logger, IStatusMessageService statusMessageService, string clientId)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statusMessageService = statusMessageService;
            _clientId = clientId;
        }

        /// <summary>
        /// Attempts to apply modifications programmatically to a file
        /// </summary>
        /// <param name="filePath">Path to the file being modified</param>
        /// <param name="originalContent">Original content of the file</param>
        /// <param name="changes">List of changes to apply</param>
        /// <param name="modifiedContent">Output parameter that will contain the modified content if successful</param>
        /// <param name="failureReason">Output parameter that will contain the reason for failure if unsuccessful</param>
        /// <returns>True if all changes were applied successfully, false otherwise</returns>
        public bool TryApplyModifications(string filePath, string originalContent, List<JObject> changes, out string modifiedContent, out string failureReason)
        {
            
            failureReason = null;
            
            // Normalize the original content line endings for comparison
            string normalizedOriginalContent = NormalizeLineEndings(originalContent);
            modifiedContent = normalizedOriginalContent;
            foreach (var change in changes)
            {
                string oldContent = change["oldContent"]?.ToString();
                string newContent = change["newContent"]?.ToString();
                
                if (string.IsNullOrEmpty(oldContent))
                {
                    _logger.LogWarning("Skipping change with empty oldContent for file '{FilePath}'.", filePath);
                    continue;
                }
                
                // Normalize the oldContent line endings for comparison
                string normalizedOldContent = NormalizeLineEndings(oldContent);
                
                // Check if normalized oldContent exists exactly once in the normalized current content
                int occurrences = CountOccurrences(normalizedOriginalContent, normalizedOldContent);
                
                if (occurrences == 0)
                {
                    failureReason = $"Cannot find oldContent in file. Change index: {changes.IndexOf(change)}";
                    _logger.LogWarning("Cannot find oldContent in file '{FilePath}'. Falling back to AI.", filePath);
                    return false;
                }
                
                if (occurrences > 1)
                {
                    failureReason = $"Multiple matches ({occurrences}) for oldContent in file. Change index: {changes.IndexOf(change)}";
                    _logger.LogWarning("Multiple matches ({Count}) for oldContent in file '{FilePath}'. Falling back to AI.", occurrences, filePath);
                    return false;
                }
                
                try
                {
                    // Find the actual oldContent in the original text with proper line endings
                    int startIndex = FindNormalizedStringPosition(modifiedContent, normalizedOriginalContent, normalizedOldContent);
                    if (startIndex >= 0)
                    {
                        // Calculate the actual length in the original text accounting for line endings
                        int actualLength = GetActualLength(modifiedContent, startIndex, normalizedOldContent.Length);
                        
                        // Verify that the substring we're about to replace matches our normalized pattern
                        string actualOldContent = modifiedContent.Substring(startIndex, actualLength);
                        string normalizedActualOldContent = NormalizeLineEndings(actualOldContent);
                        
                        if (normalizedActualOldContent != normalizedOldContent)
                        {
                            // The calculated substring doesn't match what we expected to replace
                            _logger.LogWarning("Substring mismatch in file '{FilePath}'. Expected '{Expected}' but got '{Actual}'.", 
                                filePath, normalizedOldContent, normalizedActualOldContent);
                            
                            // Log the mismatch details for debugging
                            _logger.LogDebug("Expected normalized content: '{Expected}'", normalizedOldContent);
                            _logger.LogDebug("Actual normalized content: '{Actual}'", normalizedActualOldContent);
                            _logger.LogDebug("Start index: {StartIndex}, Actual length: {ActualLength}", startIndex, actualLength);
                            
                            failureReason = $"Substring mismatch when trying to replace content. Change index: {changes.IndexOf(change)}";
                            return false;
                        }
                        
                        // Apply the change preserving the original line endings
                        modifiedContent = modifiedContent.Remove(startIndex, actualLength);
                        modifiedContent = modifiedContent.Insert(startIndex, newContent);
                        
                        // Update normalized content for next iteration
                        normalizedOriginalContent = NormalizeLineEndings(modifiedContent);
                    }
                    else
                    {
                        // Position finding failed - this shouldn't happen if we already verified occurrences == 1
                        _logger.LogWarning("Failed to find position for content in file '{FilePath}' despite occurrence check passing.", filePath);
                        failureReason = $"Failed to find position for content. Change index: {changes.IndexOf(change)}";
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // Log any exceptions during the replacement process
                    _logger.LogError(ex, "Error applying change to file '{FilePath}'. Change index: {ChangeIndex}", 
                        filePath, changes.IndexOf(change));
                    failureReason = $"Error applying change: {ex.Message}. Change index: {changes.IndexOf(change)}";
                    return false;
                }
                
                // Log successful change
                _logger.LogInformation("Successfully applied programmatic change to file '{FilePath}'.", filePath);
            }
            
            return true;
        }
        /// </summary>
        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int i = 0;
            
            while ((i = text.IndexOf(pattern, i, StringComparison.Ordinal)) != -1)
            {
                count++;
                i += pattern.Length;
            }
            
            return count;
        }
        
        /// <summary>
        /// Normalizes line endings to \n for consistent comparison
        /// </summary>
        private string NormalizeLineEndings(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            // First replace all \r\n with \n, then replace any remaining \r with \n
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }
        
        /// <summary>
        /// Finds the position of a normalized string within the original text
        /// </summary>
        private int FindNormalizedStringPosition(string originalText, string normalizedText, string normalizedPattern)
        {
            int normalizedIndex = normalizedText.IndexOf(normalizedPattern, StringComparison.Ordinal);
            if (normalizedIndex < 0)
                return -1;
                
            // Count characters in the original text up to the normalized position
            int originalIndex = 0;
            int normalizedPos = 0;
            
            // Map from normalized position to original text position
            while (normalizedPos < normalizedIndex && originalIndex < originalText.Length)
            {
                // Handle CRLF sequences in original text
                if (originalText[originalIndex] == '\r' && originalIndex + 1 < originalText.Length && originalText[originalIndex + 1] == '\n')
                {
                    // For CRLF, increment original position by 1 but don't increment normalized position yet
                    originalIndex++;
                }
                else
                {
                    // For all other characters, increment both positions
                    normalizedPos++;
                }
                originalIndex++;
            }
            
            return originalIndex;
        }
        /// Gets the actual length of a string in the original text accounting for line ending differences
        /// </summary>
        private int GetActualLength(string originalText, int startIndex, int normalizedLength)
        {
            // Create a normalized version of the original text for comparison
            string normalizedOriginal = NormalizeLineEndings(originalText);
            
            // Find the corresponding substring in the normalized text
            int normalizedStartIndex = 0;
            int originalPos = 0;
            
            // Map the original startIndex to the normalized text position
            while (originalPos < startIndex && originalPos < originalText.Length)
            {
                if (originalText[originalPos] == '\r' && originalPos + 1 < originalText.Length && originalText[originalPos + 1] == '\n')
                {
                    // Skip the \r in CRLF sequence when counting normalized positions
                    originalPos++;
                }
                else
                {
                    normalizedStartIndex++;
                }
                originalPos++;
            }
            
            // Calculate the end position in the normalized text
            int normalizedEndIndex = normalizedStartIndex + normalizedLength;
            
            // Map back to the original text to find the actual end position
            int originalEndPos = startIndex;
            int normalizedPos = normalizedStartIndex;
            
            while (normalizedPos < normalizedEndIndex && originalEndPos < originalText.Length)
            {
                if (originalText[originalEndPos] == '\r' && originalEndPos + 1 < originalText.Length && originalText[originalEndPos + 1] == '\n')
                {
                    // For CRLF, increment original position by 2 but normalized by only 1
                    originalEndPos += 2;
                    normalizedPos++;
                }
                else
                {
                    originalEndPos++;
                    normalizedPos++;
                }
            }
            
            return originalEndPos - startIndex;
        }        /// Saves debug information when automatic merging fails
        /// </summary>
        /// <param name="filePath">Path to the file being modified</param>
        /// <param name="originalContent">Original content of the file</param>
        /// <param name="changes">List of changes that were attempted</param>
        /// <param name="failureReason">The reason why the automatic merge failed</param>
        public void SaveMergeDebugInfo(string filePath, string originalContent, List<JObject> changes, string failureReason)
        {
            try
            {
                // Create debug directory if it doesn't exist
                string debugDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DebugLogs", "MergeFailures");
                Directory.CreateDirectory(debugDir);
                
                // Create a unique filename based on timestamp and file being modified
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string filename = Path.GetFileName(filePath);
                string debugFilePath = Path.Combine(debugDir, $"merge_failure_{timestamp}_{filename}.json");
                
                // Create debug data object
                var debugData = new
                {
                    Timestamp = DateTime.Now,
                    FilePath = filePath,
                    FailureReason = failureReason,
                    Changes = changes,
                    OriginalContent = originalContent
                };
                
                // Serialize to JSON and save to file
                string json = JsonConvert.SerializeObject(debugData, Formatting.Indented);
                File.WriteAllText(debugFilePath, json, Encoding.UTF8);
                
                _logger.LogInformation("Saved merge failure debug info to {DebugFilePath}", debugFilePath);
            }
            catch (Exception ex)
            {
                // Log but don't throw - debug info saving should never break tool execution
                _logger.LogWarning(ex, "Failed to save merge failure debug info for {FilePath}", filePath);
            }
        }

        /// <summary>
        /// Sends a status update using StatusMessageService if available
        /// </summary>
        private async void SendStatusUpdate(string statusMessage)
        {
            try
            {
                // Send via StatusMessageService if available and clientId is set
                if (_statusMessageService != null && !string.IsNullOrEmpty(_clientId))
                {
                    await _statusMessageService.SendStatusMessageAsync(_clientId, statusMessage);
                }
                else
                {
                    _logger.LogDebug("Status update not sent - missing StatusMessageService or clientId: {Message}", statusMessage);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - status updates should never break tool execution
                _logger.LogWarning(ex, "Failed to send status update: {Message}", statusMessage);
            }
        }
    }
}