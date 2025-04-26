// AiStudio4.Core\Tools\CodeDiff\ProgrammaticModifier.cs
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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
        /// <returns>True if all changes were applied successfully, false otherwise</returns>
        public bool TryApplyModifications(string filePath, string originalContent, List<JObject> changes, out string modifiedContent)
        {
            modifiedContent = originalContent;
            
            foreach (var change in changes)
            {
                string oldContent = change["oldContent"]?.ToString();
                string newContent = change["newContent"]?.ToString();
                
                if (string.IsNullOrEmpty(oldContent))
                {
                    _logger.LogWarning("Skipping change with empty oldContent for file '{FilePath}'.", filePath);
                    continue;
                }
                
                // Check if oldContent exists exactly once in the current content
                int occurrences = CountOccurrences(modifiedContent, oldContent);
                
                if (occurrences == 0)
                {
                    _logger.LogWarning("Cannot find oldContent in file '{FilePath}'. Falling back to AI.", filePath);
                    return false;
                }
                
                if (occurrences > 1)
                {
                    _logger.LogWarning("Multiple matches ({Count}) for oldContent in file '{FilePath}'. Falling back to AI.", occurrences, filePath);
                    return false;
                }
                
                // Apply the change
                modifiedContent = modifiedContent.Replace(oldContent, newContent);
                
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