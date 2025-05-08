// AiStudio4.Core\Tools\CodeDiff\Models\PathValidationDetails.cs
namespace AiStudio4.Core.Tools.CodeDiff.Models
{
    /// <summary>
    /// Holds validation details for a specific file path during CodeDiff processing
    /// </summary>
    public class PathValidationDetails
    {
        /// <summary>
        /// The normalized file path
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// Whether the path has a delete operation
        /// </summary>
        public bool HasDelete { get; set; }
        
        /// <summary>
        /// Whether the path has a rename operation
        /// </summary>
        public bool HasRename { get; set; }
        
        /// <summary>
        /// Whether the path has a replace operation
        /// </summary>
        public bool HasReplace { get; set; }
        
        /// <summary>
        /// Whether the path has a create operation
        /// </summary>
        public bool HasCreate { get; set; }
        
        /// <summary>
        /// Whether the path has a modify operation
        /// </summary>
        public bool HasModify { get; set; }
        
        /// <summary>
        /// The normalized target path for rename operations
        /// </summary>
        public string RenameTargetPath { get; set; }
    }
}