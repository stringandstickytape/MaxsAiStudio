namespace AiStudio4.Tools.Models
{
    /// <summary>
    /// Result from processing a built-in tool
    /// </summary>
    public class BuiltinToolResult
    {
        /// <summary>
        /// Indicates whether the tool was recognized and processed as a built-in tool
        /// </summary>
        public bool WasProcessed { get; set; }

        /// <summary>
        /// Indicates whether the tool loop should continue processing
        /// Set to false for tools like Stop that should terminate processing
        /// </summary>
        public bool ContinueProcessing { get; set; } = true;

        /// <summary>
        /// Any message to include in the result
        /// </summary>
        public string ResultMessage { get; set; } = string.Empty;

        /// <summary>
        /// Any attachments to include with the result
        /// </summary>
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// User interjection content if one was received during tool execution
        /// </summary>
        public string UserInterjection { get; set; }

        /// <summary>
        /// Task description extracted from tool parameters for UI display
        /// </summary>
        public string TaskDescription { get; set; }

        /// <summary>
        /// Output file type for formatting the result content (e.g., "json", "xml", "bash")
        /// </summary>
        public string OutputFileType { get; set; }
    }

    /// <summary>
    /// Represents an attachment in a tool result
    /// </summary>
    public class Attachment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public long Size { get; set; }

        // Optional metadata
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string TextContent { get; set; }
        public long? LastModified { get; set; }
    }
}