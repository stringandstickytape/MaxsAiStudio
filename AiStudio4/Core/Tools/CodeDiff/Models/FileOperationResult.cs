// AiStudio4.Core\Tools\CodeDiff\Models\FileOperationResult.cs
namespace AiStudio4.Core.Tools.CodeDiff.Models
{
    /// <summary>
    /// Represents the result of a file operation
    /// </summary>
    public class FileOperationResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// A message describing the result
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Creates a new FileOperationResult
        /// </summary>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="message">A message describing the result</param>
        public FileOperationResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
        
        /// <summary>
        /// Implicitly converts a tuple to a FileOperationResult
        /// </summary>
        public static implicit operator FileOperationResult((bool Success, string Message) tuple)
        {
            return new FileOperationResult(tuple.Success, tuple.Message);
        }
        
        /// <summary>
        /// Implicitly converts a FileOperationResult to a tuple
        /// </summary>
        public static implicit operator (bool Success, string Message)(FileOperationResult result)
        {
            return (result.Success, result.Message);
        }
    }
}