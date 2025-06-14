// AiStudio4.Core\Tools\CodeDiff\FileOperationHandlers\CreateFileHandler.cs

using AiStudio4.Core.Tools.CodeDiff.Models;







namespace AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers
{
    /// <summary>
    /// Handles file creation operations
    /// </summary>
    public class CreateFileHandler : BaseFileOperationHandler
    {
        public CreateFileHandler(ILogger logger, IStatusMessageService statusMessageService, string clientId) 
            : base(logger, statusMessageService, clientId)
        {
        }

        /// <summary>
        /// Handles the file creation operation
        /// </summary>
        public override async Task<FileOperationResult> HandleAsync(string filePath, JObject change)
        {
            string content = change["newContent"]?.ToString();
            string changeDesc = change["description"]?.ToString() ?? "create file content";
            
            // For create operations, allow empty string as default
            content = content ?? "";

            try
            {
                // Ensure target directory exists
                EnsureDirectoryExists(filePath);

                // Clean up content if it has markdown formatting
                content = RemoveBacktickQuotingIfPresent(content);
                
                // Write the file directly
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                _logger.LogInformation("Created file '{FilePath}' with direct content writing.", filePath);
                return new FileOperationResult(true, "Success: File created.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error creating file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: IO Error. {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError(uaEx, "Permissions error creating file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: Permissions error. {uaEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating file '{FilePath}'", filePath);
                return new FileOperationResult(false, $"Failed: Unexpected error. {ex.Message}");
            }
        }
    }
}
