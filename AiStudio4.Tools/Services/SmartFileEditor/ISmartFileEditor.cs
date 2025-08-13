using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Tools.Services.SmartFileEditor
{
    /// <summary>
    /// Interface for smart file editing with intelligent error feedback
    /// </summary>
    public interface ISmartFileEditor
    {
        /// <summary>
        /// Applies a series of edits to a file with smart error handling
        /// </summary>
        Task<EditResult> ApplyEditsAsync(string filePath, List<FileEdit> edits);
        
        /// <summary>
        /// Applies a single edit to file content (for testing/preview)
        /// </summary>
        EditResult ApplyEditToContent(string content, FileEdit edit);
    }
}