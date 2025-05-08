// AiStudio4.Core\Tools\CodeDiff\FileOperationHandlers\BaseFileOperationHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Tools.CodeDiff.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers
{
    /// <summary>
    /// Base abstract class for file operation handlers
    /// </summary>
    public abstract class BaseFileOperationHandler
    {
        protected readonly ILogger _logger;
        protected readonly IStatusMessageService _statusMessageService;
        protected readonly string _clientId;

        protected BaseFileOperationHandler(ILogger logger, IStatusMessageService statusMessageService, string clientId)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statusMessageService = statusMessageService;
            _clientId = clientId;
        }

        /// <summary>
        /// Handles the file operation
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <param name="change">The change to apply</param>
        /// <returns>A result indicating success or failure</returns>
        public abstract Task<FileOperationResult> HandleAsync(string filePath, JObject change);

        /// <summary>
        /// Sends a status update using StatusMessageService if available
        /// </summary>
        /// <param name="statusMessage">The status message to send</param>
        protected async void SendStatusUpdate(string statusMessage)
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

        /// <summary>
        /// Ensures the directory for a file exists, creating it if necessary
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        protected void EnsureDirectoryExists(string filePath)
        {
            string targetDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
                _logger.LogInformation("Created directory '{DirectoryPath}' for file operation.", targetDir);
            }
        }

        /// <summary>
        /// Removes surrounding backticks and language specifier if present.
        /// </summary>
        protected static string RemoveBacktickQuotingIfPresent(string content)
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
    }
}