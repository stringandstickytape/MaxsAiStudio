// AiStudio4.Core\Tools\CodeDiff\FileOperationHandlers\RenameFileHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Tools.CodeDiff.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers
{
    /// <summary>
    /// Handles file rename operations
    /// </summary>
    public class RenameFileHandler : BaseFileOperationHandler
    {
        public RenameFileHandler(ILogger logger, IStatusMessageService statusMessageService, string clientId) 
            : base(logger, statusMessageService, clientId)
        {
        }

        /// <summary>
        /// Handles the file rename operation
        /// </summary>
        public override async Task<FileOperationResult> HandleAsync(string oldFilePath, JObject change)
        {
            string newFilePath = change["newContent"]?.ToString();
            // newFilePath validity and security (within project root) checked during validation phase

            if (string.IsNullOrEmpty(newFilePath)) // Should be caught by validation
            {
                _logger.LogError("Rename Failed: 'newContent' (new path) is missing or invalid for rename on '{OldFilePath}' (Validation Gap?).", oldFilePath);
                return new FileOperationResult(false, "Failed: New path missing or invalid.");
            }

            try
            {
                if (!File.Exists(oldFilePath))
                {
                    _logger.LogWarning("Rename Failed: Source file '{OldFilePath}' not found.", oldFilePath);
                    return new FileOperationResult(false, "Failed: Source file not found.");
                }
                if (File.Exists(newFilePath))
                {
                    _logger.LogError("Rename Failed: Target file '{NewFilePath}' already exists.", newFilePath);
                    return new FileOperationResult(false, $"Failed: Target file '{newFilePath}' already exists.");
                }

                // Ensure target directory exists
                EnsureDirectoryExists(newFilePath);

                File.Move(oldFilePath, newFilePath);
                _logger.LogInformation("Renamed file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return new FileOperationResult(true, $"Success: Renamed to '{newFilePath}'.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error renaming file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return new FileOperationResult(false, $"Failed: IO Error. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error renaming file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return new FileOperationResult(false, $"Failed: Permissions error. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error renaming file '{OldFilePath}' to '{NewFilePath}'", oldFilePath, newFilePath);
                return new FileOperationResult(false, $"Failed: Unexpected error. {ex.Message}");
            }
        }
    }
}