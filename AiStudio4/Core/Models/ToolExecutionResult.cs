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
        /// The final response text after all tool executions
        /// </summary>
        public string ResponseText { get; set; }

        /// <summary>
        /// The final cost information accumulated across all tool executions
        /// </summary>
        public TokenCost CostInfo { get; set; }

        /// <summary>
        /// Any file attachments generated during tool execution
        /// </summary>
        public List<Attachment> Attachments { get; set; }

        /// <summary>
        /// Number of tool execution iterations performed
        /// </summary>
        public int IterationCount { get; set; }

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
        public bool ContinueProcessing { get; set; }

        /// <summary>
        /// The name of the tool that was requested by the AI
        /// </summary>
        public string ToolRequested { get; set; }

        /// <summary>
        /// The result of running the tool
        /// </summary>
        public string ToolResult { get; set; }

        /// <summary>
        /// Creates a new successful tool execution result
        /// </summary>
        public static ToolExecutionResult Successful(string responseText, TokenCost costInfo = null, List<Attachment> attachments = null, int iterationCount = 0)
        {
            return new ToolExecutionResult
            {
                ResponseText = responseText,
                CostInfo = costInfo,
                Attachments = attachments,
                IterationCount = iterationCount,
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
