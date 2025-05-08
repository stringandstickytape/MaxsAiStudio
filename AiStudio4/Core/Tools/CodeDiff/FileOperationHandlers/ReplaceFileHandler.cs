// AiStudio4.Core\Tools\CodeDiff\FileOperationHandlers\ReplaceFileHandler.cs
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
    /// Handles file replacement operations
    /// </summary>
    public class ReplaceFileHandler : BaseFileOperationHandler
    {
        public ReplaceFileHandler(ILogger logger, IStatusMessageService statusMessageService, string clientId) 
            : base(logger, statusMessageService, clientId)
        {
        }

        /// <summary>
        /// Handles the file replacement operation
        /// </summary>
        public override async Task<FileOperationResult> HandleAsync(string filePath, JObject change)
        {
            string content = change["newContent"]?.ToString();
            string changeDesc = change["description"]?.ToString() ?? "replace file content";
            
            // For replace operations, content is required (should be caught by validation)
            if (content == null)
            {
                _logger.LogError("Operation Failed: 'newContent' is missing for replace operation on '{FilePath}' (Validation Gap?).", filePath);
                return new FileOperationResult(false, "Failed: New content missing.");
            }

            try
            {
                // Ensure target directory exists
                EnsureDirectoryExists(filePath);

                // Clean up content if it has markdown formatting
                content = RemoveBacktickQuotingIfPresent(content);
                
                // Write the file directly
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                _logger.LogInformation("Replaced file '{FilePath}' with direct content writing.", filePath);
                return new FileOperationResult(true, "Success: File replaced.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error replacing file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: IO Error. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error replacing file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: Permissions error. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error replacing file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: Unexpected error. {ex.Message}");
            }
        }
    }
}