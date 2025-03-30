using AiStudio4.DataModels;
using System;
using System.Collections.Generic;

namespace AiStudio4.Core.Models
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
    }
}
