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
            modifiedContent = originalContent;
            failureReason = null;
            
            // Normalize the original content line endings for comparison
            string normalizedOriginalContent = NormalizeLineEndings(originalContent);
            
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
                
                // Find the actual oldContent in the original text with proper line endings
                int startIndex = FindNormalizedStringPosition(modifiedContent, normalizedOriginalContent, normalizedOldContent);
                if (startIndex >= 0)
                {
                    int endIndex = startIndex + GetActualLength(modifiedContent, startIndex, oldContent.Length);
                    string actualOldContent = modifiedContent.Substring(startIndex, endIndex - startIndex);
                    
                    // Apply the change preserving the original line endings
                    modifiedContent = modifiedContent.Remove(startIndex, endIndex - startIndex);
                    modifiedContent = modifiedContent.Insert(startIndex, newContent);
                    
                    // Update normalized content for next iteration
                    normalizedOriginalContent = NormalizeLineEndings(modifiedContent);
                }
                else
                {
                    // Fallback to simple replace if position finding fails
                    modifiedContent = modifiedContent.Replace(oldContent, newContent);
                    normalizedOriginalContent = NormalizeLineEndings(modifiedContent);
                }
                
                // Log successful change
                _logger.LogInformation("Successfully applied programmatic change to file '{FilePath}'.", filePath);
            }
            
            return true;
        }

        /// <summary>
        /// Counts the number of occurrences of a pattern in a text
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
            
            while (normalizedPos < normalizedIndex && originalIndex < originalText.Length)
            {
                // Skip \r in original text when counting positions
                if (originalText[originalIndex] == '\r' && originalIndex + 1 < originalText.Length && originalText[originalIndex + 1] == '\n')
                {
                    originalIndex++; // Skip the \r
                }
                else
                {
                    normalizedPos++;
                }
                originalIndex++;
            }
            
            return originalIndex;
        }
        
        /// <summary>
        /// Gets the actual length of a string in the original text accounting for line ending differences
        /// </summary>
        private int GetActualLength(string originalText, int startIndex, int normalizedLength)
        {
            int endIndex = startIndex;
            int charsProcessed = 0;
            
            while (charsProcessed < normalizedLength && endIndex < originalText.Length)
            {
                if (originalText[endIndex] == '\r' && endIndex + 1 < originalText.Length && originalText[endIndex + 1] == '\n')
                {
                    // Count \r\n as one character for normalized length
                    endIndex += 2;
                    charsProcessed++;
                }
                else
                {
                    endIndex++;
                    charsProcessed++;
                }
            }
            
            return endIndex - startIndex;
        }

        /// <summary>
        /// Saves debug information when automatic merging fails
        /// </summary>
        /// <param name="filePath">Path to the file being modified</param>
        /// <param name="originalContent">Original content of the file</param>
        /// <param name="changes">List of changes that were attempted</param>
        /// <param name="failureReason">The reason why the automatic merge failed</param>
        public void SaveMergeFailureDebugInfo(string filePath, string originalContent, List<JObject> changes, string failureReason)
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