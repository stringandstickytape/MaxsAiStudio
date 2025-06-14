using AiStudio4.Convs;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using System.Threading;


namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Defines operations for processing tool/function calls within a chat context.
    /// </summary>
    public interface IToolProcessorService
    {
        /// <summary>
        /// Processes tools from an AI response and determines if further processing is needed
        /// </summary>
        /// <param name="response">The AI response containing potential tool calls</param>
        /// <param name="conv">The current conversation state</param>
        /// <param name="collatedResponse">Builder to accumulate tool execution outputs</param>
        /// <returns>Whether to continue the tool processing loop</returns>
        Task<ToolExecutionResult> ProcessToolsAsync(AiResponse response, LinearConv conv, CancellationToken cancellationToken = default, string clientId = null);

        /// <summary>
        /// Re-applies a built-in tool with its original parameters
        /// </summary>
        /// <param name="toolName">The name of the tool to re-apply</param>
        /// <param name="toolParameters">The JSON parameters for the tool</param>
        /// <param name="clientId">The client ID making the request</param>
        /// <returns>The result of the tool execution</returns>
        Task<BuiltinToolResult> ReapplyToolAsync(string toolName, string toolParameters, string clientId);
    }
}
