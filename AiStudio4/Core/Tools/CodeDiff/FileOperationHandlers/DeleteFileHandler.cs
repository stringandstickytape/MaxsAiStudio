// AiStudio4.Core\Tools\CodeDiff\FileOperationHandlers\DeleteFileHandler.cs

using AiStudio4.Core.Tools.CodeDiff.Models;






namespace AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers
{
    /// <summary>
    /// Handles file deletion operations
    /// </summary>
    public class DeleteFileHandler : BaseFileOperationHandler
    {
        public DeleteFileHandler(ILogger logger, IStatusMessageService statusMessageService, string clientId) 
            : base(logger, statusMessageService, clientId)
        {
        }

        /// <summary>
        /// Handles the file deletion operation
        /// </summary>
        public override async Task<FileOperationResult> HandleAsync(string filePath, JObject change)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    // This might be acceptable if a previous step (like rename) moved it, but generally indicates an issue.
                    _logger.LogWarning("Delete request ignored: File '{FilePath}' not found (might have been previously renamed/deleted).", filePath);
                    // Returning true as the desired state (file doesn't exist) is achieved, but log warning.
                    return new FileOperationResult(true, "Success: File not found or already deleted.");
                    // Alternatively, return (false, "Failed: File not found.") if non-existence should be an error. Choose based on desired strictness.
                }
                File.Delete(filePath);
                _logger.LogInformation("Deleted file '{FilePath}'", filePath);
                return new FileOperationResult(true, "Success: File deleted.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error deleting file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: IO Error. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error deleting file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: Permissions error. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: Unexpected error. {ex.Message}");
            }
        }
    }
}
