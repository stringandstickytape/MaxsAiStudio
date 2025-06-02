
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

        
        
        
        
        
        
        
        
        
        public bool TryApplyModifications(string filePath, string originalContent, List<JObject> changes, out string modifiedContent, out string failureReason)
        {
            
            failureReason = null;
            
            
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
                
                
                string normalizedOldContent = NormalizeLineEndings(oldContent);
                
                
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
                    
                    int startIndex = FindNormalizedStringPosition(modifiedContent, normalizedOriginalContent, normalizedOldContent);
                    if (startIndex >= 0)
                    {
                        
                        int actualLength = GetActualLength(modifiedContent, startIndex, normalizedOldContent.Length);
                        
                        
                        string actualOldContent = modifiedContent.Substring(startIndex, actualLength);
                        string normalizedActualOldContent = NormalizeLineEndings(actualOldContent);
                        
                        if (normalizedActualOldContent != normalizedOldContent)
                        {
                            
                            _logger.LogWarning("Substring mismatch in file '{FilePath}'. Expected '{Expected}' but got '{Actual}'.", 
                                filePath, normalizedOldContent, normalizedActualOldContent);
                            
                            
                            _logger.LogDebug("Expected normalized content: '{Expected}'", normalizedOldContent);
                            _logger.LogDebug("Actual normalized content: '{Actual}'", normalizedActualOldContent);
                            _logger.LogDebug("Start index: {StartIndex}, Actual length: {ActualLength}", startIndex, actualLength);
                            
                            failureReason = $"Substring mismatch when trying to replace content. Change index: {changes.IndexOf(change)}";
                            return false;
                        }
                        
                        
                        modifiedContent = modifiedContent.Remove(startIndex, actualLength);
                        modifiedContent = modifiedContent.Insert(startIndex, newContent);
                        
                        
                        normalizedOriginalContent = NormalizeLineEndings(modifiedContent);
                    }
                    else
                    {
                        
                        _logger.LogWarning("Failed to find position for content in file '{FilePath}' despite occurrence check passing.", filePath);
                        failureReason = $"Failed to find position for content. Change index: {changes.IndexOf(change)}";
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    
                    _logger.LogError(ex, "Error applying change to file '{FilePath}'. Change index: {ChangeIndex}", 
                        filePath, changes.IndexOf(change));
                    failureReason = $"Error applying change: {ex.Message}. Change index: {changes.IndexOf(change)}";
                    return false;
                }
                
                
                _logger.LogInformation("Successfully applied programmatic change to file '{FilePath}'.", filePath);
            }
            
            return true;
        }
        
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
        
        
        
        
        private string NormalizeLineEndings(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }
        
        
        
        
        private int FindNormalizedStringPosition(string originalText, string normalizedText, string normalizedPattern)
        {
            int normalizedIndex = normalizedText.IndexOf(normalizedPattern, StringComparison.Ordinal);
            if (normalizedIndex < 0)
                return -1;
                
            
            int originalIndex = 0;
            int normalizedPos = 0;
            
            
            while (normalizedPos < normalizedIndex && originalIndex < originalText.Length)
            {
                
                if (originalText[originalIndex] == '\r' && originalIndex + 1 < originalText.Length && originalText[originalIndex + 1] == '\n')
                {
                    
                    originalIndex++;
                }
                else
                {
                    
                    normalizedPos++;
                }
                originalIndex++;
            }
            
            return originalIndex;
        }
        
        
        private int GetActualLength(string originalText, int startIndex, int normalizedLength)
        {
            
            string normalizedOriginal = NormalizeLineEndings(originalText);
            
            
            int normalizedStartIndex = 0;
            int originalPos = 0;
            
            
            while (originalPos < startIndex && originalPos < originalText.Length)
            {
                if (originalText[originalPos] == '\r' && originalPos + 1 < originalText.Length && originalText[originalPos + 1] == '\n')
                {
                    
                    originalPos++;
                }
                else
                {
                    normalizedStartIndex++;
                }
                originalPos++;
            }
            
            
            int normalizedEndIndex = normalizedStartIndex + normalizedLength;
            
            
            int originalEndPos = startIndex;
            int normalizedPos = normalizedStartIndex;
            
            while (normalizedPos < normalizedEndIndex && originalEndPos < originalText.Length)
            {
                if (originalText[originalEndPos] == '\r' && originalEndPos + 1 < originalText.Length && originalText[originalEndPos + 1] == '\n')
                {
                    
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
        }        
        
        
        
        
        
        public void SaveMergeDebugInfo(string filePath, string originalContent, List<JObject> changes, string failureReason)
        {
            try
            {
                
                string debugDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "DebugLogs", "MergeFailures");
                Directory.CreateDirectory(debugDir);
                
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string filename = Path.GetFileName(filePath);
                string debugFilePath = Path.Combine(debugDir, $"merge_failure_{timestamp}_{filename}.json");
                
                
                var debugData = new
                {
                    Timestamp = DateTime.Now,
                    FilePath = filePath,
                    FailureReason = failureReason,
                    Changes = changes,
                    OriginalContent = originalContent
                };
                
                
                string json = JsonConvert.SerializeObject(debugData, Formatting.Indented);
                File.WriteAllText(debugFilePath, json, Encoding.UTF8);
                
                _logger.LogInformation("Saved merge failure debug info to {DebugFilePath}", debugFilePath);
            }
            catch (Exception ex)
            {
                
                _logger.LogWarning(ex, "Failed to save merge failure debug info for {FilePath}", filePath);
            }
        }

        
        
        
        private async void SendStatusUpdate(string statusMessage)
        {
            try
            {
                
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
                
                _logger.LogWarning(ex, "Failed to send status update: {Message}", statusMessage);
            }
        }
    }
}
