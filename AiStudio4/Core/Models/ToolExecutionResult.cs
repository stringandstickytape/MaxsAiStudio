using AiStudio4.DataModels;
using System.Collections.Generic;
using System.Text;

namespace AiStudio4.Core.Models
{
    /// <summary>
    /// Represents the result of executing tools in a conversation
    /// </summary>
    public class ToolExecutionResult
    {
        /// <summary>
        /// The aggregated output text from all tool executions in this iteration
        /// </summary>
        public string AggregatedToolOutput { get; set; }

        /// <summary>
        /// Any file attachments generated during tool execution
        /// </summary>
        public List<Attachment> Attachments { get; set; }

        /// <summary>
        /// Indicates whether the execution completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if execution was not successful
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Flag to determine if the tool processing loop should continue
        /// </summary>
        public bool ShouldContinueToolLoop { get; set; }

        /// <summary>
        /// A summary of the tool(s) requested by the AI in this iteration (including parameter hashes)
        /// </summary>
        public string RequestedToolsSummary { get; set; }


        /// <summary>
        /// Creates a new successful tool execution result
        /// </summary>
        public static ToolExecutionResult Successful(string aggregatedToolOutput, List<Attachment> attachments = null)
        {
            return new ToolExecutionResult
            {
                AggregatedToolOutput = aggregatedToolOutput,
                Attachments = attachments,
                Success = true
            };
        }

        /// <summary>
        /// Creates a new failed tool execution result
        /// </summary>
        public static ToolExecutionResult Failed(string errorMessage)
        {
            return new ToolExecutionResult
            {
                ErrorMessage = errorMessage,
                Success = false
            };
        }
    }
}
